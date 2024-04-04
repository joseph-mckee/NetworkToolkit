using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using NetworkToolkitModern.App.Models;
using NetworkToolkitModern.Lib.Ping;
using ReactiveUI;

namespace NetworkToolkitModern.App.ViewModels;

public class TracerouteViewModel : ViewModelBase
{
    private CancellationTokenSource? _cancellationTokenSource;
    private string _delay = "200";
    private bool _doResolve;
    private string _hops = "30";
    private string _host = "8.8.8.8";
    private bool _isStarted;
    private bool _isStopped = true;
    private ObservableCollection<InterfaceModel> _networkInterfaces = new();
    private int _selectedIndex;
    private InterfaceModel? _selectedInterface;
    private string _timeout = "1000";

    private ObservableCollection<TracerouteReplyModel>? _tracerouteReplyModels;

    public TracerouteViewModel()
    {
        Reset();
    }

    public bool IsStarted
    {
        get => _isStarted;
        set => this.RaiseAndSetIfChanged(ref _isStarted, value);
    }

    public bool IsStopped
    {
        get => _isStopped;
        set => this.RaiseAndSetIfChanged(ref _isStopped, value);
    }

    public ObservableCollection<TracerouteReplyModel>? TracerouteReplyModels
    {
        get => _tracerouteReplyModels;
        set => this.RaiseAndSetIfChanged(ref _tracerouteReplyModels, value);
    }

    public string Host
    {
        get => _host;
        set => this.RaiseAndSetIfChanged(ref _host, value);
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

    public string Delay
    {
        get => _delay;
        set => this.RaiseAndSetIfChanged(ref _delay, value);
    }

    public bool DoResolve
    {
        get => _doResolve;
        set => this.RaiseAndSetIfChanged(ref _doResolve, value);
    }

    private InterfaceModel? SelectedInterface => _selectedInterface;

    public ObservableCollection<InterfaceModel> NetworkInterfaces
    {
        get => _networkInterfaces;
        set => this.RaiseAndSetIfChanged(ref _networkInterfaces, value);
    }

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

    private async void ResolveDnsInBackground(int index, CancellationToken token)
    {
        try
        {
            var hostNameOrAddress = TracerouteReplyModels?[index].IpAddress;
            if (hostNameOrAddress == null) return;
            var entry = await Dns.GetHostEntryAsync(hostNameOrAddress, token);
            token.ThrowIfCancellationRequested();
            Dispatcher.UIThread.Invoke(() =>
            {
                if (TracerouteReplyModels != null)
                    TracerouteReplyModels[index] = new TracerouteReplyModel
                    {
                        Index = TracerouteReplyModels[index].Index,
                        IpAddress = hostNameOrAddress,
                        RoundTripTime = TracerouteReplyModels[index].RoundTripTime,
                        Status = TracerouteReplyModels[index].Status,
                        HostName = entry.HostName
                    };
            });
        }
        catch (OperationCanceledException)
        {
            Debug.WriteLine("Cancelled DNS resolve.");
        }
        catch (Exception ex)
        {
            if (token.IsCancellationRequested) return;
            Dispatcher.UIThread.Invoke(() =>
            {
                if (TracerouteReplyModels != null)
                    TracerouteReplyModels[index] = new TracerouteReplyModel
                    {
                        Index = TracerouteReplyModels[index].Index,
                        IpAddress = TracerouteReplyModels[index].IpAddress,
                        RoundTripTime = TracerouteReplyModels[index].RoundTripTime,
                        Status = TracerouteReplyModels[index].Status,
                        HostName = "Unknown"
                    };
            });
            Debug.WriteLine(ex.Message);
        }
    }

    public async Task TraceRoute()
    {
        Reset();
        IsStarted = true;
        IsStopped = false;
        if (!IPAddress.TryParse(Host, out _))
            try
            {
                var addresses = await Dns.GetHostAddressesAsync(Host);
                if (addresses.Length > 0) Host = addresses[0].ToString();
            }
            catch
            {
                Debug.WriteLine("Failed to Resolve DNS");
                return;
            }

        _cancellationTokenSource = new CancellationTokenSource();
        var token = _cancellationTokenSource.Token;
        try
        {
            for (var i = 1; i < int.Parse(Hops); i++)
            {
                token.ThrowIfCancellationRequested();
                PingOptions pingOptions = new()
                {
                    Ttl = i,
                    DontFragment = true
                };
                var buffer = new byte[32];
                if (Host is not null)
                {
                    var source = IPAddress.Parse(SelectedInterface?.IpAddress ?? throw new InvalidOperationException());
                    var dest = IPAddress.Parse(Host);
                    var reply = await Task.Run(() => PingEx.Send(source, dest, int.Parse(Timeout), buffer, pingOptions),
                        token);
                    token.ThrowIfCancellationRequested();
                    if (reply.Status is IPStatus.Success or IPStatus.TtlExpired)
                    {
                        if (DoResolve)
                            try
                            {
                                TracerouteReplyModels?.Add(new TracerouteReplyModel
                                {
                                    Index = i,
                                    IpAddress = reply.IpAddress.ToString(),
                                    RoundTripTime = reply.RoundTripTime.ToString(),
                                    Status = reply.Status.ToString()
                                });
                                ResolveDnsInBackground(i - 1, token);
                            }
                            catch (SocketException e)
                            {
                                Debug.WriteLine(e.Message);
                            }
                        else
                            TracerouteReplyModels?.Add(new TracerouteReplyModel
                            {
                                Index = i,
                                IpAddress = reply.IpAddress.ToString(),
                                RoundTripTime = reply.RoundTripTime.ToString(),
                                Status = reply.Status.ToString(),
                                HostName = "Unknown"
                            });

                        if (reply.Status == IPStatus.Success)
                            break;
                    }
                    else
                    {
                        TracerouteReplyModels?.Add(new TracerouteReplyModel
                        {
                            Index = i,
                            IpAddress = reply.IpAddress.ToString(),
                            RoundTripTime = "N/A",
                            Status = reply.Status.ToString(),
                            HostName = "N/A"
                        });
                    }
                }

                // Handle empty field
                await Task.Delay(int.Parse(Delay), token);
            }
        }
        catch (OperationCanceledException)
        {
            Debug.WriteLine("Traceroute operation cancelled.");
        }
        finally
        {
            IsStarted = false;
            IsStopped = true;
        }
    }

    public void Stop()
    {
        _cancellationTokenSource?.Cancel();
        IsStarted = false;
        IsStopped = true;
    }

    public void Reset()
    {
        Stop();
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
        TracerouteReplyModels = new ObservableCollection<TracerouteReplyModel>();
    }
}