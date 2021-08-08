using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using BepInEx;
using R2API;
using R2API.Utils;
using RoR2;
using RoR2.CharacterAI;
using RoR2.Networking;
using EntityStates;

namespace RoR2GameModeAPI
{
    /// <summary>
    /// Handles all API core hooks
    /// </summary>
    internal class Hooks
    {
        /// <summary>
        /// Core hook inits. Apply config changes to the game, set core hooks
        /// </summary>
        public static void Init()
        {
            //Preassign variables
            if (Settings.MaxplayerCount != 4)
            {
                typeof(RoR2Application).GetField("maxPlayers", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy | BindingFlags.Static).SetValue(null, Settings.MaxplayerCount);
                GameNetworkManager.SvMaxPlayersConVar.instance.SetString(Settings.MaxplayerCount.ToString());
                SteamworksLobbyManager.cvSteamLobbyMaxMembers.SetString(Settings.MaxplayerCount.ToString());
            }
            if (!Settings.Modded) typeof(RoR2Application).GetField("isModded", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy | BindingFlags.Static).SetValue(null, false);

            //Set debug hooks
            if (Settings.UnlockAllArtifacts) On.RoR2.PreGameController.ResolveChoiceMask += DisplayArtifacts;
            if (Settings.UnlockAllCharacters) On.RoR2.Stats.StatSheet.HasUnlockable += UnlockAll;

            //Set core hooks
            On.RoR2.Networking.GameNetworkManager.OnServerAddPlayerInternal += PlayerConnect;
            On.RoR2.Chat.SendPlayerDisconnectedMessage += PlayerDisconnect;
            On.RoR2.Run.Start += GameStart;
            Run.onRunDestroyGlobal += GameEnd;
        }

        #region Debug Hooks
        /// <summary>
        /// Hook for unlocking all artifacts
        /// </summary>
        static void DisplayArtifacts(On.RoR2.PreGameController.orig_ResolveChoiceMask orig, PreGameController self)
        {
            RuleChoiceMask unlockedChoiceMask = Reflection.GetFieldValue<RuleChoiceMask>(self, "unlockedChoiceMask");
            for (int i = 0; i < RuleCatalog.choiceCount - 4; i++)
            {
                unlockedChoiceMask[i] = true;
                Reflection.SetFieldValue(self, "unlockedChoiceMask", unlockedChoiceMask);
            }
            orig(self);
        }

        /// <summary>
        /// Hook for unlocking all characters and loadouts
        /// </summary>
        static bool UnlockAll(On.RoR2.Stats.StatSheet.orig_HasUnlockable orig, RoR2.Stats.StatSheet self, UnlockableDef unlockableDef)
        {
            return true;
        }
        #endregion

        #region Core Hooks
        /// <summary>
        /// Hook for when game session first starts, used for setting up game mode hooks and invoking events
        /// </summary>
        static void GameStart(On.RoR2.Run.orig_Start orig, Run self)
        {
            //Pre-start
            if(VoteAPI.VoteResults[Settings.GameModeVotePollName].VoteExtraDatas.ContainsKey("Votes.Game Mode Selection"))
            {
                if (VoteAPI.VoteResults[Settings.GameModeVotePollName].VoteExtraDatas["Votes.Game Mode Selection"] is string)
                {
                    string gameModeKey = (string)VoteAPI.VoteResults[Settings.GameModeVotePollName].VoteExtraDatas["Votes.Game Mode Selection"];
                    if (Settings.GameModes.ContainsKey(gameModeKey))
                    {
                        Settings.ActiveGameMode = Settings.GameModes[gameModeKey];
                        Settings.ActiveGameMode.SetHooks();
                        Settings.ActiveGameMode.SetExtraHooks();
                        Debug.Log($"Running game mode: \"{Settings.ActiveGameMode.GameModeName}\" @GameModeAPI");
                    }
                    else Debug.LogWarning($"Warning! Game mode key: \"{gameModeKey}\" not found, assuming server sided game mode! @GameModeAPI");
                }
                else Debug.LogWarning($"Warning! Game mode not found, assuming server sided game mode! @GameModeAPI");
            }
            else Debug.LogWarning($"Warning! Game mode selection not found, assuming vanilla or server sided game mode! @GameModeAPI");
            GameModeAPI.InvokeOnPreGameStart();
            orig(self);
            //Post-start
            GameModeAPI.InvokeOnPostGameStart();
        }

        /// <summary>
        /// Hook for when game session ends, used for unhooking game mode hooks and invoking events
        /// </summary>
        static void GameEnd(Run obj)
        {
            if (Settings.ActiveGameMode != null)
            {
                Settings.ActiveGameMode.UnsetHooks();
                Settings.ActiveGameMode.UnsetExtraHooks();
                Settings.ActiveGameMode = null;
            }
            GameModeAPI.InvokeOnGameEnd();
        }

        /// <summary>
        /// Hook for when a player connects to the game session, used for invoking events
        /// </summary>
        static void PlayerConnect(On.RoR2.Networking.GameNetworkManager.orig_OnServerAddPlayerInternal orig, GameNetworkManager self, NetworkConnection conn, short playerControllerId, NetworkReader extraMessageReader)
        {
            //Ghetto code to prevent duplicates
            if (self.playerPrefab == null) return;
            if (self.playerPrefab.GetComponent<NetworkIdentity>() == null) return;
            if ((int)playerControllerId < conn.playerControllers.Count && conn.playerControllers[(int)playerControllerId].IsValid && conn.playerControllers[(int)playerControllerId].gameObject != null) return;
            if (NetworkUser.readOnlyInstancesList.Count >= self.maxConnections) return;

            orig(self, conn, playerControllerId, extraMessageReader);

            //Check if steam account connection
            ClientAuthData clientAuthData = ServerAuthManager.FindAuthData(conn);
            NetworkUserId userID = clientAuthData != null ? NetworkUserId.FromSteamId(clientAuthData.steamId.value, (byte)playerControllerId) : NetworkUserId.FromIp(conn.address, (byte)playerControllerId);

            //Find NetworkUser
            NetworkUser user = null;
            for (int i = 0; i < NetworkUser.readOnlyInstancesList.Count; i++)
            {
                if(NetworkUser.readOnlyInstancesList[i].id.Equals(userID))
                {
                    user = NetworkUser.readOnlyInstancesList[i];
                    break;
                }
            }
            GameModeAPI.InvokeOnPlayerConnect(user, conn, playerControllerId);
        }

        /// <summary>
        /// Hook for when a player disconnects to the game session, used for invoking events
        /// </summary>
        static void PlayerDisconnect(On.RoR2.Chat.orig_SendPlayerDisconnectedMessage orig, NetworkUser user)
        {
            orig(user);
            GameModeAPI.InvokeOnPlayerDisconnect(user);
        }
        #endregion
    }
}
