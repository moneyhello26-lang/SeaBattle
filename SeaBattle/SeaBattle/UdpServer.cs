using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;


namespace SeaBattle
{
    internal class UdpServer
    {
        private Socket sock;
        private EndPoint clientEP;

        public event Action<string[]> OnMessageReceived;

        public void Start(int port)
        {
            sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            sock.Bind(new IPEndPoint(IPAddress.Any, port));
            clientEP = new IPEndPoint(IPAddress.Any, 0);
            ThreadPool.QueueUserWorkItem(_ => ReciveLoop());
        }

        private void ReciveLoop()
        {
            byte[] buffer = new byte[1024];
            while (true)
            {
                try
                {
                    int received = sock.ReceiveFrom(buffer, ref clientEP);
                    string msg = Encoding.UTF8.GetString(buffer, 0, received);
                    var parts = msg.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                    
                    if (parts.Length > 0)
                        OnMessageReceived?.Invoke(parts);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"UdpServer error: {ex.Message}");
                }
            }
        }

        public void Send(string msg)
        {
            if (clientEP == null || ((IPEndPoint)clientEP).Address.Equals(IPAddress.Any))
            {
                return;
            }
            byte[] data = Encoding.UTF8.GetBytes(msg);
            sock.SendTo(data, clientEP);
        }
    }
}
