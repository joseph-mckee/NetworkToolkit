using System.Collections;
using System.Net;
using NetworkToolkitModern.App.Models;

namespace NetworkToolkitModern.App.Services;

public class IpAddressComparer : IComparer
{
    public int Compare(object? x, object? y)
    {
        var host1 = x as ScannedHostModel;
        var host2 = y as ScannedHostModel;

        if (host1?.IpAddress == null) return 0;
        var address1 = IPAddress.Parse(host1.IpAddress);
        if (host2?.IpAddress == null) return 0;
        var address2 = IPAddress.Parse(host2.IpAddress);

        var bytes1 = address1.GetAddressBytes();
        var bytes2 = address2.GetAddressBytes();

        for (var i = 0; i < bytes1.Length; i++)
        {
            if (bytes1[i] < bytes2[i]) return -1;
            if (bytes1[i] > bytes2[i]) return 1;
        }

        return 0;
    }
}