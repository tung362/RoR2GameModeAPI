using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using RoR2;
using R2API;
using R2API.Utils;
using RoR2GameModeAPI.Utils;

namespace RoR2GameModeAPI
{
    /// <summary>
    /// Base class representing a generic game mode. Handles the behavior of the game when active in the game session,
    /// provides useful predefined settings and callbacks along with the ability to expand upon the base game mode.
    /// <para>Works exactly like the vanilla game mode but expanded to be able to customize a few features.</para>
    /// <para>It's recommended to inherit from this base class and expand it's functionality to tailor a unique game mode</para>
    /// </summary>
    public class GameMode
    {
        /*Configuration*/
        /// <summary>
        /// Game mode's name
        /// <para>Ensure each name is unique</para>
        /// </summary>
        public readonly string GameModeName = "";
        /// <summary>
        /// Allow vanilla's mob spawning system
        /// <para>Turning this to false will prevent mobs from spawning by normal means</para>
        /// </summary>
        public bool AllowVanillaSpawnMobs = true;
        /// <summary>
        /// Allow vanilla's interactables spawning system (spawning of chests, drones, etc)
        /// <para>Turning this to false will prevent interactables from spawning by normal means but will still recieve callbacks to OnStagePopulate allowing creation of custom behaviors to happen</para>
        /// </summary>
        public bool AllowVanillaInteractableSpawns = true;
        /// <summary>
        /// Allow vanilla's teleporter interactions
        /// <para>Turning this to false will prevent the teleporter from activating by normal means but will still recieve callbacks to OnTeleporterInteraction allowing creation of custom behaviors to happen</para>
        /// </summary>
        public bool AllowVanillaTeleport = true;
        /// <summary>
        /// Allow vanilla's game over to happen
        /// <para>Turning this to false will prevent returning to lobby by normal means but will still recieve callbacks to OnGameOver allowing creation of custom behaviors to happen</para>
        /// </summary>
        public bool AllowVanillaGameOver = true;
        /// <summary>
        /// Prevents items contained in this collection from spawning in the game
        /// </summary>
        public HashSet<ItemIndex> BannedItems = new HashSet<ItemIndex>();
        /// <summary>
        /// Prevents equipments contained in this collection from spawning in the game
        /// </summary>
        public HashSet<EquipmentIndex> BannedEquipments = new HashSet<EquipmentIndex>();

        /// <summary>
        /// Constructor, assigns the game mode's name
        /// </summary>
        /// <param name="gameModeName">Game mode's name, ensure each name is unique</param>
        public GameMode(string gameModeName)
        {
            GameModeName = gameModeName;
        }

        /// <summary>
        /// Called on both client and server at the beginning of each stage after teleporting
        /// </summary>
        protected virtual void Start(Stage self) {}
        /// <summary>
        /// Called on both client and server at each fixed update tick
        /// </summary>
        protected virtual void Update(Stage self) {}
        /// <summary>
        /// Called on server only when something interacts with the teleporter
        /// </summary>
        protected virtual void OnTeleporterInteraction(TeleporterInteraction self, Interactor activator) {}
        /// <summary>
        /// Called on server only when game over conditions are met (when everyone is dead)
        /// </summary>
        protected virtual void OnGameOver(Run self, GameEndingDef gameEndingDef) {}
        /// <summary>
        /// Called on server only when something respawns
        /// </summary>
        protected virtual void OnPlayerRespawn(Stage self, CharacterMaster characterMaster) {}
        /// <summary>
        /// Called on server only when the stage is spawning interactables
        /// </summary>
        protected virtual void OnStagePopulate(SceneDirector self) {}
        /// <summary>
        /// Called on both client and server when the game session first starts
        /// <para>It's recommended to set your game mode specific hooks here</para>
        /// </summary>
        public virtual void SetExtraHooks() {}
        /// <summary>
        /// Called on both client and server when the game session ends
        /// <para>It's recommended to unset your game mode specific hooks here</para>
        /// </summary>
        public virtual void UnsetExtraHooks() {}

