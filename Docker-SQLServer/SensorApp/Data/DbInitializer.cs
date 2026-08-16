using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SensorApp.Models;

namespace SensorApp.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(SensorDbContext context, ILogger logger, CancellationToken cancellationToken = default)
    {
        const int maxAttempts = 10;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await EnsureDatabaseExistsAsync(context, cancellationToken);
                await context.Database.MigrateAsync(cancellationToken);

                if (await context.Devices.AnyAsync(cancellationToken))
                {
                    return;
                }

                var devices = new[]
                {
                    new Device
                    {
                        Name = "snsr-01",
                        Location = "Building A|Room 1",
                        SensorType = SensorType.Temperature,
                        Status = DeviceStatus.Active,
                        Threshold = 75,
                        Unit = "C",
                        ReportingIntervalSeconds = 30,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new Device
                    {
                        Name = "snsr-02",
                        Location = "Building A|Room 2",
                        SensorType = SensorType.Temperature,
                        Status = DeviceStatus.Active,
                        Threshold = 80,
                        Unit = "C",
                        ReportingIntervalSeconds = 30,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new Device
                    {
                        Name = "snsr-03",
                        Location = "Building B|Floor 1",
                        SensorType = SensorType.Humidity,
                        Status = DeviceStatus.Active,
                        Threshold = 70,
                        Unit = "F",
                        ReportingIntervalSeconds = 60,
                        UpdatedAt = DateTime.UtcNow
                    }
                };

                context.Devices.AddRange(devices);
                await context.SaveChangesAsync(cancellationToken);

                var readings = new List<SensorReading>();
                for (var i = 0; i < 100; i++)
                {
                    var timestamp = DateTime.UtcNow.AddMinutes(-i * 5);
                    readings.Add(new SensorReading
                    {
                        DeviceId = 1,
                        Timestamp = timestamp,
                        Temperature = 65 + (i % 15),
                        Humidity = 55 + (i % 20),
                        Pressure = 1013 + (i % 5),
                        Status = DeviceStatus.Active
                    });
                    readings.Add(new SensorReading
                    {
                        DeviceId = 2,
                        Timestamp = timestamp,
                        Temperature = 70 + (i % 10),
                        Humidity = 60 + (i % 15),
                        Pressure = 1010 + (i % 8),
                        Status = DeviceStatus.Active
                    });
                }

                context.Readings.AddRange(readings);
                await context.SaveChangesAsync(cancellationToken);
                logger.LogInformation("Database initialized with seed data.");
                return;
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                logger.LogWarning(ex, "Database initialization attempt {Attempt} failed. Retrying...", attempt);
                await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
            }
        }

        throw new InvalidOperationException("Unable to initialize database after multiple attempts.");
    }

    private static async Task EnsureDatabaseExistsAsync(SensorDbContext context, CancellationToken cancellationToken)
    {
        var connectionString = context.Database.GetConnectionString()
            ?? throw new InvalidOperationException("Database connection string is not configured.");

        var builder = new SqlConnectionStringBuilder(connectionString);
        var databaseName = builder.InitialCatalog;
        builder.InitialCatalog = "master";

        await using var connection = new SqlConnection(builder.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'{databaseName}')
            CREATE DATABASE [{databaseName}]
            """;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
