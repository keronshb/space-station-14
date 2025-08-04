using Content.Shared.Inventory;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Armor;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(ToggleableArmorSpacingProtectionSystem))]
public sealed partial class ToggleableArmorSpacingProtectionComponent : Component
{
    // TODO: Armor Components to toggle
    // TODO: Space Protection Components to toggle
    // TODO: Movement Modifier components to toggle

    /// <summary>
    ///     Action used to toggle between Armor and Spacing Protection
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId Action = "ActionToggleArmorSpacingProtection";

    [DataField, AutoNetworkedField]
    public EntityUid? ActionEntity;

    /// <summary>
    ///     Inv slot for action to be granted
    /// </summary>
    [DataField, AutoNetworkedField]
    public SlotFlags RequiredFlags = SlotFlags.OUTERCLOTHING;
}
