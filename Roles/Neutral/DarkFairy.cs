using AmongUs.GameOptions;
using TOHE.Roles.Core;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Neutral;

internal class DarkFairy : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 29100;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.DarkFairy);
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralEvil;
    //==================================================================\\

    private static OptionItem RememberCooldown;
    private static OptionItem TaskCharmCooldown;
    private static Optionitem CanVent

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.DarkFairy);
        RememberCooldown = FloatOptionItem.Create(Id + 10, "RememberCooldown", new(0f, 180f, 2.5f), 25f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.DarkFairy])
                .SetValueFormat(OptionFormat.Seconds);
        TaskCharmCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.TaskCharmCooldown, new(0f, 180f, 2.5f), 20f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.DarkFairy])
            .SetValueFormat(OptionFormat.Seconds);
        CanVent = BooleanOptionItem.Create(Id + 11, GeneralOption.CanVent, true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.DarkFairy]);
    }
    public override void Add(byte playerId)
    {
        AbilityLimit = 1;

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = AbilityLimit >= 1 ? RememberCooldown.GetFloat() : 300f;
    public override bool CanUseKillButton(PlayerControl player) => !player.Data.IsDead && (AbilityLimit > 0);
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (AbilityLimit < 1) return false;

        var role = target.GetCustomRole();

        if (role is CustomRoles.Jackal
            or CustomRoles.HexMaster
            or CustomRoles.Poisoner
            or CustomRoles.Juggernaut 
            or CustomRoles.BloodKnight
            or CustomRoles.Sheriff)
        {
            AbilityLimit--;
            SendSkillRPC();
            killer.RpcSetCustomRole(role);
            killer.GetRoleClass().OnAdd(killer.PlayerId);

            if (role.IsCrewmate())
                killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.DarkFairy), GetString("RememberedCrewmate")));
            else
                killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.DarkFairy), GetString("RememberedNeutralKiller")));

            // Notify target
            target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.DarkFairy), GetString("ImitatorImitated")));
        }
        else if (role.IsCrewmate())
        {
            AbilityLimit--;
            SendSkillRPC();
            killer.RpcSetCustomRole(CustomRoles.Sheriff);
            killer.GetRoleClass().OnAdd(killer.PlayerId);
            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.DarkFairy), GetString("RememberedCrewmate")));
            target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.DarkFairy), GetString("ImitatorImitated")));
        }
        else if (role.IsImpostor())
        {
            AbilityLimit--;
            SendSkillRPC();
            killer.RpcSetCustomRole(CustomRoles.Refugee);
            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.DarkFairy), GetString("RememberedImpostor")));
            target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.DarkFairy), GetString("ImitatorImitated")));
        }

        var killerRole = killer.GetCustomRole();

        if (killerRole != CustomRoles.DarkFairy)
        {
            killer.ResetKillCooldown();
            killer.SetKillCooldown(forceAnime: true);

            Logger.Info("Imitator remembered: " + target?.Data?.PlayerName + " = " + target.GetCustomRole().ToString(), "Imitator Assign");
            Logger.Info($"{killer.GetNameWithRole()} : {AbilityLimit} remember limits left", "Imitator");

            Utils.NotifyRoles(SpecifySeer: killer);
        }
        else if (killerRole == CustomRoles.DarkFairy)
        {
            killer.SetKillCooldown(forceAnime: true);
            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.DarkFairy), GetString("ImitatorInvalidTarget")));
        }

        return false;
    }
    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.KillButton.OverrideText(GetString("ImitatorKillButtonText"));
    }

}
