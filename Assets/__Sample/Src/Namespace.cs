using UnityEngine;
using socket.io;


namespace Sample {

    /// <summary>
    /// The sample show how to restrict yourself a namespace
    /// </summary>
    public class Namespace : MonoBehaviour {

        void Start() {
            Config.serverUrl = "http://localhost:80";

            var chat = Socket.Connect(Config.serverUrl + "/chat");
            var news = Socket.Connect(Config.serverUrl + "/news");

            chat.On("connect", () => {
                chat.Emit("hi");
            });
            chat.On("a message", (string r) => {
                Debug.Log(r);
            });

            news.On("connect", () => {
                chat.Emit("woot");
            });
            news.On("item", (string r) => {
                Debug.Log(r);
            });
        }

    }

}