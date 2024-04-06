using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using DynamicData;
using NetworkToolkitModern.App.Models;
using NetworkToolkitModern.Lib.IP;
using ReactiveUI;

namespace NetworkToolkitModern.App.ViewModels;

public class IpConfigViewModel : ViewModelBase
{
    private InterfaceInfoModel _currentEntry = new(NetworkInterface.GetAllNetworkInterfaces()[0]);
    private ObservableCollection<InterfaceInfoModel> _interfaceInfoModels = new();
    private ObservableCollection<InterfaceModel> _interfaceModels = new();
    private bool _isFocused;
    private int _selectedInterface;

    public IpConfigViewModel()
    {
        Task.Run(Refresh);
    }


    public bool IsFocused
    {
        get => _isFocused;
        set => this.RaiseAndSetIfChanged(ref _isFocused, value);
    }

    public ObservableCollection<InterfaceModel> InterfaceModels
    {
        get => _interfaceModels;
        set => this.RaiseAndSetIfChanged(ref _interfaceModels, value);
    }


    private ObservableCollection<InterfaceInfoModel> InterfaceInfoModels
    {
        get => _interfaceInfoModels;
        set => this.RaiseAndSetIfChanged(ref _interfaceInfoModels, value);
    }

    public int SelectedInterface
    {
        get => _selectedInterface;
        set
        {
            if (value < 0) return;
            this.RaiseAndSetIfChanged(ref _selectedInterface, value);
            CurrentEntry = InterfaceInfoModels[value];
        }
    }

    public InterfaceInfoModel CurrentEntry
    {
        get => _currentEntry;
        private set => this.RaiseAndSetIfChanged(ref _currentEntry, value);
    }

    private void Refresh()
    {
        try
        {
            while (true)
            {
                // if (!IsFocused) continue;
                Update();
                Thread.Sleep(1000);
            }
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
            throw;
        }
    }

    private void Update()
    {
        var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces().OrderBy(o => o.GetIPStatistics().BytesReceived).Reverse().ToList();
        
        if (InterfaceModels.Count < networkInterfaces.Count)
        {
            foreach (var networkInterface in networkInterfaces)
            {
                if (InterfaceModels.Any(x => x.Description == networkInterface.Description)) continue;
                InterfaceModels.Add(new InterfaceModel(networkInterface));
                InterfaceInfoModels.Add(new InterfaceInfoModel(networkInterface));
            }
        }

        if (InterfaceModels.Count > networkInterfaces.Count)
        {
            if (SelectedInterface > networkInterfaces.Count) SelectedInterface = 0;
            InterfaceModels.Remove(InterfaceModels.Where(x =>
                !networkInterfaces.Select(networkInterface => new InterfaceModel(networkInterface)).ToList()
                    .Select(y => y.Description).ToList().Contains(x.Description)).ToList());
            InterfaceInfoModels.Remove(InterfaceInfoModels.Where(x =>
                !networkInterfaces.Select(networkInterface => new InterfaceInfoModel(networkInterface)).ToList()
                    .Select(y => y.Description).ToList().Contains(x.Description)).ToList());
            SelectedInterface = InterfaceModels.IndexOf(
                InterfaceModels.FirstOrDefault(x => x.Description == CurrentEntry.Description) ?? InterfaceModels[0]);
        }

        CurrentEntry = new InterfaceInfoModel(networkInterfaces.First(x => x.Description == CurrentEntry.Description));
    }
}