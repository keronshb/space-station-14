using Content.Server.Body.Components;
using Content.Server.Chat.Systems;
using Content.Server.Doors.Systems;
using Content.Shared.Actions;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Magic;
using Content.Shared.Magic.Events;
using Robust.Shared.Audio;
using Robust.Shared.Physics.Systems;

namespace Content.Server.Magic;

public sealed class MagicSystem : SharedMagicSystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly DoorBoltSystem _boltsSystem = default!;
    [Dependency] private readonly SharedDoorSystem _doorSystem = default!;
    [Dependency] private readonly ChatSystem _chat = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<KnockSpellEvent>(OnKnockSpell);
    }

    /// <summary>
    /// Opens all doors within range
    /// </summary>
    /// <param name="args"></param>
    private void OnKnockSpell(KnockSpellEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        Speak(args);

        //Get the position of the player
        var transform = Transform(args.Performer);
        var coords = transform.Coordinates;

        _audio.PlayPvs(args.KnockSound, args.Performer, AudioParams.Default.WithVolume(args.KnockVolume));

        //Look for doors and don't open them if they're already open.
        foreach (var entity in _lookup.GetEntitiesInRange(coords, args.Range))
        {
            if (TryComp<DoorBoltComponent>(entity, out var bolts))
                _boltsSystem.SetBoltsDown(entity, bolts, false);

            if (TryComp<DoorComponent>(entity, out var doorComp) && doorComp.State is not DoorState.Open)
                _doorSystem.StartOpening(doorComp.Owner);
        }
    }

    // TODO: Refactor or get those methods into shared
    protected void Speak(BaseActionEvent args)
    {
        if (args is not ISpeakSpell speak || string.IsNullOrWhiteSpace(speak.Speech))
            return;

        _chat.TrySendInGameICMessage(args.Performer, Loc.GetString(speak.Speech),
            InGameICChatType.Speak, false);
    }
}
