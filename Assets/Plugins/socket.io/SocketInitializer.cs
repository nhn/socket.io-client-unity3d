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

        /// <summary>
        /// Return an UniRX observable object which run the connecting process in async mode.
        /// </summary>
        /// <param name="url"> WWW URL of a server </param>
        /// <param name="socket"> a socket which will be connected </param>
        /// <returns></returns>
        public IObservable<Socket> InitAsObservable(string url, Socket socket) {
            _connectUrl = url;
            Socket = socket;

            var matches = new Regex(_urlParamRgx).Matches(_connectUrl);
            foreach (var m in matches) {
                var tokens = m.ToString().Split('?', '&', '=');
                urlParams.Add(tokens[1], tokens[2]);
            }

            _baseUrl = _connectUrl.Split('?')[0];
            var matches2 = new Regex(_urlNamespaceRgx).Matches(_baseUrl);
            Debug.Assert(matches2.Count <= 2);

            if (matches2.Count == 2) {
                _baseUrl = matches2[0].ToString();
                Socket.nsp = matches2[1].ToString();
            }

            return Observable.FromCoroutine<Socket>((observer, cancelToken) =>
                InitCore(observer, cancelToken));
        }

        #region URL Helper
        /// <summary>
        /// WWW (Polling Mode) URL
        /// </summary>
        public string PollingUrl {
            get {
                var builder = new StringBuilder(_baseUrl);
                builder.Append("/socket.io/");

                for (int i = 0; i < urlParams.Count; ++i) {
                    var elem = urlParams.ElementAt(i);
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
                var builder = new StringBuilder(_baseUrl.Replace("http://", "ws://"));
                builder.Append("/socket.io/");

                for (int i = 0; i < urlParams.Count; ++i) {
                    var elem = urlParams.ElementAt(i);
                    builder.Append(i == 0 ? "?" : "&");
                    builder.Append(elem.Key + "=" + elem.Value);
                }

                return builder.ToString();
            }
        }

        /// <summary>
        /// An URL which is given InitAsObservable() method's param.
        /// </summary>
        string _connectUrl;

        /// <summary>
        /// An URL which has no params and no namespace.
        /// </summary>
        string _baseUrl;

        /// <summary>
        /// Regular expression for extracting params from URL
        /// </summary>
        const string _urlParamRgx = @"[\?|\&]\w+=[^\&]+";

        /// <summary>
        /// Regular expression for extracting namespace from URL
        /// </summary>
        const string _urlNamespaceRgx = @"^http://[^/]+|([\s\S]+)";

        /// <summary>
        /// A container which contains URL's params (The key is param's name and the value is param's value)
        /// </summary>
        public Dictionary<string, string> urlParams = new Dictionary<string, string>();
        #endregion

        public Socket Socket { get; private set; }

        /// <summary>
        /// Return weather Socket is current working on initialization process or not
        /// </summary>
        public bool IsBusy {
            get { return Socket != null; }
        }

        /// <summary>
        /// The json object to parse the response of PollingURL
        /// </summary>
        [Serializable]
        class PollingUrlAnswer {
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
        IEnumerator InitCore(IObserver<Socket> observer, CancellationToken cancelToken) {
            // Declare to connect in socket.io v1.0
            urlParams.Add("EIO", "3");
            urlParams.Add("transport", "polling");
            urlParams.Add("t", TimeStamp.Now);

            // Try get WebSocketTrigger instance if a connection already established _baseUrl.
            var webSocketTrigger = SocketManager.Instance.GetWebSocketTrigger(_baseUrl);
            if (webSocketTrigger == null) {
                var www = new WWW(PollingUrl);
                while (!www.isDone && !cancelToken.IsCancellationRequested)
                    yield return null;

                if (cancelToken.IsCancellationRequested) {
                    CleanUp();
                    yield break;
                }

                if (www.error != null) {
                    observer.OnError(new Exception(www.error));
                    CleanUp();
                    yield break;
                }

                var textIndex = www.text.IndexOf('{');
                if (textIndex != -1) {
                    var json = www.text.Substring(textIndex);
                    var answer = JsonUtility.FromJson<PollingUrlAnswer>(json);
                    urlParams.Add("sid", answer.sid);
                }

                webSocketTrigger = new GameObject(string.Format("webSocket - {0}", _baseUrl)).AddComponent<WebSocketTrigger>();
                SocketManager.Instance.RegisterWebSocketTrigger(_baseUrl, webSocketTrigger);
            }

            urlParams["transport"] = "websocket";
            urlParams.Remove("t");

            webSocketTrigger.WebSocket = new WebSocketWrapper(new Uri(WebSocketUrl));
            Socket.transform.parent = webSocketTrigger.transform;

            webSocketTrigger.WebSocket.Connect();
            yield return new WaitUntil(() => webSocketTrigger.IsConnected);

            webSocketTrigger.WebSocket.Send(Packet.Probe);
            yield return new WaitUntil(() => webSocketTrigger.IsProbed);

            webSocketTrigger.WebSocket.Send(new Packet(EnginePacketTypes.UPGRADE).Encode());
            webSocketTrigger.IsUpgraded = true;

            // Try to activate Socket.io namespace
            if (Socket.HasNamespace)
                webSocketTrigger.WebSocket.Send(new Packet(EnginePacketTypes.MESSAGE, SocketPacketTypes.CONNECT, Socket.nsp, string.Empty).Encode());

            // Start to receive a incoming WebSocket packet
            var capturedSocket = Socket;
            webSocketTrigger.OnRecvAsObservable(WebSocketUrl).Subscribe(r => {
                capturedSocket.OnRecvWebSocketEvent(r);
            }).AddTo(Socket);
            
            observer.OnNext(Socket);
            observer.OnCompleted();

            CleanUp();
        }
        
        void CleanUp() {
            Socket = null;
            _connectUrl = string.Empty;
            urlParams.Clear();
        }

    }

}