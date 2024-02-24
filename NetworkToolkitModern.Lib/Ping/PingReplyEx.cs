using System.ComponentModel;
using System.Net;
using System.Net.NetworkInformation;

namespace NetworkToolkitModern.Lib.Ping;

[Serializable]
public class PingReplyEx
{
    private Win32Exception? _exception;

    internal PingReplyEx(uint nativeCode, int replyStatus, IPAddress ipAddress)
    {
        NativeCode = nativeCode;
        IpAddress = ipAddress;
        if (Enum.IsDefined(typeof(IPStatus), replyStatus))
            Status = (IPStatus)replyStatus;
    }

    internal PingReplyEx(uint nativeCode, int replyStatus, IPAddress ipAddress, int roundTripTime, byte[]? buffer)
    {
        NativeCode = nativeCode;
        IpAddress = ipAddress;
        RoundTripTime = (uint)roundTripTime;
        Buffer = buffer;
        if (Enum.IsDefined(typeof(IPStatus), replyStatus))
            Status = (IPStatus)replyStatus;
    }


    /// <summary>Native result from <code>IcmpSendEcho2Ex</code>.</summary>
    public uint NativeCode { get; }

    public IPStatus Status { get; } = IPStatus.Unknown;

    /// <summary>The source address of the reply.</summary>
    public IPAddress IpAddress { get; }

    public byte[]? Buffer { get; }

    public uint RoundTripTime { get; }

    /// <summary>Resolves the <code>Win32Exception</code> from native code</summary>
    public Win32Exception? Exception => Status != IPStatus.Success ? _exception : null;

    public override string ToString()
    {
        if (Status == IPStatus.Success)
            return Status + " from " + IpAddress + " in " + RoundTripTime + " ms with " + Buffer!.Length + " bytes";
        if (Status != IPStatus.Unknown)
            return Status + " from " + IpAddress;
        return Exception?.Message + " from " + IpAddress;
    }
}