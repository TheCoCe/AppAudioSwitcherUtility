using System;

namespace AppAudioSwitcherUtility.Audio
{
    public struct AudioDevice
    {
        public AudioDevice(string name, string id)
        {
            Name = name;
            Id = id;
        }

        public string Name { get; }
        public string Id { get; }
    }

    public static class AudioDeviceUtils
    {
        private static IMMDeviceEnumerator _enumerator = null;
        private static IMMDeviceEnumerator DeviceEnumerator => _enumerator ?? (_enumerator = (IMMDeviceEnumerator)new MMDeviceEnumerator());
        
        private static IAudioPolicyConfigFactory _policyConfigFactory;
        private static IAudioPolicyConfigFactory PolicyConfigFactory => _policyConfigFactory ?? (_policyConfigFactory = new AudioPolicyConfigFactoryImplForDownlevel());

        public static AudioDevice[] GetAudioDevices(EDataFlow dataFlow, DeviceState deviceState)
        {
            IMMDeviceCollection devices = DeviceEnumerator.EnumAudioEndpoints(dataFlow, deviceState);
            uint deviceCount = devices.GetCount();
            
            AudioDevice[] ret = new AudioDevice[deviceCount];
            for (uint i = 0; i < deviceCount; i++)
            {
                var device = devices.Item(i);
                string id = device.GetId();
                var propStore = device.OpenPropertyStore(STGM.STGM_READ);
                string name = propStore.GetValue<string>(PropertyKeys.PKEY_Device_FriendlyName);

                ret[i] = new AudioDevice(name, id);
            }

            return ret;
        }

        public static bool CheckProcessForSound(uint processId, EDataFlow dataFlow, ERole role, DeviceState deviceState)
        {
            // TODO: Cache this info somehow?
            IMMDeviceCollection collection = DeviceEnumerator.EnumAudioEndpoints(dataFlow, deviceState);

            uint deviceCount = collection.GetCount();
            for (uint i = 0; i < deviceCount; i++)
            {
                IMMDevice device = collection.Item(i);
                IAudioSessionManager2 sessionManager = device.Activate<IAudioSessionManager2>();
                IAudioSessionEnumerator sessionEnumerator = sessionManager.GetSessionEnumerator();
                int sessionCount = sessionEnumerator.GetCount();
                for (int j = 0; j < sessionCount; j++)
                {
                    IAudioSessionControl session = sessionEnumerator.GetSession(j);
                    int hr = ((IAudioSessionControl2)session).GetProcessId(out uint pid);
                    if (hr == (int)HRESULT.AUDCLNT_S_NO_SINGLE_PROCESS ||
                        hr == (int)HRESULT.S_OK)
                    {
                        if (((IAudioSessionControl2)session).IsSystemSoundsSession() == HRESULT.S_OK)
                        {
                            pid = 0;
                        }

                        if (processId == pid)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }
        
        public static bool SetPersistedDefaultAudioEndpoint(uint processId, EDataFlow dataFlow, ERole role, string deviceId)
        {
            // this crashes due to failed marshalling param, no idea what that means
            IntPtr hstring = IntPtr.Zero;

            if (!string.IsNullOrEmpty(deviceId))
            {
                string str = GenerateDeviceId(deviceId, EDataFlow.eRender);
                Combase.WindowsCreateString(str, (uint)str.Length, out hstring);
            }

            return PolicyConfigFactory.SetPersistedDefaultAudioEndpoint(processId, dataFlow, role, hstring) == HRESULT.S_OK;
        }

        public static string GetPersitedDefaultAudioEndpoint(uint processId, EDataFlow dataFlow, ERole role)
        {
            PolicyConfigFactory.GetPersistedDefaultAudioEndpoint(processId, dataFlow, role, out string deviceId);
            return deviceId;
        }
        
        private const string DEVINTERFACE_AUDIO_RENDER = "#{e6327cad-dcec-4949-ae8a-991e976a79d2}";
        private const string DEVINTERFACE_AUDIO_CAPTURE = "#{2eef81be-33fa-4800-9670-1cd474972c3f}";
        private const string MMDEVAPI_TOKEN = @"\\?\SWD#MMDEVAPI#";

        public static string GenerateDeviceId(string deviceId, EDataFlow dataFlow)
        {
            return
                $"{MMDEVAPI_TOKEN}{deviceId}{(dataFlow == EDataFlow.eRender ? DEVINTERFACE_AUDIO_RENDER : DEVINTERFACE_AUDIO_CAPTURE)}";
        }
        
        public static string UnpackDeviceId(string deviceId)
        {
            if(string.IsNullOrEmpty(deviceId)) return deviceId;
            if (deviceId.StartsWith(MMDEVAPI_TOKEN)) deviceId = deviceId.Remove(0, MMDEVAPI_TOKEN.Length);
            if (deviceId.EndsWith(DEVINTERFACE_AUDIO_RENDER)) deviceId = deviceId.Remove(deviceId.Length - DEVINTERFACE_AUDIO_RENDER.Length);
            if (deviceId.EndsWith(DEVINTERFACE_AUDIO_CAPTURE)) deviceId = deviceId.Remove(deviceId.Length - DEVINTERFACE_AUDIO_CAPTURE.Length);
            return deviceId;
        }
    }
}