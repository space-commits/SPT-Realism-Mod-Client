using BepInEx.Logging;
using EFT.InventoryLogic;
using EFT;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static ActiveHealthControllerClass;
using System.Collections;
using static System.Net.Mime.MediaTypeNames;
using UnityEngine;
using System.Reflection;
using Comfort.Common;
using HarmonyLib.Tools;
using static Systems.Effects.Effects;
using static RootMotion.Warning;
using static CW2.Animations.PhysicsSimulator.Val;
using EFT.Interactive;
using System.Threading.Tasks;
using Systems.Effects;

namespace RealismMod
{

    public static class MedProperties
    {
        public static string MedType(Item med)
        {
            return !Utils.NullCheck(med.ConflictingItems) ? med.ConflictingItems[1] : "Unknown";

        }

        public static string HBleedHealType(Item med)
        {
            return !Utils.NullCheck(med.ConflictingItems) ? med.ConflictingItems[2] : "Unknown";
        }

        public static float HpPerTick(Item med)
        {
            return !Utils.NullCheck(med.ConflictingItems) && float.TryParse(med.ConflictingItems[3], out float result) ? result : 1f;
        }

        public static readonly Dictionary<string, Type> EffectTypes = new Dictionary<string, Type>
        {
            { "Painkiller", typeof(GInterface207) },
            { "Tremor", typeof(GInterface210) },
            { "BrokenBone", typeof(GInterface192) },
            { "TunnelVision", typeof(GInterface212) },
            { "Contusion", typeof(GInterface202)  },
            { "HeavyBleeding", typeof(GInterface190) },
            { "LightBleeding", typeof(GInterface189) },
            { "Dehydration", typeof(GInterface193) },
            { "Exhaustion", typeof(GInterface194) },
            { "Pain", typeof(GInterface206) }
        };
    }

    public static class DamageTracker 
    {
        public static Dictionary<EDamageType, Dictionary<EBodyPart, float>> DamageRecord = new Dictionary<EDamageType, Dictionary<EBodyPart, float>>();

        public static float TotalHeavyBleedDamage = 0f;
        public static float TotalLightBleedDamage = 0f;
        public static float TotalDehydrationDamage = 0f;
        public static float TotalExhaustionDamage = 0f;

        //need to differentiate between head and body blunt damage
        public static void UpdateDamage(EDamageType damageType, EBodyPart bodyPart, float damage)
        {
            switch (damageType) 
            {
                case EDamageType.HeavyBleeding:
                    TotalHeavyBleedDamage += damage;
                    return;
                case EDamageType.LightBleeding:
                    TotalLightBleedDamage += damage;
                    return;
            }

 /*           if (!DamageRecord.ContainsKey(damageType))
            {
                DamageRecord[damageType] = new Dictionary<EBodyPart, float>();
            }

            Dictionary<EBodyPart, float> innerDict = DamageRecord[damageType];
            if (!innerDict.ContainsKey(bodyPart))
            {
                innerDict[bodyPart] = damage;
            }
            else
            {
                innerDict[bodyPart] += damage;
            }*/
        }

        public static void ResetTracker()
        {
            DamageRecord.Clear();
            TotalHeavyBleedDamage = 0f;
            TotalLightBleedDamage = 0f;
            TotalDehydrationDamage = 0f;
            TotalExhaustionDamage = 0f;
        }
    }


    public static class RealismHealthController
    {
        public static EBodyPart[] BodyParts = { EBodyPart.Head, EBodyPart.Chest, EBodyPart.Stomach, EBodyPart.RightLeg, EBodyPart.LeftLeg, EBodyPart.RightArm, EBodyPart.LeftArm };
       
        private static List<IHealthEffect> activeHealthEffects = new List<IHealthEffect>();

        private static float doubleClickTime = 0.2f;
        private static float timeSinceLastClicked = 0f;
        private static bool clickTriggered = false;

        public static void HealthController(ManualLogSource logger)
        {
            if (!Utils.IsInHideout())
            {
                if (Plugin.healthControllerTick >= 1f)
                {
                    ControllerTick(logger, Singleton<GameWorld>.Instance.AllPlayers[0]);
                    Plugin.healthControllerTick = 0f;
                }

                if (Input.GetKeyDown(Plugin.AddEffectKeybind.Value.MainKey))
                {
                    GameWorld gameWorld = Singleton<GameWorld>.Instance;
                    if (gameWorld?.AllPlayers.Count > 0)
                    {
                        TestAddBaseEFTEffect(Plugin.AddEffectBodyPart.Value, gameWorld.AllPlayers[0], Plugin.AddEffectType.Value);
                        NotificationManagerClass.DisplayMessageNotification("Adding Health Effect " + Plugin.AddEffectType.Value + " To Part " + (EBodyPart)Plugin.AddEffectBodyPart.Value);
                    }
                }

                if (Input.GetKeyDown(Plugin.DropGearKeybind.Value.MainKey))
                {
                    if (clickTriggered)
                    {
                        GameWorld gameWorld = Singleton<GameWorld>.Instance;
                        if (gameWorld?.AllPlayers.Count > 0)
                        {
                            DropBlockingGear(gameWorld.AllPlayers[0]);
                        }
                        clickTriggered = false;
                    }
                    else
                    {
                        clickTriggered = true;
                    }
                    timeSinceLastClicked = 0f;
                }
                timeSinceLastClicked += Time.deltaTime;
                if (timeSinceLastClicked > doubleClickTime)
                {
                    clickTriggered = false;
                }

            }
            if (Utils.IsInHideout() || !Utils.IsReady)
            {
                RemoveAllEffects();
                DamageTracker.ResetTracker();

            }
        }

        public static void TestAddBaseEFTEffect(int partIndex, Player player, String effect)
        {
            MethodInfo effectMethod = GetAddBaseEFTEffectMethod();

            effectMethod.MakeGenericMethod(typeof(ActiveHealthControllerClass).GetNestedType(effect, BindingFlags.NonPublic | BindingFlags.Instance)).Invoke(player.ActiveHealthController, new object[] { (EBodyPart)partIndex, null, null, null, null, null });
        }

