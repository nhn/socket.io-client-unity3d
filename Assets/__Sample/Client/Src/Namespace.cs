using UnityEngine;
using socket.io;

namespace Sample {

    /// <summary>
    /// The sample show how to restrict yourself a namespace
    /// </summary>
    public class Namespace : MonoBehaviour {

        void Start() {
            var serverUrl = "http://localhost:4444";

            var news = Socket.Connect(serverUrl + "/news");
            news.On("connect", () => {
                news.Emit("woot");
            });
            news.On("a message", (string data) => {
                Debug.Log("news => " + data);
            });
            news.On("item", (string data) => {
                Debug.Log(data);
            });

            var chat = Socket.Connect(serverUrl + "/chat");
            chat.On("connect", () => {
                chat.Emit("hi~");
            });
            chat.On("a message", (string data) => {
                Debug.Log("chat => " + data);
            });
        }

    }

}