using System.Net;
using System.Net.WebSockets;
using System.Text;

namespace UhighLanguageServer
{
    class srv
    {
        public static async Task StartServerAsync(bool useWebSocket = true, int wsPort = 5000)
        {
            if (!useWebSocket)
            {
                Console.OutputEncoding = new UTF8Encoding();
                var app = new App(Console.OpenStandardInput(), Console.OpenStandardOutput());
                Logger.Instance.Attach(app);
                try
                {
                    await Task.Run(() =>
                    {
                        app.Listen();
                        while (true) { System.Threading.Thread.Sleep(100); }
                    });
                }
                catch (AggregateException ex)
                {
                    Console.Error.WriteLine(ex.InnerExceptions[0]);
                    Environment.Exit(-1);
                }
            }
            else
            {
                // WebSocket mode
                var listener = new HttpListener();
                listener.Prefixes.Add($"http://localhost:{wsPort}/");
                listener.Start();
                Console.WriteLine($"μHigh LSP WebSocket server listening on ws://localhost:{wsPort}/");
                while (true)
                {
                    var httpContext = await listener.GetContextAsync();
                    if (httpContext.Request.IsWebSocketRequest)
                    {
                        var wsContext = await httpContext.AcceptWebSocketAsync(null);
                        var ws = wsContext.WebSocket;
                        // Wrap WebSocket as Stream
                        using var wsStream = new WebSocketStream(ws);
                        var app = new App(wsStream, wsStream);
                        Logger.Instance.Attach(app);
                        await Task.Run(() => app.Listen());
                        break; // Only handle one connection for now
                    }
                    else
                    {
                        httpContext.Response.StatusCode = 400;
                        httpContext.Response.Close();
                    }
                }
            }
        }
    }

    // Helper: Wrap WebSocket as Stream for LSP
    public class WebSocketStream : Stream
    {
        private readonly WebSocket _ws;
        private readonly MemoryStream _readBuffer = new();
        public WebSocketStream(WebSocket ws) { _ws = ws; }
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_readBuffer.Length == 0 || _readBuffer.Position == _readBuffer.Length)
            {
                var seg = new ArraySegment<byte>(new byte[8192]);
                var result = _ws.ReceiveAsync(seg, CancellationToken.None).Result;
                _readBuffer.SetLength(0);
                _readBuffer.Position = 0;
                _readBuffer.Write(seg.Array!, 0, result.Count);
                _readBuffer.Position = 0;
            }
            return _readBuffer.Read(buffer, offset, count);
        }
        public override void Write(byte[] buffer, int offset, int count)
        {
            var seg = new ArraySegment<byte>(buffer, offset, count);
            _ws.SendAsync(seg, WebSocketMessageType.Text, true, CancellationToken.None).Wait();
        }
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return Task.Run(() => Read(buffer, offset, count), cancellationToken);
        }
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            Write(buffer, offset, count);
            return Task.CompletedTask;
        }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
    }
}
