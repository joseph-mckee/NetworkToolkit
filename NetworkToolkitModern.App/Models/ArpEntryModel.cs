namespace NetworkToolkitModern.App.Models;

public class ArpEntryModel
{
    public string? IpAddress { get; set; }
    public string? MacAddress { get; set; }
    public int Index { get; set; }
    public string? Vendor { get; set; }
}