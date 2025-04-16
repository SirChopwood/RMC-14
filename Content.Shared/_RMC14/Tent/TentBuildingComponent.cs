using System.Numerics;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Tent;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(TentBuildingSystem))]
public sealed partial class TentBuildingComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan AssembleTime = TimeSpan.FromSeconds(10);

    [DataField, AutoNetworkedField]
    public TimeSpan DisassembleTime = TimeSpan.FromSeconds(15);

    [DataField, AutoNetworkedField]
    public SoundSpecifier? AssembleStartSound = new SoundPathSpecifier("/Audio/_RMC14/Effects/flag_raising.ogg");

    [DataField, AutoNetworkedField]
    public SoundSpecifier? AssembleEndSound = new SoundPathSpecifier("/Audio/_RMC14/Effects/flag_raised.ogg");

    [DataField, AutoNetworkedField]
    public SoundSpecifier? DisassembleStartSound = new SoundPathSpecifier("/Audio/_RMC14/Effects/flag_lowering.ogg");

    [DataField, AutoNetworkedField]
    public SoundSpecifier? DisassembleEndSound = new SoundPathSpecifier("/Audio/_RMC14/Effects/flag_lowering.ogg");

    [DataField, AutoNetworkedField]
    public Vector2 DeployOffset = new(1, 0);

    [DataField("roofPrototype", false, 1, true), AutoNetworkedField]
    public EntProtoId RoofPrototype = "";
}
