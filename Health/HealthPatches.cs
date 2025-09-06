using Comfort.Common;
using EFT;
using EFT.HealthSystem;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.Health;
using HarmonyLib;
using JetBrains.Annotations;
using SPT.Reflection.Patching;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using static EFT.HealthSystem.ActiveHealthController;
using static HarmonyLib.AccessTools;
using static RealismMod.Attributes;
using Color = UnityEngine.Color;
using ExistanceClass = GClass2855;
using HealthStateClass = GClass2814<EFT.HealthSystem.ActiveHealthController.GClass2813>;
using MedUseStringClass = GClass1372;
using SetInHandsMedsInterface = GInterface176;
using MedUiString = GClass1372;
using UnityEngine.Rendering.PostProcessing;

namespace RealismMod
{
    public class HealthPanelPatch : ModulePatch
    {
        public const float MAIN_FONT_SIZE = 14f;
        public const float SECONDARY_FONT_SIZE = 30f;
        public const float FONT_CHANGE_SPEED = 1f;

        protected override MethodBase GetTargetMethod()
        {
            return typeof(HealthParametersPanel).GetMethod("method_0", BindingFlags.Instance | BindingFlags.Public);
        }

        private static Color GetCurrentGasColor(float level) 
        {
            switch(level) 
            {
                case 0:
                    return Color.white;
                case <= 25f:
                    return Color.yellow;
                case <= 50f:
                    return new Color(1.0f, 0.647f, 0.0f);
                case <= 75f:
                    return new Color(1.0f, 0.25f, 0.0f);
                case > 75f:
                    return Color.red;
                default: 
                    return Color.white;    
            }
        }

        private static Color GetCurrentRadColor(float level)
        {
            switch (level)
            {
                case 0:
                    return Color.white;
                case <= 15f:
                    return Color.yellow;
                case <= 25f:
                    return new Color(1.0f, 0.647f, 0.0f);
                case <= 50f:
                    return new Color(1.0f, 0.25f, 0.0f);
                case > 50f:
                    return Color.red;
                default:
                    return Color.white;
            }
        }

        private static Color GetGasRateColor(float level)
        {
            switch (level)
            {
                case < 0:
                    return Color.green;
                case 0:
                    return new Color(0.4549f, 0.4824f, 0.4941f, 1f);
                case <= 0.15f:
                    return Color.yellow;
                case <= 0.25f:
                    return new Color(1.0f, 0.647f, 0.0f);
                case <= 0.4f:
                    return new Color(1.0f, 0.25f, 0.0f);
                case > 0.4f:
                    return Color.red;
                default:
                    return Color.white;
            }
        }

        private static Color GetRadRateColor(float level)
        {
            switch (level)
            {
                case < 0:
                    return Color.green;
                case 0:
                    return new Color(0.4549f, 0.4824f, 0.4941f, 1f);
                case <= 0.05f:
                    return Color.yellow;
                case <= 0.15f:
                    return new Color(1.0f, 0.647f, 0.0f);
                case <= 0.25f:
                    return new Color(1.0f, 0.25f, 0.0f);
                case > 0.25f:
                    return Color.red;
                default:
                    return Color.white;
            }
        }


        [PatchPostfix]
        private static void Postfix(HealthParametersPanel __instance)
        {
#pragma warning disable CS0618
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
                        float toxicityRate = PluginConfig.EnableTrueHazardRates.Value ? HazardTracker.BaseTotalToxicityRate : HazardTracker.TotalToxicityRate;
                        CustomTextMeshProUGUI buffUI = buff.GetComponent<CustomTextMeshProUGUI>(); //can animate it by changing the font size with ping pong, and modulate the color
                        buffUI.text = (toxicityRate > 0f ? "+" : "") + toxicityRate.ToString("0.00");
                        buffUI.color = GetGasRateColor(toxicityRate);
                        buffUI.fontSize = MAIN_FONT_SIZE;
                    }
                    if (current != null)
                    {
                        float toxicityLevel = Mathf.Round(HazardTracker.TotalToxicity);
                        CustomTextMeshProUGUI currentUI = current.GetComponent<CustomTextMeshProUGUI>();
                        currentUI.text = toxicityLevel.ToString();
                        currentUI.color = GetCurrentGasColor(toxicityLevel);
                        currentUI.fontSize = SECONDARY_FONT_SIZE;
                    }
                }

                GameObject radiation = panel.transform.Find("Radiation")?.gameObject;
                if (radiation != null)
                {
                    GameObject buff = radiation.transform.Find("Buff")?.gameObject;
                    GameObject current = radiation.transform.Find("Current")?.gameObject;
                    if (buff != null)
                    {
                        float radRate = PluginConfig.EnableTrueHazardRates.Value ? HazardTracker.BaseTotalRadiationRate : HazardTracker.TotalRadiationRate;
                        CustomTextMeshProUGUI buffUI = buff.GetComponent<CustomTextMeshProUGUI>();
                        buffUI.text = (radRate > 0f ? "+" : "") + radRate.ToString("0.00");
                        buffUI.color = GetRadRateColor(radRate);
                        buffUI.fontSize = MAIN_FONT_SIZE;
                    }
                    if (current != null)
                    {
                        float radiationLevel = Mathf.Round(HazardTracker.TotalRadiation);
                        CustomTextMeshProUGUI currentUI = current.GetComponent<CustomTextMeshProUGUI>();
                        currentUI.text = radiationLevel.ToString();
                        currentUI.color = GetCurrentRadColor(radiationLevel);
                        currentUI.fontSize = SECONDARY_FONT_SIZE;
                    }
                }
            }
