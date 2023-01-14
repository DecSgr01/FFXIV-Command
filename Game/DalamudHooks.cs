using System;
using Command.Game.Chat;

namespace Command.Game; 

public static class DalamudHooks {
    public static void OnGameLogin(object? obj, EventArgs eventArgs) {
        // game state isn't ready until login succeeds, so we wait for it to be ready before updating cache
        Command.Instance.GameStateCache.Refresh();
        DeferredChat.SendDeferredMessages(6000);
    }
}