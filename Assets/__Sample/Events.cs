using UnityEngine;
using socket.io;

namespace Sample {

    /// <summary>
    /// http://socket.io/docs/#using-with-node-http-server 샘플
    /// </summary>
    public class Events : MonoBehaviour {

        void Start() {
            var socket = Socket.Connect(Config.serverUrl);
            socket.On("news", (string r) => {
                Debug.Log(r);
                socket.Emit("my other event", "{ \"my\": \"data\" }");
            });
        }

    }

}
