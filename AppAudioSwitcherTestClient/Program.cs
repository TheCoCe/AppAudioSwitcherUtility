// See https://aka.ms/new-console-template for more information

using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;

namespace AppAudioSwitcherTestClient;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        /*AppAudioSwitcherTestClient client = new AppAudioSwitcherTestClient();
        await client.Run();*/
        AppAudioSwitcherTestWebSocketClient client = new AppAudioSwitcherTestWebSocketClient();
        await client.Run();
        return 0;
    }
}

public class AppAudioSwitcherTestClient
{
    private TcpClient _client = new TcpClient();
    private readonly CancellationTokenSource _cts = new CancellationTokenSource();

    async Task Echo()
    {
        using (_client)
        using (NetworkStream stream = _client.GetStream())
        {
            byte[] buffer = new byte[1024];
            List<byte> rollingBuffer = new List<byte>(2048);
            
            while (!_cts.Token.IsCancellationRequested)
            {
                int bytesRead = 0;
                try
                {
                    bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, _cts.Token);
                }
                catch (IOException) { break; }
                catch (SocketException)  { break; }
                catch (ObjectDisposedException) { break; }
                catch (Exception ex)
                {
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
                        string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        Console.WriteLine("Received message:\n{0}", message);
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

    public async Task Run()
    {
        try
        {
            await _client.ConnectAsync(IPAddress.Loopback, 32122);
        }
        catch
        {
            return;
        }

        _ = Echo().ContinueWith(task => { _client.Close(); });

        using (_client)
        using (NetworkStream stream = _client.GetStream())
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                Console.WriteLine("Enter message to send or 'close' to exit: ");
                string? msg = Console.ReadLine();
                if (msg != null)
                {
                    if (msg.Equals("close", StringComparison.CurrentCultureIgnoreCase))
                    {
                        await _cts.CancelAsync();
                        break;
                    }

                    if (!msg.EndsWith('\0'))
                    {
                        msg += '\0';
                    }
                    byte[] buffer = Encoding.UTF8.GetBytes(msg);
                    await stream.WriteAsync(buffer, _cts.Token);
                }
            }
        }

        Console.WriteLine("Press any key to exit...");
    }
}

class AppAudioSwitcherTestWebSocketClient
{
    public async Task Run()
    {
        using ClientWebSocket socket = new ClientWebSocket();
        Uri uri = new Uri("ws://localhost:32122/ws/");

        Console.WriteLine("Connecting...");
        await socket.ConnectAsync(uri, CancellationToken.None);
        Console.WriteLine("Connected!");
        
        // Start background task to echo server messages
        _ = Task.Run(async () =>
        {
            byte[] buffer = new byte[4096];

            while (socket.State == WebSocketState.Open)
            {
                WebSocketReceiveResult result = await socket.ReceiveAsync(
                    new ArraySegment<byte>(buffer),
                    CancellationToken.None
                );

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    Console.WriteLine("Server closed connection.");
                    break;
                }

                string json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                Console.WriteLine($"[SERVER] {json}");
            }
        });

        // Send a message
        const string MESSAGE = "{\"type\":\"GetDevices\",\"payload\":{\"DataFlow\":\"eRender\"}}";
        Console.WriteLine("Type a message to send. Type 'close' to exit...");

        while (true)
        {
            string input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input))
                input = MESSAGE;
            
            if(string.Equals(input, "close", StringComparison.CurrentCultureIgnoreCase))
                break;

            byte[] bytes = Encoding.UTF8.GetBytes(input);

            await socket.SendAsync(
                new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text,
                endOfMessage: true,
                cancellationToken: CancellationToken.None
            );
        }

        Console.WriteLine("Closing connection...");
        await socket.CloseAsync(
            WebSocketCloseStatus.NormalClosure,
            "Client closing",
            CancellationToken.None
        );
    }
}