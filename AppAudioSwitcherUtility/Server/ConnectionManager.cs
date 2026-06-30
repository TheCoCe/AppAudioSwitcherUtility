using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace AppAudioSwitcherUtility.Server
{
    public class ConnectionManager
    {
        public class ConnectionInfo
        {
            public ConnectionInfo(Guid id, WebSocket webSocket)
            {
                Id = id;
                WebSocket = webSocket;
            }
            
            public Guid Id { get; }
            public WebSocket WebSocket { get; }
        }
        
        private readonly ConcurrentDictionary<Guid, ConnectionInfo> _connections = new ConcurrentDictionary<Guid, ConnectionInfo>();

        public ConnectionInfo AddSocket(WebSocket socket)
        {
            ConnectionInfo connectionInfo = new ConnectionInfo(Guid.NewGuid(), socket);
            _connections.TryAdd(connectionInfo.Id, connectionInfo);
            return connectionInfo;
        }

        public async Task RemoveSocketAsync(Guid id)
        {
            if (_connections.TryRemove(id, out ConnectionInfo connectionInfo))
            {
                await connectionInfo.WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed", CancellationToken.None);
            }
        }

        public IEnumerable<ConnectionInfo> Connections => _connections.Values;
    }
}