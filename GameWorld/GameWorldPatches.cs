using Comfort.Common;
using EFT;
using EFT.Animals;
using EFT.Ballistics;
using SPT.Reflection.Patching;
using System.Reflection;
using UnityEngine;

namespace RealismMod
{


    public class BirdPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(BirdsSpawner).GetMethod("Awake", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPrefix(BirdsSpawner __instance)
        {
            if (Plugin.FikaPresent) return;

            Bird[] birds = __instance.gameObject.GetComponentsInChildren<Bird>();

            foreach (var bird in birds) 
            {
                var col = bird.gameObject.AddComponent<SphereCollider>();
                col.radius = 0.35f;

                var bc = bird.gameObject.AddComponent<BallisticCollider>();
                bc.gameObject.layer = 12;
                bc.TypeOfMaterial = MaterialType.Body;

                var birb = bird.gameObject.AddComponent<Birb>();
                bc.OnHitAction += birb.OnHit;

                // Create a visual representation of the SphereCollider
            /*    GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.transform.SetParent(bird.transform); 
                sphere.transform.localPosition = col.center; 
                sphere.transform.localScale = Vector3.one * col.radius * 2; 
                Renderer sphereRenderer = sphere.GetComponent<Renderer>();
                sphereRenderer.material.color = new Color(1, 0, 0, 1f); 
                sphere.GetComponent<Collider>().enabled = false;*/
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
            Plugin.CurrentProfileId = Utils.GetYourPlayer().ProfileId;
            if (Plugin.ServerConfig.enable_hazard_zones)
            {
                HazardZoneSpawner.CreateZones(Singleton<GameWorld>.Instance.MainPlayer.Location, HazardZoneData.GasZoneLocations);
                HazardZoneSpawner.CreateZones(Singleton<GameWorld>.Instance.MainPlayer.Location, HazardZoneData.RadZoneLocations);
                HazardZoneSpawner.CreateZones(Singleton<GameWorld>.Instance.MainPlayer.Location, HazardZoneData.RadAssetZoneLocations);
                HazardTracker.GetHazardValues(Plugin.CurrentProfileId);
                HazardTracker.ResetTracker();
            }

            HazardZoneSpawner.GameStarted = true;
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

            HazardZoneSpawner.GameStarted = false;
        }
    }
}
