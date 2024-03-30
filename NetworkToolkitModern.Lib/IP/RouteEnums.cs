namespace NetworkToolkitModern.Lib.IP;

public enum RouteType
{
    Other = 1,
    Invalid = 2,
    Direct = 3,
    Indirect = 4
}

public enum RouteProtocol
{
    Other = 1,
    Local = 2,
    NetMgmt = 3,
    Icmp = 4,
    Egp = 5,
    Ggp = 6,
    Hello = 7,
    Rip = 8,
    IsIs = 9,
    EsIs = 10,
    Cisco = 11,
    Bbn = 12,
    Ospf = 13,
    Bgp = 14,
    Autostatic = 10002,
    Static = 10006,
    StaticNonDod = 10007
}