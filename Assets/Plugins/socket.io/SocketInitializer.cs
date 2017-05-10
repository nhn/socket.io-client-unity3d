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
        public IObservable<Socket> InitAsObservable(string url, bool reconnection, int reconnectionAttempts, Socket socket) {
            ConnectUrl = url;
            Socket = socket;
            Reconnection = reconnection;
            ReconnectionAttempts = reconnectionAttempts;

            if (Reconnection && Socket.onReconnecting != null)
                Socket.onReconnecting(ReconnectionAttempts);

            var matches = new Regex(_urlParamRgx).Matches(ConnectUrl);
            foreach (var m in matches) {
                var tokens = m.ToString().Split('?', '&', '=');
                urlParams.Add(tokens[1], tokens[2]);
            }

            BaseUrl = ConnectUrl.Split('?')[0];
            var matches2 = new Regex(_urlNamespaceRgx).Matches(BaseUrl);
            Debug.Assert(matches2.Count <= 2);

            if (matches2.Count == 2) {
                BaseUrl = matches2[0].ToString();
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
                var builder = new StringBuilder(BaseUrl);
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
                var builder = new StringBuilder(BaseUrl.Replace("http://", "ws://"));
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
        public string ConnectUrl { get; private set; }

        /// <summary>
        /// An URL which remove params and namespace string from ConnectUrl.
        /// </summary>
        public string BaseUrl { get; private set; }

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

        public bool Reconnection { get; private set; }

        public int ReconnectionAttempts { get; private set; }

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
            var webSocketTrigger = SocketManager.Instance.GetWebSocketTrigger(BaseUrl);
            if (webSocketTrigger == null || !webSocketTrigger.IsConnected) {
                var www = new WWW(PollingUrl);
                while (!www.isDone && !cancelToken.IsCancellationRequested)
                    yield return null;

                if (cancelToken.IsCancellationRequested)
                    yield break;

                if (www.error != null) {
                    observer.OnError(new Exception(www.error));
                    yield break;
                }

                var textIndex = www.text.IndexOf('{');
                if (textIndex != -1) {
                    var json = www.text.Substring(textIndex);
                    var answer = JsonUtility.FromJson<PollingUrlAnswer>(json);
                    urlParams.Add("sid", answer.sid);
                }

                if (webSocketTrigger == null) {
                    webSocketTrigger = new GameObject(string.Format("WebSocket - {0}", BaseUrl)).AddComponent<WebSocketTrigger>();
                    SocketManager.Instance.RegisterWebSocketTrigger(BaseUrl, webSocketTrigger);
                }
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
            var capturedUrl = ConnectUrl;
            var capturedSocket = Socket;
            webSocketTrigger.OnRecvAsObservable(WebSocketUrl).Subscribe(r => {
                capturedSocket.OnRecvWebSocketEvent(r);
            }, e => {
                Debug.LogErrorFormat("socket.io => {0} got an error: {1}", capturedSocket.gameObject.name, e.ToString());

                if (SocketManager.Instance.Reconnection)
                    SocketManager.Instance.Reconnect(capturedUrl, 1, capturedSocket);
            }).AddTo(Socket);
            
            observer.OnNext(Socket);
            observer.OnCompleted();
        }
        
        public void CleanUp() {
            Socket = null;
            ConnectUrl = string.Empty;
            Reconnection = false;
            ReconnectionAttempts = 0;
            urlParams.Clear();
        }

    }

}