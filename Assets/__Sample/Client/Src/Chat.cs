using UnityEngine;
using UnityEngine.UI;
using Unity.Linq;
using UniRx;
using UniRx.Triggers;
using System;
using System.Linq;
using System.Collections;
using socket.io;
using System.Collections.Generic;

namespace Sample {

    /// <summary>
    /// The deco code which implements the client-side of Chat service.
    /// You can download Chat service server-side code from URL below
    /// (https://github.com/socketio/socket.io/tree/master/examples/chat)
    /// </summary>
    public class Chat : MonoBehaviour {

        /// <summary>
        /// Login panel
        /// </summary>
        GameObject _login;

        /// <summary>
        /// Chat panel
        /// </summary>
        GameObject _chat;

        void Awake() {
            _login = gameObject.Descendants().First(d => d.name == "Login Panel");
            _login.SetActive(false);

            _chat = gameObject.Descendants().First(d => d.name == "Chat Panel");
            _chat.SetActive(false);
        }

        #region Json Stuctures
        /// <summary>
        /// Incoming and outgoing json event
        /// </summary>
        [Serializable]
        struct Message {
            public string username;
            public string message;

            #region User name color table
            /// <summary>
            /// User's name color table
            /// </summary>
            static string[] _colorTable = new string[] {
                "aqua",
                "black",
                "blue",
                "brown",
                "cyan",
                "darkblue",
                "fuchsia",
                "green",
                "grey",
                "lightblue",
                "lime",
                "magenta",
                "maroon",
                "navy",
                "olive",
                "orange",
                "purple",
                "red",
                "silver",
                "teal",
                "yellow"
            };
            #endregion

            /// <summary>
            /// Return a formatted text for Chat list elements
            /// </summary>
            public override string ToString() {
                var index = username.GetHashCode() % _colorTable.Length;
                return string.Format("<color={0}><b>{1}</b></color>: {2}", _colorTable[index], username, message);
            }
        }

        /// <summary>
        /// Typing and Stop-typing json event
        /// </summary>
        [Serializable]
        struct Typing {
            public string username;
        }

        /// <summary>
        /// User join or leave json event
        /// </summary>
        [Serializable]
        struct JoinOrLeave {
            public string username;
            public string numUsers;
        }

        /// <summary>
        /// User login json event
        /// </summary>
        [Serializable]
        struct Login {
            public string numUsers;
        }
        #endregion

        /// <summary>
        /// My name :)
        /// </summary>
        string _userName;

        IEnumerator Start() {
            Config.serverUrl = "http://localhost:3000";

            var socket = Socket.Connect(Config.serverUrl);
            yield return new WaitUntil(() => socket.IsConnected);
            
            #region Login-Code
            _login.SetActive(true);
            _login.Descendants()
                .First(d => d.name == "InputField")
                .GetComponent<InputField>()
                .OnSubmitAsObservable()
                .Select(e => e.selectedObject.GetComponent<InputField>())
                .Subscribe(i => {
                    _userName = i.text;
                    socket.Emit("add user", string.Format(@"""{0}""", _userName));
                });
            #endregion

            #region Chat-List-Code
            var textContents = _chat.Descendants()
                .First(d => d.name == "Content");

            /// <summary>
            /// Chat list elements
            /// first tuple => flag value for checking if it is a system message or not.
            /// second tuple => message text
            /// </summary>
            ReactiveCollection<Tuple<bool, string>> textItems = new ReactiveCollection<Tuple<bool, string>>();

            textItems.ObserveAdd()
                .Subscribe(t => {
                    var isSystemText = t.Value.Item1;
                    var newItem = GameObject.Instantiate<GameObject>(
                        isSystemText ? Resources.Load<GameObject>("System Text") : Resources.Load<GameObject>("Text"),
                        textContents.transform,
                        false);

                    newItem.name = string.Format("Text@{0}", textItems.Count);
                    newItem.transform.parent = textContents.transform;
                    newItem.GetComponent<Text>().text = t.Value.Item2;
                    newItem.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -20 * textItems.Count);

                    textContents.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 20f * textItems.Count);

                    // Keep scrollbar's position to zero (this makes last inserted message visible in textContents' rect.)
                    Observable.Timer(TimeSpan.FromMilliseconds(200))
                        .Subscribe(_ => {
                            _chat.Descendants()
                                .First(d => d.name == "Scrollbar Vertical")
                                .GetComponent<Scrollbar>()
                                .value = 0;
                        });

                    Debug.Log("NewLine => " + textItems.Last().Item2);
                });
            #endregion

