using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AppAudioSwitcherUtility.Audio;

namespace AppAudioSwitcherUtility.Server.Messages
{
    public class DevicesMessageRequest : IMessage
    {
        public EDataFlow DataFlow { get; }
        
        public DevicesMessageRequest(EDataFlow dataFlow)
        {
            DataFlow = dataFlow;
        }
    }

    public class DevicesMessageResponse : IMessage
    {
        public readonly struct DeviceInfo
        {
            public string DeviceId { get; }
            public string DeviceName { get; }
            public DeviceState State { get; }
            public EDataFlow DataFlow { get; }
            
            public DeviceInfo(string deviceId, string deviceName, DeviceState deviceState,
                EDataFlow dataFlow)
            {
                DeviceId = deviceId;
                DeviceName = deviceName;
                State = deviceState;
                DataFlow = dataFlow;
            }
        }
        
        public DeviceInfo[] Devices { get; }
        public DevicesMessageResponse() { }
        public DevicesMessageResponse(DeviceInfo[] devices)
        {
            Devices = devices;
        }

    }

    public class DeviceInfoHandler : IMessageHandler<DevicesMessageRequest>
    {
        public Task<IMessage> HandleAsync(DevicesMessageRequest message)
        {
            IReadOnlyDictionary<string, AudioDevice> deviceDict =
                Services.DeviceManager.GetDeviceDictReadOnly(message.DataFlow);
            DevicesMessageResponse response = deviceDict != null
                ? new DevicesMessageResponse(
                    deviceDict.Select(item => new DevicesMessageResponse.DeviceInfo(
                        item.Value.Id,
                        item.Value.Name,
                        item.Value.State,
                        item.Value.Flow)).ToArray())
                : new DevicesMessageResponse();
            return Task.FromResult<IMessage>(response);
        }
    }
}