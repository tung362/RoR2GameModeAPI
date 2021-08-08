using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;
using BepInEx;
using BepInEx.Configuration;
using R2API;
using R2API.Utils;
using RoR2;
using RoR2.ContentManagement;
using RoR2GameModeAPI.Utils;

namespace RoR2GameModeAPI
{
    /// <summary>
    /// Keeps track of databases and cached values used by the API along with handling the pre core inits
    /// </summary>
    internal class Settings
    {
        /*Paths*/
        /// <summary>
        /// Path of the config folder, can be used to create additional config files
        /// </summary>
        public static string ConfigRootPath
        {
            get
            {
                string configPath = GameModeAPI.instance.Config.ConfigFilePath;
                return configPath.Remove(configPath.Count() - $"{GameModeAPI.PluginGUID}.cfg".Length);
            }
        }

        /*Assets*/
        /// <summary>
        /// Loaded assets
        /// </summary>
        public static AssetBundle Assets;
        /// <summary>
        /// Loaded asset provider
        /// </summary>
        public static AssetBundleResourcesProvider Provider;

        /*Votes*/
        /// <summary>
        /// Vanilla game mode
        /// </summary>
        public static GameMode VanillaGameMode = new GameMode("Vanilla");
        /// <summary>
        /// API's vote poll key
        /// </summary>
        public static string GameModeVotePollName = "GameModePoll";
        /// <summary>
        /// API's game mode selection for switching game modes
        /// </summary>
        public static RuleDef GameModeSelection;

        /*Configs*/
        //Multiplayer settings
        /// <summary>
        /// Config entry to determine the max players that can join a lobby
        /// </summary>
        public static int MaxplayerCount = 4;
        /// <summary>
        /// Config entry to determine if the modded tag should be toggled
        /// </summary>
        public static bool Modded = true;

        //Debug settings
        /// <summary>
        /// Config entry to determine if all artifacts should be unlocked
        /// </summary>
        public static bool UnlockAllArtifacts = false;
        /// <summary>
        /// Config entry to determine if all characters and loadouts should be unlocked
        /// </summary>
        public static bool UnlockAllCharacters = false;

        /*Cache*/
        /// <summary>
        /// All currently registered game modes
        /// </summary>
        public static Dictionary<string, GameMode> GameModes = new Dictionary<string, GameMode>();
        /// <summary>
        /// Currently active game mode
        /// </summary>
        public static GameMode ActiveGameMode = null;

        /// <summary>
        /// Pre core inits. Registers vote options, game modes, and asset bundles
        /// </summary>
        public static void Init()
        {
            if (GameModes.ContainsKey(VanillaGameMode.GameModeName))
            {
                Debug.LogError($"Core failed to init, game mode: \"{VanillaGameMode.GameModeName}\" already exists! @GameModeAPI");
                return;
            }

            //Register assets
            EmbeddedUtils.LoadAssetBundle("RoR2GameModeAPI.Assets.gamemode.ui", "@GameModeAPI", ref Assets, ref Provider);

            //Register lobby vote options
            ChoiceMenu vanillaChoice = new ChoiceMenu
            {
                TooltipName = "Vanilla Game Mode",
                TooltipNameColor = new Color(1.0f, 0.0f, 0.0f, 0.4f),
                TooltipBody = "Standard Game Mode",
                TooltipBodyColor = Color.black,
                IconPath = "@GameModeAPI:Assets/Resources/UI/VanillaSelected.png",
                ChoiceIndex = -1,
                VotePollKey = GameModeVotePollName,
                ExtraData = VanillaGameMode.GameModeName
            };
            VoteAPI.AddVotePoll(GameModeVotePollName);
            RuleCategoryDef gameModeHeader = VoteAPI.AddVoteHeader("Game Modes", new Color(0.357f, 0.667f, 1.0f, 1.0f), false);
            GameModeSelection = VoteAPI.AddVoteSelection(gameModeHeader, "Game Mode Selection", vanillaChoice);
            GameModeSelection.defaultChoiceIndex = 0;

            //Register game mode
            GameModes.Add(VanillaGameMode.GameModeName, VanillaGameMode);
        }

        /// <summary>
        /// Load and or create the API config and apply the results
        /// </summary>
        /// <param name="config">Config to read/write to</param>
        public static void LoadConfig(ConfigFile config)
        {
            //Multiplayer settings
            MaxplayerCount = config.Bind<int>("Multiplayer Settings", "Max Multiplayer Count", MaxplayerCount, "Max amount of players that can join your game (16 max)").Value;
            Modded = config.Bind<bool>("Multiplayer Settings", "Modded", Modded, "Set to false allows you to play with unmodded players, does not enable quickplay").Value;

            //Debug settings
            UnlockAllArtifacts = config.Bind<bool>("Debug Settings", "Unlock All Artifacts", UnlockAllArtifacts, "Set to true unlocks all artifacts").Value;
            UnlockAllCharacters = config.Bind<bool>("Debug Settings", "Unlock All Characters", UnlockAllCharacters, "Set to true unlocks all characters and loadouts").Value;
        }
    }
}
