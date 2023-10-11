using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SlaveApp.Services
{
    public class UdpCommunicationService : IDisposable
    {
        private UdpClient _udpClient;
        private IPEndPoint _masterEndPoint;
        private bool _isSendingAliveSignal;

        // Inicjalizacja serwisu z IP i portem Mastera
        public void Initialize(string masterIp, int masterUdpPort)
        {
            _udpClient = new UdpClient();
            _masterEndPoint = new IPEndPoint(IPAddress.Parse(masterIp), masterUdpPort);
            IPEndPoint localEP = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 0);
            _udpClient.Client.Bind(localEP);
        }

        // Odbieranie danych przez UDP
        public byte[] ReceiveData()
        {
            try
            {
                IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                return _udpClient.Receive(ref remoteEP);
            }
            catch (SocketException)
            {
                return null;
            }
        }

        // Zwolnienie zasobów
        public void Dispose()
        {
            _udpClient?.Close();
            _udpClient?.Dispose();
        }

        // Rozpoczęcie wysyłania sygnałów "żywotności" do Mastera
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
                        var data = Encoding.UTF8.GetBytes(message);
                        _udpClient.Send(data, data.Length, _masterEndPoint);
                        await Task.Delay(TimeSpan.FromSeconds(2));
                    }
                    catch (Exception)
                    {

                    }
                }
            });
        }

        // Zatrzymanie wysyłania sygnałów "żywotności"
        public void StopSendingAliveSignal()
        {
            _isSendingAliveSignal = false;
        }
    }
}







