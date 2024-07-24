using Hazel;
using System;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate;

internal class Protector : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 29400;
    private static readonly HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Any();
    
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmatePower;
    //==================================================================\\

    private static OptionItem TaskMarkPerRoundOpt;
    private static OptionItem ShieldDuration;
    private static OptionItem ShieldIsOneTimeUse;

    private static int maxTasksMarkedPerRound = new();

    private static readonly Dictionary<byte, HashSet<int>> taskIndex = [];
    private static readonly Dictionary<byte, int> TaskMarkPerRound = [];
    private static readonly Dictionary<byte, long> shieldedPlayers = [];

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Protector);
        TaskMarkPerRoundOpt = IntegerOptionItem.Create(Id + 10, "TasksMarkPerRound", new(1, 14, 1), 3, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Protector])
            .SetValueFormat(OptionFormat.Votes);
        ShieldDuration = FloatOptionItem.Create(Id + 11, "ShieldDuration", new(1, 30, 1), 10, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Protector])
            .SetValueFormat(OptionFormat.Votes);
        ShieldIsOneTimeUse = BooleanOptionItem.Create(Id + 12, "ShieldIsOneTimeUse", false, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Protector]);
        Options.OverrideTasksData.Create(Id + 13, TabGroup.CrewmateRoles, CustomRoles.Protector);
    }

    public override void Init()
    {
        playerIdList.Clear();
        taskIndex.Clear();
        shieldedPlayers.Clear();
        TaskMarkPerRound.Clear();
        maxTasksMarkedPerRound = TaskMarkPerRoundOpt.GetInt();
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        TaskMarkPerRound[playerId] = 0;
    }
    public override void Remove(byte playerId)
    {
        playerIdList.Remove(playerId);
        TaskMarkPerRound.Remove(playerId);
    }

    private static void SendRPC(int type, byte protectorId = 0xff, byte targetId = 0xff, int taskIndex = -1)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.ProtectorRPC, SendOption.Reliable, -1);
        writer.Write(type);
        if (type == 0)
        {
            writer.Write(protectorId);
        }
        if (type == 2)
        {
            writer.Write(protectorId);
            writer.Write(TaskMarkPerRound[protectorId]);
            writer.Write(taskIndex);
        }
        if (type == 3)
        {
            writer.Write(protectorId);
            writer.Write(taskIndex);
            writer.Write(targetId);
            writer.Write(shieldedPlayers[targetId].ToString());
        }
        if (type == 4)
        {
            writer.Write(targetId);
        }
        AmongUsClient.Instance.FinishRpcImmediately(writer);

    }

    public static void ReceiveRPC(MessageReader reader)
    {
        int type = reader.ReadInt32();
        if (type == 0)
        {
            byte protectorId = reader.ReadByte();
            TaskMarkPerRound[protectorId] = 0;
            if (taskIndex.ContainsKey(protectorId)) taskIndex[protectorId].Clear();
        }
        if (type == 1) shieldedPlayers.Clear();
        if (type == 2)
        {
            byte protectorId = reader.ReadByte();
            int taskMarked = reader.ReadInt32();
            TaskMarkPerRound[protectorId] = taskMarked;
            int taskInd = reader.ReadInt32();
            if (!taskIndex.ContainsKey(protectorId)) taskIndex[protectorId] = [];
            taskIndex[protectorId].Add(taskInd);
        }
        if (type == 3)
        {
            byte protectorId = reader.ReadByte();
            int taskInd = reader.ReadInt32();
            if (!taskIndex.ContainsKey(protectorId)) taskIndex[protectorId] = [];
            taskIndex[protectorId].Remove(taskInd);
            byte targetId = reader.ReadByte();
            string stimeStamp = reader.ReadString();
            if (long.TryParse(stimeStamp, out long timeStamp)) shieldedPlayers[targetId] = timeStamp;
        }
        if (type == 4)
        {
            byte targetId = reader.ReadByte();
            shieldedPlayers.Remove(targetId);
        }
    }

    public override string GetProgressText(byte PlayerId, bool comms)
    {
        if (!TaskMarkPerRound.ContainsKey(PlayerId)) TaskMarkPerRound[PlayerId] = 0;
        int markedTasks = TaskMarkPerRound[PlayerId];
        int x = Math.Max(maxTasksMarkedPerRound - markedTasks, 0);
        return Utils.ColorString(Utils.GetRoleColor(CustomRoles.Taskinator).ShadeColor(0.25f), $"({x})");
    }

    public override void AfterMeetingTasks()
    {
        foreach (var playerId in TaskMarkPerRound.Keys.ToArray())
        {
            TaskMarkPerRound[playerId] = 0;
            if (taskIndex.ContainsKey(playerId)) taskIndex[playerId].Clear();
            SendRPC(type: 0, protectorId: playerId); //clear taskindex
        }
        if (shieldedPlayers.Any())
        {
            shieldedPlayers.Clear();
            SendRPC(type: 1); //clear all shielded players
        }
    }

    public override void OnTaskComplete(PlayerControl player, PlayerTask task) // runs for every player which compeletes a task
    {
        if (!AmongUsClient.Instance.AmHost) return;
        
        if (!HasEnabled) return;
        if (player == null || _Player == null) return;
        if (!player.IsAlive()) return;
        
        byte playerId = player.PlayerId;
        
        if (player.Is(CustomRoles.Protector))
        {
            var benefactorPC = Utils.GetPlayerById(benefactorId);
            if (benefactorPC == null) continue;

            player.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Protector), GetString("ProtectorShield")));
            player.RpcGuardAndKill();

            long now = Utils.GetTimeStamp();
            shieldedPlayers[playerId] = now;
            taskIndex[protectorId].Remove(task.Index);
            SendRPC(type: 3, protectorId: protectorId, targetId: playerId, taskIndex: task.Index); // remove taskindex and add shieldedPlayer time
            Logger.Info($"{player.GetAllRoleName()} got a shield from completing a task", "Protector");
        }
    }

    public override bool CheckMurderOnOthersTarget(PlayerControl killer, PlayerControl target)
    {
        if (target == null || killer == null) return true;
        if (!shieldedPlayers.ContainsKey(target.PlayerId)) return false;

        if (ShieldIsOneTimeUse.GetBool())
        {
            shieldedPlayers.Remove(target.PlayerId);
            SendRPC(type: 4, targetId: target.PlayerId);
            Logger.Info($"{target.GetNameWithRole()} shield broken", "ProtectorShieldBroken");
        }
        killer.RpcGuardAndKill();
        killer.SetKillCooldown();
        return true;
    }

    public override void OnFixedUpdateLowLoad(PlayerControl pc)
    {
        var now = Utils.GetTimeStamp();
        foreach (var x in shieldedPlayers.Where(x => x.Value + ShieldDuration.GetInt() < now).ToArray())
        {
            var target = x.Key;
            shieldedPlayers.Remove(target);
            Utils.GetPlayerById(target)?.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Protector), GetString("PKProtectOut")));
            Utils.GetPlayerById(target)?.RpcGuardAndKill();
            SendRPC(type: 4, targetId: target); //remove shieldedPlayer
        }
    }
}
