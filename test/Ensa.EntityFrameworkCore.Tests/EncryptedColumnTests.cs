using Ensa.Domain.Companies;
using Ensa.Domain.Shared.Enums;
using Ensa.TestBase;
using Microsoft.EntityFrameworkCore;

namespace Ensa.EntityFrameworkCore.Tests;

/// <summary>
/// Integration tests for the encrypted columns, against a real LocalDB database.
/// <para>
/// Two properties have to hold at once and they pull against each other: the value must be
/// unreadable at rest, and the column must still support equality and a unique index — a national
/// id is looked up by value and may not be registered twice. That is why the converter is
/// deterministic AES with a fixed IV (ADR-005). A round-trip test alone would pass even if the
/// value were stored in plain text, so the ciphertext is asserted directly with raw SQL.
/// </para>
/// </summary>
public class EncryptedColumnTests : IAsyncLifetime
{
    private const string NationalId = "12345678950";

    private EnsaTestFixture _fixture = null!;

    public Task InitializeAsync()
    {
        _fixture = new EnsaTestFixture(tenantId: 1, userId: 1, databaseCreate: true);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _fixture.DisposeAsync();

    private static CompanyEmployee NewEmployee(string nationalId, string name) => new()
    {
        CompanyId = 1,
        Name = name,
        LastName = "Test",
        NationalId = nationalId,
        Gender = Gender.Unspecified,
        IsActive = true
    };

    private async Task<int> InsertAsync(CompanyEmployee employee)
    {
        await using var context = _fixture.CreateContext();
        context.Set<CompanyEmployee>().Add(employee);
        await context.SaveChangesAsync();
        return employee.Id;
    }

    [Fact]
    public async Task Round_trips_the_value_through_the_database()
    {
        var id = await InsertAsync(NewEmployee(NationalId, "Ayse"));

        await using var context = _fixture.CreateContext();
        var stored = await context.Set<CompanyEmployee>().SingleAsync(e => e.Id == id);

        Assert.Equal(NationalId, stored.NationalId);
    }

    [Fact]
    public async Task Stores_ciphertext_not_the_plain_value()
    {
        await InsertAsync(NewEmployee(NationalId, "Ayse"));

        await using var context = _fixture.CreateContext();

        // Read the column with raw SQL so the value converter is out of the way; this is what
        // someone with database access would see.
        var raw = await context.Database
            .SqlQueryRaw<string>("SELECT [NationalId] AS Value FROM [ensa].[CompanyEmployee]")
            .ToListAsync();

        var value = Assert.Single(raw);
        Assert.NotEqual(NationalId, value);
        Assert.DoesNotContain(NationalId, value);
    }

    [Fact]
    public async Task Finds_a_record_by_the_encrypted_value()
    {
        await InsertAsync(NewEmployee(NationalId, "Ayse"));

        await using var context = _fixture.CreateContext();

        // Deterministic encryption is the whole point: the predicate is translated to a
        // comparison against the ciphertext, so the index is usable and no rows are loaded
        // into memory to be filtered there.
        var found = await context.Set<CompanyEmployee>()
            .FirstOrDefaultAsync(e => e.NationalId == NationalId);

        Assert.NotNull(found);
        Assert.Equal("Ayse", found!.Name);
    }

    [Fact]
    public async Task Rejects_a_second_employee_with_the_same_national_id()
    {
        await InsertAsync(NewEmployee(NationalId, "Ayse"));

        // The unique index is filtered (IsDeleted = 0 AND NationalId IS NOT NULL) and it only
        // works because the same input always encrypts to the same ciphertext.
        await Assert.ThrowsAnyAsync<DbUpdateException>(
            () => InsertAsync(NewEmployee(NationalId, "Fatma")));
    }

    [Fact]
    public async Task Leaves_a_missing_value_null_rather_than_encrypting_nothing()
    {
        var id = await InsertAsync(NewEmployee(nationalId: null!, name: "Mehmet"));

        await using var context = _fixture.CreateContext();

        var raw = await context.Database
            .SqlQueryRaw<string?>(
                "SELECT [NationalId] AS Value FROM [ensa].[CompanyEmployee] WHERE [Id] = {0}", id)
            .ToListAsync();

        Assert.Null(Assert.Single(raw));
    }
}
