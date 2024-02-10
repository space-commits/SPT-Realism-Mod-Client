using BepInEx.Logging;
using EFT.InventoryLogic;
using EFT;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using static System.Net.Mime.MediaTypeNames;
using UnityEngine;
using System.Reflection;
using Comfort.Common;
using HarmonyLib.Tools;
using static Systems.Effects.Effects;
using static CW2.Animations.PhysicsSimulator.Val;
using EFT.Interactive;
using System.Threading.Tasks;
using Systems.Effects;
using EffectClass = EFT.HealthSystem.ActiveHealthController.GClass2411;
using ExistanceClass = GClass2452;

namespace RealismMod
{
    public static class MedProperties
    {
        public static string MedType(Item med)
        {
            return !Utils.IsNull(med.ConflictingItems) ? med.ConflictingItems[1] : "Unknown";

        }

        public static string HBleedHealType(Item med)
        {
            return !Utils.IsNull(med.ConflictingItems) ? med.ConflictingItems[2] : "Unknown";
        }

        public static float HpPerTick(Item med)
        {
            return !Utils.IsNull(med.ConflictingItems) && float.TryParse(med.ConflictingItems[3], out float result) ? result : 1f;
        }

        public static bool CanBeUsedInRaid(Item med)
        {
            return !Utils.IsNull(med.ConflictingItems) && bool.TryParse(med.ConflictingItems[4], out bool result) ? result : false;
        }

        public static int PainKillerFullDuration(Item med)
        {
            return !Utils.IsNull(med.ConflictingItems) && int.TryParse(med.ConflictingItems[5], out int result) ? result : 1;
        }

        public static int PainKillerWaitTime(Item med)
        {
            return !Utils.IsNull(med.ConflictingItems) && int.TryParse(med.ConflictingItems[6], out int result) ? result : 1;
        }

        public static int PainKillerTime(Item med)
        {
            return !Utils.IsNull(med.ConflictingItems) && int.TryParse(med.ConflictingItems[7], out int result) ? result : 1;
        }

        public static float TunnelVisionStrength(Item med)
        {
            return !Utils.IsNull(med.ConflictingItems) && float.TryParse(med.ConflictingItems[8], out float result) ? result : 1f;
        }

        public static int Delay(Item med)
        {
            return !Utils.IsNull(med.ConflictingItems) && int.TryParse(med.ConflictingItems[9], out int result) ? result : 1;
        }

        public static float Strength(Item med)
        {
            return !Utils.IsNull(med.ConflictingItems) && float.TryParse(med.ConflictingItems[10], out float result) ? result : 0f;
        }

        public static readonly Dictionary<string, Type> EffectTypes = new Dictionary<string, Type>
        {
            { "Painkiller", typeof(GInterface258) },
            { "Tremor", typeof(GInterface261) },
            { "BrokenBone", typeof(GInterface243) },
            { "TunnelVision", typeof(GInterface263) },
            { "Contusion", typeof(GInterface253)  },
            { "HeavyBleeding", typeof(GInterface241) },
            { "LightBleeding", typeof(GInterface240) },
            { "Dehydration", typeof(GInterface244) },
            { "Exhaustion", typeof(GInterface245) },
            { "Pain", typeof(GInterface257) }
        };
    }

    public class DamageTracker
    {
        public Dictionary<EDamageType, Dictionary<EBodyPart, float>> DamageRecord = new Dictionary<EDamageType, Dictionary<EBodyPart, float>>();

        public float TotalHeavyBleedDamage = 0f;
        public float TotalLightBleedDamage = 0f;
        public float TotalDehydrationDamage = 0f;
        public float TotalExhaustionDamage = 0f;

        //need to differentiate between head and body blunt damage
        public void UpdateDamage(EDamageType damageType, EBodyPart bodyPart, float damage)
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

