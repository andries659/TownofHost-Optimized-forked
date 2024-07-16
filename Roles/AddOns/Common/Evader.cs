using static TOHE.Options;

namespace TOHE.Roles.AddOns.Common;

public static class Evader
{
    private const int Id = 21204;

    public static OptionItem ImpCanBeEvader;
    public static OptionItem CrewCanBeEvader;
    public static OptionItem NeutralCanBeEvader;
    public static OptionItem ChanceToEvadeVote;

    private static Dictionary<byte, bool> Evade;
        
    
    public static void SetupCustomOption()
    {
        Options.SetupAdtRoleOptions(Id, CustomRoles.Evader, canSetNum: true);
        ChanceToEvadeVote = IntegerOptionItem.Create(Id + 13, "ChanceToEvadeVote", new(0, 100, 1), 50, TabGroup.Addons, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Evader])
            .SetValueFormat(OptionFormat.Percent);
        CanBeOnImp = BooleanOptionItem.Create(Id + 10, "ImpCanBeEvader", true, TabGroup.Addons, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Evader]);
        CanBeOnCrew = BooleanOptionItem.Create(Id + 11, "CrewCanBeEvader", true, TabGroup.Addons, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Evader]);
        CanBeOnNeutral = BooleanOptionItem.Create(Id + 12, "NeutralCanBeEvader", true, TabGroup.Addons, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Evader]);
    }
    public static void Init()
    {
        Evade = [];
    }
        private static void EvadeChance()
        {
            var rd = IRandom.Instance;
            if (rd.Next(0, 101) < ChanceToEvadeVote.GetInt());
        }

    public static void CheckRealVotes(PlayerControl target, ref int VoteNum)

    {
        EvadeChance();
        {
            if (target.Is(CustomRoles.Evader))
            {
                VoteNum = 0;
            }
        }
    }
}
