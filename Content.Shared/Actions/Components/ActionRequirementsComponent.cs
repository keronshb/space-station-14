using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Actions.Components;

/// <summary>
/// If this is listed on an action, the action will need to meet the listed parameters before it can be used.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(ActionRequirementsSystem))]
[EntityCategory("Actions")]
public sealed partial class ActionRequirementsComponent :  Component
{
    // TODO: Display the requirements in the action display
    //      Like how Charges/Cooldown shows, this can update the UI info to add a line that says something like
    //      "Requires Wizard Clothes in Head and Suit slots" or something

    // TODO: List of slot requirements (hand, head, etc)

    // TODO: Tag requirements (whitelist?)
}
