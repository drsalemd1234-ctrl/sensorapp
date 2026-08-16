using Microsoft.EntityFrameworkCore;
using SensorApp.Data;
using SensorApp.Dtos;

namespace SensorApp.Services;

public interface IAuditService
{
    Task<IReadOnlyList<LegacyDataDto>> GetLogsAsync(string? deviceId, string? flag, CancellationToken cancellationToken = default);
}

public class AuditService : IAuditService
{
    private readonly SensorDbContext _context;

    public AuditService(SensorDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<LegacyDataDto>> GetLogsAsync(
        string? deviceId,
        string? flag,
        CancellationToken cancellationToken = default)
    {
        var query = _context.AuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(deviceId) && deviceId != "0" &&
            int.TryParse(deviceId, out var parsedDeviceId))
        {
            query = query.Where(l => l.DeviceId == parsedDeviceId);
        }

        if (!string.IsNullOrWhiteSpace(flag) && flag != "-1" &&
            int.TryParse(flag, out var parsedFlag))
        {
            var isAlert = parsedFlag == 1;
            query = query.Where(l => l.IsAlert == isAlert);
        }

        var logs = await query
            .OrderByDescending(l => l.Timestamp)
            .ToListAsync(cancellationToken);

        return logs.Select(LegacyDtoMapper.FromAuditLog).ToList();
    }
}
