using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using static NetworkToolkitModern.Lib.Snmp.Snmp;

namespace NetworkToolkitModern.Lib.IP;

public static class NameResolution
{
    public static async Task<string> ResolveHostnameAsync(IPAddress address, CancellationToken token)
    {
        try
        {
            var dnsTask = Dns.GetHostEntryAsync(address.ToString(), token);
            var snmpTask = GetSnmpAsync(address.ToString(), token);
            var winningTask = await Task.WhenAny(dnsTask, snmpTask, Task.Delay(1000, token));
            if (winningTask == dnsTask) return dnsTask.Result.HostName;
            if (winningTask != snmpTask) return "Unknown";
            return snmpTask.Result ?? "Unknown";
        }
        catch (SocketException e)
        {
            Debug.WriteLine($"Name resolution failed for {address}: {e.Message}");
        }
        catch (AggregateException e)
        {
            Debug.WriteLine($"Aggregate failure while resolving name for {address}.");
            e.Handle(x =>
            {
                if (x is not SocketException) return false;
                Debug.WriteLine($"Socket exception while resolving name for {address}: {x.Message}");
                return true;
            });
        }
        catch (Exception e)
        {
            Debug.WriteLine($"Unhandled Exception during name resolution for {address}: {e.Message}");
            throw;
        }

        return "Unknown";
    }
}