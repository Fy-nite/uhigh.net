using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

public class SimpleLSPTest
{
    public static async Task TestMain(string[] args)
    {
        if (args.Length > 0 && args[0] == "simple-lsp")
        {
            await RunSimpleLSP();
        }
    }
    
    private static async Task RunSimpleLSP()
    {
        var reader = new StreamReader(Console.OpenStandardInput(), Encoding.UTF8);
        var writer = new StreamWriter(Console.OpenStandardOutput(), Encoding.UTF8) { AutoFlush = true };
        
        await Console.Error.WriteLineAsync("Simple LSP starting...");
        
        while (true)
        {
            try
            {
                // Read Content-Length header
                var line = await reader.ReadLineAsync();
                if (line == null) break;
                
                if (!line.StartsWith("Content-Length:"))
                    continue;
                
                var lengthStr = line.Substring("Content-Length:".Length).Trim();
                if (!int.TryParse(lengthStr, out var contentLength))
                    continue;
                
                // Read empty line
                await reader.ReadLineAsync();
                
                // Read content
                var buffer = new char[contentLength];
                var totalRead = 0;
                while (totalRead < contentLength)
                {
                    var read = await reader.ReadAsync(buffer, totalRead, contentLength - totalRead);
                    if (read == 0) break;
                    totalRead += read;
                }
                
                var content = new string(buffer, 0, totalRead);
                await Console.Error.WriteLineAsync($"Received: {content.Substring(0, Math.Min(100, content.Length))}...");
                
                // Simple response for initialize
                if (content.Contains("\"method\":\"initialize\""))
                {
                    var response = @"{""jsonrpc"":""2.0"",""id"":0,""result"":{""capabilities"":{""textDocumentSync"":1}}}";
                    var responseBytes = Encoding.UTF8.GetBytes(response);
                    var header = $"Content-Length: {responseBytes.Length}\r\n\r\n";
                    
                    await writer.WriteAsync(header);
                    await writer.WriteAsync(response);
                    await writer.FlushAsync();
                    
                    await Console.Error.WriteLineAsync("Sent initialize response");
                }
            }
            catch (Exception ex)
            {
                await Console.Error.WriteLineAsync($"Error: {ex}");
                break;
            }
        }
    }
}
