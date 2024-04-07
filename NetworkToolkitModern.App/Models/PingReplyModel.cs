using System.Net.NetworkInformation;
using NetworkToolkitModern.Lib.Ping;

namespace NetworkToolkitModern.App.Models;

public class PingReplyModel
{
    public PingReplyModel(PingReplyEx reply, int index)
    {
        Index = index;
        IpAddress = reply.IpAddress.ToString();
        RoundtripTime = reply.RoundTripTime;
        Status = reply.Status;
    }

    public int Index { get; }
    public string? IpAddress { get; }
    public long RoundtripTime { get; }

    public IPStatus Status { get; }
}