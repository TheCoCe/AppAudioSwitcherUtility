using System;
using System.Text.Json;
using System.Threading.Tasks;
using AppAudioSwitcherUtility.Audio;
using AppAudioSwitcherUtility.Process;
using AppAudioSwitcherUtility.Server;
using AppAudioSwitcherUtility.Utils;

namespace AppAudioSwitcherUtility
{
    internal static class Program
    {
        public static async Task<int> Main(string[] args)
        {
            CommandLineParser commandLineParser = new CommandLineParser(args);
            string mode = commandLineParser.GetFirstKey();
            switch (mode)
            {
                case "get":
                {
                    string getResponse = HandleGetCommand(commandLineParser);
                    if (!string.IsNullOrEmpty(getResponse))
                    {
                        Console.WriteLine(getResponse);
                        return 0;
                    }

                    return -1;
                }
                case "set":
                {
                    return HandleSetCommand(commandLineParser);
                }
                case "server":
                {
                    return await HandleServerMode(commandLineParser);
                }
            }

            return 0;
        }

        private static async Task<int> HandleServerMode(CommandLineParser parser)
        {
            int port = 32122;
            string portStr = parser.GetStringArgument("port", 'p');
            if (!string.IsNullOrEmpty(portStr))
            {
                try
                {
                    port = int.Parse(portStr);
                }
                catch
                {
                    // Ignore
                }
            }
            
            AppAudioSwitcherServer server = new AppAudioSwitcherServer(port);
            server.MessageReceived += (message) =>
            {
                HandleMessageReceived(server, message);
            };
            
            BackgroundProcessWatcher backgroundProcessWatcher = new BackgroundProcessWatcher();
            backgroundProcessWatcher.ForegroundProcessChanged += (processId) =>
            {
                _ = server.BroadcastMessage(GetProcessJsonWithIcon(processId));
            };
            backgroundProcessWatcher.Start();
            
            return await server.RunAsync();
        }

        private static void HandleMessageReceived(AppAudioSwitcherServer server, AppAudioSwitcherServer.Request request)
        {
            if (request.Message.ToLower() == "close")
            {
                server.Stop();
                return;
            }
            
            string[] args = request.Message.Split(' ');
            CommandLineParser parser = new CommandLineParser(args);
            
            string mode = parser.GetFirstKey();
            switch (mode)
            {
                case "get":
                {
                    _ = server.SendMessage(request.Client, HandleGetCommand(parser));
                    break;
                }
                case "set":
                {
                    HandleSetCommand(parser);
                    break;
                }
            }
        }

        private static string HandleGetCommand(CommandLineParser parser)
        {
            string mode = parser.GetStringArgument("get", 'g');
            switch (mode)
            {
                case "devices":
                {
                    string dataFlowStr = parser.GetStringArgument("type", 't');
                    EDataFlow dataFlow = InteropTypeExtensions.StrToDataFlow(!string.IsNullOrEmpty(dataFlowStr) ? dataFlowStr : "render");
                    string deviceStateStr = parser.GetStringArgument("state", 's');
                    DeviceState deviceState = InteropTypeExtensions.StrToDeviceState(!string.IsNullOrEmpty(deviceStateStr) ? deviceStateStr : "active");

                    string devicesJson = GetDevicesJson(dataFlow, deviceState);
                    return devicesJson;
                }
                case "focused":
                {
                    string processJson = parser.HasStringKey("icon", 'i') ? GetProcessJsonWithIcon() : GetProcessJson();
                    return processJson;
                }
                default:
                    return null;
            }
        }

        private static string GetDevicesJson(EDataFlow dataFlow, DeviceState state)
        {
            AudioDevice[] devices = AudioDeviceUtils.GetAudioDevices(dataFlow, state);
            return JsonSerializer.Serialize(new
            {
                id = "devices", 
                payload = new { devices } 
            });
        }

        private static string GetProcessJson(uint? processId = null)
        {
            uint id = processId ?? ProcessUtilities.GetForegroundWindowProcessId();
            string deviceId = AudioDeviceUtils.GetPersitedDefaultAudioEndpoint(id, EDataFlow.eRender, ERole.eMultimedia);
            if (string.IsNullOrEmpty(deviceId))
            {
                deviceId = AudioDeviceUtils.GetPersitedDefaultAudioEndpoint(id, EDataFlow.eRender, ERole.eConsole);
            }

            return JsonSerializer.Serialize(new
            {
                id = "focused",
                payload = new
                {
                    processId = processId.ToString(),
                    processName = ProcessUtilities.GetFriendlyName((int)id),
                    deviceId = AudioDeviceUtils.UnpackDeviceId(deviceId),
                    playsSound = AudioDeviceUtils.CheckProcessForSound(id, EDataFlow.eRender, ERole.eMultimedia, DeviceState.ACTIVE),
                }
            });
        }

        private static string GetProcessJsonWithIcon(uint? processId = null)
        {
            uint id = processId ?? ProcessUtilities.GetForegroundWindowProcessId();
            string processIconBase64 = ProcessIconExtractor.GetBase64IconFromProcess((int)id) ?? "";
            string deviceId = AudioDeviceUtils.GetPersitedDefaultAudioEndpoint(id, EDataFlow.eRender, ERole.eMultimedia);
            if (string.IsNullOrEmpty(deviceId))
            {
                deviceId = AudioDeviceUtils.GetPersitedDefaultAudioEndpoint(id, EDataFlow.eRender, ERole.eConsole);
            }
            
            return JsonSerializer.Serialize(new
            {
                id = "icon",
                payload = new
                {
                    processId = id.ToString(),
                    processName = ProcessUtilities.GetFriendlyName((int)id),
                    deviceId = AudioDeviceUtils.UnpackDeviceId(deviceId),
                    playsSound = AudioDeviceUtils.CheckProcessForSound(id, EDataFlow.eRender, ERole.eMultimedia, DeviceState.ACTIVE),
                    processIconBase64
                }
            });
        }

        private static int HandleSetCommand(CommandLineParser parser)
        {
            string mode = parser.GetStringArgument("set", 's');
            switch (mode)
            {
                case "appDevice":
                {
                    // Try to get the process id param
                    string processStr = parser.GetStringArgument("process", 'p');
                    if (string.IsNullOrEmpty(processStr))
                    {
                        return -1;
                    }

                    // Try parsing the process id param into uint
                    uint processId = 0;
                    try
                    {
                        processId = uint.Parse(processStr);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Failed to parse process id as uint: {0}\n{1}", processStr, e.Message);
                        return -1;
                    }

                    // Try to parese the device id parameter
                    string device = parser.GetStringArgument("device", 'd');
                    if (string.IsNullOrEmpty(device))
                    {
                        return -1;
                    }

                    // Lastly try to set the endpoints
                    bool bConsoleSuccess = AudioDeviceUtils.SetPersistedDefaultAudioEndpoint(processId, EDataFlow.eRender, ERole.eConsole, device);
                    bool bMultimediaSuccess = AudioDeviceUtils.SetPersistedDefaultAudioEndpoint(processId, EDataFlow.eRender, ERole.eMultimedia, device);

                    return bConsoleSuccess || bMultimediaSuccess ? 0 : -1;       
                }
            }

            return -1;
        }
    }
}