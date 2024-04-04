using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
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
    private CancellationTokenSource? _cancellationTokenSource;
    private string _delay = "200";
    private int _failedPings;
    private bool _fragmentable;
    private string _hops = "30";
    private string _host = "8.8.8.8";
    private string _hostname = string.Empty;
    private bool _isContinuous;
    private bool _isIndeterminate;
    private bool _isPinging;
    private bool _isStopped;
    private ObservableCollection<InterfaceModel> _networkInterfaces = new();
    private string _packetLoss = "0%";
    private ObservableCollection<PingReplyModel>? _pingReplies;
    private int _progress;

    private ulong _replyTimes;
    private string _roundTripTime = string.Empty;
    private int _selectedIndex;
    private InterfaceModel? _selectedInterface;
    private int _successfulPings;
    private string _timeout = "1000";

    public PingViewModel()
    {
        Reset();
    }

    public event EventHandler? ScrollToNewItemRequested;

    protected virtual void OnScrollToNewItemRequested()
    {
        ScrollToNewItemRequested?.Invoke(this, EventArgs.Empty);
    }

    public async Task StartPing()
    {
        var box = MessageBoxManager.GetMessageBoxStandard("Invalid Input", "One or more input is invalid.",
            ButtonEnum.Ok, Icon.Error);
        if (!await IsInputValid())
        {
            await box.ShowAsync();
            return;
        }

        Reset();
        IsStopped = false;
        IsPinging = true;
        _cancellationTokenSource = new CancellationTokenSource();
        var token = _cancellationTokenSource.Token;
        await Task.Run(() => PreparePing(token), token);
        if (token.IsCancellationRequested) return;
        IsPinging = false;
    }

    private async Task PreparePing(CancellationToken cancellationToken)
    {
        PingOptions pingOptions = new()
        {
            Ttl = int.Parse(Hops),
            DontFragment = !Fragmentable
        };
        var buffer = new byte[int.Parse(Buffer)];
        try
        {
            ResolveDnsInBackground(Host, cancellationToken);
        }
        catch (Exception)
        {
            Debug.WriteLine("DNS Query Cancelled.");
            throw;
        }

        if (IsContinuous)
        {
            IsIndeterminate = true;
            while (true)
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
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
                    cancellationToken.ThrowIfCancellationRequested();
                    await SendPing(i, buffer, pingOptions, cancellationToken);
                }
                catch (OperationCanceledException ex)
                {
                    Debug.WriteLine(ex.Message);
                    break;
                }
        }

        IsStopped = true;
    }

    private async Task SendPing(int index, byte[]? buffer, PingOptions? pingOptions,
        CancellationToken cancellationToken)
    {
        try
        {
            if (SelectedInterface?.IpAddress is null) return;
            var source = IPAddress.Parse(SelectedInterface.IpAddress);
            var dest = IPAddress.Parse(Host);
            var reply = await Task.Run(() => PingEx.Send(source, dest, int.Parse(Timeout), buffer, pingOptions),
                cancellationToken);
            Dispatcher.UIThread.Invoke(() =>
            {
                if (reply.Status == IPStatus.Success) SuccessfulPings++;
                else FailedPings++;
                PingReplies?.Add(new PingReplyModel(reply, index + 1));
                OnScrollToNewItemRequested();
                ReplyTimes += reply.RoundTripTime;
                Progress++;
            });
        }
        catch (PingException)
        {
            StopPinging();
        }

        try
        {
            if (Progress < int.Parse(Attempts) || IsContinuous) await Task.Delay(int.Parse(Delay), cancellationToken);
        }
        catch (OperationCanceledException ex)
        {
            Debug.WriteLine(ex.Message);
        }
    }

    private async Task<bool> IsInputValid()
    {
        if (!IPAddress.TryParse(Host, out _))
            try
            {
                var addresses = await Dns.GetHostAddressesAsync(Host);
                if (addresses.Length > 0) Host = addresses[0].ToString();
            }
            catch
            {
                return false;
            }

        if (!int.TryParse(Attempts, out _)) return false;
        if (!int.TryParse(Hops, out _)) return false;
        if (!int.TryParse(Timeout, out _)) return false;
        if (!int.TryParse(Buffer, out _)) return false;
        if (!int.TryParse(Delay, out _)) return false;
        return true;
    }

    private async void ResolveDnsInBackground(string address, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var entry = await Dns.GetHostEntryAsync(address, cancellationToken);
            Dispatcher.UIThread.Invoke(() => { Hostname = entry.HostName; });
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
    }

    public void StopPinging()
    {
        _cancellationTokenSource?.Cancel();
        IsPinging = false;
        IsStopped = true;
        IsIndeterminate = false;
        // Progress = int.Parse(Attempts);
    }

    public void Reset()
    {
        StopPinging();
        var selected = SelectedIndex;
        NetworkInterfaces.Clear();
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
                NetworkInterfaces.Add(new InterfaceModel
                {
                    Name = networkInterface.Name,
                    Description = networkInterface.Description,
                    IpAddress = networkInterface.GetIPProperties().UnicastAddresses
                        .FirstOrDefault(ip => ip.Address.GetAddressBytes().Length == 4)
                        ?.Address.ToString(),
                    Index = networkInterface.GetIPProperties().GetIPv4Properties().Index
                });
        }

        SelectedIndex = selected;
        IsStopped = true;
        PingReplies = new ObservableCollection<PingReplyModel>();
        SuccessfulPings = 0;
        FailedPings = 0;
        PacketLoss = "0%";
        ReplyTimes = 0;
        RoundTripTime = string.Empty;
        Hostname = string.Empty;
        PingReplies.Clear();
        Progress = 0;
    }

    #region Properties

    public bool IsStopped
    {
        get => _isStopped;
        set => this.RaiseAndSetIfChanged(ref _isStopped, value);
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

    public ObservableCollection<PingReplyModel>? PingReplies
    {
        get => _pingReplies;
        set
        {
            this.RaiseAndSetIfChanged(ref _pingReplies, value);
            this.RaisePropertyChanged();
        }
    }


    public ObservableCollection<InterfaceModel> NetworkInterfaces
    {
        get => _networkInterfaces;
        set => this.RaiseAndSetIfChanged(ref _networkInterfaces, value);
    }

    private InterfaceModel? SelectedInterface => _selectedInterface;

    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            if (value < 0) return;
            this.RaiseAndSetIfChanged(ref _selectedInterface, NetworkInterfaces[value]);
            this.RaiseAndSetIfChanged(ref _selectedIndex, value);
        }
    }

    public bool IsContinuous
    {
        get => _isContinuous;
        set => this.RaiseAndSetIfChanged(ref _isContinuous, value);
    }

    public bool IsPinging
    {
        get => _isPinging;
        set => this.RaiseAndSetIfChanged(ref _isPinging, value);
    }

    public int SuccessfulPings
    {
        get => _successfulPings;
        set
        {
            this.RaiseAndSetIfChanged(ref _successfulPings, value);
            if (SuccessfulPings <= 0 || FailedPings <= 0) return;
            var average = (float)FailedPings / (FailedPings + SuccessfulPings) * 100;
            PacketLoss = $"{Math.Round(average, 2)}%";
        }
    }

    public int FailedPings
    {
        get => _failedPings;
        set
        {
            this.RaiseAndSetIfChanged(ref _failedPings, value);
            if (SuccessfulPings <= 0 && FailedPings <= 0) return;
            if (SuccessfulPings <= 0 && FailedPings > 0)
            {
                PacketLoss = "100%";
                return;
            }

            var average = (float)FailedPings / (FailedPings + SuccessfulPings) * 100;
            PacketLoss = $"{Math.Round(average, 2)}%";
        }
    }

    public string PacketLoss
    {
        get => _packetLoss;
        set => this.RaiseAndSetIfChanged(ref _packetLoss, value);
    }

    private ulong ReplyTimes
    {
        get => _replyTimes;
        set
        {
            this.RaiseAndSetIfChanged(ref _replyTimes, value);
            if (ReplyTimes <= 0 || SuccessfulPings <= 0) return;
            var average = (float)ReplyTimes / SuccessfulPings;
            RoundTripTime = $"{Math.Round(average, 2)} ms";
        }
    }

    public string RoundTripTime
    {
        get => _roundTripTime;
        set => this.RaiseAndSetIfChanged(ref _roundTripTime, value);
    }

    public string Hostname
    {
        get => _hostname;
        set => this.RaiseAndSetIfChanged(ref _hostname, value);
    }

    #endregion
}