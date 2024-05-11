using AmongUs.GameOptions;
using Hazel;
using System;
using System.Threading.Tasks;
using TOHE.Modules;
using TOHE.Roles.AddOns.Common;
using TOHE.Roles.AddOns.Crewmate;
using TOHE.Roles.AddOns.Impostor;
using TOHE.Roles.Core;
using TOHE.Roles.Crewmate;
using TOHE.Roles.Impostor;
using TOHE.Roles.Neutral;
using static TOHE.Translator;

namespace TOHE;

enum CustomRPC : byte
{
    // RpcCalls can increase with each AU version
    // On version 2023.11.28 the last id in RpcCalls: 61
    VersionCheck = 80,
    RequestRetryVersionCheck = 81,
    SyncCustomSettings = 100,
    RestTOHESetting,
    SetDeathReason,
    EndGame,
    PlaySound,
    SetCustomRole,

    // TOHE
    AntiBlackout,
    SetRealKiller,
    PlayCustomSound,
    SetKillTimer,
    SyncAllPlayerNames,
    SyncNameNotify,
    ShowPopUp,
    KillFlash,
    DumpLog,
    SyncRoleSkill,
    SetNameColorData,
    GuessKill,
    Judge,
    Guess,
    CouncillorJudge,
    NemesisRevenge,
    RetributionistRevenge,
    SetFriendCode,

    //Roles
    SetBountyTarget,
    SyncPuppet,
    SyncKami,
    SetKillOrSpell,
    SetKillOrHex,
    SetKillOrCurse,
    SetCaptainTargetSpeed,
    RevertCaptainTargetSpeed,
    RevertCaptainAllTargetSpeed,
    SetCaptainVotedTarget,
    RevertCaptainVoteRemove,
    SetDousedPlayer,
    DoSpell,
    DoHex,
    DoCurse,
    SniperSync,
    SetLoversPlayers,
    SetExecutionerTarget,
    RemoveExecutionerTarget,
    SendFireworkerState,
    SetCurrentDousingTarget,
    SetEvilTrackerTarget,
    SetDrawPlayer,
    SetCrewpostorTasksDone,
    SetCurrentDrawTarget,
    RpcPassBomb,
    SyncRomanticTarget,
    SyncVengefulRomanticTarget,
    SetJailerTarget,
    SetJailerExeLimit,
    SetInspectorLimit,
    KeeperRPC,
    SetAlchemistTimer,
    UndertakerLocationSync,
    RiftMakerSyncData,
    LightningSetGhostPlayer,
    SetDarkHiderKillCount,
    SetConsigliere,
    SetGreedy,
    BenefactorRPC,
    SetSwapperVotes,
    SetMarkedPlayer,
    SetConcealerTimer,
    SetMedicalerProtectList,
    SyncPsychicRedList,
    SetMorticianArrow,
    SetAmnesaicArrows,
    SetTracefinderArrow,
    PresidentEnd,
    PresidentReveal,
    SetBKTimer,
    SetCursedSoulCurseLimit,
    SetInvestgatorLimit,
    SyncInvestigator, // Unused
    SetRevealedPlayer,
    //SetCurrentRevealTarget, // Overseer
    SetCoronerArrow,
    SetCoronerkKillerArrow,
    SetVultureArrow,
    SetRadarArrow,
    SyncVultureBodyAmount,
    SetTrackerTarget,
    SpyRedNameSync,
    SpyRedNameRemove,
    SetChameleonTimer,
    SyncAdmiredList,
    SetImitateLimit,
    SetStalkerrKillCount,
    //FFA
    SyncFFAPlayer,
    SyncFFANameNotify,
}
public enum Sounds
{
    KillSound,
    TaskComplete,
    TaskUpdateSound,
    ImpTransform,

