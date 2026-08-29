using Ensa.Application;
using Ensa.EntityFrameworkCore;
using Ensa.HttpApi;
using Ensa.HttpApi.Host;
using Ensa.HttpApi.Host.Middleware;
using Serilog;

// Bootstrap logger, so that failures happening before the configuration is read still get logged.
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting the Ensa API...");

    var builder = WebApplication.CreateBuilder(args);

    // Local overrides, loaded last so they win. This repository is public: a real server's
    // credentials belong in a file .gitignore excludes, not in appsettings.*.json. The committed
    // files keep the shape and a working local default; this one holds what must not be published.
    builder.Configuration.AddJsonFile(
        $"appsettings.{builder.Environment.EnvironmentName}.local.json",
        optional: true,
        reloadOnChange: true);

    // Serilog — the real configuration is read from the Serilog section of appsettings.json.
    builder.Services.AddSerilog((services, loggerConfiguration) => loggerConfiguration
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    // --------------------------------------------------------------------
    //  Layer registrations — in the dependency order laid out in ARCHITECTURE.md §1
    // --------------------------------------------------------------------
    builder.Services.AddEnsaEntityFrameworkCore(builder.Configuration);
    builder.Services.AddEnsaApplication(builder.Configuration["AutoMapper:LicenseKey"]);
    builder.Services.AddEnsaHttpApi();
    builder.Services.AddEnsaHttpApiHost(builder.Configuration);

    var app = builder.Build();

    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint(
                $"/swagger/{EnsaHttpApiHostModule.SwaggerDocumentName}/swagger.json", "Ensa API v1");
            options.DocumentTitle = "Ensa API";
            options.DisplayRequestDuration();
        });
    }
    else
    {
        app.UseHsts();
        app.UseHttpsRedirection();
    }

    // Culture resolution. Order: ?culture=xx  ->  Accept-Language  ->  default (tr-TR).
    // Must run before anything that produces user-facing text.
    app.UseRequestLocalization(EnsaHttpApiHostModule.BuildLocalizationOptions());

    app.UseRouting();

    // CORS has to come BEFORE authentication so that preflight requests are not answered with 401.
    app.UseCors(EnsaHttpApiHostModule.CorsPolicyName);

    app.UseAuthentication();

    // Tenant resolution runs AFTER User is populated and BEFORE authorization.
    app.UseMiddleware<TenantResolutionMiddleware>();

    // Office resolution runs AFTER the tenant is known — which offices exist is a question that can
    // only be asked inside a tenant — and, like tenant resolution, before authorization, so a
    // request carrying an office its caller may not use is refused before any endpoint runs. It
    // costs nothing on a request that sends no X-Ensa-OfficeId header.
    app.UseMiddleware<OfficeResolutionMiddleware>();

    app.UseAuthorization();

    app.MapControllers();

    // Health probe for the container / load balancer.
    app.MapGet("/health", () => Results.Ok(new
    {
        status = "Healthy",
        service = "Ensa.HttpApi.Host",
        utcNow = DateTime.UtcNow
    }))
    .AllowAnonymous()
    .WithName("HealthCheck");

    app.Run();
}
catch (Exception exception) when (exception is not HostAbortedException)
{
    Log.Fatal(exception, "The Ensa API failed to start.");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
