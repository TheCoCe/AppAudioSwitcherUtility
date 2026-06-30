using System.Threading.Tasks;
using AppAudioSwitcherUtility.Audio;

namespace AppAudioSwitcherUtility.Server.Messages
{
    public class SetAppDeviceMessageRequest : IMessage
    {
        public SetAppDeviceMessageRequest(uint processId, string deviceId)
        {
            ProcessId = processId;
            DeviceId = deviceId;
        }

        public uint ProcessId { get; }
        public string DeviceId { get; }
    }
    
    public class SetAppDeviceMessageResponse : IMessage
    {
        public SetAppDeviceMessageResponse(bool success)
        {
            Success = success;
        }

        public bool Success { get; }
    }

    public class SetAppDeviceMessageHandler : IMessageHandler<SetAppDeviceMessageRequest>
    {
        public Task<IMessage> HandleAsync(SetAppDeviceMessageRequest message)
        {
            bool bConsoleSuccess = Services.DeviceManager.SetPersistedDefaultAudioEndpoint(message.ProcessId,
                EDataFlow.eRender, ERole.eConsole, message.DeviceId);
            bool bMultimediaSuccess = Services.DeviceManager.SetPersistedDefaultAudioEndpoint(message.ProcessId,
                EDataFlow.eRender, ERole.eMultimedia, message.DeviceId);

            return Task.FromResult<IMessage>(new SetAppDeviceMessageResponse(bConsoleSuccess || bMultimediaSuccess));
        }
    }
}