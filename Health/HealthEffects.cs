using EFT;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using static Systems.Effects.Effects;
using UnityEngine;
using System.Linq;
using BepInEx.Logging;

namespace RealismMod
{

    public enum EHealthEffectType 
    {
        Surgery,
        Tourniquet,
        HealthRegen,
        Adrenaline
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
        private bool hasRemovedTrnqt = false;
        public EHealthEffectType EffectType { get; }

        public SurgeryEffect(float hpTick, float? dur, EBodyPart part, Player player, float delay)
        {
            TimeExisted = 0;
            HpRegened = 0;
            HpPerTick = hpTick;
            Duration = dur;
            BodyPart = part;
            Player = player;
            Delay = delay;
            EffectType = EHealthEffectType.Surgery;
        }

        public void Tick()
        {
            if (Delay <= 0f)
            {
                if (!hasRemovedTrnqt)
                {
                    RealismHealthController.RemoveEffectOfType(typeof(TourniquetEffect), BodyPart);
                    hasRemovedTrnqt = true;
                }

                float currentHp = Player.ActiveHealthController.GetBodyPartHealth(BodyPart).Current;
                float maxHp = Player.ActiveHealthController.GetBodyPartHealth(BodyPart).Maximum;
                float maxHpRegen = maxHp / 2f;

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

            if(HpRegened >= HpRegenLimit || (currentHp >= maxHp) || currentHp == 0)
            {
                Duration = 0;
            }
        }
    }


    public class HealthChange : ActiveHealthControllerClass.GClass2102, IEffect, GInterface184, GInterface199
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
            base.HealthController.ChangeHealth(bodyPart, this.hpPerTick, GClass2146.Existence);
        }

        private float hpPerTick;

        private float time;

        private EBodyPart bodyPart;

    }
}
