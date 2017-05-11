using UnityEngine;
using UniRx;
using UniRx.Triggers;
using System;
using System.Linq;
using System.Collections.Generic;


namespace socket.io {

    /// <summary>
    /// SocketManager manages Socket and WebSocketTrigger instances
    /// </summary>
    public class SocketManager : MonoSingleton<SocketManager> {

        /// <summary>
        /// connection timeout before a connect_error and connect_timeout events are emitted 
        /// (The default value is 20000 and the unit is millisecond.)
        /// </summary>
        public int TimeOut { get; set; }

        /// <summary>
        /// whether to reconnect automatically (The default value is true)
        /// </summary>
        public bool Reconnection { get; set; }

        /// <summary>
        /// number of reconnection attempts before giving up (The default value is infinity)
        /// </summary>
        public int ReconnectionAttempts { get; set; }

        /// <summary>
        /// how long to initially wait before attempting a new reconnection
        /// (The default value is 1000 and the unit is millisecond.)
        /// </summary>
        public int ReconnectionDelay { get; set; }


        public Socket Connect(string url) {
            var socket = new GameObject(string.Format("socket.io - {0}", url)).AddComponent<Socket>();
            socket.transform.parent = transform;

            _connectRequests.Add(Tuple.Create(url, false, 0, socket, DateTime.Now));

            return socket;
        }

        public void Reconnect(string url, int reconnectionAttempts, Socket socket) {
            _connectRequests.Add(Tuple.Create(url, true, reconnectionAttempts, socket, DateTime.Now.AddMilliseconds(ReconnectionDelay)));

            if (socket.onReconnectAttempt != null)
                socket.onReconnectAttempt();

            Debug.LogFormat("socket.io => {0} attempts to reconnect", socket.gameObject.name);
        }

        void Awake() {
            TimeOut = 20000;
            Reconnection = true;
            ReconnectionAttempts = int.MaxValue;
            ReconnectionDelay = 1000;

            _socketInit = gameObject.AddComponent<SocketInitializer>();
        }

        SocketInitializer _socketInit;

        /// <summary>
        /// The pended requests to connect a server
        /// (Item1: Url, Item2: Reconnection, Item3: ReconnectionAttempts, Item4: Socket ref, Item5: TimeStamp)
        /// </summary>
        readonly List<Tuple<string, bool, int, Socket, DateTime>> _connectRequests = new List<Tuple<string, bool, int, Socket, DateTime>>();

        /// <summary>
        /// WebSocketTrigger instances (WebSocketTrigger is almost same with a sesstion object)
        /// </summary>
        readonly Dictionary<string, WebSocketTrigger> _webSocketTriggers = new Dictionary<string, WebSocketTrigger>();

        public void RegisterWebSocketTrigger(string baseUrl, WebSocketTrigger webSocketTrigger) {
            _webSocketTriggers.Add(baseUrl, webSocketTrigger);
            webSocketTrigger.transform.parent = transform;
        }

        /// <summary>
        /// Return a WebSocketTrigger instance if there is a connection to url param
        /// </summary>
        public WebSocketTrigger GetWebSocketTrigger(string url) {
            return _webSocketTriggers.ContainsKey(url) ? _webSocketTriggers[url] : null;
        }

        public Socket GetSocket(string url) {
            var go = GameObject.Find(string.Format("socket.io - {0}", url));
            return (go != null) ? go.GetComponent<Socket>() : null;
        }

        void Update() {
            int i = 0;
        }

        void Start() {
            gameObject.UpdateAsObservable()
                .Sample(TimeSpan.FromSeconds(1f))
                .Where(_ => !_socketInit.IsBusy && _connectRequests.Any(c => c.Item5 < DateTime.Now))
                .Select(_ => {
                    var i = _connectRequests.FindIndex(c => c.Item5 < DateTime.Now);
                    Debug.Assert(i != -1);

                    var ret = _connectRequests[i];
                    _connectRequests.RemoveAt(i);

                    return ret;
                })
                .SelectMany(c =>
                    _socketInit.InitAsObservable(c.Item1, c.Item2, c.Item3, c.Item4)
                    .Timeout(TimeSpan.FromMilliseconds(TimeOut))
                    )
                .OnErrorRetry((Exception e) => {
                    if (e is TimeoutException) {
                        if (_socketInit.Socket.onConnectTimeOut != null)
                            _socketInit.Socket.onConnectTimeOut();

                        Debug.LogErrorFormat("socket.io => {0} connection timed out!!", _socketInit.Socket.gameObject.name);
                    }
                    else if (e is WWWErrorException){
                        Debug.LogErrorFormat("socket.io => {0} got WWW error: {1}", _socketInit.Socket.gameObject.name, (e as WWWErrorException).RawErrorMessage);
                    }
                    else {
                        Debug.LogErrorFormat("socket.io => {0} got an unknown error: {1}", _socketInit.Socket.gameObject.name, e.ToString());
                    }

                    if (_socketInit.Reconnection) {
                        if (_socketInit.Socket.onReconnectFailed != null)
                            _socketInit.Socket.onReconnectFailed();

                        if (_socketInit.Socket.onReconnectError != null)
                            _socketInit.Socket.onReconnectError(e);

                        if (Reconnection)
                            Reconnect(_socketInit.ConnectUrl, _socketInit.ReconnectionAttempts + 1, _socketInit.Socket);
                    }
                    else {
                        if (_socketInit.Socket.onConnectError != null)
                            _socketInit.Socket.onConnectError(e);
                    }

                    _socketInit.CleanUp();
                })
                .Subscribe(_ => {
                    if (_socketInit.Reconnection) {
                        if (_socketInit.Socket.onReconnect != null)
                            _socketInit.Socket.onReconnect(_socketInit.ReconnectionAttempts);

                        Debug.LogFormat("socket.io => {0} has been reconnected~ :)", _socketInit.Socket.gameObject.name);
                    }
                    else {
                        Debug.LogFormat("socket.io => {0} has been connected~ :)", _socketInit.Socket.gameObject.name);
                    }

                    _socketInit.CleanUp();
                });
        }

    }

}