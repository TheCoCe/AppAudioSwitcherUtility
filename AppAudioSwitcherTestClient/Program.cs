// See https://aka.ms/new-console-template for more information

using System.Net;
using System.Net.Sockets;
using System.Text;

namespace AppAudioSwitcherTestClient;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        AppAudioSwitcherTestClient client = new AppAudioSwitcherTestClient();
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
            byte[] buffer = new byte[4096];
            while (!_cts.Token.IsCancellationRequested)
            {
                int bytesRead = 0;
                try
                {
                    bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, _cts.Token);
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
                Console.WriteLine("Received message:\n{0}", message);
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

                    byte[] buffer = Encoding.UTF8.GetBytes(msg);
                    await stream.WriteAsync(buffer, _cts.Token);
                }
            }
        }

        Console.WriteLine("Press any key to exit...");
    }
}