    Test,
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.HandleRpc))]
internal class RPCHandlerPatch
{
    public static bool TrustedRpc(byte id)
    => (CustomRPC)id is CustomRPC.VersionCheck or CustomRPC.RequestRetryVersionCheck or CustomRPC.AntiBlackout or CustomRPC.Judge or CustomRPC.CouncillorJudge or CustomRPC.NemesisRevenge or CustomRPC.RetributionistRevenge or CustomRPC.Guess or CustomRPC.PresidentEnd or CustomRPC.SetSwapperVotes or CustomRPC.DumpLog or CustomRPC.SetFriendCode;
    public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] byte callId, [HarmonyArgument(1)] MessageReader reader)
    {
        var rpcType = (RpcCalls)callId;
        MessageReader subReader = MessageReader.Get(reader);
        if (EAC.ReceiveRpc(__instance, callId, reader)) return false;
        Logger.Info($"{__instance?.Data?.PlayerId}({(__instance?.Data?.PlayerId == 0 ? "Host" : __instance?.Data?.PlayerName)}):{callId}({RPC.GetRpcName(callId)})", "ReceiveRPC");
        switch (rpcType)
        {
            case RpcCalls.SetName: //SetNameRPC
                string name = subReader.ReadString();
                if (subReader.BytesRemaining > 0 && subReader.ReadBoolean()) return false;
                Logger.Info("RPC Set Name For Player: " + __instance.GetNameWithRole() + " => " + name, "SetName");
                break;
            case RpcCalls.SetRole: //SetNameRPC
                var role = (RoleTypes)subReader.ReadUInt16();
                Logger.Info("RPC Set Role For Player: " + __instance.GetRealName() + " => " + role, "SetRole");
                break;
            case RpcCalls.SendChat:
                var text = subReader.ReadString();
                Logger.Info($"{__instance.GetNameWithRole().RemoveHtmlTags()}:{text.RemoveHtmlTags()}", "ReceiveChat");
                ChatCommands.OnReceiveChat(__instance, text, out var canceled);
                if (canceled) return false;
                break;
            case RpcCalls.StartMeeting:
                var p = Utils.GetPlayerById(subReader.ReadByte());
                Logger.Info($"{__instance.GetNameWithRole()} => {p?.GetNameWithRole() ?? "null"}", "StartMeeting");
                break;
        }
        if (__instance.OwnerId != AmongUsClient.Instance.HostId &&
            ((Enum.IsDefined(typeof(CustomRPC), callId) && !TrustedRpc(callId)) // Is Custom RPC
            || (!Enum.IsDefined(typeof(CustomRPC), callId) && !Enum.IsDefined(typeof(RpcCalls), callId)))) //Is not Custom RPC and not Vanilla RPC
        {
            Logger.Warn($"{__instance?.Data?.PlayerName}:{callId}({RPC.GetRpcName(callId)}) has been canceled because it was sent by someone other than the host", "CustomRPC");
            if (AmongUsClient.Instance.AmHost)
            {
                AmongUsClient.Instance.KickPlayer(__instance.GetClientId(), false);
                Logger.Warn($"Received an uncredited RPC from {__instance?.Data?.PlayerName} and kicked it out", "Kick");
                Logger.SendInGame(string.Format(GetString("Warning.InvalidRpc"), __instance?.Data?.PlayerName));
            }
            return false;
        }
        return true;
    }
    public static void Postfix(PlayerControl __instance, [HarmonyArgument(0)] byte callId, [HarmonyArgument(1)] MessageReader reader)
    {
        // Process nothing but CustomRPC
        if (callId < (byte)CustomRPC.VersionCheck) return;

        var rpcType = (CustomRPC)callId;
        switch (rpcType)
        {
            case CustomRPC.AntiBlackout:
                if (Options.EndWhenPlayerBug.GetBool())
                {
                    Logger.Fatal($"{__instance?.Data?.PlayerName}({__instance.PlayerId}): {reader.ReadString()} 错误，根据设定终止游戏", "Anti-black");
                    ChatUpdatePatch.DoBlockChat = true;
                    Main.OverrideWelcomeMsg = string.Format(GetString("RpcAntiBlackOutNotifyInLobby"), __instance?.Data?.PlayerName, GetString("EndWhenPlayerBug"));
                    _ = new LateTask(() =>
                    {
                        Logger.SendInGame(string.Format(GetString("RpcAntiBlackOutEndGame"), __instance?.Data?.PlayerName));
                    }, 3f, "Anti-Black Msg SendInGame 1");

                    _ = new LateTask(() =>
                    {
                        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Error);
                        GameManager.Instance.LogicFlow.CheckEndCriteria();
                        RPC.ForceEndGame(CustomWinner.Error);
                    }, 5.5f, "Anti-Black End Game 1");
                }
                else if (GameStates.IsOnlineGame)
                {
                    Logger.Fatal($"{__instance?.Data?.PlayerName}({__instance.PlayerId}): Change Role Setting Postfix 错误，根据设定继续游戏", "Anti-black");
                    _ = new LateTask(() =>
                    {
                        Logger.SendInGame(string.Format(GetString("RpcAntiBlackOutIgnored"), __instance?.Data?.PlayerName));
                    }, 3f, "Anti-Black Msg SendInGame 2");
                }
                break;

            case CustomRPC.VersionCheck:
                try
                {
                    Version version = Version.Parse(reader.ReadString());
                    string tag = reader.ReadString();
                    string forkId = reader.ReadString();
                    bool cheating = reader.ReadBoolean();
                    if (__instance.GetClientId() < 0)
                        break;
                    Main.playerVersion[__instance.GetClientId()] = new PlayerVersion(version, tag, forkId);

                    if (Main.VersionCheat.Value && __instance.GetClientId() == AmongUsClient.Instance.HostId) RPC.RpcVersionCheck();

                    if (__instance.GetClientId() == Main.HostClientId && cheating)
                        Main.IsHostVersionCheating = true;

                    if (Main.VersionCheat.Value && AmongUsClient.Instance.AmHost)
                        Main.playerVersion[__instance.GetClientId()] = Main.playerVersion[AmongUsClient.Instance.HostId];

                    // Kick Unmached Player Start
                    if (AmongUsClient.Instance.AmHost)
                    {
                        if (!IsVersionMatch(__instance.GetClientId()))
                        {
                            _ = new LateTask(() =>
                            {
                                if (__instance?.Data?.Disconnected is not null and not true)
                                {
                                    var msg = string.Format(GetString("KickBecauseDiffrentVersionOrMod"), __instance?.Data?.PlayerName);
                                    Logger.Warn(msg, "Version Kick");
                                    Logger.SendInGame(msg);
                                    AmongUsClient.Instance.KickPlayer(__instance.GetClientId(), false);
                                }
                            }, 5f, "Kick Because Diffrent Version Or Mod");
                        }
                    }
                    // Kick Unmached Player End
                }
                catch
                {
                    Logger.Warn($"{__instance?.Data?.PlayerName}({__instance.PlayerId}): バージョン情報が無効です", "RpcVersionCheck");
                    
                    _ = new LateTask(() =>
                    {
                        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.RequestRetryVersionCheck, SendOption.Reliable, __instance.GetClientId());
                        AmongUsClient.Instance.FinishRpcImmediately(writer);
                    }, 1f, "Retry Version Check Task");
                }
                break;

            case CustomRPC.RequestRetryVersionCheck:
                RPC.RpcVersionCheck();
                break;

            case CustomRPC.SetFriendCode:
                RPC.SetFriendCode(__instance, reader.ReadString());
                break;

            case CustomRPC.SyncCustomSettings:
                if (AmongUsClient.Instance.AmHost) break;

                List<OptionItem> listOptions = [];
                List<OptionItem> allOptionsList = [.. OptionItem.AllOptions];

                var startAmount = reader.ReadPackedInt32();
                var lastAmount = reader.ReadPackedInt32();

                var countAllOptions = OptionItem.AllOptions.Count;

                // Add Options
                for (var option = startAmount; option < countAllOptions && option <= lastAmount; option++)
                {
                    listOptions.Add(allOptionsList[option]);
                }

                var countOptions = listOptions.Count;
                Logger.Msg($"StartAmount/LastAmount: {startAmount}/{lastAmount} :--: ListOptionsCount/AllOptions: {countOptions}/{countAllOptions}", "CustomRPC.SyncCustomSettings");

                // Sync Settings
                foreach (var option in listOptions.ToArray())
                {
                    // Set Value Options
                    option.SetValue(reader.ReadPackedInt32());

                    // Set Preset 5 for modded non-host players
                    if (startAmount == 0 && option.Name == "Preset" && option.CurrentValue != 4)
                    {
                        option.SetValue(4); // 4 => Preset 5
                    }
                }
                OptionShower.GetText();
                break;

            case CustomRPC.SetDeathReason:
                RPC.GetDeathReason(reader);
                break;
            case CustomRPC.EndGame:
                RPC.EndGame(reader);
                break;
            case CustomRPC.PlaySound:
                byte playerID = reader.ReadByte();
                Sounds sound = (Sounds)reader.ReadByte();
                RPC.PlaySound(playerID, sound);
                break;
            case CustomRPC.ShowPopUp:
                string msg = reader.ReadString();
                HudManager.Instance.ShowPopUp(msg);
                break;
            case CustomRPC.SetCustomRole:
                byte CustomRoleTargetId = reader.ReadByte();
                CustomRoles role = (CustomRoles)reader.ReadPackedInt32();
                RPC.SetCustomRole(CustomRoleTargetId, role);
                break;
            case CustomRPC.SyncRoleSkill:
                RPC.SyncRoleSkillReader(reader, __instance);
                break;
            case CustomRPC.SetBountyTarget:
                BountyHunter.ReceiveRPC(reader);
                break;
            case CustomRPC.SyncPuppet:
                Puppeteer.ReceiveRPC(reader);
                break;
             case CustomRPC.SyncKami when __instance.GetRoleClass() is Kamikaze km:
                km.ReceiveRPC(reader);
                break;
            case CustomRPC.SetKillOrSpell:
                Witch.ReceiveRPC(reader, false);
                break;
            case CustomRPC.SetKillOrHex:
                HexMaster.ReceiveRPC(reader, false);
                break;

            case CustomRPC.SetCaptainTargetSpeed:
                Captain.ReceiveRPCSetSpeed(reader);
                break;
            case CustomRPC.RevertCaptainTargetSpeed:
                Captain.ReceiveRPCRevertSpeed(reader);
                break;
            case CustomRPC.RevertCaptainAllTargetSpeed:
                Captain.ReceiveRPCRevertAllSpeed();
                break;
            case CustomRPC.SetCaptainVotedTarget:
                Captain.ReceiveRPCVoteAdd(reader);
                break;
            case CustomRPC.RevertCaptainVoteRemove:
                Captain.ReceiveRPCVoteRemove(reader);
                break;
            case CustomRPC.SetDrawPlayer:
                Revolutionist.ReceiveDrawPlayerRPC(reader);
                break;
            case CustomRPC.SetRevealedPlayer:
                Overseer.ReceiveSetRevealedPlayerRPC(reader);
                break;
            case CustomRPC.SetNameColorData:
                NameColorManager.ReceiveRPC(reader);
                break;
            case CustomRPC.DoSpell:
                Witch.ReceiveRPC(reader, true);
                break;
            case CustomRPC.DoHex:
                HexMaster.ReceiveRPC(reader, true);
                break;
            case CustomRPC.SniperSync:
                Sniper.ReceiveRPC(reader);
                break;
            case CustomRPC.UndertakerLocationSync:
                Undertaker.ReceiveRPC(reader);
                break;
            case CustomRPC.RiftMakerSyncData:
                RiftMaker.ReceiveRPC(reader);
                break;
            case CustomRPC.SetLoversPlayers:
                Main.LoversPlayers.Clear();
                int count = reader.ReadInt32();
                for (int i = 0; i < count; i++)
                    Main.LoversPlayers.Add(Utils.GetPlayerById(reader.ReadByte()));
                break;
            case CustomRPC.SetExecutionerTarget:
                Executioner.ReceiveRPC(reader, SetTarget: true);
                break;
            case CustomRPC.RemoveExecutionerTarget:
                Executioner.ReceiveRPC(reader, SetTarget: false);
                break;
            case CustomRPC.SendFireworkerState:
                Fireworker.ReceiveRPC(reader);
                break;
            case CustomRPC.SetCurrentDousingTarget:
                Arsonist.ReceiveCurrentDousingTargetRPC(reader);
                break;
            case CustomRPC.SetDousedPlayer:
                Arsonist.ReceiveSetDousedPlayerRPC(reader);
                break;
            case CustomRPC.SetCurrentDrawTarget:
                Revolutionist.ReceiveSetCurrentDrawTarget(reader);
                break;
            case CustomRPC.SetEvilTrackerTarget:
                EvilTracker.ReceiveRPC(reader);
                break;
            case CustomRPC.SetRealKiller:
                byte targetId = reader.ReadByte();
                byte killerId = reader.ReadByte();
                RPC.SetRealKiller(targetId, killerId);
                break;
            case CustomRPC.SetTrackerTarget when __instance.GetRoleClass() is Tracker tr:
                tr.ReceiveRPC(reader);
                break;
            case CustomRPC.SetJailerExeLimit:
                Jailer.ReceiveRPC(reader, setTarget: false);
                break;
            case CustomRPC.SetJailerTarget:
                Jailer.ReceiveRPC(reader, setTarget: true);
                break;
            case CustomRPC.SetCrewpostorTasksDone:
                Crewpostor.ReceiveRPC(reader);
                break;
            case CustomRPC.SyncAdmiredList when __instance.GetRoleClass() is Admirer ad:
                ad.ReceiveRPC(reader, true);
                break;
            case CustomRPC.PlayCustomSound:
                CustomSoundsManager.ReceiveRPC(reader);
                break;
            case CustomRPC.LightningSetGhostPlayer:
                Lightning.ReceiveRPC(reader);
                break;
            case CustomRPC.SetStalkerrKillCount:
                Stalker.ReceiveRPC(reader);
                break;
            case CustomRPC.SetGreedy:
                Greedy.ReceiveRPC(reader);
                break;
            case CustomRPC.BenefactorRPC:
                Benefactor.ReceiveRPC(reader);
                break;
            case CustomRPC.RestTOHESetting:
                OptionItem.AllOptions.ToArray().Where(x => x.Id > 0).Do(x => x.SetValueNoRpc(x.DefaultValue));
                OptionShower.GetText();
                break;
            case CustomRPC.GuessKill:
                GuessManager.RpcClientGuess(Utils.GetPlayerById(reader.ReadByte()));
                break;
            case CustomRPC.SetMarkedPlayer:
                Ninja.ReceiveRPC(reader);
                break;
            case CustomRPC.SetMedicalerProtectList:
                Medic.ReceiveRPCForProtectList(reader);
                break;
            case CustomRPC.SyncPsychicRedList:
                Psychic.ReceiveRPC(reader);
                break;
            case CustomRPC.SetKillTimer:
                float time = reader.ReadSingle();
                PlayerControl.LocalPlayer.SetKillTimer(time);
                break;
            case CustomRPC.SyncFFAPlayer:
                FFAManager.ReceiveRPCSyncFFAPlayer(reader);
                break;
            case CustomRPC.SyncAllPlayerNames:
                Main.AllPlayerNames.Clear();
                int num = reader.ReadPackedInt32();
                for (int i = 0; i < num; i++)
                    Main.AllPlayerNames.TryAdd(reader.ReadByte(), reader.ReadString());
                break;
            case CustomRPC.SyncFFANameNotify:
                FFAManager.ReceiveRPCSyncNameNotify(reader);
                break;
            case CustomRPC.SetMorticianArrow:
                Mortician.ReceiveRPC(reader);
                break;
            case CustomRPC.SetAmnesaicArrows:
                Amnesiac.ReceiveRPC(reader);
                break;
            case CustomRPC.SetTracefinderArrow:
                Tracefinder.ReceiveRPC(reader);
                break;
            case CustomRPC.SyncNameNotify:
                NameNotifyManager.ReceiveRPC(reader);
                break;
            case CustomRPC.Judge:
                Judge.ReceiveRPC_Custom(reader, __instance);
                break;
            case CustomRPC.PresidentEnd:
                President.ReceiveRPC(reader, __instance);
                break;
            case CustomRPC.PresidentReveal:
                President.ReceiveRPC(reader, __instance, isEnd: false);
                break;
            case CustomRPC.CouncillorJudge:
                Councillor.ReceiveRPC_Custom(reader, __instance);
                break;
            case CustomRPC.Guess:
                GuessManager.ReceiveRPC(reader, __instance);
                break;
            case CustomRPC.NemesisRevenge:
                Nemesis.ReceiveRPC_Custom(reader, __instance);
                break;
            case CustomRPC.RetributionistRevenge:
                Retributionist.ReceiveRPC_Custom(reader, __instance);
                break;
            case CustomRPC.SetChameleonTimer when __instance.GetRoleClass() is Chameleon cl:
                cl.ReceiveRPC_Custom(reader);
                break;
            case CustomRPC.SetAlchemistTimer:
                Alchemist.ReceiveRPC(reader);
                break;
            case CustomRPC.SetCursedSoulCurseLimit:
                CursedSoul.ReceiveRPC(reader);
                break;
            case CustomRPC.SetConsigliere:
                Consigliere.ReceiveRPC(reader);
                break;
            case CustomRPC.SetInvestgatorLimit:
                Investigator.ReceiveRPC(reader);
                break;
            case CustomRPC.KillFlash:
                Utils.FlashColor(new(1f, 0f, 0f, 0.3f));
                if (Constants.ShouldPlaySfx()) RPC.PlaySound(PlayerControl.LocalPlayer.PlayerId, Sounds.KillSound);
                break;
            case CustomRPC.DumpLog:
                var target = Utils.GetPlayerById(reader.ReadByte());
                if (target != null && !target.FriendCode.GetDevUser().DeBug)
                {
                    Logger.Info($"Player {target.GetNameWithRole()} used /dump", "RPC_DumpLogger");
                }
                break;
            case CustomRPC.SetCoronerArrow:
                Coroner.ReceiveRPC(reader);
                break;
            case CustomRPC.SetCoronerkKillerArrow:
                Coroner.ReceiveRPCKiller(reader);
                break;
            case CustomRPC.SetVultureArrow:
                Vulture.ReceiveRPC(reader);
                break;
            case CustomRPC.SetRadarArrow:
                Radar.ReceiveRPC(reader);
                break;
            case CustomRPC.SyncVultureBodyAmount:
                Vulture.ReceiveBodyRPC(reader);
                break;
            case CustomRPC.SpyRedNameSync:
                Spy.ReceiveRPC(reader);
                break;
            case CustomRPC.SpyRedNameRemove:
                Spy.ReceiveRPC(reader, isRemove: true);
                break;
            //case CustomRPC.SetCleanserCleanLimit:
            //    Cleanser.ReceiveRPC(reader);
            //    break;
            case CustomRPC.SetInspectorLimit:
                Inspector.ReceiveRPC(reader);
                break;
            case CustomRPC.KeeperRPC:
                Keeper.ReceiveRPC(reader);
                break;
            case CustomRPC.SetSwapperVotes when __instance.GetRoleClass() is Swapper sw:
                sw.ReceiveSwapRPC(reader, __instance);
                break;
        }
    }

    private static bool IsVersionMatch(int ClientId)
    {
        if (Main.VersionCheat.Value) return true;
        Version version = Main.playerVersion[ClientId].version;
        string tag = Main.playerVersion[ClientId].tag;
        string forkId = Main.playerVersion[ClientId].forkId;
        
        if (version != Main.version
            || tag != $"{ThisAssembly.Git.Commit}({ThisAssembly.Git.Branch})"
            || forkId != Main.ForkId)
            return false;

        return true;
    }
}

