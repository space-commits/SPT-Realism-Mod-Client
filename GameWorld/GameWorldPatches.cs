using Audio.AmbientSubsystem;
using Comfort.Common;
using EFT;
using EFT.Animals;
using EFT.Ballistics;
using EFT.Communications;
using EFT.UI;
using SPT.Reflection.Patching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using QuestUIClass = GClass2046;


namespace RealismMod
{
    //cult
    class DayTimeSpawnPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(ZoneLeaveControllerClass).GetMethod("IsDayByHour");
        }
        [PatchPrefix]
        private static bool PatchPrefix(ref bool __result)
        {
            int rnd = UnityEngine.Random.Range(0, 101);
            if (rnd <= 5 || GameWorldController.IsHalloween) 
            {
                __result = false;
                return false;
            }
            return true;
        }
    }

    public class QuestCompletePatch : ModulePatch
    {
        private static string[] _hazardHealQuests = { "667c643869df8111b81cb6dc", "667dbbc9c62a7c2ee8fe25b2" };

        protected override MethodBase GetTargetMethod()
        {
            return typeof(QuestView).GetMethod("FinishQuest", BindingFlags.Instance | BindingFlags.Public, null, new Type[0], null);
        }

        [PatchPostfix]
        private static void PatchPostfix(QuestView __instance)
        {
            if (_hazardHealQuests.Contains(__instance.QuestId))
            {
                HazardTracker.TotalRadiation = 0;
                HazardTracker.TotalToxicity = 0;
                HazardTracker.UpdateHazardValues(ProfileData.PMCProfileId);
                HazardTracker.UpdateHazardValues(ProfileData.ScavProfileId);
                HazardTracker.SaveHazardValues();
                if (PluginConfig.EnableMedNotes.Value) NotificationManagerClass.DisplayNotification(new QuestUIClass("Blood Tests Came Back Clear, Your Radiation Poisoning Has Been Cured.".Localized(null), ENotificationDurationType.Long, ENotificationIconType.Quest, null));
            }
        }
    }

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
            ProfileData.CurrentProfileId = Utils.GetYourPlayer().ProfileId;
            if (Plugin.ServerConfig.enable_hazard_zones)
            {
                GameWorldController.CurrentMap = Singleton<GameWorld>.Instance.MainPlayer.Location.ToLower();
                GameWorldController.MapWithDynamicWeather = GameWorldController.CurrentMap.Contains("factory") || GameWorldController.CurrentMap == "laboratory" ? false : true;
                ZoneSpawner.CreateZones(ZoneData.GasZoneLocations);
                ZoneSpawner.CreateZones(ZoneData.RadZoneLocations);
                if (ZoneSpawner.ShouldSpawnDynamicZones()) ZoneSpawner.CreateZones(ZoneData.RadAssetZoneLocations);
                ZoneSpawner.CreateZones(ZoneData.SafeZoneLocations);
                if (GameWorldController.DoMapGasEvent) ZoneSpawner.CreateAmbientAudioPlayers(); // make it take an argument for specific ambient audio zones? dynamically create a dictionary of ambient audio zones?
                HazardTracker.GetHazardValues(ProfileData.CurrentProfileId);
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
                var sessionData = Singleton<ClientApplication<ISession>>.Instance.GetClientBackEndSession();
                ProfileData.PMCLevel = sessionData.Profile.Info.Level;
                HazardTracker.ResetTracker();
                HazardTracker.UpdateHazardValues(ProfileData.CurrentProfileId);
                HazardTracker.SaveHazardValues();
                HazardTracker.GetHazardValues(ProfileData.PMCProfileId); //update to use PMC id and not potentially scav id
            }

            GameWorldController.GameStarted = false;
        }
    }
}
