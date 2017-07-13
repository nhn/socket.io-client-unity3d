using UnityEngine;
using UniRx;
using UniRx.Triggers;
using System;
using System.Linq;
using System.Collections;


namespace socket.io {

    /// <summary>
    /// Stream-out received packet data as observable
    /// </summary>
    public class WebSocketTrigger : ObservableTriggerBase {

        public IObservable<string> OnRecvAsObservable(string url) {
            StartCoroutine(PingPong());

            if (_onRecv == null)
                _onRecv = new Subject<string>();

            return _onRecv;
        }

        protected override void RaiseOnCompletedOnDestroy() {
            if (_onRecv != null) {
                _onRecv.OnCompleted();
                _onRecv = null;
            }

            if (!IsConnected)
                WebSocket.Close();
        }

        Subject<string> _onRecv;

        /// <summary>
        /// Ping-Pong coroutine to keep connection alive.
        /// </summary>
        /// <returns></returns>
        IEnumerator PingPong() {
            while (WebSocket.IsConnected) {
                yield return new WaitForSeconds(10f);
                WebSocket.Send(Packet.Ping);

                Debug.LogFormat("socket.io => {0} ping~", WebSocket.Url.ToString());
            }
        }

        public WebSocketWrapper WebSocket { get; set; }

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

        /// <summary>
        /// This property holds the last occured WebSocket error
        /// </summary>
        public string LastWebSocketError { get; private set; }

        void CheckAndHandleWebSocketDisconnect() {
            if (IsConnected)
                return;

            if (_onRecv != null) {
                _onRecv.Dispose();
                _onRecv = null;
                IsProbed = false;
                IsUpgraded = false;

                var sockets = gameObject.GetComponentsInChildren<Socket>();
                foreach (var s in sockets) {
                    if (s.onDisconnect != null)
                        s.onDisconnect();
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