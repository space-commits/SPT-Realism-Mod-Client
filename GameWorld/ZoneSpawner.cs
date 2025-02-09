using Comfort.Common;
using EFT;
using EFT.Quests;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using static RootMotion.FinalIK.IKSolver;

namespace RealismMod
{
    public static class ZoneSpawner
    {
        public const float MinBotSpawnDistanceFromPlayer = 150f;

        private static QuestDataClass GetQuest(string questId) 
        {
            var sessionData = Singleton<ClientApplication<ISession>>.Instance.GetClientBackEndSession();
            return sessionData.Profile.QuestsData.FirstOrDefault(q => q.Id == questId);
        }

        public static bool CheckQuestStatus(string questId, EQuestStatus[] questStatuses) 
        {
            bool foundMatchingQuest = false;
            var dynamicZoneQuest = GetQuest(questId);
            if (dynamicZoneQuest != null)
            {
                foreach (var status in questStatuses)
                {
                    if (dynamicZoneQuest.Status == status)
                    {
                        foundMatchingQuest = true;
                        break;
                    }
                }
            }
            return foundMatchingQuest;
        }

        public static bool HasMetQuestCriteria(string[] questIds, EQuestStatus[] questStatuses)
        {
            foreach (var quest in questIds) 
            {
               if (CheckQuestStatus(quest, questStatuses)) return true;
            }
            return false;
        }

        public static bool ShouldSpawnDynamicZones()
        {
            return PluginConfig.ZoneDebug.Value || ProfileData.PMCLevel >= 20 || HasMetQuestCriteria(new string[] { "66dad1a18cbba6e558486336", "670ae811bd43cbf026768126" },  new EQuestStatus[] { EQuestStatus.Started, EQuestStatus.Success });
        }

        //for player, get closest spawn. For bot, sort by min distance, or furthest from player failing that.
        public static Vector3 TryGetSafeSpawnPoint(Player entitiy, bool isBot, bool blocksNav, bool isInRads)
        {
            IEnumerable<Vector3> spawns = ZoneData.GetSafeSpawns();
            if (spawns == null || (isBot && !blocksNav) || (!isBot && GameWorldController.CurrentMap == "laboratory" && !isInRads)) return entitiy.Transform.position; //can't account for bot vs player, because of maps like Labs where player should spawn in gas
            IEnumerable<Vector3> validSpawns = spawns;
            Player player = Utils.GetYourPlayer();

            if (isBot)
            {
                validSpawns = spawns.Where(s => Vector3.Distance(s, player.Transform.position) >= MinBotSpawnDistanceFromPlayer); //min distance from player for bot
            }

            if (validSpawns.Any() || !isBot)
            {
                return validSpawns.OrderBy(s => Vector3.Distance(s, entitiy.Transform.position)).First(); //if found spawns for bot, or if not a bot, find the closest spwan
            }
            else 
            {
                return spawns.OrderByDescending(s => Vector3.Distance(s, player.Transform.position)).First(); //if failed to find spawn point that is min distance from player, get the closest one
            }
        }

        public static void CreateZones(ZoneCollection collection)
        {
            var zones = ZoneData.GetZones(collection.ZoneType, GameWorldController.CurrentMap);
            if (zones == null) return;
            foreach (var zone in zones)
            {
                if (collection.ZoneType == EZoneType.Gas || collection.ZoneType == EZoneType.GasAssets) CreateZone<GasZone>(zone, collection.ZoneType);
                if (collection.ZoneType == EZoneType.Radiation || collection.ZoneType == EZoneType.RadAssets) CreateZone<RadiationZone>(zone, collection.ZoneType);
                if (collection.ZoneType == EZoneType.SafeZone) CreateZone<LabsSafeZone>(zone, collection.ZoneType);
                if (collection.ZoneType == EZoneType.Quest) CreateZone<QuestZone>(zone, collection.ZoneType);
                if (collection.ZoneType == EZoneType.Interactable) CreateZone<InteractionZone>(zone, collection.ZoneType);
            }
        }

