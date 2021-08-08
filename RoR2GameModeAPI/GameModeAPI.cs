using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using TMPro;
using BepInEx;
using BepInEx.Configuration;
using Mono.Cecil.Cil;
using RoR2;
using RoR2.ContentManagement;
using HG.Reflection;
using R2API;
using R2API.Utils;

namespace RoR2GameModeAPI
{
    /// <summary>
    /// API implementing a game mode selection system to multiplayer and singleplayer lobbies with support for vanilla and modded clients along with multiple mod support.
    /// Handles registrations of game modes, provides useful event callbacks for before and after each game session, and also provides plugin information
    /// </summary>
    [BepInDependency("com.bepis.r2api")]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [NetworkCompatibility(CompatibilityLevel.NoNeedForSync)]
    [R2APISubmoduleDependency(new string[]
    {
        "AssetPlus",
        "ResourcesAPI",
    })]
    public class GameModeAPI : BaseUnityPlugin
    {
        internal static GameModeAPI instance { get; private set; }

        /*Mod Information*/
        /// <summary>
        /// Mod GUID
        /// </summary>
        public const string PluginGUID = "GameModeAPI";
        /// <summary>
        /// Mod Name
        /// </summary>
        public const string PluginName = "Game Mode API";
        /// <summary>
        /// Mod Version
        /// </summary>
        public const string PluginVersion = "1.0.0";

        /*Events*/
        /// <summary>
        /// Invoked on both client and server right before game session starts
        /// </summary>
        public static event Action OnPreGameStart;
        /// <summary>
        /// Invoked on both client and server right after game session starts
        /// </summary>
        public static event Action OnPostGameStart;
        /// <summary>
        /// Invoked on both client and server right after game session ends
        /// </summary>
        public static event Action OnGameEnd;
        /// <summary>
        /// Invoked on server only right after a player connects
        /// <para>NetworkUser user, NetworkConnection conn, short playerControllerId</para>
        /// </summary>
        public static event Action<NetworkUser, NetworkConnection, short> OnPlayerConnect;
        /// <summary>
        /// Invoked on server only right after a player disconnects
        /// <para>NetworkUser user</para>
        /// </summary>
        public static event Action<NetworkUser> OnPlayerDisconnect;

        /*Properties*/
        /// <summary>
        /// All currently registered game modes
        /// </summary>
        public static IReadOnlyDictionary<string, GameMode> GameModes { get { return Settings.GameModes; } }
        /// <summary>
        /// Currently active game mode
        /// <para>Returns null if the player is not in a match, host is running an unknown game mode not installed on the current application, or host is vanilla client</para>
        /// </summary>
        public static GameMode ActiveGameMode { get { return Settings.ActiveGameMode; } }

        /// <summary>
        /// Constructor, automatically gets created when BepInEx loads the API
        /// </summary>
        internal GameModeAPI()
        {
            if (!instance) instance = this;
            else Debug.LogWarning("Warning! Multiple instances of \"GameModeAPI\"! @GameModeAPI");

            //Module inits
            VoteAPI.SetHook();

            //Pre core inits
            Settings.Init();

            //Pre core hooks
            SceneManager.sceneLoaded += Init; //Let me know if there's a better way of doing this
        }

        /// <summary>
        /// Loads configs and init core hooks when application has reached the title scene
        /// </summary>
        /// <param name="scene">Loaded scene</param>
        /// <param name="loadSceneMode">Loaded scene mode</param>
        void Init(Scene scene, LoadSceneMode loadSceneMode)
        {
            if (scene.name == "title")
            {
                if (BodyCatalog.availability.available &&
                    ItemCatalog.availability.available &&
                    EquipmentCatalog.availability.available)
                {
                    //Loads Config Settings
                    Settings.LoadConfig(Config);

                    //Setup
                    Hooks.Init();
                    Debug.Log("GameModeAPI setup completed @GameModeAPI");
                }
                else Debug.LogError("Failed to load GameModeAPI, please let the developer know on \"https://github.com/tung362/RoR2PVP/issues\" @GameModeAPI");
                SceneManager.sceneLoaded -= Init;
            }
        }

        #region Helpers
        /// <summary>
        /// Registers a new game mode
        /// </summary>
        /// <param name="gameModeDescription">Game mode's description while in the lobby menu</param>
        /// <param name="iconPath">Game mode's sprite icon path, can be left null if no assets are available</param>
        /// <param name="gameMode">The game mode's script</param>
        public static void RegisterGameMode(string gameModeDescription, string iconPath, GameMode gameMode)
        {
            if (gameMode == null)
            {
                Debug.LogError($"Failed to add game mode, game mode is null! @GameModeAPI");
                return;
            }
            if (string.IsNullOrEmpty(gameMode.GameModeName))
            {
                Debug.LogError($"Failed to add game mode, name of game mode is null or empty! @GameModeAPI");
                return;
            }
            if (Settings.GameModes.ContainsKey(gameMode.GameModeName))
            {
                Debug.LogError($"Failed to add game mode: \"{gameMode.GameModeName}\", duplicate game mode! Potential mod conflict! @GameModeAPI");
                return;
            }

            ChoiceMenu gameModeChoice = new ChoiceMenu
            {
                TooltipName = gameMode.GameModeName,
                TooltipNameColor = new Color(0.0f, 1.0f, 0.0f, 0.4f),
                TooltipBody = gameModeDescription,
                TooltipBodyColor = Color.black,
                IconPath = iconPath,
                ChoiceIndex = -1,
                VotePollKey = Settings.GameModeVotePollName,
                ExtraData = gameMode.GameModeName
            };
            VoteAPI.AddVoteChoice(Settings.GameModeSelection, gameModeChoice);
            Settings.GameModes.Add(gameMode.GameModeName, gameMode);
        }
        #endregion

        #region Remote Invokes
        /// <summary>
        /// Allows for invoking of OnPreGameStart event remotely
        /// </summary>
        internal static void InvokeOnPreGameStart()
        {
            OnPreGameStart?.Invoke();
        }

        /// <summary>
        /// Allows for invoking of OnPostGameStart event remotely
        /// </summary>
        internal static void InvokeOnPostGameStart()
        {
            OnPostGameStart?.Invoke();
        }

        /// <summary>
        /// Allows for invoking of OnGameEnd event remotely
        /// </summary>
        internal static void InvokeOnGameEnd()
        {
            OnGameEnd?.Invoke();
        }

        /// <summary>
        /// Allows for invoking of OnPlayerConnect event remotely
        /// </summary>
        internal static void InvokeOnPlayerConnect(NetworkUser user, NetworkConnection conn, short playerControllerId)
        {
            OnPlayerConnect?.Invoke(user, conn, playerControllerId);
        }

        /// <summary>
        /// Allows for invoking of OnPlayerDisconnect event remotely
        /// </summary>
        internal static void InvokeOnPlayerDisconnect(NetworkUser user)
        {
            OnPlayerDisconnect?.Invoke(user);
        }
        #endregion
    }
}
