#nullable enable
using System;
using Dalamud.Configuration;

namespace Command.Base; 

[Serializable]
public class PluginConfig : IPluginConfiguration {
    public int Version { get; set; } = 0;
    /**
         * Disabling safe mode allows for certain verification checks that the plugin does to be skipped.
         * This value can *only* be set through manual configuration edits.
         */
    public bool SafeMode { get; set; } = true;
       
    public int Port { get; set; } = 37984;

    /// <summary>
    /// Configure XIVDeck to listen to all available interfaces, rather than just localhost.
    ///
    /// This setting is config file only, as it introduces security concerns.
    /// </summary>
    public bool ListenOnAllInterfaces { get; set; } = false;

    public void Save() {
        Injections.PluginInterface.SavePluginConfig(this);
    }
}