        private static bool ShouldSpawnZone(HazardGroup hazardLocation, EZoneType zoneType) 
        {
            if (PluginConfig.ZoneDebug.Value) return true;

            if (!Plugin.FikaPresent) 
            {
                if (hazardLocation.QuestToBlock != null && CheckQuestStatus(hazardLocation.QuestToBlock, new EQuestStatus[] { EQuestStatus.Success })) return false;
                if (hazardLocation.QuestToEnable != null && !CheckQuestStatus(hazardLocation.QuestToEnable, new EQuestStatus[] { EQuestStatus.Success })) return false;

                bool doTimmyFactor = ProfileData.PMCLevel <= 10f && hazardLocation.SpawnChance < 1f && zoneType != EZoneType.Radiation && zoneType != EZoneType.RadAssets && GameWorldController.CurrentMap != "laboratory";
                float timmyFactor = doTimmyFactor && GameWorldController.CurrentMap == "sandbox" ? 0f : doTimmyFactor ? 0.25f : 1f;
                float zoneProbability = Mathf.Max(hazardLocation.SpawnChance * timmyFactor, 0.01f);
                zoneProbability = Mathf.Clamp01(zoneProbability);
                float randomValue = UnityEngine.Random.value;
                return randomValue <= zoneProbability;
            }

            DateTime utcNow = DateTime.UtcNow;
            int seed = utcNow.Year * 1000000 + utcNow.Month * 10000 + utcNow.Day * 100;
            int finalSeed = seed % 101;
            return finalSeed <= hazardLocation.SpawnChance * 100f;    
        }

        private static bool AnalsyableQuestChecker(string[] quests, EQuestStatus[] statuses) 
        {
            bool match = false;
            if (quests != null && quests.Length > 0)
            {
                foreach (var q in quests)
                {
                    if (CheckQuestStatus(q, statuses))
                    {
                        match = true;
                    }
                }
            }
            return match;
        }

        private static bool CheckIsAnalysable(Analysable analysable) 
        {
            if (analysable.NoRequirement) return true;
            bool isDisabled = AnalsyableQuestChecker(analysable.DisabledBy, new EQuestStatus[] { EQuestStatus.Started }); //essentially checking that the quest is not completed and not active
            bool isEnabled = AnalsyableQuestChecker(analysable.EnabledBy, new EQuestStatus[] { EQuestStatus.Started });
            return !isDisabled || isEnabled;
        }

        public static void AddGasVisual(Zone subZone, GameObject hazardZone, EZoneType zoneType, Vector3 position, Vector3 rotation, Vector3 size) 
        {
            GameObject fogAsset = Assets.FogBundle.LoadAsset<GameObject>("Assets/Fog/Gray Volume Fog.prefab");
            GameObject spawnedFog = UnityEngine.Object.Instantiate(fogAsset, position, Quaternion.Euler(rotation));
            spawnedFog.transform.SetParent(hazardZone.transform, true);
            var particleSystems = spawnedFog.GetComponentsInChildren<ParticleSystem>();
            spawnedFog.name = subZone.Name + "_particle_system";

            foreach (var ps in particleSystems)
            {
                FogScript fogComponent = ps.gameObject.AddComponent<FogScript>();
                ps.gameObject.transform.rotation = Quaternion.Euler(rotation);
                ps.gameObject.transform.position = position;
                ParticleSystem.ShapeModule shapeModule = ps.shape;
                fogComponent.Scale = size * subZone.VisZoneSizeMulti;
                fogComponent.UsePhysics = subZone.VisUsePhysics;
                fogComponent.SpeedModi = subZone.VisSpeedModi;
                fogComponent.OpacityModi = subZone.VisOpacityModi;
                fogComponent.ParticleRate = subZone.VisParticleRate;
                fogComponent.ParticleSize = new ParticleSystem.MinMaxCurve(4f, 7f);
            }
        }

