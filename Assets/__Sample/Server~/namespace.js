var app = require('http').createServer(handler)
var fs = require('fs');

app.listen(7001);

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

// Start socket.io
var io = require('socket.io')(app);

// chat nsp
var chat = io
  .of('/chat')
  .on('connection', function (socket) {
    chat.emit('a message', { everyone: 'in', '/chat': 'will get' });
    socket.emit('a message', { that: 'only', '/chat': 'will get' });
  });

// news nsp
var news = io
  .of('/news')
  .on('connection', function (socket) {
    socket.emit('item', { news: 'item' });
  });