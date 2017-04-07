using UnityEngine;
using WebSocketSharp;
using System;
using System.Collections.Generic;

namespace socket.io {

    public class Socket : MonoBehaviour {

        /// <summary>
        /// 네임스페이스 ("/"은 기본값으로 글로벌 네임스페이스임)
        /// </summary>
        public string nsp = "/";

        public bool HasNamespace {
            get { return nsp != "/"; }
        }

        public static Socket Connect(string url) {
            return SocketManager.Instance.Connect(url);
        }

        public WebSocketTrigger WebSocketTrigger {
            get {
                if (_webSocketTrigger == null && transform.parent != null)
                    _webSocketTrigger = transform.parent.GetComponent<WebSocketTrigger>();

                return _webSocketTrigger;
            }
        }

        WebSocketTrigger _webSocketTrigger;

        public bool IsConnected {
            get {
                return (WebSocketTrigger != null && 
                    WebSocketTrigger.WebSocket != null&& 
                    WebSocketTrigger.WebSocket.IsConnected && 
                    WebSocketTrigger.IsUpgraded
                    );
            }
        }
        
        public void OnRecvWebSocketEvent(string data) {
            if (data != Packet.ProbeAnswer)
                DispatchPacket(data.Decode());
        }

        void DispatchPacket(Packet pkt) {
            if (pkt.nsp != nsp)
                return;

            if (pkt.enginePktType == EnginePacketTypes.MESSAGE) {
                if (pkt.socketPktType == SocketPacketTypes.CONNECT) {
                    if (onConnect != null)
                        onConnect();
                }
                else if (pkt.socketPktType == SocketPacketTypes.DISCONNECT) {
                    Debug.Log("Socket disconnected");
                }
                else if (pkt.socketPktType == SocketPacketTypes.ACK) {
                    Debug.Assert(pkt.HasId && pkt.HasBody);

                    _acks[pkt.id](pkt.body);
                    _acks.Remove(pkt.id);
                }
                else if (pkt.socketPktType == SocketPacketTypes.EVENT) {
                    Debug.Assert(pkt.HasBody);

                    var seperateIndex = pkt.body.IndexOf(", ");
                    var seperatorLen = 2;
                    if (seperateIndex == -1) {
                        seperateIndex = pkt.body.IndexOf(',');
                        seperatorLen = 1;
                    }

                    var evtName = pkt.body.Substring(2, seperateIndex - 3);

                    if (_evtHandlers.ContainsKey(evtName)) {
                        var data = pkt.body.Substring(seperateIndex + seperatorLen, pkt.body.Length - seperateIndex - seperatorLen - 1);
                        _evtHandlers[evtName](data);
                    }
                }
            }
            //else if (pkt.enginePktType == EnginePacketTypes.PONG) {}
            //else {}
        }

        readonly Dictionary<int, Action<string>> _acks = new Dictionary<int, Action<string>>();

        /// <summary>
        /// Acks를 받기 위한 패킷 아이디 할당용 카운팅 필드
        /// </summary>
        int _idGenerator = -1;

        public void Emit(string evtName, string data, Action<string> ack) {
            if (WebSocketTrigger == null)
                return;

            if (ack != null) {
                var pkt = new Packet(EnginePacketTypes.MESSAGE, SocketPacketTypes.EVENT, ++_idGenerator, nsp, string.Format(@"[""{0}"",{1}]", evtName, data));
                WebSocketTrigger.WebSocket.Send(pkt.Encode());

                _acks.Add(pkt.id, ack);
            }
            else {
                if (data.Length > 0) {
                    var pkt = new Packet(EnginePacketTypes.MESSAGE, SocketPacketTypes.EVENT, nsp, string.Format(@"[""{0}"",{1}]", evtName, data));
                    WebSocketTrigger.WebSocket.Send(pkt.Encode());
                }
                else {
                    var pkt = new Packet(EnginePacketTypes.MESSAGE, SocketPacketTypes.EVENT, nsp, string.Format(@"[""{0}""]", evtName));
                    WebSocketTrigger.WebSocket.Send(pkt.Encode());
                }
            }
        }

        public void Emit(string evtName, string data) {
            Emit(evtName, data, null);
        }

        public void Emit(string evtName) {
            Emit(evtName, string.Empty, null);
        }

        #region 시스템 이벤트 핸들러
        public Action onConnect;
        public Action onConnectTimeOut;
        public Action onReconnectAttempt;
        public Action onReconnectFailed;
        public Action<int> onReconnect;
        public Action<int> onReconnecting;
        public Action<Exception> onConnectError;
        public Action<Exception> onReconnectError;
        #endregion

        readonly Dictionary<string, Action<string>> _evtHandlers = new Dictionary<string, Action<string>>();

        public void On(string evtName, Action<string> callback) {
            if (evtName == "connect" ||
            evtName == "connectTimeOut" ||
            evtName == "reconnectAttempt" ||
            evtName == "reconnectFailed" ||
            evtName == "reconnect" ||
            evtName == "reconnecting" ||
            evtName == "connectError" ||
            evtName == "reconnectError") {
                Debug.LogErrorFormat("{0} is reserved for system events :(", evtName);
                return;
            }

            if (!_evtHandlers.ContainsKey(evtName))
                _evtHandlers.Add(evtName, callback);
            else
                _evtHandlers[evtName] = callback;
        }

        public void On(string evtName, Action callback) {
            if (evtName == "connect")
                onConnect = callback;
            else if (evtName == "connectTimeOut")
                onConnectTimeOut = callback;
            else if (evtName == "reconnectAttempt")
                onReconnectAttempt = callback;
            else if (evtName == "reconnectFailed")
                onReconnectFailed = callback;
            else
                Debug.Assert(false);
        }

        public void On(string evtName, Action<int> callback) {
            if (evtName == "reconnect")
                onReconnect = callback;
            else if (evtName == "reconnecting")
                onReconnecting = callback;
            else
                Debug.Assert(false);
        }

        public void On(string evtName, Action<Exception> callback) {
            if (evtName == "connectError")
                onConnectError = callback;
            else if (evtName == "reconnectError")
                onReconnectError = callback;
            else
                Debug.Assert(false);
        }

        public void Off(string evtName) {
            if (evtName == "connect")
                onConnect = null;
            else if (evtName == "connectTimeOut")
                onConnectTimeOut = null;
            else if (evtName == "reconnectAttempt")
                onReconnectAttempt = null;
            else if (evtName == "reconnectFailed")
                onReconnectFailed = null;
            else if (evtName == "reconnect")
                onReconnect = null;
            else if (evtName == "reconnecting")
                onReconnecting = null;
            else if (evtName == "connectError")
                onConnectError = null;
            else if (evtName == "reconnectError")
                onReconnectError = null;
            else {
                if (!_evtHandlers.ContainsKey(evtName))
                    return;

                _evtHandlers.Remove(evtName);
            }
        }

    }

}