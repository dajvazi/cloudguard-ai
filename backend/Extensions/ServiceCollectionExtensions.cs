using Amazon.CloudWatch;
using CloudGuard.Api.Repositories;
using CloudGuard.Api.Repositories.Interfaces;
using CloudGuard.Api.Services.AI;
using CloudGuard.Api.Services.Anomalies;
using CloudGuard.Api.Services.AWS;
using CloudGuard.Api.Services.CloudServices;
using CloudGuard.Api.Services.Incidents;
using CloudGuard.Api.Services.Metrics;
using CloudGuard.Api.Services.RecoveryActions;
using CloudGuard.Api.Services.Resources;
using CloudGuard.Api.Services.Terraform;

namespace CloudGuard.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<ICloudServiceRepository, CloudServiceRepository>();
        services.AddScoped<IMetricRepository, MetricRepository>();
        services.AddScoped<IAnomalyRepository, AnomalyRepository>();
        services.AddScoped<IIncidentRepository, IncidentRepository>();
        services.AddScoped<IRecoveryActionRepository, RecoveryActionRepository>();
        services.AddScoped<ITerraformUploadRepository, TerraformUploadRepository>();
        services.AddScoped<IResourceRepository, ResourceRepository>();

        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddRepositories();

        services.AddScoped<ITerraformParserService, TerraformParserService>();
        services.AddScoped<ITerraformProjectParser, TerraformProjectParser>();
        services.AddScoped<ITerraformArchiveExtractor, TerraformArchiveExtractor>();
        services.AddScoped<ITerraformUploadService, TerraformUploadService>();
        services.AddScoped<ICloudServiceService, CloudServiceService>();
        services.AddScoped<IMetricService, MetricService>();
        services.AddScoped<IAnomalyService, AnomalyService>();
        services.AddScoped<IIncidentService, IncidentService>();
        services.AddScoped<IRecoveryActionService, RecoveryActionService>();
        services.AddScoped<IResourceService, ResourceService>();

        // AI & Self-Healing
        services.AddHttpClient("OpenAI");
        services.AddScoped<IAiAnalysisService, AiAnalysisService>();
        services.AddScoped<ISelfHealingOrchestrator, SelfHealingOrchestrator>();
        services.AddHostedService<AnomalyDetectionEngine>();

        // AWS
        services.AddDefaultAWSOptions(new Amazon.Extensions.NETCore.Setup.AWSOptions
        {
            Region = Amazon.RegionEndpoint.GetBySystemName(
                Environment.GetEnvironmentVariable("AWS_DEFAULT_REGION") ?? "us-east-1"),
        });
        services.AddAWSService<IAmazonCloudWatch>();
        services.AddScoped<IAwsCloudWatchService, AwsCloudWatchService>();

        return services;
    }
}
