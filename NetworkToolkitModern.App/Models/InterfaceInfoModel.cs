using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using NetworkToolkitModern.Lib.Arp;
using NetworkToolkitModern.Lib.IP;

namespace NetworkToolkitModern.App.Models;

[SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
public class InterfaceInfoModel
{
    public InterfaceInfoModel(NetworkInterface netInt)
    {
        Metric = Route.GetMetric(netInt);
        Name = netInt.Name; // 
        Type = netInt.NetworkInterfaceType.ToString(); // 
        Description = netInt.Description; // 
        Id = netInt.Id; // 
        Speed = $"{netInt.Speed / 1000000} Mbps"; // 
        Status = netInt.OperationalStatus.ToString(); // 
        MulticastSupport = netInt.SupportsMulticast.ToString();
        ReceiveOnly = netInt.IsReceiveOnly.ToString();
        var rawMacAddress = netInt.GetPhysicalAddress().ToString();
        var formattedMacAddress = new StringBuilder(rawMacAddress);
        for (var i = 2; i < formattedMacAddress.Length; i += 3) formattedMacAddress.Insert(i, ":");
        PhysicalAddress = formattedMacAddress.ToString(); //
        var properties = netInt.GetIPProperties(); // 
        Addresses = properties.UnicastAddresses; //
        AnycastAddresses = properties.AnycastAddresses; //
        DnsAddresses = properties.DnsAddresses; //
        DnsSuffix = properties.DnsSuffix;
        GatewayAddresses = properties.GatewayAddresses; //
        MulticastAddresses = properties.MulticastAddresses; //
        DhcpAddresses = properties.DhcpServerAddresses; //
        IsDnsEnabled = properties.IsDnsEnabled; // 
        WinsServerAddresses = properties.WinsServersAddresses; //
        IsDynamicDnsEnabled = properties.IsDynamicDnsEnabled; //
        SubnetMask = Addresses.FirstOrDefault(ip => ip.Address.AddressFamily == AddressFamily.InterNetwork)?.IPv4Mask
            .ToString();
        try
        {
            var ipv4Properties = properties.GetIPv4Properties();
            Index = ipv4Properties.Index;
            Mtu = ipv4Properties.Mtu; //
            UsesWins = ipv4Properties.UsesWins; //
            IsDhcpEnabled = ipv4Properties.IsDhcpEnabled; //
            IsForwardingEnabled = ipv4Properties.IsForwardingEnabled; //
            IsAutomaticPrivateAddressingActive = ipv4Properties.IsAutomaticPrivateAddressingActive;
            IsAutomaticPrivateAddressingEnabled = ipv4Properties.IsAutomaticPrivateAddressingEnabled;
        }
        catch (Exception)
        {
            var ipv6Properties = properties.GetIPv6Properties();
            Index = ipv6Properties.Index;
            Mtu = ipv6Properties.Mtu; //
        }

        var ipStatistics = netInt.GetIPStatistics();
        BytesSent = ipStatistics.BytesSent.ToString("N0");
        BytesReceived = ipStatistics.BytesReceived.ToString("N0");
        UnicastPacketsSent = ipStatistics.UnicastPacketsSent.ToString("N0");
        UnicastPacketsReceived = ipStatistics.UnicastPacketsReceived.ToString("N0");
        NonUnicastPacketsSent = ipStatistics.NonUnicastPacketsSent.ToString("N0");
        NonUnicastPacketsReceived = ipStatistics.NonUnicastPacketsReceived.ToString("N0");
        OutgoingPacketsDiscarded = ipStatistics.OutgoingPacketsDiscarded.ToString("N0");
        IncomingPacketsDiscarded = ipStatistics.IncomingPacketsDiscarded.ToString("N0");
        OutgoingPacketsWithErrors = ipStatistics.OutgoingPacketsWithErrors.ToString("N0");
        IncomingPacketsWithErrors = ipStatistics.IncomingPacketsWithErrors.ToString("N0");
        OutputQueueLength = ipStatistics.OutputQueueLength.ToString("N0");
        IncomingUnknownProtocolPackets = ipStatistics.IncomingUnknownProtocolPackets.ToString("N0");

        var arpEntries = Arp.GetArpCache();
        var relevantEntries = arpEntries.Where(x => x.Index == Index).ToList();
        if (relevantEntries.Any()) IsArp = true;
        foreach (var entry in relevantEntries)
            ArpEntryModels.Add(new ArpEntryModel
            {
                IpAddress = entry.IpAddress,
                MacAddress = entry.MacAddress
            });

        var routes = Route.GetRoutes();
        var relevantRoutes = routes.Where(x => x.IfIndex == Index).ToList();
        foreach (var route in relevantRoutes) RouteRowModels.Add(new RouteRowModel(route));
    }

    public bool IsArp { get; set; }

    public List<ArpEntryModel> ArpEntryModels { get; set; } = new();

    public List<RouteRowModel> RouteRowModels { get; set; } = new();

    public int Index { get; set; }

    public string IncomingUnknownProtocolPackets { get; set; }

    public string OutputQueueLength { get; set; }

    public string IncomingPacketsWithErrors { get; set; }

    public string OutgoingPacketsWithErrors { get; set; }

    public string IncomingPacketsDiscarded { get; set; }

    public string OutgoingPacketsDiscarded { get; set; }

    public string NonUnicastPacketsReceived { get; set; }

    public string NonUnicastPacketsSent { get; set; }

    public string UnicastPacketsReceived { get; set; }

    public string UnicastPacketsSent { get; set; }

    public string BytesSent { get; set; }

    public string BytesReceived { get; set; }

    public string? SubnetMask { get; set; }

    public int Metric { get; set; }

    public bool IsAutomaticPrivateAddressingEnabled { get; set; }

    public bool IsAutomaticPrivateAddressingActive { get; set; }

    public bool IsForwardingEnabled { get; set; }

    public bool IsDhcpEnabled { get; set; }

    public string Name { get; private set; }
    public string Type { get; private set; }
    public string Description { get; private set; }
    public string Id { get; private set; }
    public string Speed { get; private set; }
    public string Status { get; private set; }
    public string MulticastSupport { get; private set; }
    public string ReceiveOnly { get; private set; }
    public string PhysicalAddress { get; private set; }
    public UnicastIPAddressInformationCollection Addresses { get; }
    public IPAddressInformationCollection AnycastAddresses { get; private set; }
    public IPAddressCollection DnsAddresses { get; private set; }

    public string DnsSuffix { get; private set; }

    public bool UsesWins { get; set; }

    public int Mtu { get; set; }
    public bool IsDynamicDnsEnabled { get; set; }

    public IPAddressCollection WinsServerAddresses { get; set; }

    public bool IsDnsEnabled { get; set; }

    public IPAddressCollection DhcpAddresses { get; set; }

    public MulticastIPAddressInformationCollection MulticastAddresses { get; set; }

    public GatewayIPAddressInformationCollection GatewayAddresses { get; set; }
}