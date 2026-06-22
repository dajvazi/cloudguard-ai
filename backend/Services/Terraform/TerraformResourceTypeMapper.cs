namespace CloudGuard.Api.Services.Terraform;

public static class TerraformResourceTypeMapper
{
    private static readonly Dictionary<string, string> KnownTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["aws_instance"] = "EC2 Instance",
        ["aws_db_instance"] = "RDS Database",
        ["aws_lambda_function"] = "Lambda Function",
        ["aws_ecs_service"] = "ECS Service",
        ["aws_ecs_cluster"] = "ECS Cluster",
        ["aws_api_gateway_rest_api"] = "API Gateway",
        ["aws_s3_bucket"] = "S3 Bucket",
        ["aws_sqs_queue"] = "SQS Queue",
        ["aws_sns_topic"] = "SNS Topic",
        ["aws_elasticache_cluster"] = "ElastiCache",
        ["aws_lb"] = "Load Balancer",
        ["azurerm_linux_virtual_machine"] = "Azure VM",
        ["azurerm_windows_virtual_machine"] = "Azure VM",
        ["azurerm_sql_database"] = "Azure SQL Database",
        ["azurerm_kubernetes_cluster"] = "AKS Cluster",
        ["google_compute_instance"] = "GCE Instance",
        ["google_sql_database_instance"] = "Cloud SQL",
        ["google_container_cluster"] = "GKE Cluster",
    };

    public static string ToDisplayType(string resourceType)
    {
        if (KnownTypes.TryGetValue(resourceType, out var displayType))
            return displayType;

        return resourceType.Replace("_", " ", StringComparison.Ordinal);
    }
}