        private static void SetUpSubZone<T>(HazardGroup hazardLocation, Zone subZone, GameObject zoneGroup, EZoneType zoneType, bool isBufferZone = false) where T : MonoBehaviour, IZone
        {
            string zoneName = subZone.Name;
            Vector3 position = new Vector3(subZone.Position.X, subZone.Position.Y, subZone.Position.Z);
            Vector3 rotation = new Vector3(subZone.Rotation.X, subZone.Rotation.Y, subZone.Rotation.Z);
            Vector3 size = new Vector3(subZone.Size.X, subZone.Size.Y, subZone.Size.Z);
            bool isGasZone = zoneType == EZoneType.Gas || zoneType == EZoneType.GasAssets;

            GameObject hazardZone = new GameObject(zoneName);
            T hazard = hazardZone.AddComponent<T>();

            hazard.UsesDistanceFalloff = subZone.UsesDistanceFalloff;

            float strengthModifier = 1f;
            if (isGasZone && (!Plugin.FikaPresent && !PluginConfig.ZoneDebug.Value) && GameWorldController.CurrentMap != "laboratory")
            {
                strengthModifier = UnityEngine.Random.Range(0.9f, 1.15f);
            }
            hazard.ZoneStrength = subZone.Strength * strengthModifier;
            hazard.IsAnalysable = subZone?.Analysable == null ? false : CheckIsAnalysable(subZone.Analysable);

            hazardZone.transform.position = position;
            hazardZone.transform.rotation = Quaternion.Euler(rotation);

            EFT.Interactive.TriggerWithId trigger = hazardZone.AddComponent<EFT.Interactive.TriggerWithId>();
            trigger.SetId(zoneName);

            string questZoneName = hazardLocation.Assets != null && zoneName.Contains("dynamicquest") ? "dynamic" + GameWorldController.CurrentMap : zoneName;

            EFT.Interactive.ExperienceTrigger questTrigger = hazardZone.AddComponent<EFT.Interactive.ExperienceTrigger>();
            questTrigger.SetId(questZoneName);

            EFT.Interactive.PlaceItemTrigger placeIemTrigger = hazardZone.AddComponent<EFT.Interactive.PlaceItemTrigger>();
            placeIemTrigger.SetId(questZoneName);

            hazardZone.layer = LayerMask.NameToLayer("Triggers");
            hazardZone.name = zoneName;

            BoxCollider boxCollider = hazardZone.AddComponent<BoxCollider>();
            boxCollider.isTrigger = true;
            boxCollider.size = size;

            //if gas event or rad event, all bots have gas mask, but asset zone assets do not block bot paths so they get stuck
            bool ignoreNav = (GameWorldController.DoMapGasEvent || GameWorldController.DoMapRads) && hazard.ZoneType != EZoneType.GasAssets && hazard.ZoneType != EZoneType.RadAssets;
            hazard.BlocksNav = ignoreNav ? false : subZone.BlockNav;

            if (hazard.BlocksNav)
            {
                var navMeshObstacle = hazardZone.AddComponent<NavMeshObstacle>();
                navMeshObstacle.carving = true;
                navMeshObstacle.center = boxCollider.center;
                navMeshObstacle.size = boxCollider.size;
            }

            if (zoneType == EZoneType.Interactable) 
            {
                hazard.InteractableData = subZone.Interactable;
            }

            if (!isBufferZone && subZone.UseVisual && isGasZone && PluginConfig.ShowGasEffects.Value) AddGasVisual(subZone, hazardZone, zoneType, position, rotation, size);

            hazardZone.transform.SetParent(zoneGroup.transform, true);

            // visual representation for debugging
            if (PluginConfig.ZoneDebug.Value && !isBufferZone && zoneType != EZoneType.Interactable)
            {
                GameObject visualRepresentation = GameObject.CreatePrimitive(PrimitiveType.Cube);
                visualRepresentation.name = zoneName + "Visual";
                visualRepresentation.transform.parent = hazardZone.transform;
                visualRepresentation.transform.localScale = size;
                visualRepresentation.transform.localPosition = boxCollider.center;
                visualRepresentation.transform.rotation = boxCollider.transform.rotation;
                visualRepresentation.GetComponent<Renderer>().material.color = hazard.ZoneType == EZoneType.Radiation || hazard.ZoneType == EZoneType.RadAssets ? new UnityEngine.Color(0f, 1f, 0f, 0.15f) : new UnityEngine.Color(1f, 0f, 0f, 0.15f);
                UnityEngine.Object.Destroy(visualRepresentation.GetComponent<Collider>()); // Remove the collider from the visual representation
                MoveDaCube.AddComponentToExistingGO(visualRepresentation, zoneName);
            }
        }

