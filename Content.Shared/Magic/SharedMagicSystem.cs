using System.Numerics;
using Content.Shared.Actions;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chat;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.DoAfter;
using Content.Shared.Interaction.Events;
using Content.Shared.Magic.Components;
using Content.Shared.Magic.Events;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Content.Shared.Spawners.Components;
using Content.Shared.Storage;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager;

namespace Content.Shared.Magic;

// TODO: 2 split spellbooks into their own ECS
// TODO: 3 suffer when refactoring
// TODO: MIND

/// <summary>
/// Handles learning and using spells (actions)
/// </summary>
public abstract class SharedMagicSystem : EntitySystem
{
    [Dependency] private readonly ISerializationManager _seriMan = default!;
    [Dependency] private readonly IComponentFactory _compFact = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedGunSystem _gunSystem = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedChatSystem _chat = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedBodySystem _bodySystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        // TODO: Move Spellbooks into their own system
        // TODO: Make Master Spellbook (pointbuy)
        SubscribeLocalEvent<SpellbookComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<SpellbookComponent, UseInHandEvent>(OnUse);
        SubscribeLocalEvent<SpellbookComponent, SpellbookDoAfterEvent>(OnDoAfter);

        // TODO: Network spells
        // TODO: Make Magic Comp/Magic Caster Comp
        // TODO: Upgradeable spells
        // TODO: More spells
        // TODO: Save Spells to mind
        SubscribeLocalEvent<InstantSpawnSpellEvent>(OnInstantSpawn);
        SubscribeLocalEvent<TeleportSpellEvent>(OnTeleportSpell);
        SubscribeLocalEvent<WorldSpawnSpellEvent>(OnWorldSpawn);
        SubscribeLocalEvent<ProjectileSpellEvent>(OnProjectileSpell);
        SubscribeLocalEvent<ChangeComponentsSpellEvent>(OnChangeComponentsSpell);
        SubscribeLocalEvent<SmiteSpellEvent>(OnSmiteSpell);
    }

    private void OnDoAfter(EntityUid uid, SpellbookComponent component, DoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        _actionsSystem.AddActions(args.Args.User, component.Spells, component.LearnPermanently ? null : uid);
        args.Handled = true;
    }

    private void OnInit(EntityUid uid, SpellbookComponent component, ComponentInit args)
    {
        //Negative charges means the spell can be used without it running out.
        foreach (var (id, charges) in component.SpellActions)
        {
            var spell = Spawn(id);
            _actionsSystem.SetCharges(spell, charges < 0 ? null : charges);
            component.Spells.Add(spell);
        }
    }

    private void OnUse(EntityUid uid, SpellbookComponent component, UseInHandEvent args)
    {
        if (args.Handled)
            return;

        AttemptLearn(uid, component, args);

        args.Handled = true;
    }

    private void AttemptLearn(EntityUid uid, SpellbookComponent component, UseInHandEvent args)
    {
        var doAfterEventArgs = new DoAfterArgs(EntityManager, args.User, component.LearnTime, new SpellbookDoAfterEvent(), uid, target: uid)
        {
            BreakOnTargetMove = true,
            BreakOnUserMove = true,
            BreakOnDamage = true,
            NeedHand = true //What, are you going to read with your eyes only??
        };

        _doAfter.TryStartDoAfter(doAfterEventArgs);
    }

    #region Spells

    /// <summary>
    /// Handles the instant action (i.e. on the caster) attempting to spawn an entity.
    /// </summary>
    private void OnInstantSpawn(InstantSpawnSpellEvent args)
    {
        if (args.Handled)
            return;

        var transform = Transform(args.Performer);

        foreach (var position in GetSpawnPositions(transform, args.Pos))
        {
            if (_net.IsServer)
            {
                var ent = Spawn(args.Prototype, position.SnapToGrid(EntityManager, _mapManager));

                if (args.PreventCollideWithCaster)
                {
                    var comp = EnsureComp<PreventCollideComponent>(ent);
                    comp.Uid = args.Performer;
                }
            }
        }

        Speak(args);
        args.Handled = true;
    }

    // TODO: Deduplicate copied gun code
    private void OnProjectileSpell(ProjectileSpellEvent ev)
    {
        if (ev.Handled)
            return;

        ev.Handled = true;
        Speak(ev);

        var xform = Transform(ev.Performer);
        var fromCoords = xform.Coordinates;
        var toCoords = ev.Target;
        var userVelocity = _physics.GetMapLinearVelocity(ev.Performer);

        // If applicable, this ensures the projectile is parented to grid on spawn, instead of the map.
        var toMap = toCoords.ToMap(EntityManager, _transform);
        var fromMap = fromCoords.ToMap(EntityManager, _transform);
        var spawnCoords = _mapManager.TryFindGridAt(fromMap, out var gridUid, out _)
            ? fromCoords.WithEntityId(gridUid, EntityManager)
            : new(_mapManager.GetMapEntityId(fromMap.MapId), fromMap.Position);

        if (_net.IsServer)
        {
            var ent = Spawn(ev.Prototype, spawnCoords);
            var direction = toCoords.ToMapPos(EntityManager, _transform) -
                            spawnCoords.ToMapPos(EntityManager, _transform);
            _gunSystem.ShootProjectile(ent, direction, userVelocity, ev.Performer, ev.Performer);
        }
    }

    // TODO: See what this is used for and try to swap out component.Owner
    private void OnChangeComponentsSpell(ChangeComponentsSpellEvent ev)
    {
        if (ev.Handled)
            return;
        ev.Handled = true;
        Speak(ev);

        foreach (var toRemove in ev.ToRemove)
        {
            if (_compFact.TryGetRegistration(toRemove, out var registration))
                RemComp(ev.Target, registration.Type);
        }

        foreach (var (name, data) in ev.ToAdd)
        {
            if (HasComp(ev.Target, data.Component.GetType()))
                continue;

            var component = (Component) _compFact.GetComponent(name);
            component.Owner = ev.Target;
            var temp = (object) component;
            _seriMan.CopyTo(data.Component, ref temp);
            EntityManager.AddComponent(ev.Target, (Component) temp!);
        }
    }

    private List<EntityCoordinates> GetSpawnPositions(TransformComponent casterXform, MagicSpawnData data)
    {
        switch (data)
        {
            case TargetCasterPos:
                return new List<EntityCoordinates>(1) {casterXform.Coordinates};
            case TargetInFrontSingle:
            {
                var directionPos = casterXform.Coordinates.Offset(casterXform.LocalRotation.ToWorldVec().Normalized());

                if (!_mapManager.TryGetGrid(casterXform.GridUid, out var mapGrid))
                    return new List<EntityCoordinates>();
                if (!directionPos.TryGetTileRef(out var tileReference, EntityManager, _mapManager))
                    return new List<EntityCoordinates>();

                var tileIndex = tileReference.Value.GridIndices;
                return new List<EntityCoordinates>(1) { mapGrid.GridTileToLocal(tileIndex) };
            }
            case TargetInFront:
            {
                // This is shit but you get the idea.
                var directionPos = casterXform.Coordinates.Offset(casterXform.LocalRotation.ToWorldVec().Normalized());

                if (!_mapManager.TryGetGrid(casterXform.GridUid, out var mapGrid))
                    return new List<EntityCoordinates>();

                if (!directionPos.TryGetTileRef(out var tileReference, EntityManager, _mapManager))
                    return new List<EntityCoordinates>();

                var tileIndex = tileReference.Value.GridIndices;
                var coords = mapGrid.GridTileToLocal(tileIndex);
                EntityCoordinates coordsPlus;
                EntityCoordinates coordsMinus;

                var dir = casterXform.LocalRotation.GetCardinalDir();
                switch (dir)
                {
                    case Direction.North:
                    case Direction.South:
                    {
                        coordsPlus = mapGrid.GridTileToLocal(tileIndex + (1, 0));
                        coordsMinus = mapGrid.GridTileToLocal(tileIndex + (-1, 0));
                        return new List<EntityCoordinates>(3)
                        {
                            coords,
                            coordsPlus,
                            coordsMinus,
                        };
                    }
                    case Direction.East:
                    case Direction.West:
                    {
                        coordsPlus = mapGrid.GridTileToLocal(tileIndex + (0, 1));
                        coordsMinus = mapGrid.GridTileToLocal(tileIndex + (0, -1));
                        return new List<EntityCoordinates>(3)
                        {
                            coords,
                            coordsPlus,
                            coordsMinus,
                        };
                    }
                }

                return new List<EntityCoordinates>();
            }
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    // TODO: Error: Tried to play audio source outside of prediction
    // TODO: Swap transform.AttachToGridOrMap to system method
    /// <summary>
    /// Teleports the user to the clicked location
    /// </summary>
    /// <param name="args"></param>
    private void OnTeleportSpell(TeleportSpellEvent args)
    {
        if (args.Handled)
            return;

        var transform = Transform(args.Performer);

        if (transform.MapID != args.Target.GetMapId(EntityManager)) return;

        _transform.SetCoordinates(args.Performer, args.Target);
        transform.AttachToGridOrMap();
        _audio.PlayPvs(args.BlinkSound, args.Performer, AudioParams.Default.WithVolume(args.BlinkVolume));
        Speak(args);
        args.Handled = true;
    }

    /// <summary>
    /// Spawns entity prototypes from a list within range of click.
    /// </summary>
    /// <remarks>
    /// It will offset mobs after the first mob based on the OffsetVector2 property supplied.
    /// </remarks>
    /// <param name="args"> The Spawn Spell Event args.</param>
    private void OnWorldSpawn(WorldSpawnSpellEvent args)
    {
        if (args.Handled)
            return;

        var targetMapCoords = args.Target;

        SpawnSpellHelper(args.Contents, targetMapCoords, args.Lifetime, args.Offset);
        Speak(args);
        args.Handled = true;
    }

    // TODO: Try to get off of Prototypes?
    /// <summary>
    /// Loops through a supplied list of entity prototypes and spawns them
    /// </summary>
    /// <remarks>
    /// If an offset of 0, 0 is supplied then the entities will all spawn on the same tile.
    /// Any other offset will spawn entities starting from the source Map Coordinates and will increment the supplied
    /// offset
    /// </remarks>
    /// <param name="entityEntries"> The list of Entities to spawn in</param>
    /// <param name="entityCoords"> Map Coordinates where the entities will spawn</param>
    /// <param name="lifetime"> Check to see if the entities should self delete</param>
    /// <param name="offsetVector2"> A Vector2 offset that the entities will spawn in</param>
    private void SpawnSpellHelper(List<EntitySpawnEntry> entityEntries, EntityCoordinates entityCoords, float? lifetime, Vector2 offsetVector2)
    {
        var getProtos = EntitySpawnCollection.GetSpawns(entityEntries, _random);

        var offsetCoords = entityCoords;
        foreach (var proto in getProtos)
        {
            // TODO: Share this code with instant because they're both doing similar things for positioning.
            if (_net.IsServer)
            {
                var entity = Spawn(proto, offsetCoords);
                offsetCoords = offsetCoords.Offset(offsetVector2);

                if (lifetime != null)
                {
                    var comp = EnsureComp<TimedDespawnComponent>(entity);
                    comp.Lifetime = lifetime.Value;
                }
            }
        }
    }

    private void OnSmiteSpell(SmiteSpellEvent ev)
    {
        if (ev.Handled)
            return;

        ev.Handled = true;
        Speak(ev);

        var direction = Transform(ev.Target).MapPosition.Position - Transform(ev.Performer).MapPosition.Position;
        var impulseVector = direction * 10000;

        _physics.ApplyLinearImpulse(ev.Target, impulseVector);

        if (!TryComp<BodyComponent>(ev.Target, out var body))
            return;

        _bodySystem.GibBody(ev.Target, true, body);
    }

    #endregion

    protected void Speak(BaseActionEvent args)
    {
        /*if (args is not ISpeakSpell speak || string.IsNullOrWhiteSpace(speak.Speech))
            return;

        _chat.TrySendInGameICMessage(args.Performer, Loc.GetString(speak.Speech),
            InGameICChatType.Speak, false);*/
    }
}
