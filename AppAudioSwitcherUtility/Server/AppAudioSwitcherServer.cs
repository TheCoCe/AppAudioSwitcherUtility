using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AppAudioSwitcherUtility.Server.Messages;
using AppAudioSwitcherUtility.Utils;

namespace AppAudioSwitcherUtility.Server
{
    public class AppAudioSwitcherWebSocketServer
    {
        private readonly HttpListener _listener;
        private readonly MessageRouter _router;
        private readonly ConnectionManager _connectionManager;
        
        public AppAudioSwitcherWebSocketServer(int port)
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://localhost:{port}/ws/");
            _router = new MessageRouter();
            _connectionManager = new ConnectionManager();
        }
        
        public async Task<int> RunAsync()
        {
            try
            {
                _listener.Start();
            }
            catch (HttpListenerException e)
            {
                FileLogger.LogError(e.ToString());
                return 0;
            }
            
            FileLogger.LogInfo("Test");
            foreach (string p in _listener.Prefixes)
                FileLogger.LogInfo("Prefix active: " + p);
            FileLogger.LogInfo(_listener.Prefixes.Count.ToString());

            while (_listener.IsListening)
            {
                HttpListenerContext httpListenerContext = await _listener.GetContextAsync();

                if (httpListenerContext.Request.IsWebSocketRequest)
                {
                    _ = HandleClientAsync(httpListenerContext);
                }
                else
                {
                    FileLogger.LogInfo("Rejected connection");
                    httpListenerContext.Response.StatusCode = 400;
                    httpListenerContext.Response.Close();
                }
            }

            return 0;
        }

        private async Task HandleClientAsync(HttpListenerContext httpListenerContext)
        {
            HttpListenerWebSocketContext wsContext = await httpListenerContext.AcceptWebSocketAsync(null);
            WebSocket webSocket = wsContext.WebSocket;
            
            ConnectionManager.ConnectionInfo connectionInfo = _connectionManager.AddSocket(webSocket);
            FileLogger.LogInfo($"Client connected: {connectionInfo.Id}");
            
            byte[] buffer = new byte[4096];

            try
            {
                while (webSocket.State == WebSocketState.Open)
                {
                    WebSocketReceiveResult result =
                        await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Close)
                        break;

                    string messageContent = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    FileLogger.LogDebug($"Received message from {connectionInfo.Id}: {messageContent}");

                    PluginMessage message;
                    try
                    {
                        message = JsonSerializer.Deserialize<PluginMessage>(messageContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    }
                    catch (Exception e)
                    {
                        FileLogger.LogError($"Recieved invalid message: {e}");
                        continue;
                    }
                    
                    FileLogger.LogDebug($"Received message from {connectionInfo.Id}: {message.Type} {message.Payload}");

                    IMessage response = await _router.HandleAsync(message);
                    if (response != null)
                    {
                        _ = SendResponse(new PluginMessage(response), connectionInfo);
                    }
                }
            }
            catch (Exception e)
            {
                FileLogger.LogError(e.ToString());
            }
            finally
            {
                FileLogger.LogInfo($"Client disconnected: {connectionInfo.Id}");
                await _connectionManager.RemoveSocketAsync(connectionInfo.Id);
            }
        }

        public void Stop()
        {
            _listener.Stop();
            _listener.Close();
        }

        public async Task<PluginMessage> HandleMessage(PluginMessage message)
        {
            IMessage response = await _router.HandleAsync(message);
            return new PluginMessage(response);
        }

        public Task Broadcast(PluginMessage message)
        {
            return SendResponse(message, _connectionManager.Connections);
        }

        private static Task SendResponse(PluginMessage message, ConnectionManager.ConnectionInfo connections)
        {
            return SendResponse(message, new[] { connections });
        }
        
        private static async Task SendResponse(PluginMessage message, IEnumerable<ConnectionManager.ConnectionInfo> connections)
        {
            string json = JsonSerializer.Serialize(message);
            byte[] buffer = Encoding.UTF8.GetBytes(json);
            IEnumerable<Task> tasks = connections.Select(c =>
                c.WebSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true,
                    CancellationToken.None));
            await Task.WhenAll(tasks);
        }
    }
}