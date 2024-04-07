using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using NetworkToolkitModern.App.Models;
using NetworkToolkitModern.Lib.Ping;

namespace NetworkToolkitModern.App.ViewModels;

public partial class PingViewModel : ViewModelBase
{
    [ObservableProperty] private string _attempts = "4";
    [ObservableProperty] private string _buffer = "32";
    private CancellationTokenSource? _cancellationTokenSource;
    [ObservableProperty] private string _delay = "200";
    [ObservableProperty] private int _failedPings;
    [ObservableProperty] private bool _fragmentable;
    [ObservableProperty] private string _hops = "30";
    [ObservableProperty] private string _host = "8.8.8.8";
    [ObservableProperty] private string _hostname = string.Empty;
    [ObservableProperty] private bool _isContinuous;
    [ObservableProperty] private bool _isIndeterminate;
    [ObservableProperty] private bool _isPinging;
    [ObservableProperty] private bool _isStopped;
    [ObservableProperty] private ObservableCollection<InterfaceModel> _networkInterfaces = new();
    [ObservableProperty] private string _packetLoss = "0%";
    [ObservableProperty] private ObservableCollection<PingReplyModel>? _pingReplies;
    [ObservableProperty] private int _progress;

    [ObservableProperty] private ulong _replyTimes;
    [ObservableProperty] private string _roundTripTime = string.Empty;
    [ObservableProperty] private int _selectedIndex;
    [ObservableProperty] private InterfaceModel? _selectedInterface;
    [ObservableProperty] private int _successfulPings;
    [ObservableProperty] private string _timeout = "1000";

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
                NetworkInterfaces.Add(new InterfaceModel(networkInterface));
        }

        NetworkInterfaces = new ObservableCollection<InterfaceModel>(NetworkInterfaces.OrderBy(o => o.Metric));
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

    partial void OnSelectedIndexChanged(int value)
    {
        if (value < 0) return;
        SelectedInterface = NetworkInterfaces[value];
    }

    partial void OnSuccessfulPingsChanged(int value)
    {
        if (SuccessfulPings <= 0 || FailedPings <= 0) return;
        var average = (float)FailedPings / (FailedPings + SuccessfulPings) * 100;
        PacketLoss = $"{Math.Round(average, 2)}%";
    }

    partial void OnFailedPingsChanged(int value)
    {
        if (SuccessfulPings <= 0 && FailedPings <= 0) return;
        if (SuccessfulPings <= 0 && FailedPings > 0)
        {
            PacketLoss = "100%";
            return;
        }

        var average = (float)FailedPings / (FailedPings + SuccessfulPings) * 100;
        PacketLoss = $"{Math.Round(average, 2)}%";
    }

    partial void OnReplyTimesChanged(ulong value)
    {
        if (ReplyTimes <= 0 || SuccessfulPings <= 0) return;
        var average = (float)ReplyTimes / SuccessfulPings;
        RoundTripTime = $"{Math.Round(average, 2)} ms";
    }

    #endregion
}