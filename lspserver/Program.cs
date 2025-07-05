using LanguageServer.Infrastructure.JsonDotNet;
using LanguageServer.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UhighLanguageServer
{
    class srv
    {
       public static async Task StartServerAsync()
{
    Console.OutputEncoding = new UTF8Encoding(); // UTF8N for non-Windows platform
    var app = new App(Console.OpenStandardInput(), Console.OpenStandardOutput());
    Logger.Instance.Attach(app);
    try
    {
        await Task.Run(() => app.Listen());
    }
    catch (AggregateException ex)
    {
        Console.Error.WriteLine(ex.InnerExceptions[0]);
        Environment.Exit(-1);
    }
}

    }
}
