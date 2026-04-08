namespace TonerTrack.Domain.ValueObjects;

public enum AlertSeverity { 
    Info, 
    Warning, 
    Critical 
}

public sealed record PrinterAlert(string Description, AlertSeverity Severity)
{
    public static AlertSeverity FromSnmpCode(string code) => code switch
    {
        "3" => AlertSeverity.Critical,
        "4" => AlertSeverity.Warning,
        _ => AlertSeverity.Info
    };
}
