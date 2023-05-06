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
                return 1;
            }
            return float.Parse(med.ConflictingItems[3]);
        }
    }

    public static class RealismHealthController
    {
        public static EBodyPart[] BodyParts = { EBodyPart.Head, EBodyPart.Chest, EBodyPart.Stomach, EBodyPart.RightLeg, EBodyPart.LeftLeg, EBodyPart.RightArm, EBodyPart.LeftArm };

        private static List<IHealthEffect> activeHealthEffects = new List<IHealthEffect>();

        public static void AddBaseEFTEffect(int partIndex, Player player, String effect)
        {
            MethodInfo effectMethod = typeof(ActiveHealthControllerClass).GetMethods(BindingFlags.NonPublic | BindingFlags.Instance).First(m =>
            m.GetParameters().Length == 6
            && m.GetParameters()[0].Name == "bodyPart"
            && m.GetParameters()[5].Name == "initCallback"
            && m.IsGenericMethod);

            effectMethod.MakeGenericMethod(typeof(ActiveHealthControllerClass).GetNestedType(effect,BindingFlags.NonPublic | BindingFlags.Instance)).Invoke(player.ActiveHealthController, new object[] { (EBodyPart)partIndex, null, null, null, null, null });
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

        public static void CancelEffects(ManualLogSource logger)
        {
            for (int i = activeHealthEffects.Count - 1; i >= 0; i--)
            {
                if (activeHealthEffects[i].Delay > 0f)
                {
                    logger.LogWarning("Effect being cancelled = " + activeHealthEffects[i].GetType().ToString());
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

        public static void ControllerTick(ManualLogSource logger, Player player)
        {
            if ((int)(Time.time % 9) == 0) 
            {
                RealismHealthController.DoubleBleedCheck(logger, player);
            }

            for (int i = activeHealthEffects.Count - 1; i >= 0; i--)
            {
                IHealthEffect effect = activeHealthEffects[i];
                logger.LogWarning("Type = " + effect.GetType().ToString());
                logger.LogWarning("Delay = " + effect.Delay);
                effect.Delay = effect.Delay > 0 ? effect.Delay - 1f : effect.Delay;

                if ((int)(Time.time % 5) == 0) 
                {
                    if (effect.Duration == null || effect.Duration > 0f)
                    {
                        logger.LogWarning("Ticking Effect");
                        effect.Tick();
                    }
                    else
                    {
                        logger.LogWarning("Removing Effect Due to Duration");
                        activeHealthEffects.RemoveAt(i);
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
                    headBlocksMouth = GearProperties.BlocksMouth(item) ? true : false;
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
                Logger.LogWarning("juice denied");
                canUse = false;
                return;
            }
            Logger.LogWarning("juice time");
        }

        public static void CanUseMedItem(ManualLogSource Logger, Player player, EBodyPart bodyPart, Item item, ref bool canUse)
        {
            if (item.Template.Parent._id == "5448f3a64bdc2d60728b456a" || MedProperties.MedType(item) == "drug")
            {
                Logger.LogWarning("Is drug/stim");
                return;
            }

            Logger.LogWarning("Checking if CanUseMedItem");

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

            RealismHealthController.GetBodyPartType(bodyPart, ref isNotLimb, ref isHead, ref isBody);

            FaceShieldComponent fsComponent = player.FaceShieldObserver.Component;
            NightVisionComponent nvgComponent = player.NightVisionObserver.Component;
            bool fsIsON = fsComponent != null && (fsComponent.Togglable == null || fsComponent.Togglable.On);
            bool nvgIsOn = nvgComponent != null && (nvgComponent.Togglable == null || nvgComponent.Togglable.On);

            float medHPRes = med.MedKitComponent.HpResource;
            Logger.LogWarning("remaining hp resource = " + medHPRes);

            if (Plugin.GearBlocksEat.Value && MedProperties.MedType(item) == "pills" && (mouthBlocked || fsIsON || nvgIsOn)) 
            {
                Logger.LogWarning("Pills Blocked, Gear");

                canUse = false;
                return;
            }

            if (Plugin.GearBlocksHeal.Value && ((isBody && hasBodyGear) || (isHead && hasHeadGear))) 
            {
                Logger.LogWarning("Med Blocked, Gear");

                canUse = false;
                return;
            }


            Logger.LogWarning("==============");
            Logger.LogWarning("GClass2106");

            bool hasHeavyBleed = false;
            bool hasLightBleed = false;
            bool hasFracture = false;

            IEnumerable<IEffect> effects = RealismHealthController.GetAllEffectsOnLimb(player, bodyPart, ref hasHeavyBleed, ref hasLightBleed, ref hasFracture);

            if (hasHeavyBleed && isNotLimb && MedProperties.HBleedHealType(item) == "trnqt" && medHPRes >= 3)
            {
                canUse = false;
                return;
            }

            if (MedProperties.MedType(item) == "splint" && med.HealthEffectsComponent.DamageEffects.ContainsKey(EDamageEffectType.Fracture) && isNotLimb)
            {
                canUse = false;
                return;
            }

            if (MedProperties.MedType(item) == "medkit" && med.HealthEffectsComponent.DamageEffects.ContainsKey(EDamageEffectType.Fracture) && hasFracture && isNotLimb && !hasHeavyBleed && !hasLightBleed)
            {
                canUse = false;
                return;
            }

            foreach (IEffect effect in effects)
            {
                Logger.LogWarning("==");
                Logger.LogWarning("effect type " + effect.Type);
                Logger.LogWarning("effect body part " + effect.BodyPart);
                Logger.LogWarning("==");
            }

            Logger.LogWarning("item = " + item.TemplateId);
            Logger.LogWarning("item name = " + item.LocalizedName());
            Logger.LogWarning("EBodyPart = " + bodyPart);
            Logger.LogWarning("==============");
            return;
        }

        public static void DoubleBleedCheck(ManualLogSource logger, Player player)
        {
            IEnumerable<IEffect> commonEffects = player.ActiveHealthController.GetAllActiveEffects(EBodyPart.Common);

            if (commonEffects.OfType<GInterface191>().Any() && commonEffects.OfType<GInterface190>().Any())
            {
                logger.LogWarning("H + L Bleed Present Commonly");


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
                        logger.LogWarning("removed bleed from " + effectPart);
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
                float fractureFactor = hasFracture ? 0.5f : 1f;

                float currentHp = player.ActiveHealthController.GetBodyPartHealth(part).Current;
                float maxHp = player.ActiveHealthController.GetBodyPartHealth(part).Maximum;
                totalMaxHp += maxHp;
                totalCurrentHp += currentHp;

                float percentHp = (currentHp / maxHp);
                float percentHpStamRegen = 1f - ((1f - percentHp) / (isBody ? 10f : 5f));
                float percentHpWalk = 1f - ((1f - percentHp) / (isBody ? 10f : 5f));
                float percentHpSprint = 1f - ((1f - percentHp) / (isBody ? 8f : 4f));
                float percentHpAimMove = 1f - ((1f - percentHp) / (isArm ? 20f : 10f));
                float percentHpADS = 1f - ((1f - percentHp) / (isRightArm ? 2f : 5f));
                float percentHpStance = 1f - ((1f - percentHp) / (isRightArm ? 3f : 6f));
                float percentHpReload = 1f - ((1f - percentHp) / (isLeftArm ? 6f : 8f));
                float percentHpRecoil = 1f - ((1f - percentHp) / (isLeftArm ? 10f : 20f));

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
                    aimMoveSpeedBase = aimMoveSpeedBase * percentHpAimMove * fractureFactor;
                    ergoDeltaInjuryMulti = ergoDeltaInjuryMulti * ( 1f + (1f - percentHp)) * fractureFactor;
                    adsInjuryMulti = adsInjuryMulti * percentHpADS * fractureFactor;
                    stanceInjuryMulti = stanceInjuryMulti * percentHpStance * fractureFactor; 
                    reloadInjuryMulti = reloadInjuryMulti * percentHpReload * fractureFactor; 
                    recoilInjuryMulti = recoilInjuryMulti * (1f + (1f - percentHpRecoil)) * fractureFactor; 

                    if (isLeftArm) 
                    {
                        PlayerProperties.LeftArmRuined = player.ActiveHealthController.GetBodyPartHealth(EBodyPart.LeftArm).Current <= 10 || hasFracture;
                    }
                    if (isRightArm)
                    {
                        PlayerProperties.RightArmRuined = player.ActiveHealthController.GetBodyPartHealth(EBodyPart.RightArm).Current <= 10 || hasFracture;
                    }
                }
            }
            float totalHpPercent = totalCurrentHp / totalMaxHp;
            resourceRateInjuryMulti = 1f - totalHpPercent;

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
            PlayerProperties.HealthWalkSpeedFactor = Mathf.Max(walkSpeedInjuryMulti * percentEnergyWalk, 0.55f * percentHydroLowerLimit);
            PlayerProperties.HealthStamRegenFactor = Mathf.Max(stamRegenInjuryMulti * percentEnergyStamRegen, 0.5f * percentHydroLowerLimit);


            lastCurrentHydro = lastCurrentHydro == 0 ? currentHydro : lastCurrentHydro;
            lastCurrentEnergy = lastCurrentEnergy == 0 ? currentEnergy : lastCurrentEnergy;

            float hydroDiff = Math.Abs(currentHydro - lastCurrentHydro) / currentHydro;
            float energyDiff = Math.Abs(currentEnergy - lastCurrentEnergy) / currentEnergy;

            if (((int)(Time.time % 10) == 0 && totalCurrentHp != lastCurrentTotalHp) || energyDiff > 0.05f || hydroDiff > 0.05f) 
            {

/*                logger.LogWarning("AimMoveSpeedBase " + PlayerProperties.AimMoveSpeedBase);
                logger.LogWarning("ErgoDeltaInjuryMulti " + PlayerProperties.ErgoDeltaInjuryMulti);
                logger.LogWarning("ADSInjuryMulti " + PlayerProperties.ADSInjuryMulti);
                logger.LogWarning("StanceInjuryMulti " + PlayerProperties.StanceInjuryMulti);
                logger.LogWarning("ReloadInjuryMulti " + PlayerProperties.ReloadInjuryMulti);
                logger.LogWarning("RecoilInjuryMulti " + PlayerProperties.RecoilInjuryMulti);
                logger.LogWarning("HealthSprintSpeedFactor " + PlayerProperties.HealthSprintSpeedFactor);
                logger.LogWarning("HealthSprintAccelFactor " + PlayerProperties.HealthSprintAccelFactor);
                logger.LogWarning("HealthWalkSpeedFactor " + PlayerProperties.HealthWalkSpeedFactor);
                logger.LogWarning("HealthStamRegenFactor " + PlayerProperties.HealthStamRegenFactor);*/
   /*             logger.LogWarning("LeftArmRuined " + PlayerProperties.LeftArmRuined);
                logger.LogWarning("RightArmRuined " + PlayerProperties.RightArmRuined);
                logger.LogWarning("percentHydro " + percentHydro);
                logger.LogWarning("percentEnergy " + percentEnergy);
                logger.LogWarning("totalCurrentHp " + totalCurrentHp);*/
                lastCurrentTotalHp = totalCurrentHp;
                lastCurrentEnergy = currentEnergy; 
                lastCurrentHydro = currentHydro;
                player.RaiseChangeSpeedEvent();

                //will need to get current rate, current modifier, and per tick I need to work out what the true base 
                //rate is before applying modifier again, otherwise it will just increase.


                PropertyInfo energyProp = typeof(ActiveHealthControllerClass).GetProperty("EnergyRate");
                float currentEnergyRate = (float)energyProp.GetValue(player.ActiveHealthController);
                float prevEnergyRate = currentEnergyRate - PlayerProperties.HealthResourceRateFactor;

                logger.LogWarning("currentEnergyRate " + currentEnergyRate);
                logger.LogWarning("prevEnergyRate " + prevEnergyRate);

                PropertyInfo hydroProp = typeof(ActiveHealthControllerClass).GetProperty("HydrationRate");
                float currentHydroRate = (float)hydroProp.GetValue(player.ActiveHealthController);
                float prevHydroRate = currentHydroRate - PlayerProperties.HealthResourceRateFactor;

                PlayerProperties.HealthResourceRateFactor = -resourceRateInjuryMulti;
                logger.LogWarning("HealthResourceRateFactor " + PlayerProperties.HealthResourceRateFactor);

                float newEnergyRate = prevEnergyRate + PlayerProperties.HealthResourceRateFactor;
                energyProp.SetValue(player.ActiveHealthController, newEnergyRate, null);

                float newHydroRate = prevHydroRate + PlayerProperties.HealthResourceRateFactor;
                hydroProp.SetValue(player.ActiveHealthController, newHydroRate, null);

                logger.LogWarning("newEnergyRate " + newEnergyRate);

            }
  
        }
    }
}
