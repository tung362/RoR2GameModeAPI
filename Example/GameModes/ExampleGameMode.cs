using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;
using RoR2;
using R2API;
using R2API.Utils;
using RoR2GameModeAPI;
using RoR2GameModeAPI.Utils;

namespace Example
{
    /// <summary>
    /// Custom game mode.
    /// While you can just use the base GameMode class, it's recommended to inherit from it to expand it's functionality to tailor a unique game mode
    /// </summary>
    public class ExampleGameMode : GameMode
    {
        #region GameModeAPI Managed
        public ExampleGameMode(string gameModeName) : base(gameModeName)
        {
            //Good place to change default game mode settings
        }

        protected override void Start(Stage self)
        {
            //Good place to create custom behaviors when the stage first starts
            //Client and server
        }

        protected override void Update(Stage self)
        {
            //Good place to create custom behaviors that require constant updates
            //Client and server
        }

        protected override void OnTeleporterInteraction(TeleporterInteraction self, Interactor activator)
        {
            //Good place to create custom behaviors when a teleporter is interacted with
            //Server only
        }

        protected override void OnGameOver(Run self, GameEndingDef gameEndingDef)
        {
            //Good place to create custom behaviors when there's a game over
            //Server only
        }

        protected override void OnPlayerRespawn(Stage self, CharacterMaster characterMaster)
        {
            //Good place to modify player characters before they respawn here
            //Server only
        }

        protected override void OnStagePopulate(SceneDirector self)
        {
            //Good place to spawn interactables here (chests, drones, etc)
            //Server only
        }

        public override void SetExtraHooks()
        {
            //Set custom game mode specific hooks here
            //Client and server
            //Example: On.RoR2.ShrineRestackBehavior.AddShrineStack += PreventRevivesShuffle;
        }

        public override void UnsetExtraHooks()
        {
            //Unset custom game mode specific hooks here
            //Client and server
            //Example: On.RoR2.ShrineRestackBehavior.AddShrineStack -= PreventRevivesShuffle;
        }
        #endregion
    }
}
