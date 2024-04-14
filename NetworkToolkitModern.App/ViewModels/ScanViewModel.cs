using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NetworkToolkitModern.App.Models;
using NetworkToolkitModern.Lib.Arp;
using NetworkToolkitModern.Lib.IP;
using NetworkToolkitModern.Lib.Ping;
using NetworkToolkitModern.Lib.Tcp;
using NetworkToolkitModern.Lib.Vendor;

namespace NetworkToolkitModern.App.ViewModels;

public partial class ScanViewModel : ViewModelBase
{
    private readonly Stopwatch _stopwatch = new();
    private readonly DispatcherTimer _timer = new();
    private readonly VendorLookup _vendorLookup = new();
    private CancellationTokenSource? _cancellationTokenSource;
    [ObservableProperty] private string _elapsed = "00:00:00";
    [ObservableProperty] private ObservableCollection<ScannedHostModel> _filteredScannedHosts = new();
    [ObservableProperty] private int _goal;
    [ObservableProperty] private bool _isScanning;
    [ObservableProperty] private bool _isStopped = true;
    private List<IPAddress> _localNetwork;
    [ObservableProperty] private int _progress;
    [ObservableProperty] private string _progressText = "Scanned: 0/0";
    [ObservableProperty] private string _rangeInput = "192.168.1.1-192.168.1.254";
    [ObservableProperty] private string? _scanFilterText = string.Empty;
    [ObservableProperty] private ObservableCollection<ScannedHostModel> _scannedHosts = new();
    [ObservableProperty] private int _timeout;

    public ScanViewModel()
    {
        Timeout = 50;
        ScannedHosts = new ObservableCollection<ScannedHostModel>();
        Progress = 0;
        Goal = 1;
        ScanLocalCommand();
    }

    partial void OnScannedHostsChanged(ObservableCollection<ScannedHostModel> value)
    {
        OnScanFilterTextChanged(ScanFilterText);
    }

    partial void OnScanFilterTextChanged(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            FilteredScannedHosts = ScannedHosts;
            return;
        }