            #region My-Input-Code
            var input = _chat.Descendants()
                .First(d => d.name == "InputField")
                .GetComponent<InputField>();

            input.OnSubmitAsObservable()
                .Select(e => e.selectedObject.GetComponent<InputField>())
                .Subscribe(i => {
                    var msg = new Message();
                    msg.username = _userName;
                    msg.message = i.text;
                    textItems.Add(new Tuple<bool, string>(false, msg.ToString()));

                    socket.Emit("new message", string.Format(@"""{0}""", i.text));
                    i.text = "";
                });
            
            input.OnValueChangedAsObservable().Do(_ => { socket.Emit("typing"); })
                .Throttle(TimeSpan.FromMilliseconds(200))
                .Subscribe(_ => {
                    socket.Emit("stop typing");
                });
            #endregion

            #region Typing-Event-Code
            /// <summary>
            /// Currently typing messages (Key => username, Value => Text UI gameobject)
            /// </summary>
            ReactiveDictionary<string, GameObject> typingItems = new ReactiveDictionary<string, GameObject>();

            socket.On("typing", (string r) => {
                var typing = JsonUtility.FromJson<Typing>(r);

                if (!typingItems.ContainsKey(typing.username)) {
                    var msg = new Message();
                    msg.username = typing.username;
                    msg.message = "typing...";

                    var newItem = GameObject.Instantiate<GameObject>(Resources.Load<GameObject>("Text"), textContents.transform, false);
                    newItem.name = string.Format("Typing@{0}", typing.username);
                    newItem.transform.parent = textContents.transform;
                    newItem.GetComponent<Text>().text = msg.ToString();

                    typingItems.Add(typing.username, newItem);

                    Debug.Log("Typing => " + typing.username);
                }
            });

            socket.On("stop typing", (string r) => {
                var typing = JsonUtility.FromJson<Typing>(r);

                if (typingItems.ContainsKey(typing.username)) {
                    GameObject.Destroy(typingItems[typing.username]);
                    typingItems.Remove(typing.username);
                }
            });

            typingItems.ObserveCountChanged()
                .Subscribe(_ => {
                    var list = typingItems.ToList();
                    for (int i = 0; i < list.Count; ++i)
                        list[i].Value.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -20f * (textItems.Count + i + 1));

                    textContents.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 20f * (textItems.Count + typingItems.Count));
                });

            textItems.ObserveCountChanged()
                .Subscribe(_ => {
                    var list = typingItems.ToList();
                    for (int i = 0; i < list.Count; ++i)
                        list[i].Value.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -20f * (textItems.Count + i + 1));

                    textContents.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 20f * (textItems.Count + typingItems.Count));
                });
            #endregion

            socket.On("login", (string r) => {
                _login.SetActive(false);
                _chat.SetActive(true);

                var login = JsonUtility.FromJson<Login>(r);
                textItems.Add(new Tuple<bool, string>(true, "Welcome to Socket.IO Chat - "));
                textItems.Add(new Tuple<bool, string>(true, string.Format("there's {0} participant", login.numUsers)));
            });

            socket.On("user joined", (string r) => {
                var join = JsonUtility.FromJson<JoinOrLeave>(r);
                textItems.Add(new Tuple<bool, string>(true, string.Format("{0} joined", join.username)));
                textItems.Add(new Tuple<bool, string>(true, string.Format("there's {0} participant", join.numUsers)));
            });

            socket.On("user left", (string r) => {
                var left = JsonUtility.FromJson<JoinOrLeave>(r);
                textItems.Add(new Tuple<bool, string>(true, string.Format("{0} left", left.username)));
                textItems.Add(new Tuple<bool, string>(true, string.Format("there's {0} participant", left.numUsers)));
            });

            socket.On("new message", (string r) => {
                Debug.Log("new message => " + r);
                var msg = JsonUtility.FromJson<Message>(r);
                textItems.Add(new Tuple<bool, string>(false, msg.ToString()));
            });
        }

    }

}
