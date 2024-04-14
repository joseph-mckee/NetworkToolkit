namespace NetworkToolkitModern.App.Models;

public class ArpEntryModel
{
    public string IpAddress { get; set; } = string.Empty;
    public string MacAddress { get; set; } = string.Empty;
    public string Vendor { get; set; } = string.Empty;
}