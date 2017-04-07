socket.io-client-unity3d
====
Socket.io for Unity3d client, which is compatible with Socket.IO v1.0 and later.

# Install
Download **socket.io.unitypackage** file from https://github.nhnent.com/rtcs/socket.io-client-unity3d/releases

# Usages
## 1. Using with Node http server
* Server (app.js)

```javascript
var app = require('http').createServer(handler)
var io = require('socket.io')(app);
var fs = require('fs');

app.listen(80);

function handler (req, res) {
  fs.readFile(__dirname + '/index.html',
  function (err, data) {
    if (err) {
      res.writeHead(500);
      return res.end('Error loading index.html');
    }

    res.writeHead(200);
    res.end(data);
  });
}

io.on('connection', function (socket) {
  socket.emit('news', { hello: 'world' });
  socket.on('my other event', function (data) {
    console.log(data);
  });
});
```

* Client (Events.cs)

```c#
using UnityEngine;
using socket.io;

namespace Sample {
    public class Events : MonoBehaviour {
        void Start() {
            var socket = Socket.Connect("http://localhost:80");
            socket.On("news", (string r) => {
                Debug.Log(r);
                socket.Emit("my other event", "{ \"my\": \"data\" }");
            });
        }
    }
}
```

## 2. Sending and getting data (acknowledgements)
* Server (app.js)

```javascript
var io = require('socket.io')(80);

io.on('connection', function (socket) {
  socket.on('ferret', function (name, fn) {
    fn('woot');
  });
});
```

* Client (Acks.cs)

```c#
using UnityEngine;
using socket.io;

namespace Sample {
    public class Acks : MonoBehaviour {
        void Start() {
            var socket = Socket.Connect("http://localhost:80");
            socket.On("connect", () => {
                socket.Emit("ferret", "\"toby\"", (string r) => {
                    Debug.Log(r);
                });
            });
        }
    }
}
```

## 3. Restricting yourself to a namespace
* Server (app.js)

```javascript
var io = require('socket.io')(80);
var chat = io
  .of('/chat')
  .on('connection', function (socket) {
    socket.emit('a message', {
        that: 'only'
      , '/chat': 'will get'
    });
    chat.emit('a message', {
        everyone: 'in'
      , '/chat': 'will get'
    });
  });

var news = io
  .of('/news')
  .on('connection', function (socket) {
    socket.emit('item', { news: 'item' });
  });
```

* Client (Namespace.cs)

```c#
using UnityEngine;
using socket.io;

namespace Sample {
    public class Namespace : MonoBehaviour {
        void Start() {
            var chat = Socket.Connect("http://localhost:80/chat");
            var news = Socket.Connect("http://localhost:80/news");
            
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
```

