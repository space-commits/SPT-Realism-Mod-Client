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
using QuestUIClass = GClass2269;
using ChanceCalcClass = GClass824;
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
using Audio.AmbientSubsystem;
using static BotsPresets;

namespace RealismMod
{
    /*  public class BotPatch1 : ModulePatch
      {
          protected override MethodBase GetTargetMethod()
          {
              return typeof(Struct67).GetMethod("MoveNext");
          }

          [PatchPrefix]
          public static void PatchPrefix()
          {
              Logger.LogWarning("struct 67");
          }
      }

      public class BotPatch2 : ModulePatch
      {
          protected override MethodBase GetTargetMethod()
          {
              return typeof(Struct70).GetMethod("MoveNext");
          }

          [PatchPrefix]
          public static void PatchPrefix()
          {
              Logger.LogWarning("struct 70");
          }
      }

      public class BotPatch3 : ModulePatch
      {
          protected override MethodBase GetTargetMethod()
          {
              return typeof(Struct69).GetMethod("MoveNext");
          }

          [PatchPrefix]
          public static void PatchPrefix()
          {
              Logger.LogWarning("struct 69");
          }
      }

      public class BotPatch4 : ModulePatch
      {
          protected override MethodBase GetTargetMethod()
          {
              return typeof(Struct68).GetMethod("MoveNext");
          }

          [PatchPrefix]
          public static bool PatchPrefix()
          {
              Logger.LogWarning("struct 68");
              if (PluginConfig.test1.Value > 10f) return false;
              return true;
          }
      }*/

