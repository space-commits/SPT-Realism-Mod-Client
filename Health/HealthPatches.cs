using Comfort.Common;
using EFT;
using EFT.HealthSystem;
using EFT.InventoryLogic;
using EFT.UI.Health;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using static EFT.HealthSystem.ActiveHealthController;
using static RealismMod.Attributes;
using Color = UnityEngine.Color;
using ExistanceClass = GClass2855;
using HealthStateClass = GClass2814<EFT.HealthSystem.ActiveHealthController.GClass2813>;
using MedUseStringClass = GClass1372;
using SetInHandsMedsInterface = GInterface176;


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

                if (medStats.ConsumableType == EConsumableType.Medkit)
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
        private static bool Prefix(BasePhysicalClass __instance,ref bool __result)
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
            return typeof(HealthControllerClass).GetMethod("ApplyItem", new Type[] { typeof(Item), typeof(EBodyPart), typeof(float) });
        }

        private static void RestoreHP(HealthControllerClass hc, EBodyPart initialTarget, float hpToRestore) 
        {
            if (initialTarget != EBodyPart.Common)
            {
                hc.ChangeHealth(initialTarget, hpToRestore, ExistanceClass.MedKitUse);
                return;
            }

            foreach (EBodyPart bodyPart in Plugin.RealHealthController.PossibleBodyParts)
            {
                if (hpToRestore <= 0) break;
                float hpMissing = hc.GetBodyPartHealth(bodyPart).Maximum - hc.GetBodyPartHealth(bodyPart).Current;
                if (hpMissing <= 0) continue;
                float hpToUse = Math.Min(hpMissing, hpToRestore);
                hc.ChangeHealth(bodyPart, hpToUse, ExistanceClass.MedKitUse);
                hpToRestore -= hpToUse;
            }
        }

        private static void DoFoodItem(HealthControllerClass hc, FoodDrinkItemClass foodClass) 
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

                    Plugin.RealismAudioControllerComponent.PlayFoodPoisoningSFX(0.5f);
                    return;
                }
            }

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

            Plugin.RealHealthController.CheckIfReducesHazardInStash(foodClass, false, hc);
        }

        [PatchPostfix]
        private static void Postfix(HealthControllerClass __instance, Item item, EBodyPart bodyPart, float? amount)
        {
            var itemStats = TemplateStats.GetDataObj<Consumable>(TemplateStats.ConsumableStats, item.TemplateId);
            if (PluginConfig.EnableMedicalLogging.Value) Logger.LogWarning("Applying out of raid: " + item.LocalizedName());

            if (Plugin.ServerConfig.food_changes)
            {
                if (PluginConfig.EnableMedicalLogging.Value) Logger.LogWarning("food changes ");
                FoodDrinkItemClass foodClass = item as FoodDrinkItemClass;
                if (foodClass != null)
                {
                    if (PluginConfig.EnableMedicalLogging.Value) Logger.LogWarning("is food");
                    DoFoodItem(__instance, foodClass);
                    return;
                }
            }

            if (Plugin.ServerConfig.med_changes)
            {
                MedsItemClass MedsItemClass = item as MedsItemClass;
                if (MedsItemClass != null)
                {
                    //need to get surgery kit working later, doesnt want to remove hp resource.
                    if (itemStats.ConsumableType == EConsumableType.Medkit) // || medType == "surg"
                    {
                        RestoreHP(__instance, bodyPart, itemStats.HPRestoreAmount);
                        /*             MedsItemClass.MedKitComponent.HpResource -= 1f;
                                     MedsItemClass.MedKitComponent.Item.RaiseRefreshEvent(false, true);*/
                        return;
                    }
/*                    Plugin.RealHealthController.CheckIfReducesHazardInStash(MedsItemClass, true, __instance); //can't get it to use resource without causing issues
*/
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
        private static FieldInfo _playerField;
        private static FieldInfo _skillsField;
        private static FieldInfo _bodyPartRestoredField;

        protected override MethodBase GetTargetMethod()
        {
            _playerField = AccessTools.Field(typeof(ActiveHealthController), "Player");
            _skillsField = AccessTools.Field(typeof(ActiveHealthController), "skillManager_0");
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
                Logger.LogWarning("=======Realism Mod: FAILED TO GET BODYPARTSTATE INSTANCE=========");
                return null;
            }

            return new BodyPartStateWrapper(bodyPartStateInstance);
        }

        [PatchPrefix]
        private static bool Prefix(ActiveHealthController __instance, EBodyPart bodyPart, float healthPenalty, ref bool __result)
        {
            var player = (Player)_playerField.GetValue(__instance);
            if (player.IsYourPlayer) 
            {
                //I had to do this previously due to the type being protected, no longer is the case. Keeping for reference.
                /* BodyPartStateWrapper bodyPartStateWrapper = GetBodyPartStateWrapper(__instance, bodyPart);*/

                HealthStateClass.BodyPartState bodyPartState = __instance.Dictionary_0[bodyPart];
                SkillManager skills = (SkillManager)_skillsField.GetValue(__instance);
                Action<EBodyPart, ValueStruct> bodyPartRestoredField = (Action<EBodyPart, ValueStruct>)_bodyPartRestoredField.GetValue(__instance);

                if (!bodyPartState.IsDestroyed)
                {
                    __result = false;
                    return false;
                }

                HealthValue health = bodyPartState.Health;
                bodyPartState.IsDestroyed = false;
                healthPenalty += (1f - healthPenalty) * skills.SurgeryReducePenalty;
                bodyPartState.Health = new HealthValue(1f, Mathf.Max(1f, Mathf.Ceil(bodyPartState.Health.Maximum * healthPenalty)), 0f);
                __instance.method_43(bodyPart, EDamageType.Medicine);
                __instance.method_35(bodyPart);
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
                if (PluginConfig.EnableMedicalLogging.Value)
                {
                    Logger.LogWarning("=========");
                    Logger.LogWarning("part = " + bodyPart);
                    Logger.LogWarning("type = " + damageInfo.DamageType);
                    Logger.LogWarning("damage = " + damage);
                    Logger.LogWarning("=========");
                }

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

    //Gear blocking won't work but it's better than nothing
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
                bool shouldAllowHeal = true;
                Plugin.RealHealthController.CanUseMedItemCommon(meds, __instance, ref bodyParts, ref shouldAllowHeal);
                return shouldAllowHeal;
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
                bool shouldAllowHeal = true;
                Plugin.RealHealthController.CanUseMedItemCommon(meds, __instance, ref bodyParts, ref shouldAllowHeal);
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
                 if (PluginConfig.EnableLogging.Value)
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
