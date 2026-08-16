using SensorApp.Models;

namespace SensorApp.Services;

public static class DeviceConfigFormatter
{
    public static string ToLegacyConfig(Device device) =>
        $"thr={device.Threshold}|unit={device.Unit}|int={device.ReportingIntervalSeconds}";

    public static void ApplyLegacyConfig(Device device, string? cfg)
    {
        if (string.IsNullOrWhiteSpace(cfg))
        {
            return;
        }

        foreach (var part in cfg.Split('|', StringSplitOptions.RemoveEmptyEntries))
        {
            var pieces = part.Split('=', 2);
            if (pieces.Length != 2)
            {
                continue;
            }

            switch (pieces[0])
            {
                case "thr" when double.TryParse(pieces[1], out var threshold):
                    device.Threshold = threshold;
                    break;
                case "unit":
                    device.Unit = pieces[1];
                    break;
                case "int" when int.TryParse(pieces[1], out var interval):
                    device.ReportingIntervalSeconds = interval;
                    break;
            }
        }
    }
}
