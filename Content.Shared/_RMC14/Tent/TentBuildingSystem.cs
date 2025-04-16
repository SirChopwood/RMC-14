using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Content.Shared._RMC14.Map;
using Content.Shared._RMC14.Sprite;
using Content.Shared._RMC14.Weapons.Ranged.IFF;
using Content.Shared.CombatMode;
using Content.Shared.Coordinates;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Network;


namespace Content.Shared._RMC14.Tent;

public sealed class TentBuildingSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedCombatModeSystem _combatMode = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly RMCMapSystem _rmcMap = default!;
    [Dependency] private readonly SharedRMCSpriteSystem _rmcSprite = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    private EntityUid? _roofEntity = null;

    public override void Initialize()
    {
        SubscribeLocalEvent<TentBuildingComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<TentBuildingComponent, TentBuildingAssembleDoAfterEvent>(OnAssembleDoAfter);
        SubscribeLocalEvent<TentBuildingComponent, TentBuildingDisassembleDoAfterEvent>(OnDisassembleDoAfter);
        SubscribeLocalEvent<TentBuildingComponent, GetVerbsEvent<AlternativeVerb>>(OnGetAlternativeVerbs);
    }

    private void OnUseInHand(Entity<TentBuildingComponent> ent, ref UseInHandEvent args)
    {
        if (!CanPlantFlagPopup(ent, args.User, out _))
            return;

        args.Handled = true;
        var ev = new TentBuildingAssembleDoAfterEvent();
        var doAfter = new DoAfterArgs(EntityManager, args.User, ent.Comp.AssembleTime, ev, ent, ent, ent)
        {
            BreakOnMove = true,
            NeedHand = true,
        };

        if (_doAfter.TryStartDoAfter(doAfter))
            _audio.PlayPredicted(ent.Comp.AssembleStartSound, ent, args.User);
    }

    private void OnAssembleDoAfter(Entity<TentBuildingComponent> ent, ref TentBuildingAssembleDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        args.Handled = true;
        if (!CanPlantFlagPopup(ent, args.User, out var target))
            return;

        _transform.SetCoordinates(ent, target.Value);
        _transform.SetLocalRotation(ent, Angle.Zero);
        _transform.AnchorEntity(ent);

        //_appearance.SetData(ent, TentBuildingVisuals.Planted, true);

        if (ent.Comp.DeployOffset != Vector2.Zero)
            _rmcSprite.SetOffset(ent, ent.Comp.DeployOffset);

        if (_net.IsClient)
            return;

        _roofEntity = SpawnAttachedTo(ent.Comp.RoofPrototype, target.Value);
        _audio.PlayPvs(ent.Comp.AssembleEndSound, ent);
    }

    private void OnDisassembleDoAfter(Entity<TentBuildingComponent> ent, ref TentBuildingDisassembleDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        args.Handled = true;
        _transform.Unanchor(ent);
        _hands.TryPickupAnyHand(args.User, ent);
        _appearance.SetData(ent, TentBuildingVisuals.Planted, false);
        _rmcSprite.SetOffset(ent, Vector2.Zero);
        QueueDel(_roofEntity);
        if (!_net.IsClient)
        {
            _audio.PlayPvs(ent.Comp.DisassembleEndSound, ent);
        }
    }

    private void OnGetAlternativeVerbs(Entity<TentBuildingComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!TryComp(ent, out TransformComponent? transform) ||
            !transform.Anchored)
        {
            return;
        }

        var user = args.User;
        args.Verbs.Add(new AlternativeVerb
        {
            Text = "Pack Up Tent",
            Act = () =>
            {
                var ev = new TentBuildingDisassembleDoAfterEvent();
                var doAfter = new DoAfterArgs(EntityManager, user, ent.Comp.DisassembleTime, ev, ent, ent, ent)
                {
                    BreakOnMove = true,
                    NeedHand = true,
                };

                if (_doAfter.TryStartDoAfter(doAfter))
                    _audio.PlayPredicted(ent.Comp.DisassembleStartSound, ent, user);
            },
        });
    }

    private bool CanPlantFlagPopup(Entity<TentBuildingComponent> ent, EntityUid user, [NotNullWhen(true)] out EntityCoordinates? target)
    {
        target = null;
        if (!TryComp(user, out TransformComponent? userTransform))
            return false;

        var (coords, rot) = _transform.GetMoverCoordinateRotation(user, userTransform);
        target = coords.Offset(rot.ToWorldVec());
        if (_rmcMap.IsTileBlocked(target.Value))
        {
            _popup.PopupClient(
                $"You need a clear, open area to plant the {Name(ent)}, something is blocking the way!",
                user,
                user,
                PopupType.MediumCaution
            );

            return false;
        }

        return true;
    }
}