internal static class RPC
{
    //SyncCustomSettingsRPC Sender
    public static void SyncCustomSettingsRPC(int targetId = -1)
    {
        if (targetId != -1)
        {
            var client = Utils.GetClientById(targetId);
            if (client == null || client.Character == null || !Main.playerVersion.ContainsKey(client.Id))
            {
                return;
            }
        }

        if (!AmongUsClient.Instance.AmHost || PlayerControl.AllPlayerControls.Count <= 1 || (AmongUsClient.Instance.AmHost == false && PlayerControl.LocalPlayer == null))
        {
            return;
        }

        var amount = OptionItem.AllOptions.Count;
        int divideBy = amount / 4;

        for (var i = 0; i <= 4; i++)
        {
            SyncOptionsBetween(i * divideBy, (i + 1) * divideBy, amount, targetId);
        }
    }

    static void SyncOptionsBetween(int startAmount, int lastAmount, int amountAllOptions, int targetId = -1)
    {
        if (targetId != -1)
        {
            var client = Utils.GetClientById(targetId);
            if (client == null || client.Character == null || !Main.playerVersion.ContainsKey(client.Id))
            {
                return;
            }
        }

        if (!AmongUsClient.Instance.AmHost || PlayerControl.AllPlayerControls.Count <= 1 || (AmongUsClient.Instance.AmHost == false && PlayerControl.LocalPlayer == null))
        {
            return;
        }

        if (amountAllOptions != OptionItem.AllOptions.Count)
        {
            amountAllOptions = OptionItem.AllOptions.Count;
        }

        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncCustomSettings, SendOption.Reliable, targetId);

