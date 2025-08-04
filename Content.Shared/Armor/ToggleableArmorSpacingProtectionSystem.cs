namespace Content.Shared.Armor;

public sealed class ToggleableArmorSpacingProtectionSystem : EntitySystem
{
    // TODO: Depdendencies
    // TODO: Action Container

    public override void Initialize()
    {
        base.Initialize();
        // TODO: Subscribes
        SubscribeLocalEvent<ToggleableArmorSpacingProtectionComponent, ToggleArmorSpacingProtectionEvent>(OnToggleArmorSpacingProtection);
        // TODO: On Item Equip to Grant Action? Use GetItemActionsEvent
        // TODO: On map init add action to container?
        // TODO: On Item Unequip to Remove Action. Use GotUnequippedEvent
    }

    // TODO: Grant toggle action
    // TODO: Have toggle action effect attached clothing piece

    private void OnToggleArmorSpacingProtection(Entity<ToggleableArmorSpacingProtectionComponent> ent, ref ToggleArmorSpacingProtectionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        // TODO: Method to toggle armor/spacing prot
    }
}
