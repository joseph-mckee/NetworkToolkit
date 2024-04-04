using NetworkToolkitModern.Lib.IP;

namespace NetworkToolkitModern.App.Models;

public class RouteRowModel
{
    public RouteRowModel(RouteTableRow routeTableRow)
    {
        Destination = routeTableRow.Destination?.ToString();
        Mask = routeTableRow.Mask?.ToString();
        Policy = routeTableRow.Policy.ToString();
        NextHop = routeTableRow.NextHop?.ToString();
        IfIndex = routeTableRow.IfIndex.ToString();
        Type = routeTableRow.Type.ToString();
        Proto = routeTableRow.Proto.ToString();
        Age = routeTableRow.Age.ToString();
        NextHopAs = routeTableRow.NextHopAs.ToString();
        Metric1 = routeTableRow.Metric1.ToString();
        Metric2 = routeTableRow.Metric2.ToString();
        Metric3 = routeTableRow.Metric3.ToString();
        Metric4 = routeTableRow.Metric4.ToString();
        Metric5 = routeTableRow.Metric5.ToString();
    }

    public string? Destination { get; set; }
    public string? Mask { get; set; }
    public string Policy { get; set; }
    public string? NextHop { get; set; }
    public string IfIndex { get; set; }
    public string Type { get; set; }
    public string Proto { get; set; }
    public string Age { get; set; }
    public string NextHopAs { get; set; }
    public string Metric1 { get; set; }
    public string Metric2 { get; set; }
    public string Metric3 { get; set; }
    public string Metric4 { get; set; }
    public string Metric5 { get; set; }
}