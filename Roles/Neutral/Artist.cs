using AmongUs.GameOptions;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Neutral;

internal class Artist : RoleBase
{
    private static readonly NetworkedPlayerInfo.PlayerOutfit PaintedOutfit = new NetworkedPlayerInfo.PlayerOutfit()
        .Set("", 15, "", "", "visor_Crack", "", "");

    private static readonly Dictionary<byte, NetworkedPlayerInfo.PlayerOutfit> OriginalPlayerSkins = new();
    
    private const int Id = 28800;
    private static readonly HashSet<byte> PlayerIds = new();
    public static bool HasEnabled => PlayerIds.Any();

    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralKilling;

    private static OptionItem KillCooldown;
    private static OptionItem HideNameOfPaintedPlayer;
    private static OptionItem PaintCooldown;
    private static OptionItem CanVent;
    private static OptionItem HasImpostorVision;

    private static readonly Dictionary<byte, float> NowCooldown = new();
    private static readonly Dictionary<byte, List<byte>> PlayerSkinsPainted = new();
    private readonly Dictionary<byte, int> KillClickCount = new(); // Track click counts

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Artist);
        KillCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 30f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Artist])
            .SetValueFormat(OptionFormat.Seconds);
        PaintCooldown = FloatOptionItem.Create(Id + 11, "ArtistPaintCooldown", new(0f, 180f, 2.5f), 20f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Artist])
            .SetValueFormat(OptionFormat.Seconds);
        HideNameOfPaintedPlayer = BooleanOptionItem.Create(Id + 12, "ArtistHideNamePainted", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Artist]);
        CanVent = BooleanOptionItem.Create(Id + 13, GeneralOption.CanVent, true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Artist]);
        HasImpostorVision = BooleanOptionItem.Create(Id + 14, GeneralOption.ImpostorVision, true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Artist]);
    }

    public override void Init()
    {
        PlayerSkinsPainted.Clear();
        OriginalPlayerSkins.Clear();
        PlayerIds.Clear();
        KillClickCount.Clear(); // Reset click counts
    }

    public override void Add(byte playerId)
    {
        PlayerIds.Add(playerId);
        KillClickCount[playerId] = 0; // Initialize click count for the player

        var pc = Utils.GetPlayerById(playerId);
        pc.AddDoubleTrigger();

        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }

    public override void SetKillCooldown(byte id) 
        => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();

    public override void ApplyGameOptions(IGameOptions opt, byte id) 
        => opt.SetVision(HasImpostorVision.GetBool());

    public override bool CanUseKillButton(PlayerControl pc) => true;

    public override void UseKillButton(PlayerControl killer, PlayerControl target)
    {
        if (!KillClickCount.ContainsKey(killer.PlayerId))
            KillClickCount[killer.PlayerId] = 0;

        KillClickCount[killer.PlayerId]++;

        if (KillClickCount[killer.PlayerId] == 1)
        {
            // Paint the target grey
            SetPainting(killer, target);
            killer.Notify("You painted " + target.Data.PlayerName + " grey!");
        }
        else if (KillClickCount[killer.PlayerId] == 2)
        {
            // Perform a normal kill
            // Reset click count
            KillClickCount[killer.PlayerId] = 0;
            // Add killing logic here (this will depend on the existing kill logic in the game)
            killer.Kill(target);
            killer.Notify("You killed " + target.Data.PlayerName + "!");
        }
    }

    private void SetPainting(PlayerControl killer, PlayerControl target)
    {
        if (!PlayerSkinsPainted[killer.PlayerId].Contains(target.PlayerId))
        {
            SetSkin(target, PaintedOutfit);
        }

        PlayerSkinsPainted[killer.PlayerId].Add(target.PlayerId);
        killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Artist), GetString("ArtistPaintedSkin")));
        target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Artist), GetString("PaintedByArtist")));

        OriginalPlayerSkins[target.PlayerId] = Camouflage.PlayerSkins[target.PlayerId];
        Camouflage.PlayerSkins[target.PlayerId] = PaintedOutfit;
    }

    private static void SetSkin(PlayerControl target, NetworkedPlayerInfo.PlayerOutfit outfit)
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