        writer.WritePacked(startAmount);
        writer.WritePacked(lastAmount);

        List<OptionItem> listOptions = [];
        List<OptionItem> allOptionsList = [.. OptionItem.AllOptions];

        // Add Options
        for (var option = startAmount; option < amountAllOptions && option <= lastAmount; option++)
        {
            listOptions.Add(allOptionsList[option]);
        }

        var countListOptions = listOptions.Count;
        Logger.Msg($"StartAmount/LastAmount: {startAmount}/{lastAmount} :--: ListOptionsCount/AllOptions: {countListOptions}/{amountAllOptions}", "SyncOptionsBetween");

        // Sync Settings
        foreach (var option in listOptions.ToArray())
        {
            writer.WritePacked(option.GetValue());
        }

        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }

    // Not Used
    public static void SyncCustomSettingsRPCforOneOption(OptionItem option)
    {
        List<OptionItem> allOptions = new(OptionItem.AllOptions);
        var placement = allOptions.IndexOf(option);
        if (placement != -1)
            SyncOptionsBetween(placement, placement, OptionItem.AllOptions.Count);
    }
    public static void PlaySoundRPC(byte PlayerID, Sounds sound)
    {
        if (AmongUsClient.Instance.AmHost)
            PlaySound(PlayerID, sound);
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.PlaySound, SendOption.Reliable, -1);
        writer.Write(PlayerID);
        writer.Write((byte)sound);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void SyncAllPlayerNames()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncAllPlayerNames, SendOption.Reliable, -1);
        writer.WritePacked(Main.AllPlayerNames.Count);
        foreach (var name in Main.AllPlayerNames)
        {
            writer.Write(name.Key);
            writer.Write(name.Value);
        }
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ShowPopUp(this PlayerControl pc, string msg)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.ShowPopUp, SendOption.Reliable, pc.GetClientId());
        writer.Write(msg);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ExileAsync(PlayerControl player)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(player.NetId, (byte)RpcCalls.Exiled, SendOption.Reliable, -1);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        player.Exiled();
    }
    public static void RpcSetFriendCode(string fc)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpc(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetFriendCode, SendOption.None);
        writer.Write(fc);
        writer.EndMessage();
        SetFriendCode(PlayerControl.LocalPlayer, fc);
    }
    public static void SetFriendCode(PlayerControl target, string fc)
    {
        if (GameStates.IsVanillaServer) return; //Anticheat
        target.FriendCode = fc;
        target.Data.FriendCode = fc;
        target.GetClient().FriendCode = fc;
    }
    public static async void RpcVersionCheck()
    {
        try
        {
            while (PlayerControl.LocalPlayer == null || AmongUsClient.Instance.HostId < 0 || PlayerControl.LocalPlayer.GetClientId() < 0) await Task.Delay(500);
            var hostId = AmongUsClient.Instance.HostId;
            if (Main.playerVersion.ContainsKey(hostId) || !Main.VersionCheat.Value)
            {
                bool cheating = Main.VersionCheat.Value;
                MessageWriter writer = AmongUsClient.Instance.StartRpc(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.VersionCheck, SendOption.Reliable);
                writer.Write(cheating ? Main.playerVersion[hostId].version.ToString() : Main.PluginVersion);
                writer.Write(cheating ? Main.playerVersion[hostId].tag : $"{ThisAssembly.Git.Commit}({ThisAssembly.Git.Branch})");
                writer.Write(cheating ? Main.playerVersion[hostId].forkId : Main.ForkId);
                writer.Write(cheating);
                writer.EndMessage();
            }
            Main.playerVersion[PlayerControl.LocalPlayer.GetClientId()] = new PlayerVersion(Main.PluginVersion, $"{ThisAssembly.Git.Commit}({ThisAssembly.Git.Branch})", Main.ForkId);
        }
        catch
        {
            Logger.Error("Error while trying to send RPCVersionCheck, retry later", "RpcVersionCheck");
            _ = new LateTask(() => RpcVersionCheck(), 1f, "Retry RPCVersionCheck");
        }
    }
    public static async void RpcRequestRetryVersionCheck()
    {
        while (PlayerControl.LocalPlayer == null || AmongUsClient.Instance.GetHost() == null) await Task.Delay(500);
        var hostId = AmongUsClient.Instance.HostId;
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.RequestRetryVersionCheck, SendOption.Reliable, hostId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void SendDeathReason(byte playerId, PlayerState.DeathReason deathReason)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetDeathReason, SendOption.Reliable, -1);
        writer.Write(playerId);
        writer.WritePacked((int)deathReason);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void GetDeathReason(MessageReader reader)
    {
        var playerId = reader.ReadByte();
        var deathReason = (PlayerState.DeathReason)reader.ReadInt32();
        Main.PlayerStates[playerId].deathReason = deathReason;
        Main.PlayerStates[playerId].IsDead = true;
    }
    public static void ForceEndGame(CustomWinner win)
    {
        if (ShipStatus.Instance == null) return;
        try { CustomWinnerHolder.ResetAndSetWinner(win); }
        catch { }
        if (AmongUsClient.Instance.AmHost)
        {
            ShipStatus.Instance.enabled = false;
            try { GameManager.Instance.LogicFlow.CheckEndCriteria(); }
            catch { }
            try { GameManager.Instance.RpcEndGame(GameOverReason.ImpostorDisconnect, false); }
            catch { }
        }
    }
    public static void EndGame(MessageReader reader)
    {
        try
        {
            CustomWinnerHolder.ReadFrom(reader);
        }
        catch (Exception ex)
        {
            Logger.Error($"正常にEndGameを行えませんでした。\n{ex}", "EndGame", false);
        }
    }
    public static void PlaySound(byte playerID, Sounds sound)
    {
        if (PlayerControl.LocalPlayer.PlayerId == playerID)
        {
            switch (sound)
            {
                case Sounds.KillSound:
                    SoundManager.Instance.PlaySound(PlayerControl.LocalPlayer.KillSfx, false, 1f);
                    break;
                case Sounds.TaskComplete:
                    SoundManager.Instance.PlaySound(DestroyableSingleton<HudManager>.Instance.TaskCompleteSound, false, 1f);
                    break;
                case Sounds.TaskUpdateSound:
                    SoundManager.Instance.PlaySound(DestroyableSingleton<HudManager>.Instance.TaskUpdateSound, false, 1f);
                    break;
                case Sounds.ImpTransform:
                    SoundManager.Instance.PlaySound(DestroyableSingleton<HnSImpostorScreamSfx>.Instance.HnSOtherImpostorTransformSfx, false, 0.8f);
                    break;
            }
        }
    }
    public static void SetCustomRole(byte targetId, CustomRoles role)
    {
        if (role < CustomRoles.NotAssigned)
        {
            Main.PlayerStates[targetId].SetMainRole(role);
            targetId.GetRoleClassById()?.OnAdd(targetId);
        }
        else if (role >= CustomRoles.NotAssigned)   //500:NoSubRole 501~:SubRole 
        {
            Main.PlayerStates[targetId].SetSubRole(role);
        }

        switch (role)
        {
            case CustomRoles.LastImpostor:
                LastImpostor.Add(targetId);
                break;
            case CustomRoles.Aware:
                Aware.Add(targetId);
                break;
            case CustomRoles.Radar:
                Radar.Add(targetId);
                break;
            case CustomRoles.Glow:
                Glow.Add(targetId);
                break;
            case CustomRoles.Workhorse:
                Workhorse.Add(targetId);
                break;
            case CustomRoles.Diseased:
                Diseased.Add();
                break;
            case CustomRoles.Antidote:
                Antidote.Add();
                break;
            case CustomRoles.Burst:
                Burst.Add();
                break;
            case CustomRoles.Fool:
                Fool.Add();
                break;
            case CustomRoles.Ghoul:
                Ghoul.Add();
                break;
            case CustomRoles.Rainbow:
                Rainbow.Add();
                break;
            case CustomRoles.Statue:
                Statue.Add(targetId);
                break;

        }

        HudManager.Instance.SetHudActive(true);
    //    HudManager.Instance.Chat.SetVisible(true);

        if (PlayerControl.LocalPlayer.PlayerId == targetId) RemoveDisableDevicesPatch.UpdateDisableDevices();
    }
    public static void SyncRoleSkillReader(MessageReader reader, PlayerControl pc)
    {
        CustomRoles role = (CustomRoles)reader.ReadPackedInt32();
        Logger.Info($"Received Sync Role Skill RPC for role {role}", "SyncRoleSkillReader");

        try
        {
            switch (role)
            {
                //Crew Roles
                case CustomRoles.Admirer when pc.GetRoleClass() is Admirer ad:
                    ad.ReceiveRPC(reader, false);
                    break;
                default:
                    pc.GetRoleClass().ReceiveRPC(reader, pc);
                    break;
            }
        }
        catch (Exception error)
        {
            Logger.Error($"Role {role} - error RPC:{error}", "SyncRoleSkillReader");
        }
    }
    public static void RpcDoSpell(byte targetId, byte killerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.DoSpell, SendOption.Reliable, -1);
        writer.Write(targetId);
        writer.Write(killerId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void SyncLoversPlayers()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetLoversPlayers, SendOption.Reliable, -1);
        writer.Write(Main.LoversPlayers.Count);
        foreach (var lp in Main.LoversPlayers)
        {
            writer.Write(lp.PlayerId);
        }
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void SendRpcLogger(uint targetNetId, byte callId, int targetClientId = -1)
    {
        if (!DebugModeManager.AmDebugger) return;
        string rpcName = GetRpcName(callId);
        string from = targetNetId.ToString();
        string target = targetClientId.ToString();
        try
        {
            target = targetClientId < 0 ? "All" : AmongUsClient.Instance.GetClient(targetClientId).PlayerName;
            from = Main.AllPlayerControls.FirstOrDefault(c => c.NetId == targetNetId)?.Data?.PlayerName;
        }
        catch { }
        Logger.Info($"FromNetID:{targetNetId}({from}) TargetClientID:{targetClientId}({target}) CallID:{callId}({rpcName})", "SendRPC");
    }
    public static string GetRpcName(byte callId)
    {
        string rpcName;
        if ((rpcName = Enum.GetName(typeof(RpcCalls), callId)) != null) { }
        else if ((rpcName = Enum.GetName(typeof(CustomRPC), callId)) != null) { }
        else rpcName = callId.ToString();
        return rpcName;
    }
    public static void SetRealKiller(byte targetId, byte killerId)
    {
        var state = Main.PlayerStates[targetId];
        state.RealKiller.Item1 = DateTime.Now;
        state.RealKiller.Item2 = killerId;

        if (!AmongUsClient.Instance.AmHost) return;
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetRealKiller, SendOption.Reliable, -1);
        writer.Write(targetId);
        writer.Write(killerId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
}
[HarmonyPatch(typeof(InnerNet.InnerNetClient), nameof(InnerNet.InnerNetClient.StartRpc))]
internal class StartRpcPatch
{
    public static void Prefix(/*InnerNet.InnerNetClient __instance,*/ [HarmonyArgument(0)] uint targetNetId, [HarmonyArgument(1)] byte callId)
    {
        RPC.SendRpcLogger(targetNetId, callId);
    }
}
[HarmonyPatch(typeof(InnerNet.InnerNetClient), nameof(InnerNet.InnerNetClient.StartRpcImmediately))]
internal class StartRpcImmediatelyPatch
{
    public static void Prefix(/*InnerNet.InnerNetClient __instance,*/ [HarmonyArgument(0)] uint targetNetId, [HarmonyArgument(1)] byte callId, [HarmonyArgument(3)] int targetClientId = -1)
    {
        RPC.SendRpcLogger(targetNetId, callId, targetClientId);
    }
}

