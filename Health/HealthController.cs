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

namespace RealismMod
{

    public static class MedProperties
    {
        public static string MedType(Item med)
        {
            if (Utils.NullCheck(med.ConflictingItems))
            {
                return "unknown";
            }
            return med.ConflictingItems[1];
        }

        public static string HBleedHealType(Item med)
        {
            if (Utils.NullCheck(med.ConflictingItems))
            {
                return "unknown";
            }
            return med.ConflictingItems[2];
        }

        public static float HpPerTick(Item med)
        {
            if (Utils.NullCheck(med.ConflictingItems))
            {
                return 1f;
            }
            return float.Parse(med.ConflictingItems[3]);
        }

        public static readonly Dictionary<string, Type> EffectTypes = new Dictionary<string, Type>
        {
            { "Painkiller", typeof(GInterface208) },
            { "Tremor", typeof(GInterface211) },
            { "BrokenBone", typeof(GInterface193) },
            { "TunnelVision", typeof(GInterface213) },
            { "Contusion", typeof(GInterface203)  },
            { "HeavyBleeding", typeof(GInterface191) },
            { "LightBleeding", typeof(GInterface190) },
            { "Pain", typeof(GInterface207) }
        };
    }

    public static class DamageTracker 
    {

        public static float TotalHeavyBleedDamage = 0f;
        public static float TotalLightBleedDamage = 0f;
        public static float TotalFallDamage = 0f;
        public static float TotalBluntDamage = 0f;
        public static float TotalBarbedWireDamage = 0f;

        //need to differentiate between head and body blunt damage
        public static void AddDamage(EDamageType damageType, EBodyPart bodyPart, float damage)
        {
            switch (damageType) 
            {
                case EDamageType.HeavyBleeding:
                    TotalHeavyBleedDamage += damage;
                    break;
                case EDamageType.LightBleeding:
                    TotalLightBleedDamage += damage;
                    break;
                case EDamageType.Fall:
                    TotalFallDamage += damage;
                    break;
                case EDamageType.Blunt:
                    TotalBluntDamage += damage;
                    break;
                case EDamageType.Barbed:
                    TotalBarbedWireDamage += damage;
                    break;
            }
        }
    }


    public static class RealismHealthController
    {
        public static EBodyPart[] BodyParts = { EBodyPart.Head, EBodyPart.Chest, EBodyPart.Stomach, EBodyPart.RightLeg, EBodyPart.LeftLeg, EBodyPart.RightArm, EBodyPart.LeftArm };

        private static List<IHealthEffect> activeHealthEffects = new List<IHealthEffect>();

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
            IReadOnlyList<GClass2103> effectsList = (IReadOnlyList<GClass2103>)AccessTools.Property(typeof(ActiveHealthControllerClass), "IReadOnlyList_0").GetValue(player.ActiveHealthController);

