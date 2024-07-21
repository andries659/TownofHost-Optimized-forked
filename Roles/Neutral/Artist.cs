using AmongUs.GameOptions;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles.Neutral
{
    internal class Artist : RoleBase
    {
        private static readonly NetworkedPlayerInfo.PlayerOutfit PaintedOutfit = new NetworkedPlayerInfo.PlayerOutfit()
            .Set("", 15, "", "", "visor_Crack", "", "");

        private const int Id = 28800;
        private static readonly HashSet<byte> PlayerIds = new HashSet<byte>();
        private static OptionItem KillCooldown;
        private static OptionItem PaintCooldown;
        private static OptionItem CanVent;
        private static OptionItem HasImpostorVision;

        private static readonly Dictionary<byte, float> NowCooldown = new Dictionary<byte, float>();
        private static readonly Dictionary<byte, List<byte>> PlayerSkinsPainted = new Dictionary<byte, List<byte>>();
        private readonly Dictionary<byte, int> KillClickCount = new Dictionary<byte, int>();

        public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
        public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralKilling;

        public static bool HasEnabled => PlayerIds.Any();

        public override void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Artist);
            KillCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 30f, TabGroup.NeutralRoles, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.Artist])
                .SetValueFormat(OptionFormat.Seconds);
            PaintCooldown = FloatOptionItem.Create(Id + 11, "ArtistPaintCooldown", new(0f, 180f, 2.5f), 20f, TabGroup.NeutralRoles, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.Artist])
                .SetValueFormat(OptionFormat.Seconds);
            CanVent = BooleanOptionItem.Create(Id + 13, GeneralOption.CanVent, true, TabGroup.NeutralRoles, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.Artist]);
            HasImpostorVision = BooleanOptionItem.Create(Id + 14, GeneralOption.ImpostorVision, true, TabGroup.NeutralRoles, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.Artist]);
        }

        public override void Init()
       public override void Add(byte playerId)
    {
        AbilityLimit = MaxSteals.GetInt();
        killCooldown = KillCooldownOpt.GetFloat(); 

        var pc = Utils.GetPlayerById(playerId);
        pc?.AddDoubleTrigger();

        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }
    public override void SetKillCooldown(byte id)
    {
        Main.AllPlayerKillCooldown[id] = killCooldown;
    }
    public override void ApplyGameOptions(IGameOptions opt, byte id) => opt.SetVision(true);
    public override bool CanUseImpostorVentButton(PlayerControl pc) => CanVent.GetBool();
    public override bool CanUseSabotage(PlayerControl pc) => CanUsesSabotage.false();
    public override bool CanUseKillButton(PlayerControl pc) => true;

          public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (killer == null) return true;
        if (target == null) return true;

        if (PaintedList.Contains(target.PlayerId))
        {
            _ = new LateTask(() => { killer.SetKillCooldown(PaintCooldown.GetFloat()); }, 0.1f, "Artist Set Kill Cooldown");
            return true;
        }
        else
        {
            return killer.CheckDoubleTrigger(target, () => 
            { 
                {
                SetSkin(target, PaintedOutfit);
                PlayerSkinsPainted[killer.PlayerId].Add(target.PlayerId);
            }

            killer.Notify(ColorString(GetRoleColor(CustomRoles.Artist), GetString("ArtistPaintedSkin")));
            target.Notify(ColorString(GetRoleColor(CustomRoles.Artist), GetString("PaintedByArtist")));

        public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
        {
            if (AbilityLimit > 0)
            {
                return killer.CheckDoubleTrigger(target, () => { SetPainting(killer, target); });
            }
            else
            {
                return true; 
            }
        }

        private void SetSkin(PlayerControl target, NetworkedPlayerInfo.PlayerOutfit outfit)
        {
            var sender = CustomRpcSender.Create(name: $"Artist.RpcSetSkin({target.Data.PlayerName})");

            target.SetColor(outfit.ColorId);
            sender.AutoStartRpc(target.NetId, (byte)RpcCalls.SetColor)
                .Write(target.Data.NetId)
                .Write((byte)outfit.ColorId)
                .EndRpc();

            target.SetHat(outfit.HatId, outfit.ColorId);
            sender.AutoStartRpc(target.NetId, (byte)RpcCalls.SetHatStr)
                .Write(outfit.HatId)
                .Write(target.GetNextRpcSequenceId(RpcCalls.SetHatStr))
                .EndRpc();

            target.SetSkin(outfit.SkinId, outfit.ColorId);
            sender.AutoStartRpc(target.NetId, (byte)RpcCalls.SetSkinStr)
                .Write(outfit.SkinId)
                .Write(target.GetNextRpcSequenceId(RpcCalls.SetSkinStr))
                .EndRpc();

            target.SetVisor(outfit.VisorId, outfit.ColorId);
            sender.AutoStartRpc(target.NetId, (byte)RpcCalls.SetVisorStr)
                .Write(outfit.VisorId)
                .Write(target.GetNextRpcSequenceId(RpcCalls.SetVisorStr))
                .EndRpc();

            target.SetPet(outfit.PetId);
            sender.AutoStartRpc(target.NetId, (byte)RpcCalls.SetPetStr)
                .Write(outfit.PetId)
                .Write(target.GetNextRpcSequenceId(RpcCalls.SetPetStr))
                .EndRpc();

            sender.SendMessage();
        }
    }

    public static class PlayerControlExtensions
    {
        public static void Kill(this PlayerControl killer, PlayerControl target)
        {
            if (target != null)
            {
                killer.MurderPlayer(target);
                killer.Notify($"You have killed {target.Data.PlayerName}.");
            }
        }
    }
}
