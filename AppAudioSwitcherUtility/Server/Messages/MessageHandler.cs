using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace AppAudioSwitcherUtility.Server.Messages
{
    public interface IMessage { }

    public interface IMessageHandler<in TMessage> where TMessage : IMessage
    {
        Task<IMessage> HandleAsync(TMessage message);
    }
    
    public class MessageHandlerAdapter<TMessage> : IMessageHandler
        where TMessage : IMessage
    {
        private readonly IMessageHandler<TMessage> _inner;

        public MessageHandlerAdapter(IMessageHandler<TMessage> inner)
        {
            _inner = inner;
        }

        public Task<IMessage> HandleMessageAsync(IMessage message)
        {
            // Safe cast because router ensures correct type
            return _inner.HandleAsync((TMessage)message);
        }
    }
    
    public class PluginMessage
    {
        public PluginMessage() { }
        public PluginMessage(string messageType, JsonElement payload)
        {
            Type = messageType;
            Payload = payload;
        }

        public PluginMessage(IMessage message)
        {
            Type = message.GetType().Name;
            Payload = JsonSerializer.SerializeToElement((object)message);
        }
        
        public string Type { get; set; }
        public JsonElement Payload { get; set; }
    }
    
    public interface IMessageHandler
    {
        Task<IMessage> HandleMessageAsync(IMessage payload);
    }

    public class MessageRouter
    {
        private readonly Dictionary<Type, IMessageHandler> _handlers = new Dictionary<Type, IMessageHandler>();

        public MessageRouter()
        {
            RegisterHandlers();
        }

        private void RegisterHandlers()
        {
            IEnumerable<Type> handlerTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => !t.IsInterface && !t.IsAbstract && t.GetInterfaces().Any(i =>
                    i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IMessageHandler<>)));

            foreach (Type handlerType in handlerTypes)
            {
                Type interfaceType = handlerType.GetInterfaces().First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IMessageHandler<>));
                Type messageType = interfaceType.GetGenericArguments()[0];
                object handlerInstance = Activator.CreateInstance(handlerType);
                Type adapterType = typeof(MessageHandlerAdapter<>).MakeGenericType(messageType);
                IMessageHandler messageHandler = (IMessageHandler)Activator.CreateInstance(adapterType, handlerInstance);
                _handlers[messageType] = messageHandler;
            }
        }

        private IMessage DeserializeMessage(PluginMessage message)
        {
            Type targeType = _handlers.Keys.First(t => t.Name == message.Type);
            if (targeType == null)
            {
                throw new InvalidOperationException($"No handler found handling {message.Type}");
            }
            
            return (IMessage)message.Payload.Deserialize(targeType);
        }
        
        public Task<IMessage> HandleAsync(PluginMessage message)
        {
            IMessage messageTyped = DeserializeMessage(message);
            if (messageTyped == null)
            {
                throw new InvalidOperationException($"Failed to deserialize message {message.Type}");
            }
            
            IMessageHandler handler = _handlers[messageTyped.GetType()];
            if (handler == null)
            {
                throw new InvalidOperationException($"No handler found for {message.Type}");
            }

            return handler.HandleMessageAsync(messageTyped);
        }
    }
}