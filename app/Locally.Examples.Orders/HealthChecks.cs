using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Locally.Examples.Orders;

/// <summary>
/// A health check per dependency in each region, each served on its own URL:
///
///   /_internal/healthcheck/storage-primary
///   /_internal/healthcheck/storage-secondary
///   /_internal/healthcheck/servicebus-primary
///   /_internal/healthcheck/servicebus-secondary
///
/// These are for people and dashboards to look at. App Service's own health check
/// uses /healthz instead, so a dependency having a bad day doesn't get the API's
/// instance taken out of rotation.
/// </summary>
public static class HealthChecks
{
    private static readonly string[] Regions = ["primary", "secondary"];

    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    public static IServiceCollection AddOrderHealthChecks(this IServiceCollection services, string queue)
    {
        var checks = services.AddHealthChecks();
        foreach (var region in Regions)
        {
            checks.Add(new HealthCheckRegistration(
                $"storage-{region}",
                sp => new StorageHealthCheck(sp.GetRequiredKeyedService<BlobContainerClient>(region)),
                HealthStatus.Unhealthy,
                tags: null,
                Timeout));

            checks.Add(new HealthCheckRegistration(
                $"servicebus-{region}",
                sp => new ServiceBusHealthCheck(sp.GetRequiredKeyedService<ServiceBusClient>(region), queue),
                HealthStatus.Unhealthy,
                tags: null,
                Timeout));
        }

        return services;
    }

    public static void MapOrderHealthChecks(this IEndpointRouteBuilder app)
    {
        foreach (var region in Regions)
        {
            foreach (var name in new[] { $"storage-{region}", $"servicebus-{region}" })
            {
                app.MapHealthChecks($"/_internal/healthcheck/{name}", new HealthCheckOptions
                {
                    Predicate = check => check.Name == name,
                });
            }
        }
    }

    private class StorageHealthCheck(BlobContainerClient container) : IHealthCheck
    {
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken)
        {
            try
            {
                await container.GetPropertiesAsync(cancellationToken: cancellationToken);
                return HealthCheckResult.Healthy();
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy(exception: ex);
            }
        }
    }

    private class ServiceBusHealthCheck(ServiceBusClient client, string queue) : IHealthCheck
    {
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken)
        {
            try
            {
                // Peeking reads without locking or removing anything, so it doesn't
                // disturb whatever is consuming the queue.
                await using var receiver = client.CreateReceiver(queue);
                await receiver.PeekMessageAsync(cancellationToken: cancellationToken);
                return HealthCheckResult.Healthy();
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy(exception: ex);
            }
        }
    }
}
