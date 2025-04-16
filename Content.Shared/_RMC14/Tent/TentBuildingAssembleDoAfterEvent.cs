using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Tent;

[Serializable, NetSerializable]
public sealed partial class TentBuildingAssembleDoAfterEvent : SimpleDoAfterEvent;
