using Aki.Reflection.Patching;
using EFT.InventoryLogic;
using EFT;
using System;
using System.Reflection;
using UnityEngine;
using Comfort.Common;
using System.Linq;

namespace RealismMod
{
    public class DamageInfoPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(DamageInfo).GetConstructor(new Type[] { typeof(EDamageType), typeof(GClass2609) });
        }

        [PatchPrefix]
        private static bool Prefix(ref DamageInfo __instance, EDamageType damageType, GClass2609 shot)
        {
            Logger.LogWarning("============DamageInfo=============");
            Logger.LogInfo("Shot ID = " + shot.Ammo.Id);
            Logger.LogInfo("Shot Start Speed = " + shot.Speed);
            Logger.LogInfo("Shot Current Speed = " + shot.VelocityMagnitude);
            Logger.LogInfo("Shot Mass = " + shot.BulletMassGram);
            Logger.LogInfo("Kinetic Energy = " + (0.5f * shot.BulletMassGram * shot.VelocityMagnitude * shot.VelocityMagnitude) / 1000);
            Logger.LogInfo("Shot Damage = " + shot.Damage);
            Logger.LogInfo("Shot Penetration = " + shot.PenetrationPower);

            __instance.DamageType = damageType;
            __instance.Damage = shot.Damage;
            __instance.PenetrationPower = shot.PenetrationPower;
            __instance.HitCollider = shot.HitCollider;
            __instance.Direction = shot.Direction;
            __instance.HitPoint = shot.HitPoint;
            __instance.HitNormal = shot.HitNormal;
            __instance.HittedBallisticCollider = shot.HittedBallisticCollider;
            __instance.Player = shot.Player;
            __instance.Weapon = shot.Weapon;
            __instance.FireIndex = shot.FireIndex;
            __instance.ArmorDamage = shot.VelocityMagnitude;
            __instance.DeflectedBy = shot.DeflectedBy;
            __instance.BlockedBy = shot.BlockedBy;
            __instance.MasterOrigin = shot.MasterOrigin;
            __instance.IsForwardHit = shot.IsForwardHit;
            __instance.SourceId = shot.Ammo.TemplateId;
            BulletClass bulletClass;
            if ((bulletClass = (shot.Ammo as BulletClass)) != null)
            {
                __instance.StaminaBurnRate = bulletClass.StaminaBurnRate;
                __instance.HeavyBleedingDelta = bulletClass.HeavyBleedingDelta;
                __instance.LightBleedingDelta = bulletClass.LightBleedingDelta;
            }
            else
            {
                float lightBleedingDelta = 0f;
                float num = 0f;
                __instance.LightBleedingDelta = lightBleedingDelta;
                float heavyBleedingDelta = num;
                num = 0f;
                __instance.HeavyBleedingDelta = heavyBleedingDelta;
                __instance.StaminaBurnRate = num;
                KnifeClass knifeClass;
                if ((knifeClass = (__instance.Weapon as KnifeClass)) != null)
                {
                    __instance.StaminaBurnRate = knifeClass.KnifeComponent.Template.StaminaBurnRate;
                }
            }
            __instance.DidBodyDamage = 0f;
            __instance.DidArmorDamage = 0f;
            __instance.OverDamageFrom = null;
            __instance.BodyPartColliderType = EBodyPartColliderType.None;
            __instance.BleedBlock = false;
            Logger.LogWarning("====================================");
            return false;
        }
    }
    public class ApplyDamagePatch : ModulePatch
    {
        public readonly RepairableComponent Repairable;

        protected override MethodBase GetTargetMethod()
        {
            var result = typeof(ArmorComponent).GetMethod(nameof(ArmorComponent.ApplyDamage), BindingFlags.Public | BindingFlags.Instance);

            return result;

        }
        [PatchPrefix]
        private static bool Prefix(ref DamageInfo damageInfo, bool damageInfoIsLocal, EBodyPart bodyPartType, SkillsClass.GClass1676 lightVestsDamageReduction, SkillsClass.GClass1676 heavyVestsDamageReduction, ref ArmorComponent __instance, ref float __result)
        {


            //can create new instance using damageInfo.sourceId, then get whatever stats I want...
            AmmoTemplate ammoTemp = (AmmoTemplate)Singleton<ItemFactory>.Instance.ItemTemplates[damageInfo.SourceId];
            BulletClass ammo = new BulletClass("newAmmo", ammoTemp);


            Logger.LogWarning("Name = " + ammo.Name);
            Logger.LogWarning("Localized Name = " + ammo.LocalizedName());
            Logger.LogWarning("Penetration Power = " + ammo.PenetrationPower);
            Logger.LogWarning("Initial Speed = " + ammo.GetBulletSpeed);
            Logger.LogWarning("Current Speed = " + damageInfo.ArmorDamage);
            Logger.LogWarning("Speed Delta = " + ammo.GetBulletSpeed / damageInfo.ArmorDamage);
            Logger.LogWarning("Armor Damage = " + ammo.ArmorDamage);
            Logger.LogInfo("Kinetic Energy = " + (0.5f * ammo.BulletMassGram * damageInfo.ArmorDamage * damageInfo.ArmorDamage) / 1000);

            Logger.LogWarning("//////////New Armor Damage/////////////");
            EDamageType damageType = damageInfo.DamageType;

            float speedFactor = ammo.GetBulletSpeed / damageInfo.ArmorDamage;
            float armorDamage = ammo.ArmorDamage * speedFactor;
            float fragChance = ammo.FragmentationChance * speedFactor;
            float ricochetChance = ammo.RicochetChance * speedFactor;

            if (!damageType.IsWeaponInduced() && damageType != EDamageType.GrenadeFragment)
            {
                Logger.LogWarning("//////////Not Weapon Induced/////////////");
                __result = 0f;
                return false;
            }
            __instance.TryShatter(damageInfo.Player, damageInfoIsLocal);
            if (__instance.Repairable.Durability <= 0f)
            {
                Logger.LogWarning("//////////Durability is 0/////////////");
                __result = 0f;
                return false;
            }
            if (damageInfo.DeflectedBy == __instance.Item.Id)
            {
                Logger.LogWarning("//////////Round Deflected/////////////");
                damageInfo.Damage /= 3f;
                armorDamage /= 3f;
                damageInfo.PenetrationPower /= 3f;
            }

            float KE = (0.5f * ammo.BulletMassGram * damageInfo.ArmorDamage * damageInfo.ArmorDamage) / 1000;
            float damageToKEFactor = 24f;
            float bluntThrput = __instance.Template.BluntThroughput;
            float penPower = damageInfo.PenetrationPower;
            float duraPercent = __instance.Repairable.Durability / (float)__instance.Repairable.TemplateDurability;
            float armorResist = (float)Singleton<BackendConfigSettingsClass>.Instance.Armor.GetArmorClass(__instance.ArmorClass).Resistance;

            float armorFactor = armorResist * (Mathf.Min(1f, duraPercent * 2f));
            float throughputDuraFactored = Mathf.Min(1f, bluntThrput * (1f + ((duraPercent - 1f) * -1f)));
            float penFactoredClass = Mathf.Max(1f, armorFactor - (penPower / 2.5f));
            float maxPotentialDamage = (KE / Mathf.Max(1, (penFactoredClass / 40f)) / damageToKEFactor);
            float throughputFacotredDamage = maxPotentialDamage * throughputDuraFactored;
            float durabilityLoss = (maxPotentialDamage / 100f) * armorDamage * Singleton<BackendConfigSettingsClass>.Instance.ArmorMaterials[__instance.Template.ArmorMaterial].Destructibility;

            if (!(damageInfo.BlockedBy == __instance.Item.Id) && !(damageInfo.DeflectedBy == __instance.Item.Id))
            {
                Logger.LogWarning("=========Round Penetrated=======");

                durabilityLoss /= 2f;
                damageInfo.Damage = maxPotentialDamage;
                damageInfo.PenetrationPower *= penPower *; // factor it by the armor class, and factor armor class by remaining durability
                Logger.LogWarning("================");
            }
            else
            {
                Logger.LogWarning("=======Round Blocked=========");
                if (__instance.Template.ArmorMaterial == EArmorMaterial.ArmoredSteel)
                {
                    damageInfo.Damage = throughputFacotredDamage * Mathf.Max(1, 1 + ((fragChance - 1) * -1)) * Mathf.Max(1, (1 + ((ricochetChance - 1) * -1)));
                }
                else
                {
                    damageInfo.Damage = throughputFacotredDamage;
                }

                damageInfo.StaminaBurnRate *= ((bluntThrput > 0f) ? (3f / Mathf.Sqrt(bluntThrput)) : 1f);
                Logger.LogWarning("================");
            }
            durabilityLoss = Mathf.Max(0.01f, durabilityLoss);
            __instance.ApplyDurabilityDamage(durabilityLoss);
            __result = durabilityLoss;
            Logger.LogWarning("durabilityLoss " + durabilityLoss);
            Logger.LogWarning("///////////////////////");
            return false;


           /* Logger.LogWarning("//////////Original Armor Damage/////////////");
            EDamageType damageType = damageInfo.DamageType;
            if (!damageType.IsWeaponInduced() && damageType != EDamageType.GrenadeFragment)
            {
                Logger.LogWarning("//////////Not Weapon Induced/////////////");
                __result = 0f;
                return false;
            }
            __instance.TryShatter(damageInfo.Player, damageInfoIsLocal);
            if (__instance.Repairable.Durability <= 0f)
            {
                Logger.LogWarning("//////////Durability is 0/////////////");
                __result = 0f;
                return false;
            }
            if (damageInfo.DeflectedBy == __instance.Item.Id)
            {
                Logger.LogWarning("//////////Round Deflected/////////////");
                damageInfo.Damage /= 2f;
                damageInfo.ArmorDamage /= 2f;
                damageInfo.PenetrationPower /= 2f;
            }
            float bluntThrput = __instance.Template.BluntThroughput;
            float penetrationPower = damageInfo.PenetrationPower;
            float duraPercent = __instance.Repairable.Durability / (float)__instance.Repairable.TemplateDurability * 100f;
            float armorResist = (float)Singleton<BackendConfigSettingsClass>.Instance.Armor.GetArmorClass(__instance.ArmorClass).Resistance;
            float armorDuraFactor = (121f - 5000f / (45f + duraPercent * 2f)) * armorResist * 0.01f;
            float durabilityLoss;

            Logger.LogWarning("bluntThrput " + bluntThrput);
            Logger.LogWarning("penetrationPower " + penetrationPower);
            Logger.LogWarning("duraPercent " + duraPercent);
            Logger.LogWarning("armorResist " + armorResist);
            Logger.LogWarning("armorDuraFactor " + armorDuraFactor);

            if (!(damageInfo.BlockedBy == __instance.Item.Id) && !(damageInfo.DeflectedBy == __instance.Item.Id))
            {
                Logger.LogWarning("=========Round Penetrated=======");
                durabilityLoss = damageInfo.PenetrationPower * ammo.ArmorDamagePortion * Mathf.Clamp(penetrationPower / armorResist, 0.5f, 0.9f) * Singleton<BackendConfigSettingsClass>.Instance.ArmorMaterials[__instance.Template.ArmorMaterial].Destructibility;
                float armorPenFactor = Mathf.Clamp(penetrationPower / (armorDuraFactor + 12f), 0.6f, 1f);
                damageInfo.Damage *= armorPenFactor;
                damageInfo.PenetrationPower *= armorPenFactor;
                Logger.LogWarning("armorPenFactor " + armorPenFactor);
                Logger.LogWarning("damageInfo.PenetrationPower " + damageInfo.PenetrationPower);
                Logger.LogWarning("damageInfo.Damage " + damageInfo.Damage);
                Logger.LogWarning("================");
            }
            else
            {
                Logger.LogWarning("=======Round Blocked=========");
                durabilityLoss = damageInfo.PenetrationPower * ammo.ArmorDamagePortion * Mathf.Clamp(penetrationPower / armorResist, 0.6f, 1.1f) * Singleton<BackendConfigSettingsClass>.Instance.ArmorMaterials[__instance.Template.ArmorMaterial].Destructibility;
                damageInfo.Damage *= bluntThrput * Mathf.Clamp(1f - 0.03f * (armorDuraFactor - penetrationPower), 0.2f, 1f);
                damageInfo.StaminaBurnRate *= ((bluntThrput > 0f) ? (3f / Mathf.Sqrt(bluntThrput)) : 1f);
                Logger.LogWarning("damageInfo.Damage " + damageInfo.Damage);
                Logger.LogWarning("damageInfo.StaminaBurnRate " + damageInfo.StaminaBurnRate);
                Logger.LogWarning("================");
            }
            durabilityLoss = Mathf.Max(1f, durabilityLoss);
            __instance.ApplyDurabilityDamage(durabilityLoss);
            __result = durabilityLoss;
            Logger.LogWarning("durabilityLoss " + durabilityLoss);
            Logger.LogWarning("///////////////////////");
            return false;*/
        }

    }

    public class CreateShotPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            var result = typeof(EFT.Ballistics.BallisticsCalculator).GetMethod("CreateShot", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            return result;
        }

        [PatchPrefix]
        private static bool Prefix(EFT.Ballistics.BallisticsCalculator __instance, BulletClass ammo, Vector3 origin, Vector3 direction, int fireIndex, Player player, Item weapon, ref GClass2609 __result, float speedFactor, int fragmentIndex = 0)
        {
            /*            Logger.LogWarning("!!!!!!!!!!! Shot Created!! !!!!!!!!!!!!!!");
            Logger.LogWarning("========================STARTING BULLET VALUES============================");
            Logger.LogWarning("Round ID = " + ammo.TemplateId);
            Logger.LogWarning("Round Damage = " + ammo.Damage);
            Logger.LogWarning("Round Penetration Power = " + ammo.PenetrationPower);
            Logger.LogWarning("Round Penetration Chance = " + ammo.PenetrationChance);
            Logger.LogWarning("Round Frag Chance = " + ammo.FragmentationChance);
            Logger.LogWarning("Round Intial Speed = " + ammo.InitialSpeed);
            Logger.LogWarning("Round SPEED FACTOR = " + speedFactor);
            Logger.LogWarning("Round BC = " + ammo.BallisticCoeficient);
            Logger.LogWarning("==============================================================");*/

            int randomNum = UnityEngine.Random.Range(0, 512);
            float velocityFactored = ammo.InitialSpeed * speedFactor;
            float penChanceFactored = ammo.PenetrationChance * speedFactor;
            float damageFactored = ammo.Damage * speedFactor;
            float fragchanceFactored = Mathf.Max(ammo.FragmentationChance * speedFactor, 0);
            float penPowerFactored = EFT.Ballistics.BallisticsCalculator.GetAmmoPenetrationPower(ammo, randomNum, __instance.Randoms) * speedFactor;
            float bcFactored = Mathf.Max(ammo.BallisticCoeficient * speedFactor, 0.01f);

            /* Logger.LogWarning("========================AFTER SPEED FACTOR============================");
             Logger.LogWarning("Round ID = " + ammo.TemplateId);
             Logger.LogWarning("Round Damage = " + damageFactored);
             Logger.LogWarning("Round Penetration Power UNFACTORED = " + penPowerUnfactored);
             Logger.LogWarning("Round Penetration Power = " + penPowerFactored);
             Logger.LogWarning("Round Penetration Chance = " + penChanceFactored);
             Logger.LogWarning("Round Frag Chance = " + fragchanceFactored);
             Logger.LogWarning("Round Factored Speed = " + velocityFactored);
             Logger.LogWarning("Round Factored BC = " + bcFactored);
             Logger.LogWarning("==============================================================");*/

            __result = GClass2609.Create(ammo, fragmentIndex, randomNum, origin, direction, velocityFactored, velocityFactored, ammo.BulletMassGram, ammo.BulletDiameterMilimeters, (float)damageFactored, penPowerFactored, penChanceFactored, ammo.RicochetChance, fragchanceFactored, 1f, ammo.MinFragmentsCount, ammo.MaxFragmentsCount, EFT.Ballistics.BallisticsCalculator.DefaultHitBody, __instance.Randoms, bcFactored, player, weapon, fireIndex, null);
            return false;

        }
    }
}
