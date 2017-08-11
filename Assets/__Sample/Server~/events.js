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

// handle client's connection event
io.on('connection', function (socket) {

    // send 'news' event
    socket.emit('news', { hello: 'world' });

    // receive 'my other event' event
    socket.on('my other event', function(data) {
        console.log(data);
    });
    
});