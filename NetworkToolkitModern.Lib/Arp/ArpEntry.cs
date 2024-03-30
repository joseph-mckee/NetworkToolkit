namespace NetworkToolkitModern.Lib.Arp;

public class ArpEntry
{
    public string? IpAddress { get; init; }
    public string? MacAddress { get; init; }
    public int Index { get; init; }
}