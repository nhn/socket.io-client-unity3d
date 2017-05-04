using UnityEngine;
using socket.io;

namespace Sample {
    
    /// <summary>
    /// The sample show how to acks the message you sent.
    /// </summary>
    public class Acks : MonoBehaviour {

        void Start() {
            var serverUrl = "http://localhost:4444";
            var socket = Socket.Connect(serverUrl);

            socket.On("connect", () => {

                // "ferret" 이벤트 Send
                socket.Emit(
                    "ferret", "\"toby\"", 
                    (string r) => { Debug.Log(r); } // Ack 콜백 셋팅
                    );
            });
        }

    }

}
