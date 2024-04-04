using System.Collections.ObjectModel;

namespace NetworkToolkitModern.App.Models;

public class OidNodeModel
{
    public ObservableCollection<OidNodeModel>? Children { get; }
    public string Name { get; }
    public string Oid { get; }

    public OidNodeModel(string name, string oid)
    {
        Name = name;
        Oid = oid;
    }

    public OidNodeModel(string name, string oid, ObservableCollection<OidNodeModel> children)
    {
        Name = name;
        Oid = oid;
        Children = children;
    }
}