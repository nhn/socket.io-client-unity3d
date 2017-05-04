var app = require('http').createServer(handler)
var fs = require('fs');

app.listen(4444);

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

// Socket.io 스타트
var io = require('socket.io')(app);

// 클라이언트 컨넥션 이벤트 처리
io.on('connection', function (socket) { 

   socket.on('ferret', function (name, fn) {
        // 'woot' 문자열을 Ack 메세지로 보냄
        fn('woot');
    }); 
});