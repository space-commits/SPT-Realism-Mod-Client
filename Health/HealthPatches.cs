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
using EffectClass = EFT.HealthSystem.ActiveHealthController.GClass2411;
using ExistanceClass = GClass2452;
using StamController = GClass679;
using PhysicalClass = GClass678;
using MedkitTemplate = GInterface296;
using static EFT.HealthSystem.ActiveHealthController;
using static GClass2413;

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
                case >= 15:
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
                    else if (hpPerTick != 0)
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


    public class StimStackPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            Type nestedType = typeof(EFT.HealthSystem.ActiveHealthController).GetNestedType("Stimulator", BindingFlags.NonPublic | BindingFlags.Instance); //get the nested type used by the generic type, Class1885
            Type genericType = typeof(Class1885<>); //declare generic type
            Type constructedType = genericType.MakeGenericType(new Type[] { nestedType }); //construct type at runtime using nested type
            return constructedType.GetMethod("method_0", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static bool Prefix(ref bool __result) //can use dynamic type for instance
        {
            __result = false;
            return false;
        }
    }

    public class BreathIsAudiblePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(PhysicalClass).GetMethod("get_BreathIsAudible", BindingFlags.Instance | BindingFlags.Public);
        }
        [PatchPrefix]
        private static bool Prefix(PhysicalClass __instance,ref bool __result)
        {
            __result = !__instance.HoldingBreath && ((__instance.StaminaParameters.StaminaExhaustionStartsBreathSound && __instance.Stamina.Exhausted) || __instance.Oxygen.Exhausted || Plugin.RealHealthController.HasOverdosed);
            return false;
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

                    Plugin.RealHealthController.CanUseMedItem(__instance.Player, bodyPart, item, ref canUse);
                }
                if ((foodClass = (item as FoodClass)) != null)
                {
                    if (Plugin.EnableLogging.Value)
                    {
                        Logger.LogWarning("ApplyItem Food");
                    }

                    if (Plugin.GearBlocksEat.Value)
                    {
                        Plugin.RealHealthController.CanConsume(__instance.Player, item, ref canUse);
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
                Plugin.RealHealthController.CanConsume(__instance, boundItem, ref canUse);
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

            PropertyInfo bodyPartStateProperty = typeof(ActiveHealthController).GetProperty("Dictionary_0", BindingFlags.Instance | BindingFlags.Public);
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


            //I had to do this previously due to the type being protected, no longer is the case. Keeping for reference.
            /* BodyPartStateWrapper bodyPartStateWrapper = GetBodyPartStateWrapper(__instance, bodyPart);*/

            GClass2412<ActiveHealthController.GClass2411>.BodyPartState bodyPartState = __instance.Dictionary_0[bodyPart];
            SkillManager skills = (SkillManager)AccessTools.Field(typeof(ActiveHealthController), "skillManager_0").GetValue(__instance);
            Action<EBodyPart, ValueStruct> bodyPartRestoredField = (Action<EBodyPart, ValueStruct>)AccessTools.Field(typeof(ActiveHealthController), "BodyPartRestoredEvent").GetValue(__instance);

            if (!bodyPartState.IsDestroyed)
            {
                __result = false;
                return false;
            }

            HealthValue health = bodyPartState.Health;
            bodyPartState.IsDestroyed = false;
            healthPenalty += (1f - healthPenalty) * skills.SurgeryReducePenalty;
            bodyPartState.Health = new HealthValue(1f, Mathf.Max(1f, Mathf.Ceil(bodyPartState.Health.Maximum * healthPenalty)), 0f);
            __instance.method_40(bodyPart, EDamageType.Medicine);
            __instance.method_32(bodyPart);
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
            return typeof(StamController).GetMethod("method_21", BindingFlags.Instance | BindingFlags.Public);

        }
        [PatchPrefix]
        private static bool Prefix(StamController __instance, float baseValue, ref float __result)
        {
            float[] float_7 = (float[])AccessTools.Field(typeof(StamController), "float_7").GetValue(__instance);
            StamController.EPose epose_0 = (StamController.EPose)AccessTools.Field(typeof(StamController), "epose_0").GetValue(__instance);
            Player player_0 = (Player)AccessTools.Field(typeof(StamController), "player_0").GetValue(__instance);
            float Single_0 = (float)AccessTools.Property(typeof(StamController), "Single_0").GetValue(__instance);

            __result = baseValue * float_7[(int)epose_0] * Singleton<BackendConfigSettingsClass>.Instance.StaminaRestoration.GetAt(player_0.HealthController.Energy.Normalized) * (player_0.Skills.EnduranceBuffRestoration + 1f) * PlayerState.HealthStamRegenFactor / Single_0;
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

            Plugin.RealHealthController.CancelPendingEffects();
        }
    }

    public class FlyingBulletPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(FlyingBulletSoundPlayer).GetMethod("method_3", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void Postfix(FlyingBulletSoundPlayer __instance)
        {
            Player player = Utils.GetPlayer();
            float stressResist = player.Skills.StressPain.Value;
            float painkillerDuration = (float)Math.Round(10f * (1f + stressResist), 2);
            float negativeEffectDuration = (float)Math.Round(15f * (1f - stressResist), 2);
            float negativeEffectStrength = (float)Math.Round(0.9f * (1f - stressResist), 2);
            Plugin.RealHealthController.AddAdrenaline(player, painkillerDuration, negativeEffectDuration, negativeEffectStrength);
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
                    int delay = (int)Math.Round(15f * (1f - vitalitySkill), 2);
                    float tickRate = (float)Math.Round(0.22f * (1f + vitalitySkill), 2);

                    if (damageType == EDamageType.Dehydration)
                    {
                        Plugin.RealHealthController.DmgTracker.TotalDehydrationDamage += damage;
                        return;
                    }
                    if (damageType == EDamageType.Exhaustion)
                    {
                        Plugin.RealHealthController.DmgTracker.TotalExhaustionDamage += damage;
                        return;
                    }
                    if ((damageType == EDamageType.Fall && damage <= 12f))
                    {
                        Plugin.RealHealthController.DoPassiveRegen(tickRate, bodyPart, __instance.Player, delay, damage, damageType);
                        return;
                    }
                    if (damageType == EDamageType.Barbed)
                    {
                        Plugin.RealHealthController.DoPassiveRegen(tickRate, bodyPart, __instance.Player, delay, damage * 0.75f, damageType);
                        return;
                    }
                    if (damageType == EDamageType.Blunt && damage <= 5f)
                    {
                        Plugin.RealHealthController.DoPassiveRegen(tickRate, bodyPart, __instance.Player, delay, damage * 0.75f, damageType);
                        return;
                    }
                    if (damageType == EDamageType.HeavyBleeding || damageType == EDamageType.LightBleeding)
                    {
                        Plugin.RealHealthController.DmgTracker.UpdateDamage(damageType, bodyPart, damage);
                        return;
                    }
                    if (damageType == EDamageType.Bullet || damageType == EDamageType.Explosion || damageType == EDamageType.Landmine || (damageType == EDamageType.Fall && damage >= 16f) || (damageType == EDamageType.Blunt && damage >= 10f))
                    {
                        Plugin.RealHealthController.RemoveEffectsOfType(EHealthEffectType.HealthRegen);
                    }
                    if (damageType == EDamageType.Bullet || damageType == EDamageType.Blunt || damageType == EDamageType.Melee || damageType == EDamageType.Sniper)
                    {
                        float painkillerDuration = (float)Math.Round(20f * (1f + (stressResist / 2)), 2);
                        float negativeEffectDuration = (float)Math.Round(25f * (1f - (stressResist / 2)), 2);
                        float negativeEffectStrength = (float)Math.Round(0.95f * (1f - (stressResist / 2)), 2);
                        Plugin.RealHealthController.AddAdrenaline(__instance.Player, painkillerDuration, negativeEffectDuration, negativeEffectStrength);

                    }
                }
            }
        }
    }


    public class ProceedPatch : ModulePatch
    {

        protected override MethodBase GetTargetMethod()
        {
            return typeof(EFT.Player).GetMethod("Proceed", BindingFlags.Instance | BindingFlags.Public, null, new Type[] { typeof(MedsClass), typeof(EBodyPart), typeof(Callback<GInterface130>), typeof(int), typeof(bool) }, null);
        }

        [PatchPrefix]
        private static bool Prefix(Player __instance, MedsClass meds, ref EBodyPart bodyPart)
        {

            if (meds.Template._parent == "5448f3a64bdc2d60728b456a") 
            {

                int duration = (int)meds.HealthEffectsComponent.BuffSettings[0].Duration * 2;
                int delay = (int)meds.HealthEffectsComponent.BuffSettings[0].Delay;
                StimShellEffect stimEffect = new StimShellEffect(__instance, duration, delay, Plugin.RealHealthController.GetStimType(meds.Template._id));
                Plugin.RealHealthController.AddCustomEffect(stimEffect, true);
                Logger.LogWarning("//////////////////parent " + meds.Parent);
                Logger.LogWarning("//////////////////temp parent " + meds.Template.Parent);

    
/*                __instance.HealthController.ApplyItem(meds, EBodyPart.Head, null);
*/                return true;
            }

            if (__instance.IsYourPlayer)
            {
                string medType = MedProperties.MedType(meds);

                if (MedProperties.CanBeUsedInRaid(meds) == false)
                {
                    NotificationManagerClass.DisplayWarningNotification("This Item Can Not Be Used In Raid", EFT.Communications.ENotificationDurationType.Long);
                    return false;
                }

                float medHPRes = meds.MedKitComponent.HpResource;

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

                    bool mouthBlocked = Plugin.RealHealthController.MouthIsBlocked(head, face, equipment);

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
                        int duration = MedProperties.PainKillerDuration(meds);
                        int delay = MedProperties.Delay(meds);
                        float tunnelVisionStr = MedProperties.TunnelVisionStrength(meds);
                        float painKillStr = MedProperties.Strength(meds);

                        PainKillerEffect painKillerEffect = new PainKillerEffect(duration, __instance, delay, tunnelVisionStr, painKillStr);
                        Plugin.RealHealthController.AddCustomEffect(painKillerEffect, true);
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

                    foreach (EBodyPart part in Plugin.RealHealthController.BodyParts)
                    {
                        bool isHead = false;
                        bool isBody = false;
                        bool isNotLimb = false;

                        Plugin.RealHealthController.GetBodyPartType(part, ref isNotLimb, ref isHead, ref isBody);

                        bool hasHeavyBleed = false;
                        bool hasLightBleed = false;
                        bool hasFracture = false;

                        IEnumerable<IEffect> effects = Plugin.RealHealthController.GetInjuriesOnBodyPart(__instance, part, ref hasHeavyBleed, ref hasLightBleed, ref hasFracture);

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
                    Plugin.RealHealthController.HandleHealthEffects(medType, meds, bodyPart, __instance, hBleedHealType, canHealHBleed, canHealLBleed, canHealFract);
                }
            }

            return true;
        }
    }

   /* //IT WILL AFFECT BOTS, UNUSED
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
                __result = Singleton<BackendConfigSettingsClass>.Instance.Health.Effects.Existence.EnergyDamage * num * PlayerStats.HealthResourceRateFactor / GetDecayRate(player);
                if (Plugin.EnableLogging.Value)
                {
                    Logger.LogWarning("modified energy decay = " + __result);
                    Logger.LogWarning("original energy decay = " + Singleton<BackendConfigSettingsClass>.Instance.Health.Effects.Existence.EnergyDamage * num / GetDecayRate(player));
                }
                return false;
            }
            return true;

        }
    }*/

/*    //IT WILL AFFECT BOTS, UNUSED
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
                __result = Singleton<BackendConfigSettingsClass>.Instance.Health.Effects.Existence.HydrationDamage * num * PlayerStats.HealthResourceRateFactor / GetDecayRate(player);
                return false;
            }
            return true;
        }
    }*/
}
