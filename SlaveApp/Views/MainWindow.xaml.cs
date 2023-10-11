using System.Windows;
using SlaveApp.ViewModels;

namespace SlaveApp
{
    public partial class MainWindow : Window
    {
        // Konstruktor głównego okna
        public MainWindow()
        {
            InitializeComponent();
            // Ustawienie DataContext na nową instancję MainViewModel
            DataContext = new MainViewModel();
        }
    }
}

