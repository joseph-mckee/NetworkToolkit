using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace NetworkToolkitModern.Lib.Tcp;

public static class Tcp
{
    public static async Task<bool> ScanHostAsync(IPAddress target, int timeout, CancellationToken token, int port = 80)
    {
        token.ThrowIfCancellationRequested();
        using var tcpClient = new TcpClient();
        tcpClient.SendTimeout = timeout;
        try
        {
            await tcpClient.ConnectAsync(target.ToString(), port, token);
            // If the operation is canceled, ConnectAsync will throw OperationCanceledException.
            return tcpClient.Connected;
        }
        catch (SocketException e)
        {
            Debug.WriteLine($"TCP Response: {e.SocketErrorCode} for {target}");
            // Depending on your application logic, you may treat these specific socket errors as non-fatal.
            switch (e.SocketErrorCode)
            {
                case SocketError.ConnectionRefused:
                case SocketError.ConnectionReset:
                case SocketError.HostDown:
                case SocketError.HostUnreachable:
                    return !token.IsCancellationRequested;
                default:
                    return false;
            }
        }
        catch (OperationCanceledException)
        {
            // Cancellation has been requested; handle accordingly.
            Debug.WriteLine($"TCP Scan canceled for {target}");
            return false;
        }
        catch (Exception e)
        {
            Debug.WriteLine($"Unhandled Exception while performing TCP scan on {target}: {e.Message}");
            throw;
        }
    }
}