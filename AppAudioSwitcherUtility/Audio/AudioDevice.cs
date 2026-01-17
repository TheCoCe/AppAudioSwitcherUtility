using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text.Json.Nodes;

namespace AppAudioSwitcherUtility.Audio
{
    public class AudioDeviceManager : IMMNotificationClient
    {
        private readonly ConcurrentDictionary<string, AudioDevice>[] _devices =
            {
                new ConcurrentDictionary<string, AudioDevice>(), new ConcurrentDictionary<string, AudioDevice>()
            };

        private IMMDeviceEnumerator _enumerator;
        private IAudioPolicyConfigFactory _policyConfigFactory;

        public delegate void DeviceDelegate(AudioDevice device, EDataFlow flow);

        public event DeviceDelegate DeviceAdded;
        public event DeviceDelegate DeviceRemoved;

        public event AudioDevice.SessionDelegate SessionAdded;
        public event AudioDevice.SessionDelegate SessionRemoved;

        public AudioDeviceManager()
        {
            _policyConfigFactory = new AudioPolicyConfigFactoryImplForDownlevel();

            try
            {
                _enumerator = (IMMDeviceEnumerator)new MMDeviceEnumerator();
                _enumerator.RegisterEndpointNotificationCallback(this);

                InitializeDevices(EDataFlow.eCapture, DeviceState.MASK_ALL);
                InitializeDevices(EDataFlow.eRender, DeviceState.MASK_ALL);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to initialize audio device manager: {ex.Message}");
            }
        }

        ~AudioDeviceManager()
        {
            try
            {
                _enumerator.UnregisterEndpointNotificationCallback(this);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to uninitialize audio device manager {ex}");
            }
        }

        private void InitializeDevices(EDataFlow flow, DeviceState state)
        {
            IMMDeviceCollection devices = _enumerator.EnumAudioEndpoints(flow, state);
            uint deviceCount = devices.GetCount();
            for (uint i = 0; i < deviceCount; i++)
            {
                ((IMMNotificationClient)this).OnDeviceAdded(devices.Item(i).GetId());
            }
        }

        private ConcurrentDictionary<string, AudioDevice> GetDeviceDict(EDataFlow dataFlow)
        {
            return dataFlow < EDataFlow.eAll ? _devices[(int)dataFlow] : null;
        }

        void IMMNotificationClient.OnDeviceStateChanged(string pwstrDeviceId, DeviceState dwNewState)
        {
            AudioDevice foundDev = GetAudioDevice(pwstrDeviceId);
            if (foundDev == null)
            {
                ((IMMNotificationClient)this).OnDeviceAdded(pwstrDeviceId);
            }
            else
            {
                foundDev.UpdateState(dwNewState);
            }
        }

        void IMMNotificationClient.OnDeviceAdded(string pwstrDeviceId)
        {
            if (pwstrDeviceId == null) return;

            try
            {
                IMMDevice device = _enumerator.GetDevice(pwstrDeviceId);
                //if (device.GetState() != DeviceState.ACTIVE) return;
                AddDevice(device, out AudioDevice addedDev);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
            }
        }

        public AudioDevice GetAudioDevice(string id)
        {
            if (GetDeviceDict(EDataFlow.eRender)?.TryGetValue(id, out AudioDevice foundDev) == true ||
                GetDeviceDict(EDataFlow.eCapture)?.TryGetValue(id, out foundDev) == true) return foundDev;
            return null;
        }

        void IMMNotificationClient.OnDeviceRemoved(string pwstrDeviceId)
        {
            RemoveDevice(pwstrDeviceId);
        }

        void IMMNotificationClient.OnDefaultDeviceChanged(EDataFlow flow, ERole role, string pwstrDefaultDeviceId)
        {
        }

        void IMMNotificationClient.OnPropertyValueChanged(string pwstrDeviceId, PROPERTYKEY key)
        {
        }

