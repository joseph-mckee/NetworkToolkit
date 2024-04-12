using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NetworkToolkitModern.App.Models;
using NetworkToolkitModern.Lib.Arp;
using NetworkToolkitModern.Lib.IP;
using NetworkToolkitModern.Lib.Vendor;

namespace NetworkToolkitModern.App.ViewModels;

public partial class IpConfigViewModel : ViewModelBase
{
    [ObservableProperty] private ObservableCollection<InterfaceModel> _interfaceModels = new();
    [ObservableProperty] private bool _isFocused;
    [ObservableProperty] private bool _doRefresh = true;
    [ObservableProperty] private int _refreshInterval;

    [ObservableProperty] private int _index;
    [ObservableProperty] private int _metric;
    [ObservableProperty] private string? _name;
    [ObservableProperty] private string? _type;
    [ObservableProperty] private string? _description;
    [ObservableProperty] private string? _id;
    [ObservableProperty] private string? _speed;
    [ObservableProperty] private string? _status;
    [ObservableProperty] private bool _multicastSupport;
    [ObservableProperty] private bool _receiveOnly;
    [ObservableProperty] private bool _usesWins;
    [ObservableProperty] private bool _dhcpEnabled;
    [ObservableProperty] private bool _forwardingEnabled;
    [ObservableProperty] private bool _automaticPrivateAddressingActive;
    [ObservableProperty] private bool _automaticPrivateAddressingEnabled;
    [ObservableProperty] private bool _dnsEnabled;
    [ObservableProperty] private bool _dynamicDnsEnabled;
    [ObservableProperty] private string? _physicalAddress;
    [ObservableProperty] private UnicastIPAddressInformationCollection? _addresses;
    [ObservableProperty] private IPAddressInformationCollection? _anycastAddresses;
    [ObservableProperty] private IPAddressCollection? _dnsAddresses;
    [ObservableProperty] private string? _dnsSuffix;
    [ObservableProperty] private GatewayIPAddressInformationCollection? _gatewayAddresses;
    [ObservableProperty] private MulticastIPAddressInformationCollection? _multicastAddresses;
    [ObservableProperty] private IPAddressCollection? _dhcpAddresses;
    [ObservableProperty] private IPAddressCollection? _winsServerAddresses;
    [ObservableProperty] private string? _subnetMask;
    [ObservableProperty] private int _mtu;
    [ObservableProperty] private string? _bytesSent;
    [ObservableProperty] private string? _bytesReceived;
    [ObservableProperty] private string? _unicastPacketsSent;
    [ObservableProperty] private string? _unicastPacketsReceived;
    [ObservableProperty] private string? _nonUnicastPacketsSent;
    [ObservableProperty] private string? _nonUnicastPacketsReceived;
    [ObservableProperty] private string? _outgoingPacketsDiscarded;
    [ObservableProperty] private string? _incomingPacketsDiscarded;
    [ObservableProperty] private string? _outgoingPacketsWithErrors;
    [ObservableProperty] private string? _incomingPacketsWithErrors;
    [ObservableProperty] private string? _outputQueueLength;
    [ObservableProperty] private string? _incomingUnknownProtocolPackets;
    [ObservableProperty] private ObservableCollection<ArpEntryModel> _arpEntryModels = new();
    [ObservableProperty] private ObservableCollection<RouteRowModel> _routeRowModels = new();
    [ObservableProperty] private ObservableCollection<ArpEntryModel> _filteredArpEntryModels = new();
    [ObservableProperty] private string? _arpTableFilterText = string.Empty;

    [ObservableProperty] private bool _hasArp;
    [ObservableProperty] private bool _hasRoutes;

    [ObservableProperty] private bool _isInterfaceInfoExpanded = true;
    [ObservableProperty] private bool _isAddressInfoExpanded = true;
    [ObservableProperty] private bool _isStatisticsExpanded = true;
    [ObservableProperty] private bool _isRouteTableExpanded = true;
    [ObservableProperty] private bool _isArpTableExpanded = true;

    private Timer? _refreshTimer;
    [ObservableProperty] private int _selectedInterface;
    private readonly VendorLookup _vendorLookup = new();
    private readonly Dictionary<string, string> _vendorCache = new();
    private string _cachedFilterText = string.Empty;

    public IpConfigViewModel()
    {
        Update();
        SelectedInterface = 0;
        RefreshInterval = 1;
        FilteredArpEntryModels = ArpEntryModels;
    }

