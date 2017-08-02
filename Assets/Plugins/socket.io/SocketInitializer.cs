using UnityEngine;
using UniRx;
using System;
using System.Text;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace socket.io {
    
    /// <summary>
    /// The initializer which perform the entire process about connecting a socket.io server
    /// </summary>
    public class SocketInitializer : MonoBehaviour {

        public Socket Socket { get; private set; }

        public bool Reconnection { get; private set; }

        public int ReconnectionAttempts { get; private set; }
        
        #region URL properties

        public string BaseUrl {
            get { return Socket.Url.Scheme + "://" + Socket.Url.Authority; }
        }

        /// <summary>
        /// WWW (Polling Mode) URL
        /// </summary>
        public string PollingUrl {
            get {
                var builder = new StringBuilder(BaseUrl);
                builder.Append("/socket.io/");

                for (int i = 0; i < _urlQueries.Count; ++i) {
                    var elem = _urlQueries.ElementAt(i);
                    builder.Append(i == 0 ? "?" : "&");
                    builder.Append(elem.Key + "=" + elem.Value);
                }

                return builder.ToString();
            }
        }

        /// <summary>
        /// WebSocket URL
        /// </summary>
        public string WebSocketUrl {
            get {
                var builder = new StringBuilder(BaseUrl.Replace("http://", "ws://"));
                builder.Append("/socket.io/");

                for (int i = 0; i < _urlQueries.Count; ++i) {
                    var elem = _urlQueries.ElementAt(i);
                    builder.Append(i == 0 ? "?" : "&");
                    builder.Append(elem.Key + "=" + elem.Value);
                }

                return builder.ToString();
            }
        }

        /// <summary>
        /// The query in Url (key is the query's name and value is the query's value)
        /// </summary>
        readonly Dictionary<string, string> _urlQueries = new Dictionary<string, string>();

        #endregion

        /// <summary>
        /// Return an UniRX observable object which run the connecting process in async mode.
        /// </summary>
        /// <param name="url"> WWW URL of a server </param>
        /// <param name="socket"> a socket which will be connected </param>
        /// <returns></returns>
        public UniRx.IObservable<Socket> InitAsObservable(Socket socket, bool reconnection, int reconnectionAttempts) {
            Socket = socket;
            Reconnection = reconnection;
            ReconnectionAttempts = reconnectionAttempts;

            // Extract each key & value pairs in Socket.Url's query
            var matches = Regex.Matches(Socket.Url.Query, @"[^\?\&]+");
            foreach (var m in matches) {
                var temp = m.ToString().Split('=');
                _urlQueries.Add(temp[0], temp[1]);
            }

            if (Reconnection && Socket.OnReconnecting != null)
                Socket.OnReconnecting(ReconnectionAttempts);

            return UniRx.Observable.FromCoroutine<Socket>((observer, cancelToken) =>
                InitCore(observer, cancelToken));
        }

        /// <summary>
        /// The json object to parse the response of PollingURL
        /// </summary>
        [Serializable]
        struct PollingUrlAnswer {
            public string sid;
            public int pingInterval;
            public int pingTimeout;
        }

        /// <summary>
        /// The core method which run the async connection process.
        /// </summary>
        /// <param name="observer"> The return value of InitAsObservable() method </param>
        /// <param name="cancelToken"> The cancel token object which signals to stop the currnet coroutine </param>
        /// <returns></returns>
        IEnumerator InitCore(UniRx.IObserver<Socket> observer, UniRx.CancellationToken cancelToken) {
            // Declare to connect in socket.io v1.0
            _urlQueries.Add("EIO", "3");
            _urlQueries.Add("transport", "polling");
            _urlQueries.Add("t", TimeStamp.Now);

            // Try get WebSocketTrigger instance if a connection already established _baseUrl.
            var webSocketTrigger = SocketManager.Instance.GetWebSocketTrigger(BaseUrl);
            if (webSocketTrigger == null || !webSocketTrigger.IsConnected) {
                var www = new WWW(PollingUrl);

                while (!www.isDone && !cancelToken.IsCancellationRequested )
                    yield return null;

                if (cancelToken.IsCancellationRequested)
                    yield break;

                if (!string.IsNullOrEmpty(www.error)) {
                    observer.OnError(new Exception(www.error));
                    yield break;
                }

                var textIndex = www.text.IndexOf('{');
                if (textIndex != -1) {
                    var json = www.text.Substring(textIndex);
                    var answer = JsonUtility.FromJson<PollingUrlAnswer>(json);
                    _urlQueries.Add("sid", answer.sid);
                }

                if (webSocketTrigger == null) {
                    webSocketTrigger = new GameObject(string.Format("WebSocket - {0}", BaseUrl)).AddComponent<WebSocketTrigger>();
                    SocketManager.Instance.RegisterWebSocketTrigger(BaseUrl, webSocketTrigger);
                }
            }

            _urlQueries["transport"] = "websocket";
            _urlQueries.Remove("t");

            webSocketTrigger.WebSocket = new WebSocketWrapper(new Uri(WebSocketUrl));
            Socket.transform.parent = webSocketTrigger.transform;

            webSocketTrigger.WebSocket.Connect();
            yield return new WaitUntil(() => webSocketTrigger.IsConnected);

            if (cancelToken.IsCancellationRequested) {
                webSocketTrigger.WebSocket.Close();
                yield break;
            }

            webSocketTrigger.WebSocket.Send(Packet.Probe);
            yield return new WaitUntil(() => webSocketTrigger.IsProbed);

            if (cancelToken.IsCancellationRequested) {
                webSocketTrigger.WebSocket.Close();
                yield break;
            }

            webSocketTrigger.WebSocket.Send(new Packet(EnginePacketTypes.UPGRADE).Encode());
            webSocketTrigger.IsUpgraded = true;

            // Try to activate Socket.io namespace
            if (Socket.HasNamespace)
                webSocketTrigger.WebSocket.Send(new Packet(EnginePacketTypes.MESSAGE, SocketPacketTypes.CONNECT, Socket.Namespace, string.Empty).Encode());

            var capturedSocket = Socket;

            // Start to receive a incoming WebSocket packet
            webSocketTrigger.OnRecvAsObservable()
                .Subscribe(r => { capturedSocket.OnRecvWebSocketPacket(r); })
                .AddTo(Socket);
            
            observer.OnNext(Socket);

            yield return new WaitForSeconds(1f);
            observer.OnCompleted();
        }
        
        public void CleanUp() {
            Socket = null;
            Reconnection = false;
            ReconnectionAttempts = 0;
            _urlQueries.Clear();
        }

    }

}