        private bool AddDevice(IMMDevice device, out AudioDevice audioDevice)
        {
            EDataFlow flow = ((IMMEndpoint)device).GetDataFlow();
            AudioDevice foundDev = null;
            GetDeviceDict(flow)?.TryGetValue(device.GetId(), out foundDev);
            if (foundDev != null)
            {
                audioDevice = null;
                return false;
            }

            AudioDevice newDevice = new AudioDevice(device);
            if (GetDeviceDict(flow)?.TryAdd(newDevice.Id, newDevice) == true)
            {
                newDevice.SessionAdded += (device1, session) => { SessionAdded?.Invoke(newDevice, session); };
                newDevice.SessionRemoved += (device1, session) => { SessionRemoved?.Invoke(newDevice, session); };
                DeviceAdded?.Invoke(newDevice, flow);
                audioDevice = newDevice;
                Console.WriteLine($"New device added: {newDevice.Name} - {newDevice.Id}");
                return true;
            }

            audioDevice = null;
            return false;
        }

        private bool RemoveDevice(AudioDevice device)
        {
            if (device == null) return false;
            if (GetDeviceDict(device.Flow)?.TryRemove(device.Id, out AudioDevice removedDevice) != true) return false;
            DeviceRemoved?.Invoke(removedDevice, device.Flow);
            return true;
        }

        private bool RemoveDevice(string deviceId)
        {
            if (deviceId == null) return false;
            if (GetDeviceDict(EDataFlow.eRender)?.TryRemove(deviceId, out AudioDevice removedDev) != true &&
                GetDeviceDict(EDataFlow.eCapture)?.TryRemove(deviceId, out removedDev) != true) return false;

            DeviceRemoved?.Invoke(removedDev, removedDev.Flow);
            return true;
        }

        public object GetAudioDeviceInfo(EDataFlow flow)
        {
            return new
            {
                devices = GetDeviceDict(flow)?.Select(item => new
                {
                    item.Value.Id,
                    item.Value.Name,
                    item.Value.State,
                    item.Value.Flow
                })
            };
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
            if (string.IsNullOrEmpty(deviceId)) return deviceId;
            if (deviceId.StartsWith(MMDEVAPI_TOKEN)) deviceId = deviceId.Remove(0, MMDEVAPI_TOKEN.Length);
            if (deviceId.EndsWith(DEVINTERFACE_AUDIO_RENDER))
                deviceId = deviceId.Remove(deviceId.Length - DEVINTERFACE_AUDIO_RENDER.Length);
            if (deviceId.EndsWith(DEVINTERFACE_AUDIO_CAPTURE))
                deviceId = deviceId.Remove(deviceId.Length - DEVINTERFACE_AUDIO_CAPTURE.Length);
            return deviceId;
        }

        public bool SetPersistedDefaultAudioEndpoint(uint processId, EDataFlow dataFlow, ERole role, string deviceId)
        {
            // this crashes due to failed marshalling param, no idea what that means
            IntPtr hstring = IntPtr.Zero;

            if (!string.IsNullOrEmpty(deviceId))
            {
                string str = GenerateDeviceId(deviceId, EDataFlow.eRender);
                Combase.WindowsCreateString(str, (uint)str.Length, out hstring);
            }

            return _policyConfigFactory.SetPersistedDefaultAudioEndpoint(processId, dataFlow, role, hstring) ==
                   HRESULT.S_OK;
        }

        public string GetPersitedDefaultAudioEndpoint(uint processId, EDataFlow dataFlow, ERole role)
        {
            _policyConfigFactory.GetPersistedDefaultAudioEndpoint(processId, dataFlow, role, out string deviceId);
            return deviceId;
        }

        public bool HasSession(int processId)
        {
            return _devices[(int)EDataFlow.eRender].Any(devicePair =>
                devicePair.Value.Sessions.ContainsKey(processId) && devicePair.Value.Sessions[processId].Count > 0);
        }
    }

    public class AudioDevice : IAudioSessionNotification
    {
        private IMMDevice _device;
        private IAudioSessionManager2 _sessionManager;

        private readonly ConcurrentDictionary<int, Collection<AudioDeviceSession>> _sessions =
            new ConcurrentDictionary<int, Collection<AudioDeviceSession>>();

        private bool _isRegistered = false;

        public delegate void SessionDelegate(AudioDevice device, AudioDeviceSession session);

        public event SessionDelegate SessionAdded;
        public event SessionDelegate SessionRemoved;

