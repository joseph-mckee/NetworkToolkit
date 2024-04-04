using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Lextm.SharpSnmpLib;
using Lextm.SharpSnmpLib.Messaging;
using SnmpSharpNet;
using OctetString = Lextm.SharpSnmpLib.OctetString;

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

    public static void SnmpWalk()
    {
        // SNMP community name
            var community = new SnmpSharpNet.OctetString("public");
 
            // Define agent parameters class
            var param = new AgentParameters(community)
            {
                // Set SNMP version to 2 (GET-BULK only works with SNMP ver 2 and 3)
                Version = SnmpVersion.Ver2
            };
            // Construct the agent address object
            // IpAddress class is easy to use here because
            //  it will try to resolve constructor parameter if it doesn't
            //  parse to an IP address
            var agent = new IpAddress("192.168.1.1");
 
            // Construct target
            var target = new UdpTarget((IPAddress)agent, 161, 2000, 1);
 
            // Define Oid that is the root of the MIB
            //  tree you wish to retrieve
            var rootOid = new Oid("1.3.6"); // ifDescr
 
            // This Oid represents last Oid returned by
            //  the SNMP agent
            var lastOid = (Oid)rootOid.Clone();
 
            // Pdu class used for all requests
            var pdu = new Pdu(PduType.GetBulk)
            {
                // In this example, set NonRepeaters value to 0
                NonRepeaters = 0,
                // MaxRepetitions tells the agent how many Oid/Value pairs to return
                // in the response.
                MaxRepetitions = 5000
            };

            // Loop through results
            while (lastOid != null)
            {
                // When Pdu class is first constructed, RequestId is set to 0
                // and during encoding id will be set to the random value
                // for subsequent requests, id will be set to a value that
                // needs to be incremented to have unique request ids for each
                // packet
                if (pdu.RequestId != 0)
                {
                    pdu.RequestId += 1;
                }
                // Clear Oids from the Pdu class.
                pdu.VbList.Clear();
                // Initialize request PDU with the last retrieved Oid
                pdu.VbList.Add(lastOid);
                // Make SNMP request
                var result = (SnmpV2Packet)target.Request(pdu, param);
                // You should catch exceptions in the Request if using in real application.
 
                // If result is null then agent didn't reply or we couldn't parse the reply.
                if (result != null)
                {
                    // ErrorStatus other then 0 is an error returned by 
                    // the Agent - see SnmpConstants for error definitions
                    if (result.Pdu.ErrorStatus != 0)
                    {
                        // agent reported an error with the request
                        Debug.WriteLine("Error in SNMP reply. Error {0} index {1}", 
                            result.Pdu.ErrorStatus,
                            result.Pdu.ErrorIndex);
                        lastOid = null;
                        break;
                    }
                    else
                    {
                        // Walk through returned variable bindings
                        foreach (Vb v in result.Pdu.VbList)
                        {
                            // Check that retrieved Oid is "child" of the root OID
                            if (rootOid.IsRootOf(v.Oid))
                            {
                                Debug.WriteLine("{0} ({1}): {2}", v.Oid, SnmpConstants.GetTypeName(v.Value.Type), v.Value);
                                if (v.Value.Type == SnmpConstants.SMI_ENDOFMIBVIEW)
                                    lastOid = null;
                                else
                                    lastOid = v.Oid;
                            }
                            else
                            {
                                // we have reached the end of the requested
                                // MIB tree. Set lastOid to null and exit loop
                                lastOid = null;
                            }
                        }
                    }
                }
                else
                {
                    Debug.WriteLine("No response received from SNMP agent.");
                }
            }
            target.Close();
    }
    
}