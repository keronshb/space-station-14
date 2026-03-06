using Content.Shared.Inventory;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Actions.Components;

// TODO: Change this to Action Clothes Requirements
/// <summary>
/// If this is listed on an action, the action will need to meet the listed parameters before it can be used.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(ActionRequirementsSystem))]
[EntityCategory("Actions")]
public sealed partial class ActionRequirementsComponent :  Component
{
    // TODO: Get the below working first then try expanding after if needed.

    // TODO: Display the requirements in the action display
    //      Like how Charges/Cooldown shows, this can update the UI info to add a line that says something like
    //      "Requires Wizard Clothes in Head and Suit slots" or something

    // TODO: Maybe some sort of red X if it can't be used?

    // TODO: Should this just see if it passes requirements or do a behavior?
    // Like if it doesn't pass the requirements, do we just cancel OR have it fire off some other thing that would modify cooldowns, etc?

    /// <summary>
    /// Which inventory slots do the <see cref="Requirements"/> apply to?
    /// Only really applicable if the component/tags listed in <see cref="Requirements"/> is an item
    /// </summary>
    [DataField]
    public SlotFlags? RequiredSlots;
    // SlotFlags.MASK | SlotFlags.HEAD;
    // - HEAD in yml

    /// <summary>
    /// What's required to use this action? ex) Is the entity holding/wearing/standing next to something that meets these requirements?
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityWhitelist? Requirements;

    // TODO: How to signify this works when tile adjacent?
    // maybe a distance field that's a nullable int of some sort?
    // float distance and use xform.Coords.TryDistance?

    // TODO: Why not add some sort of event handler like the magic/action system?
    // Generic check CompEvent, where it checks to see if the action or the performer has the comp & specified value
    //  before casting?

    // TODO: Maybe add a simple event check?
    //  Event would be for custom logic
    //  So something like CheckCustomActionRequirements? It would supercede the other checks.
    //  Would run this and the others off of the ActionAttemptEvent?
    //  ECS => ActionAttemptEvent Subscribe => Run Checks (custom and non custom?) if either are cancelled, then cancel action attempt.
}
