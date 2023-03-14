using Aki.Reflection.Patching;
using EFT.InventoryLogic;
using EFT;
using System;
using System.Reflection;
using UnityEngine;
using Comfort.Common;
using System.Linq;
using HarmonyLib;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using EFT.Ballistics;

namespace RealismMod
{
    public class ApplyDamageInfoPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player).GetMethod("ApplyDamageInfo", BindingFlags.Instance | BindingFlags.Public);
        }


        private static float _armorClass;
        private static float _currentDura;
        private static float _maxDura;

        private static List<EBodyPart> _bodyParts = new List<EBodyPart> { EBodyPart.RightArm, EBodyPart.LeftArm, EBodyPart.LeftLeg, EBodyPart.RightLeg, EBodyPart.Head, EBodyPart.Common };
        private static System.Random _randNum = new System.Random();

        private static List<ArmorComponent> preAllocatedArmorComponents = new List<ArmorComponent>(10);

        private static void SetArmorStats(ArmorComponent armor)
        {
            _armorClass = armor.ArmorClass * 10f;
            _currentDura = armor.Repairable.Durability;
            _maxDura = armor.Repairable.TemplateDurability;
        }

        private static float GetBleedFactor(EBodyPart part)
        {
            switch (part)
            {
                case EBodyPart.Head:
                    return 0.4f;
                case EBodyPart.LeftLeg:
                case EBodyPart.RightLeg:
                    return 0.5f;
                case EBodyPart.LeftArm:
                case EBodyPart.RightArm:
                    return 0.25f;
                default:
                    return 1;
            }
        }

        [PatchPostfix]
        private static void PatchPostfix(Player __instance, DamageInfo damageInfo, EBodyPart bodyPartType, float absorbed, EHeadSegment? headSegment = null)
        {

            if (damageInfo.DamageType == EDamageType.Bullet)
            {

                EquipmentClass equipment = (EquipmentClass)AccessTools.Property(typeof(Player), "Equipment").GetValue(__instance);
                InventoryClass inventory = (InventoryClass)AccessTools.Property(typeof(Player), "Inventory").GetValue(__instance);
                preAllocatedArmorComponents.Clear();
                inventory.GetPutOnArmorsNonAlloc(preAllocatedArmorComponents);
                ArmorComponent armor = null;

                foreach (ArmorComponent armorComponent in preAllocatedArmorComponents)
                {
                    if (armorComponent.Item.Id == damageInfo.BlockedBy || armorComponent.Item.Id == damageInfo.DeflectedBy)
                    {
                        armor = armorComponent;
                    }
                }

                if (damageInfo.Blunt == true && armor != null && ArmorProperties.CanSpall(armor.Item) == true)
                {
                    SetArmorStats(armor);
                    AmmoTemplate ammoTemp = (AmmoTemplate)Singleton<ItemFactory>.Instance.ItemTemplates[damageInfo.SourceId];
                    BulletClass ammo = new BulletClass("newAmmo", ammoTemp);
                    damageInfo.BleedBlock = false;
                    bool reduceDurability = armor.Template.ArmorMaterial != EArmorMaterial.ArmoredSteel || armor.Template.ArmorMaterial != EArmorMaterial.Titan ? true : false;
                    float KE = ((0.5f * ammo.BulletMassGram * damageInfo.ArmorDamage * damageInfo.ArmorDamage) / 1000);
                    float bluntDamage = damageInfo.Damage;
                    float speedFactor = damageInfo.ArmorDamage / ammo.GetBulletSpeed;
                    float fragChance = ammo.FragmentationChance * speedFactor;
                    float lightBleedChance = damageInfo.LightBleedingDelta;
                    float heavyBleedChance = damageInfo.HeavyBleedingDelta;
                    float ricochetChance = ammo.RicochetChance * speedFactor;
                    float spallReduction = ArmorProperties.SpallReduction(armor.Item);
                    float damageToKEFactor = 24f;

                    float duraPercent = _currentDura / _maxDura;
                    float armorFactor = !reduceDurability? _armorClass * (Mathf.Min(1, duraPercent * 1f)) : _armorClass * (Mathf.Min(1, duraPercent * 2f)); //durability should be more important for steel plates, representing anti-spall coating. Lower the amount durapercent is factored by to increase importance
                    float penFactoredClass = Mathf.Max(1f, armorFactor - (damageInfo.PenetrationPower / 2.5f));
                    float maxPotentialDamage = (KE / Mathf.Max(1, (penFactoredClass / 40f)) / damageToKEFactor);

                    float maxSpallingDamage = maxPotentialDamage - bluntDamage;
                    float factoredSpallingDamage = maxSpallingDamage * (fragChance + 1) * (ricochetChance + 1) * spallReduction;

                    int rnd = Math.Max(1, _randNum.Next(_bodyParts.Count));
                    float splitSpallingDmg = factoredSpallingDamage / _bodyParts.Count;

                    if (Plugin.EnableLogging.Value == true)
                    {
                        Logger.LogWarning("===========SPALLING=============== ");
                        Logger.LogWarning("Spall Reduction " + spallReduction);
                        Logger.LogWarning("Dura Percent " + duraPercent);
                        Logger.LogWarning("Max Potential Damage " + maxPotentialDamage);
                        Logger.LogWarning("Max Spalling Damage " + maxSpallingDamage);
                        Logger.LogWarning("Factored Spalling Damage " + factoredSpallingDamage);
                        Logger.LogWarning("Split Spalling Dmg " + splitSpallingDmg);
                    }

                    foreach (EBodyPart part in _bodyParts.OrderBy(x => _randNum.Next()).Take(rnd))
                    {

                        if (part == EBodyPart.Common)
                        {
                            return;
                        }

                        float damage = splitSpallingDmg;
                        float bleedFactor = GetBleedFactor(part);
                        damageInfo.HeavyBleedingDelta = heavyBleedChance * bleedFactor;
                        damageInfo.LightBleedingDelta = lightBleedChance * bleedFactor;

                        if (part == EBodyPart.Head)
                        {
                            damage = Mathf.Min(10, splitSpallingDmg);
                        }
                        if ((part == EBodyPart.LeftArm || part == EBodyPart.RightArm) && (armor.Template.ArmorZone.Contains(EBodyPart.LeftArm) || armor.Template.ArmorZone.Contains(EBodyPart.RightArm)))
                        {
                            damage *= 0.5f;
                        }

                        if (Plugin.EnableLogging.Value == true)
                        {

                            Logger.LogWarning("Part Hit " + part);
                            Logger.LogWarning("Damage " + damage);
                            Logger.LogWarning("========================== ");
                        }

                        __instance.ActiveHealthController.ApplyDamage(part, damage, damageInfo);
                    }
                }
            }

            //need to detect which armor slot isn't empty and only use that armor going forward, also need to check BlockedBy status
            //check if armor hit (is blint damage or not)
            //check if worn body armor is steel
            //get blunt damage, make sure it hasn't been facted by frag or ricochet yet
            //factor it by frag and ricochet
            //randomized amount of limbs hit (head, arms and legs can get hit by spalling)
            //divide up damage between selected limbs
            //set damageinfo.bleedblock to false
            //set damageinfo light and heavy bleed delta (heavy bleed higher for legs and head, lower for arms), and/or divide up bleed chance of parent round 
            //call ApplyDamage for each body part with respective damage

            //can apply frag damage to chest and stomach:
            //check if damage is blunt damage
            //check if armor is worn, reduce frag chance by some factor related to armor penetration, or reduce frag chance via ApplyDamage (better option)
            //calc bonus damage by multiply damage by frag chance (so lower the frag chance, the more bonys damage is reduced
            //apply bonus damage with bleed chance by calling ApplyDamage
            //maybe have chance to injur another body part (maybe just between chest and stomach), maybe arms too?).
        }
    }

    public class DamageInfoPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(DamageInfo).GetConstructor(new Type[] { typeof(EDamageType), typeof(GClass2620) });
        }

        [PatchPrefix]
        private static bool Prefix(ref DamageInfo __instance, EDamageType damageType, GClass2620 shot)
        {
/*            Logger.LogWarning("============DamageInfo=============");
            Logger.LogInfo("Shot ID = " + shot.Ammo.Id);
            Logger.LogInfo("Shot Start Speed = " + shot.Speed);
            Logger.LogInfo("Shot Current Speed = " + shot.VelocityMagnitude);
            Logger.LogInfo("Shot Mass = " + shot.BulletMassGram);
            Logger.LogInfo("Kinetic Energy = " + (0.5f * shot.BulletMassGram * shot.VelocityMagnitude * shot.VelocityMagnitude) / 1000);
            Logger.LogInfo("Shot Damage = " + shot.Damage);
            Logger.LogInfo("Shot Penetration = " + shot.PenetrationPower);*/

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
            return false;
        }
    }

    public class SetPenetrationStatusPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            var result = typeof(ArmorComponent).GetMethod(nameof(ArmorComponent.SetPenetrationStatus), BindingFlags.Public | BindingFlags.Instance);

            return result;

        }
        [PatchPrefix]
        private static bool Prefix(GClass2620 shot, ref ArmorComponent __instance)
        {
            string hitPart = shot.HittedBallisticCollider.name;
            bool isArmArmor = false;
            if ((__instance.Template.ArmorZone.Contains(EBodyPart.LeftArm) || __instance.Template.ArmorZone.Contains(EBodyPart.RightArm)))
            {
                if ((hitPart == "Base HumanRUpperarm" || hitPart == "Base HumanLUpperarm"))
                {
                    isArmArmor = true;
                }
                if ((hitPart == "Base HumanRForearm1" || hitPart == "Human LForearm1"))
                {
                    return false;
                }
            }

            if (__instance.Repairable.Durability <= 0f)
            {
                return false;
            }
            float penetrationPower = shot.PenetrationPower;
            float armorDura = __instance.Repairable.Durability / (float)__instance.Repairable.TemplateDurability * 100f;
            if (__instance.Template.ArmorMaterial == EArmorMaterial.ArmoredSteel)
            {
                armorDura = 100f;
            }
            if (__instance.Template.ArmorMaterial == EArmorMaterial.Titan)
            {
                armorDura = Mathf.Min(100f, armorDura * 2f);
            }
            float armorResist = (float)Singleton<BackendConfigSettingsClass>.Instance.Armor.GetArmorClass(__instance.ArmorClass).Resistance;
            armorResist = isArmArmor == true ? armorResist = 40f : armorResist;
            float armorFactor = (121f - 5000f / (45f + armorDura * 2f)) * armorResist * 0.01f;
            if (((armorFactor >= penetrationPower + 15f) ? 0f : ((armorFactor >= penetrationPower) ? (0.4f * (armorFactor - penetrationPower - 15f) * (armorFactor - penetrationPower - 15f)) : (100f + penetrationPower / (0.9f * armorFactor - penetrationPower)))) - shot.Randoms.GetRandomFloat(shot.RandomSeed) * 100f < 0f)
            {
                shot.BlockedBy = __instance.Item.Id;
                Debug.Log(">>> Shot blocked by armor piece");
                if (Plugin.EnableLogging.Value == true)
                {
                    Logger.LogWarning("===========PEN STATUS=============== ");
                    Logger.LogWarning("Blocked");
                    Logger.LogWarning("========================== ");
                }
            }
            else
            {
                if (Plugin.EnableLogging.Value == true)
                {
                    Logger.LogWarning("============PEN STATUS============== ");
                    Logger.LogWarning("Penetrated");
                    Logger.LogWarning("========================== ");
                }
            }
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
        private static bool Prefix(ref DamageInfo damageInfo, bool damageInfoIsLocal, ref ArmorComponent __instance, ref float __result)
        {


            string hitPart = damageInfo.HittedBallisticCollider.name;
            bool isArmArmor = false;
            bool isArmor = true;
            if ((__instance.Template.ArmorZone.Contains(EBodyPart.LeftArm) || __instance.Template.ArmorZone.Contains(EBodyPart.RightArm)))
            {
                if ((hitPart == "Base HumanRUpperarm" || hitPart == "Base HumanLUpperarm")) 
                {
                    isArmArmor = true;
                }
                if ((hitPart == "Base HumanRForearm1" || hitPart == "Human LForearm1"))
                {
                    isArmor = false;
                }
            }


            AmmoTemplate ammoTemp = (AmmoTemplate)Singleton<ItemFactory>.Instance.ItemTemplates[damageInfo.SourceId];
            BulletClass ammo = new BulletClass("newAmmo", ammoTemp);

            EDamageType damageType = damageInfo.DamageType;

            float speedFactor = ammo.GetBulletSpeed / damageInfo.ArmorDamage;
            float armorDamage = ammo.ArmorDamage * speedFactor;


            if (!damageType.IsWeaponInduced() && damageType != EDamageType.GrenadeFragment)
            {
                __result = 0f;
                return false;
            }
            __instance.TryShatter(damageInfo.Player, damageInfoIsLocal);
            if (__instance.Repairable.Durability <= 0f)
            {
                __result = 0f;
                return false;
            }
   
            float KE = (0.5f * ammo.BulletMassGram * damageInfo.ArmorDamage * damageInfo.ArmorDamage) / 1000;
            float keToDamageFactor = 24f;
            float bluntThrput = isArmor == true ? 1f :__instance.Template.BluntThroughput;
            float penPower = damageInfo.PenetrationPower;
            float duraPercent = __instance.Repairable.Durability / (float)__instance.Repairable.TemplateDurability;
            float armorResist = (float)Singleton<BackendConfigSettingsClass>.Instance.Armor.GetArmorClass(__instance.ArmorClass).Resistance;
            armorResist = isArmArmor == true ? armorResist = 30f : !isArmor ? armorResist = 0f : armorResist;
            float armorDestructibility = Singleton<BackendConfigSettingsClass>.Instance.ArmorMaterials[__instance.Template.ArmorMaterial].Destructibility;

            float armorFactor = armorResist * (Mathf.Min(1f, duraPercent * 2f));
            float throughputDuraFactored = Mathf.Min(1f, bluntThrput * (1f + ((duraPercent - 1f) * -1f)));
            float penFactoredClass = Mathf.Max(1f, armorFactor - (penPower / 1.8f));
            float maxPotentialDamage = (KE / Mathf.Max(1, (penFactoredClass / 40f)) / keToDamageFactor);
            float throughputFacotredDamage = maxPotentialDamage * throughputDuraFactored;

            float armorStatReductionFactor = Mathf.Max((1 - (penFactoredClass / 100f)), 0.1f);

            if (damageInfo.DeflectedBy == __instance.Item.Id)
            {
                damageInfo.Damage *= 0.5f * armorStatReductionFactor;
                armorDamage *= 0.5f * armorStatReductionFactor;
                damageInfo.ArmorDamage *= 0.5f * armorStatReductionFactor;
                damageInfo.PenetrationPower *= 0.5f * armorStatReductionFactor;
            }

            if (__instance.Template.ArmorMaterial == EArmorMaterial.ArmoredSteel && !__instance.Template.ArmorZone.Contains(EBodyPart.Head))
            {
                float steelPenFactoredClass = Mathf.Max(1f, armorResist - (penPower / 1.8f));
                float steelMaxPotentialDamage = (KE / Mathf.Max(1, (steelPenFactoredClass / 40f)) / keToDamageFactor);
                throughputFacotredDamage = steelMaxPotentialDamage * bluntThrput;
            }
            if (__instance.Template.ArmorMaterial == EArmorMaterial.ArmoredSteel && __instance.Template.ArmorZone.Contains(EBodyPart.Head))
            {
                armorDestructibility = 0.05f;
            }
            float durabilityLoss = (maxPotentialDamage / penPower) * (Mathf.Min(1, ammo.BulletDiameterMilimeters / 10f)) * armorDamage * armorDestructibility;

            if (!(damageInfo.BlockedBy == __instance.Item.Id) && !(damageInfo.DeflectedBy == __instance.Item.Id))
            {
                if (!isArmor)
                {
                    damageInfo.PenetrationPower = penPower * 0.85f;
                }
                else 
                {
                    durabilityLoss *= (1 - (penPower / 100f));
                    damageInfo.Damage *= armorStatReductionFactor;
                    damageInfo.PenetrationPower *= armorStatReductionFactor;
                }
            }
            else
            {
                damageInfo.Damage = throughputFacotredDamage;
                damageInfo.StaminaBurnRate = throughputFacotredDamage / 100f;
            }
            durabilityLoss = isArmArmor == true ? durabilityLoss * 0.25f : !isArmor ? durabilityLoss = 0f : durabilityLoss;
            durabilityLoss = Mathf.Max(0.01f, durabilityLoss);
            __instance.ApplyDurabilityDamage(durabilityLoss);
            __result = durabilityLoss;

            if (Plugin.EnableLogging.Value == true)
            {
                Logger.LogWarning("===========ARMOR DAMAGE=============== ");
                Logger.LogWarning("KE " + KE);
                Logger.LogWarning("Pen " + penPower);
                Logger.LogWarning("Armor Damage " + armorDamage);
                Logger.LogWarning("Class " + armorResist);
                Logger.LogWarning("Throughput " + bluntThrput);
                Logger.LogWarning("Dura percent " + duraPercent);
                Logger.LogWarning("Durability Loss " + durabilityLoss);
                Logger.LogWarning("Max potential damage " + maxPotentialDamage);
                Logger.LogWarning("Damage " + damageInfo.Damage);
                Logger.LogWarning("Throughput Facotred Damage " + throughputFacotredDamage);
                Logger.LogWarning("========================== ");
            }
            return false;
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
        private static bool Prefix(EFT.Ballistics.BallisticsCalculator __instance, BulletClass ammo, Vector3 origin, Vector3 direction, int fireIndex, Player player, Item weapon, ref GClass2620 __result, float speedFactor, int fragmentIndex = 0)
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

            __result = GClass2620.Create(ammo, fragmentIndex, randomNum, origin, direction, velocityFactored, velocityFactored, ammo.BulletMassGram, ammo.BulletDiameterMilimeters, (float)damageFactored, penPowerFactored, penChanceFactored, ammo.RicochetChance, fragchanceFactored, 1f, ammo.MinFragmentsCount, ammo.MaxFragmentsCount, EFT.Ballistics.BallisticsCalculator.DefaultHitBody, __instance.Randoms, bcFactored, player, weapon, fireIndex, null);
            return false;

        }
    }
}
