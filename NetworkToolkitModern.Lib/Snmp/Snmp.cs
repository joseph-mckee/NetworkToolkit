using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Lextm.SharpSnmpLib;
using Lextm.SharpSnmpLib.Messaging;

namespace NetworkToolkitModern.Lib.Snmp;

public static class Snmp
{
    public static async Task<string?> GetSnmpAsync(string ip, CancellationToken cancellationToken)
    {
        // SNMP community name
        const string community = "public";

        // SNMP agent IP
        var target = IPAddress.Parse(ip);

        // Define your Oid
        var oid = new ObjectIdentifier("1.3.6.1.2.1.1.5.0"); // SNMPv2-MIB::sysDescr.0

        try
        {
            var result = await Messenger.GetAsync(VersionCode.V1,
                new IPEndPoint(target, 161),
                new OctetString(community),
                new List<Variable> { new(oid) },
                cancellationToken);
            return result.FirstOrDefault(x => x.Data.ToBytes().Length > 0)?.Data.ToString();
        }
        catch (OperationCanceledException)
        {
            Debug.WriteLine($"SNMP query of {ip} timed out.");
        }
        catch (SocketException e)
        {
            Debug.WriteLine($"Socket Exception: {e.Message}");
        }
        catch (Exception e)
        {
            Debug.WriteLine($"SNMP Query failed. Unhandled exception: {e}");
        }

        return null;
    }
}