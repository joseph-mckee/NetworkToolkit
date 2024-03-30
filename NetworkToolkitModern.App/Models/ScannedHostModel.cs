namespace NetworkToolkitModern.App.Models;

public class ScannedHostModel
{
    public int Index { get; set; }
    public string? IpAddress { get; set; }
    public string? MacAddress { get; set; }
    public string? Vendor { get; set; }
    public string? Hostname { get; set; }
    public uint IpBits { get; set; }
}