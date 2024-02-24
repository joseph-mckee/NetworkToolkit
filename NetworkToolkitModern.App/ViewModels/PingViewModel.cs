using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using NetworkToolkitModern.App.Models;
using NetworkToolkitModern.Lib.Ping;
using ReactiveUI;

namespace NetworkToolkitModern.App.ViewModels;

public class PingViewModel : ViewModelBase
{
    private string _attempts = "4";
    private string _buffer = "32";
    private string _delay = "1000";
    private bool _fragmentable;
    private string _hops = "30";
    private string _host = "8.8.8.8";

    private ObservableCollection<PingReplyModel> _pingReplies;
    private ObservableCollection<InterfaceModel> _networkInterfaces = new();
    private string _timeout = "4000";
    private CancellationTokenSource _cancellationTokenSource;
    private bool _isIndeterminate;
    private int _progress;
    private int _selectedIndex;
    private InterfaceModel _selectedInterface;

    public PingViewModel()
    {
        var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
        foreach (var networkInterface in networkInterfaces)
        {
            try
            {
                networkInterface.GetIPProperties().GetIPv4Properties();
            }
            catch
            {
                continue;
            }
            if (networkInterface.OperationalStatus == OperationalStatus.Up)
                _networkInterfaces.Add(new InterfaceModel
                {
                    Name = networkInterface.Name,
                    Description = networkInterface.Description,
                    IpAddress = networkInterface.GetIPProperties().UnicastAddresses
                        .FirstOrDefault(ip => ip.Address.GetAddressBytes().Length == 4)
                        ?.Address.ToString(),
                    Index = networkInterface.GetIPProperties().GetIPv4Properties().Index
                });
        }
        _pingReplies = new ObservableCollection<PingReplyModel>();
    }

    public string Host
    {
        get => _host;
        set => this.RaiseAndSetIfChanged(ref _host, value);
    }

    public string Attempts
    {
        get => _attempts;
        set => this.RaiseAndSetIfChanged(ref _attempts, value);
    }

    public string Hops
    {
        get => _hops;
        set => this.RaiseAndSetIfChanged(ref _hops, value);
    }

    public string Timeout
    {
        get => _timeout;
        set => this.RaiseAndSetIfChanged(ref _timeout, value);
    }

    public string Buffer
    {
        get => _buffer;
        set => this.RaiseAndSetIfChanged(ref _buffer, value);
    }

    public string Delay
    {
        get => _delay;
        set => this.RaiseAndSetIfChanged(ref _delay, value);
    }

    public bool Fragmentable
    {
        get => _fragmentable;
        set => this.RaiseAndSetIfChanged(ref _fragmentable, value);
    }

    public bool IsIndeterminate
    {
        get => _isIndeterminate;
        set => this.RaiseAndSetIfChanged(ref _isIndeterminate, value);
    }

    public int Progress
    {
        get => _progress;
        set => this.RaiseAndSetIfChanged(ref _progress, value);
    }
    
    public ObservableCollection<PingReplyModel> PingReplies
    {
        get => _pingReplies;
        set => this.RaiseAndSetIfChanged(ref _pingReplies, value);
    }

    public ObservableCollection<InterfaceModel> NetworkInterfaces
    {
        get => _networkInterfaces;
        set => this.RaiseAndSetIfChanged(ref _networkInterfaces, value);
    }

    public InterfaceModel SelectedInterface => _selectedInterface;
    
    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedInterface, NetworkInterfaces[value]);
            this.RaiseAndSetIfChanged(ref _selectedIndex, value);
        }
    }

    public async Task StartPing()
    {
        var box = MessageBoxManager.GetMessageBoxStandard("Invalid Input", "One or more input is invalid.",
            ButtonEnum.Ok, Icon.Error);
        if (!IsInputValid()) await box.ShowAsync();
        _cancellationTokenSource = new CancellationTokenSource();
        var token = _cancellationTokenSource.Token;
        await Task.Run(() => PreparePing(token), token);
    }

    private async Task PreparePing(CancellationToken cancellationToken)
    {
        PingOptions pingOptions = new()
        {
            Ttl = int.Parse(Hops),
            DontFragment = !Fragmentable
        };
        var buffer = new byte[int.Parse(Buffer)];
        // ResolveDnsInBackground(AddressOrHostname!, cancellationToken);
        if (int.Parse(Attempts) == 0)
        {
            IsIndeterminate = true;
            while (true)
                try
                {
                    await SendPing(Progress, buffer, pingOptions, cancellationToken);
                }
                catch (OperationCanceledException ex)
                {
                    Debug.WriteLine(ex.Message);
                    break;
                }
        }
        else
        {
            for (var i = 0; i < int.Parse(Attempts); i++)
                try
                {
                    await SendPing(i, buffer, pingOptions, cancellationToken);
                }
                catch (OperationCanceledException ex)
                {
                    Debug.WriteLine(ex.Message);
                    break;
                }
        }
    }

    private async Task SendPing(int index, byte[]? buffer, PingOptions? pingOptions,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrEmpty(Host))
        {
            // StopPinging();
            // MessageBox.Show("Enter an address or hostname to ping.", "Warning", MessageBoxButton.OK,
            //     MessageBoxImage.Warning);
            return;
        }

        try
        {
            var reply = await Task.Run(
                () => PingEx.Send(
                    IPAddress.Parse(SelectedInterface.IpAddress),
                    IPAddress.Parse(Host), int.Parse(Timeout), buffer, pingOptions), cancellationToken);
            Dispatcher.UIThread.Invoke(() =>
            {
                // if (reply.Status == IPStatus.Success)
                //     SuccessfulPings++;
                // else
                //     FailedPings++;
                PingReplies.Add(new PingReplyModel(reply, index + 1));
                // ReplyTimes += reply.RoundTripTime;
                Progress++;
            });
        }
        catch (PingException ex)
        {
            // StopPinging();
            // MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        try
        {
            if (Progress < int.Parse(Attempts) || int.Parse(Attempts) == 0) await Task.Delay(int.Parse(Delay), cancellationToken);
        }
        catch (OperationCanceledException ex)
        {
            Debug.WriteLine(ex.Message);
        }
    }

    private bool IsInputValid()
    {
        if (!int.TryParse(Attempts, out _)) return false;
        if (!int.TryParse(Hops, out _)) return false;
        if (!int.TryParse(Timeout, out _)) return false;
        if (!int.TryParse(Buffer, out _)) return false;
        if (!int.TryParse(Delay, out _)) return false;
        return true;
    }

    public void StopPinging()
    {
        _cancellationTokenSource?.Cancel();
        IsIndeterminate = false;
    }

    public void Reset()
    {
        StopPinging();
        PingReplies.Clear();
        Progress = 0;
    }
}