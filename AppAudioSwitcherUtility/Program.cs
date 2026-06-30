using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using AppAudioSwitcherUtility.Audio;
using AppAudioSwitcherUtility.Process;
using AppAudioSwitcherUtility.Server;
using AppAudioSwitcherUtility.Server.Messages;
using AppAudioSwitcherUtility.Utils;

namespace AppAudioSwitcherUtility
{
    public static class Services
    {
        public static AudioDeviceManager DeviceManager { get; set; }
        public static CommandLineParser CommandLineParser { get; set; }
    }
    
    internal static class Program
    {
        public static async Task<int> Main(string[] args)
        {
            FileLogger.Init();
            Services.CommandLineParser = new CommandLineParser(args);
            Services.DeviceManager = new AudioDeviceManager();

            string mode = Services.CommandLineParser.GetFirstKey();
            FileLogger.LogInfo($"Starting in mode {mode}");
            switch (mode)
            {
                case "get":
                {
                    string getResponse = HandleGetCommand(Services.CommandLineParser);
                    if (string.IsNullOrEmpty(getResponse)) return -1;
                    Console.WriteLine(getResponse);
                    return 0;
                }
                case "set":
                {
                    return HandleSetCommand();
                }
                case "server":
                {
                    return await HandleServerMode();
                }
            }

            return 0;
        }

        private static async Task<int> HandleServerMode()
        {
            int port = Services.CommandLineParser.GetIntArgument("port", 'p', 32122);
            AppAudioSwitcherWebSocketServer server = new AppAudioSwitcherWebSocketServer(port);
            
            BackgroundProcessWatcher backgroundProcessWatcher = new BackgroundProcessWatcher();
            backgroundProcessWatcher.ForegroundProcessChanged += (processId) =>
            {
                FileLogger.LogInfo($"Foreground process changed: {processId}");
                _ = server.HandleMessage(new PluginMessage(new FocusedMessageRequest(true, processId)))
                    .ContinueWith(t => { server.Broadcast(t.Result); });
            };
            backgroundProcessWatcher.Start();
            
            AudioDeviceManager.DeviceDelegate devicesChanged = (device, flow) =>
            {
                FileLogger.LogInfo($"Device changed: {device.Id}");
                _ = server.HandleMessage(new PluginMessage(new DevicesMessageRequest(device.Flow)))
                    .ContinueWith(t => { server.Broadcast(t.Result); });
            };

            Services.DeviceManager.DeviceAdded += devicesChanged;
            Services.DeviceManager.DeviceRemoved += devicesChanged;

            AudioDevice.SessionDelegate sessionChanged = (device, session) =>
            {
                FileLogger.LogInfo($"Session changed for process {session.ProcessId} on device {device.Id}");
                if (session.ProcessId != backgroundProcessWatcher.CurrentForegroundProcessId) return;
                FileLogger.LogInfo($"Session changed for foreground process: {backgroundProcessWatcher.CurrentForegroundProcessId}");
                _ = server.HandleMessage(new PluginMessage(
                        new FocusedMessageRequest(false, backgroundProcessWatcher.CurrentForegroundProcessId)))
                    .ContinueWith(t => { server.Broadcast(t.Result); });
            };
            
            Services.DeviceManager.SessionAdded += sessionChanged;
            Services.DeviceManager.SessionRemoved += sessionChanged;
            
            return await server.RunAsync();
        }

        private static string HandleGetCommand(CommandLineParser parser)
        {
            string mode = parser.GetStringArgument("get", 'g');
            switch (mode)
            {
                case "devices":
                {
                    string dataFlowStr = parser.GetStringArgument("type", 't');
                    FileLogger.LogDebug($"Parsing device type argument: {dataFlowStr}");
                    EDataFlow dataFlow = InteropTypeExtensions.StrToDataFlow(!string.IsNullOrEmpty(dataFlowStr) ? dataFlowStr : "render");
                    FileLogger.LogDebug($"Resulting data flow: {dataFlow}");

                    string devicesJson = GetDevicesJsonString(dataFlow);
                    FileLogger.LogDebug($"Device json string: {devicesJson}");
                    return devicesJson;
                }
                case "focused":
                {
                    string processJson = GetProcessJsonString(parser.HasStringKey("icon", 'i'));
                    return processJson;
                }
                default:
                    return null;
            }
        }

        private static string GetDevicesJsonString(EDataFlow dataFlow)
        {
            return JsonSerializer.Serialize(new
            {
                id = "devices", 
                payload = Services.DeviceManager.GetAudioDeviceInfo(dataFlow)
            });
        }

        private static string GetProcessJsonString(bool includeIcon, uint? processId = null)
        {
            uint id = processId ?? ProcessUtilities.GetForegroundWindowProcessId();
            string deviceId = Services.DeviceManager.GetPersitedDefaultAudioEndpoint(id, EDataFlow.eRender, ERole.eMultimedia);
            if (string.IsNullOrEmpty(deviceId))
            {
                deviceId = Services.DeviceManager.GetPersitedDefaultAudioEndpoint(id, EDataFlow.eRender, ERole.eConsole);
            }

            JsonObject payload = new JsonObject
            {
                ["processId"] = id.ToString(),
                ["processName"] = ProcessUtilities.GetFriendlyName((int)id),
                ["deviceId"] = AudioDeviceManager.UnpackDeviceId(deviceId),
                ["hasSession"] = Services.DeviceManager.HasSession((int)id)
            };

            if (includeIcon)
            {
                payload["processIconBase64"] = ProcessIconExtractor.GetBase64IconFromProcess((int)id) ?? "";
            }
            
            return JsonSerializer.Serialize(new
            {
                id = "focused",
                payload
            });
        }

        private static int HandleSetCommand()
        {
            string mode = Services.CommandLineParser.GetStringArgument("set", 's');
            FileLogger.LogInfo($"Handling set command: {mode}");
            switch (mode)
            {
                case "appDevice":
                {
                    // Try to get the process id param
                    string processStr = Services.CommandLineParser.GetStringArgument("process", 'p');
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
                        FileLogger.LogError($"Failed to parse process id as uint: {processStr}\n{e.Message}");
                        return -1;
                    }

                    // Try to parese the device id parameter
                    string device = Services.CommandLineParser.GetStringArgument("device", 'd');
                    if (string.IsNullOrEmpty(device))
                    {
                        return -1;
                    }
                    
                    // Lastly try to set the endpoints
                    bool bConsoleSuccess = Services.DeviceManager.SetPersistedDefaultAudioEndpoint(processId, EDataFlow.eRender, ERole.eConsole, device);
                    bool bMultimediaSuccess = Services.DeviceManager.SetPersistedDefaultAudioEndpoint(processId, EDataFlow.eRender, ERole.eMultimedia, device);

                    return bConsoleSuccess || bMultimediaSuccess ? 0 : -1;       
                }
            }

            return -1;
        }
    }
}