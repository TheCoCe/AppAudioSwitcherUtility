using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace AppAudioSwitcherUtility.Server
{
    public class AppAudioSwitcherServer
    {
        public delegate void MessageReceivedDelegate(AppAudioSwitcherServer server, string message);

        public event MessageReceivedDelegate MessageReceived;

        private TcpListener _listener = null;
        private TcpClient _client = null;
        private NetworkStream _stream = null;
        private readonly int _port = 0;
        private bool _wantsStop = false;

        public AppAudioSwitcherServer(int port)
        {
            this._port = port;
        }

        ~AppAudioSwitcherServer()
        {
            _stream?.Close();
            _client?.Close();
            _listener?.Stop();

            _stream = null;
            _client = null;
            _listener = null;
        }

        public int Run()
        {
            _listener = new TcpListener(IPAddress.Loopback, _port);
            _listener.Start();
            Console.WriteLine("Listening on port {0}", _port);

            // TBD: do we need to handle more that one connection?
            _client = _listener.AcceptTcpClient();
            _stream = _client.GetStream();
            _stream.ReadTimeout = 500;

            while (true)
            {
                try
                {
                    byte[] buffer = new byte[1024];
                    int bytesRead = _stream.Read(buffer, 0, buffer.Length);
                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                    //Console.WriteLine($"Received message: {message}");
                    MessageReceived?.Invoke(this, message);
                }
                catch (IOException e)
                {
                    if (!(e.InnerException is SocketException sockEx) || sockEx.SocketErrorCode != SocketError.TimedOut)
                    {
                        return -1;
                    }
                }

                if (_wantsStop)
                {
                    _stream.Close();
                    _stream = null;
                    
                    _client.Close();
                    _client = null;
                    
                    _listener.Stop();
                    _listener = null;
                    return 0;
                }
            }
        }

        public void Stop()
        {
            _wantsStop = true;
        }

        public void SendMessage(string message)
        {
            if (_stream == null || !_stream.CanWrite || string.IsNullOrEmpty(message)) return;

            byte[] buffer = Encoding.UTF8.GetBytes(message);
            _stream.Write(buffer, 0, buffer.Length);
        }
    }
}