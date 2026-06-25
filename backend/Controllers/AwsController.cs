using CloudGuard.Api.Services.AWS;
using CloudGuard.Api.Services.AWS.Runbooks;
using Microsoft.AspNetCore.Mvc;

namespace CloudGuard.Api.Controllers;

[ApiController]
[Route("api/aws")]
public class AwsController(
    IAwsCloudWatchService awsService,
    IAwsSsmService ssmService,
    IRunbookService runbookService,
    IConfiguration configuration) : ControllerBase
{
    [HttpGet("test-connection")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<ActionResult> TestConnection(CancellationToken ct)
    {
        var connected = await awsService.TestConnectionAsync(ct);
        return Ok(new { connected, message = connected ? "AWS connection successful" : "AWS connection failed" });
    }

    [HttpPost("import")]
    [ProducesResponseType(typeof(AwsImportResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AwsImportResult>> Import(
        [FromBody] AwsImportRequest request,
        CancellationToken ct)
    {
        var result = await awsService.ImportCloudWatchDataAsync(request, ct);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpGet("runbooks")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public ActionResult GetRunbooks()
    {
        var runbooks = runbookService.GetAll().Select(r => new
        {
            r.Id,
            r.Name,
            r.Description,
            commandCount = r.Commands.Count,
        });
        return Ok(runbooks);
    }

    [HttpGet("ssm/status")]
    [ProducesResponseType(typeof(SsmInstanceStatus), StatusCodes.Status200OK)]
    public async Task<ActionResult> GetSsmStatus(CancellationToken ct)
    {
        var instanceId = configuration["AWS:Ec2InstanceId"]
            ?? Environment.GetEnvironmentVariable("AWS_EC2_INSTANCE_ID");

        if (string.IsNullOrWhiteSpace(instanceId))
            return BadRequest(new { message = "AWS_EC2_INSTANCE_ID not configured" });

        var status = await ssmService.GetInstanceStatusAsync(instanceId, ct);
        return Ok(status);
    }

    [HttpPost("ssm/test/{runbookId}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> TestRunbook(string runbookId, CancellationToken ct)
    {
        if (!ssmService.IsEnabled)
            return BadRequest(new { message = "SSM is disabled or AWS_EC2_INSTANCE_ID is not set" });

        var runbook = runbookService.GetById(runbookId);
        if (runbook is null)
            return BadRequest(new { message = $"Unknown runbook: {runbookId}" });

        var instanceId = configuration["AWS:Ec2InstanceId"]
            ?? Environment.GetEnvironmentVariable("AWS_EC2_INSTANCE_ID");

        if (string.IsNullOrWhiteSpace(instanceId))
            return BadRequest(new { message = "AWS_EC2_INSTANCE_ID not configured" });

        var result = await ssmService.ExecuteRunbookAsync(instanceId, runbook.Commands, ct);

        return Ok(new
        {
            result.Success,
            runbookId = runbook.Id,
            instanceId,
            result.CommandId,
            result.Status,
            output = result.Output,
            error = result.Error,
        });
    }

    [HttpPost("reevaluate")]
    [ProducesResponseType(typeof(AwsEvaluationResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<AwsEvaluationResult>> Reevaluate(
        [FromServices] IAwsImportEvaluator evaluator,
        CancellationToken ct)
    {
        var result = await evaluator.EvaluateExistingAsync(ct);
        return Ok(result);
    }
}
