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

            // "news" 이벤트 처리 Receive
            socket.On("news", (string data) => {
                Debug.Log(data);

                // "my other event" 이벤트 Send
                socket.Emit(
                    "my other event",       // 이벤트명
                    "{ \"my\": \"data\" }"  // 데이터 (Json 텍스트)
                    );
            });
        }

    }

}