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
using HarmonyLib;
using EFT.Interactive;
using static RootMotion.FinalIK.InteractionTrigger.Range;
using System.Collections.Generic;
using static UnityEngine.Rendering.PostProcessing.HistogramMonitor;
using static UnityEngine.UI.Selectable;
using System.Threading.Tasks;
using static RootMotion.FinalIK.GenericPoser;

namespace RealismMod
{
    //for events I need to dynamically change boss spawn chance, but the point at which the event is declared server-side is too late for changing boss spawns
    public class BossSpawnPatch : ModulePatch 
    {
        //no good way to know what map we're currently on at this poin in the raid loading, it is what it is.
        private static string[] _forbiddenZones = { "BotZoneFloor1", "BotZoneFloor2", "BotZoneBasement", "BotZone" };

        protected override MethodBase GetTargetMethod()
        {
            return typeof(BossLocationSpawn).GetMethod("ParseMainTypesTypes");
        }

        [PatchPostfix]
        public static void PatchPostfix(BossLocationSpawn __instance)
        {
            Logger.LogWarning("1");
            if (!GameWorldController.RanEarliestGameCheck)
            {
                Logger.LogWarning("Rolling chance boss spawn patch");
                Plugin.RequestRealismDataFromServer(false, true);
                GameWorldController.RanEarliestGameCheck = true;
            }

            Logger.LogWarning("2");
            var zones = __instance.BossZone.Split(new char[]{','});
            if (_forbiddenZones.Intersect(zones).Any()) return;
            Logger.LogWarning("3");
            bool increaseSectantChance = __instance.BossType == WildSpawnType.sectantPriest && Plugin.ModInfo.DoGasEvent;
            bool increaseRaiderChance = __instance.BossType == WildSpawnType.pmcBot && Plugin.ModInfo.DoExtraRaiders;
            bool isPmc = __instance.BossType == WildSpawnType.pmcBEAR || __instance.BossType == WildSpawnType.pmcUSEC;
            bool postExpl = !isPmc && Plugin.ModInfo.IsHalloween && (Plugin.ModInfo.HasExploded || GameWorldController.DidExplosionClientSide);
            if (increaseSectantChance) 
            {
                bool doExtraCultists = Plugin.ModInfo.DoExtraCultists;
                __instance.BossChance = __instance.BossChance == 0 && !doExtraCultists ? 50f : 100f;
                __instance.ShallSpawn = true;
            }
            if (increaseRaiderChance) 
            {
                __instance.BossChance = 100f;
                __instance.ShallSpawn = true;
            }
            if ((postExpl ||Plugin.ModInfo.DoGasEvent) && (__instance.BossType != WildSpawnType.sectantPriest && __instance.BossType != WildSpawnType.pmcBot && !isPmc && !Plugin.ModInfo.DoExtraRaiders))
            {
                __instance.BossChance *= 0.1f;
                __instance.ShallSpawn = GClass761.IsTrue100(__instance.BossChance);
            }

            Logger.LogWarning($"=============");
            Logger.LogWarning($"Do Gas Event ? {Plugin.ModInfo.DoGasEvent}");
            Logger.LogWarning($"Do raider Event ? {Plugin.ModInfo.DoExtraRaiders}");
            Logger.LogWarning($"Do extra cultists ? {Plugin.ModInfo.DoExtraCultists}");
            Logger.LogWarning("Boss type " + __instance.BossType);
            Logger.LogWarning("Boss type " + __instance.BossType);
            Logger.LogWarning("Spawn Chance " + __instance.BossChance);
            Logger.LogWarning("Shall Spawn" + __instance.ShallSpawn);
            Logger.LogWarning("=============");
        }
    }


    public class GetAvailableActionsPatch : ModulePatch
    {
        public static void DummyAction() { }

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.FirstMethod(typeof(GetActionsClass), x => x.Name == nameof(GetActionsClass.GetAvailableActions) && x.GetParameters()[0].Name == "owner");
        }

        [PatchPrefix]
        public static bool PatchPrefix(object[] __args, ref ActionsReturnClass __result)
        {
            // __args[1] is a GInterface called "interactive", it represents the component that enables interaction
            if (__args[1] is InteractableComponent)
            {
                var customInteractable = __args[1] as InteractableComponent;

                __result = new ActionsReturnClass()
                {
                    Actions = customInteractable.Actions
                };
                return false;

            }
            return true;
        }