        //add low-strength buffer zone around subzone to warn player
        private static void AddBufferZone<T>(HazardGroup hazardLocation, Zone subZone, GameObject zoneGroup, EZoneType zoneType) where T : MonoBehaviour, IZone
        {
            Zone bufferZone = new Zone();
            bufferZone.Name = subZone.Name + "_buffeZone";
            bufferZone.UsesDistanceFalloff = false;
            bufferZone.Strength = 10;
            bufferZone.BlockNav = false;
            bufferZone.Position = subZone.Position;
            bufferZone.Rotation = subZone.Rotation;
            Vector3 size = new Vector3(subZone.Size.X, subZone.Size.Y, subZone.Size.Z) * 1.4f;
            bufferZone.Size = new Size { X = size.x, Y = size.y, Z = size.z };
            SetUpSubZone<T>(hazardLocation, bufferZone, zoneGroup, zoneType, true);
        }

        public static void CreateZone<T>(HazardGroup hazardLocation, EZoneType zoneType) where T : MonoBehaviour, IZone
        {
            if (hazardLocation.IsTriggered || !ShouldSpawnZone(hazardLocation, zoneType)) return;
            HandleZoneAssets(hazardLocation);
            HandleZoneLoot(hazardLocation);

            GameObject hazardGroupObject = new GameObject(zoneType + Utils.GenId());
            foreach (var subZone in hazardLocation.Zones)
            {
                SetUpSubZone<T>(hazardLocation, subZone, hazardGroupObject, zoneType);
                if(subZone.UseVisual && (zoneType == EZoneType.Gas || zoneType == EZoneType.GasAssets)) AddBufferZone<T>(hazardLocation, subZone, hazardGroupObject, zoneType);
            }

            if (zoneType == EZoneType.Interactable) 
            {
                var group = hazardGroupObject.AddComponent<InteractableGroupComponent>();
                group.GroupData = hazardLocation.InteractableGroup;
            }
        }

        public static void HandleZoneAssets(HazardGroup zone) 
        {
            if (zone.Assets == null) return;
            foreach (var asset in zone.Assets) 
            {
                if (Utils.SystemRandom.Next(101) > asset.Odds && !Plugin.FikaPresent) continue;

                if (asset.RandomizeRotation) 
                {
                    asset.Rotation.Y = Utils.SystemRandom.Range(0, 360);
                }

                Vector3 position = new Vector3(asset.Position.X, asset.Position.Y, asset.Position.Z);
                Vector3 rotation = new Vector3(asset.Rotation.X, asset.Rotation.Y, asset.Rotation.Z);

                GameObject assetPrefab = GetAndLoadAsset(asset.AssetName);
                if (assetPrefab == null) 
                {
                    Utils.Logger.LogError("Realism Mod: Error Loading Asset From Bundle For Asset: " + asset.AssetName);
                }
                GameObject spawnedAsset = UnityEngine.Object.Instantiate(assetPrefab, position, Quaternion.Euler(rotation));
            }
        }

        public static void HandleZoneLoot(HazardGroup zone)
        {
            if (zone.Loot == null || Plugin.FikaPresent) return;

            foreach (var loot in zone.Loot)
            {
                if (Utils.SystemRandom.Next(101) > loot.Odds) continue;

                if (loot.RandomizeRotation)
                {
                    loot.Rotation.Y = Utils.SystemRandom.Range(0, 360);
                }

                Vector3 position = new Vector3(loot.Position.X, loot.Position.Y, loot.Position.Z);
                Vector3 rotaiton = new Vector3(loot.Rotation.X, loot.Rotation.Y, loot.Rotation.Z);

                string lootTemplateId = loot?.LootOverride != null && loot.LootOverride.Count > 0 ? GetLootTempalteIdFromOverride(loot.LootOverride) : GetLootTempalteIdFromTier(loot.Type);  

                LoadLooseLoot(position, rotaiton, lootTemplateId);
            }
        }