        FilteredScannedHosts = new ObservableCollection<ScannedHostModel>(ScannedHosts.Where(x =>
            x.Hostname.ToString().ToLower().Contains(value.ToLower()) ||
            x.IpAddress.ToString().ToLower().Contains(value.ToLower()) ||
            x.MacAddress.ToString().ToLower().Contains(value.ToLower()) ||
            x.Vendor.ToString().ToLower().Contains(value.ToLower())).ToList());
    }

    public void Reset()
    {
        Stop();
        _stopwatch.Stop();
        _stopwatch.Reset();
        IsScanning = false;
        IsStopped = true;
        ProgressText = "Scanned: 0/0";
        ScannedHosts.Clear();
        Progress = 0;
    }

    public void Stop()
    {
        _stopwatch.Stop();
        _cancellationTokenSource?.Cancel();
    }

    [RelayCommand]
    public async Task StartScan()
    {
        Reset();
        IsScanning = true;
        IsStopped = false;
        _timer.Interval = TimeSpan.FromMilliseconds(100);
        _timer.Tick += Timer_Tick;
        _stopwatch.Start();
        _timer.Start();
        _cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = _cancellationTokenSource.Token;
        cancellationToken.ThrowIfCancellationRequested();
        var semaphore = new SemaphoreSlim(20);
        try
        {
            var range = RangeInput.Split('-');
            var scanRange = RangeFinder.GetAddressRange(range[0], range[1]);
            Goal = RangeFinder.GetNumberOfAddressesInRange(range[0], range[1]);

            var tasks = new List<Task>();

            foreach (var host in scanRange)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await semaphore.WaitAsync(cancellationToken); // Throttle concurrency

                var task = Task.Run(async () =>
                {
                    try
                    {
                        await ScanHost(host, cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        // tasks.RemoveAll(_ => true);
                        tasks.Clear();
                    }
                    finally
                    {
                        semaphore.Release(); // Release on task completion
                    }
                }, cancellationToken);

                tasks.Add(task);
            }

            await Task.WhenAll(tasks); // Wait for all tasks to complete
        }
        catch (OperationCanceledException)
        {
            IsScanning = false;
            _stopwatch.Stop();
            _timer.Stop();
            // ReSharper disable once MethodSupportsCancellation
            await Task.Delay(1000);
        }
        finally
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                Goal = 100;
                Progress = 100;
            }

            IsScanning = false;
            IsStopped = true;
            _stopwatch.Stop();
            _timer.Stop();
        }
    }

    private async ValueTask ScanHost(IPAddress address, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        try
        {
            // only tries ARP resolution if the address is part of the computer's local subnet
            var tryArp = _localNetwork.Contains(address);
            if (tryArp)
            {
                if (await Arp.ArpScanAsync(address, 1000, token)) await AddHostAsync(address, token);
                return;
            }

            // if not on the local subnet, try other scan methods
            var tcpScanTask = Tcp.ScanHostAsync(address, 1000, token);
            var pingScanTask = PingEx.ScanHostAsync(address, 1000, token);
            var scanTask = await Task.WhenAny(tcpScanTask, pingScanTask);
            token.ThrowIfCancellationRequested();
            if (await scanTask) await AddHostAsync(address, token);
        }
        catch (OperationCanceledException)
        {
            Debug.WriteLine($"Scanning host: {address} operation cancelled.");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error scanning {address}: {ex}");
            throw;
        }
        finally
        {
            // Always update the progress if the operation was not canceled.
            if (!token.IsCancellationRequested)
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    Progress++;
                    ProgressText = $"Scanned: {Progress}/{Goal}";
                });
        }
    }


    private void Timer_Tick(object? sender, EventArgs e)
    {
        Elapsed = _stopwatch.Elapsed.ToString(@"hh\:mm\:ss");
    }


    private async Task AddHostAsync(IPAddress hostAddress, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        try
        {
            var name = await NameResolution.ResolveHostnameAsync(hostAddress, token);
            var macAddress = Arp.GetArpEntry(hostAddress)?.MacAddress ?? "Unknown";
            if (macAddress == "Unknown") // Look to see if the host is the current machine.
                foreach (var netInf in NetworkInterface.GetAllNetworkInterfaces())
                {
                    var interfaceAddress = netInf.GetIPProperties().UnicastAddresses
                        .FirstOrDefault(ip => ip.Address.AddressFamily == AddressFamily.InterNetwork)?.Address;
                    // Only proceed if the host address matches that of an address assigned to the local machine.
                    if (!Equals(interfaceAddress, hostAddress)) continue;
                    var rawMacAddress = netInf.GetPhysicalAddress().ToString();
                    var formattedMacAddress = new StringBuilder(rawMacAddress);
                    for (var i = 2; i < formattedMacAddress.Length; i += 3) formattedMacAddress.Insert(i, ":");
                    macAddress = formattedMacAddress.ToString();
                    name = Dns.GetHostName();
                }

            var vendor = _vendorLookup.GetVendorName(macAddress);
            token.ThrowIfCancellationRequested();
            Dispatcher.UIThread.Invoke(() =>
            {
                ScannedHosts.Add(new ScannedHostModel
                {
                    IpAddress = hostAddress.ToString(),
                    MacAddress = macAddress,
                    Vendor = vendor,
                    Hostname = name
                });
            });
        }
        catch (OperationCanceledException)
        {
            Debug.WriteLine($"Adding host: {hostAddress} operation cancelled.");
        }
    }

    [RelayCommand]
    public void ScanLocalCommand()
    {
        var bestInterface = Route.GetBestInterface();
        var localIp = bestInterface.GetIPProperties().UnicastAddresses
            .First(ip => ip.Address.AddressFamily.Equals(AddressFamily.InterNetwork)).Address;
        var localSubnet = bestInterface.GetIPProperties().UnicastAddresses.First(ip => ip.Address.Equals(localIp))
            .IPv4Mask;
        var netInf = new NetInfo(localIp, localSubnet);
        var startAddress = IpMath.BitsToIp(IpMath.IpToBits(netInf.NetworkAddress) + 1);
        var endAddress = IpMath.BitsToIp(IpMath.IpToBits(netInf.BroadcastAddress) - 1);
        RangeInput = $"{startAddress}-{endAddress}";
        var netInfo = new NetInfo(localIp, localSubnet);
        _localNetwork = netInfo.GetAddressRangeFromNetwork().ToList();
    }
}