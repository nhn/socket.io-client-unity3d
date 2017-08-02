using UnityEngine;
using UniRx;
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

        public void Connect(Socket socket) {
            _connectRequests.Add(UniRx.Tuple.Create(socket, false, 0, DateTime.Now));
        }

        /// <summary>
        /// Reconnect method (Users seldom call this method, instead SocketManager will call when Reconnection property is true)
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="reconnectionAttempts"></param>
        public void Reconnect(Socket socket, int reconnectionAttempts) {
            // Check if request already pended
            if (_connectRequests.Any(r => r.Item1 == socket))
                return;

            _connectRequests.Add(UniRx.Tuple.Create(socket, true, reconnectionAttempts, DateTime.Now.AddMilliseconds(ReconnectionDelay)));

            if (socket.OnReconnectAttempt != null)
                socket.OnReconnectAttempt();

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
        readonly List<UniRx.Tuple<Socket, bool, int, DateTime>> _connectRequests = new List<UniRx.Tuple<Socket, bool, int, DateTime>>();

        /// <summary>
        /// WebSocketTrigger instances (WebSocketTrigger is almost same with a session object)
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

        /// <summary>
        /// Return a Socket instance if there is a connection to url param
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public Socket GetSocket(string url) {
            var go = GameObject.Find(string.Format("socket.io - {0}", url));
            return (go != null) ? go.GetComponent<Socket>() : null;
        }

        /// <summary>
        /// Current async connection request (If this value is not null, it means a connection is on running~)
        /// </summary>
        IDisposable _cancelConnectRequest;

        void Update() {
            if (_cancelConnectRequest != null || !_connectRequests.Any(c => c.Item4 < DateTime.Now))
                return;
            
            var index = _connectRequests.FindIndex(c => c.Item4 < DateTime.Now);
            var request = _connectRequests[index];
            _connectRequests.RemoveAt(index);

            _cancelConnectRequest = _socketInit.InitAsObservable(request.Item1, request.Item2, request.Item3)
                  .Timeout(TimeSpan.FromMilliseconds(TimeOut))
                  .DoOnError(e => {
                      if (e is TimeoutException) {
                          if (_socketInit.Socket.OnConnectTimeOut != null)
                              _socketInit.Socket.OnConnectTimeOut();

                          Debug.LogErrorFormat(
                              "socket.io => {0} connection timed out!!", 
                              _socketInit.Socket.gameObject.name
                              );
                      }
                      else if (e is WWWErrorException) {
                          Debug.LogErrorFormat(
                              "socket.io => {0} got WWW error: {1}", 
                              _socketInit.Socket.gameObject.name, 
                              (e as WWWErrorException).RawErrorMessage
                              );
                      }
                      else {
                          Debug.LogErrorFormat(
                              "socket.io => {0} got an unknown error: {1}", 
                              _socketInit.Socket.gameObject.name, 
                              e.ToString()
                              );
                      }

                      if (_socketInit.Reconnection) {
                          if (_socketInit.Socket.OnReconnectFailed != null)
                              _socketInit.Socket.OnReconnectFailed();

                          if (_socketInit.Socket.OnReconnectError != null)
                              _socketInit.Socket.OnReconnectError(e);

                          if (Reconnection)
                              Reconnect(_socketInit.Socket, _socketInit.ReconnectionAttempts + 1);
                      }
                      else {
                          if (_socketInit.Socket.OnConnectError != null)
                              _socketInit.Socket.OnConnectError(e);
                      }

                      _socketInit.CleanUp();
                      _cancelConnectRequest = null;
                  })
                  .DoOnCompleted(() => {
                      if (_socketInit.Reconnection) {
                          if (_socketInit.Socket.OnReconnect != null)
                              _socketInit.Socket.OnReconnect(_socketInit.ReconnectionAttempts);
                      }
                      else {
                          if (_socketInit.Socket.OnConnect != null)
                              _socketInit.Socket.OnConnect();
                      }

                      _socketInit.CleanUp();
                      _cancelConnectRequest = null;
                  })
                  .Subscribe();
        }

    }

}