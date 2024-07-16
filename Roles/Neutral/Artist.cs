using AmongUs.GameOptions;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Neutral;

internal class Artist : RoleBase
{
    private readonly static NetworkedPlayerInfo.PlayerOutfit PaintedOutfit = new NetworkedPlayerInfo.PlayerOutfit().Set("", 15, "", "", "visor_Crack", "", "");
    private static readonly Dictionary<byte, NetworkedPlayerInfo.PlayerOutfit> OriginalPlayerSkins = [];

    
    private const int Id = 12845;
    private static readonly HashSet<byte> PlayerIds = [];
    public static bool HasEnabled => PlayerIds.Any();
    
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralKilling;

    private static OptionItem KillCooldown;
    private static OptionItem PaintCooldown;
    private static OptionItem CanVent;
    private static OptionItem HasImpostorVision;
    private static OptionItem HideNamesOfPaintedPlayers;

    private static readonly Dictionary<byte, float> NowCooldown = [];
    private static readonly Dictionary<byte, List<byte>> PlayerSkinsPainted = [];

     public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Artist;
        KillCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 30f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Artist])
            .SetValueFormat(OptionFormat.Seconds);
        PaintCooldown = FloatOptionItem.Create(Id + 11, GeneralOption.PaintlCooldown, new(0f, 180f, 2.5f), 5f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Artist])
            .SetValueFormat(OptionFormat.Seconds);
        HideNameOfConsumedPlayer = BooleanOptionItem.Create(Id + 12, "ArtistHideNamePainted", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Artist]);

        public override void Init()
    {
        PlayerSkinsPainted.Clear();
        OriginalPlayerSkins.Clear();
        PlayerIds.Clear();
    }
    public override void Add(byte playerId)
    {
        PlayerSkinsPainted.TryAdd(playerId, []);
        PlayerIds.Add(playerId);
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);

        var pc = Utils.GetPlayerById(playerId);
        pc.AddDoubleTrigger();

        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
    public override void ApplyGameOptions(IGameOptions opt, byte id) => opt.SetVision(HasImpostorVision.GetBool());
    public override bool CanUseKillButton(PlayerControl pc) => true;
    public override bool CanUseImpostorVentButton(PlayerControl pc) => CanVent.GetBool();


    private void SetPainting(PlayerControl killer, PlayerControl target)
    {
        if (!IsPainted(killer.PlayerId, target.PlayerId))
        {
                SetSkin(target, PaintedOutfit);
            }

            PlayerSkinsPainted[killer.PlayerId].Add(target.PlayerId);
            Utils.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Artist), GetString("ArtistPaintedSkin")));
            target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Artist), GetString("PaintedByArtist")));

            OriginalPlayerSkins.Add(target.PlayerId, Camouflage.PlayerSkins[target.PlayerId]);
            Camouflage.PlayerSkins[target.PlayerId] = PaintedOutfit;
            
            SendRPC(killer.PlayerId, target.PlayerId);

            killer.SetKillCooldown();

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
