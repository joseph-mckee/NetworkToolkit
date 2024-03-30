using System.Net;

namespace NetworkToolkitModern.Lib.IP;

public class NetInfo
{
    private readonly uint _ipAddress;
    private readonly uint _subnetMask;

    private NetInfo(uint ipAddress, uint subnetMask)
    {
        _ipAddress = ipAddress;
        _subnetMask = subnetMask;
        NetworkAddress = GetNetworkAddress();
        BroadcastAddress = GetBroadcastAddress();
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public NetInfo(IPAddress ipAddress, IPAddress subnetMask) : this(IpMath.IpToBits(ipAddress),
        IpMath.IpToBits(subnetMask))
    {
    }

    public NetInfo(string ipAddress, string subnetMask) : this(IPAddress.Parse(ipAddress), IPAddress.Parse(subnetMask))
    {
    }

    public IPAddress NetworkAddress { get; }
    public IPAddress BroadcastAddress { get; }

    public IEnumerable<IPAddress> GetAddressRangeFromNetwork()
    {
        var startOfRangeBits = GetNetworkAddressAsBits() + 1;
        var endOfRangeBits = GetBroadcastAddressAsBits() - 1;
        for (var addressBits = startOfRangeBits; addressBits <= endOfRangeBits; addressBits++)
            yield return IpMath.BitsToIp(addressBits);
    }

    private uint GetNetworkAddressAsBits()
    {
        return _subnetMask & _ipAddress;
    }

    private IPAddress GetNetworkAddress()
    {
        return IpMath.BitsToIp(GetNetworkAddressAsBits());
    }

    private uint GetBroadcastAddressAsBits()
    {
        return ~_subnetMask | GetNetworkAddressAsBits();
    }

    private IPAddress GetBroadcastAddress()
    {
        return IpMath.BitsToIp(GetBroadcastAddressAsBits());
    }
}