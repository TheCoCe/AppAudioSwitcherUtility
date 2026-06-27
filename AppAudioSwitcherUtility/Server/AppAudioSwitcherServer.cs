using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AppAudioSwitcherUtility.Utils;

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

        private const char Delimiter = '\0';

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
            FileLogger.LogInfo($"Listening on port {((IPEndPoint)_listener.LocalEndpoint).Port}");

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

            FileLogger.LogInfo($"Client {client.Client.RemoteEndPoint} connected");
            FileLogger.LogInfo($"Num connected clients: {numClients}");
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
            FileLogger.LogInfo("Client disconnected");
            FileLogger.LogInfo($"Num connected clients: {numClients}");
        }

        private async Task HandleClientAsync(TcpClient client, CancellationToken ctsToken)
        {
            try
            {
                using (NetworkStream stream = client.GetStream())
                {
                    byte[] buffer = new byte[1024];
                    List<byte> rollingBuffer = new List<byte>(2048);
                    
                    while (!ctsToken.IsCancellationRequested)
                    {
                        int bytesRead = 0;
                        try
                        {
                            bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, ctsToken);
                        }
                        catch (IOException) { break; }
                        catch (SocketException)  { break; }
                        catch (ObjectDisposedException) { break; }
                        catch (Exception ex)
                        {
                            FileLogger.LogError($"Unexpected exception occured while reading stream: {ex}");
                            break;
                        }

                        if (bytesRead == 0)
                        {
                            break;
                        }

                        for (int i = 0; i < bytesRead; i++)
                        {
                            if (buffer[i] == 0)
                            {
                                string message = Encoding.UTF8.GetString(rollingBuffer.ToArray());
                                MessageReceived?.Invoke(new Request(client, message));
                                rollingBuffer.Clear();
                            }
                            else
                            {
                                rollingBuffer.Add(buffer[i]);
                            }
                        }
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
            FileLogger.LogInfo("Stopping server...");
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
                if (message.Length == 0 || message[message.Length - 1] != Delimiter)
                {
                    message += Delimiter;
                }
                NetworkStream stream = client.GetStream();
                FileLogger.LogInfo($"Sending message to {client.Client.RemoteEndPoint}: {message}");
                byte[] buffer = Encoding.UTF8.GetBytes(message);
                await stream.WriteAsync(buffer, 0, buffer.Length, _cts.Token);
            }
            catch (Exception ex)
            {
                FileLogger.LogError($"Send Message failed trying to send message {message} to {client.Client.RemoteEndPoint}: {ex}");
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