using Microsoft.EntityFrameworkCore;
using SensorApp.Data;
using SensorApp.Dtos;
using SensorApp.Models;

namespace SensorApp.Services;

public interface IReadingService
{
    Task<IReadOnlyList<LegacyDataDto>> GetReadingsAsync(string? type, string? deviceId, string? from, string? to, CancellationToken cancellationToken = default);
    Task<bool> SaveReadingAsync(LegacyDataDto dto, CancellationToken cancellationToken = default);
    Task<CalcResultDto?> CalculateAsync(int deviceId, CancellationToken cancellationToken = default);
    Task<StatsResultDto?> GetStatsAsync(int deviceId, CancellationToken cancellationToken = default);
}

public class ReadingService : IReadingService
{
    private const int ReadingType = 1;
    private const int AlertType = 3;

    private readonly SensorDbContext _context;
    private readonly ILogger<ReadingService> _logger;

    public ReadingService(SensorDbContext context, ILogger<ReadingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IReadOnlyList<LegacyDataDto>> GetReadingsAsync(
        string? type,
        string? deviceId,
        string? from,
        string? to,
        CancellationToken cancellationToken = default)
    {
        var includeReadings = string.IsNullOrEmpty(type) || type == "0" || type == ReadingType.ToString();
        var includeAlerts = string.IsNullOrEmpty(type) || type == "0" || type == AlertType.ToString();
        var parsedDeviceId = ParseOptionalInt(deviceId);
        var fromDate = ParseOptionalDate(from);
        var toDate = ParseOptionalDate(to);

        var results = new List<LegacyDataDto>();

        if (includeReadings)
        {
            var readingQuery = _context.Readings.AsNoTracking().AsQueryable();

            if (parsedDeviceId.HasValue)
            {
                readingQuery = readingQuery.Where(r => r.DeviceId == parsedDeviceId.Value);
            }

            if (fromDate.HasValue)
            {
                readingQuery = readingQuery.Where(r => r.Timestamp >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                readingQuery = readingQuery.Where(r => r.Timestamp <= toDate.Value);
            }

            var readings = await readingQuery
                .OrderByDescending(r => r.Timestamp)
                .Take(1000)
                .ToListAsync(cancellationToken);

            results.AddRange(readings.Select(LegacyDtoMapper.FromReading));
        }

        if (includeAlerts)
        {
            var alertQuery = _context.Alerts.AsNoTracking().AsQueryable();

            if (parsedDeviceId.HasValue)
            {
                alertQuery = alertQuery.Where(a => a.DeviceId == parsedDeviceId.Value);
            }

            if (fromDate.HasValue)
            {
                alertQuery = alertQuery.Where(a => a.Timestamp >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                alertQuery = alertQuery.Where(a => a.Timestamp <= toDate.Value);
            }

            var alerts = await alertQuery
                .OrderByDescending(a => a.Timestamp)
                .Take(1000)
                .ToListAsync(cancellationToken);

            results.AddRange(alerts.Select(LegacyDtoMapper.FromAlert));
        }

        return results
            .OrderByDescending(r => r.Ts)
            .Take(1000)
            .ToList();
    }

    public async Task<bool> SaveReadingAsync(LegacyDataDto dto, CancellationToken cancellationToken = default)
    {
        if (dto.Did <= 0)
        {
            _logger.LogWarning("Rejected reading with invalid device id {DeviceId}", dto.Did);
            return false;
        }

        var device = await _context.Devices.FirstOrDefaultAsync(d => d.Id == dto.Did, cancellationToken);
        if (device is null)
        {
            _logger.LogWarning("Rejected reading for unknown device {DeviceId}", dto.Did);
            return false;
        }

        var timestamp = ParseOptionalDate(dto.Ts) ?? DateTime.UtcNow;
        var reading = new SensorReading
        {
            DeviceId = dto.Did,
            Timestamp = timestamp,
            Temperature = dto.V,
            Humidity = dto.V2,
            Pressure = dto.V3,
            Status = Enum.IsDefined(typeof(DeviceStatus), dto.St)
                ? (DeviceStatus)dto.St
                : DeviceStatus.Active
        };

        device.UpdatedAt = DateTime.UtcNow;
        _context.Readings.Add(reading);
        _context.AuditLogs.Add(new AuditLog
        {
            DeviceId = dto.Did,
            Message = "data saved",
            Timestamp = DateTime.UtcNow,
            IsAlert = false
        });

        if (dto.V > device.Threshold)
        {
            _context.Alerts.Add(new Alert
            {
                DeviceId = dto.Did,
                Timestamp = DateTime.UtcNow,
                Value = dto.V,
                Threshold = device.Threshold,
                Message = "AUTO ALERT",
                IsAutoGenerated = true
            });
            _context.AuditLogs.Add(new AuditLog
            {
                DeviceId = dto.Did,
                Message = $"alert val={dto.V}",
                Timestamp = DateTime.UtcNow,
                IsAlert = true
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<CalcResultDto?> CalculateAsync(int deviceId, CancellationToken cancellationToken = default)
    {
        var device = await _context.Devices.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == deviceId, cancellationToken);

        if (device is null)
        {
            return null;
        }

        var cutoff = DateTime.UtcNow.AddHours(-1);
        var readings = await _context.Readings.AsNoTracking()
            .Where(r => r.DeviceId == deviceId && r.Timestamp >= cutoff)
            .ToListAsync(cancellationToken);

        if (readings.Count == 0)
        {
            return new CalcResultDto
            {
                Avg = 0,
                Mx = 0,
                Thr = device.Threshold
            };
        }

        var average = Math.Round(readings.Average(r => r.Temperature), 2);
        var maximum = Math.Round(readings.Max(r => r.Temperature), 2);

        if (maximum > device.Threshold)
        {
            _context.Alerts.Add(new Alert
            {
                DeviceId = deviceId,
                Timestamp = DateTime.UtcNow,
                Value = maximum,
                Threshold = device.Threshold,
                AverageValue = average,
                Message = "ALERT: threshold exceeded",
                IsAutoGenerated = false
            });
        }

        _context.AuditLogs.Add(new AuditLog
        {
            DeviceId = deviceId,
            Message = $"calc did={deviceId}",
            Timestamp = DateTime.UtcNow,
            IsAlert = false
        });

        await _context.SaveChangesAsync(cancellationToken);

        return new CalcResultDto
        {
            Avg = average,
            Mx = maximum,
            Thr = device.Threshold
        };
    }

    public async Task<StatsResultDto?> GetStatsAsync(int deviceId, CancellationToken cancellationToken = default)
    {
        var deviceExists = await _context.Devices.AnyAsync(d => d.Id == deviceId, cancellationToken);
        if (!deviceExists)
        {
            return null;
        }

        var readings = await _context.Readings.AsNoTracking()
            .Where(r => r.DeviceId == deviceId)
            .ToListAsync(cancellationToken);

        var alerts = await _context.Alerts.AsNoTracking()
            .Where(a => a.DeviceId == deviceId)
            .ToListAsync(cancellationToken);

        if (readings.Count == 0 && alerts.Count == 0)
        {
            return new StatsResultDto();
        }

        return new StatsResultDto
        {
            Total = readings.Count + alerts.Count,
            Avg = readings.Count == 0 ? 0 : Math.Round(readings.Average(r => r.Temperature), 2),
            Max = readings.Count == 0 ? 0 : readings.Max(r => r.Temperature),
            Min = readings.Count == 0 ? 0 : readings.Min(r => r.Temperature),
            Alerts = alerts.Count,
            Readings = readings.Count,
            Last = LegacyDtoMapper.FormatTimestamp(
                new[] { readings.MaxBy(r => r.Timestamp)?.Timestamp, alerts.MaxBy(a => a.Timestamp)?.Timestamp }
                    .Where(d => d.HasValue)
                    .Select(d => d!.Value)
                    .DefaultIfEmpty(DateTime.MinValue)
                    .Max())
        };
    }

    private static int? ParseOptionalInt(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value == "0")
        {
            return null;
        }

        return int.TryParse(value, out var parsed) ? parsed : null;
    }

    private static DateTime? ParseOptionalDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return DateTime.TryParse(value, out var parsed) ? parsed : null;
    }
}
