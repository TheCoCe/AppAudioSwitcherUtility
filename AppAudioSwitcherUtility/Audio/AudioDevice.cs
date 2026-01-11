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
        
        public static AudioDevice[] GetAudioDevices(EDataFlow dataFlow, DeviceState deviceState)
        {
            if (_enumerator == null)
            {
                _enumerator = (IMMDeviceEnumerator)new MMDeviceEnumerator();
            }
            
            var devices = _enumerator.EnumAudioEndpoints(dataFlow, deviceState);
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
    }
}