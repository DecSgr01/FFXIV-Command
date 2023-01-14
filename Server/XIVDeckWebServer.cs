using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using Dalamud.Logging;
using EmbedIO;
using Command.Exceptions;
using Command.Game.Chat;
using Command.Server.Helpers;

namespace Command.Server;

public class XIVDeckWebServer : IDisposable {
    private readonly IWebServer _webServer;
    private readonly CancellationTokenSource _cts = new();

    public XIVDeckWebServer(int port) {
        this._webServer = new WebServer(o => o
            .WithUrlPrefixes(GenerateUrlPrefixes(port))
            .WithMode(HttpListenerMode.Microsoft)
        );


        this._webServer.WithCors(origins: "file://");

        this._webServer.StateChanged += (_, e) => {
            PluginLog.Debug($"EmbedIO server changed state to {e.NewState.ToString()}");
        };

        this.ConfigureErrorHandlers();
        ApiControllerWiring.Autowire(this._webServer);
    }

    public bool IsRunning => (this._webServer.State != WebServerState.Stopped);

    public void StartServer() {
        this._webServer.Start(this._cts.Token);
    }

    public void Dispose() {
        this._cts.Cancel();
        GC.SuppressFinalize(this);
    }

    private static string[] GenerateUrlPrefixes(int port) {
        var prefixes = new List<string> { $"http://localhost:{port}", $"http://127.0.0.1:{port}" };
        
        if (Socket.OSSupportsIPv6) 
            prefixes.Add($"http://[::1]:{port}");

        if (Command.Instance.Configuration.ListenOnAllInterfaces) {
            PluginLog.Warning("XIVDeck is configured to listen on all interfaces!!");
            prefixes.Add($"http://*:{port}");
        }

        return prefixes.ToArray();
    }

    private void ConfigureErrorHandlers() {
        this._webServer.OnUnhandledException = (ctx, ex) => {
            // Handle known exception types first, as these can be thrown by various subsystems
            if(ex is PlayerNotLoggedInException||ex is IllegalGameStateException)
            {
                 throw HttpException.BadRequest(ex.Message, ex);
            }

            // And then fallback to unknown exceptions
            PluginLog.Error(ex, "Unhandled exception while processing request: " +
                                $"{ctx.Request.HttpMethod} {ctx.Request.Url.PathAndQuery}");
            ErrorNotifier.ShowError(ex.Message, debounce: true);
            return ExceptionHandler.Default(ctx, ex);
        };

        this._webServer.OnHttpException = (ctx, ex) => {
            var inner = ex.DataObject as Exception ?? (HttpException) ex;

            PluginLog.Warning(inner, $"Got HTTP {ex.StatusCode} while processing request: " +
                                     $"{ctx.Request.HttpMethod} {ctx.Request.Url.PathAndQuery}");

            // Only show messages to users if it's a POST request (button action)
            if (ctx.Request.HttpVerb == HttpVerbs.Post) {
                ErrorNotifier.ShowError(ex.Message ?? inner.Message, true);
            }
            return HttpExceptionHandler.Default(ctx, ex);
        };
    }
}
