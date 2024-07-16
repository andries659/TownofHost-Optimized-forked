using static TOHE.Options;

namespace TOHE.Roles.AddOns.Common;

public static class Evader
{
    private const int Id = 21204;
    public static OptionItem CanBeOnCrew;
    public static OptionItem CanBeOnImp;
    public static OptionItem CanBeOnNeutral;
    public static OptionItem ChanceToEvadeVote;

    public enum StateVote
    {
        TryEvadeVote
    }
        
    
    public static void SetupCustomOption()
    {
        Options.SetupAdtRoleOptions(Id, CustomRoles.Influenced, canSetNum: true);
        CanBeOnImp = BooleanOptionItem.Create(Id + 10, "ImpCanBeEvader", true, TabGroup.Addons, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Evader]);
        CanBeOnCrew = BooleanOptionItem.Create(Id + 11, "CrewCanBeEvader", true, TabGroup.Addons, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Evader]);
        CanBeOnNeutral = BooleanOptionItem.Create(Id + 12, "NeutralCanBeEvader", true, TabGroup.Addons, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Evader]);
        ChanceToEvadeVote = IntegerOptionItem.Create(Id + 13, "ChanceToEvadeVote", new(0, 100, 1), 50, TabGroup.Addons, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Evader])
            .SetValueFormat(OptionFormat.Percent);
    }

    }
    public static bool EvadeRand(PlayerControl victim, StateVote state)
    {
        var shouldBeEvade = IRandom.Instance.Next(1, 100) <= state switch
        {
            StateVote.TryEvadeVote => ChanceToEvadeVote.GetInt(),

            _ => -1
        };

        if (shouldBeEvade)
        {
            public static void CheckRealVotes(PlayerControl target, ref int VoteNum)
            {
                if (target.Is(CustomRoles.Evader))
                {
                    VoteNum = 0;
                }
                return true;
            }
        return false;
    }
}
