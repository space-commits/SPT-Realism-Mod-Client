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
using System.Drawing;
using BepInEx.Logging;

namespace RealismMod
{
    public enum EHitZone
    {
        AZone,
        CZone,
        DZone,
        Neck,
        Heart,
        Spine,
        ArmArmor,
        NeckArmor,
        SidePlate,
        ChestPlate,
        StomachArmor,
        Unknown
    }

    public enum EHitOrientation
    {
        FrontHit,
        RearHit,
        BackHit,
        LeftSideHit,
        RightSideHit,
        TopHit,
        BottomHit,
        UnknownOrientation
    }

    public static class HitBox 
    {
        public const string Head = "Base HumanHead";
        public const string UpperTorso = "Base HumanSpine3";
        public const string LowerTorso = "Base HumanSpine2";
        public const string Pelvis = "Base HumanPelvis";

        public const string RightUpperArm = "Base HumanRUpperarm";
        public const string RightForearm = "Base HumanRForearm1";
        public const string LeftUpperArm = "Base HumanLUpperarm";
        public const string LeftForearm = "Base HumanLForearm1";

        public const string LeftThigh = "Base HumanLThigh1";
        public const string LeftCalf = "Base HumanLCalf";
        public const string RightThigh = "Base HumanRThigh1";
        public const string RightCalf = "Base HumanRCalf";
    }

    public static class HitZoneModifiers
    {
        public const float Neck = 100f;
        public const float Spine = 80f;
        public const float Heart = 120f;
        public const float Calf = 0.75f;
        public const float Forearm = 0.85f;
        public const float Thigh = 1.25f;
        public const float UpperArm = 1.15f;
        public const float AZone = 2f;
        public const float CZone = 1.25f;
        public const float DZone = 0.75f;
    }

    public static class BallisticsController
    {
        public static EHitOrientation GetHitOrientation(string hitPart, Vector3 hitNormal, ManualLogSource logger)
        {
            if (Mathf.Abs(hitNormal.y) > Mathf.Abs(hitNormal.x) && Mathf.Abs(hitNormal.y) > Mathf.Abs(hitNormal.z))
            {
                if (hitNormal.y > 0)
                {
                    if (Plugin.EnableLogging.Value == true)
                    {
                        logger.LogWarning("hit top side of the box collider");
                    }
                    return EHitOrientation.TopHit;
                }
                else
                {
                    if (Plugin.EnableLogging.Value == true)
                    {
                        logger.LogWarning("hit bottom side of the box collider");
                    }
                    return EHitOrientation.BottomHit;
                }
            }
            else if (Mathf.Abs(hitNormal.x) > Mathf.Abs(hitNormal.y) && Mathf.Abs(hitNormal.x) > Mathf.Abs(hitNormal.z))
            {
                if (hitNormal.x > 0)
                {
                    if (Plugin.EnableLogging.Value == true)
                    {
                        logger.LogWarning("hit left side of the box collider");
                    }
                    return EHitOrientation.RightSideHit;
                }
                else
                {
                    if (Plugin.EnableLogging.Value == true)
                    {
                        logger.LogWarning("hit right side of the box collider");
                    }
                    return EHitOrientation.LeftSideHit;
                }
            }
            else
            {
                if (hitNormal.z > 0)
                {
                    if (Plugin.EnableLogging.Value == true)
                    {
                        logger.LogWarning("hit front side of the box collider");
                    }
                    return EHitOrientation.FrontHit;
                }
                else
                {
                    if (Plugin.EnableLogging.Value == true)
                    {
                        logger.LogWarning("hit back side of the box collider");
                    }
                    return EHitOrientation.BackHit;
                }
            }
        }

