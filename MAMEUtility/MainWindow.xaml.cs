using MAMEUtility.ViewModels;

namespace MAMEUtility;

public partial class MainWindow
{
    public MainWindow()
    {
        InitializeComponent();

        // Set the data context
        DataContext = ServiceLocator.Instance.Resolve<MainViewModel>();
    }
}