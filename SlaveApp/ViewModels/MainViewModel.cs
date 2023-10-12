using SlaveApp.Helpers;
using SlaveApp.Models;
using SlaveApp.Services;
using System;
using System.ComponentModel;
using System.Windows.Input;
using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Net.NetworkInformation;

namespace SlaveApp.ViewModels
{
    // Główny model widoku dla aplikacji
    public class MainViewModel : INotifyPropertyChanged
    {
        // Prywatne pola przechowujące instancje serwisów i dane konfiguracyjne
        private readonly UdpCommunicationService _udpService;
        private readonly FileSyncService _fileSyncService;
        private string _saveFolderPath;
        private string _masterIp;
        private int _masterUdpPort;
        private bool _isSyncing;

        // Właściwość zwracająca aktualną ścieżkę zapisu
        public string CurrentSavePath => $"Save Path: {_saveFolderPath}";


        // Właściwość odpowiadająca za IP Mastera z walidacją
        public string MasterIp
        {
            get => _masterIp;
            set
            {
                // Sprawdzenie, czy wprowadzone IP jest prawidłowe
                if (IsValidIpAddress(value))
                {
                    _masterIp = value;
                    OnPropertyChanged(nameof(MasterIp));
                    (StartCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
                else
                {
                    // Wyświetlenie komunikatu o błędzie, gdy IP jest nieprawidłowe
                    MessageBox.Show("Invalid input. Please enter a valid IP address.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // Metoda sprawdzająca poprawność adresu IP
        private bool IsValidIpAddress(string ip)
        {
            return System.Net.IPAddress.TryParse(ip, out _);
        }

        // Właściwość odpowiadająca za port UDP Mastera z walidacją
        public string MasterUdpPort
        {
            get => _masterUdpPort.ToString();
            set
            {
                // Sprawdzenie, czy wprowadzony port jest liczbą i czy różni się od obecnego
                if (int.TryParse(value, out int parsedValue) && _masterUdpPort != parsedValue)
                {
                    _masterUdpPort = parsedValue;
                    OnPropertyChanged(nameof(MasterUdpPort));
                    (StartCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
                else
                {
                    // Wyświetlenie komunikatu o błędzie, gdy port jest nieprawidłowy
                    MessageBox.Show("Invalid input. Please enter a valid port number.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // Właściwości określające, czy pola IP i portu są edytowalne
        public bool IsIpEditable => !_isSyncing;
        public bool IsPortEditable => !_isSyncing;

        // Komendy używane w interfejsie użytkownika
        public ICommand StartCommand { get; }
        public ICommand StopCommand { get; }
        public ICommand ChooseFolderCommand { get; }

        // Zdarzenie informujące o zmianie właściwości
        public event PropertyChangedEventHandler PropertyChanged;

        // Konstruktor modelu widoku
        public MainViewModel()
        {
            // Inicjalizacja serwisów i komend
            _udpService = new UdpCommunicationService();
            _fileSyncService = new FileSyncService(_saveFolderPath, _udpService);
            StartCommand = new RelayCommand(StartSync, CanStartSync);
            StopCommand = new RelayCommand(StopSync, CanStopSync);
            ChooseFolderCommand = new RelayCommand(ChooseFolder, CanChooseFolder);
            LoadConfig();

            // Ustawienie domyślnego IP dla pierwszego uruchomienia bez configu
            if (string.IsNullOrEmpty(_masterIp))
            {
                _masterIp = "0.0.0.0";
                OnPropertyChanged(nameof(MasterIp));
            }
        }

        // Destruktor modelu widoku
        ~MainViewModel()
        {
            StopSync();
        }

        // Metoda rozpoczynająca synchronizację
        private void StartSync()
        {
            try
            {
                // Sprawdzenie, czy można rozpocząć synchronizację
                if (!CanStartSync())
                {
                    MessageBox.Show("Cannot start syncing due to invalid input or port numbers.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Inicjalizacja procesu synchronizacji
                _isSyncing = true;
                _udpService.Initialize(_masterIp, _masterUdpPort);
                _udpService.StartSendingAliveSignal();
                _fileSyncService.StartListening(_saveFolderPath);
                OnPropertyChanged(nameof(IsIpEditable));
                OnPropertyChanged(nameof(IsPortEditable));
                SaveConfig();

            }
            catch (Exception ex)
            {
                // Obsługa wyjątków
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Metoda sprawdzająca, czy można rozpocząć synchronizację
        private bool CanStartSync()
        {
            // Warunki, które muszą być spełnione, aby rozpocząć synchronizację
            if (_isSyncing ||
                string.IsNullOrEmpty(_masterIp) ||
                !IsValidIpAddress(_masterIp) ||
                _masterUdpPort <= 0)
            {
                return false;
            }
            if (!IsPortAvailable(_masterUdpPort))
            {
                return false;
            }
            return true;
        }

        // Metoda sprawdzająca dostępność portu
        private bool IsPortAvailable(int port)
        {
            bool isAvailable = true;
            IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
            TcpConnectionInformation[] tcpConnInfoArray = ipGlobalProperties.GetActiveTcpConnections();
            foreach (TcpConnectionInformation tcpi in tcpConnInfoArray)
            {
                if (tcpi.LocalEndPoint.Port == port)
                {
                    isAvailable = false;
                    break;
                }
            }
            return isAvailable;
        }

        // Metoda zatrzymująca synchronizację
        private void StopSync()
        {
            try
            {
                // Zatrzymanie procesu synchronizacji
                _isSyncing = false;
                _udpService.StopSendingAliveSignal();
                _udpService.Dispose();
                _fileSyncService.StopListening();
                OnPropertyChanged(nameof(IsIpEditable));
                OnPropertyChanged(nameof(IsPortEditable));
            }
            catch (Exception ex)
            {
                // Obsługa wyjątków
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Metoda sprawdzająca, czy można zatrzymać synchronizację
        private bool CanStopSync() => _isSyncing;

        // Metoda pozwalająca wybrać folder
        private void ChooseFolder()
        {
            try
            {
                // Wybór folderu przez użytkownika
                var dialog = new OpenFileDialog
                {
                    ValidateNames = false,
                    CheckFileExists = false,
                    CheckPathExists = true,
                    FileName = "Folder Selection.",
                    Title = "Select a folder to track"
                };
                if (dialog.ShowDialog() == true)
                {
                    _saveFolderPath = Path.GetDirectoryName(dialog.FileName);
                    OnPropertyChanged(nameof(CurrentSavePath));
                }
            }
            catch (Exception ex)
            {
                // Obsługa wyjątków
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Metoda sprawdzająca, czy można wybrać folder
        private bool CanChooseFolder() => !_isSyncing;

        // Metoda zgłaszająca zmianę właściwości
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Metoda wczytująca konfigurację
        private void LoadConfig()
        {
            try
            {
                // Wczytanie konfiguracji z pliku XML
                var config = XmlConfigHelper.LoadConfig<AppConfig>("AppConfig.xml");
                _saveFolderPath = config.SaveFolderPath;
                _masterIp = config.MasterIp;
                _masterUdpPort = config.MasterUdpPort;
            }
            catch (Exception ex)
            {
                // Obsługa wyjątków
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                SetDefaultConfig();
            }
        }

        // Metoda zapisująca konfigurację
        private void SaveConfig()
        {
            try
            {
                // Zapis konfiguracji do pliku XML
                var config = new AppConfig
                {
                    SaveFolderPath = _saveFolderPath,
                    MasterIp = _masterIp,
                    MasterUdpPort = _masterUdpPort,
                };
                XmlConfigHelper.SaveConfig(config, "AppConfig.xml");
            }
            catch (Exception ex)
            {
                // Obsługa wyjątków
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Metoda ustawiająca domyślną konfigurację
        private void SetDefaultConfig()
        {
            _saveFolderPath = "default_path";
            _masterIp = "0.0.0.0";
            _masterUdpPort = 1;
        }

    }

    // Klasa reprezentująca komendę
    public class RelayCommand : ICommand
    {
        // Prywatne pola przechowujące delegaty do metod
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        // Konstruktor przyjmujący delegaty do metod
        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            // Przypisanie delegatów do pól
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        // Zdarzenie informujące o zmianie możliwości wykonania komendy
        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        // Metoda wywołująca zdarzenie CanExecuteChanged
        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }

        // Metoda sprawdzająca, czy komenda może być wykonana
        public bool CanExecute(object parameter) => _canExecute == null || _canExecute();

        // Metoda wykonująca komendę
        public void Execute(object parameter) => _execute();
    }
}