        public static string GetLootTempalteIdFromTier(string lootTier) 
        {
            Dictionary<string, int> lootDict;
            switch (lootTier) 
            {
       
                case "highTier":
                    lootDict = DynamicRadZoneLoot.HighTier;
                    break;
                case "midTier":
                    lootDict = DynamicRadZoneLoot.MidTier;
                    break;
                case "lowTier":
                default:
                    lootDict = DynamicRadZoneLoot.LowTier;
                    break;

            }
            return Utils.GetRandomWeightedKey(lootDict);
        }

        public static string GetLootTempalteIdFromOverride(Dictionary<string, int> lootOdds)
        {
            return Utils.GetRandomWeightedKey(lootOdds);
        }

        //previously I stored the loaded assets as static fields and used reflection to dynamically load them, however this strangely caused issues with certain bundles,
        //so instead I have to use this method to manually load in assets
        public static GameObject GetAndLoadAsset(string assetName)
        {
            if (assetName == "GooBarrel") return Assets.GooBarrelBundle.LoadAsset<GameObject>("Assets/Labs/yellow_barrel.prefab");
            if (assetName == "BlueBox") return Assets.BlueBoxBundle.LoadAsset<GameObject>("Assets/Prefabs/polytheneBox (6).prefab");
            if (assetName == "RedForkLift") return Assets.RedForkLiftBundle.LoadAsset<GameObject>("Assets/Prefabs/autoloader.prefab");
            if (assetName == "ElectroForkLift") return Assets.ElectroForkLiftBundle.LoadAsset<GameObject>("Assets/Prefabs/electroCar (2).prefab");
            if (assetName == "LabsCrate") return Assets.LabsCrateBundle.LoadAsset<GameObject>("Assets/Prefabs/woodBox_medium.prefab");
            if (assetName == "Ural") return Assets.UralBundle.LoadAsset<GameObject>("Assets/Prefabs/ural280_closed_update.prefab");
            if (assetName == "BluePallet") return Assets.BluePalletBundle.LoadAsset<GameObject>("Assets/Prefabs/pallete_plastic_blue (10).prefab");
            if (assetName == "BlueFuelPalletCloth") return Assets.BlueFuelPalletClothBundle.LoadAsset<GameObject>("Assets/Prefabs/pallet_barrel_heap_update.prefab");
            if (assetName == "BarrelPile") return Assets.BarrelPileBundle.LoadAsset<GameObject>("Assets/Prefabs/barrel_pile (1).prefab");
            if (assetName == "LabsCrateSmall") return Assets.LabsCrateSmallBundle.LoadAsset<GameObject>("Assets/Prefabs/woodBox_small (2).prefab");
            if (assetName == "YellowPlasticPallet") return Assets.YellowPlasticPalletBundle.LoadAsset<GameObject>("Assets/Prefabs/pallet_barrel_plastic_clear_P (4).prefab");
            if (assetName == "WhitePlasticPallet") return Assets.WhitePlasticPalletBundle.LoadAsset<GameObject>("Assets/Prefabs/pallet_barrel_plastic_clear_P (5).prefab");
            if (assetName == "MetalFence") return Assets.MetalFenceBundle.LoadAsset<GameObject>("Assets/Prefabs/fence_metall_part3_update.prefab");
            if (assetName == "LabsBarrelPile") return Assets.LabsBarrelPileBundle.LoadAsset<GameObject>("Assets/Realism Hazard Prefabs/Prefab/Barrel_plastic_clear_set_01.prefab"); 
            if (assetName == "RedContainer") return Assets.RedContainerBundle.LoadAsset<GameObject>("Assets/Prefabs/container_6m_red_close.prefab");
            if (assetName == "BlueContainer") return Assets.BlueContainerBundle.LoadAsset<GameObject>("container_6m_blue_close (1)");
            if (assetName == "RadSign1") return Assets.RadSign1.LoadAsset<GameObject>("Assets/prefabs/Rad Sign 1.prefab");
            if (assetName == "TerraGroupFence") return Assets.TerraGroupFence.LoadAsset<GameObject>("Assets/prefabs/fence_ema_nocurt (10).prefab");
            return null;
        }

