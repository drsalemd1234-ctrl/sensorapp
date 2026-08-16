namespace SensorApp.Models;

public class Device
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public SensorType SensorType { get; set; }
    public DeviceStatus Status { get; set; } = DeviceStatus.Active;
    public double Threshold { get; set; } = 75;
    public string Unit { get; set; } = "C";
    public int ReportingIntervalSeconds { get; set; } = 30;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<SensorReading> Readings { get; set; } = new List<SensorReading>();
    public ICollection<Alert> Alerts { get; set; } = new List<Alert>();
}
