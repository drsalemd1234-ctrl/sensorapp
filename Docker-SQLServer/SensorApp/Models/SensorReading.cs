namespace SensorApp.Models;

public class SensorReading
{
    public int Id { get; set; }
    public int DeviceId { get; set; }
    public DateTime Timestamp { get; set; }
    public double Temperature { get; set; }
    public double Humidity { get; set; }
    public double Pressure { get; set; }
    public DeviceStatus Status { get; set; } = DeviceStatus.Active;

    public Device Device { get; set; } = null!;
}
