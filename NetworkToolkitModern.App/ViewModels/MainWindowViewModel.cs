using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using ReactiveUI;

namespace NetworkToolkitModern.App.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty] private string _publicIpAddress = string.Empty;
    [ObservableProperty] private string _connectionStatus = string.Empty;
    private static readonly HttpClient Client = new();
    private static readonly Ping Ping = new();

    public MainWindowViewModel(ScanViewModel scanViewModel, PingViewModel pingViewModel,
        TracerouteViewModel tracerouteViewModel, IpConfigViewModel ipConfigViewModel, SnmpViewModel snmpViewModel)
    {
        ScanViewModel = scanViewModel;
        PingViewModel = pingViewModel;
        TracerouteViewModel = tracerouteViewModel;
        IpConfigViewModel = ipConfigViewModel;
        SnmpViewModel = snmpViewModel;
        Task.Run(StatusLoop);
    }

    private void StatusLoop()
    {
        try
        {
            while (true)
            {
                RefreshStatus();
                Thread.Sleep(5000);
            }
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
            throw;
        }
    }


    public MainWindowViewModel()
    {
        ScanViewModel = new ScanViewModel();
        PingViewModel = new PingViewModel();
        TracerouteViewModel = new TracerouteViewModel();
        IpConfigViewModel = new IpConfigViewModel();
        SnmpViewModel = new SnmpViewModel();
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

    public PingViewModel PingViewModel { get; set; }
    public TracerouteViewModel TracerouteViewModel { get; set; }
    public ScanViewModel ScanViewModel { get; set; }
    public IpConfigViewModel IpConfigViewModel { get; set; }
    public SnmpViewModel SnmpViewModel { get; set; }
}