        public static MethodInfo GetAddBaseEFTEffectMethod()
        {
            MethodInfo effectMethod = typeof(ActiveHealthControllerClass).GetMethods(BindingFlags.NonPublic | BindingFlags.Instance).First(m =>
            m.GetParameters().Length == 6
            && m.GetParameters()[0].Name == "bodyPart"
            && m.GetParameters()[5].Name == "initCallback"
            && m.IsGenericMethod);

            return effectMethod;
        }

        public static void RemoveBaseEFTEffect(Player player, EBodyPart targetBodyPart, string targetEffect) 
        {
            IEnumerable<IEffect> commonEffects = player.ActiveHealthController.GetAllActiveEffects(targetBodyPart);
            IReadOnlyList<GClass2102> effectsList = (IReadOnlyList<GClass2102>)AccessTools.Property(typeof(ActiveHealthControllerClass), "IReadOnlyList_0").GetValue(player.ActiveHealthController);

            Type targetType = null;
            if (MedProperties.EffectTypes.TryGetValue(targetEffect, out targetType))
            {
                for (int i = effectsList.Count - 1; i >= 0; i--)
                {
                    ActiveHealthControllerClass.GClass2102 effect = effectsList[i];
                    Type effectType = effect.Type;
                    EBodyPart effectPart = effect.BodyPart;
    
                    if (effectType == targetType && effectPart == targetBodyPart)
                    {

                        effect.ForceResidue();
                    }
                }
            }
        }

        public static void AddCustomEffect(IHealthEffect effect, bool canStack)
        {
            //need to decide if it's better to keep the old effect or to replace it with a new one.
            if (!canStack)
            {
                foreach (IHealthEffect eff in activeHealthEffects)
                {
                    if (eff.GetType() == effect.GetType() && eff.BodyPart == effect.BodyPart)
                    {
                        RemoveEffectOfType(effect.GetType(), effect.BodyPart);
                        break;
                    }
                }
            }

            activeHealthEffects.Add(effect);
        }

        public static void RemoveEffectOfType(Type effect, EBodyPart bodyPart)
        {
            for (int i = activeHealthEffects.Count - 1; i >= 0; i--)
            {
                if (activeHealthEffects[i].GetType() == effect && activeHealthEffects[i].BodyPart == bodyPart)
                {
                    activeHealthEffects.RemoveAt(i);
                }
            }
        }

        public static bool HasEffectOfType(Type effect, EBodyPart bodyPart)
        {
            bool hasEffect = false;
            for (int i = activeHealthEffects.Count - 1; i >= 0; i--)
            {
                if (activeHealthEffects[i].GetType() == effect && activeHealthEffects[i].BodyPart == bodyPart)
                {
                    hasEffect = true;
                }
            }
            return hasEffect;
        }

        public static void CancelEffects()
        {
            for (int i = activeHealthEffects.Count - 1; i >= 0; i--)
            {
                if (activeHealthEffects[i].Delay > 0f)
                {
                    activeHealthEffects.RemoveAt(i);
                }
            }
        }

        public static void RemoveRegenEffectsOfDamageType(EDamageType damageType) 
        {
            List<HealthRegenEffect> regenEffects = activeHealthEffects.OfType<HealthRegenEffect>().ToList();
            regenEffects.RemoveAll(x => x.DamageType == damageType);
            activeHealthEffects.RemoveAll(x => !regenEffects.Contains(x));
        }

        public static void RemoveEffectsOfType(EHealthEffectType effectType)
        {
            activeHealthEffects.RemoveAll(x => x.EffectType == effectType);
        }

        public static void RemoveAllEffects()
        {
            activeHealthEffects.Clear();
        }

        public static void ResetBleedDamageRecord(Player player) 
        {
            bool hasHeavyBleed = false;
            bool hasLightBleed = false;

            Type heavyBleedType;
            Type lightBleedType;
            MedProperties.EffectTypes.TryGetValue("HeavyBleeding", out heavyBleedType);
            MedProperties.EffectTypes.TryGetValue("LightBleeding", out lightBleedType);

            foreach (EBodyPart part in BodyParts)
            {
                IEnumerable<IEffect> effects = player.ActiveHealthController.GetAllActiveEffects(part);

                if (heavyBleedType != null && effects.Any(e => e.Type == heavyBleedType))
                {
                    hasHeavyBleed = true;
                }
                if (lightBleedType != null && effects.Any(e => e.Type == lightBleedType))
                {
                    hasLightBleed = true;
                }
            }
            if (!hasHeavyBleed)
            {
                DamageTracker.TotalHeavyBleedDamage = 0f;
            }
            if (!hasLightBleed)
            {
                DamageTracker.TotalLightBleedDamage = 0f;
            }
        }

