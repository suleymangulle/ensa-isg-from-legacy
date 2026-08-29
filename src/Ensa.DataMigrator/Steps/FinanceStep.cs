using Ensa.DataMigrator.Infrastructure;
using Ensa.Domain.Companies;
using Ensa.Domain.Finance;
using Ensa.Domain.Shared.Enums;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Ensa.DataMigrator.Steps;

/// <summary>
/// The money: invoices and their lines, the company ledger, the cash registers and what went
/// through them. 194,000 rows across nine legacy tables.
/// <para>
/// This is the module the business is actually run from, and the one where a wrong number is not
/// a display bug. Every amount arrives as a legacy <c>float</c> and is stored as
/// <c>decimal(18,2)</c> — the destination is decimal precisely so that a total is a total rather
/// than something within 2^-52 of one.
/// </para>
/// <para>
/// <b>The invoice number is not one.</b> The rebuilt schema declares it unique within a tenant.
/// The legacy data says otherwise: 4,375 invoices have no number at all, and among those that do,
/// 3,816 repeat within their organization — one organization writes the literal text "EARSIV" on
/// 1,689 invoices to 190 different companies. Only 16 of the repeats are the same company, date
/// and total, so these are different invoices sharing a number, not duplicates. The numbers are
/// carried verbatim and the index is relaxed to non-unique: the constraint was invented during
/// the rewrite and the data it has to hold does not satisfy it. Inventing suffixes would have
/// fabricated fiscal document numbers.
/// </para>
/// </summary>
public sealed class FinanceStep : IMigrationStep
{
    public int Order => 98;

    public string Name => "finance";

    public string Description => "Invoices, invoice lines, the company ledger, cash registers and cash transactions";

    private const int BatchSize = 500;

    public async Task<StepResult> RunAsync(
        MigrationContext context,
        CancellationToken cancellationToken = default)
    {
        using var scope = context.EnterMigrationScope();

        var results = new List<StepResult>
        {
            await PaymentMethodsAsync(context, cancellationToken),
            await ServiceItemsAsync(context, cancellationToken),
            await ExpenseCategoriesAsync(context, cancellationToken),
            await ExpenseCategoryParentsAsync(context, cancellationToken),
            await CashRegistersAsync(context, cancellationToken),
            await CashTransactionsAsync(context, cancellationToken),
            await InvoicesAsync(context, cancellationToken),
            await InvoiceLinesAsync(context, cancellationToken),
            await LedgerAsync(context, cancellationToken),
            await OfficeExpensesAsync(context, cancellationToken),
        };

        return new StepResult(
            results.Sum(r => r.Read),
            results.Sum(r => r.Written),
            results.Sum(r => r.Skipped),
            string.Join("; ", results.Select(r => r.Note).Where(note => note is not null)));
    }

    // ------------------------------------------------------------------ the small lists

    /// <summary><c>OdemeTuru_T</c> to <see cref="PaymentMethod"/>: cash, cheque, promissory note.</summary>
    private static Task<StepResult> PaymentMethodsAsync(MigrationContext context, CancellationToken cancellationToken)
        => CopyAsync<PaymentMethod>(
            context, "OdemeTuru_T", "payment methods",
            "SELECT OdemeTuruId, OdemeTuru FROM OdemeTuru_T ORDER BY OdemeTuruId;",
            "OdemeTuruId",
            (reader, _) => new PaymentMethod
            {
                Name = Fit(context, "PaymentMethod", "Name", Text(reader, "OdemeTuru")) ?? string.Empty,
                IsActive = true,
                CreationTime = DateTime.Now,
            },
            cancellationToken);

