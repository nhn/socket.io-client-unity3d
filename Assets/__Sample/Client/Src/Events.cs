using UnityEngine;
using socket.io;

namespace Sample {
    
    /// <summary>
    /// The basic sample to show how to send and receive messages.
    /// </summary>
    public class Events : MonoBehaviour {

        void Start() {
            var serverUrl = "http://localhost:7001";
            var socket = Socket.Connect(serverUrl);

            // receive "news" event
            socket.On("news", (string data) => {
                Debug.Log(data);

                // Emit raw string data
                socket.Emit("my other event", "{ my: data }");

                // Emit json-formatted string data
                socket.EmitJson("my other event", @"{ ""my"": ""data"" }");
            });
        }

    }

}