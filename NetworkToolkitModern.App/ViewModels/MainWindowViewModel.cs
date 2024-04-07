using System;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace NetworkToolkitModern.App.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private static readonly HttpClient Client = new();
    private static readonly Ping Ping = new();
    [ObservableProperty] private string _connectionStatus = string.Empty;
    [ObservableProperty] private string _publicIpAddress = string.Empty;
    private Timer _refreshTimer;

    public MainWindowViewModel(ScanViewModel scanViewModel, PingViewModel pingViewModel,
        TracerouteViewModel tracerouteViewModel, IpConfigViewModel ipConfigViewModel, SnmpViewModel snmpViewModel)
    {
        ScanViewModel = scanViewModel;
        PingViewModel = pingViewModel;
        TracerouteViewModel = tracerouteViewModel;
        IpConfigViewModel = ipConfigViewModel;
        SnmpViewModel = snmpViewModel;
        _refreshTimer = new Timer(StatusLoop, null, 0, 2000);
    }


    public MainWindowViewModel()
    {
        ScanViewModel = new ScanViewModel();
        PingViewModel = new PingViewModel();
        TracerouteViewModel = new TracerouteViewModel();
        IpConfigViewModel = new IpConfigViewModel();
        SnmpViewModel = new SnmpViewModel();
        _refreshTimer = new Timer(StatusLoop, null, 0, 2000);
    }

    public PingViewModel PingViewModel { get; set; }
    public TracerouteViewModel TracerouteViewModel { get; set; }
    public ScanViewModel ScanViewModel { get; set; }
    public IpConfigViewModel IpConfigViewModel { get; set; }
    public SnmpViewModel SnmpViewModel { get; set; }

    private void StatusLoop(object? state)
    {
        Task.Run(RefreshStatus);
    }

    private async void RefreshStatus()
    {
        PublicIpAddress = (await GetExternalIpAddress())?.ToString() ?? "Unknown";
        try
        {
            var reply = await Ping.SendPingAsync("8.8.8.8", 1000);
            if (reply.Status != IPStatus.Success) ConnectionStatus = "Offline";
            reply = await Ping.SendPingAsync("1.1.1.1", 1000);
            ConnectionStatus = reply.Status == IPStatus.Success ? "Online" : "Offline";
        }
        catch (Exception)
        {
            ConnectionStatus = "Offline";
        }
    }

    private static async Task<IPAddress?> GetExternalIpAddress()
    {
        try
        {
            var externalIpString = (await Client.GetStringAsync("http://icanhazip.com"))
                .Replace("\\r\\n", "").Replace("\\n", "").Trim();
            return !IPAddress.TryParse(externalIpString, out var ipAddress) ? null : ipAddress;
        }
        catch (Exception)
        {
            return null;
        }
    }
}