        public static bool HasBaseEFTEffect(Player player, string targetEffect)
        {
            IEnumerable<IEffect> commonEffects = player.ActiveHealthController.GetAllEffects();
            IReadOnlyList<GClass2102> effectsList = (IReadOnlyList<GClass2102>)AccessTools.Property(typeof(ActiveHealthControllerClass), "IReadOnlyList_0").GetValue(player.ActiveHealthController);

            Type targetType = null;
            if (MedProperties.EffectTypes.TryGetValue(targetEffect, out targetType))
            {
                for (int i = effectsList.Count - 1; i >= 0; i--)
                {
                    ActiveHealthControllerClass.GClass2102 effect = effectsList[i];
                    Type effectType = effect.Type;
 
                    if (effectType == targetType)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static void ResourceRegenCheck(Player player, ManualLogSource logger) 
        {
            float vitalitySkill = player.Skills.VitalityBuffSurviobilityInc;
            float delay = (float)Math.Round(15f * (1f - vitalitySkill), 2);
            float tickRate = (float)Math.Round(0.22f * (1f + vitalitySkill), 2);

            bool isDehydrated = HasBaseEFTEffect(player, "Dehydration");
            bool isExhausted = HasBaseEFTEffect(player, "Exhaustion");

            if (isDehydrated)
            {
                RemoveRegenEffectsOfDamageType(EDamageType.Dehydration);
            }
            if (!isDehydrated && DamageTracker.TotalDehydrationDamage > 0f)
            {
                RestoreHPArossBody(player, DamageTracker.TotalDehydrationDamage, delay, EDamageType.Dehydration, tickRate);
                DamageTracker.TotalDehydrationDamage = 0;
            }

            if (isExhausted)
            {
                RemoveRegenEffectsOfDamageType(EDamageType.Exhaustion);
            }
            if (!isExhausted && DamageTracker.TotalExhaustionDamage > 0f)
            {
                RestoreHPArossBody(player, DamageTracker.TotalExhaustionDamage, delay, EDamageType.Exhaustion, tickRate);
                DamageTracker.TotalExhaustionDamage = 0;
            }
   
        }

        public static void ControllerTick(ManualLogSource logger, Player player)
        {
            if ((int)(Time.time % 3) == 0)
            {
                ResetBleedDamageRecord(player);
            }
            if ((int)(Time.time % 8) == 0)
            {
                ResourceRegenCheck(player, logger);
            }
            if ((int)(Time.time % 9) == 0)
            {
                DoubleBleedCheck(logger, player);
            }
            if ((int)(Time.time % 10) == 0)
            {
                PlayerInjuryStateCheck(player, logger);
            }
  
            for (int i = activeHealthEffects.Count - 1; i >= 0; i--)
            {
                IHealthEffect effect = activeHealthEffects[i];
                if (Plugin.EnableLogging.Value)
                {
                    logger.LogWarning("Type = " + effect.GetType().ToString());
                    logger.LogWarning("Delay = " + effect.Delay);
                }
  
                effect.Delay = effect.Delay > 0 ? effect.Delay - 1f : effect.Delay;

                if ((int)(Time.time % 3) == 0) 
                {
                    if (effect.Duration == null || effect.Duration > 0f)
                    {
                        effect.Tick();
                    }
                    else
                    {
                        if (Plugin.EnableLogging.Value)
                        {
                            logger.LogWarning("Removing Effect Due to Duration");
                        }
                        activeHealthEffects.RemoveAt(i);
                    }
                }
            }   
        }

        public static void AddPainEffect(Player player) 
        {
            if (!player.ActiveHealthController.BodyPartEffects.Effects[0].Any(v => v.Key == "Pain"))
            {
                MethodInfo addEffectMethod = GetAddBaseEFTEffectMethod();
                addEffectMethod.MakeGenericMethod(typeof(ActiveHealthControllerClass).GetNestedType("Pain", BindingFlags.NonPublic | BindingFlags.Instance)).Invoke(player.ActiveHealthController, new object[] { EBodyPart.Chest, 0f, 10f, 1f, 1f, null });
            }
        }

        public static void DropBlockingGear(Player player) 
        {
            Player.ItemHandsController itemHandsController = player.HandsController as Player.ItemHandsController;
            if (itemHandsController != null && itemHandsController.CurrentCompassState)
            {
                itemHandsController.SetCompassState(false);
                return;
            }

            if (player.MovementContext.StationaryWeapon == null && !player.HandsController.IsPlacingBeacon() && !player.HandsController.IsInInteractionStrictCheck() && player.CurrentStateName != EPlayerState.BreachDoor && !player.IsSprintEnabled)
            {
                InventoryControllerClass inventoryController = (InventoryControllerClass)AccessTools.Field(typeof(Player), "_inventoryController").GetValue(player);

                EquipmentClass equipment = (EquipmentClass)AccessTools.Property(typeof(Player), "Equipment").GetValue(player);

                List<Item> gear = new List<Item>();
                List<EquipmentSlot> slots = new List<EquipmentSlot>();

                if (BodyPartHasBleed(player, EBodyPart.Head))
                {
                    slots.Add(EquipmentSlot.Headwear);
                    slots.Add(EquipmentSlot.Earpiece);
                    slots.Add(EquipmentSlot.FaceCover);
                }
                if (BodyPartHasBleed(player, EBodyPart.Stomach) || BodyPartHasBleed(player, EBodyPart.Chest))
                {
                    slots.Add(EquipmentSlot.TacticalVest);
                    slots.Add(EquipmentSlot.Backpack);
                    slots.Add(EquipmentSlot.ArmorVest);
                }
;
                if (slots.Count < 1)
                {
                    return;
                }

                foreach (EquipmentSlot slot in slots)
                {
                    Item item = equipment.GetSlot(slot).ContainedItem;
                    if (item != null)
                    {
                        gear.Add(item);
                    }
                }

                if (gear.Count < 1)
                {
                    return;
                }

                NotificationManagerClass.DisplayMessageNotification("Dropping Gear");

                foreach (Item item in gear)
                {
                    if (inventoryController.CanThrow(item))
                    {
                        inventoryController.TryThrowItem(item, null, false);

                    }
                }
            }
        }

        public static bool MouthIsBlocked(Item head, Item face, EquipmentClass equipment)
        {
            bool faceBlocksMouth = false;
            bool headBlocksMouth = false;

            LootItemClass headwear = equipment.GetSlot(EquipmentSlot.Headwear).ContainedItem as LootItemClass;
            IEnumerable<Item> nestedItems = headwear != null ? headwear.GetAllItemsFromCollection().OfType<Item>() : null;

            if (nestedItems != null) 
            {
                foreach (Item item in nestedItems)
                {
                    FaceShieldComponent fs = item.GetItemComponent<FaceShieldComponent>();
                    if (headBlocksMouth = GearProperties.BlocksMouth(item) && fs == null) 
                    {
                        return true;
                    }
                }
            }

            if (head != null)
            {
                faceBlocksMouth = GearProperties.BlocksMouth(head);
            }
            if (face != null)
            {
                headBlocksMouth = GearProperties.BlocksMouth(face);
            }

            return faceBlocksMouth || headBlocksMouth;
        }

        public static bool BodyPartHasBleed(Player player, EBodyPart part) 
        {
            IEnumerable<IEffect> effects = player.ActiveHealthController.GetAllActiveEffects(part);

            Type heavyBleedType;
            Type lightBleedType;
            MedProperties.EffectTypes.TryGetValue("HeavyBleeding", out heavyBleedType);
            MedProperties.EffectTypes.TryGetValue("LightBleeding", out lightBleedType);

            bool hasHeavyBleed = heavyBleedType != null && effects.Any(e => e.Type == heavyBleedType);
            bool hasLightBleed = lightBleedType != null && effects.Any(e => e.Type == lightBleedType);

            if (hasHeavyBleed || hasLightBleed)
            {
                return true;
            }
            return false;
        }

        public static IEnumerable<IEffect> GetInjuriesOnBodyPart(Player player, EBodyPart part, ref bool hasHeavyBleed, ref bool hasLightBleed, ref bool hasFracture)
        {
            IEnumerable<IEffect> effects = player.ActiveHealthController.GetAllActiveEffects(part);

            Type heavyBleedType;
            Type lightBleedType;
            Type fractureType;
            MedProperties.EffectTypes.TryGetValue("HeavyBleeding", out heavyBleedType);
            MedProperties.EffectTypes.TryGetValue("LightBleeding", out lightBleedType);
            MedProperties.EffectTypes.TryGetValue("BrokenBone", out fractureType);

            hasHeavyBleed = heavyBleedType != null && effects.Any(e => e.Type == heavyBleedType);
            hasLightBleed = lightBleedType != null && effects.Any(e => e.Type == lightBleedType);
            hasFracture = fractureType != null && effects.Any(e => e.Type == fractureType);   

            return effects;
        }

        public static void GetBodyPartType(EBodyPart part, ref bool isNotLimb, ref bool isHead, ref bool isBody) 
        {
            isHead = part == EBodyPart.Head;
            isBody = part == EBodyPart.Chest || part == EBodyPart.Stomach;
            isNotLimb = part == EBodyPart.Chest || part == EBodyPart.Stomach || part == EBodyPart.Head;
        }

        public static void CanConsume(ManualLogSource Logger, Player player, Item item, ref bool canUse)
        {
            EquipmentClass equipment = (EquipmentClass)AccessTools.Property(typeof(Player), "Equipment").GetValue(player);
            Item face = equipment.GetSlot(EquipmentSlot.FaceCover).ContainedItem;
            Item head = equipment.GetSlot(EquipmentSlot.Headwear).ContainedItem;

            FaceShieldComponent fsComponent = player.FaceShieldObserver.Component;
            NightVisionComponent nvgComponent = player.NightVisionObserver.Component;
            bool fsIsON = fsComponent != null && (fsComponent.Togglable == null || fsComponent.Togglable.On) && GearProperties.BlocksMouth(fsComponent.Item);
            bool nvgIsOn = nvgComponent != null && (nvgComponent.Togglable == null || nvgComponent.Togglable.On);

            bool mouthBlocked = MouthIsBlocked(head, face, equipment);

            //will have to make mask exception for moustache, balaclava etc.
            if (fsIsON || nvgIsOn || mouthBlocked)
            {
                NotificationManagerClass.DisplayWarningNotification("Can't Eat/Drink, Mouth Is Blocked By Active Faceshield/NVGs Or Mask.", EFT.Communications.ENotificationDurationType.Long);
                canUse = false;
                return;
            }
        }

        public static void RestoreHPArossBody(Player player, float hpToRestore, float delay, EDamageType damageType, float tickRate)
        {
            hpToRestore = Mathf.RoundToInt((hpToRestore) / BodyParts.Length);

            foreach (EBodyPart part in BodyParts)
            {
                HealthRegenEffect regenEffect = new HealthRegenEffect(tickRate, null, part, player, delay, hpToRestore, damageType);
                AddCustomEffect(regenEffect, false);
            }
        }

        public static void TrnqtRestoreHPArossBody(Player player, float hpToRestore, float delay, EBodyPart bodyPart, EDamageType damageType, float vitalitySkill)
        {
            hpToRestore = Mathf.RoundToInt((hpToRestore) / (BodyParts.Length - 1));
            float tickRate = (float)Math.Round(0.85f * (1f + vitalitySkill), 2);

            foreach (EBodyPart part in BodyParts)
            {
                if (part != bodyPart)
                {
                    HealthRegenEffect regenEffect = new HealthRegenEffect(tickRate, null, part, player, delay, hpToRestore, damageType);
                    AddCustomEffect(regenEffect, false);
                }
            }
        }

        private static async Task handleHeavyBleedHeal(string medType, MedsClass meds, EBodyPart bodyPart, Player player, string hBleedHealType, bool isNotLimb, float vitalitySkill, float regenTickRate)
        {
            float delay = meds.HealthEffectsComponent.UseTime;
            await Task.Delay(TimeSpan.FromSeconds(delay));

            NotificationManagerClass.DisplayMessageNotification("Heavy Bleed On " + bodyPart + " Healed, Restoring HP.", EFT.Communications.ENotificationDurationType.Long);
            
            float trnqtTickRate = (float)Math.Round(MedProperties.HpPerTick(meds) * (1f - vitalitySkill), 2);
            float maxHpToRestore = Mathf.Round(35f * (1f + vitalitySkill));
            float hpToRestore = Mathf.Min(DamageTracker.TotalHeavyBleedDamage, maxHpToRestore);

            if ((hBleedHealType == "combo" || hBleedHealType == "trnqt") && !isNotLimb)
            {
                NotificationManagerClass.DisplayWarningNotification("Tourniquet Applied On " + bodyPart + ", You Are Losing Health On This Limb. Use A Surgery Kit To Remove It.", EFT.Communications.ENotificationDurationType.Long);

                TourniquetEffect trnqt = new TourniquetEffect(trnqtTickRate, null, bodyPart, player, 0f);
                AddCustomEffect(trnqt, false);

                if (DamageTracker.TotalHeavyBleedDamage > 0f)
                {
                    TrnqtRestoreHPArossBody(player, hpToRestore, 0f, bodyPart, EDamageType.HeavyBleeding, vitalitySkill);
                }
            }
            else if (DamageTracker.TotalHeavyBleedDamage > 0f)
            {
                RestoreHPArossBody(player, hpToRestore, 0f, EDamageType.HeavyBleeding, regenTickRate);
            }
            DamageTracker.TotalHeavyBleedDamage = Mathf.Max(DamageTracker.TotalHeavyBleedDamage - hpToRestore, 0f);
        }

        private static async Task handleLightBleedHeal(string medType, MedsClass meds, EBodyPart bodyPart, Player player, bool isNotLimb, float vitalitySkill, float regenTickRate)
        {
            float delay = meds.HealthEffectsComponent.UseTime;
            await Task.Delay(TimeSpan.FromSeconds(delay));

            NotificationManagerClass.DisplayMessageNotification("Light Bleed On " + bodyPart + " Healed, Restoring HP.", EFT.Communications.ENotificationDurationType.Long);

            float trnqtTickRate = (float)Math.Round(MedProperties.HpPerTick(meds) * (1f - vitalitySkill), 2);
            float maxHpToRestore = Mathf.Round(35f * (1f + vitalitySkill));
            float hpToRestore = Mathf.Min(DamageTracker.TotalLightBleedDamage, maxHpToRestore);

            if (medType == "trnqt" && !isNotLimb)
            {
                NotificationManagerClass.DisplayWarningNotification("Tourniquet Applied On " + bodyPart + ", You Are Losing Health On This Limb. Use A Surgery Kit To Remove It.", EFT.Communications.ENotificationDurationType.Long);

                TourniquetEffect trnqt = new TourniquetEffect(trnqtTickRate, null, bodyPart, player, 0f);
                AddCustomEffect(trnqt, false);
                
                if (DamageTracker.TotalLightBleedDamage > 0f)
                {
                    TrnqtRestoreHPArossBody(player, hpToRestore, 0f, bodyPart, EDamageType.LightBleeding, vitalitySkill);
                }
            }
            else if (DamageTracker.TotalLightBleedDamage > 0f)
            {
                RestoreHPArossBody(player, hpToRestore, 0f, EDamageType.LightBleeding, regenTickRate);
            }
            DamageTracker.TotalLightBleedDamage = Mathf.Max(DamageTracker.TotalLightBleedDamage - hpToRestore, 0f);
        }

        private static async Task handleSurgery(string medType, MedsClass meds, EBodyPart bodyPart, Player player, float vitalitySkill)
        {
            float delay = meds.HealthEffectsComponent.UseTime;
            await Task.Delay(TimeSpan.FromSeconds(delay));

            NotificationManagerClass.DisplayMessageNotification("Surgery Kit Applied On " + bodyPart + ", Restoring HP.", EFT.Communications.ENotificationDurationType.Long);

            float surgTickRate = (float)Math.Round(MedProperties.HpPerTick(meds) * (1f + vitalitySkill), 2);

            if (RealismMod.RealismHealthController.HasEffectOfType(typeof(TourniquetEffect), bodyPart))
            {
                NotificationManagerClass.DisplayMessageNotification("Surgical Kit Used, Removing Tourniquet Effect Present On Limb: " + bodyPart, EFT.Communications.ENotificationDurationType.Long);
            }

            SurgeryEffect surg = new SurgeryEffect(surgTickRate, null, bodyPart, player, 0f);
            AddCustomEffect(surg, false);
        }



        public static void HandleHealtheffects(string medType, MedsClass meds, EBodyPart bodyPart, Player player, string hBleedHealType, bool canHealHBleed, bool canHealLBleed, bool canHealFract)
        {
            float vitalitySkill = player.Skills.VitalityBuffSurviobilityInc;
            float regenTickRate = (float)Math.Round(0.85f * (1f + vitalitySkill), 2);

            bool hasHeavyBleed = false;
            bool hasLightBleed = false;
            bool hasFracture = false;

            IEnumerable<IEffect> effects = GetInjuriesOnBodyPart(player, bodyPart, ref hasHeavyBleed, ref hasLightBleed, ref hasFracture);

            bool isHead = false;
            bool isBody = false;
            bool isNotLimb = false;

            GetBodyPartType(bodyPart, ref isNotLimb, ref isHead, ref isBody);

            if (Plugin.TrnqtEffect.Value && hasHeavyBleed && canHealHBleed)
            {
#pragma warning disable CS4014
                handleHeavyBleedHeal(medType, meds, bodyPart, player, hBleedHealType, isNotLimb, vitalitySkill, regenTickRate);
#pragma warning restore CS4014
            }

            if (medType == "surg")
            {
#pragma warning disable CS4014
                handleSurgery(medType, meds, bodyPart, player, vitalitySkill);
#pragma warning restore CS4014
            }

            if (canHealLBleed && hasLightBleed && !hasHeavyBleed && (medType == "trnqt" && !isNotLimb || medType != "trnqt"))
            {
#pragma warning disable CS4014
                handleLightBleedHeal(medType, meds, bodyPart, player, isNotLimb, vitalitySkill, regenTickRate);
#pragma warning restore CS4014
            }

            if (canHealFract && hasFracture && (medType == "splint" || (medType == "medkit" && !hasHeavyBleed && !hasLightBleed)))
            {
                NotificationManagerClass.DisplayMessageNotification("Fracture On " + bodyPart + " Healed, Restoring HP.", EFT.Communications.ENotificationDurationType.Long);
                
                float trnqtTickRate = (float)Math.Round(0.85f * (1f + vitalitySkill), 2);
                HealthRegenEffect regenEffect = new HealthRegenEffect(0.85f * (1f + vitalitySkill), null, bodyPart, player, meds.HealthEffectsComponent.UseTime, 12f, EDamageType.Impact);
                AddCustomEffect(regenEffect, false);
            }
        }


        public static void CanUseMedItem(ManualLogSource Logger, Player player, EBodyPart bodyPart, Item item, ref bool canUse)
        {
            if (item.Template.Parent._id == "5448f3a64bdc2d60728b456a" || MedProperties.MedType(item) == "drug")
            {
                return;
            }

            MedsClass med = item as MedsClass;

            EquipmentClass equipment = (EquipmentClass)AccessTools.Property(typeof(Player), "Equipment").GetValue(player);

            Item head = equipment.GetSlot(EquipmentSlot.Headwear).ContainedItem;
            Item ears = equipment.GetSlot(EquipmentSlot.Earpiece).ContainedItem;
            Item glasses = equipment.GetSlot(EquipmentSlot.Eyewear).ContainedItem;
            Item face = equipment.GetSlot(EquipmentSlot.FaceCover).ContainedItem;
            Item vest = equipment.GetSlot(EquipmentSlot.ArmorVest).ContainedItem;
            Item tacrig = equipment.GetSlot(EquipmentSlot.TacticalVest).ContainedItem;
            Item bag = equipment.GetSlot(EquipmentSlot.Backpack).ContainedItem;

            bool mouthBlocked = MouthIsBlocked(head, face, equipment);

            bool hasHeadGear = head != null || ears != null || face != null;
            bool hasBodyGear = vest != null || tacrig != null || bag != null;

            bool isHead = false;
            bool isBody = false;
            bool isNotLimb = false;

            string medType = MedProperties.MedType(item);

            GetBodyPartType(bodyPart, ref isNotLimb, ref isHead, ref isBody);

            FaceShieldComponent fsComponent = player.FaceShieldObserver.Component;
            NightVisionComponent nvgComponent = player.NightVisionObserver.Component;
            bool fsIsON = fsComponent != null && (fsComponent.Togglable == null || fsComponent.Togglable.On);
            bool nvgIsOn = nvgComponent != null && (nvgComponent.Togglable == null || nvgComponent.Togglable.On);

            float medHPRes = med.MedKitComponent.HpResource;
       
            if (Plugin.GearBlocksEat.Value && medType == "pills" && (mouthBlocked || fsIsON || nvgIsOn)) 
            {
                NotificationManagerClass.DisplayWarningNotification("Can't Take Pills, Mouth Is Blocked By Faceshield/NVGs/Mask. Toggle Off Faceshield/NVG Or Remove Mask/Headgear", EFT.Communications.ENotificationDurationType.Long);
                canUse = false;
                return;
            }

            if (Plugin.GearBlocksHeal.Value && ((isBody && hasBodyGear) || (isHead && hasHeadGear))) 
            {
                NotificationManagerClass.DisplayWarningNotification("Part " + bodyPart + " Has Gear On, Remove Gear First To Be Able To Heal", EFT.Communications.ENotificationDurationType.Long);

                canUse = false;
                return;
            }

            if (medType == "vas") 
            {
                return;
            }

            bool hasHeavyBleed = false;
            bool hasLightBleed = false;
            bool hasFracture = false;

            IEnumerable<IEffect> effects = GetInjuriesOnBodyPart(player, bodyPart, ref hasHeavyBleed, ref hasLightBleed, ref hasFracture);

            if (isNotLimb && MedProperties.HBleedHealType(item) == "trnqt")
            {
                NotificationManagerClass.DisplayWarningNotification("Tourniquets Can Only Stop Heavy Bleeds On Limbs", EFT.Communications.ENotificationDurationType.Long);
                canUse = false;
                return;
            }

            if (medType == "splint" && med.HealthEffectsComponent.DamageEffects.ContainsKey(EDamageEffectType.Fracture) && isNotLimb)
            {
                NotificationManagerClass.DisplayWarningNotification("Splints Can Only Fix Fractures On Limbs", EFT.Communications.ENotificationDurationType.Long);
                canUse = false;
                return;
            }

            if (medType == "medkit" && med.HealthEffectsComponent.DamageEffects.ContainsKey(EDamageEffectType.Fracture) && hasFracture && isNotLimb && !hasHeavyBleed && !hasLightBleed)
            {
                NotificationManagerClass.DisplayWarningNotification("Splints Can Only Fix Fractures On Limbs", EFT.Communications.ENotificationDurationType.Long);
                canUse = false;
                return;
            }
            
            return;
        }

        public static void DoubleBleedCheck(ManualLogSource logger, Player player)
        {
            IEnumerable<IEffect> commonEffects = player.ActiveHealthController.GetAllActiveEffects(EBodyPart.Common);

            Type heavyBleedType;
            Type lightBleedType;
            MedProperties.EffectTypes.TryGetValue("HeavyBleeding", out heavyBleedType);
            MedProperties.EffectTypes.TryGetValue("LightBleeding", out lightBleedType);

            bool hasCommonHeavyBleed = heavyBleedType != null && commonEffects.Any(e => e.Type == heavyBleedType);
            bool hasCommonLightBleed = lightBleedType != null && commonEffects.Any(e => e.Type == lightBleedType);

            if (hasCommonHeavyBleed && hasCommonLightBleed)
            {
                IReadOnlyList<GClass2102> effectsList = (IReadOnlyList<GClass2102>)AccessTools.Property(typeof(ActiveHealthControllerClass), "IReadOnlyList_0").GetValue(player.ActiveHealthController);

                for (int i = effectsList.Count - 1; i >= 0; i--)
                {
                    Type effectType = effectsList[i].Type;
                    EBodyPart effectPart = effectsList[i].BodyPart;
                    ActiveHealthControllerClass.GClass2102 effect = effectsList[i];

                    IEnumerable<IEffect> effects = player.ActiveHealthController.GetAllActiveEffects(effectPart);
                    bool hasHeavyBleed = heavyBleedType != null && effects.Any(e => e.Type == heavyBleedType);
                    bool hasLightBleed = lightBleedType != null && effects.Any(e => e.Type == lightBleedType);

                    if (hasHeavyBleed && hasLightBleed && effectType == lightBleedType)
                    {
                         effect.ForceResidue();
                    }
                }
            }
        }


        private static float lastCurrentTotalHp = 0f;
        private static float lastCurrentEnergy = 0f;
        private static float lastCurrentHydro = 0f;

        public static void PlayerInjuryStateCheck(Player player, ManualLogSource logger)
        {

/*            bool rightArmDamaged = player.MovementContext.PhysicalConditionIs(EPhysicalCondition.RightArmDamaged);
            bool leftArmDamaged = player.MovementContext.PhysicalConditionIs(EPhysicalCondition.LeftArmDamaged);*/

/*            bool hasTremor = player.MovementContext.PhysicalConditionIs(EPhysicalCondition.Tremor);
            float tremorFactor = hasTremor ? 0.95f : 1f;*/

            float aimMoveSpeedBase = 0.42f;
            float ergoDeltaInjuryMulti = 1f;
            float adsInjuryMulti = 1f;
            float stanceInjuryMulti = 1f;
            float reloadInjuryMulti = 1f;
            float recoilInjuryMulti = 1f;
            float sprintSpeedInjuryMulti = 1f;
            float sprintAccelInjuryMulti = 1f;
            float walkSpeedInjuryMulti = 1f;
            float stamRegenInjuryMulti = 1f;
            float resourceRateInjuryMulti = 1f;

            float currentEnergy = player.ActiveHealthController.Energy.Current;
            float maxEnergy = player.ActiveHealthController.Energy.Maximum;
            float percentEnergy = currentEnergy / maxEnergy;

            float currentHydro = player.ActiveHealthController.Hydration.Current;
            float maxHydro = player.ActiveHealthController.Hydration.Maximum;
            float percentHydro = currentHydro / maxHydro;

            float totalMaxHp = 0f;
            float totalCurrentHp = 0f;

            Type fractureType;
            MedProperties.EffectTypes.TryGetValue("BrokenBone", out fractureType);

            foreach (EBodyPart part in BodyParts) 
            {
                IEnumerable<IEffect> effects = player.ActiveHealthController.GetAllActiveEffects(part);
                bool hasFracture = fractureType != null && effects.Any(e => e.Type == fractureType);

                bool isLeftArm = part == EBodyPart.LeftArm;
                bool isRightArm = part == EBodyPart.LeftArm;
                bool isArm = isLeftArm || isRightArm;
                bool isLeg = part == EBodyPart.LeftLeg || part == EBodyPart.RightLeg;
                bool isBody = part == EBodyPart.Chest || part == EBodyPart.Stomach;

                float currentHp = player.ActiveHealthController.GetBodyPartHealth(part).Current;
                float maxHp = player.ActiveHealthController.GetBodyPartHealth(part).Maximum;
                totalMaxHp += maxHp;
                totalCurrentHp += currentHp;

                float percentHp = (currentHp / maxHp);
                float percentHpStamRegen = 1f - ((1f - percentHp) / (isBody ? 10f : 5f));
                float percentHpWalk = 1f - ((1f - percentHp) / (isBody ? 15f : 7.5f));
                float percentHpSprint = 1f - ((1f - percentHp) / (isBody ? 8f : 4f));
                float percentHpAimMove = 1f - ((1f - percentHp) / (isArm ? 30f : 15f));
                float percentHpADS = 1f - ((1f - percentHp) / (isRightArm ? 2f : 5f));
                float percentHpStance = 1f - ((1f - percentHp) / (isRightArm ? 3f : 6f));
                float percentHpReload = 1f - ((1f - percentHp) / (isLeftArm ? 5.5f : 7.2f));
                float percentHpRecoil = 1f - ((1f - percentHp) / (isLeftArm ? 10f : 20f));

                if (percentHp <= 0.5f) 
                {
                    AddPainEffect(player);
                }

                if (isLeg || isBody) 
                {
                    aimMoveSpeedBase = aimMoveSpeedBase * percentHpAimMove;
                    sprintSpeedInjuryMulti = sprintSpeedInjuryMulti * percentHpSprint;
                    sprintAccelInjuryMulti = sprintAccelInjuryMulti * percentHp;
                    walkSpeedInjuryMulti = walkSpeedInjuryMulti * percentHpWalk;
                    stamRegenInjuryMulti = stamRegenInjuryMulti * percentHpStamRegen;
                }

                if (isArm) 
                {
                    if (isLeftArm) 
                    {
                        PlayerProperties.LeftArmRuined = player.ActiveHealthController.GetBodyPartHealth(EBodyPart.LeftArm).Current <= 0 || hasFracture;
                    }
                    if (isRightArm)
                    {
                        PlayerProperties.RightArmRuined = player.ActiveHealthController.GetBodyPartHealth(EBodyPart.RightArm).Current <= 0 || hasFracture;
                    }

                    float ruinedFactor = PlayerProperties.LeftArmRuined ? 0.7f : PlayerProperties.RightArmRuined ? 0.8f : PlayerProperties.LeftArmRuined && PlayerProperties.RightArmRuined ? 0.5f : 1f;

                    aimMoveSpeedBase = aimMoveSpeedBase * percentHpAimMove * ruinedFactor;
                    ergoDeltaInjuryMulti = ergoDeltaInjuryMulti * (1f + (1f - percentHp)) * ruinedFactor;
                    adsInjuryMulti = adsInjuryMulti * percentHpADS * ruinedFactor;
                    stanceInjuryMulti = stanceInjuryMulti * percentHpStance * ruinedFactor;
                    reloadInjuryMulti = reloadInjuryMulti * percentHpReload * ruinedFactor;
                    recoilInjuryMulti = recoilInjuryMulti * (1f + (1f - percentHpRecoil)) * ruinedFactor;
                }
            }

            float totalHpPercent = totalCurrentHp / totalMaxHp;
            resourceRateInjuryMulti = 1f - totalHpPercent;

            if (totalHpPercent <= 0.5f)
            {
                AddPainEffect(player);
            }

            float percentEnergyFactor = percentEnergy * 1.2f;
            float percentHydroFactor = percentHydro * 1.2f;

            float percentEnergySprint = 1f - ((1f - percentEnergyFactor) / 8f);
            float percentEnergyWalk = 1f - ((1f - percentEnergyFactor) / 12f);
            float percentEnergyAimMove = 1f - ((1f - percentEnergyFactor) / 20f);
            float percentEnergyADS = 1f - ((1f - percentEnergyFactor) / 5f);
            float percentEnergyStance = 1f - ((1f - percentEnergyFactor) / 2f);
            float percentEnergyReload = 1f - ((1f - percentEnergyFactor) / 10f);
            float percentEnergyRecoil = 1f - ((1f - percentEnergyFactor) / 40f);
            float percentEnergyErgo = 1f - ((1f - percentEnergyFactor) / 2f);
            float percentEnergyStamRegen = 1f - ((1f - percentEnergyFactor) / 10f);

            float percentHydroLowerLimit = 1f - ((1f - percentHydro) / 4f);
            float percentHydroLimitRecoil = 1f + ((1f - percentHydro) / 20f);
            float percentHydroUpperLimit = 1f + (1f - percentHydroLowerLimit);

            PlayerProperties.AimMoveSpeedBase = Mathf.Max(aimMoveSpeedBase, 0.3f * percentHydroLowerLimit);
            PlayerProperties.ErgoDeltaInjuryMulti = Mathf.Min(ergoDeltaInjuryMulti * (1f + (1f - percentEnergyErgo)), 3.5f);
            PlayerProperties.ADSInjuryMulti = Mathf.Max(adsInjuryMulti * percentEnergyADS, 0.4f * percentHydroLowerLimit);
            PlayerProperties.StanceInjuryMulti = Mathf.Max(stanceInjuryMulti * percentEnergyStance, 0.5f * percentHydroLowerLimit);
            PlayerProperties.ReloadInjuryMulti = Mathf.Max(reloadInjuryMulti * percentEnergyReload, 0.7f * percentHydroLowerLimit);
            PlayerProperties.RecoilInjuryMulti = Mathf.Min(recoilInjuryMulti * (1f + (1f - percentEnergyRecoil)), 1.15f * percentHydroLimitRecoil);
            PlayerProperties.HealthSprintSpeedFactor = Mathf.Max(sprintSpeedInjuryMulti * percentEnergySprint, 0.4f * percentHydroLowerLimit);
            PlayerProperties.HealthSprintAccelFactor = Mathf.Max(sprintAccelInjuryMulti * percentEnergySprint, 0.4f * percentHydroLowerLimit);
            PlayerProperties.HealthWalkSpeedFactor = Mathf.Max(walkSpeedInjuryMulti * percentEnergyWalk, 0.6f * percentHydroLowerLimit);
            PlayerProperties.HealthStamRegenFactor = Mathf.Max(stamRegenInjuryMulti * percentEnergyStamRegen, 0.5f * percentHydroLowerLimit);

            lastCurrentHydro = lastCurrentHydro == 0 ? currentHydro : lastCurrentHydro;
            lastCurrentEnergy = lastCurrentEnergy == 0 ? currentEnergy : lastCurrentEnergy;

            float hydroDiff = Math.Abs(currentHydro - lastCurrentHydro) / currentHydro;
            float energyDiff = Math.Abs(currentEnergy - lastCurrentEnergy) / currentEnergy;

            if (((int)(Time.time % 10) == 0 && totalCurrentHp != lastCurrentTotalHp) || energyDiff > 0.05f || hydroDiff > 0.05f) 
            {

                lastCurrentTotalHp = totalCurrentHp;
                lastCurrentEnergy = currentEnergy; 
                lastCurrentHydro = currentHydro;

                PropertyInfo energyProp = typeof(ActiveHealthControllerClass).GetProperty("EnergyRate");
                float currentEnergyRate = (float)energyProp.GetValue(player.ActiveHealthController);
                float prevEnergyRate = currentEnergyRate - PlayerProperties.HealthResourceRateFactor;

                if (Plugin.EnableLogging.Value)
                {
                    logger.LogWarning("currentEnergyRate " + currentEnergyRate);
                    logger.LogWarning("prevEnergyRate " + prevEnergyRate);
                }
 
                PropertyInfo hydroProp = typeof(ActiveHealthControllerClass).GetProperty("HydrationRate");
                float currentHydroRate = (float)hydroProp.GetValue(player.ActiveHealthController);
                float prevHydroRate = currentHydroRate - PlayerProperties.HealthResourceRateFactor;

                PlayerProperties.HealthResourceRateFactor = -resourceRateInjuryMulti;

                if (Plugin.EnableLogging.Value)
                {
                    logger.LogWarning("HealthResourceRateFactor " + PlayerProperties.HealthResourceRateFactor);
                }

                float newEnergyRate = (float)Math.Round(prevEnergyRate + PlayerProperties.HealthResourceRateFactor, 2);
                energyProp.SetValue(player.ActiveHealthController, newEnergyRate, null);

                float newHydroRate = (float)Math.Round(prevHydroRate + PlayerProperties.HealthResourceRateFactor, 2);
                hydroProp.SetValue(player.ActiveHealthController, newHydroRate, null);

                if (Plugin.EnableLogging.Value)
                {
                    logger.LogWarning("newEnergyRate " + newEnergyRate);
                }
            }
        }

        //reduntant
        /*        public static void PassiveHealthRegen(Player player)
                {
                    foreach (var damageTypeEntry in DamageTracker.DamageRecord)
                    {
                        EDamageType damageType = damageTypeEntry.Key;
                        Dictionary<EBodyPart, float> bodyPartDictionary = damageTypeEntry.Value;

                        foreach (var bodyPartEntry in bodyPartDictionary)
                        {
                            EBodyPart bodyPart = bodyPartEntry.Key;
                            float hpToRestore = bodyPartEntry.Value;

                            if (hpToRestore > 0)
                            {
                                //need to run this method per tick
                                //I need to remove existing regen on same part and of same type
                                //need to calculate how much HP would have been healed in that tick
                                //and then subtract it from the recorded amount of damage
                                HealthRegenEffect regenEffect = new HealthRegenEffect(0.25f, null, bodyPart, player, 15f, hpToRestore, damageType);
                                RealismHealthController.AddCustomEffect(regenEffect, true);
                                bodyPartDictionary[bodyPart] -= hpToRestore;
                            }
                        }
                    }
                }*/
    }
}