        /// <summary>
        /// Set core game mode hooks
        /// </summary>
        internal void SetHooks()
        {
            On.RoR2.Stage.Start += StageStartHook;
            On.RoR2.Stage.FixedUpdate += StageUpdateHook;
            On.RoR2.TeleporterInteraction.OnInteractionBegin += TeleportHook;
            On.RoR2.Run.BeginGameOver += GameOverHook;
            On.RoR2.Stage.RespawnCharacter += PlayerRespawnHook;
            On.RoR2.CombatDirector.Simulate += MobSpawnHook;
            On.RoR2.SceneDirector.Start += SpawnCreditsHook;
            On.RoR2.SceneDirector.PopulateScene += InteractablePopulateHook;
            On.RoR2.Run.BuildDropTable -= BanItemHook;
            On.RoR2.Inventory.GiveItem_ItemIndex_int += EnforceBannedItems;
            On.RoR2.Inventory.SetEquipmentInternal += EnforceBannedEquipments;
        }

        /// <summary>
        /// Unset core game mode hooks
        /// </summary>
        internal void UnsetHooks()
        {
            On.RoR2.Stage.Start -= StageStartHook;
            On.RoR2.Stage.FixedUpdate -= StageUpdateHook;
            On.RoR2.TeleporterInteraction.OnInteractionBegin -= TeleportHook;
            On.RoR2.Run.BeginGameOver -= GameOverHook;
            On.RoR2.Stage.RespawnCharacter -= PlayerRespawnHook;
            On.RoR2.CombatDirector.Simulate -= MobSpawnHook;
            On.RoR2.SceneDirector.Start -= SpawnCreditsHook;
            On.RoR2.SceneDirector.PopulateScene -= InteractablePopulateHook;
            On.RoR2.Run.BuildDropTable -= BanItemHook;
            On.RoR2.Inventory.GiveItem_ItemIndex_int -= EnforceBannedItems;
            On.RoR2.Inventory.SetEquipmentInternal -= EnforceBannedEquipments;
        }

        #region Hooks
        /// <summary>
        /// Hook for Start callbacks
        /// </summary>
        void StageStartHook(On.RoR2.Stage.orig_Start orig, Stage self)
        {
            Start(self);
            orig(self);
        }

        /// <summary>
        /// Hook for Update callbacks
        /// </summary>
        void StageUpdateHook(On.RoR2.Stage.orig_FixedUpdate orig, Stage self)
        {
            Update(self);
            orig(self);
        }

        /// <summary>
        /// Hook for OnTeleporterInteraction callbacks, also controls teleporter interactions
        /// </summary>
        void TeleportHook(On.RoR2.TeleporterInteraction.orig_OnInteractionBegin orig, TeleporterInteraction self, Interactor activator)
        {
            OnTeleporterInteraction(self, activator);
            if (AllowVanillaTeleport) orig(self, activator);
        }

        /// <summary>
        /// Hook for OnGameOver callbacks, also controls game overs
        /// </summary>
        void GameOverHook(On.RoR2.Run.orig_BeginGameOver orig, Run self, GameEndingDef gameEndingDef)
        {
            OnGameOver(self, gameEndingDef);
            if (AllowVanillaGameOver) orig(self, gameEndingDef);
        }

        /// <summary>
        /// Hook for OnPlayerRespawn callbacks, also controls respawns
        /// </summary>
        void PlayerRespawnHook(On.RoR2.Stage.orig_RespawnCharacter orig, Stage self, CharacterMaster characterMaster)
        {
            OnPlayerRespawn(self, characterMaster);
            orig(self, characterMaster);
        }

        /// <summary>
        /// Hook for controlling mob spawns
        /// </summary>
        void MobSpawnHook(On.RoR2.CombatDirector.orig_Simulate orig, CombatDirector self, float deltaTime)
        {
            if(NetworkServer.active)
            {
                if (!AllowVanillaSpawnMobs)
                {
                    orig(self, 0);
                    return;
                }
            }
            orig(self, deltaTime);
        }

