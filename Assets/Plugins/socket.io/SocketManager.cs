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

        public Socket Connect(string url) {
            var socket = new GameObject(string.Format("socket.io - {0}", url)).AddComponent<Socket>();
            socket.transform.parent = transform;

            _connectRequests.Enqueue(Tuple.Create<string, Socket>(url, socket));
            return socket;
        }
        
        void Awake() {
            _socketInit = gameObject.AddComponent<SocketInitializer>();
        }

        SocketInitializer _socketInit;

        /// <summary>
        /// The pended requests to connect a server
        /// </summary>
        readonly Queue<Tuple<string, Socket>> _connectRequests = new Queue<Tuple<string, Socket>>();

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

        void Start() {
            gameObject.UpdateAsObservable()
                .Sample(TimeSpan.FromSeconds(1f))
                .Where(_ => _connectRequests.Count > 0 && !_socketInit.IsBusy)
                .Select(_ => _connectRequests.Dequeue())
                .SelectMany(c => _socketInit.InitAsObservable(c.Item1, c.Item2).Timeout(TimeSpan.FromSeconds(10f)))
                .Subscribe(_ => {
                    Debug.LogFormat("{0} has been inited~ :)", _socketInit.Socket.gameObject.name);
                }, e => {
                    if (e is TimeoutException) {
                        if (_socketInit.Socket.onConnectTimeOut != null)
                            _socketInit.Socket.onConnectTimeOut();

                        Debug.LogError("The connect trial is timed out!!");
                    }
                    else {
                        Debug.LogError("Unknown exception!!");
                    }
                });
        }

    }

}