        public static EHitZone GetHitBodyZone(ManualLogSource logger, string hitPart, Vector3 normalizedPoint, EHitOrientation hitOrientation)
        {
            float neckX = -0.9f;

            float spineZ = 0.05f;

            float heartL = 0.51f;
            float heartR = 0.25f;
            float heartTop = -0.71f;
            float heartBottom = -0.45f;

            float dZoneZUpper = 0.8f;
            float dZoneZMid = 0.65f;
            float dZoneZLower = 0.5f;
            float dZoneXLower = -0.8f;

            float aZoneZUpper = 0.45f;
            float aZoneZMid = 0.25f;
            float aZoneXMid = -0.92f;

            if (hitOrientation == EHitOrientation.LeftSideHit || hitOrientation == EHitOrientation.RightSideHit)
            {
                if (hitPart == HitBox.UpperTorso && normalizedPoint.x < neckX)
                {
                    if (Plugin.EnableLogging.Value == true)
                    {
                        logger.LogWarning("NECK HIT FROM SIDE");
                    }
                    return EHitZone.Neck;
                }
                if (hitPart == HitBox.UpperTorso)
                {
                    if (Plugin.EnableLogging.Value == true)
                    {
                        logger.LogWarning("A-ZONE HIT: SIDE");
                    }
                    return EHitZone.AZone;
                }
                if (hitPart == HitBox.LowerTorso)
                {
                    if (Plugin.EnableLogging.Value == true)
                    {
                        logger.LogWarning("C-ZONE HIT: SIDE");
                    }
                    return EHitZone.CZone;
                }
                if (hitPart == HitBox.Pelvis)
                {
                    if (Plugin.EnableLogging.Value == true)
                    {
                        logger.LogWarning("D-ZONE HIT: SIDE");
                    }
                    return EHitZone.DZone;
                }
            }

            if (hitOrientation == EHitOrientation.TopHit)
            {
                if (hitPart == HitBox.UpperTorso && normalizedPoint.x < neckX)
                {
                    if (Plugin.EnableLogging.Value == true)
                    {
                        logger.LogWarning("NECK HIT FROM TOP");
                    }
                    return EHitZone.Neck;
                }
                if (normalizedPoint.z > -spineZ && normalizedPoint.z < spineZ)
                {
                    if (Plugin.EnableLogging.Value == true)
                    {
                        logger.LogWarning("SPINE HIT FROM TOP");
                    }
                    return EHitZone.Spine;
                }
                if (Plugin.EnableLogging.Value == true)
                {
                    logger.LogWarning("A-ZONE HIT: TOP OF COLLIDER");
                }
                return EHitZone.AZone;
            }
            if (hitOrientation == EHitOrientation.BottomHit)
            {
                if (Plugin.EnableLogging.Value == true)
                {
                    logger.LogWarning("C-ZONE HIT: BOTTOM OF COLLIDER");
                }
                return EHitZone.CZone;
            }

            if (hitOrientation == EHitOrientation.BackHit || hitOrientation == EHitOrientation.FrontHit)
            {
                if (normalizedPoint.z > -spineZ && normalizedPoint.z < spineZ)
                {
                    if (Plugin.EnableLogging.Value == true)
                    {
                        logger.LogWarning("SPINE HIT");
                    }
                    return EHitZone.Spine;
                }
                if (hitPart == HitBox.UpperTorso)
                {
                    if (normalizedPoint.z < heartL && normalizedPoint.z > heartR && normalizedPoint.x > heartTop && normalizedPoint.x < heartBottom)
                    {
                        if (Plugin.EnableLogging.Value == true)
                        {
                            logger.LogWarning("HEART HIT");
                        }
                        return EHitZone.Heart;
                    }
                    if (normalizedPoint.x < neckX)
                    {
                        if (Plugin.EnableLogging.Value == true)
                        {
                            logger.LogWarning("NECK HIT");
                        }
                        return EHitZone.Neck;
                    }
                    if (normalizedPoint.z < -dZoneZUpper || normalizedPoint.z > dZoneZUpper)
                    {
                        if (Plugin.EnableLogging.Value == true)
                        {
                            logger.LogWarning("D-ZONE HIT: UPPER TORSO");
                        }
                        return EHitZone.DZone;

                    }
                    else if (normalizedPoint.z > -aZoneZUpper && normalizedPoint.z < aZoneZUpper && normalizedPoint.x >= neckX)
                    {
                        if (Plugin.EnableLogging.Value == true)
                        {
                            logger.LogWarning("A-ZONE HIT: UPPER TORSO");
                        }
                        return EHitZone.AZone;
                    }
                    else if (normalizedPoint.x >= neckX)
                    {
                        if (Plugin.EnableLogging.Value == true)
                        {
                            logger.LogWarning("C-ZONE HIT: UPPER TORSO");
                        }
                        return EHitZone.CZone;
                    }
                }

                if (hitPart == HitBox.LowerTorso)
                {
                    if (normalizedPoint.z < -dZoneZMid || normalizedPoint.z > dZoneZMid)
                    {
                        if (Plugin.EnableLogging.Value == true)
                        {
                            logger.LogWarning("D-ZONE HIT: MID TORSO");
                        }
                        return EHitZone.DZone;
                    }
                    else if (normalizedPoint.z > -aZoneZMid && normalizedPoint.z < aZoneZMid && normalizedPoint.x < aZoneXMid)
                    {
                        if (Plugin.EnableLogging.Value == true)
                        {
                            logger.LogWarning("A-ZONE HIT: MID TORSO");
                        }
                        return EHitZone.AZone;
                    }
                    else
                    {
                        if (Plugin.EnableLogging.Value == true)
                        {
                            logger.LogWarning("C-ZONE HIT: MID TORSO");
                        }
                        return EHitZone.CZone;
                    }
                }
                if (hitPart == HitBox.Pelvis)
                {
                    if (normalizedPoint.z >= -dZoneZLower && normalizedPoint.z <= dZoneZLower && normalizedPoint.x <= dZoneXLower)
                    {
                        if (Plugin.EnableLogging.Value == true)
                        {
                            logger.LogWarning("C-ZONE HIT: MID TORSO");
                        }
                        return EHitZone.CZone;
                    }
                    else
                    {
                        if (Plugin.EnableLogging.Value == true)
                        {
                            logger.LogWarning("D-ZONE HIT: MID TORSO");
                        }
                        return EHitZone.DZone;
                    }
                }
            }
            return EHitZone.Unknown;
        }

