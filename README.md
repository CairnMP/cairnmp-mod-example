# CairnMP mod example

A starting point for a MelonLoader mod for *Cairn* that works in
[CairnMP](https://github.com/CairnMP/cairnmp-mod) multiplayer sessions, through
the host-authoritative `CairnMultiplayer.Api`.

The example adds **team cheers**: in a session, press **F7**, **F8** or **F9**
to send a cheer to the other players. It shows every part of the API you need
for a real mod:

| What | Where |
| --- | --- |
| Registering an extension with a version range | `TeamCheers.Register` |
| A client command the host validates and rate-limits | `TeamCheers.HandleCheer` |
| Global and per-player state, replayed to late joiners | `team-cheers`, `cheers-by-player` |
| A transient event sent to every peer | `cheered` |
| A host-initiated update | `TeamCheers.OnSessionReady` |
| Reading a command result on the game thread | `TeamCheers.Update` |
| Running solo when CairnMP is not installed | `ExampleMod.OnInitializeMelon` |

Read the
[managed extension API guide](https://github.com/CairnMP/cairnmp-mod/blob/develop/CairnMultiplayerMod/Api/README.md)
for the full reference.

## Requirements

- *Cairn* with [CairnMP](https://github.com/CairnMP/cairnmp-mod/releases)
  installed, launched at least once so MelonLoader generates its IL2CPP
  assemblies.
- The [.NET SDK](https://dotnet.microsoft.com/download) 6 or later. The mod
  targets `net6.0`, because MelonLoader hosts mods in .NET 6.

The build references assemblies from your own game install: CairnMP,
MelonLoader, and the IL2CPP assemblies MelonLoader generated. None of them is
copied into the build output or the repository. Don't commit them.

## Build

By default, the project looks for the copy of Cairn that CairnMP launches:

```text
%LOCALAPPDATA%\CairnMultiplayerData\game
```

If your game is elsewhere, or you build on Linux, pass the folder that holds
`Mods` and `MelonLoader`:

```bash
dotnet build src/CairnMpModExample.csproj -c Release -p:CairnDir="/path/to/Cairn"
```

or set the `CAIRN_DIR` environment variable. On Linux with Proton, that folder
is inside the game's Proton prefix.

To copy the built DLL into the game's `Mods` folder after the build, add
`-p:DeployMod=true`.

## Try it

1. Build with `-p:DeployMod=true`, or copy `CairnMpModExample.dll` into
   `Mods` next to `CairnMultiplayerMod.dll`.
2. Start Cairn through CairnMP. `MelonLoader/Latest.log` shows
   `CairnMP integration ready`.
3. Host a lobby, and have a second player with the same mod join.
4. Press **F7**, **F8** or **F9**. Both logs show the cheer and the team
   count.

Players without the mod can still join: the extension is `Optional`, so cheers
are simply off for them. Failures in your extension appear in the log as
`[Extensions:com.example.team-cheers]`.

## Make it yours

1. Rename the project, the namespace and the `MelonInfo` attribute in
   `ExampleMod.cs`.
2. Change `ExtensionId` in `TeamCheers.cs` to a reverse-domain ID you own, in
   lowercase. Two mods with the same ID cannot load together.
3. Replace the cheers with your own commands, state and events. Keep every
   `CairnMultiplayer.Api` call in classes that `ExampleMod` only reaches through
   non-inlined methods, as `TeamCheers` does, so the mod still loads without
   CairnMP.
4. Bump the extension version when you change a contract, and narrow
   `MinimumPeerVersion`/`MaximumPeerVersion` when the change is incompatible.

If your mod only makes sense in multiplayer, replace
`MelonOptionalDependencies` with `MelonAdditionalDependencies` and set
`Requirement = ExtensionRequirement.Required`: MelonLoader then refuses to load
it without CairnMP, and the host rejects players who don't have it.

### Rules worth keeping

- Command handlers run on the host. Treat every request as untrusted: check its
  values and `Reject` bad ones instead of throwing. Three exceptions in a row
  disable your extension for the session.
- Change the game in `AfterCommit`, `Changed` or `Received`, which run on the
  game thread, never after an `await`.
- Use state for anything a late joiner must know, and events for one-off
  effects.
- Every payload is limited to 48 KiB.

## License

The example is released under the [MIT License](LICENSE): copy it, change it and
publish your mod under the license you choose. CairnMP itself is licensed under
AGPL-3.0. *Cairn*, Unity and MelonLoader assemblies belong to their owners and
must not be redistributed with your mod.
