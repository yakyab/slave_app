using CommonModels;
using MessagePack;
using System;
using System.IO;
using System.Threading.Tasks;

namespace SlaveApp.Services
{
    public class FileSyncService
    {
        private string _saveFolderPath;
        private readonly UdpCommunicationService _udpService;
        private bool _isSyncing;

        // Konstruktor przyjmujący ścieżkę zapisu i serwis komunikacji UDP
        public FileSyncService(string saveFolderPath, UdpCommunicationService udpService)
        {
            _saveFolderPath = saveFolderPath;
            _udpService = udpService;
        }

        // Metoda rozpoczynająca nasłuchiwanie na dane
        public void StartListening(string saveFolderPath)
        {
            _saveFolderPath = saveFolderPath;
            _isSyncing = true;
            // Uruchamianie asynchroniczne nasłuchiwania na dane
            Task.Run(() =>
            {
                while (_isSyncing)
                {
                    var receivedData = _udpService.ReceiveData();
                    ProcessReceivedData(receivedData);
                }
            });
        }

        // Metoda zatrzymująca nasłuchiwanie
        public void StopListening()
        {
            _isSyncing = false;
        }

        // Metoda przetwarzająca otrzymane dane
        private void ProcessReceivedData(byte[] data)
        {
            try
            {
                var fileEvent = DeserializeFileEvent(data);
                if (fileEvent.Type == EventType.Created)
                {
                    SaveReceivedFile(fileEvent);
                }
                else if (fileEvent.Type == EventType.Deleted)
                {
                    DeleteReceivedFile(fileEvent);
                }
            }
            catch (Exception)
            {
                // Obsługa wyjątków (można dodać logowanie błędów)
            }
        }

        // Metoda deserializująca dane otrzymane w formacie MessagePack
        private FileEvent DeserializeFileEvent(byte[] data)
        {
            var fileEvent = MessagePackSerializer.Deserialize<FileEvent>(data);
            return fileEvent;
        }

        // Metoda zapisująca otrzymane dane jako plik
        private void SaveReceivedFile(FileEvent fileEvent)
        {
            var filePath = Path.Combine(_saveFolderPath, fileEvent.FileName);
            if (fileEvent.IsDirectory)
            {
                Directory.CreateDirectory(filePath);
            }
            else
            {
                File.WriteAllBytes(filePath, fileEvent.FileContent);
            }
        }

        // Metoda usuwająca otrzymane dane (plik/katalog)
        private void DeleteReceivedFile(FileEvent fileEvent)
        {
            var filePath = Path.Combine(_saveFolderPath, fileEvent.FileName);
            if (fileEvent.IsDirectory && Directory.Exists(filePath))
            {
                Directory.Delete(filePath);
            }
            else if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }
}