        [PatchPostfix]
        public static void PatchPostfix(object[] __args, ActionsReturnClass __result)
        {
            if (__result != null && __result.Actions != null && __args != null && __args.Count() > 0)
            {
                LootItem lootItem;
                if ((lootItem = (__args[1] as LootItem)) != null)
                {
                    if(lootItem.TemplateId == Utils.GAMU_ID || lootItem.TemplateId == Utils.RAMU_ID)
                    {                                       
                        if (lootItem.gameObject.TryGetComponent<HazardAnalyser>(out HazardAnalyser analyser)) 
                        {
                            bool hasBeenAnalysed = analyser.TargetZone != null && analyser.TargetZone.HasBeenAnalysed;
                            bool alreadyHasDevice = analyser.ZoneAlreadyHasDevice();
                            if (analyser.CanTurnOn && !hasBeenAnalysed && !alreadyHasDevice) 
                            {
                                __result.Actions.AddRange(analyser.Actions);
                            }
                        }
                    }

                    if (lootItem.TemplateId == Utils.HALLOWEEN_TRANSMITTER_ID)
                    {
                        if (lootItem.gameObject.TryGetComponent<TransmitterHalloweenEvent>(out TransmitterHalloweenEvent transmitter))
                        {
                            if (transmitter.TriggeredExplosion) 
                            {
                                __result.Actions = new List<ActionsTypesClass>() { new ActionsTypesClass { Name = "", Action = DummyAction } };
                            }
                            else if (transmitter.CanTurnOn)
                            {
                                __result.Actions.AddRange(transmitter.Actions);
                            }
                        }
                    }

                }
            }
        }
    }

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
            bool isHalloweenTransmitter = __result.Item.TemplateId == Utils.HALLOWEEN_TRANSMITTER_ID;
            if (isGamu || isRamu) 
            {
                //when the item is picked up, the old componment is not destroyed because BSG persists the LootItem gameobject at least for some time before GC...
                if (__result.gameObject.TryGetComponent<HazardAnalyser>(out HazardAnalyser oldAnalyser)) 
                {
                    UnityEngine.Object.Destroy(oldAnalyser);
                }

                HazardAnalyser analyser = __result.gameObject.AddComponent<HazardAnalyser>();
                analyser._IPlayer = player; 
                analyser._Player = Utils.GetPlayerByProfileId(player.ProfileId);
                analyser._LootItem = __result;
                analyser.TargetZoneType = isGamu ? EZoneType.Gas : EZoneType.Radiation;
                BoxCollider collider = analyser.gameObject.AddComponent<BoxCollider>();
                collider.isTrigger = true;
                collider.size = new Vector3(0.1f, 0.1f, 0.1f);
            }
            if (isHalloweenTransmitter) 
            {
                if (__result.gameObject.TryGetComponent<TransmitterHalloweenEvent>(out TransmitterHalloweenEvent oldTransmitter))
                {
                    UnityEngine.Object.Destroy(oldTransmitter);
                }
                TransmitterHalloweenEvent transmitter = __result.gameObject.AddComponent<TransmitterHalloweenEvent>();
                transmitter._IPlayer = player;
                transmitter._Player = Utils.GetPlayerByProfileId(player.ProfileId);
                transmitter._LootItem = __result;
                transmitter.TargetQuestZones = new string[] { "SateliteCommLink" };
                transmitter.QuestTrigger = "SateliteCommLinkEstablished";
                BoxCollider collider = transmitter.gameObject.AddComponent<BoxCollider>();
                collider.isTrigger = true;
                collider.size = new Vector3(0.1f, 0.1f, 0.1f);
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
            Logger.LogWarning(">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>DAY TIME CHECK");
            if (Plugin.ModInfo.DoGasEvent)
            {
                __result = false;
                return false;
            }
            return true;
        }
    }

    public class QuestCompletePatch : ModulePatch
    {
        private static string[] _hazardHealQuests = { "667c643869df8111b81cb6dc", "667dbbc9c62a7c2ee8fe25b2", "6705425a0351f9f55b7d8c61" };

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
            Logger.LogWarning("--------------------------BIRD CHECK");
            //not remotely ideal but this method is called the earliest so far, but not this is not always called so will call elsewhere too.
            if (!GameWorldController.RanEarliestGameCheck)
            {
                Plugin.RequestRealismDataFromServer(false, true);
                GameWorldController.RanEarliestGameCheck = true;
            }

            Logger.LogWarning("--------------------------Done Bird Check");
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
                GameWorldController.IsMapThatCanDoGasEvent = GameWorldController.CurrentMap != "laboratory" && !GameWorldController.CurrentMap.Contains("factory");
                GameWorldController.IsMapThatCanDoRadEvent = GameWorldController.CurrentMap != "laboratory";

                //audio components
                AudioController.CreateAudioComponent();
                if (GameWorldController.DoMapGasEvent) 
                {
                    Player player = Utils.GetYourPlayer();
                    ZoneSpawner.CreateAmbientAudioPlayers(player.gameObject.transform, Plugin.GasEventAudioClips, volume: 1.15f);
                    ZoneSpawner.CreateAmbientAudioPlayers(player.gameObject.transform, Plugin.GasEventLongAudioClips, true, 14f, 60f, 0.35f, 50f, 90f);
                }

                //spawn zones
                ZoneSpawner.CreateZones(ZoneData.GasZoneLocations);
                ZoneSpawner.CreateZones(ZoneData.RadZoneLocations);
                if (ZoneSpawner.ShouldSpawnDynamicZones()) ZoneSpawner.CreateZones(ZoneData.RadAssetZoneLocations);
                ZoneSpawner.CreateZones(ZoneData.SafeZoneLocations);
                ZoneSpawner.CreateZones(ZoneData.QuestZoneLocations);
               
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
            GameWorldController.RanEarliestGameCheck = false;
        }
    }
}
