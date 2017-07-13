//#define FORCE_USE_UNITY_WEBSOCKET
using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections;
using System.Runtime.InteropServices;


namespace socket.io {

    public class WebSocketWrapper {
        
        public WebSocketWrapper(Uri url) {
            Url = url;

            var protocol = Url.Scheme;
            if (!protocol.Equals("ws") && !protocol.Equals("wss"))
                throw new ArgumentException("Unsupported protocol: " + protocol);
        }

        public Uri Url { get; private set; }

        public bool IsConnected {
            get {
                return IsConnectedInternal();
            }
        }

        public void Connect() {
            ConnectInternal(Url.ToString());
        }

        public void Close() {
            CloseInternal();
        }

        public void Send(string data) {
            SendInternal(data);
        }

        public string Recv() {
            return RecvInternal();
        }

        public string GetLastError() {
            return ErrorInternal();
        }

#if FORCE_USE_UNITY_WEBSOCKET || (UNITY_WEBGL && !UNITY_EDITOR)
        #region Internal WebSocket Dll PInvoke methods
        [DllImport("__Internal")]
        private static extern int SocketCreate(string url);

        [DllImport("__Internal")]
        private static extern int SocketState(int socketInstance);

        [DllImport("__Internal")]
        private static extern void SocketSend(int socketInstance, string ptr);

        [DllImport("__Internal")]
        private static extern void SocketRecv(int socketInstance, byte[] ptr, int length);

        [DllImport("__Internal")]
        private static extern int SocketRecvLength(int socketInstance);

        [DllImport("__Internal")]
        private static extern void SocketClose(int socketInstance);

        [DllImport("__Internal")]
        private static extern int SocketError(int socketInstance, byte[] ptr, int length);
        #endregion

        int _internalRef = 0;

        void SendInternal(string data) {
            SocketSend(_internalRef, data);
        }

        const int _recvBuffInitSize = 512;
        byte[] _recvBuff = new byte[_recvBuffInitSize];

        string RecvInternal() {
            int len = SocketRecvLength(_internalRef);
            if (len == 0)
                return null;

            if (_recvBuff.Length < len)
                _recvBuff = new byte[len];

            SocketRecv(_internalRef, _recvBuff, len);
            return Encoding.UTF8.GetString(_recvBuff, 0, len);
        }
        
        public bool IsConnectedInternal() {
            return SocketState(_internalRef) != 0;
        }

        void ConnectInternal(string url) {
            _internalRef = SocketCreate(url);
        }

        public void CloseInternal() {
            SocketClose(_internalRef);
        }

        const int _errorBuffSize = 1024;
        readonly byte[] _errorBuff = new byte[_errorBuffSize];

        public string ErrorInternal() {
            int result = SocketError(_internalRef, _errorBuff, _errorBuffSize);
            if (result == 0)
                return string.Empty;

            return Encoding.UTF8.GetString(_errorBuff);
        }
#else
        WebSocketSharp.WebSocket _webSocket;

        readonly Queue<byte[]> _messages = new Queue<byte[]>();
        readonly Queue<string> _errors = new Queue<string>();
        readonly object _recvLock = new object();
        
        public bool IsConnectedInternal() {
            return (_webSocket != null && _webSocket.IsConnected);
        }

        void ConnectInternal(string url) {
            _webSocket = new WebSocketSharp.WebSocket(url);
            _webSocket.OnError += (sender, e) => {
                lock (_recvLock)
                    _errors.Enqueue(e.Message);
            };
            _webSocket.OnMessage += (sender, e) => {
                lock (_recvLock)
                    _messages.Enqueue(e.RawData);
            };

            _webSocket.Connect();
        }

        void SendInternal(string data) {
            _webSocket.Send(data);
        }

        string RecvInternal() {
            if (_messages.Count == 0)
                return null;

            lock (_recvLock)
                return Encoding.UTF8.GetString(_messages.Dequeue());
        }

        void CloseInternal() {
            _webSocket.Close();
        }

        string ErrorInternal() {
            if (_errors.Count == 0)
                return string.Empty;

            lock (_recvLock)
                return _errors.Dequeue();
        }
#endif

    }

}