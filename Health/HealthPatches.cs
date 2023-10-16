using Aki.Reflection.Patching;
using Aki.Reflection.Utils;
using AmplifyMotion;
using BepInEx.Logging;
using Comfort.Common;
using EFT;
using EFT.Ballistics;
using EFT.HealthSystem;
using EFT.InventoryLogic;
using EFT.UI.Health;
using HarmonyLib;
using RealismMod.Health;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using Systems.Effects;
using UnityEngine;
using UnityEngine.Assertions;
using static CW2.Animations.PhysicsSimulator.Val;
using static EFT.Player;
using static RealismMod.Attributes;
using static Systems.Effects.Effects;
using EffectClass = EFT.HealthSystem.ActiveHealthController.GClass2234;
using ExistanceClass = GClass2275;
using StamController = GClass603;
using MedkitTemplate = GInterface249;

namespace RealismMod
{


    public class MedkitConstructorPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(MedKitComponent).GetConstructor(new Type[] { typeof(Item), typeof(MedkitTemplate) });
        }

        private static string getHBTypeString(string type) 
        {
            switch (type) 
            {
                case "trnqt":
                    return "TOURNIQUET";
                case "surg": 
                    return "SURGICAL";
                case "combo":
                    return "TOURNIQUET + CHEST SEAL";
                case "clot":
                    return "CLOTTING AGENT";
                default:
                    return "NONE";
            }
        }

        private static string getPKStrengthString(float str)
        {
            switch (str)
            {
                case 0:
                    return "NONE";
                case <= 5:
                    return "WEAK";
                case <= 10:
                    return "MILD";
                case <= 15:
                    return "STRONG";
                default:
                    return "NONE";
            }
        }

        [PatchPostfix]
        private static void PatchPostfix(MedKitComponent __instance, Item item)
        {
            string medType = MedProperties.MedType(item);

            if (medType == "trnqt" || medType == "medkit" || medType == "surg")
            {
                string hBleedType = MedProperties.HBleedHealType(item);
                float hpPerTick = medType != "surg" ? -MedProperties.HpPerTick(item) : MedProperties.HpPerTick(item);

                if (hBleedType != "none") 
                {
                    List<ItemAttributeClass> hbAtt = item.Attributes;
                    ItemAttributeClass hbAttClass = new ItemAttributeClass(Attributes.ENewItemAttributeId.HBleedType);
                    hbAttClass.Name = ENewItemAttributeId.HBleedType.GetName();
                    hbAttClass.StringValue = () => getHBTypeString(hBleedType);
                    hbAttClass.DisplayType = () => EItemAttributeDisplayType.Compact;
                    hbAttClass.LabelVariations = EItemAttributeLabelVariations.Colored;
                    hbAttClass.LessIsGood = false;
                    hbAtt.Add(hbAttClass);

                    if (medType == "surg")
                    {
                        List<ItemAttributeClass> hpTickAtt = item.Attributes;
                        ItemAttributeClass hpAttClass = new ItemAttributeClass(Attributes.ENewItemAttributeId.HpPerTick);
                        hpAttClass.Name = ENewItemAttributeId.HpPerTick.GetName();
                        hpAttClass.Base = () => hpPerTick;
                        hpAttClass.StringValue = () => hpPerTick.ToString();
                        hpAttClass.DisplayType = () => EItemAttributeDisplayType.Compact;
                        hpAttClass.LabelVariations = EItemAttributeLabelVariations.Colored;
                        hpAttClass.LessIsGood = false;
                        hpTickAtt.Add(hpAttClass);

                        List<ItemAttributeClass> trqntAtt = item.Attributes;
                        ItemAttributeClass trnqtClass = new ItemAttributeClass(Attributes.ENewItemAttributeId.RemoveTrnqt);
                        trnqtClass.Name = ENewItemAttributeId.RemoveTrnqt.GetName();
                        trnqtClass.DisplayType = () => EItemAttributeDisplayType.Compact;
                        trqntAtt.Add(trnqtClass);
                    }
                    else if(hpPerTick != 0)
                    {
                        List<ItemAttributeClass> hpTickAtt = item.Attributes;
                        ItemAttributeClass hpAttClass = new ItemAttributeClass(Attributes.ENewItemAttributeId.LimbHpPerTick);
                        hpAttClass.Name = ENewItemAttributeId.LimbHpPerTick.GetName();
                        hpAttClass.Base = () => hpPerTick;
                        hpAttClass.StringValue = () => hpPerTick.ToString();
                        hpAttClass.DisplayType = () => EItemAttributeDisplayType.Compact;
                        hpAttClass.LabelVariations = EItemAttributeLabelVariations.Colored;
                        hpAttClass.LessIsGood = false;
                        hpTickAtt.Add(hpAttClass);
                    }
                }
            }
            if (medType.Contains("pain"))
            {
                float strength = MedProperties.Strength(item);
                List<ItemAttributeClass> strengthAtt = item.Attributes;
                ItemAttributeClass strengthAttClass = new ItemAttributeClass(Attributes.ENewItemAttributeId.PainKillerStrength);
                strengthAttClass.Name = ENewItemAttributeId.PainKillerStrength.GetName();
                strengthAttClass.StringValue = () => getPKStrengthString(strength);
                strengthAttClass.DisplayType = () => EItemAttributeDisplayType.Compact;
                strengthAttClass.LabelVariations = EItemAttributeLabelVariations.Colored;
                strengthAttClass.LessIsGood = false;
                strengthAtt.Add(strengthAttClass);
            }
        }
    }


    //in-raid healing
    public class ApplyItemPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(PlayerHealthController).GetMethod("ApplyItem", BindingFlags.Instance | BindingFlags.Public);

        }
        [PatchPrefix]
        private static bool Prefix(PlayerHealthController __instance, Item item, EBodyPart bodyPart, ref bool __result)
        {
            MedsClass medsClass;
            FoodClass foodClass;
            bool canUse = true;
            if (__instance.Player.IsYourPlayer && __instance.CanApplyItem(item, bodyPart))
            {
                if (((medsClass = (item as MedsClass)) != null)) 
                {
                    if (Plugin.EnableLogging.Value) 
                    {
                        Logger.LogWarning("ApplyItem Med");
                    }

                    RealismHealthController.CanUseMedItem(Logger, __instance.Player, bodyPart, item, ref canUse);
                }
                if((foodClass = (item as FoodClass)) != null)
                {
                    if (Plugin.EnableLogging.Value)
                    {
                        Logger.LogWarning("ApplyItem Food");
                    }
           
                    if (Plugin.GearBlocksEat.Value) 
                    {
                        RealismHealthController.CanConsume(Logger, __instance.Player, item, ref canUse);
                    }
                }

                __result = canUse;
                return canUse;
            }
            return true;

        }
    }

    //when using quickslot
    public class SetQuickSlotPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(EFT.Player).GetMethod("SetQuickSlotItem", BindingFlags.Instance | BindingFlags.Public);
        }
        [PatchPrefix]
        private static bool Prefix(EFT.Player __instance, EBoundItem quickSlot)
        {
            InventoryControllerClass inventoryCont = (InventoryControllerClass)AccessTools.Property(typeof(EFT.Player), Utils.SetQuickSlotItemInvClassRef).GetValue(__instance);
            Item boundItem = inventoryCont.Inventory.FastAccess.GetBoundItem(quickSlot);
            FoodClass food = boundItem as FoodClass;
            if (boundItem != null && (food = (boundItem as FoodClass)) != null)
            {
                bool canUse = true;
                RealismHealthController.CanConsume(Logger, __instance, boundItem, ref canUse);
                if (Plugin.EnableLogging.Value)
                {
                    Logger.LogWarning("qucik slot, can use = " + canUse);
                }

                return canUse;
            }
            return true;
        }
    }


    public class RestoreBodyPartPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(ActiveHealthController).GetMethod("RestoreBodyPart", BindingFlags.Instance | BindingFlags.Public);

        }

        private static BodyPartStateWrapper GetBodyPartStateWrapper(ActiveHealthController instance, EBodyPart bodyPart)
        {

            PropertyInfo bodyPartStateProperty = typeof(ActiveHealthController).GetProperty("Dictionary_0", BindingFlags.Instance | BindingFlags.NonPublic);
            var bodyPartStateDict = (IDictionary)bodyPartStateProperty.GetMethod.Invoke(instance, null);
            
            object bodyPartStateInstance;
            if (bodyPartStateDict.Contains(bodyPart))
            {
                bodyPartStateInstance = bodyPartStateDict[bodyPart];
            }
            else
            {
                Logger.LogWarning("=======Realism Mod: FAILED TO GET BODYPARTSTATE INSTANCE=========");
                return null;
            }

            return new BodyPartStateWrapper(bodyPartStateInstance);
        }

        [PatchPrefix]
        private static bool Prefix(ActiveHealthController __instance, EBodyPart bodyPart, float healthPenalty, ref bool __result)
        {
            BodyPartStateWrapper bodyPartStateWrapper = GetBodyPartStateWrapper(__instance, bodyPart);
            SkillManager skills = (SkillManager)AccessTools.Field(typeof(ActiveHealthController), "skillManager_0").GetValue(__instance);
            Action<EBodyPart, ValueStruct> bodyPartRestoredField = (Action<EBodyPart, ValueStruct>)AccessTools.Field(typeof(ActiveHealthController), "BodyPartRestoredEvent").GetValue(__instance);
            MethodInfo syncSurgeryPackets = AccessTools.Method(typeof(ActiveHealthController), "method_39");
            MethodInfo syncBodypartPackets = AccessTools.Method(typeof(ActiveHealthController), "method_32");

            if (!bodyPartStateWrapper.IsDestroyed)  
            {
                __result = false;
                return false;
            }

            HealthValue health = bodyPartStateWrapper.Health;
            bodyPartStateWrapper.IsDestroyed = false;
            healthPenalty += (1f - healthPenalty) * skills.SurgeryReducePenalty;
            bodyPartStateWrapper.Health = new HealthValue(1f, Mathf.Max(1f, Mathf.Ceil(bodyPartStateWrapper.Health.Maximum * healthPenalty)), 0f);
            syncSurgeryPackets.Invoke(__instance, new object[] { bodyPart, EDamageType.Medicine });
            syncBodypartPackets.Invoke(__instance, new object[] { bodyPart});
            Action<EBodyPart, ValueStruct> bodyPartRestoredEvent = bodyPartRestoredField;
            if (bodyPartRestoredEvent != null)
            {
                bodyPartRestoredEvent(bodyPart, health.CurrentAndMaximum);
            }
            __result = true;
            return false;
        }
    }

    public class StamRegenRatePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(StamController).GetMethod("method_21", BindingFlags.Instance | BindingFlags.NonPublic);

        }
        [PatchPrefix]
        private static bool Prefix(StamController __instance, float baseValue, ref float __result)
        {
            float[] float_7 = (float[])AccessTools.Field(typeof(StamController), "float_7").GetValue(__instance);
            StamController.EPose epose_0 = (StamController.EPose)AccessTools.Field(typeof(StamController), "epose_0").GetValue(__instance);
            Player player_0 = (Player)AccessTools.Field(typeof(StamController), "player_0").GetValue(__instance);
            float Single_0 = (float)AccessTools.Property(typeof(StamController), "Single_0").GetValue(__instance);

            __result = baseValue * float_7[(int)epose_0] * Singleton<BackendConfigSettingsClass>.Instance.StaminaRestoration.GetAt(player_0.HealthController.Energy.Normalized) * (player_0.Skills.EnduranceBuffRestoration + 1f) * PlayerProperties.HealthStamRegenFactor / Single_0;
            return false;
        }
    }

    public class RemoveEffectPatch : ModulePatch
    {
        private static Type _targetType;
        private static MethodInfo _targetMethod;

        public RemoveEffectPatch()
        {
            _targetType = AccessTools.TypeByName("MedsController");
            _targetMethod = AccessTools.Method(_targetType, "Remove");

        }

        protected override MethodBase GetTargetMethod()
        {
            return _targetMethod;
        }

        [PatchPostfix]
        private static void PatchPostfix()
        {
            if (Plugin.EnableLogging.Value)
            {
                Logger.LogWarning("Cancelling Meds");
            }

            RealismHealthController.CancelEffects();
        }
    }

    public class FlyingBulletPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(FlyingBulletSoundPlayer).GetMethod("method_3", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [PatchPostfix]
        private static void Postfix(FlyingBulletSoundPlayer __instance)
        {
            Player player = Utils.GetPlayer();
            float stressResist = player.Skills.StressPain.Value;
            float painkillerDuration = (float)Math.Round(10f * (1f + stressResist), 2);
            float negativeEffectDuration = (float)Math.Round(15f * (1f - stressResist), 2);
            float negativeEffectStrength = (float)Math.Round(0.9f * (1f - stressResist), 2);
            RealismHealthController.AddAdrenaline(player, painkillerDuration, negativeEffectDuration, negativeEffectStrength);
        }
    }



    public class HCApplyDamagePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(ActiveHealthController).GetMethod("ApplyDamage", BindingFlags.Instance | BindingFlags.Public);
        }

        private static EDamageType[] acceptedDamageTypes = { EDamageType.HeavyBleeding, EDamageType.LightBleeding, EDamageType.Fall, EDamageType.Barbed, EDamageType.Dehydration, EDamageType.Exhaustion };

        [PatchPrefix]
        private static void Prefix(ActiveHealthController __instance, EBodyPart bodyPart, ref float damage, DamageInfo damageInfo)
        {
            if (__instance.Player.IsYourPlayer)
            {
                if (Plugin.EnableLogging.Value) 
                {
                    Logger.LogWarning("=========");
                    Logger.LogWarning("part = " + bodyPart);
                    Logger.LogWarning("type = " + damageInfo.DamageType);
                    Logger.LogWarning("damage = " + damage);
                    Logger.LogWarning("=========");
                }

                EDamageType damageType = damageInfo.DamageType;

                if (acceptedDamageTypes.Contains(damageType))
                {
                    float currentHp = __instance.Player.ActiveHealthController.GetBodyPartHealth(bodyPart).Current;
                    float maxHp = __instance.Player.ActiveHealthController.GetBodyPartHealth(bodyPart).Maximum;
                    float remainingHp = currentHp / maxHp;  
                   
                    if (remainingHp <= 0.5f && (damageType == EDamageType.Dehydration || damageType == EDamageType.Exhaustion))
                    {
                        damage = 0;
                        return;
                    }

                    if (currentHp <= 10f && (bodyPart == EBodyPart.Head || bodyPart == EBodyPart.Chest) && (damageType == EDamageType.LightBleeding))
                    { 
                        damage = 0;
                        return;
                    }

                    float vitalitySkill = __instance.Player.Skills.VitalityBuffSurviobilityInc.Value;
                    float stressResist = __instance.Player.Skills.StressPain.Value;
                    float delay = (float)Math.Round(15f * (1f - vitalitySkill), 2);
                    float tickRate = (float)Math.Round(0.22f * (1f + vitalitySkill), 2);

                    if (damageType == EDamageType.Dehydration)
                    {
                        DamageTracker.TotalDehydrationDamage += damage;
                        return;
                    }
                    if (damageType == EDamageType.Exhaustion)
                    {
                        DamageTracker.TotalExhaustionDamage += damage;
                        return;
                    }
                    if ((damageType == EDamageType.Fall && damage <= 12f))
                    {
                        RealismHealthController.DoPassiveRegen(tickRate, bodyPart, __instance.Player, delay, damage, damageType);
                        return;
                    }
                    if (damageType == EDamageType.Barbed)
                    {
                        RealismHealthController.DoPassiveRegen(tickRate, bodyPart, __instance.Player, delay, damage * 0.75f, damageType);
                        return;
                    }
                    if (damageType == EDamageType.Blunt && damage <= 5f)
                    {
                        RealismHealthController.DoPassiveRegen(tickRate, bodyPart, __instance.Player, delay, damage * 0.75f, damageType);
                        return;
                    }
                    if (damageType == EDamageType.HeavyBleeding || damageType == EDamageType.LightBleeding)
                    {
                        DamageTracker.UpdateDamage(damageType, bodyPart, damage);
                        return;
                    }
                    if (damageType == EDamageType.Bullet || damageType == EDamageType.Explosion || damageType == EDamageType.Landmine || (damageType == EDamageType.Fall && damage >= 16f) || (damageType == EDamageType.Blunt && damage >= 10f)) 
                    {
                        RealismHealthController.RemoveEffectsOfType(EHealthEffectType.HealthRegen);
                    }
                    if (damageType == EDamageType.Bullet || damageType == EDamageType.Blunt || damageType == EDamageType.Melee || damageType == EDamageType.Sniper)
                    {
                        float painkillerDuration = (float)Math.Round(20f * (1f + (stressResist /2)), 2);
                        float negativeEffectDuration = (float)Math.Round(25f * (1f - (stressResist / 2)), 2);
                        float negativeEffectStrength = (float)Math.Round(0.95f * (1f - (stressResist / 2)), 2);
                        RealismHealthController.AddAdrenaline(__instance.Player, painkillerDuration, negativeEffectDuration, negativeEffectStrength);

                    }
                }
            }
        }
    }


    public class ProceedPatch : ModulePatch
    {

        protected override MethodBase GetTargetMethod()
        {
            return typeof(EFT.Player).GetMethod("Proceed", BindingFlags.Instance | BindingFlags.NonPublic, null, new Type[] { typeof(MedsClass), typeof(EBodyPart), typeof(Callback<GInterface116>), typeof(int), typeof(bool) }, null);
        }

        [PatchPrefix]
        private static bool Prefix(Player __instance, MedsClass meds, ref EBodyPart bodyPart)
        {
            string medType = MedProperties.MedType(meds);
            if (__instance.IsYourPlayer && meds.Template._parent != "5448f3a64bdc2d60728b456a")
            {

                if (MedProperties.CanBeUsedInRaid(meds) == false)
                {
                    NotificationManagerClass.DisplayWarningNotification("This Item Can Not Be Used In Raid", EFT.Communications.ENotificationDurationType.Long);
                    return false;
                }

                MedsClass med = meds as MedsClass;
                float medHPRes = med.MedKitComponent.HpResource;

                string hBleedHealType = MedProperties.HBleedHealType(meds);

                bool canHealFract = meds.HealthEffectsComponent.DamageEffects.ContainsKey(EDamageEffectType.Fracture) && ((medType == "medkit" && medHPRes >= 3) || medType != "medkit");
                bool canHealLBleed = meds.HealthEffectsComponent.DamageEffects.ContainsKey(EDamageEffectType.LightBleeding);
                bool canHealHBleed = meds.HealthEffectsComponent.DamageEffects.ContainsKey(EDamageEffectType.HeavyBleeding) && ((medType == "medkit" && medHPRes >= 3) || medType != "medkit");

                if (bodyPart == EBodyPart.Common)
                {
                    EquipmentClass equipment = (EquipmentClass)AccessTools.Property(typeof(Player), "Equipment").GetValue(__instance);

                    Item head = equipment.GetSlot(EquipmentSlot.Headwear).ContainedItem;
                    Item ears = equipment.GetSlot(EquipmentSlot.Earpiece).ContainedItem;
                    Item glasses = equipment.GetSlot(EquipmentSlot.Eyewear).ContainedItem;
                    Item face = equipment.GetSlot(EquipmentSlot.FaceCover).ContainedItem;
                    Item vest = equipment.GetSlot(EquipmentSlot.ArmorVest).ContainedItem;
                    Item tacrig = equipment.GetSlot(EquipmentSlot.TacticalVest).ContainedItem;
                    Item bag = equipment.GetSlot(EquipmentSlot.Backpack).ContainedItem;

                    bool mouthBlocked = RealismHealthController.MouthIsBlocked(head, face, equipment);

                    bool hasBodyGear = vest != null || tacrig != null || bag != null;
                    bool hasHeadGear = head != null || ears != null || face != null;

                    FaceShieldComponent fsComponent = __instance.FaceShieldObserver.Component;
                    NightVisionComponent nvgComponent = __instance.NightVisionObserver.Component;
                    bool fsIsON = fsComponent != null && (fsComponent.Togglable == null || fsComponent.Togglable.On);
                    bool nvgIsOn = nvgComponent != null && (nvgComponent.Togglable == null || nvgComponent.Togglable.On);

                    if (Plugin.GearBlocksHeal.Value && medType.Contains("pills") && (mouthBlocked || fsIsON || nvgIsOn))
                    {
                        NotificationManagerClass.DisplayWarningNotification("Can't Take Pills, Mouth Is Blocked By Faceshield/NVGs/Mask. Toggle Off Faceshield/NVG Or Remove Mask/Headgear", EFT.Communications.ENotificationDurationType.Long);
                        return false;
                    }
                    if (medType.Contains("pain"))
                    {
                        float duration = MedProperties.PainKillerFullDuration(meds);
                        float delay = MedProperties.Delay(meds);
                        float wait = MedProperties.PainKillerWaitTime(meds);
                        float intermittentDur = MedProperties.PainKillerTime(meds);
                        float tunnelVisionStr = MedProperties.TunnelVisionStrength(meds);
                        float painStr = MedProperties.Strength(meds);
                        PainKillerEffect painKillerEffect = new PainKillerEffect(duration, __instance, delay, wait, intermittentDur, tunnelVisionStr, painStr);
                        RealismHealthController.AddCustomEffect(painKillerEffect, false);
                        return true;
                    }
                    if (medType.Contains("pills") || medType.Contains("drug")) 
                    {
                        return true;
                    }

                    Type heavyBleedType;
                    Type lightBleedType;
                    Type fractureType;
                    MedProperties.EffectTypes.TryGetValue("HeavyBleeding", out heavyBleedType);
                    MedProperties.EffectTypes.TryGetValue("LightBleeding", out lightBleedType);
                    MedProperties.EffectTypes.TryGetValue("BrokenBone", out fractureType);

                    foreach (EBodyPart part in RealismHealthController.BodyParts)
                    {
                        bool isHead = false;
                        bool isBody = false;
                        bool isNotLimb = false;

                        RealismHealthController.GetBodyPartType(part, ref isNotLimb, ref isHead, ref isBody);

                        bool hasHeavyBleed = false;
                        bool hasLightBleed = false;
                        bool hasFracture = false;

                        IEnumerable<IEffect> effects = RealismHealthController.GetInjuriesOnBodyPart(__instance, part, ref hasHeavyBleed, ref hasLightBleed, ref hasFracture);

                        float currentHp = __instance.ActiveHealthController.GetBodyPartHealth(part).Current;
                        float maxHp = __instance.ActiveHealthController.GetBodyPartHealth(part).Maximum;

                        if (medType == "surg" && ((isBody && !hasBodyGear) || (isHead && !hasHeadGear) || !isNotLimb))
                        {
                            if (currentHp == 0)
                            {
                                bodyPart = part;
                                break;
                            }
                            continue;
                        }

                        foreach (IEffect effect in effects)
                        {
                            if (Plugin.GearBlocksHeal.Value && ((isBody && hasBodyGear) || (isHead && hasHeadGear)))
                            {
                                continue;
                            }

                            if (canHealHBleed && effect.Type == heavyBleedType)
                            {
                                if (!isNotLimb)
                                {                          
                                    bodyPart = part;
                                    break;
                                }
                                if ((isBody || isHead) && hBleedHealType == "trnqt")
                                {
                                    NotificationManagerClass.DisplayWarningNotification("Tourniquets Can Only Stop Heavy Bleeds On Limbs", EFT.Communications.ENotificationDurationType.Long);

                                    continue;
                                }
                                if ((isBody || isHead) && (hBleedHealType == "clot" || hBleedHealType == "combo" || hBleedHealType == "surg"))
                                {
                                    bodyPart = part;
                                    break;
                                }

                                bodyPart = part;
                                break;
                            }
                            if (canHealLBleed && effect.Type == lightBleedType)
                            {
                                if (!isNotLimb)
                                {
                                    bodyPart = part;
                                    break;
                                }
                                if ((isBody || isHead) && hBleedHealType == "trnqt")
                                {
                                    NotificationManagerClass.DisplayWarningNotification("Tourniquets Can Only Stop Light Bleeds On Limbs", EFT.Communications.ENotificationDurationType.Long);

                                    continue;
                                }
                                if ((isBody || isHead) && hasHeavyBleed)
                                {
                                    continue;
                                }
              
                                bodyPart = part;
                                break;
                            }
                            if (canHealFract && effect.Type == fractureType)
                            {
                                if (!isNotLimb)
                                {
                                    bodyPart = part;
                                    break;
                                }
                                if (isNotLimb)
                                {
                                    NotificationManagerClass.DisplayWarningNotification("Splints Can Only Fix Fractures On Limbs", EFT.Communications.ENotificationDurationType.Long);

                                    continue;
                                }
      
                                bodyPart = part;
                                break;
                            }
                        }

                        if (bodyPart != EBodyPart.Common)
                        {
                            break;
                        }
                    }

                    if (bodyPart == EBodyPart.Common) 
                    {
                        if (medType == "vas") 
                        {
                            return true;
                        }

                        NotificationManagerClass.DisplayWarningNotification("No Suitable Bodypart Was Found For Healing, Gear May Be Covering The Wound.", EFT.Communications.ENotificationDurationType.Long);
                        
                         return false;
                    }
                }

                //determine if any effects should be applied based on what is being healed
                if (bodyPart != EBodyPart.Common)
                {
                   RealismHealthController.HandleHealtheffects(medType, meds, bodyPart, __instance, hBleedHealType, canHealHBleed, canHealLBleed, canHealFract);
                }
            }

            return true;
        }
    }