        public static void GetHitArmorZone(ManualLogSource logger, ArmorComponent ac, string hitPart, Vector3 normalizedPoint, EHitOrientation hitOrientation, bool hasSideArmor, bool hasStomachArmor, bool hasNeckArmor, ref bool hasBypassedArmor, ref bool hitSecondaryArmor)
        {
            float bottomOfPlate = hitOrientation == EHitOrientation.BackHit ? -0.9f : -0.5f;
            float topOfPlate = hitOrientation == EHitOrientation.BackHit ? -0.85f : -0.72f;
            float upperSides = hitOrientation == EHitOrientation.BackHit ? 0.8f : 0.38f;
            float midSides = hitOrientation == EHitOrientation.BackHit ? 0.7f : 0.38f;
            float lowerSides = hitOrientation == EHitOrientation.BackHit ? 0.6f : 0.52f;
            float topOfSidePlates = -0.55f;
            float bottomOfStomachArmorRear = -0.75f;

            if (hitPart == HitBox.UpperTorso)
            {
                if (normalizedPoint.x < topOfPlate)
                {
                    if (!hasNeckArmor) 
                    {
                        if (Plugin.EnableLogging.Value == true)
                        {
                            logger.LogWarning("ARMOR BYPASSED: NECK");
                        }
                        hasBypassedArmor = true;
                    }
                    else
                    {
                        if (Plugin.EnableLogging.Value == true)
                        {
                            logger.LogWarning("NECK ARMOR HIT");
                        }
                        hitSecondaryArmor = true;
                    }

                }

                if (normalizedPoint.z < -upperSides || normalizedPoint.z > upperSides)
                {
                    if (Plugin.EnableLogging.Value == true)
                    {
                        logger.LogWarning("ARMOR BYPASSED: UPPER SIDES");
                    }
                    hasBypassedArmor = true;
                }
            }

            if (hitPart == HitBox.LowerTorso)
            {
                if (!hasSideArmor)
                {
                    if (normalizedPoint.z < -midSides || normalizedPoint.z > midSides)
                    {
                        if (Plugin.EnableLogging.Value == true)
                        {
                            logger.LogWarning("ARMOR BYPASSED: LOWER SIDES");
                        }
                        hasBypassedArmor = true;
                    }
                }
                else
                {
                    if (normalizedPoint.x < topOfSidePlates && (normalizedPoint.z < -midSides || normalizedPoint.z > midSides))
                    {
                        if (Plugin.EnableLogging.Value == true)
                        {
                            logger.LogWarning("ARMOR BYPASSED: LOWER SIDES WITH SIDE ARMOR");
                        }
                        hasBypassedArmor = true;
                    }
                }
            }

            if (hitPart == HitBox.Pelvis)
            {
                if (!hasStomachArmor)
                {
                    if (normalizedPoint.x > bottomOfPlate)
                    {
                        if (Plugin.EnableLogging.Value == true)
                        {
                            logger.LogWarning("ARMOR BYPASSED: BELOW PLATE, STOMACH");
                        }
                        hasBypassedArmor = true;
                    }
                }
                else
                {
                    if (normalizedPoint.z > -lowerSides && normalizedPoint.z < lowerSides && ((hitOrientation == EHitOrientation.BackHit && normalizedPoint.x < bottomOfStomachArmorRear && normalizedPoint.x > bottomOfPlate) || (hitOrientation == EHitOrientation.FrontHit && normalizedPoint.x > bottomOfPlate)))
                    {
                        if (Plugin.EnableLogging.Value == true)
                        {
                            logger.LogWarning("STOMACH ARMOR HIT");
                        }
                        hitSecondaryArmor = true;
                    }
                    if (normalizedPoint.z < -lowerSides || normalizedPoint.z > lowerSides || (hitOrientation == EHitOrientation.BackHit && normalizedPoint.x > bottomOfStomachArmorRear))
                    {
                        if (Plugin.EnableLogging.Value == true)
                        {
                            logger.LogWarning("ARMOR BYPASSED: STOMACH ARMOR SIDES OR BOTTOM REAR");
                        }
                        hasBypassedArmor = true;
                    }
                }
            }

            if ((ac.Template.ArmorZone.Contains(EBodyPart.LeftArm) || ac.Template.ArmorZone.Contains(EBodyPart.RightArm)))
            {
                if ((hitPart == HitBox.RightUpperArm || hitPart == HitBox.LeftUpperArm))
                {
                    if (Plugin.EnableLogging.Value == true)
                    {
                        logger.LogWarning("ARM ARMOR HIT");
                    }
                    hitSecondaryArmor = true;
                }
                if ((hitPart == HitBox.RightForearm || hitPart == HitBox.LeftForearm))
                {
                    if (Plugin.EnableLogging.Value == true)
                    {
                        logger.LogWarning("ARMOR BYPASSED: FOREARM");
                    }
                    hasBypassedArmor = true;
                }
            }
        }
    }

