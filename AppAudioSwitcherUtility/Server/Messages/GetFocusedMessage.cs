using System.Threading.Tasks;
using AppAudioSwitcherUtility.Audio;
using AppAudioSwitcherUtility.Process;

namespace AppAudioSwitcherUtility.Server.Messages
{
    public readonly struct FocusedMessageRequest : IMessage
    {
        public FocusedMessageRequest(bool icon, uint? processId = null)
        {
            Icon = icon;
            ProcessId = processId;
        }
        public bool Icon { get; }
        public uint? ProcessId { get;  }
        public MessageType MessageType => MessageType.GetFocusedRequest;
    }
    
    public readonly struct FocusedMessageResponse : IMessage
    {
        public FocusedMessageResponse(uint processId, string processName, string deviceId, bool hasSession, string processIconBase64)
        {
            ProcessId = processId;
            ProcessName = processName;
            DeviceId = deviceId;
            HasSession = hasSession;
            ProcessIconBase64 = processIconBase64; 
        }

        public uint ProcessId { get; }
        public string ProcessName { get; }
        public string DeviceId { get; }
        public bool HasSession { get; }
        public string ProcessIconBase64 { get; }
        public MessageType MessageType => MessageType.GetFocusedResponse;
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

            FocusedMessageResponse response = new FocusedMessageResponse(processId,
                ProcessUtilities.GetFriendlyName((int)processId), AudioDeviceManager.UnpackDeviceId(deviceId),
                Services.DeviceManager.HasSession((int)processId),
                message.Icon ? ProcessIconExtractor.GetBase64IconFromProcess((int)processId) : null);
            return Task.FromResult<IMessage>(response);
        }
    }
}