namespace ENGINE.Models;

public class LicenseKey
{
    public int Id { get; set; }
    public string Key { get; set; } = "";
    public string Duration { get; set; } = ""; // "1Day", "7Day", "30Day", "Lifetime"
    public int DurationDays { get; set; }       // -1 = Lifetime
    public string Status { get; set; } = "Unused"; // Unused, Active, Expired, Revoked
    public string? HWID { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ActivatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? IpAddress { get; set; }
    public string? Country { get; set; }
    public string? AppVersion { get; set; }
    public bool IsOwner { get; set; }
    public bool IsRecoilKey { get; set; }
    public string? GameType { get; set; } // "R6S", "CODW", "AR", "FN"
}

public class AnalyticsEvent
{
    public int Id { get; set; }
    public string EventType { get; set; } = ""; // Activate, Validate, Revoke, Expire
    public string? Key { get; set; }
    public string? HWID { get; set; }
    public string? IpAddress { get; set; }
    public string? AppVersion { get; set; }
    public string? Note { get; set; }
    public DateTime Timestamp { get; set; }
    public bool Success { get; set; }
}

public class HwidBan
{
    public int Id { get; set; }
    public string HWID { get; set; } = "";
    public string Reason { get; set; } = "";
    public DateTime BannedAt { get; set; }
}
