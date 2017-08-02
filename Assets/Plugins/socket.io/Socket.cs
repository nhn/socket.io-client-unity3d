using UnityEngine;
using System;
using System.Collections.Generic;

namespace socket.io {

    /// <summary>
    /// socket.io interface component
    /// </summary>
    public class Socket : MonoBehaviour {

        /// <summary>
        /// Establishes a connection to a given url
        /// </summary>
        /// <param name="url"> The URL of the remote host </param>
        /// <returns></returns>
        public static Socket Connect(string url) {
            var socket = new GameObject(string.Format("socket.io - {0}", url)).AddComponent<Socket>();
            socket.transform.SetParent(SocketManager.Instance.transform, false);
            socket.Url = new Uri(url);

            SocketManager.Instance.Connect(socket);
            return socket;
        }

        #region On/Off methods

        public void On(string eventName, Action<string> callback) {
            if (SystemEventHelper.IsSystemEvent(eventName)) {
                Debug.LogErrorFormat("{0} is reserved as the system event and not allowed to use", eventName);
                return;
            }

            if (!_handlers.ContainsKey(eventName))
                _handlers.Add(eventName, callback);
            else
                _handlers[eventName] = callback;
        }

        public void On(string eventName, Action callback) {
            On(SystemEventHelper.FromString(eventName), callback);
        }

        public void On(string eventName, Action<int> callback) {
            On(SystemEventHelper.FromString(eventName), callback);
        }

        public void On(string eventName, Action<Exception> callback) {
            On(SystemEventHelper.FromString(eventName), callback);
        }

        public void Off(string eventName) {
            var @event = SystemEventHelper.FromString(eventName);
            if (@event != SystemEvents.Max) {
                Off(@event);
            }
            else {
                if (!_handlers.ContainsKey(eventName))
                    Debug.LogErrorFormat("{0} is not assigned anywhere (Maybe mistyping eventName??)", eventName);
                else
                    _handlers.Remove(eventName);
            }
        }

        public void On(SystemEvents @event, Action callback) {
            switch (@event) {
                case SystemEvents.connect:
                    OnConnect = callback;
                    break;

                case SystemEvents.connectTimeOut:
                    OnConnectTimeOut = callback;
                    break;

                case SystemEvents.reconnectAttempt:
                    OnReconnectAttempt = callback;
                    break;

                case SystemEvents.reconnectFailed:
                    OnReconnectFailed = callback;
                    break;

                case SystemEvents.disconnect:
                    OnDisconnect = callback;
                    break;

                default:
                    Debug.LogErrorFormat("{0} event can not be handled by Action callback (Try the other On() methods)", @event.ToString());
                    break;
            }
        }

        public void On(SystemEvents @event, Action<int> callback) {
            switch (@event) {
                case SystemEvents.reconnect:
                    OnReconnect = callback;
                    break;

                case SystemEvents.reconnecting:
                    OnReconnecting = callback;
                    break;

                default:
                    Debug.LogErrorFormat("{0} event can not be handled by Action<int> callback (Try the other On() methods)", @event.ToString());
                    break;
            }
        }

        public void On(SystemEvents @event, Action<Exception> callback) {
            switch (@event) {
                case SystemEvents.connectError:
                    OnConnectError = callback;
                    break;

                case SystemEvents.reconnectError:
                    OnReconnectError = callback;
                    break;

                default:
                    Debug.LogErrorFormat("{0} event can not be handled by Action<Exception> callback (Try the other On() methods)", @event.ToString());
                    break;
            }
        }

        public void Off(SystemEvents @event) {
            switch (@event) {
                case SystemEvents.connect:
                    OnConnect = null;
                    break;

                case SystemEvents.connectTimeOut:
                    OnConnectTimeOut = null;
                    break;

                case SystemEvents.reconnectAttempt:
                    OnReconnectAttempt = null;
                    break;

                case SystemEvents.reconnectFailed:
                    OnReconnectFailed = null;
                    break;

                case SystemEvents.disconnect:
                    OnDisconnect = null;
                    break;

                case SystemEvents.reconnect:
                    OnReconnect = null;
                    break;

                case SystemEvents.reconnecting:
                    OnReconnecting = null;
                    break;

                case SystemEvents.connectError:
                    OnConnectError = null;
                    break;

                case SystemEvents.reconnectError:
                    OnReconnectError = null;
                    break;

                default:
                    Debug.LogErrorFormat("{0} is not a system event", @event.ToString());
                    break;
            }
        }

        #endregion

        #region Emit methods

        public void Emit(string eventName) {
            Emit(eventName, string.Empty, null);
        }

        public void Emit(string eventName, string data) {
            Emit(eventName, data, null);
        }

        public void EmitJson(string eventName, string jsonData) {
            EmitJson(eventName, jsonData, null);
        }

