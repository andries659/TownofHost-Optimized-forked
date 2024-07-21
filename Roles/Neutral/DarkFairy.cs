using TOHE.Roles.AddOns.Crewmate;
using AmongUs.GameOptions;
using TOHE.Roles.Core;
using TOHE.Roles.Double;
using UnityEngine;
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

    private enum ConvertedCountModeSelectList
    {
        DarkFairy_ConvertedCountMode_None,
        DarkFairy_ConvertedCountMode_DarkFairy,
        DarkFairy_ConvertedCountMode_Original
    }

    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.DarkFairy, 1, zeroOne: false);
        ConvertCooldown = FloatOptionItem.Create(Id + 10, "DarkFairyCharmCooldown", new(0f, 180f, 2.5f), 30f, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.DarkFairy])
             .SetValueFormat(OptionFormat.Seconds);
        ConvertCooldownIncrese = FloatOptionItem.Create(Id + 11, "DarkFairyCharmCooldownIncrese", new(0f, 180f, 2.5f), 10f, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.DarkFairy])
            .SetValueFormat(OptionFormat.Seconds);
        ConvertMax = IntegerOptionItem.Create(Id + 12, "DarkFairyCharmMax", new(1, 15, 1), 15, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.DarkFairy])
            .SetValueFormat(OptionFormat.Times);
        KnowTargetRole = BooleanOptionItem.Create(Id + 13, "DarkFairyKnowTargetRole", true, TabGroup.NeutralRoles, false)
             .SetParent(CustomRoleSpawnChances[CustomRoles.DarkFairy]);
         FairiesKnowEachOther = BooleanOptionItem.Create(Id + 14, "DarkFairyFairiesKnowEachOther", true, TabGroup.NeutralRoles, false)
             .SetParent(CustomRoleSpawnChances[CustomRoles.DarkFairy]);
         ConvertedCountMode = StringOptionItem.Create(Id + 17, "DarkFairy_CharmedCountMode", EnumHelper.GetAllNames<ConvertedCountModeSelectList>(), 1, TabGroup.NeutralRoles, false)
             .SetParent(CustomRoleSpawnChances[CustomRoles.DarkFairy]);
         CanConvertNeutral = BooleanOptionItem.Create(Id + 18, "DarkFairyCanCharmNeutral", false, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.DarkFairy]);
    }

    public override void Add(byte playerId)
    {
        AbilityLimit = ConvertMax.GetInt();

         if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = AbilityLimit >= 1 ? ConvertCooldown.GetFloat() + (ConvertMax.GetInt() - AbilityLimit) * ConvertCooldownIncrese.GetFloat() : 300f;
    // I set ConvertCooldown back to KillCooldown, because both function the same way at this state -Ape

    public override bool OnCheckMurderAsKiller(PlayerControl killer,PlayerControl target)
    {
        {
            AbilityLimit--;
            SendSkillRPC();
            target.RpcSetCustomRole(CustomRoles.Converted);

            target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.DarkFairy), GetString("ConvertedByDarkFairy")));
                
            target.RpcGuardAndKill(target);

            Logger.Info("设置职业:" + target?.Data?.PlayerName + " = " + target.GetCustomRole().ToString() + " + " + CustomRoles.Converted.ToString(), "Assign " + CustomRoles.Converted.ToString());
            return true; // Missing return statement
        }
        return false; // Missing return statement 
    }

    public static bool FairiesKnowEachOtherFunc => FairiesKnowEachOther.GetBool(); // Function name conflict resolution

    public static bool KnowRole(PlayerControl player, PlayerControl target)
    {
        if (player.Is(CustomRoles.Converted) && target.Is(CustomRoles.DarkFairy)) return true;

        if (KnowTargetRole.GetBool())
        {
             if (player.Is(CustomRoles.DarkFairy) && target.Is(CustomRoles.Converted)) return true;
            if (FairiesKnowEachOtherFunc && player.Is(CustomRoles.Converted) && target.Is(CustomRoles.Converted)) return true;
        }
        return false;
    }

    public override string GetProgressText(byte playerId, bool cooms) => Utils.ColorString(AbilityLimit >= 1 ? Utils.GetRoleColor(CustomRoles.DarkFairy).ShadeColor(0.25f) : Color.gray, $"({AbilityLimit})");

    public static bool CanBeConverted(PlayerControl pc)
    {
         return pc != null && (pc.GetCustomRole().IsCrewmate() || pc.GetCustomRole().IsImpostor() || 
             (CanConvertNeutral.GetBool() && pc.GetCustomRole().IsNeutral())) && !pc.Is(CustomRoles.Converted) 
             && !pc.Is(CustomRoles.Admired) && !pc.Is(CustomRoles.Loyal) && !pc.Is(CustomRoles.Infectious) 
             && !pc.Is(CustomRoles.Virus) && !pc.Is(CustomRoles.DarkFairy)
             && !(pc.GetCustomSubRoles().Contains(CustomRoles.Hurried) && !Hurried.CanBeConverted.GetBool());
    }
    public static bool NameRoleColor(PlayerControl seer, PlayerControl target)
    {
        if (seer.Is(CustomRoles.Converted) && target.Is(CustomRoles.DarkFairy)) return true;
        if (seer.Is(CustomRoles.DarkFairy) && target.Is(CustomRoles.Converted)) return true;
        if (seer.Is(CustomRoles.Converted) && target.Is(CustomRoles.Converted) && FairiesKnowEachOtherFunc) return true;
            
        return false;
        
    }
}

