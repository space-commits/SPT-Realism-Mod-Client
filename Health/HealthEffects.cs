﻿using EFT;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using static Systems.Effects.Effects;
using UnityEngine;
using System.Linq;
using BepInEx.Logging;
using EffectClass = EFT.HealthSystem.ActiveHealthController.GClass2234;
using ExistanceClass = GClass2275;

namespace RealismMod
{
    public enum EHealthEffectType 
    {
        Surgery,
        Tourniquet,
        HealthRegen,
        Adrenaline,
        ResourceRate,
        PainKiller
    }

    public interface IHealthEffect
    {
        public EBodyPart BodyPart { get; set; }
        public float? Duration { get; }
        public float TimeExisted { get; set; }
        public void Tick();
        public Player Player { get; }
        public float Delay { get; set; }
        public EHealthEffectType EffectType { get; }
    }

    public class TourniquetEffect : IHealthEffect
    {
        public EBodyPart BodyPart { get; set; }
        public float? Duration { get; }
        public float TimeExisted { get; set; }
        public float HpPerTick { get; }
        public Player Player { get; }
        public float Delay { get; set; }
        public EHealthEffectType EffectType { get; }
        private bool haveNotified = false;

        public TourniquetEffect(float hpTick, float? dur, EBodyPart part, Player player, float delay)
        {
            TimeExisted = 0f;
            HpPerTick = -hpTick;
            Duration = dur;
            BodyPart = part;
            Player = player;
            Delay = delay;
            EffectType = EHealthEffectType.Tourniquet; 
        }

        public void Tick()
        {
            if (Delay <= 0f) 
            {
                if (!haveNotified) 
                {
                    NotificationManagerClass.DisplayWarningNotification("Tourniquet Applied On " + BodyPart + ", You Are Losing Health On This Limb. Use A Surgery Kit To Remove It.", EFT.Communications.ENotificationDurationType.Long);
                    haveNotified = true;
                }

                float currentPartHP = Player.ActiveHealthController.GetBodyPartHealth(BodyPart).Current;

                TimeExisted += 3f;

                if (TimeExisted > 10f && (int)(Time.time % 10) == 0) 
                {
                    RealismHealthController.RemoveBaseEFTEffect(Player, BodyPart, "HeavyBleeding");
                    RealismHealthController.RemoveBaseEFTEffect(Player, BodyPart, "LightBleeding");
                }
               
                if (currentPartHP > 25f)
                {
                    MethodInfo addEffectMethod = RealismHealthController.GetAddBaseEFTEffectMethodInfo();
                    Type healthChangeType = typeof(HealthChange);
                    MethodInfo genericEffectMethod = addEffectMethod.MakeGenericMethod(healthChangeType);
                    HealthChange healthChangeInstance = new HealthChange();
                    genericEffectMethod.Invoke(Player.ActiveHealthController, new object[] { BodyPart, 0f, 3f, 1f, HpPerTick, null });
                }
            }
        }
    }

    public class SurgeryEffect : IHealthEffect
    {
        public EBodyPart BodyPart { get; set; }
        public float? Duration { get; set; }
        public float TimeExisted { get; set; }
        public float HpPerTick { get; }
        public Player Player { get; }
        public float HpRegened { get; set; }
        public float Delay { get; set; }
        public EHealthEffectType EffectType { get; }
        public float HpRegenLimit { get; }
        private bool hasRemovedTrnqt = false;
        private bool haveNotified = false;

        public SurgeryEffect(float hpTick, float? dur, EBodyPart part, Player player, float delay, float limit)
        {
            TimeExisted = 0;
            HpRegened = 0;
            HpPerTick = hpTick;
            Duration = dur;
            BodyPart = part;
            Player = player;
            Delay = delay;
            HpRegenLimit = limit;
            EffectType = EHealthEffectType.Surgery;
        }

