namespace SensorApp.Dtos;

/// <summary>
/// Legacy-compatible DTO matching the original single-model API contract.
/// </summary>
public class LegacyDataDto
{
    public int Id { get; set; }
    public int Did { get; set; }
    public string Ts { get; set; } = string.Empty;
    public double V { get; set; }
    public double V2 { get; set; }
    public double V3 { get; set; }
    public int Typ { get; set; }
    public int St { get; set; }
    public int Flg { get; set; }
    public string N { get; set; } = string.Empty;
    public string Nm { get; set; } = string.Empty;
    public string Loc { get; set; } = string.Empty;
    public int Tp { get; set; }
    public string Cfg { get; set; } = string.Empty;
}

public class SaveResultDto
{
    public bool Ok { get; set; }
}

public class CalcResultDto
{
    public double Avg { get; set; }
    public double Mx { get; set; }
    public double Thr { get; set; }
}

public class StatsResultDto
{
    public int Total { get; set; }
    public double Avg { get; set; }
    public double Max { get; set; }
    public double Min { get; set; }
    public int Alerts { get; set; }
    public int Readings { get; set; }
    public string Last { get; set; } = string.Empty;
}
