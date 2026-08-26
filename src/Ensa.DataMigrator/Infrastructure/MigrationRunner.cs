using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Ensa.DataMigrator.Infrastructure;

/// <summary>
/// Runs the steps in order and prints a reconciliation.
/// <para>
/// The summary is the point. A migration that reports "done" tells you nothing; one that reports
/// how many rows it read, wrote and deliberately left behind can be checked against the source.
/// Every skipped row is a decision, so every step that skips has to say why.
/// </para>
/// </summary>
public sealed class MigrationRunner(
    IEnumerable<IMigrationStep> steps,
    MigrationContext context,
    ILogger<MigrationRunner> logger)
{
    public async Task<int> RunAsync(
        IReadOnlyCollection<string> onlySteps,
        CancellationToken cancellationToken = default)
    {
        var ordered = steps.OrderBy(step => step.Order).ToList();

        if (onlySteps.Count > 0)
        {
            var unknown = onlySteps
                .Where(name => !ordered.Exists(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            if (unknown.Count > 0)
            {
                logger.LogError(
                    "Unknown step(s): {Unknown}. Available: {Available}",
                    string.Join(", ", unknown),
                    string.Join(", ", ordered.Select(s => s.Name)));
                return 1;
            }

            ordered = ordered
                .Where(step => onlySteps.Any(name => step.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        logger.LogInformation(
            "{Mode}: {Legacy} -> {Modern} on {Server}. {Count} step(s).",
            context.DryRun ? "DRY RUN (nothing is written)" : "Migrating",
            context.Target.LegacyDatabase, context.Target.ModernDatabase, context.Target.Server,
            ordered.Count);

        var totals = new List<(string Name, StepResult Result, TimeSpan Elapsed)>();

        foreach (var step in ordered)
        {
            var startedTime = DateTime.UtcNow;
            var stopwatch = Stopwatch.StartNew();

            logger.LogInformation("--- {Order:D2} {Name}: {Description}", step.Order, step.Name, step.Description);

            StepResult result;
            try
            {
                result = await step.RunAsync(context, cancellationToken);
            }
            catch (Exception exception)
            {
                stopwatch.Stop();
                logger.LogError(exception, "Step {Name} failed after {Elapsed}.", step.Name, stopwatch.Elapsed);
                PrintSummary(totals);
                return 1;
            }

            stopwatch.Stop();
            totals.Add((step.Name, result, stopwatch.Elapsed));

            logger.LogInformation(
                "    read {Read}, written {Written}, skipped {Skipped} in {Elapsed:mm\\:ss}. {Note}",
                result.Read, result.Written, result.Skipped, stopwatch.Elapsed, result.Note ?? "");

            if (!context.DryRun)
            {
                await context.IdMap.LogStepAsync(
                    step.Name, startedTime, result.Read, result.Written, result.Skipped,
                    result.Note, cancellationToken);
            }
        }

        PrintSummary(totals);
        return 0;
    }

    private void PrintSummary(List<(string Name, StepResult Result, TimeSpan Elapsed)> totals)
    {
        if (totals.Count == 0)
        {
            return;
        }

        logger.LogInformation("=== SUMMARY ===");

        foreach (var (name, result, elapsed) in totals)
        {
            logger.LogInformation(
                "  {Name,-28} read {Read,9}  written {Written,9}  skipped {Skipped,9}  {Elapsed:mm\\:ss}",
                name, result.Read, result.Written, result.Skipped, elapsed);
        }

        logger.LogInformation(
            "  {Label,-28} read {Read,9}  written {Written,9}  skipped {Skipped,9}",
            "TOTAL",
            totals.Sum(t => t.Result.Read),
            totals.Sum(t => t.Result.Written),
            totals.Sum(t => t.Result.Skipped));
    }
}
