using UnityEngine;
using socket.io;

namespace Sample {

    /// <summary>
    /// The basic sample to show how to connect to a server
    /// </summary>
    public class Connection : MonoBehaviour {

        void Start() {
            var serverUrl = "http://localhost:4444";
            var socket = Socket.Connect(serverUrl);

            socket.On("connect", () => {
                Debug.Log("Hello, Socket.io~");
            });

            socket.On("disconnect", () => {
                Debug.Log("Bye~");
            });
        }

    }

}