        public void Tick()
        {
            if (Delay <= 0f)
            {
                if (!haveNotified) 
                {
                    NotificationManagerClass.DisplayMessageNotification("Surgery Kit Applied On " + BodyPart + ", Restoring HP.", EFT.Communications.ENotificationDurationType.Long);
                    haveNotified = true; 
                }

                if (!hasRemovedTrnqt && RealismMod.RealismHealthController.HasEffectOfType(typeof(TourniquetEffect), BodyPart))
                {
                    RealismHealthController.RemoveEffectOfType(typeof(TourniquetEffect), BodyPart);
                    hasRemovedTrnqt = true;
                    NotificationManagerClass.DisplayMessageNotification("Surgical Kit Used, Removing Tourniquet Effect Present On Limb: " + BodyPart, EFT.Communications.ENotificationDurationType.Long);
                }

                float currentHp = Player.ActiveHealthController.GetBodyPartHealth(BodyPart).Current;
                float maxHp = Player.ActiveHealthController.GetBodyPartHealth(BodyPart).Maximum;
                float maxHpRegen = maxHp * HpRegenLimit;

                if (HpRegened < maxHpRegen)
                {
                    MethodInfo addEffectMethod = RealismHealthController.GetAddBaseEFTEffectMethodInfo();
                    Type healthChangeType = typeof(HealthChange);
                    MethodInfo genericEffectMethod = addEffectMethod.MakeGenericMethod(healthChangeType);
                    HealthChange healthChangeInstance = new HealthChange();
                    genericEffectMethod.Invoke(Player.ActiveHealthController, new object[] { BodyPart, 0f, 3f, 1f, HpPerTick, null });
                    HpRegened += HpPerTick;
                }
                if (HpRegened >= maxHpRegen || currentHp == maxHp)
                {
                    NotificationManagerClass.DisplayMessageNotification("Surgical Kit Health Regeneration On " + BodyPart + " Has Expired", EFT.Communications.ENotificationDurationType.Long);
                    Duration = 0;
                    return;
                }

            }
        }
    }

    public class HealthRegenEffect : IHealthEffect
    {
        public EBodyPart BodyPart { get; set; }
        public float? Duration { get; set; }
        public float TimeExisted { get; set; }
        public float HpPerTick { get; }
        public Player Player { get; }
        public float HpRegened { get; set; }
        public float HpRegenLimit { get; }
        public float Delay { get; set; }
        public EDamageType DamageType { get; }
        public EHealthEffectType EffectType { get; }
        private bool deductedRecordedDamage = false;

        public HealthRegenEffect(float hpTick, float? dur, EBodyPart part, Player player, float delay, float limit, EDamageType damageType)
        {
            TimeExisted = 0;
            HpRegened = 0;
            HpRegenLimit = limit;
            HpPerTick = hpTick;
            Duration = dur;
            BodyPart = part;
            Player = player;
            Delay = delay;
            DamageType = damageType;
            EffectType = EHealthEffectType.HealthRegen;    
        }

        public void Tick()
        {
            float currentHp = Player.ActiveHealthController.GetBodyPartHealth(BodyPart).Current;
            float maxHp = Player.ActiveHealthController.GetBodyPartHealth(BodyPart).Maximum;

            if (!deductedRecordedDamage) 
            {
                if (DamageType == EDamageType.HeavyBleeding)
                {
                    DamageTracker.TotalHeavyBleedDamage = Mathf.Max(DamageTracker.TotalHeavyBleedDamage - HpRegenLimit, 0f);
                }
                if (DamageType == EDamageType.LightBleeding)
                {
                    DamageTracker.TotalLightBleedDamage = Mathf.Max(DamageTracker.TotalLightBleedDamage - HpRegenLimit, 0f);

                }
                deductedRecordedDamage = true;
            }

            if (HpRegened < HpRegenLimit)
            {
                if (Delay <= 0f)
                {
                    MethodInfo addEffectMethod = RealismHealthController.GetAddBaseEFTEffectMethodInfo();
                    Type healthChangeType = typeof(HealthChange);
                    MethodInfo genericEffectMethod = addEffectMethod.MakeGenericMethod(healthChangeType);
                    HealthChange healthChangeInstance = new HealthChange();
                    genericEffectMethod.Invoke(Player.ActiveHealthController, new object[] { BodyPart, 0f, 3f, 1f, HpPerTick, null });
                    HpRegened += HpPerTick;
                }
            }

            if (HpRegened >= HpRegenLimit || (currentHp >= maxHp) || currentHp == 0)
            {
                Duration = 0;
            }
        }
    }

    public class ResourceRateEffect : IHealthEffect
    {
        public EBodyPart BodyPart { get; set; }
        public float? Duration { get; set; }
        public float TimeExisted { get; set; }
        public float ResourcePerTick { get; }
        public Player Player { get; }
        public float Delay { get; set; }
        public EHealthEffectType EffectType { get; }

        public ResourceRateEffect(float resourcePerTick, float? dur, Player player, float delay)
        {
            TimeExisted = 0;
            Duration = dur;
            Player = player;
            Delay = delay;
            EffectType = EHealthEffectType.ResourceRate;
            BodyPart = EBodyPart.Stomach;
            ResourcePerTick = resourcePerTick;
        }

