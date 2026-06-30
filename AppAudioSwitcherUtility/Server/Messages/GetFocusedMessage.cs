using System.Threading.Tasks;
using AppAudioSwitcherUtility.Audio;
using AppAudioSwitcherUtility.Process;

namespace AppAudioSwitcherUtility.Server.Messages
{
    public class FocusedMessageRequest : IMessage
    {
        public FocusedMessageRequest(bool icon, uint? processId = null)
        {
            Icon = icon;
            ProcessId = processId;
        }
        public bool Icon { get; }
        public uint? ProcessId { get;  }
    }
    
    public class FocusedMessageResponse : IMessage
    {
        public uint ProcessId { get; set; }
        public string ProcessName { get; set; }
        public string DeviceId { get; set; }
        public bool HasSession { get; set; }
        public string ProcessIconBase64 { get; set; }
    }

    public class FocusedMessageHandler : IMessageHandler<FocusedMessageRequest>
    {
        public Task<IMessage> HandleAsync(FocusedMessageRequest message)
        {
            uint processId = message.ProcessId ?? ProcessUtilities.GetForegroundWindowProcessId();
            string deviceId = Services.DeviceManager.GetPersitedDefaultAudioEndpoint(processId, EDataFlow.eRender, ERole.eMultimedia);
            if (string.IsNullOrEmpty(deviceId))
            {
                deviceId = Services.DeviceManager.GetPersitedDefaultAudioEndpoint(processId, EDataFlow.eRender, ERole.eConsole);
            }

            FocusedMessageResponse response = new FocusedMessageResponse
            {
                ProcessId = processId,
                ProcessName = ProcessUtilities.GetFriendlyName((int)processId), 
                DeviceId = AudioDeviceManager.UnpackDeviceId(deviceId),
                HasSession = Services.DeviceManager.HasSession((int)processId),
                ProcessIconBase64 = message.Icon ? ProcessIconExtractor.GetBase64IconFromProcess((int)processId) : null
            };
            return Task.FromResult<IMessage>(response);
        }
    }
}