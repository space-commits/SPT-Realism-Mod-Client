using EFT;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using static Systems.Effects.Effects;
using UnityEngine;
using System.Linq;
using BepInEx.Logging;
using EffectClass = EFT.HealthSystem.ActiveHealthController.GClass2411;
using ExistanceClass = GClass2452;

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
        public int? Duration { get; }
        public int TimeExisted { get; set; }
        public void Tick();
        public Player _Player { get; }
        public int Delay { get; set; }
        public EHealthEffectType EffectType { get; }
    }

    public class TourniquetEffect : IHealthEffect
    {
        public EBodyPart BodyPart { get; set; }
        public int? Duration { get; }
        public int TimeExisted { get; set; }
        public float HpPerTick { get; }
        public Player _Player { get; }
        public int Delay { get; set; }
        public EHealthEffectType EffectType { get; }
        private bool haveNotified = false;

        public TourniquetEffect(float hpTick, int? dur, EBodyPart part, Player player, int delay)
        {
            TimeExisted = 0;
            HpPerTick = -hpTick;
            Duration = dur;
            BodyPart = part;
            _Player = player;
            Delay = delay;
            EffectType = EHealthEffectType.Tourniquet;
        }

        public void Tick()
        {
            if (Delay <= 0)
            {
                TimeExisted++;
       
                if (!haveNotified)
                {
                    NotificationManagerClass.DisplayWarningNotification("Tourniquet Applied On " + BodyPart + ", You Are Losing Health On This Limb. Use A Surgery Kit To Remove It.", EFT.Communications.ENotificationDurationType.Long);
                    haveNotified = true;
                }

                float currentPartHP = _Player.ActiveHealthController.GetBodyPartHealth(BodyPart).Current;

                if (TimeExisted > 10 && TimeExisted % 10 == 0)
                {
                    Plugin.RealHealthController.RemoveBaseEFTEffect(_Player, BodyPart, "HeavyBleeding");
                    Plugin.RealHealthController.RemoveBaseEFTEffect(_Player, BodyPart, "LightBleeding");
                }

                if (currentPartHP > 25f && TimeExisted % 3 == 0) 
                {
                    _Player.ActiveHealthController.AddEffect<HealthChange>(BodyPart, 0f, 3f, 1f, HpPerTick, null);
                }
            }
        }
    }

    public class SurgeryEffect : IHealthEffect
    {
        public EBodyPart BodyPart { get; set; }
        public int? Duration { get; set; }
        public int TimeExisted { get; set; }
        public float HpPerTick { get; }
        public Player _Player { get; }
        public float HpRegened { get; set; }
        public int Delay { get; set; }
        public EHealthEffectType EffectType { get; }
        public float HpRegenLimit { get; }
        private bool hasRemovedTrnqt = false;
        private bool haveNotified = false;

        public SurgeryEffect(float hpTick, int? dur, EBodyPart part, Player player, int delay, float limit)
        {
            TimeExisted = 0;
            HpRegened = 0;
            HpPerTick = hpTick;
            Duration = dur;
            BodyPart = part;
            _Player = player;
            Delay = delay;
            HpRegenLimit = limit;
            EffectType = EHealthEffectType.Surgery;
        }

        public void Tick()
        {
            if (Delay <= 0)
            {
                TimeExisted++;

                if (!haveNotified)
                {
                    NotificationManagerClass.DisplayMessageNotification("Surgery Kit Applied On " + BodyPart + ", Restoring HP.", EFT.Communications.ENotificationDurationType.Long);
                    haveNotified = true;
                }

                if (!hasRemovedTrnqt && RealismMod.Plugin.RealHealthController.HasCustomEffectOfType(typeof(TourniquetEffect), BodyPart))
                {
                    Plugin.RealHealthController.RemoveCustomEffectOfType(typeof(TourniquetEffect), BodyPart);
                    hasRemovedTrnqt = true;
                    NotificationManagerClass.DisplayMessageNotification("Surgical Kit Used, Removing Tourniquet Effect Present On Limb: " + BodyPart, EFT.Communications.ENotificationDurationType.Long);
                }

                float currentHp = _Player.ActiveHealthController.GetBodyPartHealth(BodyPart).Current;
                float maxHp = _Player.ActiveHealthController.GetBodyPartHealth(BodyPart).Maximum;
                float maxHpRegen = maxHp * HpRegenLimit;

                if (HpRegened < maxHpRegen && TimeExisted % 3 == 0)
                {
                    _Player.ActiveHealthController.AddEffect<HealthChange>(BodyPart, 0f, 3f, 1f, HpPerTick, null);
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
        public int? Duration { get; set; }
        public int TimeExisted { get; set; }
        public float HpPerTick { get; }
        public Player _Player { get; }
        public float HpRegened { get; set; }
        public float HpRegenLimit { get; }
        public int Delay { get; set; }
        public EDamageType DamageType { get; }
        public EHealthEffectType EffectType { get; }
        private bool deductedRecordedDamage = false;

        public HealthRegenEffect(float hpTick, int? dur, EBodyPart part, Player player, int delay, float limit, EDamageType damageType)
        {
            TimeExisted = 0;
            HpRegened = 0;
            HpRegenLimit = limit;
            HpPerTick = hpTick;
            Duration = dur;
            BodyPart = part;
            _Player = player;
            Delay = delay;
            DamageType = damageType;
            EffectType = EHealthEffectType.HealthRegen;
        }

        public void Tick()
        {
            TimeExisted++;
            float currentHp = _Player.ActiveHealthController.GetBodyPartHealth(BodyPart).Current;
            float maxHp = _Player.ActiveHealthController.GetBodyPartHealth(BodyPart).Maximum;

            if (!deductedRecordedDamage)
            {
                if (DamageType == EDamageType.HeavyBleeding)
                {
                    Plugin.RealHealthController.DmgTracker.TotalHeavyBleedDamage = Mathf.Max(Plugin.RealHealthController.DmgTracker.TotalHeavyBleedDamage - HpRegenLimit, 0f);
                }
                if (DamageType == EDamageType.LightBleeding)
                {
                    Plugin.RealHealthController.DmgTracker.TotalLightBleedDamage = Mathf.Max(Plugin.RealHealthController.DmgTracker.TotalLightBleedDamage - HpRegenLimit, 0f);

                }
                deductedRecordedDamage = true;
            }

            if (HpRegened < HpRegenLimit)
            {
                if (Delay <= 0 && TimeExisted % 3 == 0)
                {
                    _Player.ActiveHealthController.AddEffect<HealthChange>(BodyPart, 0f, 3f, 1f, HpPerTick, null);
                    HpRegened += HpPerTick;
                }
            }

            if (HpRegened >= HpRegenLimit || (currentHp >= maxHp) || currentHp <= 0f)
            {
                Duration = 0;
            }
        }
    }

    public class ResourceRateEffect : IHealthEffect
    {
        public EBodyPart BodyPart { get; set; }
        public int? Duration { get; set; }
        public int TimeExisted { get; set; }
        public float ResourcePerTick { get; }
        public Player _Player { get; }
        public int Delay { get; set; }
        public EHealthEffectType EffectType { get; }
        private ManualLogSource Logger;

        public ResourceRateEffect(float resourcePerTick, int? dur, Player player, int delay, ManualLogSource logger)
        {
            TimeExisted = 0;
            Duration = dur;
            _Player = player;
            Delay = delay;
            EffectType = EHealthEffectType.ResourceRate;
            BodyPart = EBodyPart.Stomach;
            ResourcePerTick = resourcePerTick;
            Logger = logger;
        }

        public void Tick()
        {
            if (Delay <= 0)
            {
                TimeExisted++;
                if (TimeExisted % 3 == 0) 
                {
                    _Player.ActiveHealthController.AddEffect<ResourceRateChange>(BodyPart, 0f, 3f, 0f, ResourcePerTick, null);
                    Duration = 0;                
                }
            }
        }
    }

    public class PainKillerEffect : IHealthEffect
    {
        public EBodyPart BodyPart { get; set; }
        public int? Duration { get; set; }
        public int TimeExisted { get; set; }
        public Player _Player { get; }
        public int Delay { get; set; }
        public EHealthEffectType EffectType { get; }
        public float TunnelVisionStrength { get; }
        public int IntermittentWaitDur { get; }
        public int IntermittentEffectDur { get; }
        public float PainKillerStrength { get; set; }
        private bool addedEffect = false;
        private ManualLogSource logger;

        public PainKillerEffect(int? dur, Player player, int delay, int intermittentWaitDur, int intermittentEffectDur, float tunnelStrength, float painKillerStrength, ManualLogSource logger)
        {
            TimeExisted = 0;
            Duration = dur;
            BodyPart = EBodyPart.Head;
            _Player = player;
            Delay = delay;
            EffectType = EHealthEffectType.PainKiller;
            IntermittentWaitDur = intermittentWaitDur;
            IntermittentEffectDur = intermittentEffectDur;
            TunnelVisionStrength = tunnelStrength;
            PainKillerStrength = painKillerStrength;
            this.logger = logger;
        }

        public void Tick()
        {
            if (Delay <= 0)
            {
                Duration--;
                if (Duration <= 0)
                {
                    logger.LogWarning("removing PK effect");
                    Plugin.RealHealthController.PainReliefStrength -= PainKillerStrength;
                    Plugin.RealHealthController.PainTunnelStrength -= TunnelVisionStrength;
                    Plugin.RealHealthController.ReliefDuration -= IntermittentEffectDur;
                    Plugin.RealHealthController.ReliefWaitDuration += IntermittentWaitDur;
                }
                else if (!addedEffect)
                {
                    logger.LogWarning("adding PK effect");
                    addedEffect = true;
                    Plugin.RealHealthController.PainReliefStrength += PainKillerStrength;
                    Plugin.RealHealthController.PainTunnelStrength += TunnelVisionStrength;
                    Plugin.RealHealthController.ReliefDuration += IntermittentEffectDur;
                    Plugin.RealHealthController.ReliefWaitDuration -= IntermittentWaitDur;

                    if (!Plugin.RealHealthController.HasBaseEFTEffect(_Player, "PainKiller"))
                    {
                        Plugin.RealHealthController.AddBasesEFTEffect(_Player, "PainKiller", EBodyPart.Head, 0f, IntermittentEffectDur, 1f, 1f);
                        Plugin.RealHealthController.AddBasesEFTEffect(_Player, "TunnelVision", EBodyPart.Head, 0f, IntermittentEffectDur, 1f, TunnelVisionStrength);
                    }
                }
            }
            else 
            {
                logger.LogWarning("delay " + Delay);
            }
        }
    }

    public class HealthChange : EffectClass, IEffect, GInterface235, GInterface250
    {
        public override void Started()
        {
            this.hpPerTick = base.Strength;
            this.SetHealthRatesPerSecond(this.hpPerTick, 0f, 0f, 0f);
            this.bodyPart = base.BodyPart;
        }

        public override void RegularUpdate(float deltaTime)
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

    public class ResourceRateChange : EffectClass, IEffect, GInterface235, GInterface250
    {
        public override void Started()
        {
            this.resourcePerTick = base.Strength;
            this.bodyPart = base.BodyPart;
            this.SetHealthRatesPerSecond(0f, -this.resourcePerTick, -this.resourcePerTick, 0f);
        }

        public override void RegularUpdate(float deltaTime)
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