        public void Tick()
        {
            if (Delay <= 0f)
            {
                Duration -= 3;
                MethodInfo addEffectMethod = RealismHealthController.GetAddBaseEFTEffectMethodInfo();
                Type resourceRatesType = typeof(ResourceRateChange);
                MethodInfo genericEffectMethod = addEffectMethod.MakeGenericMethod(resourceRatesType);
                ResourceRateChange healthChangeInstance = new ResourceRateChange();
                genericEffectMethod.Invoke(Player.ActiveHealthController, new object[] { BodyPart, 0f, 3f, 0f, ResourcePerTick, null });
            }
        }
    }

    public class PainKillerEffect : IHealthEffect
    {
        public EBodyPart BodyPart { get; set; }
        public float? Duration { get; set; }
        public float TimeExisted { get; set; }
        public Player Player { get; }
        public float Delay { get; set; }
        public EHealthEffectType EffectType { get; }
        public float TunnelVisionStrength { get; }
        public float IntermittentWaitDur { get; }
        public float IntermittentEffectDur { get;}
        private float waitCounter { get; set; }
        private float durCounter { get; set; }
        private bool addedEffect = false;
        private bool canSkipWait = true;
        public float PainKillerStrength { get; set; }

        public PainKillerEffect(float? dur, Player player, float delay, float intermittentWaitDur, float intermittentEffectDur, float tunnelStrength, float painKillerStrength)
        {
            TimeExisted = 0;
            Duration = dur;
            BodyPart = EBodyPart.Head;
            Player = player;
            Delay = delay;
            EffectType = EHealthEffectType.PainKiller;
            IntermittentWaitDur = intermittentWaitDur;
            IntermittentEffectDur = intermittentEffectDur;
            waitCounter = intermittentWaitDur;
            durCounter = intermittentEffectDur;
            TunnelVisionStrength = tunnelStrength;
            PainKillerStrength = painKillerStrength;    
        }

        public void Tick()
        {
            if (Delay <= 0f)
            {
                Duration -= 3f;
                waitCounter -= 3f;

                if (waitCounter <= 0f || canSkipWait) 
                {
                    durCounter -= 3f;

                    if (!addedEffect) 
                    {
                        canSkipWait = false;
                        addedEffect = true;

                        if (PainKillerStrength >= RealismHealthController.PainStrength) 
                        {
                            RealismHealthController.AddBasesEFTEffect(Player, "PainKiller", BodyPart, 0f, IntermittentEffectDur, 1f, 1f);
                            RealismHealthController.AddBasesEFTEffect(Player, "TunnelVision", BodyPart, 0f, IntermittentEffectDur, 1f, TunnelVisionStrength);
                        }
                    }

                    if (durCounter <= 0f) 
                    {
                        waitCounter = IntermittentWaitDur;
                        durCounter = IntermittentEffectDur;
                        addedEffect = false;
                    }
                }

            }
        }
    }

    public class HealthChange : EffectClass, IEffect, GInterface191, GInterface211
    {
        protected override void Started()
        {
            this.hpPerTick = base.Strength;
            this.SetHealthRatesPerSecond(this.hpPerTick, 0f, 0f, 0f);
            this.bodyPart = base.BodyPart;
        }

        protected override void RegularUpdate(float deltaTime)
        {
            this.time += deltaTime;
            if (this.time < 3f)
            {
                return;
            }
            this.time -= 3f;
            base.HealthController.ChangeHealth(bodyPart, this.hpPerTick, ExistanceClass.Existence);
        }

        private float hpPerTick;

        private float time;

        private EBodyPart bodyPart;

    }

    public class ResourceRateChange : EffectClass, IEffect, GInterface191, GInterface211
    {
        protected override void Started()
        {
            this.resourcePerTick = base.Strength;
            this.bodyPart = base.BodyPart;
            this.SetHealthRatesPerSecond(0f, -this.resourcePerTick, -this.resourcePerTick, 0f);
        }

        protected override void RegularUpdate(float deltaTime)
        {
            this.time += deltaTime;
            if (this.time < 3f)
            {
                return;
            }
            this.time -= 3f;
            base.HealthController.ChangeEnergy(-this.resourcePerTick);
            base.HealthController.ChangeHydration(-this.resourcePerTick);
        }

        private float resourcePerTick;

        private float time;

        private EBodyPart bodyPart;
    }

}
