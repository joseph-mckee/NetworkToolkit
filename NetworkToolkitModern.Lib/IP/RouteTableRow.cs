using System.Net;

namespace NetworkToolkitModern.Lib.IP;

public class RouteTableRow
{
    public IPAddress? Destination { get; set; }
    public IPAddress? Mask { get; set; }
    public int Policy { get; set; }
    public IPAddress? NextHop { get; set; }
    public int IfIndex { get; set; }
    public RouteType Type { get; set; }
    public RouteProtocol Proto { get; set; }
    public uint Age { get; set; }
    public int NextHopAs { get; set; }
    public int Metric1 { get; set; }
    public int Metric2 { get; set; }
    public int Metric3 { get; set; }
    public int Metric4 { get; set; }
    public int Metric5 { get; set; }
}