/*    //THIS IS DOG SHIT, IT WILL AFFECT BOTS, UNUSED
    public class EnergyRatePatch : ModulePatch
    {

        private static Type _targetType;
        private static MethodInfo _targetMethod;

        public EnergyRatePatch()
        {
            _targetType = AccessTools.TypeByName("Existence");
            _targetMethod = AccessTools.Method(_targetType, "method_5");
        }

        protected override MethodBase GetTargetMethod()
        {
            return _targetMethod;
        }

        private static float GetDecayRate(Player player)
        {
            float energyDecayRate = Singleton<BackendConfigSettingsClass>.Instance.Health.Effects.Existence.EnergyLoopTime;
            if (player.HealthController.IsBodyPartDestroyed(EBodyPart.Stomach))
            {
                energyDecayRate /= Singleton<BackendConfigSettingsClass>.Instance.Health.Effects.Existence.DestroyedStomachEnergyTimeFactor;
            }
            return energyDecayRate;
        }

        [PatchPrefix]
        private static bool Prefix(ref float __result)
        {
            if (Utils.IsReady && !Utils.IsInHideout()) 
            {
                Player player = Utils.GetPlayer();
                float num = 1f - player.Skills.HealthHydration;
                __result = Singleton<BackendConfigSettingsClass>.Instance.Health.Effects.Existence.EnergyDamage * num * PlayerProperties.HealthResourceRateFactor / GetDecayRate(player);
                if (Plugin.EnableLogging.Value)
                {
                    Logger.LogWarning("modified energy decay = " + __result);
                    Logger.LogWarning("original energy decay = " + Singleton<BackendConfigSettingsClass>.Instance.Health.Effects.Existence.EnergyDamage * num / GetDecayRate(player));
                }
                return false;
            }
            return true;
 
        }
    }

    //THIS IS DOG SHIT, IT WILL AFFECT BOTS, UNUSED
    public class HydoRatePatch : ModulePatch
    {
        private static Type _targetType;
        private static MethodInfo _targetMethod;

        public HydoRatePatch()
        {
            _targetType = AccessTools.TypeByName("Existence");
            _targetMethod = AccessTools.Method(_targetType, "method_6");
        }

        protected override MethodBase GetTargetMethod()
        {
            return _targetMethod;
        }

        private static float GetDecayRate(Player player)
        {
            float energyDecayRate = Singleton<BackendConfigSettingsClass>.Instance.Health.Effects.Existence.HydrationLoopTime;
            if (player.HealthController.IsBodyPartDestroyed(EBodyPart.Stomach))
            {
                energyDecayRate /= Singleton<BackendConfigSettingsClass>.Instance.Health.Effects.Existence.DestroyedStomachHydrationTimeFactor;
            }
            return energyDecayRate;
        }

        [PatchPrefix]
        private static bool Prefix(ref float __result)
        {
            if (Utils.IsReady && !Utils.IsInHideout())
            {
                Player player = Utils.GetPlayer();
                float num = 1f - player.Skills.HealthHydration;
                __result = Singleton<BackendConfigSettingsClass>.Instance.Health.Effects.Existence.HydrationDamage * num * PlayerProperties.HealthResourceRateFactor / GetDecayRate(player);
                return false;
            }
            return true;
        }
    }*/
}
