using System.Net;

namespace NetworkToolkitModern.Lib.IP;

public static class RangeFinder
{
    private static IEnumerable<IPAddress> GetAddressRange(IPAddress startOfRange, IPAddress endOfRange)
    {
        var startOfRangeBits = IpMath.IpToBits(startOfRange);
        var endOfRangeBits = IpMath.IpToBits(endOfRange);
        for (var addressBits = startOfRangeBits; addressBits <= endOfRangeBits; addressBits++)
            yield return IpMath.BitsToIp(addressBits);
    }

    public static IEnumerable<IPAddress> GetAddressRange(string startOfRange, string endOfRange)
    {
        return GetAddressRange(IPAddress.Parse(startOfRange), IPAddress.Parse(endOfRange));
    }

    public static bool IsAddressInRange(IPAddress address, List<IPAddress> range)
    {
        var addressBits = IpMath.IpToBits(address);
        var firstBits = IpMath.IpToBits(range.First());
        var lastBits = IpMath.IpToBits(range.Last());
        return addressBits <= lastBits && addressBits >= firstBits;
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public static int GetNumberOfAddressesInRange(IPAddress startOfRange, IPAddress endOfRange)
    {
        var startOfRangeBits = IpMath.IpToBits(startOfRange);
        var endOfRangeBits = IpMath.IpToBits(endOfRange);
        return (int)(endOfRangeBits - startOfRangeBits + 1);
    }

    public static int GetNumberOfAddressesInRange(string startOfRange, string endOfRange)
    {
        return GetNumberOfAddressesInRange(IPAddress.Parse(startOfRange), IPAddress.Parse(endOfRange));
    }
}