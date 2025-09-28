using Content.Shared.Inventory;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Actions.Components;

/// <summary>
/// If this is listed on an action, the action will need to meet the listed parameters before it can be used.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(ActionRequirementsSystem))]
[EntityCategory("Actions")]
public sealed partial class ActionRequirementsComponent :  Component
{
    // TODO: Display the requirements in the action display
    //      Like how Charges/Cooldown shows, this can update the UI info to add a line that says something like
    //      "Requires Wizard Clothes in Head and Suit slots" or something

    /// <summary>
    /// Which inventory slots do the <see cref="Requirements"/> apply to?
    /// Only really applicable if the component/tags listed in <see cref="Requirements"/> is an item
    /// </summary>
    [DataField]
    public SlotFlags? RequiredSlots;

    /// <summary>
    /// What's required to use this action?
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityWhitelist? Requirements;

    // TODO: How to signify this works when tile adjacent?

    // TODO: Why not add some sort of event handler like the magic/action system?
    // Generic check CompEvent, where it checks to see if the action or the performer has the comp & specified value
    //  before casting?
}
