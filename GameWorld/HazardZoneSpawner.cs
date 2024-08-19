using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace RealismMod
{
    public static class HazardZoneSpawner
    {
        public static bool GameStarted { get; set; } = false;

        public static void CreateZones(string map,ZoneCollection collection)
        {
            var zones = HazardZoneLocations.GetZones(collection.ZoneType, map.ToLower());
            if (zones == null) return;
            foreach (var zone in zones)
            {
                if(collection.ZoneType == EZoneType.Gas || collection.ZoneType == EZoneType.GasAssets) CreateZone<GasZone>(zone);
                else CreateZone<RadiationZone>(zone);
            }
        }

        private static bool ShouldSpawnZone(float zoneProbability) 
        {
            if(PluginConfig.ZoneDebug.Value) return true;

            if (!Plugin.FikaPresent) 
            {
                zoneProbability = Mathf.Max(zoneProbability, 0.01f);
                zoneProbability = Mathf.Clamp01(zoneProbability);
                float randomValue = UnityEngine.Random.value;
                return randomValue <= zoneProbability;
            }

            DateTime utcNow = DateTime.UtcNow;
            int seed = utcNow.Year * 1000000 + utcNow.Month * 10000 + utcNow.Day * 100 + utcNow.Hour * 10;
            int finalSeed = seed % 101;
            return finalSeed <= zoneProbability * 100f;    
        }

        public static void CreateZone<T>(HazardLocation zone) where T : MonoBehaviour, IHazardZone
        {
            if (!ShouldSpawnZone(zone.SpawnChance)) return;

            HandleZoneAssets(zone);

            string zoneName = zone.Name;
            Vector3 position = new Vector3(zone.Position.X, zone.Position.Y, zone.Position.Z);
            Vector3 rotation = new Vector3(zone.Rotation.X, zone.Rotation.Y, zone.Rotation.Z);
            Vector3 size = new Vector3(zone.Size.X, zone.Size.Y, zone.Size.Z);
            Vector3 scale = size;

            GameObject hazardZone = new GameObject(zoneName);
            T hazard = hazardZone.AddComponent<T>();

            float strengthModifier = 1f;
            if ((hazard.ZoneType == EZoneType.Gas || hazard.ZoneType == EZoneType.GasAssets) && (!Plugin.FikaPresent && !PluginConfig.ZoneDebug.Value))
            { 
               strengthModifier = UnityEngine.Random.Range(0.95f, 1.3f); 
            } 
            hazard.ZoneStrengthModifier = zone.Strength * strengthModifier;

            hazardZone.transform.position = position;
            hazardZone.transform.rotation = Quaternion.Euler(rotation);

            EFT.Interactive.TriggerWithId trigger = hazardZone.AddComponent<EFT.Interactive.TriggerWithId>();
            trigger.SetId(zoneName);

            EFT.Interactive.ExperienceTrigger questTrigger = hazardZone.AddComponent<EFT.Interactive.ExperienceTrigger>();
            questTrigger.SetId(zoneName);

            hazardZone.layer = LayerMask.NameToLayer("Triggers");
            hazardZone.name = zoneName;

            BoxCollider boxCollider = hazardZone.AddComponent<BoxCollider>();
            boxCollider.isTrigger = true;
            boxCollider.size = size;

            if (zone.BlockNav)
            {
                var navMeshObstacle = hazardZone.AddComponent<NavMeshObstacle>();
                navMeshObstacle.carving = true;
                navMeshObstacle.center = boxCollider.center;
                navMeshObstacle.size = boxCollider.size;
            }

            // visual representation for debugging
            if (PluginConfig.ZoneDebug.Value)
            {
                GameObject visualRepresentation = GameObject.CreatePrimitive(PrimitiveType.Cube);
                visualRepresentation.name = zoneName + "Visual";
                visualRepresentation.transform.parent = hazardZone.transform;
                visualRepresentation.transform.localScale = size;
                visualRepresentation.transform.localPosition = boxCollider.center;
                visualRepresentation.transform.rotation = boxCollider.transform.rotation;
                visualRepresentation.GetComponent<Renderer>().material.color = hazard.ZoneType == EZoneType.Radiation || hazard.ZoneType == EZoneType.RadAssets ?  new Color(0f, 1f, 0f, 0.15f) : new Color(1f, 0f, 0f, 0.15f);
                UnityEngine.Object.Destroy(visualRepresentation.GetComponent<Collider>()); // Remove the collider from the visual representation
            }
        }

        public static void HandleZoneAssets(HazardLocation zone) 
        {
            if (zone.Assets == null || Plugin.FikaPresent) return;
            System.Random rnd = new System.Random();
            foreach (var asset in zone.Assets) 
            {
                if (rnd.Next(101) > asset.Odds) continue;

                if (asset.RandomizeRotation) 
                {
                    asset.Rotation.Y = rnd.Range(0, 360);
                }

                Vector3 position = new Vector3(asset.Position.X, asset.Position.Y, asset.Position.Z);
                Vector3 rotaiton = new Vector3(asset.Rotation.X, asset.Rotation.Y, asset.Rotation.Z);

                if (asset.Type == "asset") UnityEngine.Object.Instantiate(GetAsset(asset.AssetName), position, Quaternion.Euler(rotaiton));
                else if (asset.Type == "loot") LoadLooseLoot(position, rotaiton, asset.AssetName);
            }
        }

        public static UnityEngine.Object GetAsset(string asset) 
        {
            switch (asset)
            {
                case ("bluebox"):
                    return Plugin.BlueBox;
                case ("yellow_barrel"):
                default:
                    return Plugin.GooBarrel;
                    
            }
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
                visualRepresentation.GetComponent<Renderer>().material.color = new Color(1f, 1f, 1f, 0.25f);
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
