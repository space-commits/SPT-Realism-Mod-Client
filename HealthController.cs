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
            if ((int)(Time.time % 10) == 0) 
            {
                logger.LogWarning("Checking Double Bleeds");
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

        public static bool MouthIsBlocked(Item head, Item face)
        {
            bool faceBlocksMouth = false;
            bool headBlocksMouth = false;

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
            bool fsIsON = fsComponent != null && (fsComponent.Togglable == null || fsComponent.Togglable.On);
            bool nvgIsOn = nvgComponent != null && (nvgComponent.Togglable == null || nvgComponent.Togglable.On);

            bool mouthBlocked = MouthIsBlocked(head, face);

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

            bool mouthBlocked = MouthIsBlocked(head, face);

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
            Logger.LogWarning("remeaining hp resource = " + medHPRes);

            if (MedProperties.MedType(item) == "pills" && (mouthBlocked || fsIsON || nvgIsOn)) 
            {
                Logger.LogWarning("Pills Blocked, Gear");

                canUse = false;
                return;
            }

            //will have to make mask exception for moustache and similar
            if ((isBody && hasBodyGear) || (isHead && hasHeadGear))
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

        public static void PlayerInjuryStateCheck(Player player, ManualLogSource logger)
        {
            //get limb % health if about 0 hp, use as factor
            //tremor should be minor factor
            //check if limb has fracture, use as factor
            //also calc movement speed and inertia penalties.
            //keep the the both arms damaged factors as the max reduction values.

            //for aim move speed, I will need to instead create a number that gets subtracted from the base speed.
            //issue with current loop method is that arm factor doubles up...I need to at least reduce the penalty factor depending on which arm it is and
            //type of penalty
            //aim move speed needs to be affected by legs too

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
                float percentHpWalk = 1f - ((1f - percentHp) / (isBody ? 8f : 4f));
                float percentHpSprint = 1f - ((1f - percentHp) / (isBody ? 6f : 3f));
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

   /*         float percentTotalHp = (totalCurrentHp / totalMaxHp);
            float percentTotalHpSprint = 1f - ((1f - percentTotalHp) / 8f);
            float percentTotalHpWalk = 1f - ((1f - percentTotalHp) / 8f);
            float percentTotalHpAimMove = 1f - ((1f - percentTotalHp) / 20f);
            float percentTotalHpADS = 1f - ((1f - percentTotalHp) / 5f);
            float percentTotalHpStance = 1f - ((1f - percentTotalHp) / 2f);
            float percentTotalHpReload = 1f - ((1f - percentTotalHp) / 10f);
            float percentTotalHpRecoil = 1f - ((1f - percentTotalHp) / 20f);
            float percentTotalHpErgo = 1f - ((1f - percentTotalHp) / 2f);*/

            PlayerProperties.AimMoveSpeedBase = Mathf.Max(aimMoveSpeedBase, 0.3f);
            PlayerProperties.ErgoDeltaInjuryMulti = Mathf.Min(ergoDeltaInjuryMulti, 3.5f);
            PlayerProperties.ADSInjuryMulti = Mathf.Max(adsInjuryMulti, 0.4f);
            PlayerProperties.StanceInjuryMulti = Mathf.Max(stanceInjuryMulti, 0.5f);
            PlayerProperties.ReloadInjuryMulti = Mathf.Max(reloadInjuryMulti, 0.7f);
            PlayerProperties.RecoilInjuryMulti = Mathf.Min(recoilInjuryMulti, 1.15f);
            PlayerProperties.HealthSprintSpeedFactor = Mathf.Max(sprintSpeedInjuryMulti, 0.4f);
            PlayerProperties.HealthSprintAccelFactor = Mathf.Max(sprintSpeedInjuryMulti, 0.4f);
            PlayerProperties.HealthWalkSpeedFactor = Mathf.Max(sprintSpeedInjuryMulti, 0.55f);

            if ((int)(Time.time % 5) == 0) 
            {
                player.RaiseChangeSpeedEvent();
            }
  
        }
    }
}
