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

    public interface IHealthEffect
    {
        public EBodyPart BodyPart { get; set; }
        public int Duration { get; }
        public float TimeExisted { get; set; }
        public bool IsActive { get; set; }
        public void Tick();
        public Player Player { get; }
    }

    public class TourniquetEffect : IHealthEffect
    {
        public EBodyPart BodyPart { get; set; }
        public int Duration { get; }
        public float TimeExisted { get; set; }
        public bool IsActive { get; set; }
        public float DamagePerTick { get; }
        public Player Player { get; }

        public TourniquetEffect(float dmgTick, int dur, EBodyPart part, Player player) 
        {
            TimeExisted = 0;
            IsActive = true;
            DamagePerTick = dmgTick;
            Duration = dur;
            BodyPart = part;
            Player = player;
        }

        public void Tick()
        {
            float currentPartHP = Player.ActiveHealthController.GetBodyPartHealth(BodyPart).Current;

            if (currentPartHP >= 20f) 
            {
                Player.ActiveHealthController.ChangeHealth(BodyPart, DamagePerTick, default);
            }
        }
    }

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
    }

    public static class RealismHealthController
    {
        public static EBodyPart[] BodyParts = { EBodyPart.Head, EBodyPart.Chest, EBodyPart.Stomach, EBodyPart.RightLeg, EBodyPart.LeftLeg, EBodyPart.RightArm, EBodyPart.LeftArm};

        private static List<IHealthEffect> _activeHealthEffects;

        public static void AddBaseEFTEffect(int partIndex, Player player, String effect) 
        {
            MethodInfo effectMethod = typeof(ActiveHealthControllerClass).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic).First(m => 
            m.GetParameters().Length == 6 
            && m.GetParameters()[0].Name == "bodyPart"
            && m.GetParameters()[5].Name == "initCallback"
            && m.IsGenericMethod);

            effectMethod.MakeGenericMethod(typeof(ActiveHealthControllerClass).GetNestedType(effect, BindingFlags.Instance | BindingFlags.NonPublic)).Invoke(player.ActiveHealthController, new object[] { (EBodyPart)partIndex, null, null, null, null, null });
        }

        public static void AddCustomEffect(IHealthEffect effect, bool canStack)
        {
            //need to decide if it's better to keep the old effect or to replace it with a new one.
            if (!canStack)
            {
                foreach (IHealthEffect eff in _activeHealthEffects)
                {
                    if (eff == effect && eff.BodyPart == effect.BodyPart)
                    {
                        return;
                    }
                }
            }
            else
            {
                _activeHealthEffects.Add(effect);
            }
        }

        public static void RemoveEffect(IHealthEffect effect, EBodyPart bodyPart)
        {
            for (int i = _activeHealthEffects.Count - 1; i >= 0; i--)
            {
                if (_activeHealthEffects[i] == effect && _activeHealthEffects[i].BodyPart == bodyPart)
                {
                    _activeHealthEffects.RemoveAt(i);
                }
            }
        }

        public static void RemoveAllEffectsOfType(IHealthEffect effect)
        {
            _activeHealthEffects.RemoveAll(element => element.Equals(effect));
        }

        public static void RemoveAllEffects()
        {
            _activeHealthEffects.Clear();
        }

        public static void TickEffects()
        {
            for (int i = _activeHealthEffects.Count - 1; i >= 0; i--)
            {
                IHealthEffect effect = _activeHealthEffects[i];
                if (effect.IsActive)
                {
                    effect.Tick();
                }
                else
                {
                    _activeHealthEffects.RemoveAt(i);
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

            bool isHead = bodyPart == EBodyPart.Head;
            bool isBody = bodyPart == EBodyPart.Chest || bodyPart == EBodyPart.Stomach;
            bool isNotLimb = bodyPart == EBodyPart.Chest || bodyPart == EBodyPart.Stomach || bodyPart == EBodyPart.Head;

            FaceShieldComponent fsComponent = player.FaceShieldObserver.Component;
            NightVisionComponent nvgComponent = player.NightVisionObserver.Component;
            bool fsIsON = fsComponent != null && (fsComponent.Togglable == null || fsComponent.Togglable.On);
            bool nvgIsOn = nvgComponent != null && (nvgComponent.Togglable == null || nvgComponent.Togglable.On);

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

            IEnumerable<IEffect> effects = player.ActiveHealthController.GetAllActiveEffects(bodyPart);
            bool hasHeavyBleed = effects.OfType<GInterface191>().Any();
            bool hasLightBleed = effects.OfType<GInterface190>().Any();
            bool hasFracture = effects.OfType<GInterface193>().Any();

            if (hasHeavyBleed && isNotLimb && MedProperties.HBleedHealType(item) == "trnqt")
            {
                canUse = false;
                return;
            }

            if (MedProperties.MedType(item) == "splint" && med.HealthEffectsComponent.DamageEffects.ContainsKey(EDamageEffectType.Fracture) && isNotLimb)
            {
                canUse = false;
                return;
            }

            if (MedProperties.MedType(item) == "medkit" && med.HealthEffectsComponent.DamageEffects.ContainsKey(EDamageEffectType.Fracture) && hasFracture && !hasHeavyBleed && !hasLightBleed && isNotLimb)
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
            bool rightArmDamaged = player.MovementContext.PhysicalConditionIs(EPhysicalCondition.RightArmDamaged);
            bool leftArmDamaged = player.MovementContext.PhysicalConditionIs(EPhysicalCondition.LeftArmDamaged);
            bool tremor = player.MovementContext.PhysicalConditionIs(EPhysicalCondition.Tremor);

            PlayerProperties.RightArmBlacked = rightArmDamaged;
            PlayerProperties.LeftArmBlacked = leftArmDamaged;

            if (!rightArmDamaged && !leftArmDamaged && !tremor)
            {
                PlayerProperties.AimMoveSpeedBase = 0.42f;
                PlayerProperties.ErgoDeltaInjuryMulti = 1f;
                PlayerProperties.ADSInjuryMulti = 1f;
                PlayerProperties.StanceInjuryMulti = 1f;
                PlayerProperties.ReloadInjuryMulti = 1f;
                PlayerProperties.RecoilInjuryMulti = 1f;
            }
            if (tremor == true)
            {
                PlayerProperties.AimMoveSpeedBase = 0.4f;
                PlayerProperties.ErgoDeltaInjuryMulti = 1.15f;
                PlayerProperties.ADSInjuryMulti = 0.85f;
                PlayerProperties.StanceInjuryMulti = 0.85f;
                PlayerProperties.ReloadInjuryMulti = 0.9f;
                PlayerProperties.RecoilInjuryMulti = 1.025f;
            }
            if ((rightArmDamaged == true && !leftArmDamaged))
            {
                PlayerProperties.AimMoveSpeedBase = 0.38f;
                PlayerProperties.ErgoDeltaInjuryMulti = 1.5f;
                PlayerProperties.ADSInjuryMulti = 0.51f;
                PlayerProperties.StanceInjuryMulti = 0.6f;
                PlayerProperties.ReloadInjuryMulti = 0.85f;
                PlayerProperties.RecoilInjuryMulti = 1.05f;
            }
            if ((!rightArmDamaged && leftArmDamaged == true))
            {
                PlayerProperties.AimMoveSpeedBase = 0.34f;
                PlayerProperties.ErgoDeltaInjuryMulti = 2f;
                PlayerProperties.ADSInjuryMulti = 0.59f;
                PlayerProperties.StanceInjuryMulti = 0.7f;
                PlayerProperties.ReloadInjuryMulti = 0.8f;
                PlayerProperties.RecoilInjuryMulti = 1.1f;
            }
            if (rightArmDamaged == true && leftArmDamaged == true)
            {
                if (Plugin.EnableLogging.Value == true)
                {
                    logger.LogWarning("both arms damaged");
                }
                PlayerProperties.AimMoveSpeedBase = 0.3f;
                PlayerProperties.ErgoDeltaInjuryMulti = 3.5f;
                PlayerProperties.ADSInjuryMulti = 0.42f;
                PlayerProperties.StanceInjuryMulti = 0.5f;
                PlayerProperties.ReloadInjuryMulti = 0.75f;
                PlayerProperties.RecoilInjuryMulti = 1.15f;
            }
        }
    }
}
