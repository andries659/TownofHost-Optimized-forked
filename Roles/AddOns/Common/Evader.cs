using static TOHE.Options;

namespace TOHE.Roles.AddOns.Common
{
    public static class Evader
    {
        private const int Id = 21204;
        public static OptionItem CanBeOnCrew;
        public static OptionItem CanBeOnImp;
        public static OptionItem CanBeOnNeutral;
        public static OptionItem ChanceToEvadeVote;

        public static void SetupCustomOption()
        {
            Options.SetupAdtRoleOptions(Id, CustomRoles.Influenced, canSetNum: true);
            CanBeOnImp = BooleanOptionItem.Create(Id + 10, "ImpCanBeEvader", true, TabGroup.Addons, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Evader]);
            CanBeOnCrew = BooleanOptionItem.Create(Id + 11, "CrewCanBeEvader", true, TabGroup.Addons, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Evader]);
            CanBeOnNeutral = BooleanOptionItem.Create(Id + 12, "NeutralCanBeEvader", true, TabGroup.Addons, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Evader]);
            ChanceToEvadeVote = IntegerOptionItem.Create(Id + 13, "ChanceToEvadeVote", new(0, 100, 1), 50, TabGroup.Addons, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Evader])
                .SetValueFormat(OptionFormat.Percent);
        }

        public static bool TryEvadeVote()
        {
            int evadeChance = ChanceToEvadeVote.GetValue();
            return IRandom.Instance.Next(1, 101) <= evadeChance;
        }

        public static void HandleMeetingEjection(Player votedOutPlayer)
        {
            if (votedOutPlayer.Role == CustomRoles.Evader)
            {
                bool evaded = TryEvadeVote();
                return;
            }

            EjectPlayer(votedOutPlayer);
        }
    }
}
