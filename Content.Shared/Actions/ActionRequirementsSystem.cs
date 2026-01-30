using Content.Shared.Actions.Components;
using Content.Shared.Actions.Events;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.Whitelist;

namespace Content.Shared.Actions;

public sealed class ActionRequirementsSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = null!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = null!;
    [Dependency] private readonly SharedPopupSystem _popup = null!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ActionRequirementsComponent, ActionAttemptEvent>(OnActionAttempt);
    }

    private void OnActionAttempt(Entity<ActionRequirementsComponent> ent, ref ActionAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        // TODO: Maybe an enum or something instead of bools?
        // ex) Requirements.RequiresSlots
        // ex) Requirements.RequiresNotInSlot (ex. Hardsuit)
        // ex) Requirements.Speech

        var hasReqs = true;

        if (ent.Comp.RequiredSlots != null)
        {
            var enumerator = _inventory.GetSlotEnumerator(args.User, ent.Comp.RequiredSlots.Value);
            while (enumerator.MoveNext(out var containerSlot))
            {
                // TODO: May need multiple reqs? Or some way to consolidate this list
                //      What if you wanted two different reqs?
                //      This might work as is cause it's on the whitelist
                //      eg. Item has WizardClothes and is in Slots

                if (containerSlot.ContainedEntity is { } item)
                    hasReqs = _whitelist.IsWhitelistPass(ent.Comp.Requirements, item);
                // Not wearing anything in the slot so reqs aren't met
                else
                    hasReqs = false;

                if (!hasReqs)
                    break;

            }
        }

        // TODO: see if this works with muted comp
        // TODO: will probably also need to check if hasreqs was already set to false
        hasReqs = _whitelist.IsWhitelistPass(ent.Comp.Requirements, args.User);

        // TODO: Requirement next to (blood puddle, etc)

        if (hasReqs)
            return;

        // TODO: Rename loc to action requirements failed
        // TODO: Try to explain what's missing (slots, etc)
        args.Cancelled = true;
        _popup.PopupClient(Loc.GetString("spell-requirements-failed"), args.User, args.User);
    }
}
