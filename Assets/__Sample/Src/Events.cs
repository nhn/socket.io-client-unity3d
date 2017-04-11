using UnityEngine;
using socket.io;

namespace Sample {
    
    /// <summary>
    /// The basic sample to show how to send and receive messages.
    /// </summary>
    public class Events : MonoBehaviour {

        void Start() {
            Config.serverUrl = "http://localhost:80";

            var socket = Socket.Connect(Config.serverUrl);
            socket.On("news", (string r) => {
                Debug.Log(r);
                socket.Emit("my other event", "{ \"my\": \"data\" }");
            });
        }

    }

}
