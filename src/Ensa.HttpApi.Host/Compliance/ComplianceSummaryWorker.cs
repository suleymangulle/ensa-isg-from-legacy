using Ensa.Domain.Common;
using Ensa.Domain.Companies;
using Microsoft.Extensions.Options;

namespace Ensa.HttpApi.Host.Compliance;

/// <summary>Settings of the compliance summary job. Bound from the <c>ComplianceSummary</c> section.</summary>
public class ComplianceSummaryOptions
{
    /// <summary>Configuration section the settings are bound from.</summary>
    public const string SectionName = "ComplianceSummary";

    /// <summary>Minutes between two recalculation rounds. Zero or less switches the job off.</summary>
    public int IntervalMinutes { get; set; } = 30;

    /// <summary>How long to wait after start-up before the first round.</summary>
    public int StartupDelaySeconds { get; set; } = 20;
}

/// <summary>
/// Keeps <see cref="CompanyComplianceSummary"/> current for every company.
/// <para>
/// <b>Why this exists.</b> The summary is what the company detail screen's compliance panel shows
/// and what the legacy customer portal put on its landing page: how many employees have had no
/// safety training, how many are missing a pre-employment examination, how much equipment is
/// overdue for inspection. <c>CompanyComplianceSummary</c> documented itself as
/// <i>"recomputed periodically by a background job"</i> — and no such job existed. Nothing ever
/// wrote the table, so <c>CompanyNavigationDto.WarningSummary</c> was always null and the panel
/// was permanently empty. The figures were not wrong; they were absent.
/// </para>
/// <para>
/// <b>Why a job rather than a live query.</b> The panel needs six aggregates over the training,
/// health and equipment modules. Computing them on every read would put that on the critical path
/// of a screen that opens constantly, and the numbers do not move by the second — an employee's
/// training status changes on the day they finish it. The table exists so the read is one row.
/// A company the job has not reached yet is computed once, on its first read, by
/// <see cref="ICompanyComplianceCalculator"/>.
/// </para>
/// <para>
/// <b>Where the rules live.</b> Not here. What "missing training" and "overdue inspection" mean is
/// a statutory question, so it lives in the domain service; this class only decides when to ask.
/// That is also what stops the job and the first-read path from computing different numbers.
/// </para>
/// <para>
/// <b>Tenancy.</b> A worker has no user and no single tenant, so it runs with the tenant filter
/// disabled and covers every organization in one pass, exactly like <c>MailDeliveryWorker</c>
/// (ADR-027). Each summary row is stamped with the tenant of the company it belongs to, so
/// nothing crosses over.
/// </para>
/// </summary>
public class ComplianceSummaryWorker(
    IServiceProvider serviceProvider,
    IOptions<ComplianceSummaryOptions> options,
    ILogger<ComplianceSummaryWorker> logger) : BackgroundService
{
    private readonly ComplianceSummaryOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_options.IntervalMinutes <= 0)
        {
            logger.LogInformation("The compliance summary job is switched off (IntervalMinutes <= 0).");
            return;
        }

        try
        {
            await Task.Delay(
                TimeSpan.FromSeconds(Math.Max(0, _options.StartupDelaySeconds)), stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(_options.IntervalMinutes));

        do
        {
            try
            {
                await RecalculateAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                // A failed round must not take the worker down; the next one tries again.
                logger.LogError(exception, "The compliance summary round failed.");
            }
        }
        while (await SafeWaitAsync(timer, stoppingToken));
    }

    private static async Task<bool> SafeWaitAsync(PeriodicTimer timer, CancellationToken stoppingToken)
    {
        try
        {
            return await timer.WaitForNextTickAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    private async Task RecalculateAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();

        var dataFilter = scope.ServiceProvider.GetRequiredService<IDataFilter>();
        var calculator = scope.ServiceProvider.GetRequiredService<ICompanyComplianceCalculator>();

        // A worker serves every organization at once; each row it writes carries the tenant of the
        // company it belongs to.
        using var _ = dataFilter.Disable<IMultiTenant>();

        var written = await calculator.RecalculateAllAsync(cancellationToken);

        if (written > 0)
        {
            logger.LogInformation("Compliance summaries recomputed: {Written} row(s) written.", written);
        }
    }
}
