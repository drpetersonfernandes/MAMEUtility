using MAMEUtility.ViewModels;

namespace MAMEUtility;

/// <inheritdoc cref="System.Windows.Window" />
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
    /// <inheritdoc />
    /// <summary>
    /// Constructor
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();

        // Set the data context
        DataContext = ServiceLocator.Instance.Resolve<MainViewModel>();
    }
}