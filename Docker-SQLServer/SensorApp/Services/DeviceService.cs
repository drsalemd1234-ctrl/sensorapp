using Microsoft.EntityFrameworkCore;
using SensorApp.Data;
using SensorApp.Dtos;
using SensorApp.Models;

namespace SensorApp.Services;

public interface IDeviceService
{
    Task<IReadOnlyList<LegacyDataDto>> GetDevicesAsync(string? status, CancellationToken cancellationToken = default);
    Task<bool> SaveDeviceAsync(LegacyDataDto dto, CancellationToken cancellationToken = default);
}

public class DeviceService : IDeviceService
{
    private readonly SensorDbContext _context;
    private readonly ILogger<DeviceService> _logger;

    public DeviceService(SensorDbContext context, ILogger<DeviceService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IReadOnlyList<LegacyDataDto>> GetDevicesAsync(string? status, CancellationToken cancellationToken = default)
    {
        var query = _context.Devices.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && status != "0" &&
            Enum.TryParse<DeviceStatus>(status, out var parsedStatus))
        {
            query = query.Where(d => d.Status == parsedStatus);
        }

        var devices = await query.OrderBy(d => d.Id).ToListAsync(cancellationToken);
        return devices.Select(LegacyDtoMapper.FromDevice).ToList();
    }

    public async Task<bool> SaveDeviceAsync(LegacyDataDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Nm))
        {
            _logger.LogWarning("Rejected device save with empty name");
            return false;
        }

        if (dto.Id > 0)
        {
            var device = await _context.Devices.FirstOrDefaultAsync(d => d.Id == dto.Id, cancellationToken);
            if (device is null)
            {
                return false;
            }

            device.Name = dto.Nm;
            device.Location = dto.Loc ?? string.Empty;
            device.SensorType = Enum.IsDefined(typeof(SensorType), dto.Tp)
                ? (SensorType)dto.Tp
                : device.SensorType;
            device.Status = Enum.IsDefined(typeof(DeviceStatus), dto.St)
                ? (DeviceStatus)dto.St
                : device.Status;
            DeviceConfigFormatter.ApplyLegacyConfig(device, dto.Cfg);
            device.UpdatedAt = DateTime.UtcNow;

            _context.AuditLogs.Add(new AuditLog
            {
                DeviceId = device.Id,
                Message = "dev updated",
                Timestamp = DateTime.UtcNow,
                IsAlert = false
            });
        }
        else
        {
            var device = new Device
            {
                Name = dto.Nm,
                Location = dto.Loc ?? string.Empty,
                SensorType = Enum.IsDefined(typeof(SensorType), dto.Tp)
                    ? (SensorType)dto.Tp
                    : SensorType.Temperature,
                Status = Enum.IsDefined(typeof(DeviceStatus), dto.St)
                    ? (DeviceStatus)dto.St
                    : DeviceStatus.Active,
                UpdatedAt = DateTime.UtcNow
            };

            DeviceConfigFormatter.ApplyLegacyConfig(device, dto.Cfg);
            if (string.IsNullOrWhiteSpace(dto.Cfg))
            {
                device.Threshold = dto.V > 0 ? dto.V : device.Threshold;
                device.ReportingIntervalSeconds = dto.V2 > 0 ? (int)dto.V2 : device.ReportingIntervalSeconds;
            }

            _context.Devices.Add(device);
            await _context.SaveChangesAsync(cancellationToken);

            _context.AuditLogs.Add(new AuditLog
            {
                DeviceId = null,
                Message = $"dev added {device.Name}",
                Timestamp = DateTime.UtcNow,
                IsAlert = false
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
