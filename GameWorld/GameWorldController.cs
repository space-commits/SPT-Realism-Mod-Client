using SPT.Reflection.Patching;
using SPT.Reflection.Utils;
using Comfort.Common;
using EFT;
using EFT.Interactive;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace RealismMod
{
    public static class GameWorldController
    {
        public static bool GameStarted { get; set; } = false;

        public static void CreateZones(string map)
        {
            var gasZones = HazardZoneLocations.GetGasZones(map.ToLower());
            if (gasZones == null) return;
            foreach (var zone in gasZones)
            {
                CreateZone<GasZone>(zone);
            }

            var radZones = HazardZoneLocations.GetRadZones(map.ToLower());
            if (radZones == null) return;
            foreach (var zone in radZones)
            {
                CreateZone<RadiationZone>(zone);
            }
        }

        private static bool ShouldSpawnZone(float zoneProbability) 
        {
            if(Plugin.ZoneDebug.Value) return true;

            if (!Plugin.IsUsingFika) 
            {
                zoneProbability = Mathf.Max(zoneProbability, 0.1f);
                zoneProbability = Mathf.Clamp01(zoneProbability);
                float randomValue = UnityEngine.Random.value;
                return randomValue <= zoneProbability;
            }

            DateTime utcNow = DateTime.UtcNow;
            int seed = utcNow.Year * 1000000 + utcNow.Month * 10000 + utcNow.Day * 100 + utcNow.Hour * 10;
            int finalSeed = seed % 101;
            return finalSeed <= zoneProbability * 100f;    
        }

        public static void CreateZone<T>(KeyValuePair<string, (float spawnChance, float strength, Vector3 position, Vector3 rotation, Vector3 size)> zone) where T : MonoBehaviour, IHazardZone
        {
            if (!ShouldSpawnZone(zone.Value.spawnChance)) return;

            string zoneName = zone.Key;
            Vector3 position = zone.Value.position;
            Vector3 rotation = zone.Value.rotation;
            Vector3 size = zone.Value.size;
            Vector3 scale = zone.Value.size;

            GameObject hazardZone = new GameObject(zoneName);
            T hazard = hazardZone.AddComponent<T>();

            float strengthModifier = 1f;
            if (hazard.ZoneType == EZoneType.Toxic)
            { 
               strengthModifier = Plugin.IsUsingFika || Plugin.ZoneDebug.Value ? 1f : UnityEngine.Random.Range(0.9f, 1.25f); 
            } 
            hazard.ZoneStrengthModifier = zone.Value.strength * strengthModifier;

            hazardZone.transform.position = position;
            hazardZone.transform.rotation = Quaternion.Euler(rotation);

            EFT.Interactive.TriggerWithId trigger = hazardZone.AddComponent<EFT.Interactive.TriggerWithId>();
            trigger.SetId(zoneName);

            hazardZone.layer = LayerMask.NameToLayer("Triggers");
            hazardZone.name = zoneName;

            BoxCollider boxCollider = hazardZone.AddComponent<BoxCollider>();
            boxCollider.isTrigger = true;
            boxCollider.size = size;

            // visual representation for debugging
            if (Plugin.ZoneDebug.Value)
            {
                GameObject visualRepresentation = GameObject.CreatePrimitive(PrimitiveType.Cube);
                visualRepresentation.name = zoneName + "Visual";
                visualRepresentation.transform.parent = hazardZone.transform;
                visualRepresentation.transform.localScale = size;
                visualRepresentation.transform.localPosition = boxCollider.center;
                visualRepresentation.transform.rotation = boxCollider.transform.rotation;
                visualRepresentation.GetComponent<Renderer>().material.color = new Color(1f, 1f, 1f, 0.25f);
                UnityEngine.Object.Destroy(visualRepresentation.GetComponent<Collider>()); // Remove the collider from the visual representation
            }
        }

        public static void DebugZones()
        {
            string targetZone = Plugin.TargetZone.Value;
            GameObject gasZone = GameObject.Find(targetZone);
            if (gasZone == null)
            {
                gasZone = new GameObject(targetZone);
                gasZone.transform.position = new Vector3(Plugin.test4.Value, Plugin.test5.Value, Plugin.test6.Value);
                gasZone.transform.rotation = Quaternion.Euler(new Vector3(Plugin.test7.Value, Plugin.test8.Value, Plugin.test9.Value));

                EFT.Interactive.TriggerWithId trigger = gasZone.AddComponent<EFT.Interactive.TriggerWithId>();
                trigger.SetId(targetZone);

                gasZone.layer = LayerMask.NameToLayer("Triggers");
                gasZone.name = targetZone;

                BoxCollider boxCollider = gasZone.AddComponent<BoxCollider>();
                boxCollider.isTrigger = true;
                boxCollider.size = new Vector3(Plugin.test1.Value, Plugin.test2.Value, Plugin.test3.Value);

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
                gasZone.transform.position = new Vector3(Plugin.test4.Value, Plugin.test5.Value, Plugin.test6.Value);
                gasZone.transform.rotation = Quaternion.Euler(new Vector3(Plugin.test7.Value, Plugin.test8.Value, Plugin.test9.Value));
                BoxCollider boxCollider = gasZone.GetComponent<BoxCollider>();
                boxCollider.size = new Vector3(Plugin.test1.Value, Plugin.test2.Value, Plugin.test3.Value);

                GameObject visualRepresentation = GameObject.Find(targetZone + "Visual");
                visualRepresentation.transform.parent = gasZone.transform;
                visualRepresentation.transform.localScale = boxCollider.size;
                visualRepresentation.transform.localPosition = boxCollider.center;
                visualRepresentation.transform.rotation = boxCollider.transform.rotation;
                Utils.Logger.LogWarning("player pos " + Utils.GetYourPlayer().Transform.position);
                Utils.Logger.LogWarning("gasZone pos " + gasZone.transform.position);
                Utils.Logger.LogWarning("gasZone rot " + gasZone.transform.rotation);
                Utils.Logger.LogWarning("gasZone size " + gasZone.GetComponent<BoxCollider>().size);

                /* UnityEngine.Object.Destroy(GameObject.Find("DebugZone"));
     */



            }
        }
    }

    public class OnGameStartPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GameWorld).GetMethod("OnGameStarted", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(GameWorld __instance)
        {   
            /*WeatherController.Instance.WindController.CloudWindMultiplier = 1;*/
            /*GameWorldController.CreateDebugZone();*/

            Plugin.CurrentProfileId = Utils.GetYourPlayer().ProfileId;
            if (Plugin.ServerConfig.enable_hazard_zones) 
            {
                GameWorldController.CreateZones(Singleton<GameWorld>.Instance.MainPlayer.Location);
                HazardTracker.GetHazardValues(Plugin.CurrentProfileId);
                HazardTracker.ResetTracker();
            }

            GameWorldController.GameStarted = true;
        }
    }

    public class OnGameEndPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GameWorld).GetMethod("OnDestroy", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static void PatchPrefix(GameWorld __instance)
        {
            if (Plugin.ServerConfig.enable_hazard_zones) 
            {
                HazardTracker.ResetTracker();
                HazardTracker.UpdateHazardValues(Plugin.CurrentProfileId);
                HazardTracker.SaveHazardValues();
                HazardTracker.GetHazardValues(Plugin.PMCProfileId); //update to use PMC id and not potentially scav id
            }

            GameWorldController.GameStarted = false;
        }
    }
}
