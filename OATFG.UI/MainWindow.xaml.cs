using System.Windows;
using OATFG.UI.ViewModels;

namespace OATFG.UI;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }
}