using Content.Shared.Actions;
using Robust.Shared.Audio;

namespace Content.Shared.Magic.Events;

public sealed partial class KnockSpellEvent : InstantActionEvent, ISpeakSpell
{
    // TODO: Move to magic component
    /// <summary>
    /// The range this spell opens doors in
    /// 4f is the default
    /// </summary>
    [DataField("range")]
    public float Range = 4f;

    // TODO: Move to magic component
    [DataField("knockSound")]
    public SoundSpecifier KnockSound = new SoundPathSpecifier("/Audio/Magic/knock.ogg");

    // TODO: Move to magic component
    /// <summary>
    /// Volume control for the spell.
    /// </summary>
    [DataField("knockVolume")]
    public float KnockVolume = 5f;

    // TODO: Move to magic component
    [DataField("speech")]
    public string? Speech { get; private set; }
}
