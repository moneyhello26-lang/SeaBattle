using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Threading;

namespace SeaBattle
{
    internal class UdpClient
    {
        private Socket _sock;
        private EndPoint _endPoint;
        public event Action<string[]> OnMessageReceived;

        public void Connect(string ip, int port)
        {
            _endPoint = new IPEndPoint(IPAddress.Parse(ip), port);
            _sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            _sock.Connect(_endPoint);
            ThreadPool.QueueUserWorkItem(_ => ReceiveLoop());
        }

        private void ReceiveLoop()
        {
            byte[] buf = new byte[1024];
            EndPoint ep = new IPEndPoint(IPAddress.Any, 0);

            while (true)
            {
                int n = _sock.ReceiveFrom(buf, ref ep);
                var parts = Encoding.UTF8.GetString(buf, 0, n).Split(';');
                OnMessageReceived?.Invoke(parts);
            }
        }

    }
}
