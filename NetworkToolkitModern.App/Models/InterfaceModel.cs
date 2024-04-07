using System.Linq;
using System.Net.NetworkInformation;
using NetworkToolkitModern.Lib.IP;

namespace NetworkToolkitModern.App.Models;

public class InterfaceModel
{
    public InterfaceModel(NetworkInterface netInt)
    {
        Name = netInt.Name;
        Description = netInt.Description;
        IpAddress = netInt.GetIPProperties().UnicastAddresses
            .FirstOrDefault(ip => ip.Address.GetAddressBytes().Length == 4)
            ?.Address.ToString();
        try
        {
            Index = netInt.GetIPProperties().GetIPv4Properties().Index;
        }
        catch (NetworkInformationException)
        {
            Index = netInt.GetIPProperties().GetIPv6Properties().Index;
        }

        Metric = Route.GetMetric(netInt);
    }

    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? IpAddress { get; init; }
    public int Index { get; init; }
    public int Metric { get; set; }
}