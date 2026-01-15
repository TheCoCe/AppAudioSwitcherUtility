using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AppAudioSwitcherUtility.Server
{
    public class AppAudioSwitcherServer
    {
        public readonly struct Request
        {
            public Request(TcpClient client, string message)
            {
                Client = client;
                Message = message;
            }
            
            public TcpClient Client { get; }
            public string Message { get; }
        }
        
        public delegate void MessageReceivedDelegate(Request request);

        public event MessageReceivedDelegate MessageReceived;

        private TcpListener _listener = null;
        private readonly List<TcpClient> _clients = new List<TcpClient>();
        private readonly object _lock = new object();
        private CancellationTokenSource _cts = new CancellationTokenSource();

        public AppAudioSwitcherServer(int port)
        {
            _listener = new TcpListener(IPAddress.Loopback, port);
        }

        ~AppAudioSwitcherServer()
        {
            _listener?.Stop();
            _listener = null;
        }

        public async Task<int> RunAsync()
        {
            _listener.Start();
            Console.WriteLine("Listening on port {0}", ((IPEndPoint)_listener.LocalEndpoint).Port);

            while (!_cts.IsCancellationRequested)
            {
                try
                {
                    AddClient(await _listener.AcceptTcpClientAsync());
                }
                catch (ObjectDisposedException)
                {
                    return -1;
                }
            }

            return 0;
        }

        private void AddClient(TcpClient client)
        {
            if(client == null) return;

            int numClients = 0;
            lock (_lock)
            {
                _clients.Add(client);
                numClients = _clients.Count;
            }
            
            _ = HandleClientAsync(client, _cts.Token);

            Console.WriteLine($"Client {client.Client.RemoteEndPoint} connected");
            Console.WriteLine($"Num connected clients: {numClients}");
        }

        private void RemoveClient(TcpClient client)
        {
            if (client == null) return;
            
            int numClients = 0;
            lock (_lock)
            {
                _clients.Remove(client);
                numClients = _clients.Count;
            }
            
            client.Close();
            Console.WriteLine("Client disconnected");
            Console.WriteLine($"Num connected clients: {numClients}");
        }

        private async Task HandleClientAsync(TcpClient client, CancellationToken ctsToken)
        {
            try
            {
                using (NetworkStream stream = client.GetStream())
                {
                    byte[] buffer = new byte[1024];
                    while (!ctsToken.IsCancellationRequested)
                    {
                        int bytesRead = 0;
                        try
                        {
                            bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, ctsToken);
                        }
                        catch (Exception ex) when (ex is IOException || ex is SocketException ||
                                                   ex is ObjectDisposedException)
                        {
                            break;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Unexpected exception occured while reading stream: {0}", ex);
                            break;
                        }

                        if (bytesRead == 0)
                        {
                            break;
                        }

                        string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        MessageReceived?.Invoke(new Request(client, message));
                    }
                }
            }
            finally
            {
                RemoveClient(client);
            }
        }

        public void Stop()
        {
            _cts.Cancel();
            _listener.Stop();

            lock (_lock)
            {
                foreach (TcpClient client in _clients)
                {
                    try
                    {
                        client?.Close();
                    }
                    catch
                    {
                        // ignored
                    }
                }
                
                _clients.Clear();
            }
        }

        public async Task SendMessage(TcpClient client, string message)
        {
            if (client == null || !client.Connected) return;
            try
            {
                NetworkStream stream = client.GetStream();
                Console.WriteLine("Sending message to {0}: {1}", client.Client.RemoteEndPoint, message);
                byte[] buffer = Encoding.UTF8.GetBytes(message);
                await stream.WriteAsync(buffer, 0, buffer.Length, _cts.Token);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        
        public async Task BroadcastMessage(string message)
        {
            List<TcpClient> clients;
            lock (_lock)
            {
                clients = _clients;
            }
            
            List<Task> tasks = clients.Select(client => SendMessage(client, message)).ToList();
            await Task.WhenAll(tasks.ToArray());
        }
    }
}