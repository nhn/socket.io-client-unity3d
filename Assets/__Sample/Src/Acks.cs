using UnityEngine;
using socket.io;

namespace Sample {
    
    /// <summary>
    /// The sample show how to acks the message you sent.
    /// </summary>
    public class Acks : MonoBehaviour {

        void Start() {
            Config.serverUrl = "http://localhost:80";

            var socket = Socket.Connect(Config.serverUrl);
            socket.On("connect", () => {
                socket.Emit("ferret", "\"toby\"", (string r) => {
                    Debug.Log(r);
                });
            });
        }

    }

}