#pragma warning restore CS0618
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
                stringBuilder.Append((__instance.Cost + 1) + " HP"); //add 1 to the cost to ensure it accurately reflects how resource is deducted from medkits
            }
            if (__instance.HealthPenaltyMax == 69) //only way of verifying it's a rad or toxicity value
            {
                stringBuilder.Append($"(<color=#54C1FFFF>{-__instance.FadeOut}</color>)");
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

        private static string GetHBTypeString(EHeavyBleedHealType type)
        {
            switch (type)
            {
                case EHeavyBleedHealType.Tourniquet:
                    return "TOURNIQUET";
                case EHeavyBleedHealType.Surgical:
                    return "SURGICAL";
                case EHeavyBleedHealType.Combo:
                    return "TOURNIQUET + CHEST SEAL";
                case EHeavyBleedHealType.Clot:
                    return "CLOTTING AGENT";
                default:
                    return "NONE";
            }
        }

        private static string GetPKStrengthString(float str)
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
        private static void PatchPostfix(HealthEffectsComponent __instance, Item item)
        {
            var medStats = TemplateStats.GetDataObj<Consumable>(TemplateStats.ConsumableStats, item.TemplateId);
            bool isPainMed = medStats.ConsumableType == EConsumableType.PainPills || medStats.ConsumableType == EConsumableType.PainDrug;
            if (item.Template.ParentId == "5448f3a64bdc2d60728b456a")
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

            if (isPainMed || medStats.ConsumableType == EConsumableType.Alcohol)
            {
                float strength = medStats.Strength;
                List<ItemAttributeClass> strengthAtt = item.Attributes;
                ItemAttributeClass strengthAttClass = new ItemAttributeClass(Attributes.ENewItemAttributeId.PainKillerStrength);
                strengthAttClass.Name = ENewItemAttributeId.PainKillerStrength.GetName();
                strengthAttClass.StringValue = () => GetPKStrengthString(strength);
                strengthAttClass.DisplayType = () => EItemAttributeDisplayType.Compact;
                strengthAttClass.LabelVariations = EItemAttributeLabelVariations.Colored;
                strengthAttClass.LessIsGood = false;
                strengthAtt.Add(strengthAttClass);
            }

            if (medStats.ConsumableType == EConsumableType.Tourniquet || medStats.ConsumableType == EConsumableType.Medkit || medStats.ConsumableType == EConsumableType.Surgical)
            {
                float hpPerTick = medStats.ConsumableType != EConsumableType.Surgical ? -medStats.TrnqtDamage : medStats.HPRestoreTick;

                if (medStats.ConsumableType == EConsumableType.Medkit || medStats.ConsumableType == EConsumableType.Surgical)
                {
                    float hp = medStats.HPRestoreAmount;
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

                if (medStats.HeavyBleedHealType != EHeavyBleedHealType.None)
                {
                    List<ItemAttributeClass> hbAtt = item.Attributes;
                    ItemAttributeClass hbAttClass = new ItemAttributeClass(Attributes.ENewItemAttributeId.HBleedType);
                    hbAttClass.Name = ENewItemAttributeId.HBleedType.GetName();
                    hbAttClass.StringValue = () => GetHBTypeString(medStats.HeavyBleedHealType);
                    hbAttClass.DisplayType = () => EItemAttributeDisplayType.Compact;
                    hbAttClass.LabelVariations = EItemAttributeLabelVariations.Colored;
                    hbAttClass.LessIsGood = false;
                    hbAtt.Add(hbAttClass);

                    if (medStats.ConsumableType == EConsumableType.Surgical)
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
        }
    }

    public class StimStackPatch1 : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            Type nestedType = typeof(EFT.HealthSystem.ActiveHealthController).GetNestedType("Stimulator", BindingFlags.NonPublic | BindingFlags.Instance); //get the nested type used by the generic type, Class1885
            Type genericType = typeof(Class2115<>); //declare generic type
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

    public class StimStackPatch2 : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            Type nestedType = typeof(EFT.HealthSystem.ActiveHealthController).GetNestedType("Stimulator", BindingFlags.NonPublic | BindingFlags.Instance); //get the nested type used by the generic type, Class1885
            Type genericType = typeof(Class2114<>); //declare generic type
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
            return typeof(BasePhysicalClass).GetMethod("get_BreathIsAudible", BindingFlags.Instance | BindingFlags.Public);
        }
        [PatchPrefix]
        private static bool Prefix(BasePhysicalClass __instance, ref bool __result)
        {
            if (__instance.iobserverToPlayerBridge_0.iPlayer.IsAI) return true;
            __result = !__instance.HoldingBreath && ((__instance.StaminaParameters.StaminaExhaustionStartsBreathSound && __instance.Stamina.Exhausted) || __instance.Oxygen.Exhausted || Plugin.RealHealthController.HasOverdosed);
            return false;
        }
    }

    public class MedsController2Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player.MedsController.Class1172).GetMethod("method_4");
        }

        [PatchPrefix]
        private static bool Prefix(ref bool __result)
        {
            __result = false;
            return false;
        }
    }

    //if out of raid, pretend everything has a resource rate above 0 to enable applying it
    public class MedKitHpRatePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(MedKitComponent).GetMethod("get_HpResourceRate", BindingFlags.Instance | BindingFlags.Public);
        }
        [PatchPrefix]
        private static bool Prefix(MedKitComponent __instance, ref float __result)
        {
            var medStats = TemplateStats.GetDataObj<Consumable>(TemplateStats.ConsumableStats, __instance.Item.TemplateId);

            if (!GameWorldController.IsInRaid() && medStats != null && Plugin.RealHealthController.ShouldAlwaysAllowOutOfRaid(__instance.Item, medStats))
            {
                __result = 1f;
                return false;
            }
            return true;
        }
    }

    public class HealthControllerTryGetBodyPartToApplyPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(HealthControllerClass).GetMethod("TryGetBodyPartToApply");
        }

        [PatchPrefix]
        private static bool Prefix(HealthControllerClass __instance, Item item, EBodyPart bodyPart, out EBodyPart? damagedBodyPart, ref bool __result)
        {
            Logger.LogWarning("HealthControllerClass TryGetBodyPartToApply");
            var medStats = TemplateStats.GetDataObj<Consumable>(TemplateStats.ConsumableStats, item.TemplateId);
            if (!GameWorldController.IsInRaid() && Plugin.RealHealthController.ShouldAlwaysAllowOutOfRaid(item, medStats))
            {
                damagedBodyPart = bodyPart;
                __result = true;
                return false;
            }
            else
            {
                damagedBodyPart = bodyPart;
                return true;
            }
        }
    }

    public class TryGetPartsToApplyPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            var constructed = typeof(GClass2814<>).MakeGenericType(typeof(HealthControllerClass.GClass2819));
            return constructed.GetMethod("TryGetBodyPartToApply");
        }

        [PatchPrefix]
        private static bool Prefix(Item item, EBodyPart bodyPart, out EBodyPart? damagedBodyPart, ref bool __result)
        {
            var medStats = TemplateStats.GetDataObj<Consumable>(TemplateStats.ConsumableStats, item.TemplateId);
            if (!GameWorldController.IsInRaid())
            {
                damagedBodyPart = bodyPart;
                __result = true;
                return false;
            }
            else 
            {
                damagedBodyPart = bodyPart;
                return true;
            }
        }
    }

    public class HealthHasPartsToApplyBasePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            var constructed = typeof(GClass2814<>).MakeGenericType(typeof(HealthControllerClass.GClass2819));
            return constructed.GetMethod("HasPartsToApply");
        }

        [PatchPrefix]
        private static bool Prefix(Item item, ref IResult __result)
        {
            if (GameWorldController.IsInRaid()) return true;

            var medStats = TemplateStats.GetDataObj<Consumable>(TemplateStats.ConsumableStats, item.TemplateId);

            if (medStats != null && Plugin.RealHealthController.ShouldAlwaysAllowOutOfRaid(item, medStats))
            {
                __result = SuccessfulResult.New;
                return false;
            }

            return true;
        }
    }

    public class Method8Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            var constructed = typeof(GClass2814<>).MakeGenericType(typeof(HealthControllerClass.GClass2819));
            return constructed.GetMethod("method_8", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }

        [PatchPrefix]
        private static bool Prefix(ref bool __result, HealthEffectsComponent healthEffects, MedKitComponent medKit, EBodyPart bodyPart)
        {
            if (!GameWorldController.IsInRaid() || medKit == null)
                return true;

            var medStats = TemplateStats.GetDataObj<Consumable>(TemplateStats.ConsumableStats, medKit.Item.TemplateId);

            if (medStats != null && Plugin.RealHealthController.ShouldAlwaysAllowOutOfRaid(medKit.Item, medStats))
            {
                __result = true;
                return false; 
            }

            return true;
        }
    }
    public class HealthCanApplyItemBasePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            var constructed = typeof(GClass2814<>).MakeGenericType(typeof(HealthControllerClass.GClass2819));
            return constructed.GetMethod("CanApplyItem");
        }

        [PatchPrefix]
        private static bool Prefix(ref bool __result, Item item, EBodyPart bodyPart) //
        {
            if (GameWorldController.IsInRaid()) return true;

            var medStats = TemplateStats.GetDataObj<Consumable>(TemplateStats.ConsumableStats, item.TemplateId);

            if (medStats != null && Plugin.RealHealthController.ShouldAlwaysAllowOutOfRaid(item,medStats)) 
            {
                __result = true;
                return false;
            }

            return true;
        }
    }

    public class MedEffectStartedPatch : ModulePatch
    {
        private static readonly Type MedEffectType = typeof(HealthControllerClass).GetNestedType("MedEffect", BindingFlags.NonPublic);

        private static readonly PropertyInfo HealthControllerProperty = AccessTools.Property(MedEffectType, "HealthControllerClass");
        private static readonly PropertyInfo BodyPartProperty = AccessTools.Property(MedEffectType, "BodyPart");
        private static readonly PropertyInfo MedItemProperty = AccessTools.Property(MedEffectType, "MedItem");
        private static readonly FieldInfo MedKitComponentField = AccessTools.Field(MedEffectType, "medKitComponent_0");

        protected override MethodBase GetTargetMethod()
        {
            var method = MedEffectType.GetMethod("Started", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return method;
        }

        private static void ApplyStim(HealthControllerClass healthController, Item medItem) 
        {
            GControl6 gcontrols = healthController.inventoryController_0 as GControl6;
            gcontrols?.Heal(medItem, EBodyPart.Head, Mathf.RoundToInt(1));
            GClass2821.RemoveItem(medItem);
            var medItemTypes = medItem.GetType();
            var raiseRefreshMethods = AccessTools.Method(medItemTypes, "RaiseRefreshEvent");
            raiseRefreshMethods?.Invoke(medItem, new object[] { false, true });

            Singleton<GUISounds>.Instance.PlayItemSound(medItem.ItemSound, EInventorySoundType.offline_use, false);
        }

        //TODO: implement other debuffs
        private static void TryApplyDebuffs(HealthControllerClass healthController, GClass2823.GClass2848.GClass2849[] buffs, bool getsAdditionalDebuff)
        {
            var painDrugFactor = getsAdditionalDebuff ? RealismHealthController.OUT_OF_RAID_RESOURCE_DEBUFF_MULTI : 1f;

            var buffsList = buffs.ToList();
            var hydrationDebuff = buffsList.FirstOrDefault(b => b.BuffType == EStimulatorBuffType.HydrationRate);
            var energyDebuff = buffsList.FirstOrDefault(b => b.BuffType == EStimulatorBuffType.EnergyRate);
            if (hydrationDebuff != null)
                healthController.ChangeHydration(hydrationDebuff.Value * hydrationDebuff.Duration * painDrugFactor);
            if (energyDebuff != null)
                healthController.ChangeEnergy(energyDebuff.Value * energyDebuff.Duration * painDrugFactor);
        }

        //TODO: implement bleeding removal and other buffs
        private static bool HandleBuffs(HealthControllerClass healthController, Item medItem, MedsItemClass medClass, GClass2823.GClass2848.GClass2849[] buffs, bool getsAdditionalDebuff) 
        {
            var hasUsedStim = false;
            foreach (var buff in buffs)
            {
                if (buff.BuffType == EStimulatorBuffType.HealthRate)
                {
                    float restoredHP = RestoreHP(healthController, EBodyPart.Common, buff.Value * buff.Duration, true);
                    hasUsedStim = restoredHP > 0f;
                }
            }
            return hasUsedStim;
        }

        private static bool HandleStim(HealthControllerClass healthController, MedsItemClass medClass, Item medItem)
        {
            var treatedHazard = Plugin.RealHealthController.TryHazardTreatmentOutOfRaid(medClass);
            var buffs = medClass.HealthEffectsComponent.BuffSettings;
            var appliedBuffs = HandleBuffs(healthController, medItem, medClass, buffs, false);

            if (treatedHazard || appliedBuffs)
            {
                TryApplyDebuffs(healthController, buffs, false);
                ApplyStim(healthController, medItem);
                Logger.LogWarning($"treatedHazard {treatedHazard}, appliedBuffs{appliedBuffs}");
                return false;
            }

            return true;
        }

        private static bool HandleDrug(HealthControllerClass healthController, MedsItemClass medClass, Item medItem, MedKitComponent medKitComponent, bool getsAdditionalDebuff)
        {
            var treatedHazard = Plugin.RealHealthController.TryHazardTreatmentOutOfRaid(medClass);
            var buffs = medClass.HealthEffectsComponent.BuffSettings;

            var appliedBuffs = HandleBuffs(healthController, medItem, medClass, buffs, getsAdditionalDebuff);
 
            if (treatedHazard || appliedBuffs)
            {
                TryApplyDebuffs(healthController, buffs, getsAdditionalDebuff);
                return ApplyMedItem(healthController, medKitComponent, medItem, EBodyPart.Head, 1f);
            }
            return true;
        }

        private static float HealBodyPart(HealthControllerClass hc, EBodyPart bodyPart, ref float hpToRestore)
        {
            float bodyPartMissingHp = hc.GetBodyPartHealth(bodyPart).Maximum - hc.GetBodyPartHealth(bodyPart).Current;
            if (bodyPartMissingHp <= 0) return 0f;

            float hpToUse = Math.Min(bodyPartMissingHp, hpToRestore);
            hc.ChangeHealth(bodyPart, hpToUse, ExistanceClass.MedKitUse);

            hpToRestore -= hpToUse;
            return hpToUse;
        }

        private static float RestoreHP(HealthControllerClass hc, EBodyPart initialTarget, float hpToRestore, bool restoreAll)
        {
            var hpRestored = 0f;

            if (initialTarget != EBodyPart.Common && !restoreAll)
            {
                return HealBodyPart(hc, initialTarget, ref hpToRestore);
            }

            foreach (EBodyPart bodyPart in Plugin.RealHealthController.PossibleBodyParts)
            {
                if (hpToRestore <= 0) break;
                hpRestored += HealBodyPart(hc, bodyPart, ref hpToRestore);
            }

            return hpRestored;
        }

        private static bool ApplyMedItem(HealthControllerClass healthController, MedKitComponent medKitComponent, Item medItem, EBodyPart bodyPart, float cost) 
        {
            bodyPart = bodyPart == EBodyPart.Common ? EBodyPart.Head : bodyPart;

            var medKitComponentType = medKitComponent.GetType();
            var hpResourceField = AccessTools.Field(medKitComponentType, "HpResource");
            float currentHp = (float)hpResourceField.GetValue(medKitComponent);

            GControl6 gcontrol = healthController.inventoryController_0 as GControl6;
            gcontrol?.Heal(medItem, bodyPart, Mathf.RoundToInt(cost));

            float newHp = currentHp - cost;
            hpResourceField.SetValue(medKitComponent, newHp);

            if (newHp <= 0f)
            {
                GClass2821.RemoveItem(medItem);
                Singleton<GUISounds>.Instance.PlayItemSound(medItem.ItemSound, EInventorySoundType.offline_use, false);
                return false;
            }

            var medItemType = medItem.GetType();
            var raiseRefreshMethod = AccessTools.Method(medItemType, "RaiseRefreshEvent");
            raiseRefreshMethod?.Invoke(medItem, new object[] { false, true });

            Singleton<GUISounds>.Instance.PlayItemSound(medItem.ItemSound, EInventorySoundType.offline_use, false);

            return true;
        }

        private static bool HandleHealing(HealthControllerClass healthController, Consumable itemStats, Item medItem, EBodyPart bodyPart, MedKitComponent medKitComponent)
        {
            if (itemStats.HPRestoreAmount <= 0) return false;

            //can't allow Ebodypart.common in earlier methods as it cause exceptions, so have to guess if the intended target is common
            bool restoreAll = healthController.GetBodyPartHealth(bodyPart).Maximum - healthController.GetBodyPartHealth(bodyPart).Current <= 0f;

            float hpRestored = RestoreHP(healthController, bodyPart, itemStats.HPRestoreAmount, restoreAll);
            if (hpRestored > 0f) 
            {
                return ApplyMedItem(healthController, medKitComponent, medItem, bodyPart, 1f);
            }
            return true;
        }

        [PatchPrefix]
        private static bool Prefix(object __instance)
        {
            if (GameWorldController.IsInRaid()) return true;

            var healthController = (HealthControllerClass)HealthControllerProperty.GetValue(__instance);
            var bodyPart = (EBodyPart)BodyPartProperty.GetValue(__instance); 
            var medItem = (Item)MedItemProperty.GetValue(__instance);
            var medKitComponent = (MedKitComponent)MedKitComponentField.GetValue(__instance);

            var medClass = medItem as MedsItemClass;
            var itemStats = TemplateStats.GetDataObj<Consumable>(TemplateStats.ConsumableStats, medItem.TemplateId);

            if (!Plugin.RealHealthController.ShouldAlwaysAllowOutOfRaid(medItem, itemStats)) return true;

            if (medClass != null && medItem is StimulatorItemClass)
            {
                HandleStim(healthController, medClass, medItem);
                return false;
            }

            if (itemStats.ConsumableType == EConsumableType.Medkit || itemStats.ConsumableType == EConsumableType.Surgical)
                return HandleHealing(healthController, itemStats, medItem, bodyPart, medKitComponent);

            var isDrug = 
                itemStats.ConsumableType == EConsumableType.Drug || 
                itemStats.ConsumableType == EConsumableType.PainPills || 
                itemStats.ConsumableType == EConsumableType.Pills || 
                itemStats.ConsumableType == EConsumableType.PainDrug;

            if (medClass != null && isDrug)
                return HandleDrug(healthController, medClass, medItem, medKitComponent, itemStats.DoesExtraResourceDebuff);

            return true;
        }
    }

    public class ApplyItemStashPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(HealthControllerClass).GetMethod("ApplyItem", new Type[] { typeof(Item), typeof(GStruct353<EBodyPart>), typeof(float) });
        }

        private static void CallMedEffect(HealthControllerClass hc, Item item, EBodyPart? eBodyPart, float amountUsed) 
        {
            // Find the protected nested type "MedEffect"
            var medEffectType = typeof(HealthControllerClass).GetNestedType(
                "MedEffect",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static
            );

            if (medEffectType == null)
                throw new Exception("Could not find MedEffect type");

            // Construct the base generic: GClass2820<GStruct364>
            var baseGeneric = typeof(HealthControllerClass.GClass2820<>).MakeGenericType(typeof(GStruct364));

            // Get the "Create" method definition
            var createMethodDef = baseGeneric.GetMethod(
                "Create",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static
            );

            if (createMethodDef == null)
                throw new Exception("Could not find Create<T>()");

            // Make it generic for MedEffect
            var createMethod = createMethodDef.MakeGenericMethod(medEffectType);

            // Call it
            var result = createMethod.Invoke(
                null,
                new object[]
                {
                    hc,
                    eBodyPart.Value,
                    new Profile.ProfileHealthClass.GClass1974(),
                    (int?)hc.UpdateTime,
                    new GStruct364 { ItemId = item.Id, Amount = amountUsed }
                }
            );
        }

        private static void HandleBuffs(HealthControllerClass hc, FoodDrinkItemClass foodClass) 
        {
            foreach (var buff in foodClass.HealthEffectsComponent.BuffSettings)
            {
                if (PluginConfig.EnableMedicalLogging.Value) Logger.LogWarning("buff " + buff.BuffName + buff.BuffType);
                if (buff.BuffType == EStimulatorBuffType.EnergyRate)
                {
                    if (PluginConfig.EnableMedicalLogging.Value) Logger.LogWarning("has energy buff " + buff.Value);
                    if (buff.Value > 0)
                    {
                        hc.ChangeEnergy(buff.Value * buff.Duration);
                    }
                }

                if (buff.BuffType == EStimulatorBuffType.HydrationRate)
                {
                    if (PluginConfig.EnableMedicalLogging.Value) Logger.LogWarning("has hydration buff " + buff.Value);
                    if (buff.Value > 0)
                    {
                        hc.ChangeHydration(buff.Value * buff.Duration);
                    }
                }
            }
        }

        private static bool TryDoPoisoning(HealthControllerClass hc, FoodDrinkItemClass foodClass) 
        {
            var toxinDebuffs = foodClass.HealthEffectsComponent.BuffSettings.Where(b => b.BuffType == EStimulatorBuffType.UnknownToxin);
            if (toxinDebuffs.Count() > 0)
            {
                var debuff = toxinDebuffs.First();
                if (PluginConfig.EnableMedicalLogging.Value) Logger.LogWarning("has toxin debuff " + debuff.Chance);
                if (debuff.Chance > 0 && UnityEngine.Random.Range(0, 100) < debuff.Chance * 100)
                {
                    if (PluginConfig.EnableMedicalLogging.Value) Logger.LogWarning("applying toxin debuff");
                    float energyDrain = UnityEngine.Random.Range(debuff.Chance * 250, debuff.Chance * 500);
                    energyDrain = Mathf.Clamp(energyDrain, 2.5f, 90f);
                    float hydrationDrain = UnityEngine.Random.Range(debuff.Chance * 250, debuff.Chance * 500);
                    hydrationDrain = Mathf.Clamp(hydrationDrain, 2.5f, 90f);
                    hc.ChangeEnergy(-energyDrain);
                    hc.ChangeHydration(-hydrationDrain);

                    Singleton<GUISounds>.Instance.PlaySound(Plugin.RealismAudioController.FoodPoisoningSfx.RandomElement().Value, false, false, 0.5f);

                    return true;
                }
            }
            return false;
        }

        private static void DoFoodItem(HealthControllerClass hc, FoodDrinkItemClass foodClass)
        {
            if (TryDoPoisoning(hc, foodClass)) return;
   
            HandleBuffs(hc, foodClass);

            Plugin.RealHealthController.TryReduceToxinInStashFood(foodClass, false, hc);
        }


        [PatchPrefix]
        private static bool PatchPrefix(HealthControllerClass __instance, Item item, GStruct353<EBodyPart> bodyParts, float? amount, ref bool __result)
        {
            if (GameWorldController.IsInRaid()) return true;

            Plugin.BSGHealthController = __instance;

            var itemStats = TemplateStats.GetDataObj<Consumable>(TemplateStats.ConsumableStats, item.TemplateId);

            if (bodyParts.Length == 0)
            {
                __result = false;
                return false;
            }

            EBodyPart? ebodyPart = bodyParts[0];
            ebodyPart = ebodyPart == EBodyPart.Common ? EBodyPart.Head : ebodyPart;

            FoodDrinkItemClass foodDrinkItemClass = item as FoodDrinkItemClass;
            float amountUsed = 1f;
            if (foodDrinkItemClass != null)
            {
                amountUsed = (amount ?? (foodDrinkItemClass.FoodDrinkComponent.HpPercent / foodDrinkItemClass.FoodDrinkComponent.MaxResource));
            }

            //may want functionality to apply to multiple parts at time, so this would be called per bodypart
            CallMedEffect(__instance, item, ebodyPart, amountUsed);

            Profile profile = __instance.inventoryController_0.Profile as Profile;
            if (profile != null)
            {
                profile.Health = __instance.Store(null);
            }

            __result = true;
            return false;
        }

        [PatchPostfix]
        private static void Postfix(HealthControllerClass __instance, Item item, GStruct353<EBodyPart> bodyParts, float? amount)
        {
            if (Plugin.ServerConfig.food_changes)
            {
                FoodDrinkItemClass foodClass = item as FoodDrinkItemClass;
                if (foodClass != null)
                {
                    if (PluginConfig.EnableMedicalLogging.Value) Logger.LogWarning("is food");
                    DoFoodItem(__instance, foodClass);
                    Singleton<GUISounds>.Instance.PlayItemSound(item.ItemSound, EInventorySoundType.offline_use, false);
                    return;
                }
            }
        }
    }

    //in-raid
    public class ApplyItemPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GControl4).GetMethod("ApplyItem", new Type[] { typeof(Item), typeof(EBodyPart), typeof(float) });
        }
        [PatchPrefix]
        private static bool Prefix(GControl4 __instance, Item item, EBodyPart bodyPart, ref bool __result)
        {
            Logger.LogWarning("ApplyItem");
            var player = (Player)AccessTools.Field(typeof(GControl4), "Player").GetValue(__instance);
            if (player.IsYourPlayer)
            {  
                if (!__instance.CanApplyItem(item, bodyPart)) return true;

                MedsItemClass MedsItemClass;
                FoodDrinkItemClass foodClass;
                bool canUse = true;
                if (((MedsItemClass = (item as MedsItemClass)) != null))
                {
                    if (PluginConfig.EnableMedicalLogging.Value)
                    {
                        Logger.LogWarning("ApplyItem Med");
                    }
                    Plugin.RealHealthController.CanUseMedItem(player, bodyPart, item, ref canUse);
                }
                if ((foodClass = (item as FoodDrinkItemClass)) != null)
                {
                    if (PluginConfig.EnableMedicalLogging.Value)
                    {
                        Logger.LogWarning("ApplyItem Food");
                    }

                    if (PluginConfig.GearBlocksEat.Value)
                    {
                        Plugin.RealHealthController.CanConsume(player, item, ref canUse);
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
        private static bool Prefix(EFT.Player __instance, EBoundItem quickSlot, Callback<IHandsController> callback)
        {
            Logger.LogWarning("SetQuickSlotItem");
            if (__instance.IsYourPlayer)
            {
                Item boundItem = __instance.InventoryController.Inventory.FastAccess.GetBoundItem(quickSlot);
                FoodDrinkItemClass foodItem = boundItem as FoodDrinkItemClass;
                if (boundItem != null && foodItem != null)
                {
                    bool canUse = true;
                    Plugin.RealHealthController.CanConsume(__instance, boundItem, ref canUse);
                    if (PluginConfig.EnableMedicalLogging.Value) Logger.LogWarning("quick slot, can use = " + canUse);
                    if (!canUse) callback(null);
                    return canUse;
                }
                if (Plugin.FikaPresent)
                {
                    Logger.LogWarning("SetQuickSlotItem Fika");
                    MedsItemClass medItem = boundItem as MedsItemClass;
                    if (boundItem != null && medItem != null)
                    {
                        __instance.SetInHands(medItem, EBodyPart.Common, 1, new Callback<SetInHandsMedsInterface>(GControl4.Class2153.class2153_0.method_1));
                        callback(null);
                        return false;
                    }
                }
            }
            return true;
        }
    }

    public class RestoreBodyPartPatch : ModulePatch
    {
        private static FieldInfo _bodyPartRestoredField;

        protected override MethodBase GetTargetMethod()
        {
            _bodyPartRestoredField = AccessTools.Field(typeof(ActiveHealthController), "BodyPartRestoredEvent");
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
                Logger.LogError("=======Realism Mod: FAILED TO GET BODYPARTSTATE INSTANCE=========");
                return null;
            }

            return new BodyPartStateWrapper(bodyPartStateInstance);
        }

        [PatchPrefix]
        private static bool Prefix(ActiveHealthController __instance, EBodyPart bodyPart, float healthPenalty, ref bool __result)
        {
            if (__instance.Player.IsYourPlayer) 
            {
                Logger.LogWarning("RestoreBodyPart");
                //I had to do this previously due to the type being protected, no longer is the case. Keeping for reference.
                /* BodyPartStateWrapper bodyPartStateWrapper = GetBodyPartStateWrapper(__instance, bodyPart);*/

                HealthStateClass.BodyPartState bodyPartState = __instance.Dictionary_0[bodyPart];
                Action<EBodyPart, ValueStruct> bodyPartRestoredField = (Action<EBodyPart, ValueStruct>)_bodyPartRestoredField.GetValue(__instance);

                if (!bodyPartState.IsDestroyed)
                {
                    __result = false;
                    return false;
                }

                HealthValue health = bodyPartState.Health;
                bodyPartState.IsDestroyed = false;
                healthPenalty += (1f - healthPenalty) * __instance.skillManager_0.SurgeryReducePenalty;
                bodyPartState.Health = new HealthValue(1f, Mathf.Max(1f, Mathf.Ceil(bodyPartState.Health.Maximum * healthPenalty)), 0f);
                __instance.method_43(bodyPart, EDamageType.Medicine);
                __instance.method_35(bodyPart);
                Action<EBodyPart, ValueStruct> bodyPartRestoredEvent = bodyPartRestoredField; // __instance.BodyPartRestoredEvent;
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
            Logger.LogWarning("Remove");
            if (PluginConfig.EnableMedicalLogging.Value)
            {
                Logger.LogWarning("Cancelling Meds");
            }

            Plugin.RealHealthController.CancelPendingEffects();
        }
    }

    public class HCApplyDamagePatch : ModulePatch
    {
        private static FieldInfo _playerField;
        protected override MethodBase GetTargetMethod()
        {
            _playerField = AccessTools.Field(typeof(ActiveHealthController), "Player");
            return typeof(ActiveHealthController).GetMethod("ApplyDamage", BindingFlags.Instance | BindingFlags.Public);
        }

        private static EDamageType[] _acceptedDamageTypes = { 
            EDamageType.HeavyBleeding, EDamageType.LightBleeding, 
            EDamageType.Fall, EDamageType.Barbed, EDamageType.Dehydration, 
            EDamageType.Exhaustion, EDamageType.Poison,  EDamageType.Melee,
            EDamageType.Explosion, EDamageType.Bullet, EDamageType.Blunt,};

        private static void CancelRegen() 
        {
            Plugin.RealHealthController.CancelPassiveRegen = true;
            Plugin.RealHealthController.CurrentPassiveRegenBlockDuration = Plugin.RealHealthController.BlockPassiveRegenBaseDuration;
        }

        private static void HandlePassiveRegenTimer(float damage, EDamageType damageType)
        {
            if (damageType == EDamageType.Bullet || damageType == EDamageType.Explosion ||
                damageType == EDamageType.Sniper || damageType == EDamageType.Btr ||
                damageType == EDamageType.HeavyBleeding || damageType == EDamageType.LightBleeding ||
                damageType == EDamageType.Poison || damageType == EDamageType.Exhaustion ||
                damageType == EDamageType.Dehydration || damageType == EDamageType.RadExposure ||
                damageType == EDamageType.Impact || damageType == EDamageType.Melee ||
                damageType == EDamageType.Flame || damageType == EDamageType.Medicine ||
                damageType == EDamageType.LethalToxin || damageType == EDamageType.Stimulator ||
                damage > 5f)
            {
                CancelRegen();
            }
        }

        [PatchPrefix]
        private static void Prefix(ActiveHealthController __instance, EBodyPart bodyPart, ref float damage, DamageInfoStruct damageInfo)
        {
            var player = (Player)_playerField.GetValue(__instance);
            if (player.IsYourPlayer)
            {
                EDamageType damageType = damageInfo.DamageType;

                float currentHp = __instance.GetBodyPartHealth(bodyPart).Current;
                float maxHp = __instance.GetBodyPartHealth(bodyPart).Maximum;
                float remainingHp = currentHp / maxHp;

                HandlePassiveRegenTimer(damage, damageType);

                if (damageType == EDamageType.Dehydration || damageType == EDamageType.Exhaustion || damageType == EDamageType.Poison)
                {
                    damage = 0;
                    return;
                }

                if (currentHp <= 10f && (bodyPart == EBodyPart.Head || bodyPart == EBodyPart.Chest) && (damageType == EDamageType.LightBleeding))
                {
                    damage = 0;
                    return;
                }

                float vitalitySkill = player.Skills.VitalityBuffSurviobilityInc.Value;
                float stressResist = player.Skills.StressPain.Value;
                int delay = (int)Math.Round(15f * (1f - vitalitySkill), 2);
                float tickRate = (float)Math.Round(0.22f * (1f + vitalitySkill), 2);
                float fallDamageLimit = 17 * vitalitySkill;
                float bluntDamageLimit = 7.5f * vitalitySkill;

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
                if ((damageType == EDamageType.Fall && damage <= fallDamageLimit))
                {
                    Plugin.RealHealthController.DoPassiveRegen(tickRate, bodyPart, player, delay, damage, damageType);
                    return;
                }
                if (damageType == EDamageType.Barbed)
                {
                    Plugin.RealHealthController.DoPassiveRegen(tickRate, bodyPart, player, delay, damage * 0.75f, damageType);
                    return;
                }
                if (damageType == EDamageType.Blunt && damage <= bluntDamageLimit)
                {
                    Plugin.RealHealthController.DoPassiveRegen(tickRate, bodyPart, player, delay, damage * 0.75f, damageType);
                    return;
                }
                if (damageType == EDamageType.HeavyBleeding || damageType == EDamageType.LightBleeding)
                {
                    Plugin.RealHealthController.DmgeTracker.UpdateDamage(damageType, bodyPart, damage);
                    return;
                }
                if (damageType == EDamageType.Bullet || damageType == EDamageType.Explosion || damageType == EDamageType.Landmine || (damageType == EDamageType.Fall && damage >= fallDamageLimit + 2f) || (damageType == EDamageType.Blunt && damage >= bluntDamageLimit + 2f))
                {
                    Plugin.RealHealthController.RemoveEffectsOfType(EHealthEffectType.HealthRegen);
                }
                if (damageType == EDamageType.Bullet || damageType == EDamageType.Blunt || damageType == EDamageType.Melee || damageType == EDamageType.Sniper)
                {
                    float painkillerDuration = (float)Math.Round(20f * (1f + (stressResist / 2)), 2);
                    float negativeEffectDuration = (float)Math.Round(25f * (1f - (stressResist / 2)), 2);
                    float negativeEffectStrength = (float)Math.Round(0.95f * (1f - (stressResist / 2)), 2);
                    Plugin.RealHealthController.TryAddAdrenaline(player, negativeEffectDuration, negativeEffectDuration, negativeEffectStrength);

                }
            }
        }
    }

    //For Fika. Gear blocking won't work but it's better than nothing
    public class SetMedsInHandsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(EFT.Player).GetMethod("SetInHands", BindingFlags.Instance | BindingFlags.Public, null, new Type[] { typeof(MedsItemClass), typeof(GStruct353<EBodyPart>), typeof(int), typeof(Callback<SetInHandsMedsInterface>)}, null);
        }

        [PatchPrefix]
        private static bool Prefix(Player __instance, MedsItemClass meds, ref GStruct353<EBodyPart> bodyParts)
        {
            if (__instance.IsYourPlayer && Plugin.FikaPresent)
            {
                Logger.LogWarning("SetInHands");
                Logger.LogWarning("is empty " + (bodyParts.nullable_0 == null || bodyParts.Length <= 0));
                if (bodyParts.nullable_0 == null || bodyParts.Length <= 0) return true;
                var processResult = Plugin.RealHealthController.ProcessHealAttempt(meds, __instance, bodyParts[0]);
                bodyParts = new GStruct353<EBodyPart>(processResult.NewBodyPart);
                return processResult.ShouldAllowHeal;
            }
            return true;
        }
    }

    public class ProceedMedsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(EFT.Player).GetMethod("Proceed", BindingFlags.Instance | BindingFlags.Public, null, new Type[] { typeof(MedsItemClass), typeof(GStruct353<EBodyPart>), typeof(Callback<SetInHandsMedsInterface>), typeof(int), typeof(bool) }, null);
        }

        [PatchPrefix]
        private static bool Prefix(Player __instance, MedsItemClass meds, ref GStruct353<EBodyPart> bodyParts)
        {
            if (__instance.IsYourPlayer && !Plugin.FikaPresent)  //Fika overrides Proceed methods
            { 
                Logger.LogWarning("Proceed");
                Logger.LogWarning("is empty " + (bodyParts.nullable_0 == null || bodyParts.Length <= 0));
                Logger.LogWarning("Length" + bodyParts.Length);
                if (bodyParts.nullable_0 == null || bodyParts.Length <= 0) return true;

                for (int i = 0; i < bodyParts.Length; i++)
                {
                    Logger.LogWarning(bodyParts[i].ToString());
                }

                var processResult = Plugin.RealHealthController.ProcessHealAttempt(meds, __instance, bodyParts[0]);
                bodyParts = new GStruct353<EBodyPart>(processResult.NewBodyPart);
                Logger.LogWarning("new part " + bodyParts[0]);
                Logger.LogWarning("Length" + bodyParts.Length);
                return processResult.ShouldAllowHeal;
            }
            return true;
        }
    }
}
