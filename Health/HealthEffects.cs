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
        HealthDrain,
        Adrenaline,
        ResourceRate,
        PainKiller,
        Stim
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
                    _Player.ActiveHealthController.AddEffect<HealthRegen>(BodyPart, 0f, 3f, 1f, HpPerTick, null);
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
                    _Player.ActiveHealthController.AddEffect<HealthRegen>(BodyPart, 0f, 3f, 1f, HpPerTick, null);
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

    public class HealthDrainEffect : IHealthEffect
    {
        public EBodyPart BodyPart { get; set; }
        public int? Duration { get; set; }
        public int TimeExisted { get; set; }
        public float HpPerTick { get; }
        public Player _Player { get; }
        public float HpDrained { get; set; }
        public float HpDrainLimit { get; }
        public int Delay { get; set; }
        public EHealthEffectType EffectType { get; }

        public HealthDrainEffect(float hpTick, Player player, int delay, float limit)
        {
            TimeExisted = 0;
            HpDrained = 0;
            HpDrainLimit = limit;
            HpPerTick = hpTick;
            _Player = player;
            Delay = delay;
            EffectType = EHealthEffectType.HealthDrain;
            BodyPart = EBodyPart.Common;
        }

        public void Tick()
        {
            TimeExisted++;
            if (HpDrained < HpDrainLimit)
            {
                if (Delay <= 0 && TimeExisted % 3 == 0)
                {
                    _Player.ActiveHealthController.AddEffect<HealthDrain>( 0f, 3f, 1f, HpPerTick, null);
                    HpDrained += HpPerTick;
                }
            }

            if (HpDrained >= HpDrainLimit)
            {
                Duration = 0;
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
                    _Player.ActiveHealthController.AddEffect<HealthRegen>(BodyPart, 0f, 3f, 1f, HpPerTick, null);
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
        public Player _Player { get; }
        public int Delay { get; set; }
        public EHealthEffectType EffectType { get; }
        private bool addedEffect = false;

        public ResourceRateEffect(int? dur, Player player, int delay)
        {
            TimeExisted = 0;
            Duration = dur;
            _Player = player;
            Delay = delay;
            EffectType = EHealthEffectType.ResourceRate;
            BodyPart = EBodyPart.Stomach;
        }

        public void Tick()
        {
            if (!addedEffect) 
            {
                _Player.ActiveHealthController.AddEffect<ResourceRateChange>(BodyPart, 0f, null, 0f, 0f, null);
                addedEffect = false;
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
        public float PainKillerStrength { get; set; }
        private bool addedEffect = false;

        public PainKillerEffect(int? dur, Player player, int delay, float tunnelStrength, float painKillerStrength)
        {
            TimeExisted = 0;
            Duration = dur;
            BodyPart = EBodyPart.Head;
            _Player = player;
            Delay = delay;
            EffectType = EHealthEffectType.PainKiller;
            TunnelVisionStrength = tunnelStrength;
            PainKillerStrength = painKillerStrength;
        }

        public void Tick()
        {
            if (Delay <= 0)
            {
                Duration--;
                if (Duration <= 0)
                {
                    Plugin.RealHealthController.PainReliefStrength -= PainKillerStrength;
                    Plugin.RealHealthController.PainTunnelStrength -= TunnelVisionStrength;
                }
                else if (!addedEffect)
                {
                    addedEffect = true;
                    Plugin.RealHealthController.PainReliefStrength += PainKillerStrength;
                    Plugin.RealHealthController.PainTunnelStrength += TunnelVisionStrength;
                    Plugin.RealHealthController.ReliefDuration += (int)Duration;
                }
            }
        }
    }

    public class StimShellEffect : IHealthEffect
    {
        public EBodyPart BodyPart { get; set; }
        public int? Duration { get; set; }
        public int TimeExisted { get; set; }
        public Player _Player { get; }
        public int Delay { get; set; }
        public EHealthEffectType EffectType { get; }
        public EStimType StimType { get; }

        public StimShellEffect(Player player, int? dur, int delay, EStimType stimType)
        {
            TimeExisted = 0;
            Duration = dur;
            _Player = player;
            Delay = delay;
            EffectType = EHealthEffectType.Stim;
            BodyPart = EBodyPart.Head;
            StimType = stimType;
        }

        public void Tick()
        {
            if (Delay <= 0)
            {
                Duration--;
                if (Duration <= 0) Duration = 0;
            }
        }
    }

    public class HealthDrain : EffectClass, IEffect, GInterface235, GInterface250
    {
        public override void Started()
        {
            this.hpPerTick = base.Strength;
            this.SetHealthRatesPerSecond(this.hpPerTick / bodyParts.Count(), 0f, 0f, 0f);
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
            foreach (EBodyPart part in bodyParts)
            {
                if (this.HealthController.GetBodyPartHealth(part).Current > 0f) 
                {
                    this.HealthController.ApplyDamage(part, this.hpPerTick / bodyParts.Count(), ExistanceClass.PoisonDamage);
                }
            }
        }

        private float hpPerTick;

        private float time;

        private EBodyPart bodyPart;
        private EBodyPart[] bodyParts = { EBodyPart.Chest, EBodyPart.Stomach };
    }


    public class HealthRegen : EffectClass, IEffect, GInterface235, GInterface250
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
            this.resourcePerTick = Plugin.RealHealthController.ResourcePerTick;
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
            Utils.Logger.LogWarning("tick");
            Utils.Logger.LogWarning("resourcePerTick " + resourcePerTick);
            this.resourcePerTick = Plugin.RealHealthController.ResourcePerTick;
            base.HealthController.ChangeEnergy(-this.resourcePerTick);
            base.HealthController.ChangeHydration(-this.resourcePerTick);
            this.SetHealthRatesPerSecond(0f, -this.resourcePerTick, -this.resourcePerTick, 0f);
        }

        private float resourcePerTick;

        private float time;

        private EBodyPart bodyPart;
    }

}
