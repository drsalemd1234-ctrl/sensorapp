using Microsoft.AspNetCore.Mvc;
using SensorApp.Dtos;
using SensorApp.Services;

namespace SensorApp.Controllers;

[ApiController]
[Route("api")]
public class SensorController : ControllerBase
{
    private readonly IReadingService _readingService;
    private readonly IDeviceService _deviceService;
    private readonly IAuditService _auditService;

    public SensorController(
        IReadingService readingService,
        IDeviceService deviceService,
        IAuditService auditService)
    {
        _readingService = readingService;
        _deviceService = deviceService;
        _auditService = auditService;
    }

    [HttpGet("data")]
    public async Task<ActionResult<IReadOnlyList<LegacyDataDto>>> GetData(
        [FromQuery] string tp = "0",
        [FromQuery] string did = "0",
        [FromQuery] string df = "",
        [FromQuery] string dt = "",
        CancellationToken cancellationToken = default)
    {
        var results = await _readingService.GetReadingsAsync(tp, did, df, dt, cancellationToken);
        return Ok(results);
    }

    [HttpPost("data")]
    public async Task<ActionResult<SaveResultDto>> PostData(
        [FromBody] LegacyDataDto dto,
        CancellationToken cancellationToken = default)
    {
        var ok = await _readingService.SaveReadingAsync(dto, cancellationToken);
        return Ok(new SaveResultDto { Ok = ok });
    }

    [HttpGet("dev")]
    public async Task<ActionResult<IReadOnlyList<LegacyDataDto>>> GetDevices(
        [FromQuery] string st = "0",
        CancellationToken cancellationToken = default)
    {
        var devices = await _deviceService.GetDevicesAsync(st, cancellationToken);
        return Ok(devices);
    }

    [HttpPost("dev")]
    public async Task<ActionResult<SaveResultDto>> PostDevice(
        [FromBody] LegacyDataDto dto,
        CancellationToken cancellationToken = default)
    {
        var ok = await _deviceService.SaveDeviceAsync(dto, cancellationToken);
        return Ok(new SaveResultDto { Ok = ok });
    }

    [HttpGet("calc")]
    public async Task<ActionResult<CalcResultDto>> Calculate(
        [FromQuery] int did = 1,
        CancellationToken cancellationToken = default)
    {
        var result = await _readingService.CalculateAsync(did, cancellationToken);
        if (result is null)
        {
            return NotFound();
        }

        return Ok(result);
    }

    [HttpGet("log")]
    public async Task<ActionResult<IReadOnlyList<LegacyDataDto>>> GetLogs(
        [FromQuery] string did = "0",
        [FromQuery] string flg = "-1",
        CancellationToken cancellationToken = default)
    {
        var logs = await _auditService.GetLogsAsync(did, flg, cancellationToken);
        return Ok(logs);
    }

    [HttpGet("stats")]
    public async Task<ActionResult<StatsResultDto>> GetStats(
        [FromQuery] int did = 1,
        CancellationToken cancellationToken = default)
    {
        var stats = await _readingService.GetStatsAsync(did, cancellationToken);
        if (stats is null)
        {
            return NotFound();
        }

        return Ok(stats);
    }
}
