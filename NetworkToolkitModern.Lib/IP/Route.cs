using System.Net.NetworkInformation;
using System.Runtime.InteropServices;

namespace NetworkToolkitModern.Lib.IP;

public static class Route
{
    [DllImport("iphlpapi.dll", ExactSpelling = true)]
    private static extern int GetIpForwardTable([Out] byte[] pIpForwardTable, ref ulong pdwSize, bool bOrder);

    public static NetworkInterface GetBestInterface()
    {
        var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces().ToList();
        return networkInterfaces.First(x =>
            x.GetIPProperties().GetIPv4Properties().Index == GetRoutes().OrderBy(y => y.Metric1).First().IfIndex);
    }

    public static int GetMetric(NetworkInterface networkInterface)
    {
        try
        {
            return GetRoutes().Where(row => row.IfIndex == networkInterface.GetIPProperties().GetIPv4Properties().Index)
                .OrderBy(x => x.Metric1).First().Metric1;
        }
        // catch (NetworkInformationException)
        // {
        //     return GetRoutes().Where(row => row.IfIndex == networkInterface.GetIPProperties().GetIPv6Properties().Index)
        //         .OrderBy(x => x.Metric1).First().Metric1;
        // }
        catch (Exception)
        {
            return 100000;
        }
    }

    public static List<RouteTableRow> GetRoutes()
    {
        ulong bufferSize = 0;
        // ReSharper disable once RedundantAssignment
        var result = GetIpForwardTable(null!, ref bufferSize, false);
        var buffer = new byte[bufferSize];
        result = GetIpForwardTable(buffer, ref bufferSize, false);

        if (result != 0) throw new Exception();

        var entriesCount = BitConverter.ToInt32(buffer, 0);
        var table = new MibIpForwardTable
        {
            dwNumEntries = (uint)entriesCount,
            table = new MibIpForwardRow[entriesCount]
        };

        var sizeOfRow = Marshal.SizeOf(typeof(MibIpForwardRow));
        var bufferPtr = Marshal.AllocHGlobal((int)bufferSize);

        var output = new List<RouteTableRow>();

        try
        {
            Marshal.Copy(buffer, 0, bufferPtr, (int)bufferSize);

            for (var i = 0; i < entriesCount; i++)
            {
                var rowPtr = IntPtr.Add(bufferPtr, sizeof(int) + i * sizeOfRow);
                table.table[i] = Marshal.PtrToStructure<MibIpForwardRow>(rowPtr);
                var row = table.table[i];
                output.Add(new RouteTableRow
                {
                    Destination = IpMath.BitsToIp(row.dwForwardDest, false),
                    Mask = IpMath.BitsToIp(row.dwForwardMask, false),
                    Policy = (int)row.dwForwardPolicy,
                    NextHop = IpMath.BitsToIp(table.table[i].dwForwardNextHop, false),
                    IfIndex = (int)row.dwForwardIfIndex,
                    Type = (RouteType)row.dwForwardType,
                    Proto = (RouteProtocol)row.dwForwardProto,
                    Age = row.dwForwardAge,
                    NextHopAs = (int)row.dwForwardNextHopAS,
                    Metric1 = (int)row.dwForwardMetric1,
                    Metric2 = (int)row.dwForwardMetric2,
                    Metric3 = (int)row.dwForwardMetric3,
                    Metric4 = (int)row.dwForwardMetric4,
                    Metric5 = (int)row.dwForwardMetric5
                });
            }
        }
        finally
        {
            Marshal.FreeHGlobal(bufferPtr);
        }

        return output;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MibIpForwardTable
    {
        public uint dwNumEntries;
        public MibIpForwardRow[] table;
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct MibIpForwardRow
    {
        public readonly uint dwForwardDest;
        public readonly uint dwForwardMask;
        public readonly uint dwForwardPolicy;
        public readonly uint dwForwardNextHop;
        public readonly uint dwForwardIfIndex;
        public readonly uint dwForwardType;
        public readonly uint dwForwardProto;
        public readonly uint dwForwardAge;
        public readonly uint dwForwardNextHopAS;
        public readonly uint dwForwardMetric1;
        public readonly uint dwForwardMetric2;
        public readonly uint dwForwardMetric3;
        public readonly uint dwForwardMetric4;
        public readonly uint dwForwardMetric5;
    }
}