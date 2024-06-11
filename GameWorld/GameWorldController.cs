using Aki.Reflection.Patching;
using Comfort.Common;
using EFT;
using System.Reflection;
using UnityEngine;

namespace RealismMod
{
    public static class GameWorldController
    {
        public static bool GameStarted { get; set; } = false;
       
        public static void CreateGasZones(string map)
        {
            var zones = HazardZoneLocations.GetMapZones(map.ToLower());
            if (zones == null) return;
            foreach (var zone in zones)
            {
                if (!Plugin.ZoneDebug.Value && UnityEngine.Random.Range(1, 10) + zone.Value.spawnChance < 5f) continue;

                float strengthModifier = UnityEngine.Random.Range(0.8f, 1.2f);

                string zoneName = zone.Key;
                Vector3 position = zone.Value.position;
                Vector3 rotation = zone.Value.rotation;
                Vector3 size = zone.Value.size;
                Vector3 scale = zone.Value.size;

                GameObject gasZone = new GameObject(zoneName);
                GasZone gas = gasZone.AddComponent<GasZone>();
                gas.GasStrengthModifier = zone.Value.strength * strengthModifier;

                gasZone.transform.position = position;
                gasZone.transform.rotation = Quaternion.Euler(rotation);

                EFT.Interactive.TriggerWithId trigger = gasZone.AddComponent<EFT.Interactive.TriggerWithId>();
                trigger.SetId(zoneName);

                gasZone.layer = LayerMask.NameToLayer("Triggers");
                gasZone.name = zoneName;

                BoxCollider boxCollider = gasZone.AddComponent<BoxCollider>();
                boxCollider.isTrigger = true;
                boxCollider.size = size;

                // visual representation for debugging
                if (Plugin.ZoneDebug.Value) 
                {
                    GameObject visualRepresentation = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    visualRepresentation.transform.parent = gasZone.transform;
                    visualRepresentation.transform.localScale = size;
                    visualRepresentation.transform.localPosition = boxCollider.center;
                    visualRepresentation.transform.localRotation = boxCollider.transform.localRotation;
                    visualRepresentation.GetComponent<Renderer>().material.color = new Color(1f, 1f, 1f, 0.25f);
                    UnityEngine.Object.Destroy(visualRepresentation.GetComponent<Collider>()); // Remove the collider from the visual representation
                }
            }
        }


        public static void CreateDebugZone()
        {
            UnityEngine.Object.Destroy(GameObject.Find("DebugZone"));
            GameObject gasZone = new GameObject("DebugZone");
            gasZone.transform.position = new Vector3(Plugin.test4.Value, Plugin.test5.Value, Plugin.test6.Value);
            gasZone.transform.rotation = Quaternion.Euler(new Vector3(Plugin.test7.Value, Plugin.test8.Value, Plugin.test9.Value));

            EFT.Interactive.TriggerWithId trigger = gasZone.AddComponent<EFT.Interactive.TriggerWithId>();
            trigger.SetId("DebugZone");

            gasZone.layer = LayerMask.NameToLayer("Triggers");
            gasZone.name = "DebugZone";

            BoxCollider boxCollider = gasZone.AddComponent<BoxCollider>();
            boxCollider.isTrigger = true;
            boxCollider.size = new Vector3(Plugin.test1.Value, Plugin.test2.Value, Plugin.test3.Value);

            // visual representation for debugging
            GameObject visualRepresentation = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visualRepresentation.name = "DebugZoneVisual";
            visualRepresentation.transform.parent = gasZone.transform;
            visualRepresentation.transform.localScale = boxCollider.size;
            visualRepresentation.transform.localPosition = boxCollider.center;
            visualRepresentation.transform.localRotation = boxCollider.transform.localRotation;
            visualRepresentation.GetComponent<Renderer>().material.color = new Color(1f, 1f, 1f, 0.25f);
            UnityEngine.Object.Destroy(visualRepresentation.GetComponent<Collider>()); // Remove the collider from the visual representation

            Utils.Logger.LogWarning("player pos " + Utils.GetYourPlayer().Transform.position);
            Utils.Logger.LogWarning("gasZone pos " + gasZone.transform.position);
            Utils.Logger.LogWarning("gasZone rot " + gasZone.transform.rotation);
            Utils.Logger.LogWarning("gasZone size " + gasZone.GetComponent<BoxCollider>().size);
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
            if (Plugin.ServerConfig.med_changes) 
            {
                GameWorldController.CreateGasZones(Singleton<GameWorld>.Instance.MainPlayer.Location);
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
            if (Plugin.ServerConfig.med_changes) 
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