    partial void OnArpEntryModelsChanged(ObservableCollection<ArpEntryModel> value)
    {
        if (string.IsNullOrEmpty(ArpTableFilterText))
        {
            FilteredArpEntryModels = ArpEntryModels;
            return;
        }

        if (ArpTableFilterText != _cachedFilterText) OnArpTableFilterTextChanged(ArpTableFilterText);
    }

    partial void OnArpTableFilterTextChanged(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            FilteredArpEntryModels = ArpEntryModels;
            return;
        }

        _cachedFilterText = value;
        FilteredArpEntryModels = new(ArpEntryModels.Where(x =>
            x.Vendor.ToLower().Contains(value.ToLower()) || x.IpAddress.ToLower().Contains(value.ToLower()) ||
            x.MacAddress.ToLower().Contains(value.ToLower())).ToList());
    }

    partial void OnDoRefreshChanged(bool value)
    {
        if (!value) _refreshTimer?.Dispose();
        else _refreshTimer = new Timer(Refresh, null, 0, RefreshInterval * 1000);
    }

    partial void OnRefreshIntervalChanged(int value)
    {
        _refreshTimer?.Dispose();
        _refreshTimer = new Timer(Refresh, null, 0, value * 1000);
    }

    partial void OnSelectedInterfaceChanged(int value)
    {
        if (value < 0) return;
        Update();
    }

    [RelayCommand]
    public void ExpandAll()
    {
        IsInterfaceInfoExpanded = true;
        IsAddressInfoExpanded = true;
        IsStatisticsExpanded = true;
        IsRouteTableExpanded = true;
        IsArpTableExpanded = true;
    }

    [RelayCommand]
    public void CollapseAll()
    {
        IsInterfaceInfoExpanded = false;
        IsAddressInfoExpanded = false;
        IsStatisticsExpanded = false;
        IsRouteTableExpanded = false;
        IsArpTableExpanded = false;
    }

    [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
    private void UpdateCurrentInterface(NetworkInterface networkInterface)
    {
        Metric = Route.GetMetric(networkInterface);
        if (Name != networkInterface.Name) Name = networkInterface.Name;
        if (Type != networkInterface.NetworkInterfaceType.ToString())
            Type = networkInterface.NetworkInterfaceType.ToString();
        if (Description != networkInterface.Description) Description = networkInterface.Description;
        if (Id != networkInterface.Id) Id = networkInterface.Id;
        if (Speed != $"{networkInterface.Speed / 1000000} Mbps") Speed = $"{networkInterface.Speed / 1000000} Mbps";
        if (Status != networkInterface.OperationalStatus.ToString())
            Status = networkInterface.OperationalStatus.ToString();
        if (ReceiveOnly != networkInterface.IsReceiveOnly) ReceiveOnly = networkInterface.IsReceiveOnly;
        var rawMacAddress = networkInterface.GetPhysicalAddress().ToString();
        var formattedMacAddress = new StringBuilder(rawMacAddress);
        for (var i = 2; i < formattedMacAddress.Length; i += 3) formattedMacAddress.Insert(i, ":");
        if (PhysicalAddress != formattedMacAddress.ToString()) PhysicalAddress = formattedMacAddress.ToString();
        var props = networkInterface.GetIPProperties();
        Addresses = props.UnicastAddresses;
        AnycastAddresses = props.AnycastAddresses;
        DnsAddresses = props.DnsAddresses;
        if (DnsSuffix != props.DnsSuffix) DnsSuffix = props.DnsSuffix;
        GatewayAddresses = props.GatewayAddresses;
        MulticastAddresses = props.MulticastAddresses;
        DhcpAddresses = props.DhcpServerAddresses;
        if (DynamicDnsEnabled != props.IsDynamicDnsEnabled) DynamicDnsEnabled = props.IsDynamicDnsEnabled;
        var subnetMask = Addresses.FirstOrDefault(ip => ip.Address.AddressFamily == AddressFamily.InterNetwork)
            ?.IPv4Mask.ToString();
        if (SubnetMask != subnetMask) SubnetMask = subnetMask ?? string.Empty;

        try
        {
            var properties = networkInterface.GetIPProperties().GetIPv4Properties();
            if (Index != properties.Index) Index = properties.Index;
            if (Mtu != properties.Mtu) Mtu = properties.Mtu;
            if (UsesWins != properties.UsesWins) UsesWins = properties.UsesWins;
            if (DhcpEnabled != properties.IsDhcpEnabled) DhcpEnabled = properties.IsDhcpEnabled;
            if (ForwardingEnabled != properties.IsForwardingEnabled) ForwardingEnabled = properties.IsForwardingEnabled;
            if (AutomaticPrivateAddressingActive != properties.IsAutomaticPrivateAddressingActive)
                AutomaticPrivateAddressingActive = properties.IsAutomaticPrivateAddressingActive;
            if (AutomaticPrivateAddressingEnabled != properties.IsAutomaticPrivateAddressingEnabled)
                AutomaticPrivateAddressingEnabled = properties.IsAutomaticPrivateAddressingEnabled;
        }
        catch (NetworkInformationException)
        {
            var properties = networkInterface.GetIPProperties().GetIPv6Properties();
            if (Index != properties.Index) Index = properties.Index;
            if (Mtu != properties.Mtu) Mtu = properties.Mtu;
        }

        var statistics = networkInterface.GetIPStatistics();
        BytesSent = statistics.BytesSent.ToString("N0");
        BytesReceived = statistics.BytesReceived.ToString("N0");
        UnicastPacketsSent = statistics.UnicastPacketsSent.ToString("N0");
        UnicastPacketsReceived = statistics.UnicastPacketsReceived.ToString("N0");
        NonUnicastPacketsSent = statistics.NonUnicastPacketsSent.ToString("N0");
        NonUnicastPacketsReceived = statistics.NonUnicastPacketsReceived.ToString("N0");
        OutgoingPacketsDiscarded = statistics.OutgoingPacketsDiscarded.ToString("N0");
        IncomingPacketsDiscarded = statistics.IncomingPacketsDiscarded.ToString("N0");
        OutgoingPacketsWithErrors = statistics.OutgoingPacketsWithErrors.ToString("N0");
        IncomingPacketsWithErrors = statistics.IncomingPacketsWithErrors.ToString("N0");
        OutputQueueLength = statistics.OutputQueueLength.ToString("N0");
        IncomingUnknownProtocolPackets = statistics.IncomingUnknownProtocolPackets.ToString("N0");

        var arpEntries = Arp.GetArpCache();
        var relevantEntries = arpEntries.Where(x => x.Index == Index).ToList();
        HasArp = relevantEntries.Any();
        ArpEntryModels = new();
        foreach (var entry in relevantEntries)
        {
            if (!_vendorCache.ContainsKey(entry.MacAddress))
                _vendorCache.Add(entry.MacAddress, _vendorLookup.GetVendorName(entry.MacAddress));
            ArpEntryModels.Add(new ArpEntryModel
            {
                IpAddress = entry.IpAddress,
                MacAddress = entry.MacAddress,
                Vendor = _vendorCache[entry.MacAddress]
            });
        }

        var routes = Route.GetRoutes();
        var relevantRoutes = routes.Where(x => x.IfIndex == Index).ToList();
        HasRoutes = relevantRoutes.Any();
        RouteRowModels = new();
        foreach (var route in relevantRoutes) RouteRowModels.Add(new RouteRowModel(route));
    }

    private void Refresh(object? state)
    {
        Task.Run(Update);
    }

    [RelayCommand]
    public void Update()
    {
        try
        {
            var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces()
                .OrderBy(o => o.GetIPStatistics().BytesReceived).Reverse().ToList();
            var comparedInterfaces = new ObservableCollection<InterfaceModel>();
            foreach (var networkInterface in networkInterfaces)
            {
                comparedInterfaces.Add(new InterfaceModel(networkInterface));
            }

            if (InterfaceModels.Count < comparedInterfaces.Count)
            {
                foreach (var model in comparedInterfaces)
                {
                    var difference = InterfaceModels.FirstOrDefault(x => x.Index == model.Index);
                    if (difference is null) InterfaceModels.Add(model);
                }
            }

            if (InterfaceModels.Count > comparedInterfaces.Count)
            {
                for (var index = 0; index < InterfaceModels.Count; index++)
                {
                    var model = InterfaceModels[index];
                    var difference = comparedInterfaces.FirstOrDefault(x => x.Index == model.Index);
                    if (difference is null) InterfaceModels.Remove(model);
                }
            }

            try
            {
                var currentInterface =
                    networkInterfaces.FirstOrDefault(x => x.Name == InterfaceModels[SelectedInterface].Name);
                if (currentInterface is null)
                {
                    return;
                }

                UpdateCurrentInterface(currentInterface);
            }
            catch (ArgumentOutOfRangeException)
            {
                SelectedInterface = 0;
            }
        }
        catch (ArgumentOutOfRangeException)
        {
            Debug.WriteLine("Caught index out of range bug. Currently brute forcing through it.");
            throw;
        }
    }
}