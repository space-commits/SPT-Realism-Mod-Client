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

        //position, rotation, size
        public static Dictionary<string, (Vector3 position, Vector3 rotation, Vector3 size)> GasZones = new Dictionary<string, (Vector3, Vector3, Vector3)>
        {
            { "FactortTest1", (new Vector3(-17.5f, 0.2f, -41.4f), new Vector3(0f, 0f, 0f), new Vector3(10f, 5f, 10f)) },
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

                BoxCollider boxCollider = gasZone.AddComponent<BoxCollider>();
                boxCollider.isTrigger = true;
                boxCollider.size = size;

                gasZone.transform.position = position; 
          
                EFT.Interactive.TriggerWithId trigger = gasZone.AddComponent<EFT.Interactive.TriggerWithId>();
                trigger.SetId("gasZone1");

                gasZone.layer = LayerMask.NameToLayer("Triggers");
                gasZone.name = "gasZone1";

                /*
                                string zoneName = zone.Key;
                                Vector3 position = zone.Value.position;
                                Vector3 rotation = zone.Value.rotation;
                                Vector3 size = zone.Value.size;

                                // Create a new GameObject
                                GameObject gasZone = new GameObject(zoneName);

                                // Set the position and rotation of the GameObject
                                gasZone.transform.position = position;
                                gasZone.transform.rotation = Quaternion.Euler(rotation);

                                // Add a BoxCollider component and set it as a trigger
                                BoxCollider boxCollider = gasZone.AddComponent<BoxCollider>();
                                boxCollider.isTrigger = true;
                                boxCollider.size = size;
                                boxCollider.center = Vector3.zero; // Center the collider at the GameObject's position

                                // Attach the RadiationZone component
                                GasZone gas = gasZone.AddComponent<GasZone>();

                          *//*      // Add a Rigidbody component to ensure trigger detection
                                Rigidbody rb = gasZone.AddComponent<Rigidbody>();
                                rb.isKinematic = true; // Prevent physics interactions*/

                /*     gasZone.layer = Utils.GetYourPlayer().gameObject.layer;*/
                /*  gasZone.layer = LayerMask.NameToLayer("Triggers");*/

                // Optional: Add a visual representation for debugging
                GameObject visualRepresentation = GameObject.CreatePrimitive(PrimitiveType.Cube);
                visualRepresentation.transform.parent = gasZone.transform;
                visualRepresentation.transform.localScale = size;
                visualRepresentation.transform.localPosition = boxCollider.center;
                visualRepresentation.GetComponent<Renderer>().material.color = new Color(1f, 1f, 1f, 0.25f); // Set a transparent color
                UnityEngine.Object.Destroy(visualRepresentation.GetComponent<Collider>()); // Remove the collider from the visual representation

            }
        }


        public static void CreateGasZone()
        {
            // Create a new GameObject
            GameObject gasZone = new GameObject("GasZone");

            // Set the position of the GameObject in the scene
            gasZone.transform.position = new Vector3(Plugin.test1.Value, Plugin.test2.Value, Plugin.test3.Value);
            gasZone.transform.rotation = Quaternion.Euler(new Vector3(Plugin.test4.Value, Plugin.test5.Value, Plugin.test6.Value));

            // Add a BoxCollider component to the GameObject
            BoxCollider boxCollider = gasZone.AddComponent<BoxCollider>();
            boxCollider.isTrigger = true;

            // Set the size of the BoxCollider
            boxCollider.size = new Vector3(Plugin.test7.Value, Plugin.test8.Value, Plugin.test9.Value);

            // Set the position of the BoxCollider (center of the BoxCollider relative to the GameObject's position)
            boxCollider.center = Vector3.zero; // Center the collider at the GameObject's position

            // Optional: Add a visual representation (a cube) to the GameObject for debugging
            GameObject visualRepresentation = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visualRepresentation.transform.parent = gasZone.transform;
            visualRepresentation.transform.localScale = boxCollider.size;
            visualRepresentation.transform.localPosition = boxCollider.center;
            visualRepresentation.GetComponent<Renderer>().material.color = new Color(1f, 1f, 1f, 0.25f); 
            UnityEngine.Object.Destroy(visualRepresentation.GetComponent<Collider>());
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
