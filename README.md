## Socket.IO-Client-Unity3D

Socket.IO-Client for Unity3D, which is compatible with Socket.IO v1.0 and later

## Prerequisites

* Unity3D v5.5 and later

## Platforms

* Windows
* OSX
* iOS
* Android
* WebGL

## Installation

Download Socket.IO.unitypackage from [https://github.com/nhnent/socket.io-client-unity3d/releases/download/v1.0.0/Socket.IO-Client.unitypackage](https://github.com/nhnent/socket.io-client-unity3d/releases/download/v1.0.0/Socket.IO-Client.unitypackage)

## Samples

### 1. Send and receive messages via HTTP server

#### HTTP Server
``` javascript
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

#### Unity3D Client
``` csharp
using UnityEngine;
using socket.io;

public class Events : MonoBehaviour {

        void Start() {
            var socket = Socket.Connect(Config.serverUrl);
            socket.On("news", (string r) => {
                Debug.Log(r);
                socket.Emit("my other event", "{ \"my\": \"data\" }");
            });
        }

    }
```

### 2. Acks message

#### HTTP Server
``` javascript
var io = require('socket.io')(80);

io.on('connection', function (socket) {
  socket.on('ferret', function (name, fn) {
    fn('woot');
  });
});
```

#### Client
``` csharp
using UnityEngine;
using socket.io;

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
```

### 3. Restricting yourself to a namespace

#### HTTP Server
``` javascript
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

#### Client
``` csharp
using UnityEngine;
using socket.io;

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
```

## Client Events

1. connect - Emitted when on a successful connection.
2. disconnect - Emitted when the connection is closed.
3. error - Emitted on an error.

## Versioning

* The version of TOAST Haste follows Semantic Versioning 2.0.

* Given a version number MAJOR.MINOR.PATCH, increment the:
    i. MAJOR version when you make incompatible API changes,
    ii. MINOR version when you add functionality in a backwards-compatible manner, and
    iii. PATCH version when you make backwards-compatible bug fixes.

    * Additional labels for pre-release and build metadata are available as extensions to the MAJOR.MINOR.PATCH format.

## Roadmap

* At NHN Entertainment, we service Real-time Channel ServiceMultiplayer (a.k.a. RTCS) developed by NHNEnt Blackpick.
* So, We will try to improve performance and convenience according to this roadmap.

### Milestones

| Milestone | Release Date |
| --- | --- |
| 1.0.0 | April 2017 |
| 1.0.1 | 2017 |

### Planned 1.0.1 features

Improve performance and convenience, and documentation.
Consider the performance test.

## Bug Reporting

If you find a bug, it is very important to report it. We would like to help you and smash the bug away. If you can fix a bug, you can send pull request (Should register a issue before sending PR)

### Before Reporting

Look into our issue tracker to see if the bug was already reported and you can add more information of the bug.

### Creating new issue

A bug report should contain the following

* An useful description of the bug

* The steps to reproduce the bug

* Details of system environments (OS)

* What actually happened?

* Which branch have you used?

#### Thank you for reporting a bug!

## Mailing list

dl_rtcs@nhnent.com

## Contributor

* Junhwan, Oh
* Doyoung, An
* Chanyoung, Park

## License

Socket.IO-Client-Unity3D is licensed under the Apache 2.0 license, see LICENSE for details.

```
The MIT License (MIT)

Copyright (c) 2017 NHN Entertainment Corp.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```
