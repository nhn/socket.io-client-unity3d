using UnityEngine;
using socket.io;

namespace Sample {
    
    /// <summary>
    /// The sample show how to acks the message you sent.
    /// </summary>
    public class Acks : MonoBehaviour {

        void Start() {
            var serverUrl = "http://localhost:7001";
            var socket = Socket.Connect(serverUrl);

            socket.On(SystemEvents.connect, () => {
                // send "ferret" event
                socket.Emit(
                    "ferret", "toby", 
                    (string r) => { Debug.Log(r); } // set callback handler for Ack
                    );
            });
        }

    }

}
