using System;
using System.Text.Json;
using AppAudioSwitcherUtility.Audio;
using AppAudioSwitcherUtility.Process;
using AppAudioSwitcherUtility.Utils;

namespace AppAudioSwitcherUtility
{
    internal static class Program
    {
        public static int Main(string[] args)
        {
            CommandLineParser commandLineParser = new CommandLineParser(args);
            string mode = commandLineParser.GetFirstKey();
            switch (mode)
            {
                case "get":
                {
                    return HandleGetCommand(commandLineParser);
                }
                case "set":
                {
                    return HandleSetCommand(commandLineParser);
                }
            }

            return 0;
        }

        static int HandleGetCommand(CommandLineParser parser)
        {
            string mode = parser.GetStringArgument("get", 'g', out bool bFound);
            switch (mode)
            {
                case "devices":
                {
                    string dataFlowStr = parser.GetStringArgument("type", 't', out bFound);
                    EDataFlow dataFlow = InteropTypeExtensions.StrToDataFlow(bFound ? dataFlowStr : "render");
                    string deviceStateStr = parser.GetStringArgument("state", 's', out bFound);
                    DeviceState deviceState = InteropTypeExtensions.StrToDeviceState(bFound ? deviceStateStr : "active");

                    string devicesJson = GetDevicesJson(dataFlow, deviceState);
                    Console.WriteLine(devicesJson);
                    return 0;
                }
                case "focused":
                {
                    string processJson = parser.HasStringKey("icon", 'i') ? GetProcessJsonWithIcon() : GetProcessJson();
                    Console.WriteLine(processJson);
                    return 0;
                }
                default:
                    return -1;
            }
        }

        private static string GetDevicesJson(EDataFlow dataFlow, DeviceState state)
        {
            AudioDevice[] devices = AudioDeviceUtils.GetAudioDevices(dataFlow, state);
            return JsonSerializer.Serialize(new { devices });
        }

        private static string GetProcessJson()
        {
            uint processId = ProcessUtilities.GetForegroundWindowProcessId();
            string deviceId = GetPersitedDefaultAudioEndpoint(processId, EDataFlow.eRender, ERole.eMultimedia);
            if (string.IsNullOrEmpty(deviceId))
            {
                deviceId = GetPersitedDefaultAudioEndpoint(processId, EDataFlow.eRender, ERole.eConsole);
            }

            return JsonSerializer.Serialize(new
            {
                processId = processId.ToString(),
                processName = ProcessUtilities.GetProcessName((int)processId),
                deviceId = UnpackDeviceId(deviceId)
            });
        }

        private static string GetProcessJsonWithIcon()
        {
            uint processId = ProcessUtilities.GetForegroundWindowProcessId();
            string processIconBase64 = ProcessIconExtractor.GetBase64IconFromProcess((int)processId) ?? "";
            string deviceId = GetPersitedDefaultAudioEndpoint(processId, EDataFlow.eRender, ERole.eMultimedia);
            if (string.IsNullOrEmpty(deviceId))
            {
                deviceId = GetPersitedDefaultAudioEndpoint(processId, EDataFlow.eRender, ERole.eConsole);
            }
            
            return JsonSerializer.Serialize(new
            {
                processId = processId.ToString(),
                processName = ProcessUtilities.GetFriendlyName((int)processId),
                deviceId = UnpackDeviceId(deviceId),
                processIconBase64
            });
        }

        private static int HandleSetCommand(CommandLineParser parser)
        {
            string mode = parser.GetStringArgument("set", 's', out bool bFound);
            switch (mode)
            {
                case "appDevice":
                {
                    // Try to get the process id param
                    string processStr = parser.GetStringArgument("process", 'p', out bFound);
                    if (!bFound || string.IsNullOrEmpty(processStr))
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
                    string device = parser.GetStringArgument("device", 'd', out bFound);
                    if (!bFound || string.IsNullOrEmpty(device))
                    {
                        return -1;
                    }

                    // Lastly try to set the endpoints
                    bool bConsoleSuccess = SetPersistedDefaultAudioEndpoint(processId, EDataFlow.eRender, ERole.eConsole, device);
                    bool bMultimediaSuccess = SetPersistedDefaultAudioEndpoint(processId, EDataFlow.eRender, ERole.eMultimedia, device);

                    return bConsoleSuccess || bMultimediaSuccess ? 0 : -1;       
                }
            }

            return -1;
        }

        private const string DEVINTERFACE_AUDIO_RENDER = "#{e6327cad-dcec-4949-ae8a-991e976a79d2}";
        private const string DEVINTERFACE_AUDIO_CAPTURE = "#{2eef81be-33fa-4800-9670-1cd474972c3f}";
        private const string MMDEVAPI_TOKEN = @"\\?\SWD#MMDEVAPI#";

        private static string GenerateDeviceId(string deviceId, EDataFlow dataFlow)
        {
            return
                $"{MMDEVAPI_TOKEN}{deviceId}{(dataFlow == EDataFlow.eRender ? DEVINTERFACE_AUDIO_RENDER : DEVINTERFACE_AUDIO_CAPTURE)}";
        }
        
        private static string UnpackDeviceId(string deviceId)
        {
            if(string.IsNullOrEmpty(deviceId)) return deviceId;
            if (deviceId.StartsWith(MMDEVAPI_TOKEN)) deviceId = deviceId.Remove(0, MMDEVAPI_TOKEN.Length);
            if (deviceId.EndsWith(DEVINTERFACE_AUDIO_RENDER)) deviceId = deviceId.Remove(deviceId.Length - DEVINTERFACE_AUDIO_RENDER.Length);
            if (deviceId.EndsWith(DEVINTERFACE_AUDIO_CAPTURE)) deviceId = deviceId.Remove(deviceId.Length - DEVINTERFACE_AUDIO_CAPTURE.Length);
            return deviceId;
        }

        private static bool SetPersistedDefaultAudioEndpoint(uint processId, EDataFlow dataFlow, ERole role, string deviceId)
        {
            // this crashes due to failed marshalling param, no idea what that means
            IAudioPolicyConfigFactory policyConfig = new AudioPolicyConfigFactoryImplForDownlevel();
            IntPtr hstring = IntPtr.Zero;

            if (!string.IsNullOrEmpty(deviceId))
            {
                string str = GenerateDeviceId(deviceId, EDataFlow.eRender);
                Combase.WindowsCreateString(str, (uint)str.Length, out hstring);
            }

            return policyConfig.SetPersistedDefaultAudioEndpoint(processId, dataFlow, role, hstring) == HRESULT.S_OK;
        }

        private static string GetPersitedDefaultAudioEndpoint(uint processId, EDataFlow dataFlow, ERole role)
        {
            IAudioPolicyConfigFactory policyConfig = new AudioPolicyConfigFactoryImplForDownlevel();
            policyConfig.GetPersistedDefaultAudioEndpoint(processId, dataFlow, role, out string deviceId);
            return deviceId;
        }
    }
}