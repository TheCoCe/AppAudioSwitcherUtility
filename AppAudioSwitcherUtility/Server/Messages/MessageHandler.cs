using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace AppAudioSwitcherUtility.Server.Messages
{
    public interface IMessage
    {
        MessageType MessageType { get; }
    }

    public interface IMessageHandler<in TMessage> where TMessage : struct, IMessage
    {
        Task<IMessage> HandleAsync(TMessage message);
    }
    
    public readonly struct InvalidMessage : IMessage
    {
        public MessageType MessageType => MessageType.Invalid;
    }

    public enum MessageType
    {
        Invalid,
        GetDevicesRequest,
        GetDevicesResponse,
        GetFocusedRequest,
        GetFocusedResponse,
        SetAppDeviceRequest,
        SetAppDeviceResponse,
    }
    
    public struct PluginMessage
    {
        public PluginMessage(MessageType messageType, JsonElement payload)
        {
            Type = messageType;
            Payload = payload;
        }
        
        public MessageType Type { get; set; }
        public JsonElement Payload { get; set; }

        public static PluginMessage FromMessage<TMessage>(TMessage message) where TMessage : IMessage
        {
            return new PluginMessage(message.MessageType, JsonSerializer.SerializeToElement((object)message));
        }
    }
    
    public interface IMessageHandler
    {
        MessageType MessageType { get; }
        Type PayloadType { get; }
        Task<JsonElement?> HandleMessageAsync(JsonElement payload);
    }

    public class MessageRouter
    {
        private readonly Dictionary<Type, Func<IMessage, Task<IMessage>>> _handlers =
            new Dictionary<Type, Func<IMessage, Task<IMessage>>>();
        private readonly Dictionary<MessageType, Type> _messageTypes = new Dictionary<MessageType, Type>();
        
        public void RegisterHandler<TMessage>(IMessageHandler<TMessage> handler) where TMessage : struct, IMessage
        {
            _handlers[typeof(TMessage)] = (msgBase) =>
            {
                TMessage typed = (TMessage)msgBase;
                return handler.HandleAsync(typed);
            };
        }

        public void RegisterMessageType<TMessage>() where TMessage : struct, IMessage
        {
            TMessage temp = default(TMessage);
            MessageType messageType = temp.MessageType;
            _messageTypes[messageType] = typeof(TMessage);
        }

        private Func<IMessage, Task<IMessage>> GetHandler(Type messageType)
        {
            return _handlers[messageType];
        }

        private Type GetMessageType(MessageType messageType)
        {
            return _messageTypes[messageType];
        }
        
        public Task<IMessage> HandleAsync(PluginMessage message)
        {
            Type targeType = GetMessageType(message.Type);
            if (targeType == null)
            {
                throw new InvalidOperationException("Cannot handle message of type " + message.Type);
            }
            
            IMessage messageTyped = (IMessage)JsonSerializer.Deserialize(message.Payload.ToString(), targeType);
            if (messageTyped == null)
            {
                throw new InvalidOperationException($"Failed to cast message to {targeType}, the message payload might be malformed for the type {message.Type}");
            }
            
            return RouteAsync(messageTyped);
        }

        private Task<IMessage> RouteAsync(IMessage message)
        {
            Type msgType = message.GetType();
            Func<IMessage, Task<IMessage>> handler = GetHandler(msgType);
            return handler == null ? throw new Exception($"Missing handler for {msgType}") : handler(message);
        }
    }
}