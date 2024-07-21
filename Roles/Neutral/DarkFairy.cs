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
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralEvil;
    //==================================================================\\

    private static OptionItem ConvertCooldown;
    private static OptionItem ConvertCooldownIncrese;
    private static OptionItem ConvertMax;
    private static OptionItem KnowTargetRole;
    private static OptionItem FairiesKnowEachOther;
    private static OptionItem CanConvertNeutral;
    public static OptionItem ConvertedCountMode;

    private enum CharmedCountModeSelectList
    {
        DarkFairy_ConvertedCountMode_None,
        DarkFairy_ConvertedCountMode_DarkFairy,
        DarkFairy_ConvertedCountMode_Original
    }

    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.DarkFairy, 1, zeroOne: false);
        ConvertCooldown = FloatOptionItem.Create(Id + 10, "DarkFairyCharmCooldown", new(0f, 180f, 2.5f), 30f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.DarkFairy])
            .SetValueFormat(OptionFormat.Seconds);
        ConvertCooldownIncrese = FloatOptionItem.Create(Id + 11, "DarkFairyCharmCooldownIncrese", new(0f, 180f, 2.5f), 10f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.DarkFairy])
            .SetValueFormat(OptionFormat.Seconds);
        ConvertMax = IntegerOptionItem.Create(Id + 12, "DarkFairyCharmMax", new(1, 15, 1), 15, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.DarkFairy])
            .SetValueFormat(OptionFormat.Times);
        KnowTargetRole = BooleanOptionItem.Create(Id + 13, "DarkFairyKnowTargetRole", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.DarkFairy]);
        FairiesKnowEachOther = BooleanOptionItem.Create(Id + 14, "DarkFairyFairiesKnowEachOther", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.DarkFairy]);
        ConvertedCountMode = StringOptionItem.Create(Id + 17, "DarkFairy_CharmedCountMode", EnumHelper.GetAllNames<CharmedCountModeSelectList>(), 1, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.DarkFairy]);
        CanConvertNeutral = BooleanOptionItem.Create(Id + 18, "DarkFairyCanCharmNeutral", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.DarkFairy]);
    }
    public override void Add(byte playerId)
    {
        AbilityLimit = ConvertMax.GetInt();

        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }
    public override void SetConvertCooldown(byte id) => Main.AllPlayerKillCooldown[id] = AbilityLimit >= 1 ? ConvertCooldown.GetFloat() + (ConvertMax.GetInt() - AbilityLimit) * ConvertCooldownIncrese.GetFloat() : 300f;
    public override bool OnCheckMurderAsKiller(PlayerControl target)
    {
        if (AbilityLimit < 1) return false;
        if (Mini.Age < 18 && (target.Is(CustomRoles.NiceMini) || target.Is(CustomRoles.EvilMini)))
        else if (CanBeConverted(target) && Mini.Age == 18 || CanBeConverted(target) && Mini.Age < 18 && !(target.Is(CustomRoles.NiceMini) || target.Is(CustomRoles.EvilMini)))
        {
            AbilityLimit--;
            SendSkillRPC();
            target.RpcSetCustomRole(CustomRoles.Converterd);

            target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.DarkFairy), GetString("ConvertedByDarkFairy")));
            
            Utils.NotifyRoles(SpecifySeer: killer, SpecifyTarget: target, ForceLoop: true);
            Utils.NotifyRoles(SpecifySeer: target, SpecifyTarget: killer, ForceLoop: true);

            killer.ResetKillCooldown();
            killer.SetKillCooldown();
            if (!DisableShieldAnimations.GetBool()) killer.RpcGuardAndKill(target);
            target.RpcGuardAndKill(killer);
            target.RpcGuardAndKill(target);

            Logger.Info("设置职业:" + target?.Data?.PlayerName + " = " + target.GetCustomRole().ToString() + " + " + CustomRoles.Charmed.ToString(), "Assign " + CustomRoles.Charmed.ToString());
            Logger.Info($"{killer.GetNameWithRole()} : 剩余{AbilityLimit}次魅惑机会", "Cultist");
            return false;
        }
        killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Cultist), GetString("CultistInvalidTarget")));
        Logger.Info($"{killer.GetNameWithRole()} : 剩余{AbilityLimit}次魅惑机会", "Cultist");
        return false;
    }
    public static bool TargetKnowOtherTargets => TargetKnowOtherTarget.GetBool();
    public static bool KnowRole(PlayerControl player, PlayerControl target)
    {
        if (player.Is(CustomRoles.Charmed) && target.Is(CustomRoles.Cultist)) return true;

        if (KnowTargetRole.GetBool())
        {
            if (player.Is(CustomRoles.Cultist) && target.Is(CustomRoles.Charmed)) return true;
            if (TargetKnowOtherTarget.GetBool() && player.Is(CustomRoles.Charmed) && target.Is(CustomRoles.Charmed)) return true;
        }
        return false;
    }
    public override string GetProgressText(byte playerid, bool cooms) => Utils.ColorString(AbilityLimit >= 1 ? Utils.GetRoleColor(CustomRoles.Cultist).ShadeColor(0.25f) : Color.gray, $"({AbilityLimit})");
    public static bool CanBeCharmed(PlayerControl pc)
    {
        return pc != null && (pc.GetCustomRole().IsCrewmate() || pc.GetCustomRole().IsImpostor() || 
            (CanCharmNeutral.GetBool() && pc.GetCustomRole().IsNeutral())) && !pc.Is(CustomRoles.Charmed) 
            && !pc.Is(CustomRoles.Admired) && !pc.Is(CustomRoles.Loyal) && !pc.Is(CustomRoles.Infectious) 
            && !pc.Is(CustomRoles.Virus) && !pc.Is(CustomRoles.Cultist)
            && !(pc.GetCustomSubRoles().Contains(CustomRoles.Hurried) && !Hurried.CanBeConverted.GetBool());
    }
    public static bool NameRoleColor(PlayerControl seer, PlayerControl target)
    {
        if (seer.Is(CustomRoles.Charmed) && target.Is(CustomRoles.Cultist)) return true;
        if (seer.Is(CustomRoles.Cultist) && target.Is(CustomRoles.Charmed)) return true;
        if (seer.Is(CustomRoles.Charmed) && target.Is(CustomRoles.Charmed) && TargetKnowOtherTarget.GetBool()) return true;
        
        return false;
    }
    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.KillButton.OverrideText(GetString("CultistKillButtonText"));
    }
    public override Sprite GetKillButtonSprite(PlayerControl player, bool shapeshifting) => CustomButton.Get("Subbus");
}
