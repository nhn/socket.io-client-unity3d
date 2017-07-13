using UnityEngine;
using UniRx;
using UniRx.Triggers;
using System;
using System.Collections;


namespace socket.io {

    /// <summary>
    /// UniRX ObservableTrigger which pass-through received packet data as observable object
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

        public class WebSocketErrorException : Exception {
            public WebSocketErrorException(string message) : base(message) { }
        }

        void Update() {
            var err = WebSocket.GetLastError();

            if (!string.IsNullOrEmpty(err)) {
                _onRecv.OnError(new WebSocketErrorException(err));
                _onRecv.Dispose();
                _onRecv = null;
                IsProbed = false;
                IsUpgraded = false;

                if (!IsConnected) {
                    var sockets = gameObject.GetComponentsInChildren<Socket>();

                    foreach (var s in sockets) {
                        if (SocketManager.Instance.Reconnection)
                            SocketManager.Instance.Reconnect(s, 1);
                    }
                }
            }

            if (IsConnected) {
                var recvData = WebSocket.Recv();
                if (recvData != null) {
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
            }
        }
        
    }

}