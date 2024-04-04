using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using ReactiveUI;

namespace NetworkToolkitModern.App.ViewModels;

public class MainWindowViewModel : ViewModelBase
{

    private string _publicIpAddress = string.Empty;
    private string _connectionStatus = string.Empty;
    
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
            Ping ping = new();
            var reply = await ping.SendPingAsync("8.8.8.8", 1000);
            ConnectionStatus = reply.Status == IPStatus.Success ? "Online" : "Offline";
        }
        catch (Exception)
        {
            ConnectionStatus = "Offline";
        }
    }
    
    private async Task<IPAddress?> GetExternalIpAddress()
    {
        try
        {
            var externalIpString = (await new HttpClient().GetStringAsync("http://icanhazip.com"))
                .Replace("\\r\\n", "").Replace("\\n", "").Trim();
            return !IPAddress.TryParse(externalIpString, out var ipAddress) ? null : ipAddress;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public string PublicIpAddress
    {
        get => _publicIpAddress;
        set => this.RaiseAndSetIfChanged(ref _publicIpAddress, value);
    }

    public string ConnectionStatus
    {
        get => _connectionStatus;
        set => this.RaiseAndSetIfChanged(ref _connectionStatus, value);
    }
    public PingViewModel PingViewModel { get; set; }
    public TracerouteViewModel TracerouteViewModel { get; set; }
    public ScanViewModel ScanViewModel { get; set; }
    public IpConfigViewModel IpConfigViewModel { get; set; }
    public SnmpViewModel SnmpViewModel { get; set; }
}