using Comfort.Common;
using EFT;
using EFT.HealthSystem;
using EFT.InventoryLogic;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using BrokenBoneInterface = GInterface245;
using ContusionInterface = GInterface255;
using DamageTypeClass = GClass2456;
using DehydrationInterface = GInterface246;
using EffectClass = EFT.HealthSystem.ActiveHealthController.GClass2415;
using ExhaustionInterface = GInterface247;
using HeavyBleedingInterface = GInterface243;
using LightBleedingInterface = GInterface242;
using PainInterface = GInterface259;
using PainKillerInterface = GInterface260;
using TremorInterface = GInterface263;
using TunnelVisionInterface = GInterface265;
using ToxinInterface = GInterface249;
using LethalToxinInterface = GInterface250;

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

        public static int PainKillerDuration(Item med)
        {
            return !Utils.IsNull(med.ConflictingItems) && int.TryParse(med.ConflictingItems[5], out int result) ? result : 1;
        }

        public static float HPRestoreAmount(Item med)
        {
            return !Utils.IsNull(med.ConflictingItems, 7) && float.TryParse(med.ConflictingItems[6], out float result) ? result : 1;
        }

        public static int Unused2(Item med)
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
            { "PainKiller", typeof(PainKillerInterface) },
            { "Tremor", typeof(TremorInterface) },
            { "BrokenBone", typeof(BrokenBoneInterface) },
            { "TunnelVision", typeof(TunnelVisionInterface) },
            { "Contusion", typeof(ContusionInterface)  },
            { "HeavyBleeding", typeof(HeavyBleedingInterface) },
            { "LightBleeding", typeof(LightBleedingInterface) },
            { "Dehydration", typeof(DehydrationInterface) },
            { "Exhaustion", typeof(ExhaustionInterface) },
            { "LethalToxin", typeof(ToxinInterface) },
            { "Intoxication", typeof(LethalToxinInterface) }
        };
    }

    public enum EStimType 
    {
        Regenerative,
        Damage,
        Adrenal,
        Clotting,
        Temperature,
        Performance,
        Generic,
        Weight
    }

    public class RealismHealthController
    {

        public Dictionary<string, EStimType> StimTypes = new Dictionary<string, EStimType>()
        {
            {"5c10c8fd86f7743d7d706df3", EStimType.Adrenal},
            {"5ed515e03a40a50460332579", EStimType.Adrenal},
            {"637b620db7afa97bfc3d7009", EStimType.Adrenal},
            {"5c0e533786f7747fa23f4d47", EStimType.Clotting},
            {"5ed515f6915ec335206e4152", EStimType.Clotting},
            {"5ed515ece452db0eb56fc028", EStimType.Damage},
            {"637b6179104668754b72f8f5", EStimType.Damage},
            {"5ed5160a87bb8443d10680b5", EStimType.Performance},
            {"5ed515c8d380ab312177c0fa", EStimType.Performance},
            {"5c0e531286f7747fa54205c2", EStimType.Performance},
            {"5c0e531d86f7747fa23f4d42", EStimType.Performance},
            {"5c0e530286f7747fa1419862", EStimType.Regenerative},
            {"5c0e534186f7747fa1419867", EStimType.Regenerative},
            {"SJ0", EStimType.Regenerative},
            {"637b6251104668754b72f8f9", EStimType.Generic},
            {"637b612fb7afa97bfc3d7005", EStimType.Generic},
            {"5fca13ca637ee0341a484f46", EStimType.Generic},
            {"637b60c3b7afa97bfc3d7001", EStimType.Generic},
            {"5ed5166ad380ab312177c100", EStimType.Generic},
            {"5ed51652f6c34d2cc26336a1", EStimType.Weight }
        };

        public List<EBodyPart> PossibleBodyParts = new List<EBodyPart> 
        {
            EBodyPart.Head, EBodyPart.Chest, EBodyPart.Stomach, EBodyPart.RightLeg, EBodyPart.LeftLeg, EBodyPart.RightArm, EBodyPart.LeftArm 
        };

        private List<EStimType> activeStimOverdoses = new List<EStimType>();

        public DamageTracker DmgeTracker { get; }

        public PlayerHazardBridge PlayerHazardBridge { get;  private set; }

        public bool HasAdrenalineEffect { get; set; } = false;

        public float AdrenalineMovementBonus
        {
            get
            {
                return HasAdrenalineEffect ? 1f + Mathf.Pow(PlayerState.StressResistanceFactor, 1.5f) : 1f;
            }
        }

        public float AdrenalineReloadBonus
        {
            get
            {
                return HasAdrenalineEffect ? 1f + Mathf.Pow(PlayerState.StressResistanceFactor, 1.25f) : 1f;
            }
        }

        public float AdrenalineStanceBonus
        {
            get
            {
                return HasAdrenalineEffect ? 1f + Mathf.Pow(PlayerState.StressResistanceFactor, 1.25f) : 1f;
            }
        }

        public float AdrenalineADSBonus
        {
            get
            {
                return HasAdrenalineEffect ? 1f + (PlayerState.StressResistanceFactor * 1.5f) : 1f;
            }
        }

        public EBodyPart[] BodyParts = { EBodyPart.Head, EBodyPart.Chest, EBodyPart.Stomach, EBodyPart.RightLeg, EBodyPart.LeftLeg, EBodyPart.RightArm, EBodyPart.LeftArm };

        private List<ICustomHealthEffect> activeHealthEffects = new List<ICustomHealthEffect>();


        private float _healthControllerTime = 0f;
        private float _effectsTime = 0f;
        private float _reliefWaitTime = 0f;

        private float _stimOverdoseWaitTime = 0f;
        private bool _doStimOverdoseTimer = false;
        private string _overdoseEffectToAdd = "";

        private const float doubleClickTime = 0.2f;
        private float timeSinceLastClicked = 0f;
        private bool clickTriggered = false;

        private float adrenalineCooldownTime = 70f * (1f - PlayerState.StressResistanceFactor);
        public bool AdrenalineCooldownActive = false;

        //temporary solution
        private bool reset1 = false;
        private bool reset2 = false;
        private bool reset3 = false;
        private bool reset4 = false;
        private bool reset5 = false;

        private float baseMaxHPRestore = 86f;

        public float PainStrength = 0f;
        public float PainEffectThreshold = 10f;
        public float PainReliefStrength = 0f;
        public float PainTunnelStrength = 0f;
        public int ReliefDuration = 0;
        private bool hasPKStims = false;

        private const float _painReliefInterval = 15f;
        public const float PainArmThreshold = 30f;
        public const float PainReliefThreshold = 30f;
        public const float BasePKOverdoseThreshold = 45f;

        private const float ToxicityThreshold = 40f;
        private const float RadiationThreshold = 60f;
        private const float _baseToxicityRecoveryRate = -0.05f;
        private const float _hazardInterval = 10f;
        private float _hazardWaitTime = 0f;

        private bool _rightArmRuined = false;
        private bool _leftArmRuined = false;

        private bool _hasOverdosedStim = false;

        public float ResourcePerTick = 0;

        private bool haveNotifiedPKOverdose = false;

        private bool addedCustomEffectsToDict = false;

        public bool IsPoisoned { get; set; } = false;   

        public bool ArmsAreIncapacitated 
        {
            get 
            {
                return (_rightArmRuined || _leftArmRuined || (PainStrength > PainArmThreshold && PainStrength > PainReliefStrength)) && !IsOnPKStims;
            }
        }

        public bool HealthConditionPreventsTacSprint
        {
            get
            {
                return HazardTracker.TotalToxicity > ToxicityThreshold || HazardTracker.TotalRadiation > RadiationThreshold || ArmsAreIncapacitated || HasOverdosed || IsPoisoned;
            }
        }

        public bool HealthConditionForcedLowReady
        {
            get
            {
                return HazardTracker.TotalToxicity > ToxicityThreshold || HazardTracker.TotalRadiation > 80f || ArmsAreIncapacitated || HasOverdosed || IsPoisoned;
            }
        }

        public bool HasOverdosed
        {
            get
            {
                return PainReliefStrength > PKOverdoseThreshold || _hasOverdosedStim;
            }
        }

        public float PKOverdoseThreshold
        {
            get
            {
                return BasePKOverdoseThreshold * (1f + PlayerState.ImmuneSkillStrong);
            }
        }

        public bool IsOnPKStims
        {
            get
            {
                return hasPKStims && !_hasOverdosedStim;
            }
        }

        public RealismHealthController(DamageTracker dmgTracker) 
        {
            DmgeTracker = dmgTracker;
        }

        public void ControllerUpdate()
        {
            //needed for Fika
            if (!addedCustomEffectsToDict) 
            {
                AddCustomEffectsToDict();
                addedCustomEffectsToDict = true;
            }

            if (!Utils.IsInHideout && Utils.IsReady)
            {
                _healthControllerTime += Time.deltaTime;
                _effectsTime += Time.deltaTime;
                _reliefWaitTime += Time.deltaTime;
                _hazardWaitTime += Time.deltaTime;
                ControllerTick();

                if (Input.GetKeyDown(Plugin.AddEffectKeybind.Value.MainKey))
                {
/*                    AddStimDebuffs(Utils.GetYourPlayer(), Plugin.AddEffectType.Value);*/ // use this to test stim debuffs
                    TestAddBaseEFTEffect(Plugin.AddEffectBodyPart.Value, Utils.GetYourPlayer(), Plugin.AddEffectType.Value);
                    NotificationManagerClass.DisplayMessageNotification("Adding Health Effect " + Plugin.AddEffectType.Value + " To Part " + (EBodyPart)Plugin.AddEffectBodyPart.Value);
                }

                if (Input.GetKeyDown(Plugin.DropGearKeybind.Value.MainKey))
                {
                    if (clickTriggered)
                    {
                        DropBlockingGear(Utils.GetYourPlayer());
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

                if (_doStimOverdoseTimer)
                {
                    _stimOverdoseWaitTime += Time.deltaTime;
                    if (_stimOverdoseWaitTime >= 10f)
                    {
                        AddStimDebuffs(Utils.GetYourPlayer(), _overdoseEffectToAdd);
                        _doStimOverdoseTimer = false;
                        _stimOverdoseWaitTime = 0f;
                    }
                }
            }

            if (Utils.IsInHideout || !Utils.IsReady)
            {
                ResetAllEffects();
                DmgeTracker.ResetTracker();
            }
 
            if (AdrenalineCooldownActive && adrenalineCooldownTime > 0.0f)
            {
                adrenalineCooldownTime -= Time.deltaTime;
            }
            if (AdrenalineCooldownActive && adrenalineCooldownTime <= 0.0f)
            {
                adrenalineCooldownTime = 70f * (1f - PlayerState.StressResistanceFactor);
                AdrenalineCooldownActive = false;
            }
        }

        //To prevent null ref exceptions while using Fika, Realism's custom effects must be added to a dicitionary of existing EFT effects
        public void AddCustomEffectsToDict() 
        {
            Type type1 = typeof(GClass2463.GClass2464);
            FieldInfo dictionaryField1 = type1.GetField("dictionary_1", BindingFlags.NonPublic | BindingFlags.Static);
            var effectDict1 = (Dictionary<byte, string>)dictionaryField1.GetValue(null);

            effectDict1.Add(Convert.ToByte(effectDict1.Count + 1), "ResourceRateDrain");
            effectDict1.Add(Convert.ToByte(effectDict1.Count + 1), "HealthChange");
            effectDict1.Add(Convert.ToByte(effectDict1.Count + 1), "HealthDrain");

            dictionaryField1.SetValue(null, effectDict1);

            Type type0 = typeof(GClass2463.GClass2464);
            FieldInfo dictionaryField0 = type0.GetField("dictionary_0", BindingFlags.NonPublic | BindingFlags.Static);
            var effectDict0 = (Dictionary<string, byte>)dictionaryField0.GetValue(null);

            effectDict0.Add("ResourceRateDrain", Convert.ToByte(effectDict0.Count + 1));
            effectDict0.Add("HealthChange", Convert.ToByte(effectDict0.Count + 1));
            effectDict0.Add("HealthDrain", Convert.ToByte(effectDict0.Count + 1));

            dictionaryField0.SetValue(null, effectDict0);

            Type typeType = typeof(GClass2463.GClass2464);
            FieldInfo typeFieldInfo = typeType.GetField("type_0", BindingFlags.NonPublic | BindingFlags.Static);
            var typeArr = (Type[])typeFieldInfo.GetValue(null);

            Type[] customTypes = new Type[] { typeof(ResourceRateDrain), typeof(HealthChange), typeof(HealthDrain) };

            customTypes.CopyTo(typeArr, 0);
            typeFieldInfo.SetValue(null, customTypes);

        }


        public void TestAddBaseEFTEffect(int partIndex, Player player, String effect)
        {
            if (effect == "removeHP")
            {
                player.ActiveHealthController.ChangeHealth((EBodyPart)partIndex, -player.ActiveHealthController.GetBodyPartHealth((EBodyPart)partIndex).Maximum, DamageTypeClass.Existence);
                return;
            }
            if (effect == "addHP")
            {
                player.ActiveHealthController.ChangeHealth((EBodyPart)partIndex, player.ActiveHealthController.GetBodyPartHealth((EBodyPart)partIndex).Maximum, DamageTypeClass.Existence);
                return;
            }
            if (effect == "")
            {
                return;
            }

            Type effectType = typeof(EFT.HealthSystem.ActiveHealthController).GetNestedType(effect, BindingFlags.NonPublic | BindingFlags.Instance);
            if (effectType == null)
            {
                Utils.Logger.LogError("nest type is null: " + effect);
                return;
            }
            MethodInfo effectMethod = GetAddBaseEFTEffectMethodInfo();
            effectMethod.MakeGenericMethod(effectType).Invoke(player.ActiveHealthController, new object[] { (EBodyPart)partIndex, null, null, null, null, null });
        }

        public void AddBasesEFTEffect(Player player, String effect, EBodyPart bodyPart, float? delayTime, float? duration, float? residueTime, float? strength)
        {
            MethodInfo effectMethod = GetAddBaseEFTEffectMethodInfo();
            effectMethod.MakeGenericMethod(typeof(EFT.HealthSystem.ActiveHealthController).GetNestedType(effect, BindingFlags.NonPublic | BindingFlags.Instance)).Invoke(player.ActiveHealthController, new object[] { bodyPart, delayTime, duration, residueTime, strength, null });
        }

        public void AddBaseEFTEffectIfNoneExisting(Player player, string effect, EBodyPart bodyPart, float delayTime, float duration, float residueTime, float strength)
        {
            if (!player.ActiveHealthController.BodyPartEffects.Effects[0].Any(e => e.Key == effect))
            {
                AddBasesEFTEffect(player, effect, bodyPart, delayTime, duration, residueTime, strength);
            }
        }

        public void AddToExistingBaseEFTEffect(Player player, string targetEffect, EBodyPart bodyPart, float delayTime, float? duration, float residueTime, float strength)
        {
            if (!player.ActiveHealthController.BodyPartEffects.Effects[0].Any(e => e.Key == targetEffect))
            {
                AddBasesEFTEffect(player, targetEffect, bodyPart, delayTime, duration, residueTime, strength);
            }
            else
            {
                IReadOnlyList<EffectClass> effectsList = player.ActiveHealthController.IReadOnlyList_0;
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

        public void TryAddAdrenaline(Player player, float painkillerDuration, float negativeEffectDuration, float negativeEffectStrength)
        {
            if (Plugin.EnableAdrenaline.Value && !AdrenalineCooldownActive)
            {
                AdrenalineCooldownActive = true;
                AdrenalineEffect adrenalineEffect = new AdrenalineEffect(player, (int)painkillerDuration, 0, negativeEffectDuration, painkillerDuration, negativeEffectStrength, this);
                Plugin.RealHealthController.AddCustomEffect(adrenalineEffect, true);
            }
        }

        public MethodInfo GetAddBaseEFTEffectMethodInfo()
        {
            MethodInfo effectMethodInfo = typeof(EFT.HealthSystem.ActiveHealthController).GetMethods(BindingFlags.Public | BindingFlags.Instance).First(e =>
            e.GetParameters().Length == 6
            && e.GetParameters()[0].Name == "bodyPart"
            && e.GetParameters()[5].Name == "initCallback"
            && e.IsGenericMethod);
            return effectMethodInfo;
        }

        public void RemoveBaseEFTEffect(Player player, EBodyPart targetBodyPart, string targetEffect)
        {
            IEnumerable<IEffect> commonEffects = player.ActiveHealthController.GetAllActiveEffects(targetBodyPart);
            IReadOnlyList<EffectClass> effectsList = player.ActiveHealthController.IReadOnlyList_0;

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

        public void AddCustomEffect(ICustomHealthEffect newEffect, bool canStack)
        {
            //need to decide if it's better to keep the old effect or to replace it with a new one.
            if (!canStack)
            {
                foreach (ICustomHealthEffect existingEff in activeHealthEffects)
                {
                    if (existingEff.GetType() == newEffect.GetType() && existingEff.BodyPart == newEffect.BodyPart)
                    {
                        RemoveCustomEffectOfType(newEffect.GetType(), newEffect.BodyPart);
                        break;
                    }
                }
            }

            activeHealthEffects.Add(newEffect);
        }

        public bool AddCustomEffectIfNoneExisting(ICustomHealthEffect newEffect)
        {
            foreach (ICustomHealthEffect existingEff in activeHealthEffects)
            {
                if (existingEff.GetType() == newEffect.GetType() && existingEff.BodyPart == newEffect.BodyPart)
                {
                    return false;
                }
            }

            activeHealthEffects.Add(newEffect);
            return true;    
        }


        public void RemoveCustomEffectOfType(Type effect, EBodyPart bodyPart)
        {
            for (int i = activeHealthEffects.Count - 1; i >= 0; i--)
            {
                ICustomHealthEffect activeHealthEffect = activeHealthEffects[i];
                if (activeHealthEffect.GetType() == effect && activeHealthEffect.BodyPart == bodyPart)
                {
                    activeHealthEffects.RemoveAt(i);
                }
            }
        }

        public bool HasCustomEffectOfType(Type effect, EBodyPart bodyPart)
        {
            bool hasEffect = false;
            for (int i = activeHealthEffects.Count - 1; i >= 0; i--)
            {
                ICustomHealthEffect activeHealthEffect = activeHealthEffects[i];
                if (activeHealthEffect.GetType() == effect && activeHealthEffect.BodyPart == bodyPart)
                {
                    hasEffect = true;
                }
            }
            return hasEffect;
        }

        public void CancelPendingEffects()
        {
            for (int i = activeHealthEffects.Count - 1; i >= 0; i--)
            {
                if (activeHealthEffects[i].Delay > 0f)
                {
                    activeHealthEffects.RemoveAt(i);
                }
            }
        }

        public void RemoveRegenEffectsOfType(EDamageType damageType)
        {
            List<HealthRegenEffect> regenEffects = activeHealthEffects.OfType<HealthRegenEffect>().ToList();
            regenEffects.RemoveAll(r => r.DamageType == damageType);
            activeHealthEffects.RemoveAll(a => !regenEffects.Contains(a));
        }

        public void RemoveEffectsOfType(EHealthEffectType effectType)
        {
            activeHealthEffects.RemoveAll(a => a.EffectType == effectType);
        }

        public void ResetAllEffects()
        {
            activeStimOverdoses.Clear();
            activeHealthEffects.Clear();
            PainStrength = 0f;
            PainReliefStrength = 0f;
            PainTunnelStrength = 0f;
            ReliefDuration = 0;
            _hasOverdosedStim = false;
            _leftArmRuined = false;  
            _rightArmRuined = false;
            resetHealhPenalties();
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

                if (heavyBleedType != null && effects.Any(h => h.Type == heavyBleedType))
                {
                    hasHeavyBleed = true;
                }
                if (lightBleedType != null && effects.Any(l => l.Type == lightBleedType))
                {
                    hasLightBleed = true;
                }
            }
            if (!hasHeavyBleed)
            {
                DmgeTracker.TotalHeavyBleedDamage = 0f;
            }
            if (!hasLightBleed)
            {
                DmgeTracker.TotalLightBleedDamage = 0f;
            }
        }

        public bool HasBaseEFTEffect(Player player, string targetEffect)
        {
            IEnumerable<IEffect> commonEffects = player.ActiveHealthController.GetAllEffects();
            IReadOnlyList<EffectClass> effectsList = player.ActiveHealthController.IReadOnlyList_0;
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


        private void AddStimDebuffs(Player player, string debuffId)
        {
            MedsClass placeHolderItem = (MedsClass)Singleton<ItemFactory>.Instance.CreateItem(Utils.GenId(), debuffId, null);
            placeHolderItem.CurrentAddress = player.GClass2761_0.FindQuestGridToPickUp(placeHolderItem); //item needs an address to be valid, this is a hacky workaround
            player.ActiveHealthController.DoMedEffect(placeHolderItem, EBodyPart.Head, null);

            if (Plugin.EnableLogging.Value)
            {
                Utils.Logger.LogWarning("is null " + (placeHolderItem == null));
                Utils.Logger.LogWarning("" + placeHolderItem.HealthEffectsComponent.StimulatorBuffs);
                Utils.Logger.LogWarning("added " + debuffId);
            }
        }

        private void EvaluateStimSingles(Player player, IEnumerable<IGrouping<EStimType, StimShellEffect>> stimGroups)
        {

            hasPKStims = false;
            foreach (var group in stimGroups) 
            {
                switch (group.Key)
                {
                    case EStimType.Adrenal:
                        activeStimOverdoses.Remove(EStimType.Adrenal);
                        hasPKStims = true;
                        break;
                    case EStimType.Regenerative:
                        activeStimOverdoses.Remove(EStimType.Regenerative);
                        break;
                    case EStimType.Damage:
                        activeStimOverdoses.Remove(EStimType.Damage);
                        hasPKStims = true;
                        break;
                    case EStimType.Clotting:
                        activeStimOverdoses.Remove(EStimType.Clotting);
                        break;
                    case EStimType.Weight:
                        activeStimOverdoses.Remove(EStimType.Weight);
                        break;
                    case EStimType.Performance:
                        activeStimOverdoses.Remove(EStimType.Performance);
                        break;
                    case EStimType.Generic:
                        activeStimOverdoses.Remove(EStimType.Generic);
                        break;
                }
            }
        }

        private void EvaluateStimDuplicates(Player player, IEnumerable<IGrouping<EStimType, StimShellEffect>> stimGroups) 
        {
            foreach (var group in stimGroups) // use this to count duplicates per category
            {
                switch (group.Key)
                {
                    case EStimType.Adrenal:
                        if (!activeStimOverdoses.Contains(EStimType.Adrenal)) //if no active adrenal overdose
                        {
                        
                            activeStimOverdoses.Add(EStimType.Adrenal);
                            _doStimOverdoseTimer = true;
                            _overdoseEffectToAdd = "adrenal_debuff";
                            NotificationManagerClass.DisplayWarningNotification("Overdosed On Adrenal Stims", EFT.Communications.ENotificationDurationType.Long);
                        }
                        break;
                    case EStimType.Regenerative:
                        if (!activeStimOverdoses.Contains(EStimType.Regenerative)) 
                        {
                            activeStimOverdoses.Add(EStimType.Regenerative);
                            _doStimOverdoseTimer = true;
                            _overdoseEffectToAdd = "regen_debuff";
                            NotificationManagerClass.DisplayWarningNotification("Overdosed On Regenerative Stims", EFT.Communications.ENotificationDurationType.Long);
                        }
                        break;
                    case EStimType.Damage:
                        if (!activeStimOverdoses.Contains(EStimType.Damage))
                        {
                            activeStimOverdoses.Add(EStimType.Damage);
                            _doStimOverdoseTimer = true;
                            _overdoseEffectToAdd = "damage_debuff";
                            NotificationManagerClass.DisplayWarningNotification("Overdosed On Combat Stims", EFT.Communications.ENotificationDurationType.Long);
                        }
                        break;
                    case EStimType.Clotting:
                        if (!activeStimOverdoses.Contains(EStimType.Clotting))
                        {
                            activeStimOverdoses.Add(EStimType.Clotting);
                            AddStimDebuffs(player, "clotting_debuff");
                            NotificationManagerClass.DisplayWarningNotification("Overdosed On Coagulating Stims", EFT.Communications.ENotificationDurationType.Long);
                        }

                        break;
                    case EStimType.Weight:
                        if (!activeStimOverdoses.Contains(EStimType.Weight))
                        {
                            activeStimOverdoses.Add(EStimType.Weight);
                            _doStimOverdoseTimer = true;
                            _overdoseEffectToAdd = "weight_debuff";
                            NotificationManagerClass.DisplayWarningNotification("Overdosed On Weight-Reducing Stims", EFT.Communications.ENotificationDurationType.Long);
                        }
                        break;
                    case EStimType.Performance:
                        if (!activeStimOverdoses.Contains(EStimType.Performance))
                        {
                            activeStimOverdoses.Add(EStimType.Performance);
                            _doStimOverdoseTimer = true;
                            _overdoseEffectToAdd = "performance_debuff";
                            NotificationManagerClass.DisplayWarningNotification("Overdosed On Performance-Enhancing Stims", EFT.Communications.ENotificationDurationType.Long);
                        }
                        break;
                    case EStimType.Generic:
                        if (!activeStimOverdoses.Contains(EStimType.Generic))
                        {
                            activeStimOverdoses.Add(EStimType.Generic);
                            _doStimOverdoseTimer = true;
                            _overdoseEffectToAdd = "generic_debuff";
                            NotificationManagerClass.DisplayWarningNotification("Overdosed On Stims", EFT.Communications.ENotificationDurationType.Long);
                        }
                        break;
                }
            }
        }

        public void EvaluateActiveStims(Player player) 
        {
            IEnumerable<StimShellEffect> activeStims = activeHealthEffects.OfType<StimShellEffect>();
            var stimTypeGroups = activeStims.GroupBy(effect => effect.StimType);
            var duplicatesGrouping = stimTypeGroups.Where(group => group.Count() > 1);
            var singlesGrouping = stimTypeGroups.Where(group => group.Count() <= 1);
            int totalDuplicates = duplicatesGrouping.Sum(group => group.Count());
            EvaluateStimDuplicates(player, duplicatesGrouping);
            EvaluateStimSingles(player, singlesGrouping);
            if (totalDuplicates > 1)
            {
                _hasOverdosedStim = true;
            }
            else _hasOverdosedStim = false;
        }

        public EStimType GetStimType(string id) 
        {
            return StimTypes.TryGetValue(id, out EStimType type) ? type : EStimType.Generic;
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
                RemoveRegenEffectsOfType(EDamageType.Dehydration);
            }
            if (!isDehydrated && DmgeTracker.TotalDehydrationDamage > 0f)
            {
                RestoreHPArossBody(player, DmgeTracker.TotalDehydrationDamage, delay, EDamageType.Dehydration, tickRate);
                DmgeTracker.TotalDehydrationDamage = 0;
            }

            if (isExhausted)
            {
                RemoveRegenEffectsOfType(EDamageType.Exhaustion);
            }
            if (!isExhausted && DmgeTracker.TotalExhaustionDamage > 0f)
            {
                RestoreHPArossBody(player, DmgeTracker.TotalExhaustionDamage, delay, EDamageType.Exhaustion, tickRate);
                DmgeTracker.TotalExhaustionDamage = 0;
            }
        }

        private void PainReliefCheck(Player player)
        {
            if (PainStrength >= PainEffectThreshold && !IsOnPKStims)
            {
                AddBaseEFTEffectIfNoneExisting(player, "Pain", EBodyPart.Chest, 0f, 15f, 1f, 1f);
            }

            ReliefDuration = Math.Max(ReliefDuration - 1, 0);
            if (ReliefDuration > 0) 
            {
                if (PainReliefStrength >= PainStrength || IsOnPKStims)
                {
                    AddBaseEFTEffectIfNoneExisting(player, "PainKiller", EBodyPart.Head, 1f, ReliefDuration, 5f, 1f);
                }
                else if (PainStrength > PainReliefStrength)
                {
                    RemoveBaseEFTEffect(player, EBodyPart.Head, "PainKiller");                  
                }

                if (_reliefWaitTime >= _painReliefInterval)
                {
                    AddBasesEFTEffect(player, "TunnelVision", EBodyPart.Head, 1f, _painReliefInterval, 5f, PainTunnelStrength);

                    if (PainReliefStrength > PKOverdoseThreshold)
                    {
                        if (!haveNotifiedPKOverdose) 
                        {
                            NotificationManagerClass.DisplayWarningNotification("You Have Overdosed.", EFT.Communications.ENotificationDurationType.Long);
                            haveNotifiedPKOverdose = true;
                        }
                        AddBasesEFTEffect(player, "Contusion", EBodyPart.Head, 1f, _painReliefInterval, 5f, 0.35f);
                        AddToExistingBaseEFTEffect(player, "Tremor", EBodyPart.Head, 1f, _painReliefInterval, 5f, 1f);
                    }
                    else haveNotifiedPKOverdose = false;

                    _reliefWaitTime = 0f;
                }
            }
        }

        private void TickEffects() 
        {
            for (int i = activeHealthEffects.Count - 1; i >= 0; i--)
            {
                ICustomHealthEffect effect = activeHealthEffects[i];
          /*      if (Plugin.EnableLogging.Value)
                {
                    Utils.Logger.LogWarning("Type = " + effect.GetType().ToString());
                    Utils.Logger.LogWarning("Delay = " + effect.Delay);
                }*/

                effect.Delay = Math.Max(effect.Delay - 1, 0);

                if (effect.Duration == null || effect.Duration > 0f)
                {
                    effect.Tick();
                }
                else
                {
                    if (Plugin.EnableLogging.Value)
                    {
                        Utils.Logger.LogWarning("Removing Effect Due to Duration");
                    }
                    activeHealthEffects.RemoveAt(i);
                }
            }
        }

        //replace all this logic with a schedular class to control execution timing
        public void ControllerTick()
        {
            Player player = Singleton<GameWorld>.Instance.MainPlayer;
            PlayerState.ImmuneSkillWeak = player.Skills.ImmunityPainKiller.Value;
            PlayerState.ImmuneSkillStrong = player.Skills.ImmunityMiscEffects.Value;
            PlayerState.StressResistanceFactor = player.Skills.StressPain.Value;

            if (_healthControllerTime >= 0.5f && !reset1)
            {
                ResetBleedDamageRecord(player);
                reset1 = true;
            }
            if (_healthControllerTime >= 1f && !reset2)
            {
                ResourceRegenCheck(player);
                reset2 = true;
            }
            if (_healthControllerTime >= 2f && !reset3)
            {
                DoubleBleedCheck(player);
                reset3 = true;
            }
            if (_healthControllerTime >= 2.5f && !reset4)
            {
                EvaluateActiveStims(player);
                reset4 = true;
            }
            if (_healthControllerTime >= 3f && !reset5)
            {
                PlayerInjuryStateCheck(player);
                reset5 = true;
            }


            if (_effectsTime >= 1f)
            {
                HazardZoneHealthEffectTick(player);
                PainReliefCheck(player);
                TickEffects();
                _effectsTime = 0f;
            }

            DoResourceDrain(player.ActiveHealthController, Time.deltaTime);

            if (_healthControllerTime >= 3f) 
            {
                _healthControllerTime = 0f;
                reset1 = false;
                reset2 = false;
                reset3 = false;
                reset4 = false;
                reset5 = false;
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
                EquipmentClass equipment = player.Equipment;

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
            EquipmentClass equipment = player.Equipment;
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

            ConsumeAlcohol(player, item);
            CheckIfReducesHazardInRaid(item, player, false);
            PlayerState.BlockFSWhileConsooming = true;
        }

        private void ConsumeAlcohol(Player player, Item item) 
        {
            if (MedProperties.MedType(item) == "alcohol") 
            {
                AddPainkillerEffect(player, item);
            }
        }

        public void DoPassiveRegen(float tickRate, EBodyPart bodyPart, Player player, int delay, float hpToRestore, EDamageType damageType)
        {
            if (!HasCustomEffectOfType(typeof(TourniquetEffect), bodyPart))
            {
                HealthRegenEffect regenEffect = new HealthRegenEffect(tickRate, null, bodyPart, player, delay, hpToRestore, damageType, this);
                AddCustomEffect(regenEffect, false);
            }
        }

        public void RestoreHPArossBody(Player player, float hpToRestore, int delay, EDamageType damageType, float tickRate)
        {
            hpToRestore = Mathf.RoundToInt((hpToRestore) / BodyParts.Length);

            foreach (EBodyPart part in BodyParts)
            {
                if (!HasCustomEffectOfType(typeof(TourniquetEffect), part))
                {
                    HealthRegenEffect regenEffect = new HealthRegenEffect(tickRate, null, part, player, delay, hpToRestore, damageType, this);
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
                if (part != bodyPart && !HasCustomEffectOfType(typeof(TourniquetEffect), part))
                {
                    HealthRegenEffect regenEffect = new HealthRegenEffect(tickRate, null, part, player, delay, hpToRestore, damageType, this);
                    AddCustomEffect(regenEffect, false);
                }
            }
        }

        private void handleHeavyBleedHeal(string medType, MedsClass meds, EBodyPart bodyPart, Player player, string hBleedHealType, bool isNotLimb, float vitalitySkill, float regenTickRate)
        {
            int delay = (int)meds.HealthEffectsComponent.UseTime;

            NotificationManagerClass.DisplayMessageNotification("Heavy Bleed On " + bodyPart + " Healed, Restoring HP.", EFT.Communications.ENotificationDurationType.Long);

            float trnqtTickRate = (float)Math.Round(MedProperties.HpPerTick(meds) * (1f - vitalitySkill), 2);
            float maxHpToRestore = Mathf.Round(baseMaxHPRestore * (1f + vitalitySkill));
            float hpToRestore = Mathf.Min(DmgeTracker.TotalHeavyBleedDamage, maxHpToRestore);

            if ((hBleedHealType == "combo" || hBleedHealType == "trnqt") && !isNotLimb)
            {
                TourniquetEffect trnqt = new TourniquetEffect(trnqtTickRate, null, bodyPart, player, delay, this);
                AddCustomEffect(trnqt, false);

                if (DmgeTracker.TotalHeavyBleedDamage > 0f)
                {
                    TrnqtRestoreHPArossBody(player, hpToRestore, delay, bodyPart, EDamageType.HeavyBleeding, vitalitySkill);
                }
            }
            else if (DmgeTracker.TotalHeavyBleedDamage > 0f)
            {
                RestoreHPArossBody(player, hpToRestore, delay, EDamageType.HeavyBleeding, regenTickRate);
            }
            DmgeTracker.TotalHeavyBleedDamage = Mathf.Max(DmgeTracker.TotalHeavyBleedDamage - hpToRestore, 0f);
        }

        private void handleLightBleedHeal(string medType, MedsClass meds, EBodyPart bodyPart, Player player, bool isNotLimb, float vitalitySkill, float regenTickRate)
        {
            int delay = (int)meds.HealthEffectsComponent.UseTime;

            NotificationManagerClass.DisplayMessageNotification("Light Bleed On " + bodyPart + " Healed, Restoring HP.", EFT.Communications.ENotificationDurationType.Long);

            float trnqtTickRate = (float)Math.Round(MedProperties.HpPerTick(meds) * (1f - vitalitySkill), 2);
            float maxHpToRestore = Mathf.Round(baseMaxHPRestore * (1f + vitalitySkill));
            float hpToRestore = Mathf.Min(DmgeTracker.TotalLightBleedDamage, maxHpToRestore);

            if (medType == "trnqt" && !isNotLimb)
            {
                TourniquetEffect trnqt = new TourniquetEffect(trnqtTickRate, null, bodyPart, player, delay, this);
                AddCustomEffect(trnqt, false);

                if (DmgeTracker.TotalLightBleedDamage > 0f)
                {
                    TrnqtRestoreHPArossBody(player, hpToRestore, delay, bodyPart, EDamageType.LightBleeding, vitalitySkill);
                }
            }
            else if (DmgeTracker.TotalLightBleedDamage > 0f)
            {
                RestoreHPArossBody(player, hpToRestore, delay, EDamageType.LightBleeding, regenTickRate);
            }
            DmgeTracker.TotalLightBleedDamage = Mathf.Max(DmgeTracker.TotalLightBleedDamage - hpToRestore, 0f);
        }

        private void handleSurgery(string medType, MedsClass meds, EBodyPart bodyPart, Player player, float surgerySkill)
        {
            int delay = (int)meds.HealthEffectsComponent.UseTime;
            float regenLimitFactor = 0.5f * (1f + surgerySkill);
            float surgTickRate = (float)Math.Round(MedProperties.HpPerTick(meds) * (1f + surgerySkill), 2);
            SurgeryEffect surg = new SurgeryEffect(surgTickRate, null, bodyPart, player, delay, regenLimitFactor, this);
            AddCustomEffect(surg, false);
        }

        private void handleSplint(MedsClass meds, float regenTickRate, EBodyPart bodyPart, Player player)
        {
            NotificationManagerClass.DisplayMessageNotification("Fracture On " + bodyPart + " Healed, Restoring HP.", EFT.Communications.ENotificationDurationType.Long);
            int delay = (int)meds.HealthEffectsComponent.UseTime;
            HealthRegenEffect regenEffect = new HealthRegenEffect(regenTickRate, null, bodyPart, player, delay, 12f, EDamageType.Impact, this);
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

        private void AddPainkillerEffect(Player player, Item item) 
        {
            int duration = (int)(MedProperties.PainKillerDuration(item) * (1f + PlayerState.ImmuneSkillWeak));
            int delay = (int)Mathf.Round(MedProperties.Delay(item) * (1f - player.Skills.HealthEnergy.Value));
            float tunnelVisionStr = MedProperties.TunnelVisionStrength(item) * (1f - PlayerState.ImmuneSkillWeak);
            float painKillStr = MedProperties.Strength(item);

            PainKillerEffect painKillerEffect = new PainKillerEffect(duration, player, delay, tunnelVisionStr, painKillStr, this);
            Plugin.RealHealthController.AddCustomEffect(painKillerEffect, true);
        }

        public void CheckIfReducesHazardInStash(Item item, bool isMed, HealthControllerClass hc)
        {
            if (HazardTracker.TotalToxicity <= 0) return;

            GClass1235 details = null;
            if (isMed && item as MedsClass != null)
            {
                MedsClass med = item as MedsClass;
                details = med.HealthEffectsComponent.DamageEffects.ContainsKey(EDamageEffectType.Intoxication) ? med.HealthEffectsComponent.DamageEffects[EDamageEffectType.Intoxication] : null;
            }
            if (!isMed && item as FoodClass != null)
            {
                FoodClass food = item as FoodClass;
                details = food.HealthEffectsComponent.DamageEffects.ContainsKey(EDamageEffectType.Intoxication) ? food.HealthEffectsComponent.DamageEffects[EDamageEffectType.Intoxication] : null;
            }

            if (details != null)
            {
                float strength = details.FadeOut;
                int duration = (int)details.Duration;
                HazardTracker.TotalToxicity -= strength * duration;
                HazardTracker.UpdateHazardValues(Plugin.PMCProfileId);
                HazardTracker.SaveHazardValues();

                //doesn't work :(
         /*       if (isMed)
                {
                    var med = item as MedsClass;
                    med.MedKitComponent.HpResource -= 1f;
                    med.MedKitComponent.Item.RaiseRefreshEvent(false, true);
                }*/
            }

        }

        public void CheckIfReducesHazardInRaid(Item item, Player player, bool isMed) 
        {
            GClass1235 detoxDetails = null;
            GClass1235 deradDetails = null;
            if (isMed && item as MedsClass != null)
            {
                MedsClass med = item as MedsClass;
                detoxDetails = med.HealthEffectsComponent.DamageEffects.ContainsKey(EDamageEffectType.Intoxication) ? med.HealthEffectsComponent.DamageEffects[EDamageEffectType.Intoxication] : null;
                deradDetails = med.HealthEffectsComponent.DamageEffects.ContainsKey(EDamageEffectType.RadExposure) ? med.HealthEffectsComponent.DamageEffects[EDamageEffectType.RadExposure] : null;
            }
            if (!isMed && item as FoodClass != null)
            {
                FoodClass food = item as FoodClass;
                detoxDetails = food.HealthEffectsComponent.DamageEffects.ContainsKey(EDamageEffectType.Intoxication) ? food.HealthEffectsComponent.DamageEffects[EDamageEffectType.Intoxication] : null;
                deradDetails = food.HealthEffectsComponent.DamageEffects.ContainsKey(EDamageEffectType.RadExposure) ? food.HealthEffectsComponent.DamageEffects[EDamageEffectType.RadExposure] : null;
            }

            if (detoxDetails != null)
            {
                float strength = -detoxDetails.FadeOut;
                int delay = (int)detoxDetails.Delay;
                int duration = (int)detoxDetails.Duration;

                DetoxificationEffect detox = new DetoxificationEffect(player, duration, delay, this, strength);
                Plugin.RealHealthController.AddCustomEffect(detox, true);
            }
            if (deradDetails != null)
            {
                float strength = -deradDetails.FadeOut;
                int delay = (int)deradDetails.Delay;
                int duration = (int)deradDetails.Duration;

                RadationTreatmentEffect derad = new RadationTreatmentEffect(player, duration, delay, this, strength);
                Plugin.RealHealthController.AddCustomEffect(derad, true);
            }
        }

        public void CanUseMedItemCommon(MedsClass meds, Player player, ref EBodyPart bodyPart, ref bool shouldAllowHeal) 
        {
            CheckIfReducesHazardInRaid(meds, player, true); //the type of items that can reduce toxicity and radiation can't be blocked so should be fine

            if (meds.Template._parent == "5448f3a64bdc2d60728b456a")
            {
                int duration = (int)meds.HealthEffectsComponent.BuffSettings[0].Duration * 2;
                int delay = Mathf.Max((int)meds.HealthEffectsComponent.BuffSettings[0].Delay, 5);
                EStimType stimType = Plugin.RealHealthController.GetStimType(meds.Template._id);

                StimShellEffect stimEffect = new StimShellEffect(player, duration, delay, stimType, this);
                Plugin.RealHealthController.AddCustomEffect(stimEffect, true);

                shouldAllowHeal = true;
                return;
            }

            string medType = MedProperties.MedType(meds);

            if (MedProperties.CanBeUsedInRaid(meds) == false)
            {
                NotificationManagerClass.DisplayWarningNotification("This Item Can Not Be Used In Raid", EFT.Communications.ENotificationDurationType.Long);
                shouldAllowHeal = false;
                return;
            }

            float medHPRes = meds.MedKitComponent.HpResource;
            string hBleedHealType = MedProperties.HBleedHealType(meds);

            bool canHealFract = meds.HealthEffectsComponent.DamageEffects.ContainsKey(EDamageEffectType.Fracture) && ((medType == "medkit" && medHPRes >= 3) || medType != "medkit");
            bool canHealLBleed = meds.HealthEffectsComponent.DamageEffects.ContainsKey(EDamageEffectType.LightBleeding);
            bool canHealHBleed = meds.HealthEffectsComponent.DamageEffects.ContainsKey(EDamageEffectType.HeavyBleeding) && ((medType == "medkit" && medHPRes >= 3) || medType != "medkit");

            if (bodyPart == EBodyPart.Common)
            {
                EquipmentClass equipment = (EquipmentClass)AccessTools.Property(typeof(Player), "Equipment").GetValue(player);
                Item head = equipment.GetSlot(EquipmentSlot.Headwear).ContainedItem;
                Item ears = equipment.GetSlot(EquipmentSlot.Earpiece).ContainedItem;
                Item glasses = equipment.GetSlot(EquipmentSlot.Eyewear).ContainedItem;
                Item face = equipment.GetSlot(EquipmentSlot.FaceCover).ContainedItem;
                Item vest = equipment.GetSlot(EquipmentSlot.ArmorVest).ContainedItem;
                Item tacrig = equipment.GetSlot(EquipmentSlot.TacticalVest).ContainedItem;
                Item bag = equipment.GetSlot(EquipmentSlot.Backpack).ContainedItem;

                bool mouthBlocked = Plugin.RealHealthController.MouthIsBlocked(head, face, equipment);
                bool hasBodyGear = vest != null || tacrig != null; //|| bag != null
                bool hasHeadGear = head != null || ears != null || face != null;

                FaceShieldComponent fsComponent = player.FaceShieldObserver.Component;
                NightVisionComponent nvgComponent = player.NightVisionObserver.Component;
                bool fsIsON = fsComponent != null && (fsComponent.Togglable == null || fsComponent.Togglable.On);
                bool nvgIsOn = nvgComponent != null && (nvgComponent.Togglable == null || nvgComponent.Togglable.On);

                if (Plugin.GearBlocksHeal.Value && medType.Contains("pills") && (mouthBlocked || fsIsON || nvgIsOn))
                {
                    NotificationManagerClass.DisplayWarningNotification("Can't Take Pills, Mouth Is Blocked By Faceshield/NVGs/Mask. Toggle Off Faceshield/NVG Or Remove Mask/Headgear", EFT.Communications.ENotificationDurationType.Long);
                    shouldAllowHeal = false;
                    return;
                }
                if (medType.Contains("pain"))
                {
                    AddPainkillerEffect(player, meds);
                    shouldAllowHeal = true;
                    return;
                }
                if (medType.Contains("pills") || medType.Contains("drug"))
                {
                    shouldAllowHeal = true;
                    return;
                }

                Type heavyBleedType;
                Type lightBleedType;
                Type fractureType;
                MedProperties.EffectTypes.TryGetValue("HeavyBleeding", out heavyBleedType);
                MedProperties.EffectTypes.TryGetValue("LightBleeding", out lightBleedType);
                MedProperties.EffectTypes.TryGetValue("BrokenBone", out fractureType);

                if (medType == "surg")
                {
                    bool isHead = false;
                    bool isBody = false;
                    bool isNotLimb = false;

                    bodyPart = Plugin.RealHealthController.BodyParts
                        .Where(b => player.ActiveHealthController.GetBodyPartHealth(b).Current / player.ActiveHealthController.GetBodyPartHealth(b).Maximum < 1)
                        .OrderBy(b => player.ActiveHealthController.GetBodyPartHealth(b).Current / player.ActiveHealthController.GetBodyPartHealth(b).Maximum).FirstOrDefault();

                    //IDE is a liar, it can be null
#pragma warning disable CS0472 
                    if (bodyPart == null) bodyPart = EBodyPart.Common;
#pragma warning restore CS0472 

                    if (bodyPart == EBodyPart.Common)
                    {
                        NotificationManagerClass.DisplayWarningNotification("No Suitable Bodypart Was Found For Surgery Kit", EFT.Communications.ENotificationDurationType.Long);
                        shouldAllowHeal = false;
                        return;
                    }

                    Plugin.RealHealthController.GetBodyPartType(bodyPart, ref isNotLimb, ref isHead, ref isBody);

                    if (Plugin.GearBlocksHeal.Value && ((isBody && hasBodyGear) || (isHead && hasHeadGear)))
                    {
                        NotificationManagerClass.DisplayWarningNotification("Gear Is Blocking Wound", EFT.Communications.ENotificationDurationType.Long);
                    }
                    Plugin.RealHealthController.HandleHealthEffects(medType, meds, bodyPart, player, hBleedHealType, canHealHBleed, canHealLBleed, canHealFract);
                    return;
                }
                else 
                {
                    foreach (EBodyPart part in Plugin.RealHealthController.BodyParts)
                    {
                        bool isHead = false;
                        bool isBody = false;
                        bool isNotLimb = false;

                        Plugin.RealHealthController.GetBodyPartType(part, ref isNotLimb, ref isHead, ref isBody);

                        bool hasHeavyBleed = false;
                        bool hasLightBleed = false;
                        bool hasFracture = false;

                        IEnumerable<IEffect> effects = Plugin.RealHealthController.GetInjuriesOnBodyPart(player, part, ref hasHeavyBleed, ref hasLightBleed, ref hasFracture);

                        float currentHp = player.ActiveHealthController.GetBodyPartHealth(part).Current;
                        float maxHp = player.ActiveHealthController.GetBodyPartHealth(part).Maximum;


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
                }

                if (bodyPart == EBodyPart.Common)
                {
                    if (medType == "vas")
                    {
                        shouldAllowHeal = true;
                        return;
                    }

                    NotificationManagerClass.DisplayWarningNotification("No Suitable Bodypart Was Found For Healing, Gear May Be Covering The Wound.", EFT.Communications.ENotificationDurationType.Long);

                    shouldAllowHeal = false;
                    return;
                }
            }

            //determine if any effects should be applied based on what is being healed
            if (bodyPart != EBodyPart.Common)
            {
                Plugin.RealHealthController.HandleHealthEffects(medType, meds, bodyPart, player, hBleedHealType, canHealHBleed, canHealLBleed, canHealFract);
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
            EquipmentClass equipment = player.Equipment;

            Item head = equipment.GetSlot(EquipmentSlot.Headwear).ContainedItem;
            Item ears = equipment.GetSlot(EquipmentSlot.Earpiece).ContainedItem;
            Item glasses = equipment.GetSlot(EquipmentSlot.Eyewear).ContainedItem;
            Item face = equipment.GetSlot(EquipmentSlot.FaceCover).ContainedItem;
            Item vest = equipment.GetSlot(EquipmentSlot.ArmorVest).ContainedItem;
            Item tacrig = equipment.GetSlot(EquipmentSlot.TacticalVest).ContainedItem;
            Item bag = equipment.GetSlot(EquipmentSlot.Backpack).ContainedItem;

            bool mouthBlocked = MouthIsBlocked(head, face, equipment);

            bool hasHeadGear = head != null || ears != null || face != null;
            bool hasBodyGear = vest != null || tacrig != null; // bag != null

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
                NotificationManagerClass.DisplayWarningNotification(bodyPart + " Has Gear On, Remove Gear First To Be Able To Heal", EFT.Communications.ENotificationDurationType.Long);

                canUse = false;
                return;
            }

            bool hasHeavyBleed = false;
            bool hasLightBleed = false;
            bool hasFracture = false;

            IEnumerable<IEffect> effects = GetInjuriesOnBodyPart(player, bodyPart, ref hasHeavyBleed, ref hasLightBleed, ref hasFracture);

            bool canHealLightBleed = med.HealthEffectsComponent.DamageEffects.ContainsKey(EDamageEffectType.LightBleeding) && med.HealthEffectsComponent.DamageEffects[EDamageEffectType.LightBleeding].Cost + 1 <= med.MedKitComponent.HpResource && hasLightBleed;
            bool canHealHeavyBleed = med.HealthEffectsComponent.DamageEffects.ContainsKey(EDamageEffectType.HeavyBleeding) && med.HealthEffectsComponent.DamageEffects[EDamageEffectType.HeavyBleeding].Cost + 1 <= med.MedKitComponent.HpResource && hasHeavyBleed;
            bool canHealFracture = med.HealthEffectsComponent.DamageEffects.ContainsKey(EDamageEffectType.Fracture) && med.HealthEffectsComponent.DamageEffects[EDamageEffectType.Fracture].Cost + 1 <= med.MedKitComponent.HpResource && hasFracture;
            bool partHasTreatableInjury = canHealLightBleed || canHealHeavyBleed || canHealFracture;


            if (medType == "medkit" && !partHasTreatableInjury)
            {
                canUse = false;
                return;
            }

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
                IReadOnlyList<EffectClass> effectsList = player.ActiveHealthController.IReadOnlyList_0;

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

        private void resetHealhPenalties() 
        {
            PlayerState.AimMoveSpeedInjuryMulti = 1;
            PlayerState.ADSInjuryMulti = 1;
            PlayerState.StanceInjuryMulti = 1;
            PlayerState.ReloadInjuryMulti = 1;
            PlayerState.HealthSprintSpeedFactor = 1;
            PlayerState.HealthSprintAccelFactor = 1;
            PlayerState.HealthWalkSpeedFactor = 1;
            PlayerState.HealthStamRegenFactor =1;
            PlayerState.ErgoDeltaInjuryMulti = 1;
            PlayerState.RecoilInjuryMulti = 1;

        }

        private void DoResourceDrain(ActiveHealthController hc, float dt)
        {
            hc.ChangeEnergy(-ResourcePerTick * dt * Plugin.EnergyRateMulti.Value);
            hc.ChangeHydration(-ResourcePerTick * dt * Plugin.HydrationRateMulti.Value);
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
            float resourceRateInjuryMulti = 0f;

            float drugFactor = _hasOverdosedStim ? 90f + PainReliefStrength : PainReliefStrength;
            float resourcePainReliefFactor = drugFactor / 200f;

            float currentEnergy = player.ActiveHealthController.Energy.Current;
            float maxEnergy = player.ActiveHealthController.Energy.Maximum;
            float percentEnergy = currentEnergy / maxEnergy;

            float currentHydro = player.ActiveHealthController.Hydration.Current;
            float maxHydro = player.ActiveHealthController.Hydration.Maximum;
            float percentHydro = currentHydro / maxHydro;

            IsPoisoned = HasBaseEFTEffect(player, "LethalToxin");

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
                    PainStrength += 15f * (1f - PlayerState.StressResistanceFactor);
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
                float percentHpReload = 1f - ((1f - percentHp) / (isLeftArm ? 2f : isRightArm ? 3f : 4f));
                float percentHpRecoil = 1f - ((1f - percentHp) / (isLeftArm ? 10f : 20f));

                if (currentHp <= 0f) PainStrength += 20f * (1f - PlayerState.StressResistanceFactor);
                else if (percentHp <= 0.5f) PainStrength += 5f * (1f - PlayerState.StressResistanceFactor);

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
                    bool isArmRuined = (currentHp <= 0f || hasFracture) && !HasBaseEFTEffect(player, "PainKiller");
                    if (isLeftArm)
                    {
                        _leftArmRuined = isArmRuined;
                    }
                    if (isRightArm)
                    {
                        _rightArmRuined = isArmRuined;
                    }

                    float armFractureFactor = isLeftArm && hasFracture ? 0.75f : isRightArm && hasFracture ? 0.85f : 1f;

                    aimMoveSpeedMulti *= percentHpAimMove * armFractureFactor;
                    adsInjuryMulti *= percentHpADS * armFractureFactor;
                    stanceInjuryMulti *= percentHpStance * armFractureFactor;
                    reloadInjuryMulti *= percentHpReload * armFractureFactor;
                    ergoDeltaInjuryMulti *= (1f + (1f - percentHp)) * armFractureFactor;
                    recoilInjuryMulti *= (1f + (1f - percentHpRecoil)) * armFractureFactor;
                }
            }

            float totalHpPercent = totalCurrentHp / totalMaxHp;
            resourceRateInjuryMulti = Mathf.Clamp(1f - totalHpPercent, 0f, 1f) * 0.15f;
            float percentEnergyFactor = Mathf.Max(percentEnergy * 1.1f, 0.01f);

            float percentEnergySprint = 1f - ((1f - percentEnergyFactor) / 8f);
            float percentEnergyWalk = 1f - ((1f - percentEnergyFactor) / 12f);
            float percentEnergyAimMove = 1f - ((1f - percentEnergyFactor) / 20f);
            float percentEnergyADS = 1f - ((1f - percentEnergyFactor) / 5f);
            float percentEnergyStance = 1f - ((1f - percentEnergyFactor) / 2f);
            float percentEnergyReload = 1f - ((1f - percentEnergyFactor) / 10f);
            float percentEnergyRecoil = 1f - ((1f - percentEnergyFactor) / 40f);
            float percentEnergyErgo = 1f - ((1f - percentEnergyFactor) / 2f);
            float percentEnergyStamRegen = 1f - ((1f - percentEnergyFactor) / 10f);

            float percentHydroLowerLimit = (1f - ((1f - percentHydro) / 4f));
            float percentHydroLimitRecoil = (1f + ((1f - percentHydro) / 20f));
            float percentHydroLimitErgo = (1f + ((1f - percentHydro) / 4f));

            float painFactor = Mathf.Max(PainStrength - PainReliefStrength, 0f);
            painFactor = _hasOverdosedStim ? 90f + painFactor : painFactor;
            float painKillerFactor = Mathf.Clamp(1f - (painFactor / 1000f), 0.85f, 1f);
            float painKillerFactorInverse = Mathf.Clamp(1f + (painFactor / 1000f), 1f, 1.15f);
            float skillFactor = (1f + (player.Skills.HealthEnergy.Value / 4));
            float skillFactorInverse = (1f - (player.Skills.HealthEnergy.Value / 4));

            float toxicity = HazardTracker.TotalToxicity / 100f; 
            float toxicityFactor = 1f - toxicity;
            float toxicityInverse = 1f + toxicity;

            float radiation = HazardTracker.TotalRadiation / 150f;
            float radiationFactor = 1f - radiation;
            float radiationInverse = 1f + radiation;

            float poisonDebuffFactor = IsPoisoned ? 0.8f : 1f;
            float poisonDebuffFactorInverse = IsPoisoned ? 1.2f : 1f;

            float hazardFactor = toxicityFactor * radiationFactor * poisonDebuffFactor;
            float hazardFactorInverse = toxicityInverse * radiationInverse * poisonDebuffFactorInverse;

            PlayerState.AimMoveSpeedInjuryMulti = Mathf.Clamp(aimMoveSpeedMulti * percentEnergyAimMove * painKillerFactor * skillFactor * hazardFactor, 0.6f * percentHydroLowerLimit, 1f);
            PlayerState.ADSInjuryMulti = Mathf.Clamp(adsInjuryMulti * percentEnergyADS * painKillerFactor * skillFactor * hazardFactor, 0.35f * percentHydroLowerLimit, 1f);
            PlayerState.StanceInjuryMulti = Mathf.Clamp(stanceInjuryMulti * percentEnergyStance * painKillerFactor * skillFactor * hazardFactor, 0.65f * percentHydroLowerLimit, 1f);
            PlayerState.ReloadInjuryMulti = Mathf.Clamp(reloadInjuryMulti * percentEnergyReload * painKillerFactor * skillFactor * hazardFactor, 0.75f * percentHydroLowerLimit, 1f);
            PlayerState.HealthSprintSpeedFactor = Mathf.Clamp(sprintSpeedInjuryMulti * percentEnergySprint * painKillerFactor * skillFactor * hazardFactor, 0.4f * percentHydroLowerLimit, 1f);
            PlayerState.HealthSprintAccelFactor = Mathf.Clamp(sprintAccelInjuryMulti * percentEnergySprint * painKillerFactor * skillFactor * hazardFactor, 0.4f * percentHydroLowerLimit, 1f);
            PlayerState.HealthWalkSpeedFactor = Mathf.Clamp(walkSpeedInjuryMulti * percentEnergyWalk * painKillerFactor * skillFactor * hazardFactor, 0.6f * percentHydroLowerLimit, 1f);
            PlayerState.HealthStamRegenFactor = Mathf.Clamp(stamRegenInjuryMulti * percentEnergyStamRegen * painKillerFactor * skillFactor * hazardFactor, 0.5f * percentHydroLowerLimit, 1f);
            PlayerState.ErgoDeltaInjuryMulti = Mathf.Clamp(ergoDeltaInjuryMulti * (1f + (1f - percentEnergyErgo)) * painKillerFactorInverse * skillFactorInverse * hazardFactorInverse, 1f, 1.3f * percentHydroLimitErgo);
            PlayerState.RecoilInjuryMulti = Mathf.Clamp(recoilInjuryMulti * (1f + (1f - percentEnergyRecoil)) * painKillerFactorInverse * skillFactorInverse * hazardFactorInverse, 1f, 1.12f * percentHydroLimitRecoil);

            if (Plugin.ResourceRateChanges.Value)
            {
                if (!HasCustomEffectOfType(typeof(ResourceRateEffect), EBodyPart.Chest))
                {
                    ResourceRateEffect resEffect = new ResourceRateEffect(null, player, 0, this);
                    AddCustomEffect(resEffect, false);
                }
                else
                {
                    float playerWeightFactor = PlayerState.TotalModifiedWeight >= 10f ? PlayerState.TotalModifiedWeight / 500f : 0f;
                    float sprintMulti = PlayerState.IsSprinting ? 1.45f : 1f;
                    float sprintFactor = PlayerState.IsSprinting ? 0.1f : 0f;
                    float poisonSprintFactor = IsPoisoned ? 1.5f : 1f;
                    float totalResourceRate = (resourceRateInjuryMulti + resourcePainReliefFactor + sprintFactor + playerWeightFactor) * sprintMulti * (1f - player.Skills.HealthEnergy.Value) * toxicityInverse * radiationInverse * poisonSprintFactor;
                    ResourcePerTick = totalResourceRate;
                }
            }
        }

        private void HazardZoneHealthEffectTick(Player player) 
        {
            if (!GameWorldController.GameStarted) return;

            if (PlayerHazardBridge == null)
            {
                PlayerHazardBridge = player.gameObject.GetComponent<PlayerHazardBridge>();
            }

            GasZoneTick(player);
            RadiationZoneTick(player);
            HazardEffectsTick(player);
        }

        private void RadiationZoneTick(Player player) 
        {
            if (PlayerHazardBridge.IsInRadZone && GearController.CurrentRadProtection < 1f)
            {
                float increase = (PlayerHazardBridge.RadAmount + HazardTracker.RadiationRateMeds) * (1f - GearController.CurrentRadProtection) * (1f - PlayerState.ImmuneSkillWeak);
                increase = Mathf.Max(increase, 0f);
                HazardTracker.TotalRadiation += increase;
                HazardTracker.TotalRadiationRate = increase;
            }
            else if (HazardTracker.TotalRadiation > 0f || GearController.CurrentRadProtection >= 1f)
            {
                float reduction = HazardTracker.RadiationRateMeds * (1f + PlayerState.ImmuneSkillStrong);
                float threshold = HazardTracker.TotalRadiation <= 20f ? 0f : HazardTracker.GetNextLowestHazardLevel((int)HazardTracker.TotalRadiation);
                HazardTracker.TotalRadiation = Mathf.Clamp(HazardTracker.TotalRadiation + reduction, threshold, 100f);
                HazardTracker.TotalRadiationRate = HazardTracker.TotalRadiation == threshold ? 0f : reduction;
            }
            else
            {
                HazardTracker.TotalRadiationRate = 0f;
            }

            if (HazardTracker.TotalRadiation <= RadiationThreshold)
            {
                RemoveCustomEffectOfType(typeof(RadiationEffect), EBodyPart.Chest);
            }
        }

        private void GasZoneTick(Player player) 
        {
            if (PlayerHazardBridge.IsInGasZone && GearController.CurrentGasProtection < 1f)
            {
                float increase = (PlayerHazardBridge.GasAmount + HazardTracker.ToxicityRateMeds) * (1f - GearController.CurrentGasProtection) * (1f - PlayerState.ImmuneSkillWeak);
                increase = Mathf.Max(increase, 0f);
                HazardTracker.TotalToxicity += increase;
                HazardTracker.TotalToxicityRate = increase;
            }
            else if (HazardTracker.TotalToxicity > 0f || GearController.CurrentGasProtection >= 1f)
            {
                float reduction = (_baseToxicityRecoveryRate + HazardTracker.ToxicityRateMeds) * (1f + PlayerState.ImmuneSkillStrong);
                float threshold = HazardTracker.ToxicityRateMeds < 0f ? 0f : HazardTracker.GetNextLowestHazardLevel((int)HazardTracker.TotalToxicity);
                HazardTracker.TotalToxicity = Mathf.Clamp(HazardTracker.TotalToxicity + reduction, threshold, 100f);
                HazardTracker.TotalToxicityRate = HazardTracker.TotalToxicity == threshold ? 0f : reduction;
            }
            else
            {
                HazardTracker.TotalToxicityRate = 0f;
            }

            if (HazardTracker.TotalToxicity <= ToxicityThreshold)
            {
                RemoveCustomEffectOfType(typeof(ToxicityEffect), EBodyPart.Chest);
            }
        }

        private void HazardEffectsTick(Player player) 
        {
            if (_hazardWaitTime > _hazardInterval) 
            {
                if (HazardTracker.TotalToxicity >= 10f)
                {
                    if (HazardTracker.TotalToxicity >= ToxicityThreshold && !HasCustomEffectOfType(typeof(ToxicityEffect), EBodyPart.Chest))
                    {
                        ToxicityEffect toxicity = new ToxicityEffect(null, player, 0, this);
                        AddCustomEffect(toxicity, false);
                    }

                    float effectStrength = HazardTracker.TotalToxicity / 100f;
                    AddBasesEFTEffect(player, "TunnelVision", EBodyPart.Head, 1f, _hazardInterval, 5f, Mathf.Min(effectStrength * 2f, 1f));
                    if (HazardTracker.TotalToxicity >= ToxicityThreshold) AddToExistingBaseEFTEffect(player, "Contusion", EBodyPart.Head, 1f, _hazardInterval, 5f, effectStrength * 0.7f);
                }

                if (HazardTracker.TotalRadiation >= 40f)
                {
                    if (HazardTracker.TotalRadiation >= RadiationThreshold && !HasCustomEffectOfType(typeof(ToxicityEffect), EBodyPart.Chest))
                    {
                        RadiationEffect radiation = new RadiationEffect(null, player, 0, this);
                        AddCustomEffect(radiation, false);
                    }

                    float effectStrength = HazardTracker.TotalRadiation / 100f;
                    AddBasesEFTEffect(player, "TunnelVision", EBodyPart.Head, 1f, _hazardInterval, 5f, Mathf.Min(effectStrength, 1f));
                }

                _hazardWaitTime = 0f;
            }
        }
    }
}
 