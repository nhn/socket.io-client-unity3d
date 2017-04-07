using UnityEngine;
using socket.io;

namespace Sample {

    /// <summary>
    /// http://socket.io/docs/#sending-and-getting-data-(acknowledgements) 샘플
    /// </summary>
    public class Acks : MonoBehaviour {

        void Start() {
            var socket = Socket.Connect(Config.serverUrl);
            socket.On("connect", () => {
                socket.Emit("ferret", "\"toby\"", (string r) => {
                    Debug.Log(r);
                });
            });
        }

    }

}
