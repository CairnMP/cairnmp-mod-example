using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CairnMultiplayer.Api;
using MelonLoader;

namespace CairnMpModExample;

/// <summary>
/// Every CairnMultiplayer.Api call of the mod lives here. The entry points are never
/// inlined into ExampleMod, so without CairnMP the missing assembly fails inside
/// Register, where ExampleMod catches it, instead of breaking the whole mod.
/// </summary>
internal static class TeamCheers
{
    private const string ExtensionId = "com.example.team-cheers";
    private const double CheerCooldownSeconds = 2.0;

    private static MelonLogger.Instance _log;
    private static MultiplayerExtension _extension;
    private static ReplicatedState<int> _teamCheers;
    private static ReplicatedState<int> _cheersByPlayer;
    private static MultiplayerEvent<CheerKind> _cheered;
    private static MultiplayerCommand<CheerRequest> _cheer;
    private static Task<CommandResult> _pendingCheer;

    // Host only: when each player last had a cheer accepted.
    private static readonly Dictionary<int, DateTime> LastCheerByPlayer = new();

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void Register(MelonLogger.Instance log)
    {
        _log = log;
        _extension = MultiplayerApi.RegisterExtension(new ExtensionRegistration(
            ExtensionId, new Version(1, 0, 0))
        {
            // Players without this mod can still join; cheers are off for them.
            Requirement = ExtensionRequirement.Optional,
            MinimumPeerVersion = new Version(1, 0, 0),
            MaximumPeerVersion = new Version(1, 0, 99),
        });

        _teamCheers = _extension.RegisterState<int>("team-cheers");
        _cheersByPlayer = _extension.RegisterState<int>("cheers-by-player");
        _cheered = _extension.RegisterEvent<CheerKind>("cheered");
        _cheer = _extension.RegisterCommand<CheerRequest>("cheer", HandleCheer);

        _teamCheers.Changed += change =>
        {
            if (!change.Removed) _log.Msg($"Team cheers: {change.Value}");
        };
        _cheered.Received += OnCheered;

        MultiplayerApi.SessionReady += OnSessionReady;
        MultiplayerApi.SessionEnded += LastCheerByPlayer.Clear;
        MultiplayerApi.PlayerLeft += player => LastCheerByPlayer.Remove(player.Id);
        MultiplayerApi.ExtensionDisabled += (extensionId, reason) =>
        {
            if (extensionId == ExtensionId) _log.Warning($"Cheers disabled for this session: {reason}");
        };
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void Request(CheerKind kind)
    {
        if (!MultiplayerApi.IsConnected || _pendingCheer != null) return;
        _pendingCheer = _cheer.SendAsync(new CheerRequest { Kind = kind });
    }

    /// <summary>
    /// Reads the host's answer from OnUpdate. Code after an await on SendAsync can run on
    /// a worker thread, where touching the game can crash it.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void Update()
    {
        if (_pendingCheer is not { IsCompleted: true }) return;
        var result = _pendingCheer.Result;
        _pendingCheer = null;
        if (!result.Committed)
            _log.Warning($"Cheer not sent ({result.Status}): {result.Reason}");
    }

    // Runs on the host only. The request comes from another player's machine: check it.
    private static void HandleCheer(HostCommandContext<CheerRequest> context)
    {
        var request = context.Request;
        if (request == null || !Enum.IsDefined(request.Kind))
        {
            context.Reject("Unknown cheer.");
            return;
        }

        int senderId = context.Sender.Id;
        var now = DateTime.UtcNow;
        if (LastCheerByPlayer.TryGetValue(senderId, out var last)
            && (now - last).TotalSeconds < CheerCooldownSeconds)
        {
            context.Reject("Wait a moment before cheering again.");
            return;
        }

        _teamCheers.TryGet(out var teamCheers);
        _cheersByPlayer.TryGetForPlayer(senderId, out var playerCheers);
        context.Set(_teamCheers, teamCheers + 1);
        context.SetForPlayer(_cheersByPlayer, senderId, playerCheers + 1);
        context.Broadcast(_cheered, request.Kind);
        // Only start the cooldown once the cheer is really published.
        context.AfterCommit(() => LastCheerByPlayer[senderId] = now);
    }

    private static void OnSessionReady()
    {
        // Publishes an explicit zero, so every player sees the counter before the first cheer.
        if (MultiplayerApi.IsHost)
            _extension.Commit(context => context.Set(_teamCheers, 0));
    }

    // Runs on every peer, on the game thread: a real mod would show this on screen.
    private static void OnCheered(MultiplayerEventMessage<CheerKind> message)
    {
        // States are applied before events, so the count already includes this cheer.
        _cheersByPlayer.TryGetForPlayer(message.SourcePlayerId, out var playerCheers);
        _log.Msg($"{PlayerName(message.SourcePlayerId)}: {Describe(message.Payload)} ({playerCheers} so far)");
    }

    private static string PlayerName(int playerId)
    {
        foreach (var player in MultiplayerApi.Players)
        {
            if (player.Id == playerId) return player.Name;
        }
        return $"Player {playerId}";
    }

    private static string Describe(CheerKind kind) => kind switch
    {
        CheerKind.NiceMove => "Nice move!",
        CheerKind.TakeABreak => "Let's take a break.",
        CheerKind.Summit => "Summit!",
        _ => "Cheers!",
    };
}