    /// <summary><c>HizmetKartlari_T</c> to <see cref="ServiceItem"/> — the priced service cards.</summary>
    private static async Task<StepResult> ServiceItemsAsync(
        MigrationContext context,
        CancellationToken cancellationToken)
    {
        var organizationMap = await context.IdMap.LoadAsync("Firma_T:Kurum", cancellationToken);

        return await CopyAsync<ServiceItem>(
            context, "HizmetKartlari_T", "service items",
            """
            SELECT HizmetKartiID, Kodu, HizmetKarti, Birimi, DefaultDeger, KdvOrani, KartTuru, KurumId
            FROM HizmetKartlari_T ORDER BY HizmetKartiID;
            """,
            "HizmetKartiID",
            (reader, _) => new ServiceItem
            {
                Code = Fit(context, "ServiceItem", "Code", Text(reader, "Kodu")) ?? string.Empty,
                Name = Fit(context, "ServiceItem", "Name", Text(reader, "HizmetKarti")) ?? string.Empty,
                Unit = Fit(context, "ServiceItem", "Unit", Text(reader, "Birimi")) ?? string.Empty,
                DefaultValue = Int(reader, "DefaultDeger") ?? 0,
                VatRate = Int(reader, "KdvOrani") ?? 0,
                CardType = ServiceItemTypeOf(Text(reader, "KartTuru")),
                IsActive = true,
                CreationTime = DateTime.Now,
                TenantId = organizationMap.TryGetValue(Required(reader, "KurumId"), out var tenantId) ? tenantId : null,
            },
            cancellationToken);
    }

    /// <summary>
    /// <c>CikisKalem_T</c> to <see cref="ExpenseCategory"/> — what money leaving the cash register
    /// was spent on. Written without its parent, which the next pass fills in.
    /// </summary>
    private static async Task<StepResult> ExpenseCategoriesAsync(
        MigrationContext context,
        CancellationToken cancellationToken)
    {
        var organizationMap = await context.IdMap.LoadAsync("Firma_T:Kurum", cancellationToken);

        return await CopyAsync<ExpenseCategory>(
            context, "CikisKalem_T", "expense categories",
            "SELECT KalemId, Aciklama, Aktif, KurumId, EklemeTarihi FROM CikisKalem_T ORDER BY KalemId;",
            "KalemId",
            (reader, _) => new ExpenseCategory
            {
                Description = Fit(context, "ExpenseCategory", "Description", Text(reader, "Aciklama")) ?? string.Empty,

                // A category can name a parent that appears later in the table, so the reference
                // is set once every row exists rather than guessed at from insertion order.
                ParentExpenseCategoryId = null,

                IsActive = Bit(reader, "Aktif"),
                CreationTime = Date(reader, "EklemeTarihi") ?? DateTime.Now,
                TenantId = Int(reader, "KurumId") is int legacyTenantId
                           && organizationMap.TryGetValue(legacyTenantId, out var tenantId)
                    ? tenantId
                    : null,
            },
            cancellationToken);
    }

