using UnityEngine;
using UniRx;
using UniRx.Triggers;
using System;
using System.Linq;
using System.Collections;


namespace socket.io {

    /// <summary>
    /// Streams out received packets as observable
    /// </summary>
    public class WebSocketTrigger : ObservableTriggerBase {

        /// <summary>
        /// Observes received packets and also starts Ping-Pong routine
        /// </summary>
        /// <returns></returns>
        public UniRx.IObservable<string> OnRecvAsObservable() {
            if (_cancelPingPong == null) {
                _cancelPingPong = gameObject.UpdateAsObservable()
                    .Sample(TimeSpan.FromSeconds(10f))
                    .Subscribe(_ => {
                        WebSocket.Send(Packet.Ping);
                        Debug.LogFormat("socket.io => {0} ping~", WebSocket.Url.ToString());
                    });
            }

            if (_onRecv == null)
                _onRecv = new Subject<string>();

            return _onRecv;
        }
        
        protected override void RaiseOnCompletedOnDestroy() {
            if (_cancelPingPong != null) {
                _cancelPingPong.Dispose();
                _cancelPingPong = null;
            }

            if (_onRecv != null) {
                _onRecv.OnCompleted();
                _onRecv = null;
            }

            if (!IsConnected)
                WebSocket.Close();
        }

        IDisposable _cancelPingPong;
        Subject<string> _onRecv;

        /// <summary>
        /// WebSocket object ref
        /// </summary>
        public WebSocketWrapper WebSocket { get; set; }

        /// <summary>
        /// Holds the last error on WebSocket
        /// </summary>
        public string LastWebSocketError { get; private set; }

        public bool IsConnected {
            get { return WebSocket != null && WebSocket.IsConnected; }
        }

        public bool IsProbed { get; set; }

        public bool IsUpgraded { get; set; }
        

        void Update() {
            LastWebSocketError = WebSocket.GetLastError();

            if (!string.IsNullOrEmpty(LastWebSocketError)) {
                CheckAndHandleWebSocketDisconnect();
                Debug.LogError(LastWebSocketError);
            }

            if (IsConnected)
                ReceiveWebSocketData();
        }

        void ReceiveWebSocketData() {
            var recvData = WebSocket.Recv();

            if (string.IsNullOrEmpty(recvData))
                return;

            if (recvData == Packet.ProbeAnswer) {
                IsProbed = true;
                Debug.LogFormat("socket.io => {0} probed~", WebSocket.Url.ToString());
            }
            else if (recvData == Packet.Pong) {
                Debug.LogFormat("socket.io => {0} pong~", WebSocket.Url.ToString());
            }
            else {
                if (_onRecv != null)
                    _onRecv.OnNext(recvData);
            }
        }
        
        void CheckAndHandleWebSocketDisconnect() {
            if (IsConnected)
                return;

            if (_onRecv != null) {
                _cancelPingPong.Dispose();
                _cancelPingPong = null;
                _onRecv.Dispose();
                _onRecv = null;
                IsProbed = false;
                IsUpgraded = false;

                var sockets = gameObject.GetComponentsInChildren<Socket>();
                foreach (var s in sockets) {
                    if (s.OnDisconnect != null)
                        s.OnDisconnect();
                }
            }
            
            if (SocketManager.Instance.Reconnection) {
                var sockets = gameObject.GetComponentsInChildren<Socket>();
                foreach (var s in sockets)
                    SocketManager.Instance.Reconnect(s, 1);
            }
        }
        
    }

}