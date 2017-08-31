using System.Collections;
using UnityEngine;
using socket.io;

namespace Sample {

    /// <summary>
    /// The basic sample to show how to connect to a server
    /// </summary>
    public class Connect : MonoBehaviour {

        IEnumerator Start() {
            var serverUrl = "http://localhost:7001";
            var socket = Socket.Connect(serverUrl);
            
            socket.On(SystemEvents.connect, () => {
                Debug.Log("Hello, Socket.io~");
            });

            socket.On(SystemEvents.reconnect, (int reconnectAttempt) => {
                Debug.Log("Hello, Again! " + reconnectAttempt);
            });

            socket.On(SystemEvents.disconnect, () => {
                Debug.Log("Bye~");
            });

            yield return new WaitForSeconds(1f);

            Socket.Disconnect(socket);

            yield return new WaitForSeconds(1f);

            Socket.Reconnect(socket);
        }

    }

}