        public void Emit(string eventName, string data, Action<string> ack) {
            if (WebSocketTrigger == null)
                return;

            if (ack != null) {
                var pkt = data.Length > 0 ?
                    new Packet(EnginePacketTypes.MESSAGE, SocketPacketTypes.EVENT, ++_idGenerator, Namespace, string.Format(@"[""{0}"",""{1}""]", eventName, data)) :
                    new Packet(EnginePacketTypes.MESSAGE, SocketPacketTypes.EVENT, ++_idGenerator, Namespace, string.Format(@"[""{0}""]", eventName));

                WebSocketTrigger.WebSocket.Send(pkt.Encode());
                _acks.Add(pkt.id, ack);
            }
            else {
                var pkt = data.Length > 0 ?
                    new Packet(EnginePacketTypes.MESSAGE, SocketPacketTypes.EVENT, Namespace, string.Format(@"[""{0}"",""{1}""]", eventName, data)) :
                    new Packet(EnginePacketTypes.MESSAGE, SocketPacketTypes.EVENT, Namespace, string.Format(@"[""{0}""]", eventName));

                WebSocketTrigger.WebSocket.Send(pkt.Encode());
            }
        }

        public void EmitJson(string eventName, string jsonData, Action<string> ack) {
            if (WebSocketTrigger == null)
                return;

            if (ack != null) {
                var pkt = jsonData.Length > 0 ?
                    new Packet(EnginePacketTypes.MESSAGE, SocketPacketTypes.EVENT, ++_idGenerator, Namespace, string.Format(@"[""{0}"",{1}]", eventName, jsonData)) :
                    new Packet(EnginePacketTypes.MESSAGE, SocketPacketTypes.EVENT, ++_idGenerator, Namespace, string.Format(@"[""{0}""]", eventName));

                WebSocketTrigger.WebSocket.Send(pkt.Encode());
                _acks.Add(pkt.id, ack);
            }
            else {
                var pkt = jsonData.Length > 0 ?
                    new Packet(EnginePacketTypes.MESSAGE, SocketPacketTypes.EVENT, Namespace, string.Format(@"[""{0}"",{1}]", eventName, jsonData)) :
                    new Packet(EnginePacketTypes.MESSAGE, SocketPacketTypes.EVENT, Namespace, string.Format(@"[""{0}""]", eventName));

                WebSocketTrigger.WebSocket.Send(pkt.Encode());
            }
        }

        /// <summary>
        /// The unique value generator for acks message id.
        /// </summary>
        int _idGenerator = -1;

        #endregion

        #region Event handlers

        public Action OnConnect { get; private set; }
        public Action OnConnectTimeOut { get; private set; }
        public Action OnReconnectAttempt { get; private set; }
        public Action OnReconnectFailed { get; private set; }
        public Action OnDisconnect { get; private set; }
        public Action<int> OnReconnect { get; private set; }
        public Action<int> OnReconnecting { get; private set; }
        public Action<Exception> OnConnectError { get; private set; }
        public Action<Exception> OnReconnectError { get; private set; }

        readonly Dictionary<int, Action<string>> _acks = new Dictionary<int, Action<string>>();
        readonly Dictionary<string, Action<string>> _handlers = new Dictionary<string, Action<string>>();

        #endregion

        /// <summary>
        /// The URL of the remote host which socket connected
        /// </summary>
        public Uri Url { get; private set; }
        
        /// <summary>
        /// Namespace ("/" is the default namespace which means global namespace.)
        /// </summary>
        public string Namespace {
            get { return (Url != null) ? Url.AbsolutePath : "/"; }
        }

        public bool HasNamespace {
            get { return Namespace != "/"; }
        }

        public bool IsConnected {
            get {
                return (WebSocketTrigger != null &&
                    WebSocketTrigger.WebSocket != null &&
                    WebSocketTrigger.WebSocket.IsConnected &&
                    WebSocketTrigger.IsUpgraded
                    );
            }
        }

        protected WebSocketTrigger WebSocketTrigger {
            get {
                if (_webSocketTrigger == null && transform.parent != null)
                    _webSocketTrigger = transform.parent.GetComponent<WebSocketTrigger>();

                return _webSocketTrigger;
            }
        }

        WebSocketTrigger _webSocketTrigger;
        
        public void OnRecvWebSocketPacket(string pkt) {
            if (pkt == Packet.ProbeAnswer)
                return;

            DispatchPacket(pkt.Decode());
        }

        void DispatchPacket(Packet pkt) {
            if (pkt.nsp != Namespace)
                return;

            if (pkt.enginePktType != EnginePacketTypes.MESSAGE)
                return;
            
            switch (pkt.socketPktType) {
                case SocketPacketTypes.ACK:
                    if (!pkt.HasId) {
                        Debug.LogWarningFormat("{0} has no id", pkt.ToString());
                        return;
                    }

                    _acks[pkt.id](pkt.body);
                    _acks.Remove(pkt.id);
                    break;

                case SocketPacketTypes.EVENT:
                    if (!pkt.HasBody) {
                        Debug.LogWarningFormat("{0} has no body(data)", pkt.ToString());
                        return;
                    }

                    var seperateIndex = pkt.body.IndexOf(", ");

                    var seperatorLen = 2;
                    if (seperateIndex == -1) {
                        seperateIndex = pkt.body.IndexOf(',');
                        seperatorLen = 1;
                    }

                    var eventName = pkt.body.Substring(2, seperateIndex - 3);
                    if (!_handlers.ContainsKey(eventName)) {
                        Debug.LogWarningFormat("{0} event doesn't have a handler", eventName);
                        break;
                    }

                    var data = pkt.body.Substring(seperateIndex + seperatorLen, pkt.body.Length - seperateIndex - seperatorLen - 1);
                    _handlers[eventName](data);
                    break;

                default:
                    break;
            }
        }

    }

}