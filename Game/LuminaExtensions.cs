using Lumina.Excel.GeneratedSheets;

namespace Command.Game; 

public static class LuminaExtensions {
    public static bool IsUnlocked(this Emote emote) => GameStateCache.IsEmoteUnlocked(emote);
    public static bool IsUnlocked(this Companion minion) => GameStateCache.IsMinionUnlocked(minion.RowId);
    public static bool IsUnlocked(this Mount mount) => GameStateCache.IsMountUnlocked(mount.RowId);
    public static bool IsUnlocked(this Ornament ornament) => GameStateCache.IsOrnamentUnlocked(ornament.RowId);
}