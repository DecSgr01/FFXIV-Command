using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace Command.UI.Windows;

public class SettingsWindow : Window
{
  public const string WindowKey = "###xivDeckSettingsWindow";

  private readonly Command _plugin = Command.Instance;

  // settings
  private int Port;
  private bool _safeMode = true;
  private bool _listenOnAllInterfaces;

  public SettingsWindow(bool forceMainWindow = true) :
      base(WindowKey, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoCollapse, forceMainWindow)
  {

    var minSize = new Vector2(350, 250);

    this.Size = minSize;
    this.SizeCondition = ImGuiCond.FirstUseEver;
    this.SizeConstraints = new WindowSizeConstraints
    {
      MinimumSize = minSize,
      MaximumSize = new Vector2(450, 400)
    };

    this.IsOpen = true;
  }

  public override void OnOpen()
  {
    this.Port = this._plugin.Configuration.Port;
    this._safeMode = this._plugin.Configuration.SafeMode;
    this._listenOnAllInterfaces = this._plugin.Configuration.ListenOnAllInterfaces;
  }

  public override void OnClose()
  {
    this._plugin.WindowSystem.RemoveWindow(this);
  }

  public override void Draw()
  {
    var windowSize = ImGui.GetContentRegionAvail();
    this.WindowName = "Command Plugin设置" + WindowKey;

    var pbs = ImGuiHelpers.GetButtonSize("placeholder");

    ImGui.BeginChild("SettingsPane", windowSize with { Y = windowSize.Y - pbs.Y - 6 });

    if (!this._safeMode)
    {
      ImGui.PushTextWrapPos();
      ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudRed);
      ImGui.Text("危险：禁用安全模式！您可能可以从您的Command向游戏发送非法命令");
      ImGui.PopStyleColor();
      ImGui.PopTextWrapPos();
      ImGui.Spacing();
    }

    ImGui.PushItemWidth(80);

    if (ImGui.InputInt("API端口", ref this.Port, 0))
    {
      if (this.Port < 1024) this.Port = 1024;
      if (this.Port > 59999) this.Port = 59999;
    }

    ImGui.PopItemWidth();
    ImGuiComponents.HelpMarker(string.Format("默认端口: {0}范围: {1}-{2}", 37984, 1024, 59999));

    if (this._plugin.Configuration.ListenOnAllInterfaces)
    {
      ImGui.Checkbox("在所有接口上监听", ref this._listenOnAllInterfaces);
      ImGui.PushTextWrapPos();
      ImGui.TextWrapped(string.Format("监听IP: {0}", "0.0.0.0"));
      ImGui.TextColored(ImGuiColors.DalamudYellow, "命令可从网络上的计算机访问");
      ImGui.PopTextWrapPos();
    }
    else
    {
      ImGui.TextWrapped(string.Format("监听IP: {0}", "127.0.0.1"));
    }

    ImGui.Spacing();
    ImGui.EndChild();
    /* FOOTER */
    ImGui.Separator();

    var applyText = "应用设置";
    var applyButtonSize = ImGuiHelpers.GetButtonSize(applyText);

    ImGui.SameLine(windowSize.X - applyButtonSize.X - 5);
    if (ImGui.Button(applyText))
    {
      this.SaveSettings();
    }
  }

  private void SaveSettings()
  {
    if (this.Port != this._plugin.Configuration.Port)
    {
      this._plugin.Configuration.Port = this.Port;
    }

    this._plugin.Configuration.ListenOnAllInterfaces = this._listenOnAllInterfaces;
    this._plugin.Configuration.Save();
    // initialize regardless of change(s) so that we can easily restart the server when necessary
    this._plugin.InitializeWebServer();
  }
}