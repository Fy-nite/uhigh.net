using System.Text;
using uhigh.Net.LanguageServer.Core;

namespace uhigh.Net.LanguageServer
{
    public class LanguageServerHost
    {
        private readonly UhighLanguageServer _server;
        private readonly Stream _input;
        private readonly Stream _output;

        public LanguageServerHost(Stream input, Stream output)
        {
            _server = new UhighLanguageServer();
            _input = input;
            _output = output;
        }

        public async Task RunAsync()
        {
            try
            {
                var reader = new StreamReader(_input, Encoding.UTF8);
                var writer = new StreamWriter(_output, Encoding.UTF8) { AutoFlush = true };

                // Provide the output writer to the server for notifications
                _server.SetOutputStream(writer);

                var messageCount = 0;

                while (true)
                {
                    try
                    {
                        var message = await ReadMessage(reader);
                        if (message == null)
                        {
                            break;
                        }

                        messageCount++;
                        var response = await _server.ProcessRequest(message);
                        if (!string.IsNullOrEmpty(response))
                        {
                            await WriteMessage(writer, response);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Only break on fatal errors
                        if (ex is System.IO.EndOfStreamException || ex is ObjectDisposedException)
                        {
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Silent failure - don't pollute the LSP stream
                throw;
            }
        }

        private async Task<string?> ReadMessage(StreamReader reader)
        {
            try
            {
                string? line;
                int contentLength = 0;

                // Read headers - block until we get something
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    if (string.IsNullOrEmpty(line))
                        break; // End of headers

                    if (line.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase))
                    {
                        var lengthStr = line.Substring("Content-Length:".Length).Trim();
                        int.TryParse(lengthStr, out contentLength);
                    }
                }

                if (contentLength <= 0)
                {
                    return null;
                }

                // Read content
                var buffer = new char[contentLength];
                var totalRead = 0;

                while (totalRead < contentLength)
                {
                    var read = await reader.ReadAsync(buffer, totalRead, contentLength - totalRead);
                    if (read == 0)
                    {
                        return null;
                    }
                    totalRead += read;
                }

                var content = new string(buffer, 0, totalRead);
                return content;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        private async Task WriteMessage(StreamWriter writer, string message)
        {
            try
            {
                var contentBytes = Encoding.UTF8.GetBytes(message);
                var header = $"Content-Length: {contentBytes.Length}\r\n\r\n";

                await writer.WriteAsync(header);
                await writer.WriteAsync(message);
                await writer.FlushAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }

    // This is an alternative entry point that can be used if you want to run just the language server
    // To use this, compile with: dotnet build -o lsp
    // Then run: ./lsp/uhigh --lsp
    /*
    public class LanguageServerProgram
    {
        public static async Task Main(string[] args)
        {
            var host = new LanguageServerHost(Console.OpenStandardInput(), Console.OpenStandardOutput());
            await host.RunAsync();
        }
    }
    */
}