    /// <summary>The second pass over <c>CikisKalem_T</c>, now that every category has an id.</summary>
    private static async Task<StepResult> ExpenseCategoryParentsAsync(
        MigrationContext context,
        CancellationToken cancellationToken)
    {
        await using var db = context.CreateDbContext();

        var categoryMap = await context.IdMap.LoadAsync("CikisKalem_T", cancellationToken);
        if (categoryMap.Count == 0 || context.DryRun)
        {
            return new StepResult(0, 0, 0, null);
        }

        var parents = new Dictionary<int, int>();

        await using (var connection = await context.OpenLegacyAsync(cancellationToken))
        await using (var command = new SqlCommand(
                         "SELECT KalemId, UstKalemId FROM CikisKalem_T WHERE UstKalemId IS NOT NULL;",
                         connection))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                if (categoryMap.TryGetValue(Required(reader, "KalemId"), out var childId)
                    && categoryMap.TryGetValue(Required(reader, "UstKalemId"), out var parentId))
                {
                    parents[childId] = parentId;
                }
            }
        }

        if (parents.Count == 0)
        {
            return new StepResult(0, 0, 0, null);
        }

        var ids = parents.Keys.ToList();
        var rows = await db.Set<ExpenseCategory>()
            .Where(c => ids.Contains(c.Id) && c.ParentExpenseCategoryId == null)
            .ToListAsync(cancellationToken);

        foreach (var row in rows)
        {
            row.ParentExpenseCategoryId = parents[row.Id];
        }

        await db.SaveChangesAsync(cancellationToken);

        return new StepResult(parents.Count, rows.Count, 0, $"expense category parents: {rows.Count} linked");
    }

    // ------------------------------------------------------------------ cash

    private static async Task<StepResult> CashRegistersAsync(
        MigrationContext context,
        CancellationToken cancellationToken)
    {
        var officeMap = await context.IdMap.LoadAsync("Ofisler_T", cancellationToken);
        var organizationMap = await context.IdMap.LoadAsync("Firma_T:Kurum", cancellationToken);

        return await CopyAsync<CashRegister>(
            context, "Kasa_T", "cash registers",
            """
            SELECT KasaId, KasaAdi, OfisId, MerkezKasa, KurumId, EklemeTarihi, GuncellemeTarihi
            FROM Kasa_T ORDER BY KasaId;
            """,
            "KasaId",
            (reader, orphan) =>
            {
                if (!officeMap.TryGetValue(Required(reader, "OfisId"), out var officeId))
                {
                    orphan();
                    return null;
                }

                return new CashRegister
                {
                    CashRegisterName = Fit(context, "CashRegister", "CashRegisterName", Text(reader, "KasaAdi")) ?? string.Empty,
                    OfficeId = officeId,
                    IsHeadquarterCashRegister = Bit(reader, "MerkezKasa"),
                    IsActive = true,
                    CreationTime = Date(reader, "EklemeTarihi") ?? DateTime.Now,
                    LastModificationTime = Date(reader, "GuncellemeTarihi"),
                    TenantId = organizationMap.TryGetValue(Required(reader, "KurumId"), out var tenantId) ? tenantId : null,
                };
            },
            cancellationToken);
    }

    private static async Task<StepResult> CashTransactionsAsync(
        MigrationContext context,
        CancellationToken cancellationToken)
    {
        var registerMap = await context.IdMap.LoadAsync("Kasa_T", cancellationToken);
        var methodMap = await context.IdMap.LoadAsync("OdemeTuru_T", cancellationToken);
        var categoryMap = await context.IdMap.LoadAsync("CikisKalem_T", cancellationToken);
        var organizationMap = await context.IdMap.LoadAsync("Firma_T:Kurum", cancellationToken);

        return await CopyAsync<CashTransaction>(
            context, "KasaDetay_T", "cash transactions",
            """
            SELECT KasaDetayId, KasaId, OdemeTuruId, IslemTuru, IslemTutari, Aciklama,
                   CikisKalemId, modul, IslemTarihi, Aktif, KurumId, EklemeTarihi, GuncellemeTarihi
            FROM KasaDetay_T ORDER BY KasaDetayId;
            """,
            "KasaDetayId",
            (reader, orphan) =>
            {
                if (!registerMap.TryGetValue(Required(reader, "KasaId"), out var registerId)
                    || !methodMap.TryGetValue(Required(reader, "OdemeTuruId"), out var methodId))
                {
                    orphan();
                    return null;
                }

                return new CashTransaction
                {
                    CashRegisterId = registerId,
                    PaymentMethodId = methodId,
                    OperationType = CashTypeOf(Text(reader, "IslemTuru")),
                    OperationAmount = Money(reader, "IslemTutari"),
                    Description = Fit(context, "CashTransaction", "Description", Text(reader, "Aciklama")),
                    SourceModule = SourceModuleOf(Text(reader, "modul")),

                    // IslemId names a row in whichever module the text column points at, and the
                    // three modules that occur here - collection, cash withdrawal, partner
                    // contribution - have no table of their own in the rebuilt schema. A legacy id
                    // copied into a column read as a modern one would point at an unrelated row.
                    SourceRecordId = null,

                    ExitItemId = Lookup(categoryMap, Int(reader, "CikisKalemId")),
                    OperationDate = Date(reader, "IslemTarihi") ?? Date(reader, "EklemeTarihi"),
                    IsActive = Bit(reader, "Aktif"),
                    CreationTime = Date(reader, "EklemeTarihi") ?? DateTime.Now,
                    LastModificationTime = Date(reader, "GuncellemeTarihi"),
                    TenantId = organizationMap.TryGetValue(Required(reader, "KurumId"), out var tenantId) ? tenantId : null,
                };
            },
            cancellationToken);
    }

    // ------------------------------------------------------------------ invoices

    private static async Task<StepResult> InvoicesAsync(
        MigrationContext context,
        CancellationToken cancellationToken)
    {
        var companyMap = await LoadCompanyMapAsync(context, cancellationToken);
        var officeMap = await context.IdMap.LoadAsync("Ofisler_T", cancellationToken);
        var organizationMap = await context.IdMap.LoadAsync("Firma_T:Kurum", cancellationToken);

        return await CopyAsync<Invoice>(
            context, "Faturalar_T", "invoices",
            """
            SELECT FaturaId, FaturaNo, FirmaId, FaturaTarihi, Toplam, KdvToplam, GenelToplam,
                   Yaziyla, Turu, Modul, FaturaAciklamasi, Sube_ID, CariAdi, KurumId, EklemeTarihi
            FROM Faturalar_T ORDER BY FaturaId;
            """,
            "FaturaId",
            (reader, orphan) =>
            {
                if (!companyMap.TryGetValue(Required(reader, "FirmaId"), out var companyId)
                    || !organizationMap.TryGetValue(Required(reader, "KurumId"), out var tenantId))
                {
                    orphan();
                    return null;
                }

                return new Invoice
                {
                    // Verbatim, blanks included. See the type's remarks: this column is not the
                    // unique document number the rebuilt schema took it for.
                    InvoiceNo = Fit(context, "Invoice", "InvoiceNo", Text(reader, "FaturaNo")) ?? string.Empty,

                    CompanyId = companyId,
                    InvoiceDate = Date(reader, "FaturaTarihi") ?? Date(reader, "EklemeTarihi") ?? DateTime.Now,
                    InvoiceType = InvoiceTypeOf(Text(reader, "Turu")),
                    SourceModule = SourceModuleOf(Text(reader, "Modul")),
                    OfficeId = Lookup(officeMap, Int(reader, "Sube_ID")),
                    AccountCurrentName = Fit(context, "Invoice", "AccountCurrentName", Text(reader, "CariAdi")) ?? string.Empty,
                    InvoiceDescription = Fit(context, "Invoice", "InvoiceDescription", Text(reader, "FaturaAciklamasi")),
                    InWords = Fit(context, "Invoice", "InWords", Text(reader, "Yaziyla")),
                    Total = Money(reader, "Toplam"),
                    VatTotal = Money(reader, "KdvToplam"),
                    GeneralTotal = Money(reader, "GenelToplam"),
                    CreationTime = Date(reader, "EklemeTarihi") ?? DateTime.Now,
                    TenantId = tenantId,
                };
            },
            cancellationToken);
    }

    private static async Task<StepResult> InvoiceLinesAsync(
        MigrationContext context,
        CancellationToken cancellationToken)
    {
        var invoiceMap = await context.IdMap.LoadAsync("Faturalar_T", cancellationToken);
        var serviceMap = await context.IdMap.LoadAsync("HizmetKartlari_T", cancellationToken);
        var companyMap = await LoadCompanyMapAsync(context, cancellationToken);
        var organizationMap = await context.IdMap.LoadAsync("Firma_T:Kurum", cancellationToken);

        // The legacy table records no line order, so it is the order the rows are in — which, read
        // by id, is the order they were entered.
        var ordinals = new Dictionary<int, int>();

        return await CopyAsync<InvoiceLine>(
            context, "FaturaSatirlari_T", "invoice lines",
            """
            SELECT FaturaSatirId, FaturaId, HizmetKalemi, SatirAciklama, Adet, Birim, Tutar,
                   ToplamTutar, Kdv, KdvTutari, KdvliTutar, FirmaId, KurumId
            FROM FaturaSatirlari_T ORDER BY FaturaSatirId;
            """,
            "FaturaSatirId",
            (reader, orphan) =>
            {
                var legacyInvoiceId = Required(reader, "FaturaId");

                if (!invoiceMap.TryGetValue(legacyInvoiceId, out var invoiceId)
                    || !organizationMap.TryGetValue(Required(reader, "KurumId"), out var tenantId))
                {
                    orphan();
                    return null;
                }

                ordinals.TryGetValue(legacyInvoiceId, out var ordinal);
                ordinals[legacyInvoiceId] = ordinal + 1;

                return new InvoiceLine
                {
                    InvoiceId = invoiceId,
                    ServiceItemId = Lookup(serviceMap, Int(reader, "HizmetKalemi")),
                    LineDescription = Fit(context, "InvoiceLine", "LineDescription", Text(reader, "SatirAciklama")) ?? string.Empty,
                    Count = Int(reader, "Adet") ?? 0,
                    Unit = Fit(context, "InvoiceLine", "Unit", Text(reader, "Birim")) ?? string.Empty,
                    UnitPrice = Money(reader, "Tutar"),
                    TotalAmount = Money(reader, "ToplamTutar"),
                    VatRate = Int(reader, "Kdv") ?? 0,
                    VatAmount = Money(reader, "KdvTutari"),
                    GrossWithVatAmount = Money(reader, "KdvliTutar"),
                    CompanyId = Lookup(companyMap, Int(reader, "FirmaId")),
                    OrderNo = ordinal + 1,
                    CreationTime = DateTime.Now,
                    TenantId = tenantId,
                };
            },
            cancellationToken);
    }

    // ------------------------------------------------------------------ the ledger

    /// <summary>
    /// <c>FirmaHareket_T</c> to <see cref="CompanyLedgerEntry"/>: what each company owes and has
    /// paid.
    /// <para>
    /// The legacy table keeps debit and credit in two columns; the rebuilt one keeps a side and an
    /// amount. That is only a safe collapse because no row uses both — checked across all 79,817:
    /// 58,871 are debit only, 20,874 credit only, none both, and 72 are zero on both sides. The 72
    /// are carried as debits of zero, because they exist in the ledger and their description is
    /// the record of something.
    /// </para>
    /// </summary>
    private static async Task<StepResult> LedgerAsync(
        MigrationContext context,
        CancellationToken cancellationToken)
    {
        var companyMap = await LoadCompanyMapAsync(context, cancellationToken);
        var invoiceMap = await context.IdMap.LoadAsync("Faturalar_T", cancellationToken);
        var organizationMap = await context.IdMap.LoadAsync("Firma_T:Kurum", cancellationToken);

        return await CopyAsync<CompanyLedgerEntry>(
            context, "FirmaHareket_T", "company ledger",
            """
            SELECT FirmaHareketId, FirmaId, Tarih, Aciklama, Borc, Alacak, ResmiHesap,
                   Modul, IslemId, KurumId, EklemeTarihi
            FROM FirmaHareket_T ORDER BY FirmaHareketId;
            """,
            "FirmaHareketId",
            (reader, orphan) =>
            {
                if (!companyMap.TryGetValue(Required(reader, "FirmaId"), out var companyId)
                    || !organizationMap.TryGetValue(Required(reader, "KurumId"), out var tenantId))
                {
                    orphan();
                    return null;
                }

                var credit = MoneyOrNull(reader, "Alacak") ?? 0m;
                var debit = MoneyOrNull(reader, "Borc") ?? 0m;
                var module = SourceModuleOf(Text(reader, "Modul"));

                return new CompanyLedgerEntry
                {
                    CompanyId = companyId,
                    Date = Date(reader, "Tarih") ?? Date(reader, "EklemeTarihi") ?? DateTime.Now,
                    LedgerEntryType = credit != 0m ? LedgerEntryType.Credit : LedgerEntryType.Debit,
                    Amount = credit != 0m ? credit : debit,
                    Description = Fit(context, "CompanyLedgerEntry", "Description", Text(reader, "Aciklama")),
                    OfficialAccount = Bit(reader, "ResmiHesap"),
                    SourceModule = module,

                    // Translated only where it can be: an invoice line in the ledger names a real
                    // invoice. A collection names a row in a module the rebuilt schema does not
                    // have, so it is left unset rather than pointed somewhere plausible.
                    OperationId = module == SourceModule.Invoice
                        ? Lookup(invoiceMap, Int(reader, "IslemId"))
                        : null,

                    CreationTime = Date(reader, "EklemeTarihi") ?? DateTime.Now,
                    TenantId = tenantId,
                };
            },
            cancellationToken);
    }

    private static async Task<StepResult> OfficeExpensesAsync(
        MigrationContext context,
        CancellationToken cancellationToken)
    {
        var officeMap = await context.IdMap.LoadAsync("Ofisler_T", cancellationToken);
        var organizationMap = await context.IdMap.LoadAsync("Firma_T:Kurum", cancellationToken);

        return await CopyAsync<OfficeExpense>(
            context, "FirmaGider_T", "office expenses",
            """
            SELECT FirmaGiderId, GiderTanimi, Tutar, GiderTarihi, OfisId, KurumId, EklemeTarihi
            FROM FirmaGider_T ORDER BY FirmaGiderId;
            """,
            "FirmaGiderId",
            (reader, orphan) =>
            {
                if (!officeMap.TryGetValue(Required(reader, "OfisId"), out var officeId))
                {
                    orphan();
                    return null;
                }

                return new OfficeExpense
                {
                    ExpenseTag = Fit(context, "OfficeExpense", "ExpenseTag", Text(reader, "GiderTanimi")) ?? string.Empty,
                    Amount = Money(reader, "Tutar"),
                    ExpenseDate = Date(reader, "GiderTarihi"),
                    OfficeId = officeId,
                    CreationTime = Date(reader, "EklemeTarihi") ?? DateTime.Now,
                    TenantId = organizationMap.TryGetValue(Required(reader, "KurumId"), out var tenantId) ? tenantId : null,
                };
            },
            cancellationToken);
    }

    // ------------------------------------------------------------------ value mapping

    /// <summary><c>Faturalar_T.Turu</c>: "Satis" or "Alis". The legacy system records no returns.</summary>
    private static InvoiceType InvoiceTypeOf(string? type)
        => Fold(type) == "ALIS" ? InvoiceType.Purchase : InvoiceType.Sale;

    /// <summary><c>KasaDetay_T.IslemTuru</c>: "giris" or "cikis".</summary>
    private static CashTransactionType CashTypeOf(string? type)
        => Fold(type) == "CIKIS" ? CashTransactionType.Outflow : CashTransactionType.Inflow;

    /// <summary>
    /// The Turkish module labels the legacy tables write into a free-text column.
    /// <para>
    /// "Acilis" is an opening balance and "Hesap Islemleri" an account adjustment; neither is a
    /// module, both are entries somebody made by hand, which is what
    /// <see cref="SourceModule.Manual"/> is for. Anything unrecognised stays
    /// <see cref="SourceModule.Unspecified"/> rather than being filed under the nearest guess.
    /// </para>
    /// </summary>
    private static SourceModule SourceModuleOf(string? module)
    {
        var folded = Fold(module);

        if (folded is null)
        {
            return SourceModule.Unspecified;
        }

        if (folded.Contains("FATURA", StringComparison.Ordinal))
        {
            return SourceModule.Invoice;
        }

        if (folded.Contains("TAHSILAT", StringComparison.Ordinal))
        {
            return SourceModule.Collection;
        }

        if (folded.Contains("CIKIS", StringComparison.Ordinal) || folded.Contains("GIDER", StringComparison.Ordinal))
        {
            return SourceModule.Expense;
        }

        if (folded.Contains("KASA", StringComparison.Ordinal))
        {
            return SourceModule.CashRegister;
        }

        return folded is "ACILIS" or "HESAP ISLEMLERI" or "ORTAKLARDAN GIRIS"
            ? SourceModule.Manual
            : SourceModule.Unspecified;
    }

    /// <summary>
    /// <c>HizmetKartlari_T.KartTuru</c> names the staff role the card is priced for — "Uzman",
    /// "Doktor", "Diger" — not the kind of service, which is what
    /// <see cref="ServiceItemType"/> asks. A safety specialist's card is an OHS service and a
    /// physician's is health screening; that inference is the whole of the mapping, and anything
    /// else, "seradmin" included, is left unspecified rather than forced into a category.
    /// </summary>
    private static ServiceItemType ServiceItemTypeOf(string? cardType)
        => Fold(cardType) switch
        {
            "UZMAN" => ServiceItemType.OhsService,
            "DOKTOR" => ServiceItemType.HealthScreening,
            "DIGER" => ServiceItemType.Other,
            _ => ServiceItemType.Unspecified,
        };

    private static string? Fold(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var folded = value.Trim()
            .Replace('ı', 'i').Replace('İ', 'I')
            .Replace('ş', 's').Replace('Ş', 'S')
            .Replace('ğ', 'g').Replace('Ğ', 'G')
            .Replace('ü', 'u').Replace('Ü', 'U')
            .Replace('ö', 'o').Replace('Ö', 'O')
            .Replace('ç', 'c').Replace('Ç', 'C')
            .ToUpperInvariant();

        return folded.Length == 0 ? null : folded;
    }

    // ------------------------------------------------------------------ the shared copy

    private static async Task<StepResult> CopyAsync<TEntity>(
        MigrationContext context,
        string legacyTable,
        string label,
        string sql,
        string keyColumn,
        Func<SqlDataReader, Action, TEntity?> project,
        CancellationToken cancellationToken)
        where TEntity : class
    {
        await using var db = context.CreateDbContext();

        var already = await context.IdMap.LoadAsync(legacyTable, cancellationToken);

        var read = 0;
        var written = 0;
        var orphaned = 0;
        var pairs = new List<(int, int)>();
        var batch = new List<(int LegacyId, TEntity Entity)>();

        await using (var connection = await context.OpenLegacyAsync(cancellationToken))
        await using (var command = new SqlCommand(sql, connection) { CommandTimeout = 3600 })
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                read++;

                var legacyId = Required(reader, keyColumn);
                if (already.ContainsKey(legacyId))
                {
                    continue;
                }

                var wasOrphaned = false;
                var entity = project(reader, () => wasOrphaned = true);

                if (entity is null || wasOrphaned)
                {
                    orphaned++;
                    continue;
                }

                batch.Add((legacyId, entity));

                if (batch.Count >= BatchSize && !context.DryRun)
                {
                    written += await FlushAsync(db, context, legacyTable, batch, pairs, cancellationToken);
                }
            }
        }

        if (batch.Count > 0)
        {
            written += context.DryRun
                ? DryRunFlush(context, batch, pairs)
                : await FlushAsync(db, context, legacyTable, batch, pairs, cancellationToken);
        }

        var note = $"{label}: {written} written";
        if (orphaned > 0)
        {
            note += $", {orphaned} SKIPPED (a referenced record is missing)";
        }

        return new StepResult(read, written, orphaned, note);
    }

    // ------------------------------------------------------------------ helpers

    private static async Task<Dictionary<int, int>> LoadCompanyMapAsync(
        MigrationContext context,
        CancellationToken cancellationToken)
    {
        var map = new Dictionary<int, int>(await context.IdMap.LoadAsync("Firma_T", cancellationToken));

        foreach (var (legacyId, modernId) in
                 await context.IdMap.LoadAsync("Firma_T:KurumSirket", cancellationToken))
        {
            map[legacyId] = modernId;
        }

        return map;
    }

    private static async Task<int> FlushAsync<TEntity>(
        DbContext db,
        MigrationContext context,
        string legacyTable,
        List<(int LegacyId, TEntity Entity)> batch,
        List<(int, int)> pairs,
        CancellationToken cancellationToken)
        where TEntity : class
    {
        db.Set<TEntity>().AddRange(batch.Select(item => item.Entity));
        await db.SaveChangesAsync(cancellationToken);

        var chunkPairs = batch
            .Select(item => (item.LegacyId, (int)db.Entry(item.Entity).Property("Id").CurrentValue!))
            .ToList();

        await context.IdMap.SaveAsync(legacyTable, chunkPairs, 'I', cancellationToken);
        pairs.AddRange(chunkPairs);

        var count = batch.Count;
        batch.Clear();
        db.ChangeTracker.Clear();

        return count;
    }

    private static int DryRunFlush<TEntity>(
        MigrationContext context,
        List<(int LegacyId, TEntity Entity)> batch,
        List<(int, int)> pairs)
    {
        pairs.AddRange(batch.Select(item => (item.LegacyId, context.NextDryRunId())));

        var count = batch.Count;
        batch.Clear();
        return count;
    }

    /// <summary>
    /// A legacy <c>float</c> amount as the <c>decimal(18,2)</c> the destination stores.
    /// <para>
    /// Rounded away from zero at two places, the way money is rounded rather than the way IEEE
    /// rounds. A value the column cannot hold is not silently truncated to something plausible —
    /// it becomes zero, which is visibly wrong, rather than a wrong number that looks right.
    /// </para>
    /// </summary>
    private static decimal Money(SqlDataReader reader, string column)
        => MoneyOrNull(reader, column) ?? 0m;

    private static decimal? MoneyOrNull(SqlDataReader reader, string column)
    {
        var index = reader.GetOrdinal(column);
        if (reader.IsDBNull(index))
        {
            return null;
        }

        var value = Convert.ToDouble(reader.GetValue(index));

        if (double.IsNaN(value) || double.IsInfinity(value) || Math.Abs(value) >= 1e15)
        {
            return 0m;
        }

        return Math.Round((decimal)value, 2, MidpointRounding.AwayFromZero);
    }

    private static int? Lookup(Dictionary<int, int> map, int? legacyId)
        => legacyId is int id && map.TryGetValue(id, out var modernId) ? modernId : null;

    private static string? Fit(MigrationContext context, string table, string column, string? value)
        => context.Fitter.Fit(table, column, value);

    private static string? Text(SqlDataReader reader, string column)
    {
        var index = reader.GetOrdinal(column);
        if (reader.IsDBNull(index))
        {
            return null;
        }

        var value = reader.GetValue(index)?.ToString()?.Trim();
        return string.IsNullOrEmpty(value) ? null : value;
    }

    private static int? Int(SqlDataReader reader, string column)
    {
        var index = reader.GetOrdinal(column);
        return reader.IsDBNull(index) ? null : Convert.ToInt32(reader.GetValue(index));
    }

    private static DateTime? Date(SqlDataReader reader, string column)
    {
        var index = reader.GetOrdinal(column);
        return reader.IsDBNull(index) ? null : reader.GetDateTime(index);
    }

    private static bool Bit(SqlDataReader reader, string column)
    {
        var index = reader.GetOrdinal(column);
        return !reader.IsDBNull(index) && Convert.ToBoolean(reader.GetValue(index));
    }

    private static int Required(SqlDataReader reader, string column)
        => reader.GetInt32(reader.GetOrdinal(column));
}
