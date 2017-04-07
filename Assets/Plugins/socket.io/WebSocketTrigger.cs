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
            return _onRecv;
        }

        protected override void RaiseOnCompletedOnDestroy() {
            if (_onRecv != null)
                _onRecv.OnCompleted();
            if (!IsConnected)
                WebSocket.Close();
        }

        readonly Subject<string> _onRecv = new Subject<string>();

        /// <summary>
        /// Ping-Pong coroutine to keep connection alive.
        /// </summary>
        /// <returns></returns>
        IEnumerator PingPong() {
            while (WebSocket.IsConnected) {
                yield return new WaitForSeconds(10f);
                WebSocket.Send(Packet.Ping);
                Debug.Log("socket.io => ping~");
            }
        }
        
        public WebSocketWrapper WebSocket { get; set; }

        public bool IsConnected {
            get {
                return WebSocket != null && WebSocket.IsConnected;
            }
        }

        public bool IsProbed { get; set; }

        public bool IsUpgraded { get; set; }

        public class WebSocketError : Exception {
            public WebSocketError(string message) : base(message) { }
        }

        void Update() {
            if (!IsConnected)
                return;

            var error = WebSocket.GetLastError();
            if (error != string.Empty) {
                _onRecv.OnError(new WebSocketError(error));
                IsProbed = false;
                IsUpgraded = false;
            }

            var recvData = WebSocket.Recv();
            if (recvData != null) {
                if (recvData == Packet.ProbeAnswer) {
                    IsProbed = true;
                    Debug.Log("socket.io => probed~");
                }
                else if (recvData == Packet.Pong) {
                    Debug.Log("socket.io => pong~");
                }
                else {
                    _onRecv.OnNext(recvData);
                }
            }
        }
        
    }

}