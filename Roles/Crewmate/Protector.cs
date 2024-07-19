using Hazel;
using System;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate;

internal class Protector
{
    //===========================SETUP================================\\
    private const int Id = 29000;
    private static readonly HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Any();
    
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateSupport;
    //==================================================================\\

    private static OptionItem ShieldDuration;
    private static OptionItem ShieldIsOneTimeUse;

    private static readonly Dictionary<byte, HashSet<int>> taskIndex = [];
    private static readonly Dictionary<byte, long> shieldedPlayers = [];

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Protector);
        ShieldDuration = FloatOptionItem.Create(Id + 10, "ShieldDuration", new(1, 30, 1), 10, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Protector])
            .SetValueFormat(OptionFormat.Votes);
        ShieldIsOneTimeUse = BooleanOptionItem.Create(Id + 11, "ShieldIsOneTimeUse", false, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Protector]);
        Options.OverrideTasksData.Create(Id + 12, TabGroup.CrewmateRoles, CustomRoles.Protector);
    }
    // INCOMPLETE, MORE TO ADD
}
