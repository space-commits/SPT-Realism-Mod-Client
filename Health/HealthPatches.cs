using Aki.Reflection.Patching;
using Comfort.Common;
using EFT;
using EFT.HealthSystem;
using EFT.InventoryLogic;
using EFT.UI.Health;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static EFT.HealthSystem.ActiveHealthController;
using static RealismMod.Attributes;
using ExistanceClass = GClass2456;
using HealthStateClass = GClass2416<EFT.HealthSystem.ActiveHealthController.GClass2415>;
using MedkitTemplate = IMedkitResource;
using MedUseStringClass = GClass1235;
using PhysicalClass = GClass681;
using StamController = GClass682;

namespace RealismMod
{
    public class HealthPanelPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(HealthParametersPanel).GetMethod("method_0", BindingFlags.Instance | BindingFlags.Public);
        }

        private static Color GetCurrentColor(float level) 
        {
            switch(level) 
            {
                case 0:
                    return Color.white;
                case <= 0.25f:
                    return Color.yellow;
                case <= 0.5f:
                    return new Color(1.0f, 0.647f, 0.0f);
                case <= 0.75f:
                    return new Color(1.0f, 0.25f, 0.0f);
                case <= 1f:
                    return Color.red;
                default: 
                    return Color.white;    
            }
        }

        private static Color GetRateColor(float level)
        {
            switch (level)
            {
                case < 0:
                    return Color.green;
                case 0:
                    return new Color(0.4549f, 0.4824f, 0.4941f, 1f);
                case <= 0.25f:
                    return Color.yellow;
                case <= 0.5f:
                    return new Color(1.0f, 0.647f, 0.0f);
                case <= 0.75f:
                    return new Color(1.0f, 0.25f, 0.0f);
                case <= 1f:
                    return Color.red;
                default:
                    return Color.white;
            }
        }

        [PatchPostfix]
        private static void Postfix(HealthParametersPanel __instance)
        {
            HealthParameterPanel _radiation = (HealthParameterPanel)AccessTools.Field(typeof(HealthParametersPanel), "_radiation").GetValue(__instance);
            GameObject panel = __instance.gameObject;
            if (panel.transform.childCount > 0)
            {
                GameObject poisoning = panel.transform.Find("Poisoning")?.gameObject;
                if (poisoning != null)
                {
                    GameObject buff = poisoning.transform.Find("Buff")?.gameObject;
                    GameObject current = poisoning.transform.Find("Current")?.gameObject;
                    if (buff != null)
                    {
                        float toxicityRate = Plugin.RealHealthController.DmgeTracker.ToxicityRate;
                        //can animate it by changing the font size with ping pong, and modulate the color
#pragma warning disable CS0618 
                        CustomTextMeshProUGUI buffUI = buff.GetComponent<CustomTextMeshProUGUI>();
#pragma warning restore CS0618 
                        buffUI.text = toxicityRate.ToString("0.00");
                        buffUI.color = GetCurrentColor(toxicityRate);
                        buffUI.fontSize = Plugin.test1.Value;
                    }
                    if (current != null) 
                    {
                        Utils.Logger.LogWarning("found Current");
                        float toxicityLevel = Plugin.RealHealthController.DmgeTracker.TotalToxicity;
                        //can animate it by changing the font size with ping pong, and modulate the color
#pragma warning disable CS0618 
                        CustomTextMeshProUGUI buffUI = buff.GetComponent<CustomTextMeshProUGUI>();
#pragma warning restore CS0618 
                        buffUI.text = toxicityLevel.ToString();
                        buffUI.color = GetCurrentColor(toxicityLevel);
                        buffUI.fontSize = Plugin.test2.Value;
                    }
                }
            }

 /*           if (_radiation != null)
            {
                _radiation.SetParameterValue(new ValueStruct
                {
                    Current = Plugin.test1.Value,
                    Minimum = 0f,
                    Maximum = 100f
                }, Plugin.test2.Value, 0, true);

            }*/

        }
    }

    public class HealCostDisplayShortPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(MedUseStringClass).GetMethod("GetStringValue", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static bool Prefix(MedUseStringClass __instance, ref string __result)
        {
            StringBuilder stringBuilder = new StringBuilder();
            if (__instance.Delay > 1f)
            {
                stringBuilder.Append(string.Format("{0} {1}{2}", "Del.".Localized(null), __instance.Delay, "sec".Localized(null)));
            }
            if (__instance.Duration > 0f)
            {
                if (stringBuilder.Length > 0)
                {
                    stringBuilder.Append(" / ");
                }
                stringBuilder.Append(string.Format("{0} {1}{2}", "Dur.".Localized(null), __instance.Duration, "sec".Localized(null)));
            }
            if (__instance.Cost > 0)
            {
                if (stringBuilder.Length > 0)
                {
                    stringBuilder.Append(" / ");
                }
                stringBuilder.Append((__instance.Cost + 1) + " HP");
            }
            __result = stringBuilder.ToString();
            return false;
        }
    }

    public class HealCostDisplayFullPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(MedUseStringClass).GetMethod("GetFullStringValue", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static bool Prefix(MedUseStringClass __instance, string displayName, ref string __result)
        {
            if (__instance.Delay.IsZero() && __instance.Duration.IsZero() && __instance.Cost == 0)
            {
                __result = string.Empty;
            }
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(displayName.Localized(null));
            if (__instance.Delay > 1f)
            {
                stringBuilder.Append(string.Format("\n{0} {1}{2}", "Delay".Localized(null), __instance.Delay, "sec".Localized(null)));
            }
            if (__instance.Duration > 0f)
            {
                stringBuilder.Append(string.Format("\n{0} {1}{2}", "Duration".Localized(null), (__instance.Duration + 1), "sec".Localized(null)));
            }
            if (__instance.Cost > 0)
            {
                stringBuilder.Append("\n" + (__instance.Cost + 1) + " HP");
            }
            __result = stringBuilder.ToString();
            return false;
        }
    }

    public class HealthEffectsConstructorPatch : ModulePatch
    {
        private static List<string> modifiedMeds = new List<string>();  

        protected override MethodBase GetTargetMethod()
        {
            return typeof(HealthEffectsComponent).GetConstructor(new Type[] { typeof(Item), typeof(IHealthEffect) });
        }

        [PatchPostfix]
        private static void PatchPostfix(HealthEffectsComponent __instance, Item item)
        {
            string medType = MedProperties.MedType(item);
            if (item.Template._parent == "5448f3a64bdc2d60728b456a")
            {
                List<ItemAttributeClass> stimAtt = item.Attributes;
                ItemAttributeClass stimAttClass = new ItemAttributeClass(Attributes.ENewItemAttributeId.StimType);
                stimAttClass.Name = ENewItemAttributeId.StimType.GetName();
                stimAttClass.StringValue = () => Plugin.RealHealthController.GetStimType(item.TemplateId).ToString();
                stimAttClass.DisplayType = () => EItemAttributeDisplayType.Compact;
                stimAttClass.LabelVariations = EItemAttributeLabelVariations.Colored;
                stimAttClass.LessIsGood = false;
                stimAtt.Add(stimAttClass);
            }
        }
    }

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
                case > 15:
                    return "VERY STRONG";
                default:
                    return "NONE";
            }
        }

        [PatchPostfix]
        private static void PatchPostfix(MedKitComponent __instance, Item item)
        {
            string medType = MedProperties.MedType(item);

            if (item.Template._parent == "5448f3a64bdc2d60728b456a") 
            {
                List<ItemAttributeClass> stimAtt = item.Attributes;
                ItemAttributeClass stimAttClass = new ItemAttributeClass(Attributes.ENewItemAttributeId.StimType);
                stimAttClass.Name = ENewItemAttributeId.StimType.GetName();
                stimAttClass.StringValue = () => Plugin.RealHealthController.GetStimType(item.TemplateId).ToString();
                stimAttClass.DisplayType = () => EItemAttributeDisplayType.Compact;
                stimAttClass.LabelVariations = EItemAttributeLabelVariations.Colored;
                stimAttClass.LessIsGood = false;
                stimAtt.Add(stimAttClass);
            }

            if (medType == "trnqt" || medType == "medkit" || medType == "surg")
            {
                string hBleedType = MedProperties.HBleedHealType(item);
                float hpPerTick = medType != "surg" ? -MedProperties.HpPerTick(item) : MedProperties.HpPerTick(item);

                if (medType == "medkit") 
                {
                    float hp = MedProperties.HPRestoreAmount(item);
                    List<ItemAttributeClass> hbAtt = item.Attributes;
                    ItemAttributeClass hpAttClass = new ItemAttributeClass(Attributes.ENewItemAttributeId.OutOfRaidHP);
                    hpAttClass.Name = ENewItemAttributeId.OutOfRaidHP.GetName();
                    hpAttClass.Base = () => hp;
                    hpAttClass.StringValue = () => hp.ToString();
                    hpAttClass.DisplayType = () => EItemAttributeDisplayType.Compact;
                    hpAttClass.LabelVariations = EItemAttributeLabelVariations.Colored;
                    hpAttClass.LessIsGood = false;
                    hbAtt.Add(hpAttClass);
                }

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

    public class StimStackPatch2 : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            Type nestedType = typeof(EFT.HealthSystem.ActiveHealthController).GetNestedType("Stimulator", BindingFlags.NonPublic | BindingFlags.Instance); //get the nested type used by the generic type, Class1885
            Type genericType = typeof(Class1884<>); //declare generic type
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

    public class StimStackPatch1 : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            Type nestedType = typeof(EFT.HealthSystem.ActiveHealthController).GetNestedType("Stimulator", BindingFlags.NonPublic | BindingFlags.Instance); //get the nested type used by the generic type, Class1885
            Type genericType = typeof(Class1885<>); //declare generic type
            Type constructedType = genericType.MakeGenericType(new Type[] { nestedType }); //construct type at runtime using nested type
            return constructedType.GetMethod("method_0", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static bool Prefix(ref bool __result)
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

    //out-of-raid
    public class ApplyItemStashPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(HealthControllerClass).GetMethod("ApplyItem", BindingFlags.Instance | BindingFlags.Public);

        }

        private static void restoreHP(HealthControllerClass controller, EBodyPart initialTarget, float hpToRestore) 
        {
            if (initialTarget != EBodyPart.Common)
            {
                controller.ChangeHealth(initialTarget, hpToRestore, ExistanceClass.MedKitUse);
                return;
            }

            foreach (EBodyPart bodyPart in Plugin.RealHealthController.PossibleBodyParts)
            {
                if (hpToRestore <= 0) break;
                float hpMissing = controller.GetBodyPartHealth(bodyPart).Maximum - controller.GetBodyPartHealth(bodyPart).Current;
                if (hpMissing <= 0) continue;
                float hpToUse = Math.Min(hpMissing, hpToRestore);
                controller.ChangeHealth(bodyPart, hpToUse, ExistanceClass.MedKitUse);
                hpToRestore -= hpToUse;
            }
        }

        [PatchPostfix]
        private static void Postfix(HealthControllerClass __instance, Item item, EBodyPart bodyPart, float? amount)
        {
            if (Plugin.EnableLogging.Value)
            {
                Logger.LogWarning("applying " + item.LocalizedName());
            }

            if (Plugin.ServerConfig.food_changes)
            {
                FoodClass foodClass = item as FoodClass;
                if (foodClass != null)
                {
                    foreach (var buff in foodClass.HealthEffectsComponent.BuffSettings)
                    {
                        if (buff.BuffType == EStimulatorBuffType.EnergyRate)
                        {
                            if (buff.Value > 0)
                            {
                                __instance.ChangeEnergy(buff.Value * buff.Duration);
                            }
                        }
                        if (buff.BuffType == EStimulatorBuffType.HydrationRate)
                        {
                            if (buff.Value > 0)
                            {
                                __instance.ChangeHydration(buff.Value * buff.Duration);
                            }
                        }
                    }
                    return;
                }
            }
            if (Plugin.ServerConfig.med_changes)
            {
                MedsClass medsClass = item as MedsClass;
                if (medsClass != null)
                {
                    string medType = MedProperties.MedType(medsClass);
                    //need to get surgery kit working later, doesnt want to remove hp resource.
                    if (medType == "medkit") // || medType == "surg"
                    {
                        restoreHP(__instance, bodyPart, MedProperties.HPRestoreAmount(medsClass));
                        /*             medsClass.MedKitComponent.HpResource -= 1f;
                                     medsClass.MedKitComponent.Item.RaiseRefreshEvent(false, true);*/
                        return;
                    }
                }
            }
        }
    }

    //in-raid
    public class ApplyItemPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(PlayerHealthController).GetMethod("ApplyItem", BindingFlags.Instance | BindingFlags.Public);

        }
        [PatchPrefix]
        private static bool Prefix(PlayerHealthController __instance, Item item, EBodyPart bodyPart, ref bool __result)
        {
            if (__instance.Player.IsYourPlayer)
            {
                if (!__instance.CanApplyItem(item, bodyPart)) return true;

                MedsClass medsClass;
                FoodClass foodClass;
                bool canUse = true;
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
            if (__instance.IsYourPlayer)
            {
                Item boundItem = __instance.GClass2761_0.Inventory.FastAccess.GetBoundItem(quickSlot);
                FoodClass foodItem = boundItem as FoodClass;
                if (boundItem != null && foodItem != null)
                {
                    bool canUse = true;
                    Plugin.RealHealthController.CanConsume(__instance, boundItem, ref canUse);
                    if (Plugin.EnableLogging.Value)
                    {
                        Logger.LogWarning("quick slot, can use = " + canUse);
                    }

                    return canUse;
                }
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
            if (__instance.Player.IsYourPlayer) 
            {
                //I had to do this previously due to the type being protected, no longer is the case. Keeping for reference.
                /* BodyPartStateWrapper bodyPartStateWrapper = GetBodyPartStateWrapper(__instance, bodyPart);*/

                HealthStateClass.BodyPartState bodyPartState = __instance.Dictionary_0[bodyPart];
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
            return true;
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
            Player player = Utils.GetYourPlayer();
            float stressResist = player.Skills.StressPain.Value;
            float painkillerDuration = (float)Math.Round(12f * (1f + stressResist), 2);
            float negativeEffectDuration = (float)Math.Round(15f * (1f - stressResist), 2);
            float negativeEffectStrength = (float)Math.Round(0.75f * (1f - stressResist), 2);
            Plugin.RealHealthController.TryAddAdrenaline(player, painkillerDuration, negativeEffectDuration, negativeEffectStrength);
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

                    if (damageType == EDamageType.Dehydration || damageType == EDamageType.Exhaustion)
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
                        Plugin.RealHealthController.DmgeTracker.TotalDehydrationDamage += damage;
                        return;
                    }
                    if (damageType == EDamageType.Exhaustion)
                    {
                        Plugin.RealHealthController.DmgeTracker.TotalExhaustionDamage += damage;
                        return;
                    }
                    if ((damageType == EDamageType.Fall && damage <= 20f))
                    {
                        Plugin.RealHealthController.DoPassiveRegen(tickRate, bodyPart, __instance.Player, delay, damage, damageType);
                        return;
                    }
                    if (damageType == EDamageType.Barbed)
                    {
                        Plugin.RealHealthController.DoPassiveRegen(tickRate, bodyPart, __instance.Player, delay, damage * 0.75f, damageType);
                        return;
                    }
                    if (damageType == EDamageType.Blunt && damage <= 7.5f)
                    {
                        Plugin.RealHealthController.DoPassiveRegen(tickRate, bodyPart, __instance.Player, delay, damage * 0.75f, damageType);
                        return;
                    }
                    if (damageType == EDamageType.HeavyBleeding || damageType == EDamageType.LightBleeding)
                    {
                        Plugin.RealHealthController.DmgeTracker.UpdateDamage(damageType, bodyPart, damage);
                        return;
                    }
                    if (damageType == EDamageType.Bullet || damageType == EDamageType.Explosion || damageType == EDamageType.Landmine || (damageType == EDamageType.Fall && damage >= 17f) || (damageType == EDamageType.Blunt && damage >= 10f))
                    {
                        Plugin.RealHealthController.RemoveEffectsOfType(EHealthEffectType.HealthRegen);
                    }
                    if (damageType == EDamageType.Bullet || damageType == EDamageType.Blunt || damageType == EDamageType.Melee || damageType == EDamageType.Sniper)
                    {
                        float painkillerDuration = (float)Math.Round(20f * (1f + (stressResist / 2)), 2);
                        float negativeEffectDuration = (float)Math.Round(25f * (1f - (stressResist / 2)), 2);
                        float negativeEffectStrength = (float)Math.Round(0.95f * (1f - (stressResist / 2)), 2);
                        Plugin.RealHealthController.TryAddAdrenaline(__instance.Player, painkillerDuration, negativeEffectDuration, negativeEffectStrength);

                    }
                }
            }
        }
    }

    //Gear blocking won't work but it's better than nothing
    public class SetMedsInHandsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(EFT.Player).GetMethod("SetInHands", BindingFlags.Instance | BindingFlags.Public, null, new Type[] { typeof(MedsClass), typeof(EBodyPart), typeof(int), typeof(Callback<GInterface130>)}, null);
        }

        [PatchPrefix]
        private static bool Prefix(Player __instance, MedsClass meds, ref EBodyPart bodyPart)
        {
            if (__instance.IsYourPlayer && Plugin.IsUsingFika)
            {
                bool shouldAllowHeal = true;
                Plugin.RealHealthController.CanUseMedItemCommon(meds, __instance, ref bodyPart, ref shouldAllowHeal);
                return shouldAllowHeal;
            }
            return true;
        }
    }

    //Fika overrides Proceed methods
    public class ProceedMedsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(EFT.Player).GetMethod("Proceed", BindingFlags.Instance | BindingFlags.Public, null, new Type[] { typeof(MedsClass), typeof(EBodyPart), typeof(Callback<GInterface130>), typeof(int), typeof(bool) }, null);
        }

        [PatchPrefix]
        private static bool Prefix(Player __instance, MedsClass meds, ref EBodyPart bodyPart)
        {
            if (__instance.IsYourPlayer && !Plugin.IsUsingFika)
            {
                bool shouldAllowHeal = true;
                Plugin.RealHealthController.CanUseMedItemCommon(meds, __instance, ref bodyPart, ref shouldAllowHeal);
                return shouldAllowHeal;
            }
            return true;
        }
    }

    //patch itself works, so possible to patch methods of nested types
    /*    public class ExistenceEnergyDrainPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                Type nestedType = typeof(EFT.HealthSystem.ActiveHealthController).GetNestedType("Existence", BindingFlags.NonPublic | BindingFlags.Instance); //get the nested type used by the generic type, Class1885
                return nestedType.GetMethod("method_5", BindingFlags.Instance | BindingFlags.Public);
            }

            [PatchPrefix]
            private static bool Prefix(ref float __result, dynamic __instance) //can use dynamic type for instance
            {

                if (__instance.HealthController.Player.IsYourPlayer)
                {
                    Player player = __instance.HealthController.Player;
                    float skillFactor = 1f - player.Skills.HealthEnergy;
                    float baseDrain = ActiveHealthController.GClass2415.GClass2424_0.Existence.EnergyDamage + Plugin.active
                    __result = *skillFactor / this.float_16;
                    return false;
                }

            }
        }
    */
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
