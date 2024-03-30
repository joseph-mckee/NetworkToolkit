using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using NetworkToolkitModern.App.Models;
using ReactiveUI;

namespace NetworkToolkitModern.App.ViewModels;

public class IpConfigViewModel : ViewModelBase
{
    private InterfaceInfoModel _currentEntry = new(NetworkInterface.GetAllNetworkInterfaces()[0]);
    private ObservableCollection<InterfaceInfoModel> _interfaceInfoModels = new();
    private ObservableCollection<InterfaceModel> _interfaceModels = new();
    private NetworkInterface[] _networkInterfaces = Array.Empty<NetworkInterface>();
    private int _selectedInterface;
    private bool _isFocused;

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
            Console.WriteLine(e);
            throw;
        }
    }

    private void Update()
    {
        List<InterfaceModel> interfaceModelsList = new();
        List<InterfaceInfoModel> interfaceInfoModels = new();
        if (_networkInterfaces.Length != NetworkInterface.GetAllNetworkInterfaces().Length)
        {
            _networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
            interfaceModelsList.AddRange(_networkInterfaces.Select(networkInterface => new InterfaceModel
            {
                Name = networkInterface.Name,
                Index = networkInterface.GetIPProperties().GetIPv6Properties().Index
            }));
            InterfaceModels = new ObservableCollection<InterfaceModel>(interfaceModelsList.OrderBy(o => o.Index));
            interfaceModelsList.Clear();
        }

        _networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
        interfaceInfoModels.AddRange(_networkInterfaces.Select(networkInterface =>
            new InterfaceInfoModel(networkInterface)));
        InterfaceInfoModels = new ObservableCollection<InterfaceInfoModel>(interfaceInfoModels.OrderBy(o => o.Index));
        try
        {
            CurrentEntry = InterfaceInfoModels[SelectedInterface];
        }
        catch
        {
            SelectedInterface = 0;
            CurrentEntry = InterfaceInfoModels[SelectedInterface];
        }

        interfaceInfoModels.Clear();
        GC.Collect();
    }
}