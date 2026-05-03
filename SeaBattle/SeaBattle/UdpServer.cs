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

        public void Start()
        {
            sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            sock.Bind(new IPEndPoint(IPAddress.Any, 12345));
            clientEP = new IPEndPoint(IPAddress.Any, 0);
            ThreadPool.QueueUserWorkItem(_ => ReciveLoop());
        }

        private void ReciveLoop()
        {
            byte[] buffer = new byte[1024];
            while (true)
            {
                int received = sock.ReceiveFrom(buffer, ref clientEP);
                string message = Encoding.UTF8.GetString(buffer, 0, received);
                Console.WriteLine($"Received from {clientEP}: {message}");
                byte[] data = Encoding.UTF8.GetBytes(message);
                sock.SendTo(data, clientEP);
            }

        }

    }
}
