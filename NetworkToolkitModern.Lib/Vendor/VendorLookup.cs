using System.Globalization;
using System.Reflection;
using CsvHelper;

namespace NetworkToolkitModern.Lib.Vendor;

public class VendorLookup
{
    private readonly List<VendorInfo>? _vendors;

    public VendorLookup()
    {
        var assembly = Assembly.GetExecutingAssembly();
        const string resourceName = "NetworkToolkitModern.Lib.Vendor.MACList.oui.csv";
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null) return;
        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        var records = csv.GetRecords<VendorInfo>();
        _vendors = records.ToList();
    }

    private List<VendorInfo>? GetVendorByMac()
    {
        return _vendors;
    }

    public string GetVendorName(string macAddress)
    {
        var uniqueIdentifiers = macAddress.Split(':');
        if (uniqueIdentifiers.Length < 3) return "Unknown";
        var uniqueIdentifier = $"{uniqueIdentifiers[0]}{uniqueIdentifiers[1]}{uniqueIdentifiers[2]}";
        var data = GetVendorByMac();
        if (data == null) return "Unknown";
        var found = data.FirstOrDefault(x => x.Assignment == uniqueIdentifier.ToUpper());
        return found != null ? found.OrganizationName : "Unknown";
    }
}