        private static void LoadLooseLoot(Vector3 postion, Vector3 rotation, string tempalteId)
        {
            Quaternion quat = Quaternion.Euler(rotation);
#pragma warning disable CS4014
            Utils.LoadLoot(postion, quat, tempalteId); //yes, I know this isn't running asnyc
#pragma warning restore CS4014
        }

        public static void DebugZones()
        {
            string targetZone = PluginConfig.TargetZone.Value;
            GameObject gasZone = GameObject.Find(targetZone);
            if (gasZone == null)
            {
                gasZone = new GameObject(targetZone);
                gasZone.transform.position = new Vector3(PluginConfig.test4.Value, PluginConfig.test5.Value, PluginConfig.test6.Value);
                gasZone.transform.rotation = Quaternion.Euler(new Vector3(PluginConfig.test7.Value, PluginConfig.test8.Value, PluginConfig.test9.Value));

                EFT.Interactive.TriggerWithId trigger = gasZone.AddComponent<EFT.Interactive.TriggerWithId>();
                trigger.SetId(targetZone);

                gasZone.layer = LayerMask.NameToLayer("Triggers");
                gasZone.name = targetZone;

                BoxCollider boxCollider = gasZone.AddComponent<BoxCollider>();
                boxCollider.isTrigger = true;
                boxCollider.size = new Vector3(PluginConfig.test1.Value, PluginConfig.test2.Value, PluginConfig.test3.Value);

                // visual representation for debugging
                GameObject visualRepresentation = GameObject.CreatePrimitive(PrimitiveType.Cube);
                visualRepresentation.name = targetZone + "Visual";
                visualRepresentation.transform.parent = gasZone.transform;
                visualRepresentation.transform.localScale = boxCollider.size;
                visualRepresentation.transform.localPosition = boxCollider.center;
                visualRepresentation.transform.rotation = boxCollider.transform.rotation;
                visualRepresentation.GetComponent<Renderer>().material.color = new UnityEngine.Color(1f, 1f, 1f, 0.25f);
                UnityEngine.Object.Destroy(visualRepresentation.GetComponent<Collider>()); // Remove the collider from the visual representation

                Utils.Logger.LogWarning("player pos " + Utils.GetYourPlayer().Transform.position);
                Utils.Logger.LogWarning("gasZone pos " + gasZone.transform.position);
                Utils.Logger.LogWarning("gasZone rot " + gasZone.transform.rotation);
                Utils.Logger.LogWarning("gasZone size " + gasZone.GetComponent<BoxCollider>().size);
            }
            else 
            {
                gasZone.transform.position = new Vector3(PluginConfig.test4.Value, PluginConfig.test5.Value, PluginConfig.test6.Value);
                gasZone.transform.rotation = Quaternion.Euler(new Vector3(PluginConfig.test7.Value, PluginConfig.test8.Value, PluginConfig.test9.Value));
                BoxCollider boxCollider = gasZone.GetComponent<BoxCollider>();
                boxCollider.size = new Vector3(PluginConfig.test1.Value, PluginConfig.test2.Value, PluginConfig.test3.Value);

                GameObject visualRepresentation = GameObject.Find(targetZone + "Visual");
                visualRepresentation.transform.parent = gasZone.transform;
                visualRepresentation.transform.localScale = boxCollider.size;
                visualRepresentation.transform.localPosition = boxCollider.center;
                visualRepresentation.transform.rotation = boxCollider.transform.rotation;
                Utils.Logger.LogWarning("player pos " + Utils.GetYourPlayer().Transform.position);
                Utils.Logger.LogWarning("gasZone pos " + gasZone.transform.position);
                Utils.Logger.LogWarning("gasZone rot " + gasZone.transform.rotation);
                Utils.Logger.LogWarning("gasZone size " + gasZone.GetComponent<BoxCollider>().size);

                /* UnityEngine.Object.Destroy(GameObject.Find("DebugZone"));*/
            }
        }
    }
}