            Type targetType = null;
            if (MedProperties.EffectTypes.TryGetValue(targetEffect, out targetType))
            {
                for (int i = effectsList.Count - 1; i >= 0; i--)
                {
                    ActiveHealthControllerClass.GClass2103 effect = effectsList[i];
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

        public static void CancelEffects(ManualLogSource logger)
        {
            for (int i = activeHealthEffects.Count - 1; i >= 0; i--)
            {
                if (activeHealthEffects[i].Delay > 0f)
                {
                    if (Plugin.EnableLogging.Value) 
                    {
                        logger.LogWarning("Effect being cancelled = " + activeHealthEffects[i].GetType().ToString());
                    }
                    activeHealthEffects.RemoveAt(i);
                }
            }
        }

        public static void RemoveAllEffectsOfType(IHealthEffect effect)
        {
            activeHealthEffects.RemoveAll(element => element.Equals(effect));
        }

        public static void RemoveAllEffects()
        {
            activeHealthEffects.Clear();
        }

        public static void RestoreHPArossBody(Player player, float hpToRestore, float delay)
        {
            hpToRestore = Mathf.RoundToInt((hpToRestore) / RealismHealthController.BodyParts.Length);

            foreach (EBodyPart part in RealismHealthController.BodyParts)
            {
                HealthRegenEffect regenEffect = new HealthRegenEffect(1f, null, part, player, delay, hpToRestore);
                RealismHealthController.AddCustomEffect(regenEffect, false);
            }
        }

        public static void TrnqtRestoreHPArossBody(Player player, float hpToRestore, float delay, EBodyPart bodyPart)
        {
            hpToRestore = Mathf.RoundToInt((hpToRestore) / (RealismHealthController.BodyParts.Length - 1));

            foreach (EBodyPart part in RealismHealthController.BodyParts)
            {
                if (part != bodyPart) 
                {
                    HealthRegenEffect regenEffect = new HealthRegenEffect(1f, null, part, player, delay, hpToRestore);
                    RealismHealthController.AddCustomEffect(regenEffect, false);
                }
            }
        }

        public static void ResetBleedDamageRecord(Player player) 
        {
            bool hasHeavyBleed = false;
            bool hasLightBleed = false;
   
            foreach (EBodyPart part in RealismHealthController.BodyParts)
            {
                IEnumerable<IEffect> effects = player.ActiveHealthController.GetAllActiveEffects(part);

                if (effects.OfType<GInterface191>().Any()) 
                {
                    hasHeavyBleed = true;
                }
                if (effects.OfType<GInterface190>().Any())
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

        public static void ControllerTick(ManualLogSource logger, Player player)
        {
            if ((int)(Time.time % 9) == 0) 
            {
                DoubleBleedCheck(logger, player);
            }
            if ((int)(Time.time % 3) == 0)
            {
                ResetBleedDamageRecord(player);
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
                MethodInfo addEffectMethod = RealismHealthController.GetAddBaseEFTEffectMethod();
                addEffectMethod.MakeGenericMethod(typeof(ActiveHealthControllerClass).GetNestedType("Pain", BindingFlags.NonPublic | BindingFlags.Instance)).Invoke(player.ActiveHealthController, new object[] { EBodyPart.Chest, 0f, 10f, 1f, 1f, null });
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

        public static IEnumerable<IEffect> GetAllEffectsOnLimb(Player player, EBodyPart part, ref bool hasHeavyBleed, ref bool hasLightBleed, ref bool hasFracture)
        {
            IEnumerable<IEffect> effects = player.ActiveHealthController.GetAllActiveEffects(part);

            hasHeavyBleed = effects.OfType<GInterface191>().Any();
            hasLightBleed = effects.OfType<GInterface190>().Any();
            hasFracture = effects.OfType<GInterface193>().Any();

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
                NotificationManagerClass.DisplayWarningNotification("Can't Eat/Drink, Mouth Is Blocked By Faceshield/NVGs/Mask. Toggle Off Faceshield/NVG Or Remove Mask/Headgear", EFT.Communications.ENotificationDurationType.Long);
                canUse = false;
                return;
            }
            if (Plugin.EnableLogging.Value)
            {
                Logger.LogWarning("juice time");
            }
        }

        public static void CanUseMedItem(ManualLogSource Logger, Player player, EBodyPart bodyPart, Item item, ref bool canUse)
        {
            if (item.Template.Parent._id == "5448f3a64bdc2d60728b456a" || MedProperties.MedType(item) == "drug")
            {
                return;
            }

            if (Plugin.EnableLogging.Value)
            {
                Logger.LogWarning("Checking if CanUseMedItem");
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

            RealismHealthController.GetBodyPartType(bodyPart, ref isNotLimb, ref isHead, ref isBody);

            FaceShieldComponent fsComponent = player.FaceShieldObserver.Component;
            NightVisionComponent nvgComponent = player.NightVisionObserver.Component;
            bool fsIsON = fsComponent != null && (fsComponent.Togglable == null || fsComponent.Togglable.On);
            bool nvgIsOn = nvgComponent != null && (nvgComponent.Togglable == null || nvgComponent.Togglable.On);

            float medHPRes = med.MedKitComponent.HpResource;
            if (Plugin.EnableLogging.Value)
            {
                Logger.LogWarning("remaining hp resource = " + medHPRes);
            }
       
            if (Plugin.GearBlocksEat.Value && medType == "pills" && (mouthBlocked || fsIsON || nvgIsOn)) 
            {
                if (Plugin.EnableLogging.Value)
                {
                    Logger.LogWarning("Pills Blocked, Gear");
                }
                NotificationManagerClass.DisplayWarningNotification("Can't Take Pills, Mouth Is Blocked By Faceshield/NVGs/Mask. Toggle Off Faceshield/NVG Or Remove Mask/Headgear", EFT.Communications.ENotificationDurationType.Long);
                canUse = false;
                return;
            }

            if (Plugin.GearBlocksHeal.Value && ((isBody && hasBodyGear) || (isHead && hasHeadGear))) 
            {
                if (Plugin.EnableLogging.Value)
                {
                    Logger.LogWarning("Med Blocked, Gear");
                }
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

            IEnumerable<IEffect> effects = RealismHealthController.GetAllEffectsOnLimb(player, bodyPart, ref hasHeavyBleed, ref hasLightBleed, ref hasFracture);

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

            foreach (IEffect effect in effects)
            {
                if (Plugin.EnableLogging.Value)
                {
                    Logger.LogWarning("==");
                    Logger.LogWarning("effect type " + effect.Type);
                    Logger.LogWarning("effect body part " + effect.BodyPart);
                    Logger.LogWarning("==");
                }

            }
            if (Plugin.EnableLogging.Value)
            {
                Logger.LogWarning("item = " + item.TemplateId);
                Logger.LogWarning("item name = " + item.LocalizedName());
                Logger.LogWarning("EBodyPart = " + bodyPart);
            }
   
            return;
        }

        public static void DoubleBleedCheck(ManualLogSource logger, Player player)
        {
            IEnumerable<IEffect> commonEffects = player.ActiveHealthController.GetAllActiveEffects(EBodyPart.Common);

            if (commonEffects.OfType<GInterface191>().Any() && commonEffects.OfType<GInterface190>().Any())
            {
                if (Plugin.EnableLogging.Value)
                {
                    logger.LogWarning("H + L Bleed Present Commonly");
                }

                IReadOnlyList<GClass2103> effectsList = (IReadOnlyList<GClass2103>)AccessTools.Property(typeof(ActiveHealthControllerClass), "IReadOnlyList_0").GetValue(player.ActiveHealthController);

                for (int i = effectsList.Count - 1; i >= 0; i--)
                {
                    Type effectType = effectsList[i].Type;
                    EBodyPart effectPart = effectsList[i].BodyPart;
                    ActiveHealthControllerClass.GClass2103 effect = effectsList[i];

                    IEnumerable<IEffect> effects = player.ActiveHealthController.GetAllActiveEffects(effectPart);
                    bool hasHeavyBleed = effects.OfType<GInterface191>().Any();
                    bool hasLightBleed = effects.OfType<GInterface190>().Any();

                    if (hasHeavyBleed && hasLightBleed && effectType == typeof(GInterface190))
                    {
                        if (Plugin.EnableLogging.Value)
                        {
                            logger.LogWarning("removed bleed from " + effectPart);
                        }
     
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

            foreach (EBodyPart part in BodyParts) 
            {
                IEnumerable<IEffect> effects = player.ActiveHealthController.GetAllActiveEffects(part);

                bool isLeftArm = part == EBodyPart.LeftArm;
                bool isRightArm = part == EBodyPart.LeftArm;
                bool isArm = isLeftArm || isRightArm;
                bool isLeg = part == EBodyPart.LeftLeg || part == EBodyPart.RightLeg;
                bool isBody = part == EBodyPart.Chest || part == EBodyPart.Stomach;

                bool hasFracture = effects.OfType<GInterface193>().Any();

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
    }
}
