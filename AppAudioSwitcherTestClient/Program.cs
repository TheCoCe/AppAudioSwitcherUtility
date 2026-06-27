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