    public class DamageInfoPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(DamageInfo).GetConstructor(new Type[] { typeof(EDamageType), typeof(GClass2620) });
        }

        private static void modifyDamageByHitZone(string hitPart, EHitZone hitZone, ref DamageInfo di) 
        {
            bool hitCalf = hitPart == HitBox.LeftCalf || hitPart == HitBox.RightCalf ? true : false;
            bool hitThigh = hitPart == HitBox.LeftThigh || hitPart == HitBox.RightThigh ? true : false;
            bool hitUpperArm = hitPart == HitBox.LeftUpperArm || hitPart == HitBox.RightUpperArm ? true : false;
            bool hitForearm = hitPart == HitBox.LeftForearm || hitPart == HitBox.RightForearm ? true : false;

            if (hitCalf == true)
            {
                di.Damage *= HitZoneModifiers.Calf;
            }
            if (hitThigh == true)
            {
                di.Damage *= HitZoneModifiers.Thigh;
            }
            if (hitForearm == true)
            {
                di.Damage *= HitZoneModifiers.Forearm;
            }
            if (hitUpperArm == true)
            {
                di.Damage *= HitZoneModifiers.UpperArm;
            }
            if (hitZone == EHitZone.AZone) 
            {
                di.Damage *= HitZoneModifiers.AZone;
            }
            if (hitZone == EHitZone.CZone)
            {
                di.Damage *= HitZoneModifiers.CZone;
            }
            if (hitZone == EHitZone.DZone)
            {
                di.Damage *= HitZoneModifiers.DZone;
            }
            if (hitZone == EHitZone.Neck)
            {
                di.Damage += HitZoneModifiers.Neck;
            }
            if (hitZone == EHitZone.Heart)
            {
                di.Damage += HitZoneModifiers.Heart;
            }
            if (hitZone == EHitZone.Spine)
            {
                di.Damage += HitZoneModifiers.Spine;
            }
        }


        [PatchPrefix]
        private static bool Prefix(ref DamageInfo __instance, EDamageType damageType, GClass2620 shot)
        {
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

            if (!__instance.Blunt && __instance.DamageType == EDamageType.Bullet)
            {
                Collider col = shot.HitCollider;
                Vector3 localPoint = col.transform.InverseTransformPoint(shot.HitPoint);
                Vector3 normalizedPoint = localPoint.normalized;
                Vector3 hitNormal = shot.HitNormal;
                string hitPart = shot.HittedBallisticCollider.name;
                EHitOrientation hitOrinetation = BallisticsController.GetHitOrientation(hitPart, hitNormal, Logger);
                EHitZone hitZone = BallisticsController.GetHitBodyZone(Logger, hitPart, normalizedPoint, hitOrinetation);
                Logger.LogWarning("===================");
                Logger.LogWarning("damage before = " + __instance.Damage);
                modifyDamageByHitZone(hitPart, hitZone, ref __instance);
                Logger.LogWarning("hit collider = " + hitPart);
                Logger.LogWarning("x = " + normalizedPoint.x);
                Logger.LogWarning("y = " + normalizedPoint.y);
                Logger.LogWarning("z = " + normalizedPoint.z);
                Logger.LogWarning("damage after = " + __instance.Damage);
                Logger.LogWarning("===================");
            }

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

        [PatchPrefix]
        private static void Prefix(Player __instance, DamageInfo damageInfo, EBodyPart bodyPartType, float absorbed, EHeadSegment? headSegment = null)
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

                Collider col = damageInfo.HitCollider;
                Vector3 localPoint = col.transform.InverseTransformPoint(damageInfo.HitPoint);
                Vector3 normalizedPoint = localPoint.normalized;
                Vector3 hitNormal = damageInfo.HitNormal;
                string hitPart = damageInfo.HittedBallisticCollider.name;
                bool hitCalf = hitPart == HitBox.LeftCalf || hitPart == HitBox.RightCalf ? true : false;
                bool hitThigh = hitPart == HitBox.LeftThigh || hitPart == HitBox.RightThigh ? true : false;
                bool hitUpperArm = hitPart == HitBox.LeftUpperArm || hitPart == HitBox.RightUpperArm ? true : false;
                bool hitForearm = hitPart == HitBox.LeftForearm || hitPart == HitBox.RightForearm ? true : false;

                EHitOrientation hitOrientation = EHitOrientation.UnknownOrientation;

                if (hitPart == HitBox.UpperTorso || hitPart == HitBox.LowerTorso || hitPart == HitBox.Pelvis)
                {
                    hitOrientation = BallisticsController.GetHitOrientation(hitPart, hitNormal, Logger);
                }

                if (armor != null)
                {
                    bool hasSideArmor = ArmorProperties.HasSideArmor(armor.Item);
                    bool hasStomachArmor = ArmorProperties.HasStomachArmor(armor.Item);
                    bool hasNeckArmor = ArmorProperties.HasStomachArmor(armor.Item);
                    bool hasArmArmor = armor.Template.ArmorZone.Contains(EBodyPart.LeftArm) || armor.Template.ArmorZone.Contains(EBodyPart.RightArm);
                    bool hitSecondaryArmor = false;
                    bool hasBypassedArmor = false;

                    BallisticsController.GetHitArmorZone(Logger, armor, hitPart, normalizedPoint, hitOrientation, hasSideArmor, hasStomachArmor, hasNeckArmor, ref hasBypassedArmor, ref hitSecondaryArmor);

                    if (damageInfo.Blunt == true && ArmorProperties.CanSpall(armor.Item) == true)
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

                        float duraPercent = _currentDura / _maxDura;
                        float armorFactor = !reduceDurability ? _armorClass * (Mathf.Min(1, duraPercent * 1f)) : _armorClass * (Mathf.Min(1, duraPercent * 2f)); //durability should be more important for steel plates, representing anti-spall coating. Lower the amount durapercent is factored by to increase importance
                        float penFactoredClass = Mathf.Max(1f, armorFactor - (damageInfo.PenetrationPower / 2.5f));
                        float maxPotentialDamage = (KE / penFactoredClass);

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

                            if (part == EBodyPart.Head && (hitOrientation != EHitOrientation.LeftSideHit || hitOrientation != EHitOrientation.RightSideHit))
                            {
                                damage = hasNeckArmor == true ? Mathf.Min(10, splitSpallingDmg * 0.25f) : Mathf.Min(10, splitSpallingDmg);
                                bleedFactor = hasNeckArmor == true ? 0f : bleedFactor;
                            }
                            if ((part == EBodyPart.LeftArm || part == EBodyPart.RightArm) && hasArmArmor)
                            {
                                damage *= 0.5f;
                                bleedFactor *= 0.5f;
                            }

                            if (Plugin.EnableLogging.Value == true)
                            {
                                Logger.LogWarning("Part Hit " + part);
                                Logger.LogWarning("Damage " + damage);
                                Logger.LogWarning("========================== ");
                            }
                            damageInfo.HeavyBleedingDelta = heavyBleedChance * bleedFactor;
                            damageInfo.LightBleedingDelta = lightBleedChance * bleedFactor;
                            __instance.ActiveHealthController.ApplyDamage(part, damage, damageInfo);
                        }
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

            RaycastHit raycast = (RaycastHit)AccessTools.Field(typeof(GClass2620), "raycastHit_0").GetValue(shot);
            Collider col = raycast.collider;
            Vector3 localPoint = col.transform.InverseTransformPoint(raycast.point);
            Vector3 normalizedPoint = localPoint.normalized;
            Vector3 hitNormal = raycast.normal;
            string hitPart = shot.HittedBallisticCollider.name;

            bool hitSecondaryArmor = false;
            bool hasBypassedArmor = false;
            bool hasSideArmor = ArmorProperties.HasSideArmor(__instance.Item);
            bool hasStomachArmor = ArmorProperties.HasStomachArmor(__instance.Item);
            bool hasNeckArmor = ArmorProperties.HasNeckArmor(__instance.Item);
            EHitOrientation hitOrientation = EHitOrientation.UnknownOrientation;

            if (hitPart == HitBox.UpperTorso || hitPart == HitBox.LowerTorso || hitPart == HitBox.Pelvis)
            {
                hitOrientation = BallisticsController.GetHitOrientation(hitPart, hitNormal, Logger);
            }
  
            BallisticsController.GetHitArmorZone(Logger, __instance, hitPart, normalizedPoint, hitOrientation, hasSideArmor, hasStomachArmor, hasNeckArmor, ref hasBypassedArmor, ref hitSecondaryArmor);

            if (Plugin.EnableLogging.Value == true) 
            {
                Logger.LogWarning("=============SetPenStatus: Hit Zone ===============");
                Logger.LogWarning("collider = " + shot.HittedBallisticCollider);
                Logger.LogWarning("has bypassed armor = " + hasBypassedArmor);
                Logger.LogWarning("has secondary armor = " + hitSecondaryArmor);
                Logger.LogWarning("hit x = " + normalizedPoint.x);
                Logger.LogWarning("hit y = " + normalizedPoint.y);
                Logger.LogWarning("hit z = " + normalizedPoint.z);
                Logger.LogWarning("============================");
            }


            if (hasBypassedArmor == true) 
            {
                return false;
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
            armorResist = hitSecondaryArmor == true ? armorResist = 40f : armorResist;
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

            Collider col = damageInfo.HitCollider;
            Vector3 localPoint = col.transform.InverseTransformPoint(damageInfo.HitPoint);
            Vector3 normalizedPoint = localPoint.normalized;
            Vector3 hitNormal = damageInfo.HitNormal;

            string hitPart = damageInfo.HittedBallisticCollider.name;
            bool hitSecondaryArmor = false;
            bool hasBypassedArmor = false;

            bool hasSideArmor = ArmorProperties.HasSideArmor(__instance.Item);
            bool hasStomachArmor = ArmorProperties.HasStomachArmor(__instance.Item);
            bool hasNeckArmor = ArmorProperties.HasNeckArmor(__instance.Item);
            EHitOrientation hitOrientation = EHitOrientation.UnknownOrientation;

            if (hitPart == HitBox.UpperTorso || hitPart == HitBox.LowerTorso || hitPart == HitBox.Pelvis)
            {
                hitOrientation = BallisticsController.GetHitOrientation(hitPart, hitNormal, Logger);
            }

            BallisticsController.GetHitArmorZone(Logger, __instance, hitPart, normalizedPoint, hitOrientation, hasSideArmor, hasStomachArmor, hasNeckArmor, ref hasBypassedArmor, ref hitSecondaryArmor);

            if (Plugin.EnableLogging.Value == true)
            {
                Logger.LogWarning("============ApplyDamagePatch: Hit Zone ============== ");
                Logger.LogWarning("collider = " + damageInfo.HittedBallisticCollider);
                Logger.LogWarning("Has Bypassed Armor = " + hasBypassedArmor);
                Logger.LogWarning("Has Hit Secondary Armor = " + hitSecondaryArmor);
                Logger.LogWarning("hit x = " + normalizedPoint.x);
                Logger.LogWarning("hit y = " + normalizedPoint.y);
                Logger.LogWarning("hit z = " + normalizedPoint.z);
                Logger.LogWarning("========================== ");
            }

            AmmoTemplate ammoTemp = (AmmoTemplate)Singleton<ItemFactory>.Instance.ItemTemplates[damageInfo.SourceId];
            BulletClass ammo = new BulletClass("newAmmo", ammoTemp);

            EDamageType damageType = damageInfo.DamageType;

            float speedFactor = ammo.GetBulletSpeed / damageInfo.ArmorDamage;
            float armorDamageActual = ammo.ArmorDamage * speedFactor;

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
            float bluntThrput = hasBypassedArmor == true ? 1f : hitSecondaryArmor == true ? __instance.Template.BluntThroughput * 1.5f : __instance.Template.BluntThroughput;
            float penPower = damageInfo.PenetrationPower;
            float duraPercent = __instance.Repairable.Durability / (float)__instance.Repairable.TemplateDurability;
            float armorResist = (float)Singleton<BackendConfigSettingsClass>.Instance.Armor.GetArmorClass(__instance.ArmorClass).Resistance;
            armorResist = hitSecondaryArmor == true ? armorResist = 30f : hasBypassedArmor == true ? armorResist = 0f : armorResist;
            float armorDestructibility = Singleton<BackendConfigSettingsClass>.Instance.ArmorMaterials[__instance.Template.ArmorMaterial].Destructibility;

            float armorFactor = armorResist * (Mathf.Min(1f, duraPercent * 1.25f));
            float throughputDuraFactored = Mathf.Min(1f, bluntThrput * (1f + ((duraPercent - 1f) * -1f)));
            float penFactoredClass = Mathf.Max(1f, armorFactor - (penPower / 1.8f));
            float maxPotentialDamage = KE / penFactoredClass;
            float throughputFacotredDamage = maxPotentialDamage * throughputDuraFactored;

            float armorStatReductionFactor = Mathf.Max((1 - (penFactoredClass / 100f)), 0.1f);

            if (damageInfo.DeflectedBy == __instance.Item.Id)
            {
                damageInfo.Damage *= 0.5f * armorStatReductionFactor;
                armorDamageActual *= 0.5f * armorStatReductionFactor;
                damageInfo.ArmorDamage *= 0.5f * armorStatReductionFactor;
                damageInfo.PenetrationPower *= 0.5f * armorStatReductionFactor;
            }

            if (__instance.Template.ArmorMaterial == EArmorMaterial.ArmoredSteel && !__instance.Template.ArmorZone.Contains(EBodyPart.Head))
            {
                float steelPenFactoredClass = Mathf.Max(1f, armorResist - (penPower / 1.8f));
                float steelMaxPotentialDamage = (KE / steelPenFactoredClass);
                throughputFacotredDamage = steelMaxPotentialDamage * bluntThrput;
            }
            if (__instance.Template.ArmorMaterial == EArmorMaterial.ArmoredSteel && __instance.Template.ArmorZone.Contains(EBodyPart.Head))
            {
                armorDestructibility = 0.05f;
            }
            float durabilityLoss = (maxPotentialDamage / penPower) * Mathf.Clamp(ammo.BulletDiameterMilimeters / 7.62f, 1f, 1.25f) * armorDamageActual * armorDestructibility; //blunt damage shouldn't come into it.

            if (!(damageInfo.BlockedBy == __instance.Item.Id) && !(damageInfo.DeflectedBy == __instance.Item.Id) && !hasBypassedArmor)
            {
                durabilityLoss *= (1 - (penPower / 100f));
                damageInfo.Damage *= armorStatReductionFactor;
                damageInfo.PenetrationPower *= armorStatReductionFactor;
            }
            else if(!hasBypassedArmor)
            {
                damageInfo.Damage = throughputFacotredDamage;
                damageInfo.StaminaBurnRate = throughputFacotredDamage / 100f;
            }

            if ((damageInfo.BlockedBy == __instance.Item.Id || damageInfo.DeflectedBy == __instance.Item.Id) && !hasBypassedArmor) 
            {
                damageInfo.HeavyBleedingDelta =  0f;
                damageInfo.LightBleedingDelta = 0f;
            }

            durabilityLoss = hitSecondaryArmor == true ? durabilityLoss * 0.25f : hasBypassedArmor == true ? durabilityLoss = 0f : durabilityLoss;
            durabilityLoss = Mathf.Max(0.01f, durabilityLoss);
            __instance.ApplyDurabilityDamage(durabilityLoss);
            __result = durabilityLoss;

            if (Plugin.EnableLogging.Value == true)
            {
                Logger.LogWarning("===========ARMOR DAMAGE=============== ");
                Logger.LogWarning("KE " + KE);
                Logger.LogWarning("Pen " + penPower);
                Logger.LogWarning("Armor Damage " + armorDamageActual);
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


    /*    public class AltSetPenetrationStatusPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                var result = typeof(ArmorComponent).GetMethod(nameof(ArmorComponent.SetPenetrationStatus), BindingFlags.Public | BindingFlags.Instance);

                return result;

            }

            private static void GetMaterialRandomFactor(EArmorMaterial armorMat, ref float min, ref float max)
            {
                switch (armorMat)
                {
                    case EArmorMaterial.UHMWPE:
                        min = 0.97f;
                        max = 1.03f;
                        break;
                    case EArmorMaterial.ArmoredSteel:
                        min = 1f;
                        max = 1f;
                        break;
                    case EArmorMaterial.Titan:
                        min = 0.99f;
                        max = 1.01f;
                        break;
                    case EArmorMaterial.Aluminium:
                        min = 0.98f;
                        max = 1.02f;
                        break;
                    case EArmorMaterial.Glass:
                        min = 1f;
                        max = 1f;
                        break;
                    case EArmorMaterial.Ceramic:
                        min = 0.99f;
                        max = 1.01f;
                        break;
                    case EArmorMaterial.Combined:
                        min = 0.98f;
                        max = 1.02f;
                        break;
                    case EArmorMaterial.Aramid:
                        min = 0.96f;
                        max = 1.04f;
                        break;

                }
            }

            [PatchPrefix]
            private static bool Prefix(GClass2620 shot, ref ArmorComponent __instance)
            {

                float penetrationPower = shot.PenetrationPower;
                float armorDura = __instance.Repairable.Durability / (float)__instance.Repairable.TemplateDurability;
                float armorDuraFactor = armorDura;
                float velocity = shot.VelocityMagnitude;
                float KE = (0.5f * shot.BulletMassGram * velocity * velocity) / 1000f;

                if (__instance.Template.ArmorMaterial == EArmorMaterial.ArmoredSteel)
                {
                    armorDuraFactor = 1f;
                }
                else if (__instance.Template.ArmorMaterial == EArmorMaterial.Titan)
                {
                    armorDuraFactor = Mathf.Min(1f, armorDuraFactor * 2f);
                }
                else
                {
                    armorDuraFactor = Mathf.Min(1f, armorDuraFactor * 1.25f);
                }

                float minRand = 1f;
                float maxRand = 1f;
                GetMaterialRandomFactor(__instance.Template.ArmorMaterial, ref minRand, ref maxRand);
                float randomFactor = UnityEngine.Random.Range(minRand * armorDura, maxRand * armorDura);

                //need an arm armor proxy or default values
                float minVel = ArmorProperties.MinVelocity(__instance.Item) * armorDuraFactor * randomFactor;
                float minKE = ArmorProperties.MinKE(__instance.Item) * armorDuraFactor * randomFactor;
                float minPen = ArmorProperties.MinPen(__instance.Item) * armorDuraFactor * randomFactor;

                if (isArmArmor == true)
                {
                    minVel = 290f * armorDuraFactor * randomFactor;
                    minKE = 160f * ArmorProperties.MinKE(__instance.Item) * armorDuraFactor * randomFactor;
                    minPen = 40f * ArmorProperties.MinPen(__instance.Item) * armorDuraFactor * randomFactor;
                }

                Logger.LogWarning("===========PEN STATUS=============== ");
                if (KE < minKE || penetrationPower < minPen || penetrationPower < minPen)
                {
                    shot.BlockedBy = __instance.Item.Id;
                    Debug.Log(">>> Shot blocked by armor piece");
                    if (Plugin.EnableLogging.Value == true)
                    {
                        Logger.LogWarning("Blocked");
                    }
                }
                else
                {
                    if (Plugin.EnableLogging.Value == true)
                    {
                        Logger.LogWarning("Penetrated");
                    }
                }

                Logger.LogWarning("min vel = " + minVel);
                Logger.LogWarning("min pen = " + minPen);
                Logger.LogWarning("min KE = " + minKE);
                Logger.LogWarning("======= ");
                Logger.LogWarning("vel = " + velocity);
                Logger.LogWarning("pen = " + penetrationPower);
                Logger.LogWarning("ke = " + KE);
                Logger.LogWarning("========================== ");

                return false;
            }
        }*/
}
