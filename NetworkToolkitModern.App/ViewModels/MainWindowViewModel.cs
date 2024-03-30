namespace NetworkToolkitModern.App.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    public MainWindowViewModel(ScanViewModel scanViewModel, PingViewModel pingViewModel,
        TracerouteViewModel tracerouteViewModel, IpConfigViewModel ipConfigViewModel)
    {
        ScanViewModel = scanViewModel;
        PingViewModel = pingViewModel;
        TracerouteViewModel = tracerouteViewModel;
        IpConfigViewModel = ipConfigViewModel;
    }

    public MainWindowViewModel()
    {
        ScanViewModel = new ScanViewModel();
        PingViewModel = new PingViewModel();
        TracerouteViewModel = new TracerouteViewModel();
        IpConfigViewModel = new IpConfigViewModel();
    }

    public PingViewModel PingViewModel { get; set; }
    public TracerouteViewModel TracerouteViewModel { get; set; }
    public ScanViewModel ScanViewModel { get; set; }
    public IpConfigViewModel IpConfigViewModel { get; set; }
}