        /// <summary>
        /// Hook for controlling pre stage setup mob spawns via credits and interactables spawning via credits
        /// </summary>
        void SpawnCreditsHook(On.RoR2.SceneDirector.orig_Start orig, SceneDirector self)
        {
            if (NetworkServer.active)
            {
                ClassicStageInfo sceneInfo = SceneInfo.instance.GetComponent<ClassicStageInfo>();
                if (!AllowVanillaInteractableSpawns)
                {
                    sceneInfo.sceneDirectorInteractibleCredits = 0;
                    sceneInfo.bonusInteractibleCreditObjects = null;
                }
                if (!AllowVanillaSpawnMobs) sceneInfo.sceneDirectorMonsterCredits = 0;
            }
            orig(self);
        }

        /// <summary>
        /// Hook for controlling interactables spawning
        /// </summary>
        void InteractablePopulateHook(On.RoR2.SceneDirector.orig_PopulateScene orig, SceneDirector self)
        {
            OnStagePopulate(self);
            orig(self);
        }

        /// <summary>
        /// Hook for removing items and equipments from the drop table
        /// </summary>
        void BanItemHook(On.RoR2.Run.orig_BuildDropTable orig, Run self)
        {
            if (NetworkServer.active)
            {
                foreach (ItemIndex itemIndex in BannedItems) Run.instance.availableItems.Remove(itemIndex);
                foreach (EquipmentIndex equipmentIndex in BannedEquipments) Run.instance.availableEquipment.Remove(equipmentIndex);
            }
            orig(self);

            //Lists causes errors if left empty so add junk item to empty list
            if (self.availableTier1DropList.Count == 0) self.availableTier1DropList.Add(PickupCatalog.FindPickupIndex(RoR2Content.Items.ExtraLifeConsumed.itemIndex));
            if (self.availableTier2DropList.Count == 0) self.availableTier2DropList.Add(PickupCatalog.FindPickupIndex(RoR2Content.Items.ExtraLifeConsumed.itemIndex));
            if (self.availableTier3DropList.Count == 0) self.availableTier3DropList.Add(PickupCatalog.FindPickupIndex(RoR2Content.Items.ExtraLifeConsumed.itemIndex));
            if (self.availableLunarDropList.Count == 0) self.availableLunarDropList.Add(PickupCatalog.FindPickupIndex(RoR2Content.Items.ExtraLifeConsumed.itemIndex));
            if (self.availableEquipmentDropList.Count == 0) self.availableEquipmentDropList.Add(PickupCatalog.FindPickupIndex(RoR2Content.Items.ExtraLifeConsumed.itemIndex));
            if (self.availableBossDropList.Count == 0) self.availableBossDropList.Add(PickupCatalog.FindPickupIndex(RoR2Content.Items.ExtraLifeConsumed.itemIndex));
            if (self.availableLunarEquipmentDropList.Count == 0) self.availableLunarEquipmentDropList.Add(PickupCatalog.FindPickupIndex(RoR2Content.Items.ExtraLifeConsumed.itemIndex));
            if (self.availableNormalEquipmentDropList.Count == 0) self.availableNormalEquipmentDropList.Add(PickupCatalog.FindPickupIndex(RoR2Content.Items.ExtraLifeConsumed.itemIndex));
        }

