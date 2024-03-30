using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace NetworkToolkitModern.Lib.IP;

public static class NetworkEnv
{
    public static IPAddress GetBestIp()
    {
        var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
        var best = networkInterfaces.Where(networkInterface =>
            networkInterface.OperationalStatus == OperationalStatus.Up &&
            IPAddress.TryParse(networkInterface.GetIPProperties().UnicastAddresses[0].Address.ToString(), out _));
        Debug.WriteLine(best.First().GetIPProperties().UnicastAddresses
            .First(ip => ip.Address.AddressFamily == AddressFamily.InterNetwork).Address);
        return best.First().GetIPProperties().UnicastAddresses
            .First(ip => ip.Address.AddressFamily == AddressFamily.InterNetwork).Address;
    }
}