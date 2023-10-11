namespace SlaveApp.Models
{
    // Klasa modelu konfiguracji aplikacji
    public class AppConfig
    {
        // Ścieżka do zapisu plików
        public string SaveFolderPath { get; set; }
        // Adres IP Mastera
        public string MasterIp { get; set; }
        // Port UDP Mastera
        public int MasterUdpPort { get; set; }
    }
}


