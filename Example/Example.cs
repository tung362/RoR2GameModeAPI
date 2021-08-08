using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;
using BepInEx;
using RoR2;
using R2API;
using R2API.Utils;
using RoR2GameModeAPI;
using RoR2GameModeAPI.Utils;

namespace Example
{
    /// <summary>
    /// This mod showcase an example of how to use the GameModeAPI.
    /// The goal is to create a new game mode that's similar to vanilla but has the feature to enable/disable mob spawns along with creating a votable game mode config in the lobby menu
    /// </summary>
    [BepInDependency("com.bepis.r2api")]
    [BepInDependency("GameModeAPI")] //Always include to ensure that the API loads before the mod does or you will encounter errors!
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [NetworkCompatibility(CompatibilityLevel.NoNeedForSync)]
    [R2APISubmoduleDependency(new string[]
    {
        "AssetPlus",
        "ResourcesAPI",
    })]
    public class Example : BaseUnityPlugin
    {
        /*Mod Information*/
        public const string PluginGUID = "Example";
        public const string PluginName = "Example GameModeAPI";
        public const string PluginVersion = "1.0.0";

        /*Assets Cache*/
        public static AssetBundle Assets;
        public static AssetBundleResourcesProvider Provider;

        /*Game Mode Information*/
        public static ExampleGameMode GameModeExample = new ExampleGameMode("Example");

        /*Votes*/
        public const string VotePollName = "ExampleVotePoll";
        public static int SpawnMobsChoiceIndex = 1;

        //Constructor, automatically gets created when BepInEx loads the API
        public Example()
        {
            RegisterSetup();

            //Useful GameModeAPI hooks
            GameModeAPI.OnPreGameStart += OnPreGameStart;
            GameModeAPI.OnPostGameStart += OnPostGameStart;
            GameModeAPI.OnGameEnd += OnGameEnd;
            GameModeAPI.OnPlayerConnect += OnPlayerConnect;
            GameModeAPI.OnPlayerDisconnect += OnPlayerDisconnect;
        }

        void RegisterSetup()
        {
            /*Register asset bundles*/
            EmbeddedUtils.LoadAssetBundle("Example.Assets.example.ui", "@Example", ref Assets, ref Provider);

            /*Register game mode*/
            GameModeAPI.RegisterGameMode("Game mode description", "@Example:Assets/Resources/UI/Example.png", GameModeExample);

            /*Setup Example Game Mode Vote Settings When In Lobby*/
            //Register vote option polls
            VoteAPI.AddVotePoll(VotePollName);

            //Register vote option headers
            RuleCategoryDef header = VoteAPI.AddVoteHeader("Example Settings", new Color(1.0f, 0.0f, 0.0f, 1.0f), false);

            //Register vote option selections and choices
            RuleDef spawnMobsSelection = VoteAPI.AddVoteSelection(header, "Spawn Mobs Selection", new ChoiceMenu("Spawn Mobs", new Color(0.0f, 0.58f, 1.0f, 0.4f), "Mobs will spawn on each stage", Color.black, "@Example:Assets/Resources/UI/ExampleOn.png", SpawnMobsChoiceIndex, VotePollName));
            VoteAPI.AddVoteChoice(spawnMobsSelection, new ChoiceMenu("Don't Spawn Mobs", new Color(1.0f, 0.0f, 0.0f, 0.4f), "Mobs will not spawn", Color.black, "@Example:Assets/Resources/UI/ExampleOff.png", -1, VotePollName));
            spawnMobsSelection.defaultChoiceIndex = 0;
        }

        #region GameModeAPI Hooks
        static void OnPreGameStart()
        {
            //Good place to create custom behaviors before the game session first starts
            //Client and server

            /*Applies vote results to the game mode*/
            //Ensures this only runs on the server side
            if (NetworkServer.active)
            {
                //Check if the active game mode is a ExampleGameMode instance
                if (GameModeAPI.ActiveGameMode is ExampleGameMode)
                {
                    //Cast the active game mode as ExampleGameMode
                    ExampleGameMode exampleGameMode = (ExampleGameMode)GameModeAPI.ActiveGameMode;

                    //Get the poll results belonging to this mod (The results of the votes while in the lobby)
                    VoteAPI.VoteResult pollResults = VoteAPI.VoteResults[VotePollName];

                    //Apply the vote results to the game mode settings
                    exampleGameMode.AllowVanillaSpawnMobs = pollResults.Vote.HasVote(SpawnMobsChoiceIndex);

                    //Another example use of HasVote
                    if (pollResults.Vote.HasVote(SpawnMobsChoiceIndex))
                    {
                        //Do something
                        Debug.Log("Mobs spawns are enabled @Example");
                    }
                    else
                    {
                        //Do something
                        Debug.Log("Mobs spawns are disabled @Example");
                    }
                }
            }
        }

        static void OnPostGameStart()
        {
            //Good place to create custom behaviors right after the game session first starts
            //Client and server
        }

        static void OnGameEnd()
        {
            //Good place to create custom behaviors when the game session just ended
            //Client and server
        }

        static void OnPlayerConnect(NetworkUser user, NetworkConnection conn, short playerControllerId)
        {
            //Good place to create custom behaviors when a player connects to the lobby or game session
            //Server only
        }

        static void OnPlayerDisconnect(NetworkUser user)
        {
            //Good place to create custom behaviors when a player disconnects from the lobby or game session
            //Server only
        }
        #endregion
    }
}
