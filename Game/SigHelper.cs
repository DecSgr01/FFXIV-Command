using System;
using System.Runtime.InteropServices;
using System.Text;
using Dalamud.Hooking;
using Dalamud.Logging;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.System.Memory;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Common.Configuration;
using Command.Game.Structs;

// ReSharper disable InconsistentNaming - matching expected documentation things
// ReSharper disable UnusedAutoPropertyAccessor.Local - handled by siggingway and reflection

namespace Command.Game;

internal unsafe class SigHelper : IDisposable
{
    private static class Signatures
    {
        // todo: this is *way* too broad. this game is very write-happy when it comes to gearset updates.
        internal const string SaveGearset = "48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 30 48 8B F2 48 8B F9 33 D2";

        // This signature appears to be called whenever the Macro Agent does some form of update operation on a macro.
        // I'm not exactly sure what this method docs, but it seems to be relatively reliable.
        // According to aers, this might be related to marking the macro page as modified for file update purposes.
        // called in UI::Agent::AgentMacro::ReceiveEvent a few times - generally immediately after LOBYTE(x) = 1 call
        internal const string UpdateMacro = "E8 ?? ?? ?? ?? 83 3E 00";

        internal const string SendChatMessage = "48 89 5C 24 ?? 57 48 83 EC 20 48 8B FA 48 8B D9 45 84 C9";
        internal const string SanitizeChatString = "E8 ?? ?? ?? ?? EB 0A 48 8D 4C 24 ?? E8 ?? ?? ?? ?? 48 8D 8D";

        internal const string SetConfigValueUInt = "E8 ?? ?? ?? ?? 0F B6 5E 73";
    }

    /***** functions *****/
    [Signature(Signatures.SanitizeChatString, Fallibility = Fallibility.Fallible)]
    private readonly delegate* unmanaged<Utf8String*, int, IntPtr, void> _sanitizeChatString = null!;

    // UIModule, message, unused, byte
    [Signature(Signatures.SendChatMessage, Fallibility = Fallibility.Fallible)]
    private readonly delegate* unmanaged<IntPtr, IntPtr, IntPtr, byte, void> _processChatBoxEntry = null!;

    [Signature(Signatures.SetConfigValueUInt, Fallibility = Fallibility.Fallible)]
    private readonly delegate* unmanaged<ConfigEntry*, uint, byte> _setConfigValueUint = null!;

    /***** hooks *****/
    private delegate IntPtr RaptureGearsetModule_WriteFile(IntPtr a1, IntPtr a2);
    private delegate IntPtr MacroUpdate(IntPtr a1, IntPtr macroPage, IntPtr macroNumber);

    [Signature(Signatures.SaveGearset, DetourName = nameof(DetourGearsetSave))]
    private Hook<RaptureGearsetModule_WriteFile>? RGM_WriteFileHook { get; init; }

    [Signature(Signatures.UpdateMacro, DetourName = nameof(DetourMacroUpdate))]
    private Hook<MacroUpdate>? MacroUpdateHook { get; init; }

    /***** the actual class *****/

    internal SigHelper()
    {
        SignatureHelper.Initialise(this);

        this.RGM_WriteFileHook?.Enable();
        this.MacroUpdateHook?.Enable();
    }

    public void Dispose()
    {
        this.RGM_WriteFileHook?.Dispose();
        this.MacroUpdateHook?.Dispose();

        GC.SuppressFinalize(this);
    }

    internal string GetSanitizedString(string input)
    {
        var uString = Utf8String.FromString(input);

        this._sanitizeChatString(uString, 0x27F, IntPtr.Zero);
        var output = uString->ToString();

        uString->Dtor();
        IMemorySpace.Free(uString);

        return output;
    }

    internal void SendChatMessage(string message)
    {
        if (this._processChatBoxEntry == null)
        {
            throw new InvalidOperationException("Signature for ProcessChatBoxEntry/SendMessage not found!");
        }

        var messageBytes = Encoding.UTF8.GetBytes(message);

        switch (messageBytes.Length)
        {
            case 0:
                throw new ArgumentException(@"Message cannot be empty", nameof(message));
            case > 500:
                throw new ArgumentException(@"Message exceeds 500char limit", nameof(message));
        }

        var payloadMem = Marshal.AllocHGlobal(400);
        Marshal.StructureToPtr(new ChatPayload(messageBytes), payloadMem, false);

        this._processChatBoxEntry((IntPtr)Framework.Instance()->GetUiModule(), payloadMem, IntPtr.Zero, 0);

        Marshal.FreeHGlobal(payloadMem);
    }

    internal bool IsQuestCompleted(uint questId)
    {
        return QuestManager.IsQuestComplete(questId);
    }

    internal void CalcBForSlot(HotBarSlot* slot, out HotbarSlotType actionType, out uint actionId)
    {
        var hotbarModule = Framework.Instance()->GetUiModule()->GetRaptureHotbarModule();

        // Take in default values, just in case GetSlotAppearance fails for some reason
        var acType = slot->IconTypeB;
        var acId = slot->IconB;
        ushort acCA = slot->UNK_0xCA;

        RaptureHotbarModule.GetSlotAppearance(&acType, &acId, &acCA, hotbarModule, slot);

        actionType = acType;
        actionId = acId;
    }

    internal int CalcIconForSlot(HotBarSlot* slot)
    {
        if (slot->CommandType == HotbarSlotType.Empty)
        {
            return 0;
        }

        this.CalcBForSlot(slot, out var slotActionType, out var slotActionId);
        return slot->GetIconIdForSlot(slotActionType, slotActionId);
    }

    internal bool SetConfigValueUInt(ConfigEntry* entry, uint value = 1)
    {
        if (this._setConfigValueUint == null)
        {
            return false;
        }

        return this._setConfigValueUint(entry, value) == 1;
    }

    private IntPtr DetourGearsetSave(IntPtr a1, IntPtr a2)
    {
        PluginLog.Debug("Gearset update!");
        var tmp = this.RGM_WriteFileHook!.Original(a1, a2);
        return tmp;
    }

    private IntPtr DetourMacroUpdate(IntPtr a1, IntPtr macroPage, IntPtr macroSlot)
    {
        PluginLog.Debug("Macro update!");
        var tmp = this.MacroUpdateHook!.Original(a1, macroPage, macroSlot);
        return tmp;
    }
}