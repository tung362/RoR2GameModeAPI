using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.Networking;
using BepInEx;
using R2API;
using R2API.Utils;
using RoR2;
using RoR2.Networking;
using RoR2.UI;
using RoR2.CharacterAI;

namespace RoR2GameModeAPI.Utils
{
    /// <summary>
    /// Useful utilities methods for handling game mode functionalities
    /// </summary>
    public static class GameModeUtils
    {
        /// <summary>
        /// Attempts to spawn multiple of the specified interactable randomly throughout the stage
        /// </summary>
        /// <param name="prefabPath">Path of the interactable to try spawn
        /// <para>ie: "SpawnCards/InteractableSpawnCard/iscChest1"</para></param>
        /// <param name="amountToTrySpawn">Amount to try attempt to spawn</param>
        /// <param name="Price">Interactable's price to set
        /// <para>Setting the price to -1 will use predefined price value instead</para></param>
        /// <param name="rng">RNG to use for the attempts
        /// <para>ie: Run.instance.stageRng</para></param>
        public static void CustomGenerate(string prefabPath, int amountToTrySpawn, int Price, Xoroshiro128Plus rng)
        {
            for (int i = 0; i < amountToTrySpawn; i++)
            {
                //Amount of attempts to try spawning this prefab before moving on
                int tries = 0;
                while (tries < 10)
                {
                    DirectorPlacementRule placementRule = new DirectorPlacementRule
                    {
                        placementMode = DirectorPlacementRule.PlacementMode.Random
                    };
                    //Spawn
                    GameObject spawnedObject = DirectorCore.instance.TrySpawnObject(new DirectorSpawnRequest((InteractableSpawnCard)Resources.Load(prefabPath), placementRule, rng));

                    if (spawnedObject)
                    {
                        //Find PurchaseInteraction
                        PurchaseInteraction purchaseInteraction = spawnedObject.GetComponent<PurchaseInteraction>();
                        if (purchaseInteraction)
                        {
                            if (purchaseInteraction.costType == CostTypeIndex.Money)
                            {
                                //Apply unscaled cost
                                purchaseInteraction.Networkcost = Price == -1 ? purchaseInteraction.cost : Price;
                                break;
                            }
                        }

                        break;
                    }
                    else tries++;
                }
            }
        }

        /// <summary>
        /// Attempts to spawn the specified interactable randomly on the stage
        /// </summary>
        /// <param name="prefabPath">Path of the interactable to try spawn
        /// <para>ie: "SpawnCards/InteractableSpawnCard/iscChest1"</para></param>
        /// <param name="Price">Interactable's price to set
        /// <para>Setting the price to -1 will use predefined price value instead</para></param>
        /// <param name="rng">RNG to use for the attempts
        /// <para>ie: Run.instance.stageRng</para></param>
        /// <returns></returns>
        public static GameObject CustomGenerate(string prefabPath, int Price, Xoroshiro128Plus rng)
        {
            //Amount of attempts to try spawning this prefab before moving on
            int tries = 0;
            while (tries < 10)
            {
                DirectorPlacementRule placementRule = new DirectorPlacementRule
                {
                    placementMode = DirectorPlacementRule.PlacementMode.Random
                };
                //Spawn
                GameObject spawnedObject = DirectorCore.instance.TrySpawnObject(new DirectorSpawnRequest((InteractableSpawnCard)Resources.Load(prefabPath), placementRule, rng));

                if (spawnedObject)
                {
                    //Find PurchaseInteraction
                    PurchaseInteraction purchaseInteraction = spawnedObject.GetComponent<PurchaseInteraction>();
                    if (purchaseInteraction)
                    {
                        if (purchaseInteraction.costType == CostTypeIndex.Money)
                        {
                            //Apply unscaled cost
                            purchaseInteraction.Networkcost = Price == -1 ? purchaseInteraction.cost : Price;
                            break;
                        }
                    }

                    return spawnedObject;
                }
                else tries++;
            }
            return null;
        }

        /// <summary>
        /// Attempts to find the specified stage and outputs the result
        /// </summary>
        /// <param name="stageName">Name of the stage</param>
        /// <param name="stage">Output stage, null if stage doesn't exist</param>
        /// <returns>Returns true if stage exists</returns>
        public static bool TryGetStage(string stageName, out SceneDef stage)
        {
            stage = SceneCatalog.GetSceneDefFromSceneName(stageName);
            if (!stage)
            {
                Debug.LogWarning($"Warning! Stage name: \"{stageName}\" does not exist, mod using GameModeAPI might be outdated! @GameModeAPI");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Attempts to find and add the specified stage to a list
        /// </summary>
        /// <param name="stageName">Name of the stage</param>
        /// <param name="stages">List to add the stage to</param>
        public static void TryAddStage(string stageName, List<SceneDef> stages)
        {
            if(TryGetStage(stageName, out SceneDef stage)) stages.Add(stage);
        }

        /// <summary>
        /// Attempts to roll for an item from a list
        /// </summary>
        /// <param name="list">List to roll from</param>
        /// <param name="rng">RNG to use for the attempts
        /// <para>ie: Run.instance.treasureRng</para></param>
        /// <returns>Returns the rolled item</returns>
        public static ItemIndex TryGetRandomItem(List<PickupIndex> list, Xoroshiro128Plus rng)
        {
            if (list.Count != 0)
            {
                int tries = 0;
                while (tries < 10)
                {
                    ItemIndex index = PickupCatalog.GetPickupDef(list[rng.RangeInt(0, list.Count)]).itemIndex;
                    if (index != ItemIndex.None) return index;
                    else tries++;
                }
            }
            return ItemIndex.None;
        }

        /// <summary>
        /// Attempts to roll for an equipment from a list
        /// </summary>
        /// <param name="list">List to roll from</param>
        /// <param name="rng">RNG to use for the attempts
        /// <para>ie: Run.instance.treasureRng</para></param>
        /// <returns>Returns the rolled equipment</returns>
        public static EquipmentIndex TryGetRandomEquipment(List<PickupIndex> list, Xoroshiro128Plus rng)
        {
            if (list.Count != 0)
            {
                int tries = 0;
                while (tries < 10)
                {
                    EquipmentIndex index = PickupCatalog.GetPickupDef(list[rng.RangeInt(0, list.Count)]).equipmentIndex;
                    if (index != EquipmentIndex.None) return index;
                    else tries++;
                }
            }
            return EquipmentIndex.None;
        }
    }
}
