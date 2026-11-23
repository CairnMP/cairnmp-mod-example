using System.IO;
using CairnMpModExample;
using MelonLoader;
using UnityEngine.InputSystem;

[assembly: MelonInfo(typeof(ExampleMod), "CairnMP Mod Example", "1.0.0", "Your name")]
[assembly: MelonGame("TheGameBakers", "Cairn")]
// Loads without CairnMP: TeamCheers is only touched once the API resolved.
[assembly: MelonOptionalDependencies("CairnMultiplayerMod")]

namespace CairnMpModExample;

/// <summary>
/// MelonLoader entry point. It only reads input and forwards it, so the mod keeps
/// running solo when CairnMP is not installed.
/// </summary>
public sealed class ExampleMod : MelonMod
{
    private bool _multiplayerAvailable;

    public override void OnInitializeMelon()
    {
        try
        {
            TeamCheers.Register(LoggerInstance);
            _multiplayerAvailable = true;
            LoggerInstance.Msg("CairnMP integration ready. In a session, press F7, F8 or F9 to cheer.");
        }
        catch (FileNotFoundException)
        {
            LoggerInstance.Msg("CairnMP is not installed: multiplayer cheers are off.");
        }
    }

    public override void OnUpdate()
    {
        if (!_multiplayerAvailable) return;

        TeamCheers.Update();

        var keyboard = Keyboard.current;
        if (keyboard == null) return;
        if (keyboard.f7Key.wasPressedThisFrame) TeamCheers.Request(CheerKind.NiceMove);
        else if (keyboard.f8Key.wasPressedThisFrame) TeamCheers.Request(CheerKind.TakeABreak);
        else if (keyboard.f9Key.wasPressedThisFrame) TeamCheers.Request(CheerKind.Summit);
    }
}
