﻿using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using NetworkToolkitModern.App.Models;
using NetworkToolkitModern.Lib.Ping;

namespace NetworkToolkitModern.App.ViewModels;

public partial class TracerouteViewModel : ViewModelBase
{
    private CancellationTokenSource? _cancellationTokenSource;
    [ObservableProperty] private int? _delay = 200;
    [ObservableProperty] private bool _doResolve;
    [ObservableProperty] private int? _hops = 30;
    [ObservableProperty] private string _host = "8.8.8.8";
    [ObservableProperty] private bool _isStarted;
    [ObservableProperty] private bool _isStopped = true;
    [ObservableProperty] private ObservableCollection<InterfaceModel> _networkInterfaces = new();
    [ObservableProperty] private int _selectedIndex;
    [ObservableProperty] private InterfaceModel? _selectedInterface;
    [ObservableProperty] private int? _timeout = 1000;

    [ObservableProperty] private ObservableCollection<TracerouteReplyModel>? _tracerouteReplyModels;

    public TracerouteViewModel()
    {
        Reset();
    }

    partial void OnSelectedIndexChanged(int value)
    {
        if (value < 0) return;
        SelectedInterface = NetworkInterfaces[value];
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
            for (var i = 1; i < Hops; i++)
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
                    var reply = await Task.Run(() => PingEx.Send(source, dest, Timeout.Value, buffer, pingOptions),
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
                await Task.Delay(Delay.Value, token);
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
                NetworkInterfaces.Add(new InterfaceModel(networkInterface));
        }

        NetworkInterfaces = new ObservableCollection<InterfaceModel>(NetworkInterfaces.OrderBy(o => o.Metric));
        SelectedIndex = selected;
        TracerouteReplyModels = new ObservableCollection<TracerouteReplyModel>();
    }
}