        /// <summary>
        /// Hook for enforcing banned item rules
        /// </summary>
        void EnforceBannedItems(On.RoR2.Inventory.orig_GiveItem_ItemIndex_int orig, Inventory self, ItemIndex itemIndex, int count)
        {
            ItemIndex item = itemIndex;
            if (NetworkServer.active)
            {
                //Ban check
                bool isBanned = BannedItems.Contains(item) ? true : false;

                //Reroll if banned
                if (isBanned)
                {
                    Chat.SendBroadcastChat(new Chat.SimpleChatMessage
                    {
                        baseToken = Util.GenerateColoredString("Banned item detected! rerolling...", new Color32(255, 106, 0, 255))
                    });

                    bool assigned = false;
                    ItemDef itemDef = ItemCatalog.GetItemDef(item);
                    if (itemDef != null)
                    {
                        if (itemDef.tier == ItemTier.Tier1)
                        {
                            if (Run.instance.availableTier1DropList.Count != 0)
                            {
                                item = GameModeUtils.TryGetRandomItem(Run.instance.availableTier1DropList, Run.instance.treasureRng);
                                assigned = true;
                            }
                        }
                        else if (itemDef.tier == ItemTier.Tier2)
                        {
                            if (Run.instance.availableTier2DropList.Count != 0)
                            {
                                item = GameModeUtils.TryGetRandomItem(Run.instance.availableTier2DropList, Run.instance.treasureRng);
                                assigned = true;
                            }
                        }
                        else if (itemDef.tier == ItemTier.Tier3)
                        {
                            if (Run.instance.availableTier3DropList.Count != 0)
                            {
                                item = GameModeUtils.TryGetRandomItem(Run.instance.availableTier3DropList, Run.instance.treasureRng);
                                assigned = true;
                            }
                        }
                        else if (itemDef.tier == ItemTier.Lunar)
                        {
                            if (Run.instance.availableLunarDropList.Count != 0)
                            {
                                item = GameModeUtils.TryGetRandomItem(Run.instance.availableLunarDropList, Run.instance.treasureRng);
                                assigned = true;
                            }
                        }
                        else if (itemDef.tier == ItemTier.Boss)
                        {
                            if (Run.instance.availableBossDropList.Count != 0)
                            {
                                item = GameModeUtils.TryGetRandomItem(Run.instance.availableBossDropList, Run.instance.treasureRng);
                                assigned = true;
                            }
                        }
                        else
                        {
                            item = ItemIndex.None;
                            assigned = true;
                        }
                    }
                    if (!assigned) item = ItemIndex.None;
                }
            }
            if (item == ItemIndex.None) return;
            orig(self, item, count);
        }

        /// <summary>
        /// Hook for enforcing banned equipment rules
        /// </summary>
        bool EnforceBannedEquipments(On.RoR2.Inventory.orig_SetEquipmentInternal orig, Inventory self, EquipmentState equipmentState, uint slot)
        {
            EquipmentState equipment = equipmentState;
            if (NetworkServer.active)
            {
                //Ban check
                bool isBanned = BannedEquipments.Contains(equipment.equipmentIndex) ? true : false;

                //Reroll if banned
                if (isBanned)
                {
                    Chat.SendBroadcastChat(new Chat.SimpleChatMessage
                    {
                        baseToken = Util.GenerateColoredString("Banned equipment detected! rerolling...", new Color32(255, 106, 0, 255))
                    });

                    bool assigned = false;
                    if (equipment.equipmentDef != null)
                    {
                        if (equipment.equipmentDef.isLunar)
                        {
                            if (Run.instance.availableLunarEquipmentDropList.Count != 0)
                            {
                                EquipmentIndex equipmentIndex = GameModeUtils.TryGetRandomEquipment(Run.instance.availableLunarEquipmentDropList, Run.instance.treasureRng);
                                equipment = new EquipmentState(equipmentIndex, equipment.chargeFinishTime, equipment.charges);
                                assigned = true;
                            }
                        }
                        else
                        {
                            if (Run.instance.availableNormalEquipmentDropList.Count != 0)
                            {
                                EquipmentIndex equipmentIndex = GameModeUtils.TryGetRandomEquipment(Run.instance.availableNormalEquipmentDropList, Run.instance.treasureRng);
                                equipment = new EquipmentState(equipmentIndex, equipment.chargeFinishTime, equipment.charges);
                                assigned = true;
                            }
                        }
                    }
                    if (!assigned) equipment = EquipmentState.empty;
                }
            }
            return orig(self, equipment, slot);
        }
        #endregion
    }
}