            if (!DamageRecord.ContainsKey(damageType))
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
            }
        }

        public void ResetTracker()
        {
            if (DamageRecord.Any()) 
            {
                DamageRecord.Clear();
            }
 
            TotalHeavyBleedDamage = 0f;
            TotalLightBleedDamage = 0f;
            TotalDehydrationDamage = 0f;
            TotalExhaustionDamage = 0f;
        }
    }


    public class RealismHealthController
    {
        public DamageTracker DmgTracker;

        public ManualLogSource Logger;

        private float healthControllerTime = 0f;
        private float effectsTime = 0f;

        public EBodyPart[] BodyParts = { EBodyPart.Head, EBodyPart.Chest, EBodyPart.Stomach, EBodyPart.RightLeg, EBodyPart.LeftLeg, EBodyPart.RightArm, EBodyPart.LeftArm };

        private List<IHealthEffect> activeHealthEffects = new List<IHealthEffect>();

        private float doubleClickTime = 0.2f;
        private float timeSinceLastClicked = 0f;
        private bool clickTriggered = false;

        private float adrenalineCooldownTime = 60f * (1f - PlayerStats.StressResistanceFactor);
        public bool AdrenalineCooldownActive = false;

        public float PainStrength = 0f;

        public bool Reset1 = false;
        public bool Reset2 = false;
        public bool Reset3 = false;
        public bool Reset4 = false;
        public bool Reset5 = false;
        public bool Reset6 = false;

        public RealismHealthController(DamageTracker dmgTracker, ManualLogSource logger) 
        {
            DmgTracker = dmgTracker;
            Logger = logger;
        }

        public void ControllerUpdate()
        {
            if (!Utils.IsInHideout && Utils.IsReady)
            {
                healthControllerTime += Time.deltaTime;
                effectsTime += Time.deltaTime;
                ControllerTick();

                if (Input.GetKeyDown(Plugin.AddEffectKeybind.Value.MainKey))
                {
                    GameWorld gameWorld = Singleton<GameWorld>.Instance;
                    if (gameWorld?.MainPlayer != null)
                    {
                        TestAddBaseEFTEffect(Plugin.AddEffectBodyPart.Value, gameWorld.AllAlivePlayersList[0], Plugin.AddEffectType.Value);
                        NotificationManagerClass.DisplayMessageNotification("Adding Health Effect " + Plugin.AddEffectType.Value + " To Part " + (EBodyPart)Plugin.AddEffectBodyPart.Value);
                    }
                }

                if (Input.GetKeyDown(Plugin.DropGearKeybind.Value.MainKey))
                {
                    if (clickTriggered)
                    {
                        GameWorld gameWorld = Singleton<GameWorld>.Instance;
                        if (gameWorld?.AllAlivePlayersList.Count > 0)
                        {
                            DropBlockingGear(gameWorld.AllAlivePlayersList[0]);
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

            if (Utils.IsInHideout || !Utils.IsReady)
            {
                RemoveAllEffects();
                DmgTracker.ResetTracker();
            }
 
            if (AdrenalineCooldownActive && adrenalineCooldownTime > 0.0f)
            {
                adrenalineCooldownTime -= Time.deltaTime;
            }
            if (AdrenalineCooldownActive && adrenalineCooldownTime <= 0.0f)
            {
                adrenalineCooldownTime = 60f * (1f - PlayerStats.StressResistanceFactor);
                AdrenalineCooldownActive = false;
            }
        }


        public void TestAddBaseEFTEffect(int partIndex, Player player, String effect)
        {
            if (effect == "removeHP")
            {
                player.ActiveHealthController.ChangeHealth((EBodyPart)partIndex, -player.ActiveHealthController.GetBodyPartHealth((EBodyPart)partIndex).Maximum, ExistanceClass.Existence);
                return;
            }
            if (effect == "addHP")
            {
                player.ActiveHealthController.ChangeHealth((EBodyPart)partIndex, player.ActiveHealthController.GetBodyPartHealth((EBodyPart)partIndex).Maximum, ExistanceClass.Existence);
                return;
            }
            if (effect == "")
            {
                return;
            }

            MethodInfo effectMethod = GetAddBaseEFTEffectMethodInfo();
            effectMethod.MakeGenericMethod(typeof(EFT.HealthSystem.ActiveHealthController).GetNestedType(effect, BindingFlags.NonPublic | BindingFlags.Instance)).Invoke(player.ActiveHealthController, new object[] { (EBodyPart)partIndex, null, null, null, null, null });
        }

        public void AddBasesEFTEffect(Player player, String effect, EBodyPart bodyPart, float delayTime, float duration, float residueTime, float strength)
        {
            MethodInfo effectMethod = GetAddBaseEFTEffectMethodInfo();
            effectMethod.MakeGenericMethod(typeof(EFT.HealthSystem.ActiveHealthController).GetNestedType(effect, BindingFlags.NonPublic | BindingFlags.Instance)).Invoke(player.ActiveHealthController, new object[] { bodyPart, delayTime, duration, residueTime, strength, null });
        }


        public void AddBaseEFTEffectIfNoneExisting(Player player, string effect, EBodyPart bodyPart, float delayTime, float duration, float residueTime, float strength)
        {
            if (!player.ActiveHealthController.BodyPartEffects.Effects[0].Any(v => v.Key == effect))
            {
                AddBasesEFTEffect(player, effect, bodyPart, delayTime, duration, residueTime, strength);
            }
        }

        public void AddToExistingBaseEFTEffect(Player player, string targetEffect, EBodyPart bodyPart, float delayTime, float duration, float residueTime, float strength)
        {
            if (!player.ActiveHealthController.BodyPartEffects.Effects[0].Any(v => v.Key == targetEffect))
            {
                AddBasesEFTEffect(player, targetEffect, bodyPart, delayTime, duration, residueTime, strength);
            }
            else
            {
                IReadOnlyList<EffectClass> effectsList = (IReadOnlyList<EffectClass>)AccessTools.Property(typeof(EFT.HealthSystem.ActiveHealthController), "IReadOnlyList_0").GetValue(player.ActiveHealthController);
                Type targetType = null;
                MedProperties.EffectTypes.TryGetValue(targetEffect, out targetType);
                for (int i = effectsList.Count - 1; i >= 0; i--)
                {
                    EffectClass existingEffect = effectsList[i];
                    Type effectType = existingEffect.Type;
                    EBodyPart effectPart = existingEffect.BodyPart;

                    if (effectType == targetType)
                    {
                        existingEffect.AddWorkTime(duration, false);
                    }
                }
            }
        }

        public void AddAdrenaline(Player player, float painkillerDuration, float negativeEffectDuration, float negativeEffectStrength)
        {
            if (Plugin.EnableAdrenaline.Value && !AdrenalineCooldownActive)
            {
                AdrenalineCooldownActive = true;
                AddToExistingBaseEFTEffect(player, "PainKiller", EBodyPart.Head, 0f, painkillerDuration, 3f, 1f);
                AddToExistingBaseEFTEffect(player, "TunnelVision", EBodyPart.Head, 0f, negativeEffectDuration, 3f, negativeEffectStrength);
                AddToExistingBaseEFTEffect(player, "Tremor", EBodyPart.Head, painkillerDuration, negativeEffectDuration, 3f, negativeEffectStrength);
            }
        }


        public MethodInfo GetAddBaseEFTEffectMethodInfo()
        {
            MethodInfo effectMethodInfo = typeof(EFT.HealthSystem.ActiveHealthController).GetMethods(BindingFlags.NonPublic | BindingFlags.Instance).First(m =>
            m.GetParameters().Length == 6
            && m.GetParameters()[0].Name == "bodyPart"
            && m.GetParameters()[5].Name == "initCallback"
            && m.IsGenericMethod);

            return effectMethodInfo;
        }

        public void RemoveBaseEFTEffect(Player player, EBodyPart targetBodyPart, string targetEffect)
        {
            IEnumerable<IEffect> commonEffects = player.ActiveHealthController.GetAllActiveEffects(targetBodyPart);
            IReadOnlyList<EffectClass> effectsList = (IReadOnlyList<EffectClass>)AccessTools.Property(typeof(EFT.HealthSystem.ActiveHealthController), "IReadOnlyList_0").GetValue(player.ActiveHealthController);

            Type targetType = null;
            if (MedProperties.EffectTypes.TryGetValue(targetEffect, out targetType))
            {
                for (int i = effectsList.Count - 1; i >= 0; i--)
                {
                    EffectClass effect = effectsList[i];
                    Type effectType = effect.Type;
                    EBodyPart effectPart = effect.BodyPart;

                    if (effectType == targetType && effectPart == targetBodyPart)
                    {
                        effect.ForceResidue();
                    }
                }
            }
        }

        public void AddCustomEffect(IHealthEffect newEffect, bool canStack)
        {
            //need to decide if it's better to keep the old effect or to replace it with a new one.
            if (!canStack)
            {
                foreach (IHealthEffect existingEff in activeHealthEffects)
                {
                    if (existingEff.GetType() == newEffect.GetType() && existingEff.BodyPart == newEffect.BodyPart)
                    {
                        RemoveEffectOfType(newEffect.GetType(), newEffect.BodyPart);
                        break;
                    }
                }
            }

            activeHealthEffects.Add(newEffect);
        }

        public void RemoveEffectOfType(Type effect, EBodyPart bodyPart)
        {
            for (int i = activeHealthEffects.Count - 1; i >= 0; i--)
            {
                IHealthEffect activeHealthEffect = activeHealthEffects[i];
                if (activeHealthEffect.GetType() == effect && activeHealthEffect.BodyPart == bodyPart)
                {
                    activeHealthEffects.RemoveAt(i);
                }
            }
        }

        public bool HasEffectOfType(Type effect, EBodyPart bodyPart)
        {
            bool hasEffect = false;
            for (int i = activeHealthEffects.Count - 1; i >= 0; i--)
            {
                IHealthEffect activeHealthEffect = activeHealthEffects[i];
                if (activeHealthEffect.GetType() == effect && activeHealthEffect.BodyPart == bodyPart)
                {
                    hasEffect = true;
                }
            }
            return hasEffect;
        }

        public void CancelEffects()
        {
            for (int i = activeHealthEffects.Count - 1; i >= 0; i--)
            {
                if (activeHealthEffects[i].Delay > 0f)
                {
                    activeHealthEffects.RemoveAt(i);
                }
            }
        }

        public void RemoveRegenEffectsOfDamageType(EDamageType damageType)
        {
            List<HealthRegenEffect> regenEffects = activeHealthEffects.OfType<HealthRegenEffect>().ToList();
            regenEffects.RemoveAll(x => x.DamageType == damageType);
            activeHealthEffects.RemoveAll(x => !regenEffects.Contains(x));
        }

        public void RemoveEffectsOfType(EHealthEffectType effectType)
        {
            activeHealthEffects.RemoveAll(x => x.EffectType == effectType);
        }

        public void RemoveAllEffects()
        {
            if (activeHealthEffects.Any()) 
            {
                activeHealthEffects.Clear();
            }
        }

        public void ResetBleedDamageRecord(Player player)
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
                DmgTracker.TotalHeavyBleedDamage = 0f;
            }
            if (!hasLightBleed)
            {
                DmgTracker.TotalLightBleedDamage = 0f;
            }
        }

        public bool HasBaseEFTEffect(Player player, string targetEffect)
        {
            IEnumerable<IEffect> commonEffects = player.ActiveHealthController.GetAllEffects();
            IReadOnlyList<EffectClass> effectsList = (IReadOnlyList<EffectClass>)AccessTools.Property(typeof(EFT.HealthSystem.ActiveHealthController), "IReadOnlyList_0").GetValue(player.ActiveHealthController);

            Type targetType = null;
            if (MedProperties.EffectTypes.TryGetValue(targetEffect, out targetType))
            {
                for (int i = effectsList.Count - 1; i >= 0; i--)
                {
                    EffectClass effect = effectsList[i];
                    Type effectType = effect.Type;

                    if (effectType == targetType)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public void ResourceRegenCheck(Player player)
        {
            float vitalitySkill = player.Skills.VitalityBuffSurviobilityInc.Value;
            int delay = (int)Math.Round(15f * (1f - vitalitySkill), 2);
            float tickRate = (float)Math.Round(0.22f * (1f + vitalitySkill), 2);

            bool isDehydrated = HasBaseEFTEffect(player, "Dehydration");
            bool isExhausted = HasBaseEFTEffect(player, "Exhaustion");

            if (isDehydrated)
            {
                RemoveRegenEffectsOfDamageType(EDamageType.Dehydration);
            }
            if (!isDehydrated && DmgTracker.TotalDehydrationDamage > 0f)
            {
                RestoreHPArossBody(player, DmgTracker.TotalDehydrationDamage, delay, EDamageType.Dehydration, tickRate);
                DmgTracker.TotalDehydrationDamage = 0;
            }

            if (isExhausted)
            {
                RemoveRegenEffectsOfDamageType(EDamageType.Exhaustion);
            }
            if (!isExhausted && DmgTracker.TotalExhaustionDamage > 0f)
            {
                RestoreHPArossBody(player, DmgTracker.TotalExhaustionDamage, delay, EDamageType.Exhaustion, tickRate);
                DmgTracker.TotalExhaustionDamage = 0;
            }

        }

        public void ControllerTick()
        {
            Player player = Singleton<GameWorld>.Instance.MainPlayer;
            if (healthControllerTime >= 0.5f && !Reset1)
            {
                ResetBleedDamageRecord(player);
                Reset1 = true;
            }
            if (healthControllerTime >= 1f && !Reset2)
            {
                ResourceRegenCheck(player);
                Reset2 = true;
            }
            if (healthControllerTime >= 2f && !Reset3)
            {
                DoubleBleedCheck(player);
                Reset3 = true;
            }
            if (healthControllerTime >= 3f && !Reset4)
            {
                PlayerInjuryStateCheck(player);
                Reset4 = true;
            }


            if (effectsTime >= 1f)
            {
                for (int i = activeHealthEffects.Count - 1; i >= 0; i--)
                {
                    IHealthEffect effect = activeHealthEffects[i];
                    if (Plugin.EnableLogging.Value)
                    {
                        Logger.LogWarning("Type = " + effect.GetType().ToString());
                        Logger.LogWarning("Delay = " + effect.Delay);
                    }

                    effect.Delay = effect.Delay > 0 ? effect.Delay - 1 : effect.Delay;

                    if (effect.Duration == null || effect.Duration > 0f)
                    {
                        effect.Tick();
                    }
                    else
                    {
                        if (Plugin.EnableLogging.Value)
                        {
                            Logger.LogWarning("Removing Effect Due to Duration");
                        }
                        activeHealthEffects.RemoveAt(i);
                    }
                }
                effectsTime = 0f;
            }
            
            if (healthControllerTime >= 3f) 
            {
                healthControllerTime = 0f;
                Reset1 = false;
                Reset2 = false;
                Reset3 = false;
                Reset4 = false;
                Reset5 = false;
                Reset6 = false;
            }
        }

        public void DropBlockingGear(Player player)
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

                foreach (Item item in gear)
                {
                    if (inventoryController.CanThrow(item))
                    {
                        inventoryController.TryThrowItem(item, null, false);
                    }
                }
            }
        }

        public bool MouthIsBlocked(Item head, Item face, EquipmentClass equipment)
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
                    if (GearStats.BlocksMouth(item) && fs == null)
                    {
                        return true;
                    }
                }
            }

            if (head != null)
            {
                faceBlocksMouth = GearStats.BlocksMouth(head);
            }
            if (face != null)
            {
                headBlocksMouth = GearStats.BlocksMouth(face);
            }

            return faceBlocksMouth || headBlocksMouth;
        }

        public bool BodyPartHasBleed(Player player, EBodyPart part)
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

        public IEnumerable<IEffect> GetInjuriesOnBodyPart(Player player, EBodyPart part, ref bool hasHeavyBleed, ref bool hasLightBleed, ref bool hasFracture)
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

        public void GetBodyPartType(EBodyPart part, ref bool isNotLimb, ref bool isHead, ref bool isBody)
        {
            isHead = part == EBodyPart.Head;
            isBody = part == EBodyPart.Chest || part == EBodyPart.Stomach;
            isNotLimb = part == EBodyPart.Chest || part == EBodyPart.Stomach || part == EBodyPart.Head;
        }

        public void CanConsume(Player player, Item item, ref bool canUse)
        {
            EquipmentClass equipment = (EquipmentClass)AccessTools.Property(typeof(Player), "Equipment").GetValue(player);
            Item face = equipment.GetSlot(EquipmentSlot.FaceCover).ContainedItem;
            Item head = equipment.GetSlot(EquipmentSlot.Headwear).ContainedItem;

            FaceShieldComponent fsComponent = player.FaceShieldObserver.Component;
            NightVisionComponent nvgComponent = player.NightVisionObserver.Component;
            bool fsIsON = fsComponent != null && (fsComponent.Togglable == null || fsComponent.Togglable.On) && GearStats.BlocksMouth(fsComponent.Item);
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

        public void DoPassiveRegen(float tickRate, EBodyPart bodyPart, Player player, int delay, float hpToRestore, EDamageType damageType)
        {
            if (!HasEffectOfType(typeof(TourniquetEffect), bodyPart))
            {
                HealthRegenEffect regenEffect = new HealthRegenEffect(tickRate, null, bodyPart, player, delay, hpToRestore, damageType);
                AddCustomEffect(regenEffect, false);
            }
        }


        public void RestoreHPArossBody(Player player, float hpToRestore, int delay, EDamageType damageType, float tickRate)
        {
            hpToRestore = Mathf.RoundToInt((hpToRestore) / BodyParts.Length);

            foreach (EBodyPart part in BodyParts)
            {
                if (!HasEffectOfType(typeof(TourniquetEffect), part))
                {
                    HealthRegenEffect regenEffect = new HealthRegenEffect(tickRate, null, part, player, delay, hpToRestore, damageType);
                    AddCustomEffect(regenEffect, false);
                }
            }
        }

        public void TrnqtRestoreHPArossBody(Player player, float hpToRestore, int delay, EBodyPart bodyPart, EDamageType damageType, float vitalitySkill)
        {
            hpToRestore = Mathf.RoundToInt((hpToRestore) / (BodyParts.Length - 1));
            float tickRate = (float)Math.Round(0.85f * (1f + vitalitySkill), 2);

            foreach (EBodyPart part in BodyParts)
            {
                if (part != bodyPart && !HasEffectOfType(typeof(TourniquetEffect), part))
                {
                    HealthRegenEffect regenEffect = new HealthRegenEffect(tickRate, null, part, player, delay, hpToRestore, damageType);
                    AddCustomEffect(regenEffect, false);
                }
            }
        }

        private void handleHeavyBleedHeal(string medType, MedsClass meds, EBodyPart bodyPart, Player player, string hBleedHealType, bool isNotLimb, float vitalitySkill, float regenTickRate)
        {
            int delay = (int)meds.HealthEffectsComponent.UseTime;

            NotificationManagerClass.DisplayMessageNotification("Heavy Bleed On " + bodyPart + " Healed, Restoring HP.", EFT.Communications.ENotificationDurationType.Long);

            float trnqtTickRate = (float)Math.Round(MedProperties.HpPerTick(meds) * (1f - vitalitySkill), 2);
            float maxHpToRestore = Mathf.Round(35f * (1f + vitalitySkill));
            float hpToRestore = Mathf.Min(DmgTracker.TotalHeavyBleedDamage, maxHpToRestore);

            if ((hBleedHealType == "combo" || hBleedHealType == "trnqt") && !isNotLimb)
            {
                TourniquetEffect trnqt = new TourniquetEffect(trnqtTickRate, null, bodyPart, player, delay);
                AddCustomEffect(trnqt, false);

                if (DmgTracker.TotalHeavyBleedDamage > 0f)
                {
                    TrnqtRestoreHPArossBody(player, hpToRestore, delay, bodyPart, EDamageType.HeavyBleeding, vitalitySkill);
                }
            }
            else if (DmgTracker.TotalHeavyBleedDamage > 0f)
            {
                RestoreHPArossBody(player, hpToRestore, delay, EDamageType.HeavyBleeding, regenTickRate);
            }
            DmgTracker.TotalHeavyBleedDamage = Mathf.Max(DmgTracker.TotalHeavyBleedDamage - hpToRestore, 0f);
        }

        private void handleLightBleedHeal(string medType, MedsClass meds, EBodyPart bodyPart, Player player, bool isNotLimb, float vitalitySkill, float regenTickRate)
        {
            int delay = (int)meds.HealthEffectsComponent.UseTime;

            NotificationManagerClass.DisplayMessageNotification("Light Bleed On " + bodyPart + " Healed, Restoring HP.", EFT.Communications.ENotificationDurationType.Long);

            float trnqtTickRate = (float)Math.Round(MedProperties.HpPerTick(meds) * (1f - vitalitySkill), 2);
            float maxHpToRestore = Mathf.Round(35f * (1f + vitalitySkill));
            float hpToRestore = Mathf.Min(DmgTracker.TotalLightBleedDamage, maxHpToRestore);

            if (medType == "trnqt" && !isNotLimb)
            {
                TourniquetEffect trnqt = new TourniquetEffect(trnqtTickRate, null, bodyPart, player, delay);
                AddCustomEffect(trnqt, false);

                if (DmgTracker.TotalLightBleedDamage > 0f)
                {
                    TrnqtRestoreHPArossBody(player, hpToRestore, delay, bodyPart, EDamageType.LightBleeding, vitalitySkill);
                }
            }
            else if (DmgTracker.TotalLightBleedDamage > 0f)
            {
                RestoreHPArossBody(player, hpToRestore, delay, EDamageType.LightBleeding, regenTickRate);
            }
            DmgTracker.TotalLightBleedDamage = Mathf.Max(DmgTracker.TotalLightBleedDamage - hpToRestore, 0f);
        }

        private void handleSurgery(string medType, MedsClass meds, EBodyPart bodyPart, Player player, float surgerySkill)
        {
            int delay = (int)meds.HealthEffectsComponent.UseTime;
            float regenLimitFactor = 0.5f * (1f + surgerySkill);
            float surgTickRate = (float)Math.Round(MedProperties.HpPerTick(meds) * (1f + surgerySkill), 2);
            SurgeryEffect surg = new SurgeryEffect(surgTickRate, null, bodyPart, player, delay, regenLimitFactor);
            AddCustomEffect(surg, false);
        }

        private void handleSplint(MedsClass meds, float regenTickRate, EBodyPart bodyPart, Player player)
        {

            NotificationManagerClass.DisplayMessageNotification("Fracture On " + bodyPart + " Healed, Restoring HP.", EFT.Communications.ENotificationDurationType.Long);

            int delay = (int)meds.HealthEffectsComponent.UseTime;
            HealthRegenEffect regenEffect = new HealthRegenEffect(regenTickRate, null, bodyPart, player, delay, 12f, EDamageType.Impact);
            AddCustomEffect(regenEffect, false);
        }

        public void HandleHealthEffects(string medType, MedsClass meds, EBodyPart bodyPart, Player player, string hBleedHealType, bool canHealHBleed, bool canHealLBleed, bool canHealFract)
        {
            float vitalitySkill = player.Skills.VitalityBuffBleedChanceRed.Value;
            float surgerySkill = player.Skills.SurgeryReducePenalty.Value;
            float regenTickRate = (float)Math.Round(0.4f * (1f + vitalitySkill), 2);

            bool hasHeavyBleed = false;
            bool hasLightBleed = false;
            bool hasFracture = false;

            IEnumerable<IEffect> effects = GetInjuriesOnBodyPart(player, bodyPart, ref hasHeavyBleed, ref hasLightBleed, ref hasFracture);

            bool isHead = false;
            bool isBody = false;
            bool isNotLimb = false;

            GetBodyPartType(bodyPart, ref isNotLimb, ref isHead, ref isBody);

            if (Plugin.EnableTrnqtEffect.Value && hasHeavyBleed && canHealHBleed)
            {
                handleHeavyBleedHeal(medType, meds, bodyPart, player, hBleedHealType, isNotLimb, vitalitySkill, regenTickRate);
            }

            if (medType == "surg")
            {
                handleSurgery(medType, meds, bodyPart, player, surgerySkill);
            }

            if (canHealLBleed && hasLightBleed && !hasHeavyBleed && (medType == "trnqt" && !isNotLimb || medType != "trnqt"))
            {
                handleLightBleedHeal(medType, meds, bodyPart, player, isNotLimb, vitalitySkill, regenTickRate);
            }

            if (canHealFract && hasFracture && (medType == "splint" || (medType == "medkit" && !hasHeavyBleed && !hasLightBleed)))
            {
                handleSplint(meds, regenTickRate, bodyPart, player);
            }
        }


        public void CanUseMedItem(Player player, EBodyPart bodyPart, Item item, ref bool canUse)
        {
            if (item.Template.Parent._id == "5448f3a64bdc2d60728b456a" || MedProperties.MedType(item).Contains("drug"))
            {
                return;
            }

            if (MedProperties.CanBeUsedInRaid(item) == false)
            {
                NotificationManagerClass.DisplayWarningNotification("This Item Can Not Be Used In Raid", EFT.Communications.ENotificationDurationType.Long);
                canUse = false;
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

            if (medType == "vas")
            {
                return;
            }

            if (medType.Contains("pills"))
            {
                if (Plugin.GearBlocksEat.Value && (mouthBlocked || fsIsON || nvgIsOn))
                {
                    NotificationManagerClass.DisplayWarningNotification("Can't Take Pills, Mouth Is Blocked By Faceshield/NVGs/Mask. Toggle Off Faceshield/NVG Or Remove Mask/Headgear", EFT.Communications.ENotificationDurationType.Long);
                    canUse = false;
                    return;
                }
                return;
            }


            if (Plugin.GearBlocksHeal.Value && ((isBody && hasBodyGear) || (isHead && hasHeadGear)))
            {
                NotificationManagerClass.DisplayWarningNotification("Part " + bodyPart + " Has Gear On, Remove Gear First To Be Able To Heal", EFT.Communications.ENotificationDurationType.Long);

                canUse = false;
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

        public void DoubleBleedCheck(Player player)
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
                IReadOnlyList<EffectClass> effectsList = (IReadOnlyList<EffectClass>)AccessTools.Property(typeof(EFT.HealthSystem.ActiveHealthController), "IReadOnlyList_0").GetValue(player.ActiveHealthController);

                for (int i = effectsList.Count - 1; i >= 0; i--)
                {

                    EffectClass effect = effectsList[i];
                    Type effectType = effect.Type;
                    EBodyPart effectPart = effect.BodyPart;


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

        public void PlayerInjuryStateCheck(Player player)
        {
            bool rightArmDamaged = player.MovementContext.PhysicalConditionIs(EPhysicalCondition.RightArmDamaged);
            bool leftArmDamaged = player.MovementContext.PhysicalConditionIs(EPhysicalCondition.LeftArmDamaged);

            bool hasTremor = player.MovementContext.PhysicalConditionIs(EPhysicalCondition.Tremor);
            float tremorFactor = hasTremor ? 0.95f : 1f;

            float aimMoveSpeedMulti = 1f;
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

            PainStrength = 0f;

            Type fractureType;
            MedProperties.EffectTypes.TryGetValue("BrokenBone", out fractureType);

            foreach (EBodyPart part in BodyParts)
            {
                IEnumerable<IEffect> effects = player.ActiveHealthController.GetAllActiveEffects(part);
                bool hasFracture = fractureType != null && effects.Any(e => e.Type == fractureType);

                if (hasFracture)
                {
                    PainStrength += 5;
                }

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
                float percentHpAimMove = 1f - ((1f - percentHp) / (isArm ? 20f : 14f));
                float percentHpADS = 1f - ((1f - percentHp) / (isRightArm ? 1f : 2f));
                float percentHpStance = 1f - ((1f - percentHp) / (isRightArm ? 1.5f : 3f));
                float percentHpReload = 1f - ((1f - percentHp) / (isLeftArm ? 2f : 3.5f));
                float percentHpRecoil = 1f - ((1f - percentHp) / (isLeftArm ? 10f : 20f));

                if (percentHp <= 0.5f)
                {
                    AddBaseEFTEffectIfNoneExisting(player, "Pain", part, 0f, 10f, 1f, 1f);
                    PainStrength += 1;
                }

                if (currentHp <= 0)
                {
                    PainStrength += 5;
                }

                if (isLeg || isBody)
                {
                    aimMoveSpeedMulti *= percentHpAimMove;
                    sprintSpeedInjuryMulti *= percentHpSprint;
                    sprintAccelInjuryMulti *= percentHp;
                    walkSpeedInjuryMulti *= percentHpWalk;
                    stamRegenInjuryMulti *= percentHpStamRegen;
                }

                if (isArm)
                {
                    bool isArmRuined = currentHp <= 0 || hasFracture;
                    if (isLeftArm)
                    {
                        PlayerStats.LeftArmRuined = isArmRuined;
                    }
                    if (isRightArm)
                    {
                        PlayerStats.RightArmRuined = isArmRuined;
                    }

                    float ruinedFactor = PlayerStats.LeftArmRuined ? 0.8f : PlayerStats.RightArmRuined ? 0.9f : PlayerStats.LeftArmRuined && PlayerStats.RightArmRuined ? 0.7f : 1f;
                    float armFractureFactor = isLeftArm && hasFracture ? 0.8f : isRightArm && hasFracture ? 0.9f : 1f;

                    aimMoveSpeedMulti *= percentHpAimMove * armFractureFactor;
                    ergoDeltaInjuryMulti *= (1f + (1f - percentHp)) * armFractureFactor;
                    adsInjuryMulti *= percentHpADS * armFractureFactor;
                    stanceInjuryMulti *= percentHpStance * armFractureFactor;
                    reloadInjuryMulti *= percentHpReload * armFractureFactor;
                    recoilInjuryMulti *= (1f + (1f - percentHpRecoil)) * armFractureFactor;
                }
            }

            float totalHpPercent = totalCurrentHp / totalMaxHp;
            resourceRateInjuryMulti = Mathf.Clamp((1f - (totalHpPercent * 1.25f)), 0f, 1f);

            if (totalHpPercent <= 0.5f)
            {
                AddBaseEFTEffectIfNoneExisting(player, "Pain", EBodyPart.Chest, 0f, 10f, 1f, 1f);
                PainStrength += 5;
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

            PlayerStats.AimMoveSpeedInjuryMulti = Mathf.Max(aimMoveSpeedMulti, 0.6f * percentHydroLowerLimit);
            PlayerStats.ErgoDeltaInjuryMulti = Mathf.Min(ergoDeltaInjuryMulti * (1f + (1f - percentEnergyErgo)), 3.5f);
            PlayerStats.ADSInjuryMulti = Mathf.Max(adsInjuryMulti * percentEnergyADS, 0.35f * percentHydroLowerLimit);
            PlayerStats.StanceInjuryMulti = Mathf.Max(stanceInjuryMulti * percentEnergyStance, 0.45f * percentHydroLowerLimit);
            PlayerStats.ReloadInjuryMulti = Mathf.Max(reloadInjuryMulti * percentEnergyReload, 0.75f * percentHydroLowerLimit);
            PlayerStats.RecoilInjuryMulti = Mathf.Min(recoilInjuryMulti * (1f + (1f - percentEnergyRecoil)), 1.15f * percentHydroLimitRecoil);
            PlayerStats.HealthSprintSpeedFactor = Mathf.Max(sprintSpeedInjuryMulti * percentEnergySprint, 0.4f * percentHydroLowerLimit);
            PlayerStats.HealthSprintAccelFactor = Mathf.Max(sprintAccelInjuryMulti * percentEnergySprint, 0.4f * percentHydroLowerLimit);
            PlayerStats.HealthWalkSpeedFactor = Mathf.Max(walkSpeedInjuryMulti * percentEnergyWalk, 0.6f * percentHydroLowerLimit);
            PlayerStats.HealthStamRegenFactor = Mathf.Max(stamRegenInjuryMulti * percentEnergyStamRegen, 0.5f * percentHydroLowerLimit);

            if (totalHpPercent < 1f)
            {
                ResourceRateEffect resEffect = new ResourceRateEffect(resourceRateInjuryMulti, null, player, 0);
                AddCustomEffect(resEffect, true);
            }
        }
    }
}
