using System.Globalization;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Command.Base;
using Command.Game;
using Command.Game.Chat;
using Command.Server;
using Command.UI;
using Command.UI.Windows;

namespace Command;

// ReSharper disable once ClassNeverInstantiated.Global - instantiation handled by Dalamud
public sealed class Command : IDalamudPlugin
{
  internal static Command Instance { get; private set; } = null!;
  public string Name => "Command Plugin";
  internal PluginConfig Configuration { get; }
  internal WindowSystem WindowSystem { get; }
  internal SigHelper SigHelper { get; }
  internal GameStateCache GameStateCache { get; }

  private DalamudPluginInterface PluginInterface { get; }
  private XIVDeckWebServer _xivDeckWebServer = null!;
  private readonly ChatLinkWiring _chatLinkWiring;

  public Command(DalamudPluginInterface pluginInterface)
  {
    pluginInterface.Create<Injections>();

    Instance = this;
    this.PluginInterface = pluginInterface;

    this.Configuration = this.PluginInterface.GetPluginConfig() as PluginConfig ?? new PluginConfig();

    // Various managers for advanced hooking into the game
    this.SigHelper = new SigHelper();

    // Load in and initialize a lot of various game state and plugin interface things.
    this.GameStateCache = new GameStateCache();
    // More plugin interfaces
    this._chatLinkWiring = new ChatLinkWiring();
    this.WindowSystem = new WindowSystem(this.Name);

    // Start the websocket server itself.
    this.InitializeWebServer();

    this.PluginInterface.UiBuilder.Draw += this.WindowSystem.Draw;
    this.PluginInterface.UiBuilder.OpenConfigUi += this.DrawConfigUI;

    Injections.ClientState.Login += DalamudHooks.OnGameLogin;

  }

  public void Dispose()
  {
    this.WindowSystem.RemoveAllWindows();
    DeferredChat.Cancel();
    this._xivDeckWebServer.Dispose();
    this._chatLinkWiring.Dispose();
    this.SigHelper.Dispose();

    Injections.ClientState.Login -= DalamudHooks.OnGameLogin;

    // setting to null here is okay as this will only be called on plugin teardown.
    // Nothing should *ever* run past this point.
    Instance = null!;
  }

  internal void DrawConfigUI()
  {
    var instance = this.WindowSystem.GetWindow(SettingsWindow.WindowKey);

    if (instance == null)
    {
      this.WindowSystem.AddWindow(new SettingsWindow());
    }
  }

  internal void InitializeWebServer()
  {
    if (this._xivDeckWebServer is { IsRunning: true })
    {
      this._xivDeckWebServer.Dispose();
    }
    this._xivDeckWebServer = new XIVDeckWebServer(this.Configuration.Port);
    this._xivDeckWebServer.StartServer();
  }
}