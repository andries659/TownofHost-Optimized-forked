using System;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate;

internal class Protector : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 29500;
    private static readonly HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Any();
    
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmatePower;
    //==================================================================\\

    private static OptionItem MaxShields;
    private static OptionItem ShieldDuration;
    private static OptionItem ShieldIsOneTimeUse;

    // Might use this check later, ignore warning
    private static bool IsShielded = false;

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Protector);
        MaxShields = IntegerOptionItem.Create(Id + 10, "ProtectorMaxShields", new(1, 14, 1), 3, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Protector])
            .SetValueFormat(OptionFormat.Votes);
        ShieldDuration = FloatOptionItem.Create(Id + 11, "ShieldDuration", new(1, 30, 1), 10, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Protector])
            .SetValueFormat(OptionFormat.Seconds);
        ShieldIsOneTimeUse = BooleanOptionItem.Create(Id + 12, "ShieldIsOneTimeUse", false, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Protector]);
        Options.OverrideTasksData.Create(Id + 13, TabGroup.CrewmateRoles, CustomRoles.Protector);
    }
    public override void Init()
    {
        playerIdList.Clear();
    }

    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
    }

    // player = Protector
    // I dont fucking know why its a bool, I dont fucking know why it needs int TaskCounts, it just needs to be that way.
    public override bool OnTaskComplete(PlayerControl player, int completedTaskCount, int totalTaskCount)
    {
        if (player.IsAlive())
        {
        player.Notify(GetString("ProtectorShieldActive"), ShieldDuration.GetFloat());
        }
        return true;
    // All thats needed is to just add a shield to player when OnTaskComplete
    }
}
