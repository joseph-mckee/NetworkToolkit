using System.Collections.ObjectModel;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using NetworkToolkitModern.App.Models;

namespace NetworkToolkitModern.App.ViewModels;

public partial class IpConfigViewModel : ViewModelBase
{
    [ObservableProperty] private InterfaceInfoModel _currentEntry = new(NetworkInterface.GetAllNetworkInterfaces()[0]);
    [ObservableProperty] private ObservableCollection<InterfaceInfoModel> _interfaceInfoModels = new();
    [ObservableProperty] private ObservableCollection<InterfaceModel> _interfaceModels = new();
    [ObservableProperty] private bool _isFocused;

    private Timer _refreshTimer;
    [ObservableProperty] private int _selectedInterface;


    public IpConfigViewModel()
    {
        _refreshTimer = new Timer(Refresh, null, 0, 1000);
    }

    partial void OnSelectedInterfaceChanged(int value)
    {
        if (value < 0) return;
        CurrentEntry = InterfaceInfoModels[value];
    }

    private void Refresh(object? state)
    {
        Task.Run(Update);
    }

    private void Update()
    {
        var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces()
            .OrderBy(o => o.GetIPStatistics().BytesReceived).Reverse().ToList();
        var lastEntry = CurrentEntry;
        if (InterfaceModels.Count < networkInterfaces.Count)
            foreach (var networkInterface in networkInterfaces)
            {
                if (InterfaceModels.Any(x => x.Description == networkInterface.Description)) continue;
                InterfaceModels.Add(new InterfaceModel(networkInterface));
                InterfaceInfoModels.Add(new InterfaceInfoModel(networkInterface));
            }

        if (InterfaceModels.Count > networkInterfaces.Count)
        {
            if (SelectedInterface > networkInterfaces.Count) SelectedInterface = 0;
            foreach (var interfaceModel in InterfaceModels.Where(x =>
                         !networkInterfaces.Select(networkInterface => new InterfaceModel(networkInterface)).ToList()
                             .Select(y => y.Description).ToList().Contains(x.Description)).ToList())
                InterfaceModels.Remove(interfaceModel);
            foreach (var interfaceInfoModel in InterfaceInfoModels.Where(x =>
                         !networkInterfaces.Select(networkInterface => new InterfaceInfoModel(networkInterface)).ToList()
                             .Select(y => y.Description).ToList().Contains(x.Description)).ToList())
                InterfaceInfoModels.Remove(interfaceInfoModel);
            SelectedInterface = InterfaceModels.IndexOf(
                InterfaceModels.FirstOrDefault(x => x.Description == lastEntry.Description) ?? InterfaceModels[0]);
        }

        CurrentEntry =
            new InterfaceInfoModel(networkInterfaces.FirstOrDefault(x => x.Description == lastEntry.Description) ??
                                   networkInterfaces[0]);
    }
}