    //attempt to prevent stutter when game needlessly generates new bot waves
    public class SpawnUpdatePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(NonWavesSpawnScenario).GetMethod("Update");
        }

        [PatchPrefix]
        public static bool PatchPrefix(NonWavesSpawnScenario __instance)
        {
            if (GameWorldController.TimeInRaid >= 180f)
            {
                return false;
            }
            return true;

        }
    }

    public class RigidLootSpawnPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GameWorld).GetMethod("CreateLootWithRigidbody");
        }

        [PatchPostfix]
        public static void PatchPostfix(Item item)
        {
            GameWorldController.ModifyLootResources(item);
        }
    }

    public class StaticLootSpawnPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GameWorld).GetMethod("CreateStaticLoot");
        }

        [PatchPostfix]
        public static void PatchPostfix(Item item)
        {
            GameWorldController.ModifyLootResources(item);
        }
    }

    class DayTimeAmbientPatch : ModulePatch
    {
/*        private static FieldInfo _dayAudioSourceField;
        private static FieldInfo _nightAudioSourceField;*/
        protected override MethodBase GetTargetMethod()
        {
/*            _dayAudioSourceField = AccessTools.Field(typeof(AudioSource), "_outdoorAmbientDaySource");
            _nightAudioSourceField = AccessTools.Field(typeof(AudioSource), "_outdoorAmbientNightSource");*/
            return typeof(DayTimeAmbientBlender).GetMethod("SetSeasonStatus");
        }

        [PatchPrefix]
        private static bool PatchPrefix(DayTimeAmbientBlender __instance)
        {
            GameWorldController.RunEarlyGameCheck();

            if (GameWorldController.MuteAmbientAudio) return false;
            return true;
        }
    }

    class AmbientSoundPlayerGroupPatch : ModulePatch
    {
        private static string[] _clipsToDisable =
        {
            "lark", "crow", "nightingale", "greenmocking", "woodpecker", "robin", "raven", "rook", "bullfinch", "starling", "sparrow"
        };
        private static FieldInfo _playerGroupField;

        protected override MethodBase GetTargetMethod()
        {
            _playerGroupField = AccessTools.Field(typeof(AmbientSoundPlayerGroup), "_soundPlayers");
            return typeof(AmbientSoundPlayerGroup).GetMethod("Play");
        }

        [PatchPrefix]
        private static bool PatchPrefix(AmbientSoundPlayerGroup __instance)
        {
            GameWorldController.RunEarlyGameCheck();

            if (!GameWorldController.MuteAmbientAudio) return true;
            var soundPlayers = (List<BaseAmbientSoundPlayer>)_playerGroupField.GetValue(__instance);
            foreach (var soundPlayer in soundPlayers)
            {
                if (_clipsToDisable.Contains(soundPlayer.name.ToLower())) continue;
                return false;
            }
            return false;
        }
    }

    public class LampPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(LampController).GetMethod("ManualUpdate");
        }

        [PatchPostfix]
        public static void PatchPostfix(LampController __instance)
        {
            GameWorldController.RunEarlyGameCheck();

            if (!Plugin.ServerConfig.enable_hazard_zones || !Plugin.ModInfo.IsHalloween || !GameWorldController.IsMapThatCanDoGasEvent) return;
            if (GameWorldController.DidExplosionClientSide || Plugin.ModInfo.HasExploded)
            {
                __instance.Switch(Turnable.EState.Off);
                __instance.enabled = false;
            }
        }
    }

    //for events I need to dynamically change boss spawn chance, but the point at which the event is declared server-side is too late for changing boss spawns
    public class BossSpawnPatch : ModulePatch 
    {
        //Gas event can't be on labs or factory, so using these zones as proxy for map detection
        //no good way to know what map we're currently on at this point in the raid loading, it is what it is.
        private static string[] _forbiddenZones = { "BotZone", "BotZoneFloor1", "BotZoneFloor2", "BotZoneBasement" };


        protected override MethodBase GetTargetMethod()
        {
            return typeof(BossLocationSpawn).GetMethod("ParseMainTypesTypes");
        }

        private static void HandleZombies(BossLocationSpawn __instance, ref bool disabledZombieSpawn, bool isZombie)
        {
            bool disableZombie = !Plugin.ServerConfig.realistic_zombies || (Plugin.ServerConfig.realistic_zombies && !Plugin.ModInfo.DoGasEvent);
            if (isZombie && disableZombie)
            {
                __instance.BossChance = 0f;
                __instance.ShallSpawn = false;
                disabledZombieSpawn = true;
            }
        }
        
        [PatchPostfix]
        public static void PatchPostfix(BossLocationSpawn __instance)
        {
            GameWorldController.RunEarlyGameCheck();
            bool isZombie = __instance.BossType.ToString().ToLower().Contains("infected");
            bool disabledZombieSpawn = false;
            HandleZombies(__instance, ref disabledZombieSpawn, isZombie);

            var zones = __instance.BossZone.Split(new char[] { ',' });
            if (disabledZombieSpawn || _forbiddenZones.Intersect(zones).Any()) return;

            bool increaseSectantChance = __instance.BossType == WildSpawnType.sectantPriest && Plugin.ModInfo.DoGasEvent && !Plugin.ModInfo.DoExtraRaiders;
            bool increaseRaiderChance = __instance.BossType == WildSpawnType.pmcBot && Plugin.ModInfo.DoExtraRaiders;
            bool isPmc = __instance.BossType == WildSpawnType.pmcBEAR || __instance.BossType == WildSpawnType.pmcUSEC;
            bool postExpl = !isPmc && Plugin.ModInfo.IsHalloween && (Plugin.ModInfo.HasExploded || GameWorldController.DidExplosionClientSide);
            bool isPreExpl = Plugin.ModInfo.IsPreExplosion && GameWorldController.IsRightDateForExp;
            bool isSpecialEvent = postExpl || Plugin.ModInfo.DoGasEvent || isPreExpl;
            bool isRaider = __instance.BossType == WildSpawnType.pmcBot;
            bool isSectant = __instance.BossType != WildSpawnType.sectantPriest;
            if (increaseSectantChance) 
            {
                bool doExtraCultists = Plugin.ModInfo.DoExtraCultists;
                __instance.BossChance = __instance.BossChance == 0 && !doExtraCultists ? 25f : 100f;
                __instance.ShallSpawn = ChanceCalcClass.IsTrue100(__instance.BossChance);
            }
            else if (increaseRaiderChance) 
            {
                __instance.BossChance = 100f;
            }
            else if ((isSpecialEvent || Plugin.ModInfo.DoExtraRaiders) && (!isRaider && !isSectant && !isPmc && !isZombie))
            {
                __instance.BossChance = 0f;
                __instance.ShallSpawn = false;
            }

            if (PluginConfig.ZoneDebug.Value) 
            {
                Logger.LogWarning($"=============");
                Logger.LogWarning($"Do Gas Event ? {Plugin.ModInfo.DoGasEvent}");
                Logger.LogWarning($"Do raider Event ? {Plugin.ModInfo.DoExtraRaiders}");
                Logger.LogWarning($"Do extra cultists ? {Plugin.ModInfo.DoExtraCultists}");
                Logger.LogWarning("Boss type " + __instance.BossType);
                Logger.LogWarning("Boss type " + __instance.BossType);
                Logger.LogWarning("Spawn Chance " + __instance.BossChance);
                Logger.LogWarning("Shall Spawn " + __instance.ShallSpawn);
                Logger.LogWarning("=============");
            }

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
                            bool alreadyHasDevice = transmitter.ZoneAlreadyHasDevice();
                            bool hasBeenAnalysed = transmitter.TargetZone != null && transmitter.TargetZone.HasBeenAnalysed;
                            if (transmitter.TriggeredExplosion || hasBeenAnalysed) 
                            {
                                __result.Actions = new List<ActionsTypesClass>() { new ActionsTypesClass { Name = "", Action = DummyAction } };
                            }
                            else if (transmitter.CanTurnOn && !alreadyHasDevice)
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
            GameWorldController.RunEarlyGameCheck();

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
            GameWorldController.RunEarlyGameCheck();

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
                Plugin.RealismAudioControllerComponent.RunReInitPlayer();

                if (GameWorldController.DoMapGasEvent)
                {
                    Player player = Utils.GetYourPlayer();
                    AudioController.CreateAmbientAudioPlayer(player, player.gameObject.transform, Plugin.GasEventAudioClips, volume: 1.2f, minDelayBeforePlayback: 60f); //spooky short playback
                    AudioController.CreateAmbientAudioPlayer(player, player.gameObject.transform, Plugin.GasEventLongAudioClips, true, 5f, 30f, 0.38f, 55f, 65f, minDelayBeforePlayback: 0f); //long ambient
                }

                if (GameWorldController.DoMapRads)
                {
                    Player player = Utils.GetYourPlayer();
                    AudioController.CreateAmbientAudioPlayer(player, player.gameObject.transform, Plugin.RadEventAudioClips, volume: 1f, minDelayBeforePlayback: 60f); //thunder
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
                GameWorldController.ClearGameObjectLists();   
            }

            GameWorldController.GameStarted = false;
            GameWorldController.RanEarliestGameCheck = false;
            GameWorldController.TimeInRaid = 0f;
        }
    }
}
