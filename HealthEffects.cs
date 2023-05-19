using EFT;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using static Systems.Effects.Effects;
using UnityEngine;

namespace RealismMod
{
    public interface IHealthEffect
    {
        public EBodyPart BodyPart { get; set; }
        public int? Duration { get; }
        public float TimeExisted { get; set; }
        public void Tick();
        public Player Player { get; }
        public float Delay { get; set; }
    }

    public class TourniquetEffect : IHealthEffect
    {
        public EBodyPart BodyPart { get; set; }
        public int? Duration { get; }
        public float TimeExisted { get; set; }
        public float HpPerTick { get; }
        public Player Player { get; }
        public float Delay { get; set; }

        public TourniquetEffect(float hpTick, int? dur, EBodyPart part, Player player, float delay)
        {
            TimeExisted = 0;
            HpPerTick = -hpTick;
            Duration = dur;
            BodyPart = part;
            Player = player;
            Delay = delay;
        }

        public void Tick()
        {
            if (Delay <= 0f) 
            {
                float currentPartHP = Player.ActiveHealthController.GetBodyPartHealth(BodyPart).Current;

                if (currentPartHP > 25f)
                {
                    /*           Player.ActiveHealthController.ChangeHealth(BodyPart, -hpPerTick, default);*/
                    MethodInfo addEffectMethod = RealismHealthController.GetAddBaseEFTEffectMethod();
                    addEffectMethod.MakeGenericMethod(typeof(ActiveHealthControllerClass).GetNestedType("ScavRegeneration", BindingFlags.NonPublic | BindingFlags.Instance)).Invoke(Player.ActiveHealthController, new object[] { BodyPart, 0f, 3f, 1f, HpPerTick, null });
                }
            }
        }
    }

    public class SurgeryEffect : IHealthEffect
    {
        public EBodyPart BodyPart { get; set; }
        public int? Duration { get; set; }
        public float TimeExisted { get; set; }
        public float HpPerTick { get; }
        public Player Player { get; }
        public float HpRegened { get; set; }
        public float Delay { get; set; }
        private bool _hasRemovedTrnqt = false;

        public SurgeryEffect(float hpTick, int? dur, EBodyPart part, Player player, float delay)
        {
            TimeExisted = 0;
            HpRegened = 0;
            HpPerTick = hpTick;
            Duration = dur;
            BodyPart = part;
            Player = player;
            Delay = delay;
        }

        //should only restore HP equivelent to half of the limbs max hp.
        public void Tick()
        {
            if (Delay <= 0f)
            {
                if (!_hasRemovedTrnqt)
                {
                    NotificationManagerClass.DisplayMessageNotification("Surgical Kit Used, Removing Any Tourniquet Effect Present On Limb: " + BodyPart, EFT.Communications.ENotificationDurationType.Long);
                    RealismHealthController.RemoveEffectOfType(typeof(TourniquetEffect), BodyPart);
                    _hasRemovedTrnqt = true;
                }

                float currentHp = Player.ActiveHealthController.GetBodyPartHealth(BodyPart).Current;
                float maxHp = Player.ActiveHealthController.GetBodyPartHealth(BodyPart).Maximum;
                float maxHpRegen = maxHp / 2f;

                HpRegened += HpPerTick / 20f; //BSG formula for ScavRegen rate.

                if (HpRegened >= maxHpRegen || currentHp == maxHp)
                {
                    NotificationManagerClass.DisplayMessageNotification("Surgical Kit Health Regeneration On " + BodyPart + "Has Expired", EFT.Communications.ENotificationDurationType.Long);
                    Duration = 0;
                    return;
                }
                if (HpRegened < maxHpRegen)
                {
                    /* Player.ActiveHealthController.ChangeHealth(BodyPart, HpPerTick, default);*/
                    MethodInfo addEffectMethod = RealismHealthController.GetAddBaseEFTEffectMethod();
                    addEffectMethod.MakeGenericMethod(typeof(ActiveHealthControllerClass).GetNestedType("ScavRegeneration", BindingFlags.NonPublic | BindingFlags.Instance)).Invoke(Player.ActiveHealthController, new object[] { BodyPart, 0f, 3f, 1f, HpPerTick, null });
                }
            }
        }
    }
}
