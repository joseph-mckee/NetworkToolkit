using System.Runtime.InteropServices;

namespace NetworkToolkitModern.Lib.Ping;

public static class Interop
{
    private static IntPtr? _icmpHandle;
    private static int? _replyStructLength;

    public static IntPtr IcmpHandle
    {
        get
        {
            _icmpHandle ??= IcmpCreateFile();
            //TODO Close Icmp Handle appropriate
            return _icmpHandle.GetValueOrDefault();
        }
    }

    public static int ReplyMarshalLength
    {
        get
        {
            _replyStructLength ??= Marshal.SizeOf(typeof(Reply));
            return _replyStructLength.GetValueOrDefault();
        }
    }


    [DllImport("Iphlpapi.dll", SetLastError = true)]
    private static extern IntPtr IcmpCreateFile();

    [DllImport("Iphlpapi.dll", SetLastError = true)]
    private static extern bool IcmpCloseHandle(IntPtr handle);

    [DllImport("Iphlpapi.dll", SetLastError = true)]
    public static extern uint IcmpSendEcho2Ex(IntPtr icmpHandle, IntPtr @event, IntPtr apcroutine, IntPtr apccontext,
        uint sourceAddress, uint destinationAddress, byte[]? requestData, short requestSize, ref Option requestOptions,
        IntPtr replyBuffer, int replySize, int timeout);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct Option
    {
        public byte Ttl;
        public readonly byte Tos;
        public byte Flags;
        public readonly byte OptionsSize;
        public readonly IntPtr OptionsData;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct Reply
    {
        public readonly uint Address;
        public readonly int Status;
        public readonly int RoundTripTime;
        public readonly short DataSize;
        public readonly short Reserved;
        public readonly IntPtr DataPtr;
        public readonly Option Options;
    }
}