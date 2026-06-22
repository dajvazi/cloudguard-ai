using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace CloudGuard.Api.Services.AI;

public class AiAnalysisService(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<AiAnalysisService> logger) : IAiAnalysisService
{
    public async Task<AiAnalysisResult> AnalyzeIncidentAsync(
        string serviceName,
        string serviceType,
        string anomalyType,
        string anomalyDescription,
        CancellationToken cancellationToken = default)
    {
        var apiKey = configuration["OpenAI:ApiKey"];

        if (string.IsNullOrEmpty(apiKey))
        {
            logger.LogWarning("OpenAI API key not configured, using rule-based analysis");
            return RuleBasedAnalysis(serviceName, serviceType, anomalyType, anomalyDescription);
        }

        try
        {
            return await CallOpenAiAsync(serviceName, serviceType, anomalyType, anomalyDescription, apiKey, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "OpenAI call failed, falling back to rule-based analysis");
            return RuleBasedAnalysis(serviceName, serviceType, anomalyType, anomalyDescription);
        }
    }

    private async Task<AiAnalysisResult> CallOpenAiAsync(
        string serviceName,
        string serviceType,
        string anomalyType,
        string anomalyDescription,
        string apiKey,
        CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("OpenAI");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var prompt = $"You are a cloud infrastructure AI analyst for an AIOps self-healing platform.\n" +
            $"Analyze this anomaly and provide a recovery recommendation.\n\n" +
            $"Service: {serviceName} (type: {serviceType})\n" +
            $"Anomaly: {anomalyType}\n" +
            $"Details: {anomalyDescription}\n\n" +
            "Respond in JSON only:\n" +
            """{"rootCause": "brief root cause", "recommendedAction": "specific action", "actionType": "one of: Restart Service, Scale Replicas, Clear Cache, Rotate Credentials, Rollback Deployment, Increase Resources", "severity": "one of: Critical, Warning, Info"}""";

        var body = new
        {
            model = configuration["OpenAI:Model"] ?? "gpt-4o-mini",
            messages = new[]
            {
                new { role = "system", content = "You are a cloud infrastructure analyst. Respond only in valid JSON." },
                new { role = "user", content = prompt },
            },
            temperature = 0.3,
            max_tokens = 300,
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions")
        {
            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"),
        };

        var response = await client.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);
        var content = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? "";

        var cleanJson = content.Trim();
        if (cleanJson.StartsWith("```")) cleanJson = cleanJson.Split('\n', 3)[1..^1].Aggregate((a, b) => a + "\n" + b);

        using var result = JsonDocument.Parse(cleanJson);
        var root = result.RootElement;

        return new AiAnalysisResult(
            RootCause: root.GetProperty("rootCause").GetString() ?? "Unknown",
            RecommendedAction: root.GetProperty("recommendedAction").GetString() ?? "Restart service",
            ActionType: root.GetProperty("actionType").GetString() ?? "Restart Service",
            Severity: root.GetProperty("severity").GetString() ?? "Warning"
        );
    }

    private static AiAnalysisResult RuleBasedAnalysis(
        string serviceName,
        string serviceType,
        string anomalyType,
        string anomalyDescription)
    {
        var (rootCause, action, actionType, severity) = anomalyType.ToLower() switch
        {
            var t when t.Contains("cpu") => (
                $"High CPU utilization on {serviceName} likely caused by processing spike or resource leak",
                $"Scale {serviceName} horizontally by adding replicas and investigate resource-heavy processes",
                "Scale Replicas",
                "Warning"),

            var t when t.Contains("memory") => (
                $"Memory pressure on {serviceName} indicating possible memory leak or cache overflow",
                $"Restart {serviceName} to clear memory and enable memory profiling for leak detection",
                "Restart Service",
                "Warning"),

            var t when t.Contains("latency") => (
                $"Latency spike on {serviceName} caused by downstream dependency slowdown or connection saturation",
                $"Restart {serviceName} pods with rolling strategy and verify upstream dependencies",
                "Restart Service",
                "Critical"),

            var t when t.Contains("error") => (
                $"Error rate surge on {serviceName} indicating service degradation or failed deployment",
                $"Rollback last deployment on {serviceName} and scale up healthy replicas",
                "Rollback Deployment",
                "Critical"),

            _ => (
                $"Anomaly detected on {serviceName}: {anomalyDescription}",
                $"Restart {serviceName} and monitor for recovery",
                "Restart Service",
                "Warning"),
        };

        return new AiAnalysisResult(rootCause, action, actionType, severity);
    }
}
