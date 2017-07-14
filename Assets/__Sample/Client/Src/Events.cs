using UnityEngine;
using socket.io;

namespace Sample {
    
    /// <summary>
    /// The basic sample to show how to send and receive messages.
    /// </summary>
    public class Events : MonoBehaviour {

        void Start() {
            var serverUrl = "http://localhost:4444";
            var socket = Socket.Connect(serverUrl);

            // receive "news" event
            socket.On("news", (string data) => {
                Debug.Log(data);

                // send "my other event" event
                socket.Emit(
                    "my other event",       // event-name
                    "{ 'my': 'data'  }"  // data (in Json-format)
                    );
            });
        }

    }

}