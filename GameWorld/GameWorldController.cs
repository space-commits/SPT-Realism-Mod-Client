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

namespace RealismMod
{
    public static class GameWorldController
    {

        public static Dictionary<string, (float strength, Vector3 position, Vector3 rotation, Vector3 size)> GasZones = new Dictionary<string, (float strength, Vector3, Vector3, Vector3)>
        {
            { "FactoryTent", (100f, new Vector3(-17.5f, 0.35f, -41.4f), new Vector3(0f, 0f, 0f), new Vector3(10f, 3f, 20f)) },
        };


        public static void CreateGasZones()
        {
            foreach (var zone in GasZones)
            {
                string zoneName = zone.Key;
                Vector3 position = zone.Value.position;
                Vector3 rotation = zone.Value.rotation;
                Vector3 size = zone.Value.size;
                Vector3 scale = zone.Value.size;

                GameObject gasZone = new GameObject(zoneName);
                GasZone gas = gasZone.AddComponent<GasZone>();
                gas.GasStrengthModifier = zone.Value.strength;

                BoxCollider boxCollider = gasZone.AddComponent<BoxCollider>();
                boxCollider.isTrigger = true;
                boxCollider.size = size;

                gasZone.transform.position = position; 
          
                EFT.Interactive.TriggerWithId trigger = gasZone.AddComponent<EFT.Interactive.TriggerWithId>();
                trigger.SetId(zoneName);

                gasZone.layer = LayerMask.NameToLayer("Triggers");
                gasZone.name = zoneName;

                // Optional: Add a visual representation for debugging
                GameObject visualRepresentation = GameObject.CreatePrimitive(PrimitiveType.Cube);
                visualRepresentation.transform.parent = gasZone.transform;
                visualRepresentation.transform.localScale = size;
                visualRepresentation.transform.localPosition = boxCollider.center;
                visualRepresentation.GetComponent<Renderer>().material.color = new Color(1f, 1f, 1f, 0.25f); // Set a transparent color
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
            Logger.LogWarning(" =================================== GAME START ===================================");
            GameWorldController.CreateGasZones();
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
            Logger.LogWarning(" =================================== GAME END ===================================");
            Logger.LogWarning("player " + Utils.GetYourPlayer().ProfileId);
        }
    }
}
