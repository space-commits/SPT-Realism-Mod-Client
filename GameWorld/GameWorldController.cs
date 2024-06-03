using Aki.Reflection.Patching;
using EFT.CameraControl;
using EFT.InventoryLogic;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using EFT;
using Comfort.Common;
using EFT.Interactive;
using EFT.UI.Ragfair;
using static RootMotion.FinalIK.IKSolver;

namespace RealismMod
{
    public static class GameWorldController
    {
        public static bool GameStarted { get; set; } = false;

        public static Dictionary<string, (float spawnChance, float strength, Vector3 position, Vector3 rotation, Vector3 size)> FactoryGasZones = new Dictionary<string, (float spawnChance, float strength, Vector3, Vector3, Vector3)>
        {
            { "FactoryTent", (0f, 50f, new Vector3(-17.5f, 0.35f, -41.4f), new Vector3(0f, 0f, 0f), new Vector3(6.5f, 4.5f, 38f)) },
            { "FactoryBasement", (0f, 75f, new Vector3(-6f, -3.6f, -20.5f), new Vector3(0f, 0f, 0f), new Vector3(25f, 4.5f, 36f)) },
            { "FactoryTanks", (0f, 50f, new Vector3(7f, -2.5f, 17f), new Vector3(0f, 0f, 0f), new Vector3(35f, 3f, 12f)) },
        };

        public static Dictionary<string, (float spawnChance, float strength, Vector3 position, Vector3 rotation, Vector3 size)> CustomsGasZones = new Dictionary<string, (float spawnChance, float strength, Vector3, Vector3, Vector3)>
        {
            { "CustomsSwitchBasement", (0f, 60f, new Vector3(335f, -3.1f, -60.3f), new Vector3(0f, 0f, 0f), new Vector3(25f, 6.5f, 50f)) },
            { "CustomsZB013", (0f, 20f, new Vector3(199f, -2.8f, -145f), new Vector3(0f, 0f, 0f), new Vector3(12f, 5f, 16f)) },
        };

        public static Dictionary<string, (float spawnChance, float strength, Vector3 position, Vector3 rotation, Vector3 size)> GZGasZones = new Dictionary<string, (float spawnChance, float strength, Vector3, Vector3, Vector3)>
        {
            { "GZUnderground", (-3f, 300f, new Vector3(80f, 15f, -20f), new Vector3(0f, 0f, 0f), new Vector3(50f, 3f, 110f)) },
        };

        public static Dictionary<string, (float spawnChance, float strength, Vector3 position, Vector3 rotation, Vector3 size)> ShorelineGasZones = new Dictionary<string, (float spawnChance, float strength, Vector3, Vector3, Vector3)>
        {
            { "ShorelinePool", (1f, 100f, new Vector3(-185f, -9.4f, -84f), new Vector3(0f, 0f, 0f), new Vector3(100f, 8f, 40f)) },
        };

        public static Dictionary<string, (float spawnChance, float strength, Vector3 position, Vector3 rotation, Vector3 size)> LabsGasZones = new Dictionary<string, (float spawnChance, float strength, Vector3, Vector3, Vector3)>
        {
            { "Labs", (10f, 400f, new Vector3(-193.5f, -4.1f, -342.9f), new Vector3(0f, 0f, 0f), new Vector3(200f, 25f, 200f)) },
        };

        public static Dictionary<string, (float spawnChance, float strength, Vector3 position, Vector3 rotation, Vector3 size)> InterchangeGasZones = new Dictionary<string, (float spawnChance, float strength, Vector3, Vector3, Vector3)>
        {
            { "InterchangeSaferoom", (10f, 5f, new Vector3(-48.4f, 22f, 43.6f), new Vector3(0f, 0f, 0f), new Vector3(10f, 3f, 4.5f)) },
        };

        public static Dictionary<string, (float spawnChance, float strength, Vector3 position, Vector3 rotation, Vector3 size)> ReserveGasZones = new Dictionary<string, (float spawnChance, float strength, Vector3, Vector3, Vector3)>
        {
            { "ReserveStorage", (0f, 500f, new Vector3(50f, -13.5f, -110f), new Vector3(0f, 0f, 0f), new Vector3(90f, 6f, 190f)) },
            { "ReserveShaft", (0f, 50f, new Vector3(-58.9f, -15.9f, 179.9f), new Vector3(0f, 0f, 0f), new Vector3(14f, 55f, 16f)) },
            { "ReserveD2Extract", (0f, 40f, new Vector3(-109f, -18.4f, 161f), new Vector3(0f, 0f, 0f), new Vector3(50f, 5f, 25f)) },
            { "ReserveD2Rat", (0f, 50f, new Vector3(-67f, -18.6f, 141f), new Vector3(0f, 15f, 0f), new Vector3(45f, 4.5f, 15f)) },
            { "ReserveD2Tank", (0f, 50f, new Vector3(-78.5f, -19.8f, 113f), new Vector3(0f, 15f, 0f), new Vector3(55f, 4.5f, 15f)) },
            { "ReserveBunker", (0f, 250f, new Vector3(-105.5f, -14.5f, 42.5f), new Vector3(0f, 9f, 0f), new Vector3(65f, 4.5f, 45f)) }
        };

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

        private static Dictionary<string, (float spawnChance, float strength, Vector3 position, Vector3 rotation, Vector3 size)> GetMapZones(string map) 
        {
            switch (map) 
            {
                case "rezervbase":
                    return ReserveGasZones;
                case "bigmap":
                    return CustomsGasZones;
                case "factory4_night":
                case "factory4_day":
                    return FactoryGasZones;
                case "interchange":
                    return InterchangeGasZones;
                case "laboratory":
                    return LabsGasZones;
                case "shoreline":
                    return ShorelineGasZones;
                case "sandbox":
                    return GZGasZones;
                default: 
                    return null;
            }
        }

        public static void CreateGasZones(string map)
        {
            var zones = GetMapZones(map.ToLower());
            if (zones == null) return;
            foreach (var zone in zones)
            {
                if (UnityEngine.Random.Range(1, 10) + zone.Value.spawnChance < 5f) continue;

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
                GameObject visualRepresentation = GameObject.CreatePrimitive(PrimitiveType.Cube);
                visualRepresentation.transform.parent = gasZone.transform;
                visualRepresentation.transform.localScale = size;
                visualRepresentation.transform.localPosition = boxCollider.center;
                visualRepresentation.GetComponent<Renderer>().material.color = new Color(1f, 1f, 1f, 0.25f);
                UnityEngine.Object.Destroy(visualRepresentation.GetComponent<Collider>()); // Remove the collider from the visual representation
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
        /*    Logger.LogWarning(" =================================== GAME START ===================================");
            Logger.LogWarning(" =================================== Location " + Singleton<GameWorld>.Instance.MainPlayer.Location);
          */
            Plugin.CurrentProfileId = Utils.GetYourPlayer().ProfileId;
            /*      GameWorldController.CreateDebugZone();*/
            GameWorldController.CreateGasZones(Singleton<GameWorld>.Instance.MainPlayer.Location);
            HazardTracker.GetHazardValues(Plugin.CurrentProfileId);
            HazardTracker.ResetTracker();
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
            HazardTracker.ResetTracker();
            HazardTracker.UpdateHazardValues(Plugin.CurrentProfileId);
            HazardTracker.SaveHazardValues();
            HazardTracker.GetHazardValues(Plugin.ServerConfig.profile_id); //update to use PMC id and not potentially scav id
            GameWorldController.GameStarted = false;
        }
    }
}
