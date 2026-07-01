using System;
using System.Text.Json;
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
                    return await HandleGetCommand();
                }
                case "set":
                {
                    return await HandleSetCommand();
                }
                case "server":
                {
                    return await HandleServerMode();
                }
            }

            return -1;
        }

        private static async Task<int> HandleGetCommand()
        {
            string mode = Services.CommandLineParser.GetStringArgument("get", 'g');
            IMessage response; 
            switch (mode)
            {
                case "devices":
                {
                    MessageRouter router = new MessageRouter();
                    string dataFlowStr = Services.CommandLineParser.GetStringArgument("type", 't');
                    EDataFlow dataFlow = InteropTypeExtensions.StrToDataFlow(!string.IsNullOrEmpty(dataFlowStr) ? dataFlowStr : "render");
                    response = await router.HandleAsync(new PluginMessage(new DevicesMessageRequest(dataFlow)));
                    break;
                }
                case "focused":
                {
                    MessageRouter router = new MessageRouter();
                    response = await router.HandleAsync(new PluginMessage(new FocusedMessageRequest(Services.CommandLineParser.HasStringKey("icon", 'i'))));
                    break;
                }
                default:
                    return -1;
            }
            
            if (response == null) return -1;
            Console.WriteLine(JsonSerializer.Serialize(response));
            return 0;
        }

        private static async Task<int> HandleSetCommand()
        {
            string mode = Services.CommandLineParser.GetStringArgument("set", 's');
            FileLogger.LogInfo($"Handling set command: {mode}");
            switch (mode)
            {
                case "appDevice":
                {
                    int processId = Services.CommandLineParser.GetIntArgument("process", 'p', -1);
                    if(processId < 0)
                    {
                        FileLogger.LogError($"Invalid process id: {processId}");
                        return -1;
                    }

                    string device = Services.CommandLineParser.GetStringArgument("device", 'd');
                    if (string.IsNullOrEmpty(device))
                    {
                        return -1;
                    }
                    
                    MessageRouter router = new MessageRouter();
                    SetAppDeviceMessageResponse message =
                        await router.HandleAsync(
                                new PluginMessage(new SetAppDeviceMessageRequest((uint)processId, device))) as
                            SetAppDeviceMessageResponse;

                    return message?.Success ?? false ? 0 : -1;
                }
            }

            return -1;
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
    }
}