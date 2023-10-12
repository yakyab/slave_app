using System.Windows;
using SlaveApp.ViewModels;

namespace SlaveApp
{
    public partial class MainWindow : Window
    {
        // Konstruktor głównego okna
        public MainWindow()
        {
            InitializeComponent();  // Inicjalizacja komponentów interfejsu użytkownika zdefiniowanych w XAML
            DataContext = new MainViewModel();  // Ustawienie kontekstu danych na nową instancję MainViewModel
        }
    }
}

