using System.Net;

namespace NetworkToolkitModern.Lib.IP;

public static class IPMath
{
    public static uint IPToBits(IPAddress address, bool reverse = true)
    {
        var addressAsBytes = address.GetAddressBytes();
        if (reverse) Array.Reverse(addressAsBytes);
        return BitConverter.ToUInt32(addressAsBytes, 0);
    }

    public static IPAddress BitsToIP(uint address, bool reverse = true)
    {
        var addressAsBytes = BitConverter.GetBytes(address);
        if (reverse) Array.Reverse(addressAsBytes);
        return new IPAddress(addressAsBytes);
    }
}