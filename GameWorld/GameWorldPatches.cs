using Comfort.Common;
using EFT;
using EFT.Animals;
using EFT.Ballistics;
using EFT.Communications;
using EFT.UI;
using Sirenix.Serialization;
using SPT.Reflection.Patching;
using System;
using System.Drawing;
using System.Linq;
using System.Reflection;
using UnityEngine;
using QuestUIClass = GClass2046;
using Color = UnityEngine.Color;
using EFT.InventoryLogic;

namespace RealismMod
{
    class DropItemPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GameWorld).GetMethod("ThrowItem", new Type[] { typeof(Item), typeof(IPlayer), typeof(Vector3), typeof(Quaternion), typeof(Vector3), typeof(Vector3), typeof(bool), typeof(bool), typeof(float) });
        }

        [PatchPostfix]
        private static void PatchPostfix(ref EFT.Interactive.LootItem __result, IPlayer player)
        {


            bool isGamu = __result.Item.TemplateId == Utils.GAMU_ID;
            bool isRamu = __result.Item.TemplateId == Utils.RAMU_ID;
            if (isGamu || isRamu) 
            {
                HazardAnalyser analyser = __result.gameObject.AddComponent<HazardAnalyser>();
                analyser._Player = Utils.GetPlayerByProfileId(player.ProfileId);
                analyser._LootItem = __result;
                analyser.TargetZoneType = isGamu ? EZoneType.Gas : EZoneType.Radiation;
                BoxCollider collider = analyser.gameObject.AddComponent<BoxCollider>();
                collider.isTrigger = true;
                collider.size = new Vector3(0.1f, 0.1f, 0.1f);
                

                if (PluginConfig.ZoneDebug.Value)
                {
                    GameObject visualRepresentation = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    visualRepresentation.name = "ItemVisual";
                    visualRepresentation.transform.parent = collider.transform;
                    visualRepresentation.transform.localScale = collider.size;
                    visualRepresentation.transform.localPosition = collider.center;
                    visualRepresentation.transform.rotation = collider.transform.rotation;
                    visualRepresentation.GetComponent<Renderer>().material.color = Color.green;
                    UnityEngine.Object.Destroy(visualRepresentation.GetComponent<Collider>()); 
                }
            }
        }
    }

    //makes culstists spawn during day time

    class DayTimeSpawnPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(ZoneLeaveControllerClass).GetMethod("IsDayByHour");
        }
        [PatchPrefix]
        private static bool PatchPrefix(ref bool __result)
        {
            if (!GameWorldController.RanEarlyGameCheck)
            {
                Plugin.RequestRealismDataFromServer(false, true);
                GameWorldController.RanEarlyGameCheck = true;
            }
            if (GameWorldController.DoMapGasEvent)
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
        private static void PatchPostfix(BirdsSpawner __instance)
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

                if (PluginConfig.ZoneDebug.Value)
                {
                    GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    sphere.transform.SetParent(bird.transform);
                    sphere.transform.localPosition = col.center;
                    sphere.transform.localScale = Vector3.one * col.radius * 2;
                    Renderer sphereRenderer = sphere.GetComponent<Renderer>();
                    sphereRenderer.material.color = new Color(1, 0, 0, 1f);
                    sphere.GetComponent<Collider>().enabled = false;
                }
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
                //update tracked map info
                GameWorldController.CurrentMap = Singleton<GameWorld>.Instance.MainPlayer.Location.ToLower();
                GameWorldController.MapWithDynamicWeather = GameWorldController.CurrentMap.Contains("factory") || GameWorldController.CurrentMap == "laboratory" ? false : true;

                //audio components
                AudioController.CreateAudioComponent();
                if (GameWorldController.DoMapGasEvent) 
                {
                    Player player = Utils.GetYourPlayer();
                    ZoneSpawner.CreateAmbientAudioPlayers(player.gameObject.transform, Plugin.GasEventAudioClips, volume: 1.15f);
                    ZoneSpawner.CreateAmbientAudioPlayers(player.gameObject.transform, Plugin.GasEventLongAudioClips, true, 14f, 60f, 0.15f, minDistance: 65, maxDistance: 105f);
                }

                //spawn zones
                ZoneSpawner.CreateZones(ZoneData.GasZoneLocations);
                ZoneSpawner.CreateZones(ZoneData.RadZoneLocations);
                if (ZoneSpawner.ShouldSpawnDynamicZones()) ZoneSpawner.CreateZones(ZoneData.RadAssetZoneLocations);
                ZoneSpawner.CreateZones(ZoneData.SafeZoneLocations);
               
                //hazardtracker 
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
            GameWorldController.RanEarlyGameCheck = false;
        }
    }
}
