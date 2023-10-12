using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SlaveApp.Services
{
    /// <summary>
    /// Serwis odpowiedzialny za komunikację UDP między aplikacjami slave i master.
    /// </summary>
    public class UdpCommunicationService : IDisposable
    {
        private UdpClient _udpClient;
        private IPEndPoint _masterEndPoint;
        private bool _isSendingAliveSignal;

        // Metoda inicjalizująca serwis komunikacji.
        public void Initialize(string masterIp, int masterUdpPort)
        {
            _udpClient = new UdpClient();
            _masterEndPoint = new IPEndPoint(IPAddress.Parse(masterIp), masterUdpPort);
            IPEndPoint localEP = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 0);
            _udpClient.Client.Bind(localEP);
        }

        /// <summary>
        /// Odbiera dane od klienta UDP, sprawdza sumę kontrolną i zwraca dane użytkowe.
        /// </summary>
        /// <returns>Dane użytkowe, jeśli suma kontrolna jest prawidłowa; w przeciwnym razie null.</returns>
        public byte[] ReceiveData()
        {
            try
            {
                IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = _udpClient.Receive(ref remoteEP);

                // Sprawdzenie, czy ramka danych jest prawidłowa.
                if (data.Length > 4 && data[0] == 0xA0 && data[1] == 0xA1)
                {
                    ushort receivedChecksum = (ushort)((data[^2] << 8) | data[^1]);
                    ushort calculatedChecksum = CalculateChecksum(data, data.Length - 2);

                    // Sprawdzenie, czy obliczona suma kontrolna zgadza się z otrzymaną.
                    if (receivedChecksum == calculatedChecksum)
                    {
                        byte[] payload = new byte[data.Length - 4];
                        Buffer.BlockCopy(data, 2, payload, 0, payload.Length);
                        return payload;
                    }
                }
                return null;
            }
            catch (SocketException)
            {
                return null;
            }
        }

        /// <summary>
        /// Zwalnia zasoby używane przez serwis komunikacji UDP.
        /// </summary>
        public void Dispose()
        {
            _udpClient?.Close();
            _udpClient?.Dispose();
        }

        /// <summary>
        /// Rozpoczyna wysyłanie sygnału "żywotności" do aplikacji master.
        /// </summary>
        public void StartSendingAliveSignal()
        {
            _isSendingAliveSignal = true;
            Task.Run(async () =>
            {
                while (_isSendingAliveSignal)
                {
                    try
                    {
                        var localEndPoint = (IPEndPoint)_udpClient.Client.LocalEndPoint;
                        var ipAddress = localEndPoint.Address.ToString();
                        var port = localEndPoint.Port;
                        var message = $"SLAVE_ALIVE;{ipAddress};{port}";
                        var messageBytes = Encoding.UTF8.GetBytes(message);

                        // Tworzenie ramki danych do wysłania.
                        byte[] frameData = new byte[messageBytes.Length + 4];
                        frameData[0] = 0xA0;
                        frameData[1] = 0xA1;
                        Buffer.BlockCopy(messageBytes, 0, frameData, 2, messageBytes.Length);

                        ushort checksum = CalculateChecksum(frameData, frameData.Length - 2);
                        frameData[^2] = (byte)(checksum >> 8);
                        frameData[^1] = (byte)(checksum & 0xFF);

                        // Wysyłanie ramki danych do mastera.
                        _udpClient.Send(frameData, frameData.Length, _masterEndPoint);
                        await Task.Delay(TimeSpan.FromSeconds(2));
                    }
                    catch (Exception)
                    {

                    }
                }
            });
        }

        /// <summary>
        /// Zatrzymuje wysyłanie sygnału "żywotności" do aplikacji master.
        /// </summary>
        public void StopSendingAliveSignal()
        {
            _isSendingAliveSignal = false;
        }

        /// <summary>
        /// Oblicza sumę kontrolną dla danych.
        /// </summary>
        /// <param name="data">Dane, dla których obliczana jest suma kontrolna.</param>
        /// <param name="length">Długość danych, dla których obliczana jest suma kontrolna.</param>
        /// <returns>Obliczona suma kontrolna.</returns>
        private ushort CalculateChecksum(byte[] data, int length)
        {
            ushort checksum = 0;
            for (int i = 0; i < length; i++)
            {
                checksum += data[i];
            }
            return checksum;
        }
    }
}











