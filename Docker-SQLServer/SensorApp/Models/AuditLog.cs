namespace SensorApp.Models;

public class AuditLog
{
    public int Id { get; set; }
    public int? DeviceId { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public bool IsAlert { get; set; }
}