        public AudioDevice(IMMDevice device)
        {
            _device = device;
            Id = device.GetId();
            State = device.GetState();
            Flow = ((IMMEndpoint)_device).GetDataFlow();

            if (_device.GetState() == DeviceState.ACTIVE)
            {
                InitializeSessions();
            }

            UpdateDeviceData();
        }

        ~AudioDevice()
        {
            _sessionManager?.UnregisterSessionNotification(this);
            _sessions.Clear();
        }

        public string Name { get; private set; }
        public string Id { get; }
        public EDataFlow Flow { get; }
        public ConcurrentDictionary<int, Collection<AudioDeviceSession>> Sessions => _sessions;
        public DeviceState State { get; private set; }

        private void UpdateDeviceData()
        {
            try
            {
                IPropertyStore propStore = _device.OpenPropertyStore(STGM.STGM_READ);
                Name = propStore.GetValue<string>(PropertyKeys.PKEY_Device_FriendlyName);
            }
            catch (Exception ex) when ((uint)ex.HResult == (uint)HRESULT.AUDCLNT_E_DEVICE_INVALIDATED)
            {
                // Ignore
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
            }
        }

        public void OnSessionCreated(IAudioSessionControl sessionControl)
        {
            AudioDeviceSession newSession = new AudioDeviceSession(sessionControl);
            if (newSession.State == AudioSessionState.Expired) return;
            newSession.PropertyChanged += Session_PropertyChanged;

            Collection<AudioDeviceSession> collection =
                _sessions.GetOrAdd(newSession.ProcessId, (key) => new Collection<AudioDeviceSession>());
            collection.Add(newSession);
            Console.WriteLine($"Session created: {newSession.Id} for process: {newSession.ProcessId}");
            SessionAdded?.Invoke(this, newSession);
        }

        private void Session_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            AudioDeviceSession session = sender as AudioDeviceSession;
            if (session == null) return;
            if (e.PropertyName == nameof(session.State))
            {
                if (session.State == AudioSessionState.Expired)
                {
                    RemoveSession(session);
                }
            }
        }

        private void RemoveSession(AudioDeviceSession session)
        {
            session.PropertyChanged -= Session_PropertyChanged;
            SessionRemoved?.Invoke(this, session);
            if (_sessions.TryGetValue(session.ProcessId, out Collection<AudioDeviceSession> collection))
            {
                collection.Remove(session);
                Console.WriteLine($"Session removed: {session.Id} for process: {session.ProcessId}");
            }
        }

        public bool HasSession(int processId)
        {
            return _sessions.ContainsKey(processId) && _sessions[processId].Count > 0;
        }

        private void InitializeSessions()
        {
            ClearSessions();

            if (_sessionManager == null)
            {
                _sessionManager = _device.Activate<IAudioSessionManager2>();
            }

            if (!_isRegistered)
            {
                _sessionManager.RegisterSessionNotification(this);
                _isRegistered = true;
            }

            IAudioSessionEnumerator sessionEnumerator = _sessionManager.GetSessionEnumerator();
            int count = sessionEnumerator.GetCount();
            for (int i = 0; i < count; i++)
            {
                OnSessionCreated(sessionEnumerator.GetSession(i));
            }
        }

        private void ClearSessions()
        {
            if (_sessionManager != null && _isRegistered)
            {
                _sessionManager.UnregisterSessionNotification(this);
                _isRegistered = false;
            }

            foreach (KeyValuePair<int, Collection<AudioDeviceSession>> sessionPair in _sessions)
            {
                for (int i = sessionPair.Value.Count - 1; i >= 0; i--)
                {
                    RemoveSession(sessionPair.Value[i]);
                }
            }

            _sessions.Clear();
        }

        public void UpdateState(DeviceState newState)
        {
            State = newState;
            switch (State)
            {
                case DeviceState.ACTIVE:
                {
                    InitializeSessions();
                    break;
                }
                case DeviceState.DISABLED:
                case DeviceState.NOTPRESENT:
                case DeviceState.UNPLUGGED:
                {
                    ClearSessions();
                    break;
                }
                default:
                    break;
            }
        }
    }
}