using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using NetworkToolkitModern.Lib.IP;

namespace NetworkToolkitModern.Lib.Ping;

public static class PingEx
{
    public static PingReplyEx Send(IPAddress srcAddress, IPAddress destAddress, int timeout = 5000,
        byte[]? buffer = null, PingOptions? po = null)
    {
        if (destAddress is not { AddressFamily: AddressFamily.InterNetwork } ||
            destAddress.Equals(IPAddress.Any))
            throw new ArgumentException();

        //Defining pinvoke args
        var source = IPMath.IPToBits(srcAddress, false);
        var destination = IPMath.IPToBits(destAddress, false);
        var sendBuffer = buffer;
        var options = new Interop.Option
        {
            Ttl = po == null ? (byte)255 : (byte)po.Ttl,
            Flags = po == null ? (byte)0 : po.DontFragment ? (byte)0x02 : (byte)0 //0x02
        };
        var fullReplyBufferSize =
            Interop.ReplyMarshalLength + sendBuffer!.Length; //Size of Reply struct and the transmitted buffer length.


        var allocSpace =
            Marshal.AllocHGlobal(
                fullReplyBufferSize); // unmanaged allocation of reply size. TODO Maybe should be allocated on stack
        try
        {
            var nativeCode = Interop.IcmpSendEcho2Ex(
                Interop.IcmpHandle, //_In_      HANDLE IcmpHandle,
                default, //_In_opt_  HANDLE Event,
                default, //_In_opt_  PIO_APC_ROUTINE ApcRoutine,
                default, //_In_opt_  PVOID ApcContext
                source, //_In_      IPAddr SourceAddress,
                destination, //_In_      IPAddr DestinationAddress,
                sendBuffer, //_In_      LPVOID RequestData,
                (short)sendBuffer.Length, //_In_      WORD RequestSize,
                ref options, //_In_opt_  PIP_OPTION_INFORMATION RequestOptions,
                allocSpace, //_Out_     LPVOID ReplyBuffer,
                fullReplyBufferSize, //_In_      DWORD ReplySize,
                timeout //_In_      DWORD Timeout
            );
            var reply = (Interop.Reply)Marshal.PtrToStructure(allocSpace,
                typeof(Interop.Reply))!; // Parse the beginning of reply memory to reply struct

            byte[]? replyBuffer = null;
            if (sendBuffer.Length != 0)
            {
                replyBuffer = new byte[sendBuffer.Length];
                Marshal.Copy(allocSpace + Interop.ReplyMarshalLength, replyBuffer, 0,
                    sendBuffer.Length); //copy the rest of the reply memory to managed byte[]
            }

            if (nativeCode == 0) //Means that native method is faulted.
                return new PingReplyEx(nativeCode, reply.Status, new IPAddress(reply.Address));
            else
                return new PingReplyEx(nativeCode, reply.Status, new IPAddress(reply.Address), reply.RoundTripTime,
                    replyBuffer);
        }
        finally
        {
            Marshal.FreeHGlobal(allocSpace); //free allocated space
        }
    }
}