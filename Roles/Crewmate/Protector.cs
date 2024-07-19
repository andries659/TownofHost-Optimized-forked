using Hazel;
using System;
using System.Collections.Generic;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate
{
    internal class Protector : CustomRole
    {
        //===========================SETUP================================\
        private const int Id = 29000;
        private static readonly HashSet<byte> playerIdList = new();
        public static bool HasEnabled => playerIdList.Count > 0;

        public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
        public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmatePower;
        //==================================================================\

        private static OptionItem ShieldDuration;
        private static OptionItem ShieldIsOneTimeUse;

        private static readonly Dictionary<byte, HashSet<int>> taskIndex = new();
        private static readonly Dictionary<byte, long> shieldedPlayers = new();

        private static void OnTaskComplete(GameData.PlayerInfo playerInfo)
        {
            if (!playerIdList.Contains(playerInfo.PlayerId)) return;

            var protectorRole = RoleManager.GetPlayerRole<Protector>(playerInfo.PlayerId);
            if (protectorRole == null) return;

            ApplyShield(playerInfo.PlayerId);
        }

        private static void ApplyShield(byte playerId)
        {
            long duration = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + ((int)ShieldDuration.GetValue() * 1000);
            shieldedPlayers[playerId] = duration;

            PlayerControl playerControl = PlayerControl.allPlayerControls.FirstOrDefault(p => p.PlayerId == playerId);
            if (playerControl != null)
            {
                // code to give a temporary shield (e.g., setting a flag or applying a visual effect)
            }
        }

        private static void RemoveShield(byte playerId)
        {
            shieldedPlayers.Remove(playerId);
            PlayerControl playerControl = PlayerControl.allPlayerControls.FirstOrDefault(p => p.PlayerId == playerId);
            if (playerControl != null)
            {
                // code to remove the shield (e.g., clearing the flag or removing the visual effect)
            }
        }

        public override void SetupCustomOption()
        {
            Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Protector);
            ShieldDuration = FloatOptionItem.Create(Id + 10, "ShieldDuration", new(1, 30, 1), 10, TabGroup.CrewmateRoles, false)
                                            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Protector])
                                            .SetValueFormat(OptionFormat.Votes);
                                            
            ShieldIsOneTimeUse = BooleanOptionItem.Create(Id + 11, "ShieldIsOneTimeUse", false, TabGroup.CrewmateRoles, false)
                                                  .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Protector]);
                                                  
            Options.OverrideTasksData.Create(Id + 12, TabGroup.CrewmateRoles, CustomRoles.Protector);
        }

        public override void OnPlayerTaskComplete(PlayerControl player, byte taskId)
        {
            base.OnPlayerTaskComplete(player, taskId);
            if (shieldedPlayers.ContainsKey(player.PlayerId) && ShieldIsOneTimeUse.GetBool())
            {
                RemoveShield(player.PlayerId);
            }
            else
            {
                OnTaskComplete(GameData.Instance.GetPlayerById(player.PlayerId));
            }
        }

        public override void OnPlayerMove(PlayerControl player)
        {
            base.OnPlayerMove(player);
            long currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (shieldedPlayers.TryGetValue(player.PlayerId, out long endTime) && endTime <= currentTime)
            {
            []
                RemoveShield(player.PlayerId);
            }
        }
    }
}
