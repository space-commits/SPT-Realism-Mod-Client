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
using BepInEx.Logging;
using System.IO;
using System.Collections;


namespace RealismMod
{
    public enum EBodyHitZone
    {
        AZone,
        CZone,
        DZone,
        Heart,
        Spine,
        Unknown
    }

    public enum EHitOrientation
    {
        FrontHit,
        BackHit,
        LeftSideHit,
        RightSideHit,
        TopHit,
        BottomHit,
        UnknownOrientation
    }

    public static class HitZoneModifiers
    {
        public const float Neck = 100f;
        public const float Spine = 80f;
        public const float Heart = 120f;
        public const float Calf = 0.9f;
        public const float Forearm = 0.65f;
        public const float Thigh = 1.3f;
        public const float UpperArm = 1.35f;
        public const float AZone = 2f;
        public const float CZone = 1f;
        public const float DZone = 0.7f;
    }

    public static class BallisticsController
    {
        public static EBodyPartColliderType[] HeadCollidors = { EBodyPartColliderType.Eyes, EBodyPartColliderType.Ears, EBodyPartColliderType.Jaw, EBodyPartColliderType.BackHead, EBodyPartColliderType.NeckFront, EBodyPartColliderType.NeckBack, EBodyPartColliderType.HeadCommon, EBodyPartColliderType.ParietalHead };
        public static EBodyPartColliderType[] ArmCollidors = { EBodyPartColliderType.LeftUpperArm, EBodyPartColliderType.RightUpperArm, EBodyPartColliderType.LeftForearm, EBodyPartColliderType.RightForearm, };
        public static EBodyPartColliderType[] FaceSpallProtectionCollidors = { EBodyPartColliderType.NeckBack, EBodyPartColliderType.NeckFront, EBodyPartColliderType.Jaw, EBodyPartColliderType.Eyes, EBodyPartColliderType.HeadCommon };
        public static EBodyPartColliderType[] LegSpallProtectionCollidors = { EBodyPartColliderType.PelvisBack, EBodyPartColliderType.Pelvis};


        public static void CalcAfterPenStats(float actualDurability, float armorClass, float templateDurability, ref float damage, ref float penetration, float factor = 1) 
        {
            float armorFactor = 1f - ((armorClass / 80f) * (actualDurability / templateDurability));
            float damageReductionFactor = Mathf.Clamp(armorFactor, 0.85f, 1f);
            float penReductionFactor = Mathf.Clamp(armorFactor, 0.85f, 1f) * factor;
            damage *= damageReductionFactor;
            penetration *= penReductionFactor;
        }

        public static void ModifyDamageByHitZone(EBodyPartColliderType hitPart, ref DamageInfo di)
        {
            {
                switch (hitPart) 
                { 
                    case EBodyPartColliderType.RightCalf:
                    case EBodyPartColliderType.LeftCalf:
                        di.Damage *= 0.8f * Plugin.GlobalDamageModifier.Value;
                        di.HeavyBleedingDelta *= 0.75f;
                        di.LightBleedingDelta *= 0.75f;
                        break;
                    case EBodyPartColliderType.RightThigh:
                    case EBodyPartColliderType.LeftThigh:
                        di.Damage *= 1f * Plugin.GlobalDamageModifier.Value;
                        di.HeavyBleedingDelta *= 1.25f;
                        di.LightBleedingDelta *= 1.25f;
                        break;
                    case EBodyPartColliderType.RightForearm:
                    case EBodyPartColliderType.LeftForearm:
                        di.Damage *= 0.75f * Plugin.GlobalDamageModifier.Value;
                        di.HeavyBleedingDelta *= 0.55f;
                        di.LightBleedingDelta *= 0.55f;
                        break;
                    case EBodyPartColliderType.LeftUpperArm:
                    case EBodyPartColliderType.RightUpperArm:
                        di.Damage *= 0.9f * Plugin.GlobalDamageModifier.Value;
                        di.HeavyBleedingDelta *= 0.75f;
                        di.LightBleedingDelta *= 0.75f;
                        break;
                    case EBodyPartColliderType.PelvisBack:
                    case EBodyPartColliderType.Pelvis:
                        di.Damage *= 0.9f * Plugin.GlobalDamageModifier.Value;
                        di.HeavyBleedingDelta *= 0.85f;
                        di.LightBleedingDelta *= 0.85f;
                        break;
                    case EBodyPartColliderType.RibcageUp:
                    case EBodyPartColliderType.SpineTop:
                        di.Damage *= 1.01f * Plugin.GlobalDamageModifier.Value;
                        di.HeavyBleedingDelta *= 1.15f;
                        di.LightBleedingDelta *= 1.15f;
                        break;
                    case EBodyPartColliderType.RibcageLow:
                    case EBodyPartColliderType.SpineDown:
                        di.Damage *= 0.9f * Plugin.GlobalDamageModifier.Value;
                        di.HeavyBleedingDelta *= 0.95f;
                        di.LightBleedingDelta *= 0.95f;
                        break;
                    case EBodyPartColliderType.LeftSideChestDown:
                    case EBodyPartColliderType.RightSideChestDown:
                        di.Damage *= 0.9f * Plugin.GlobalDamageModifier.Value;
                        di.HeavyBleedingDelta *= 0.9f;
                        di.LightBleedingDelta *= 0.9f;
                        break;
                    case EBodyPartColliderType.LeftSideChestUp:
                    case EBodyPartColliderType.RightSideChestUp:
                        di.Damage *= 1.05f * Plugin.GlobalDamageModifier.Value;
                        di.HeavyBleedingDelta *= 1f;
                        di.LightBleedingDelta *= 1f;
                        break;
                    case EBodyPartColliderType.NeckBack:
                    case EBodyPartColliderType.NeckFront:
                        di.Damage *= 0.85f;
                        di.HeavyBleedingDelta *= 1.15f;
                        di.LightBleedingDelta *= 1.15f;
                        break;
                    case EBodyPartColliderType.Jaw:
                        di.Damage *= 0.85f;
                        di.HeavyBleedingDelta *= 0.95f;
                        di.LightBleedingDelta *= 0.95f;
                        break;
                    case EBodyPartColliderType.ParietalHead:
                        di.Damage *= 0.85f;
                        di.HeavyBleedingDelta *= 0.9f;
                        di.LightBleedingDelta *= 0.9f;
                        break;
                    case EBodyPartColliderType.BackHead:
                        di.Damage *= 1f;
                        di.HeavyBleedingDelta *= 1.15f;
                        di.LightBleedingDelta *= 1.15f;
                        break;
                    default:
                        break; 
                }
            }
        }
    }
    public static class HitBox
    {
        public static EHitOrientation GetHitOrientation(Vector3 hitNormal, Transform colliderTransform, ManualLogSource logger)
        {
            Vector3 localHitNormal = colliderTransform.InverseTransformDirection(hitNormal);

            if (Mathf.Abs(localHitNormal.y) > Mathf.Abs(localHitNormal.x) && Mathf.Abs(localHitNormal.y) > Mathf.Abs(localHitNormal.z))
            {
                if (localHitNormal.y > 0)
                {
                    if (Plugin.EnableBallisticsLogging.Value == true)
                    {
                        logger.LogWarning("hit front side of the box collider");
                    }
                    return EHitOrientation.FrontHit; //FRONT
                }
                else
                {
                    if (Plugin.EnableBallisticsLogging.Value == true)
                    {
                        logger.LogWarning("hit back side of the box collider");
                    }
                    return EHitOrientation.BackHit; //BACK
                }
            }
            else if (Mathf.Abs(localHitNormal.x) > Mathf.Abs(localHitNormal.y) && Mathf.Abs(localHitNormal.x) > Mathf.Abs(localHitNormal.z))
            {
                if (localHitNormal.x > 0)
                {
                    if (Plugin.EnableBallisticsLogging.Value == true)
                    {
                        logger.LogWarning("hit bottom side of the box collider");
                    }
                    return EHitOrientation.BottomHit; //BOTTOM
                }
                else
                {
                    if (Plugin.EnableBallisticsLogging.Value == true)
                    {
                        logger.LogWarning("hit top side of the box collider");
                    }
                    return EHitOrientation.TopHit; //TOP
                }
            }
            else
            {
                if (localHitNormal.z > 0)
                {
                    if (Plugin.EnableBallisticsLogging.Value == true)
                    {
                        logger.LogWarning("hit left side of the box collider");
                    }
                    return EHitOrientation.LeftSideHit; // LEFT
                }
                else
                {
                    if (Plugin.EnableBallisticsLogging.Value == true)
                    {
                        logger.LogWarning("hit right side of the box collider");
                    }
                    return EHitOrientation.RightSideHit; // RIGHT
                }
            }
        }

        private static bool hitSpine(Vector3 localPoint, bool isSideHit, float spineZ, ManualLogSource logger)
        {
            if (localPoint.z >= -spineZ && localPoint.z <= spineZ && !isSideHit)
            {
                if (Plugin.EnableBallisticsLogging.Value == true)
                {
                    logger.LogWarning("SPINE HIT");
                }
                return true;
            }
            return false;
        }

        public static EBodyHitZone GetHitBodyZone(ManualLogSource logger, Vector3 localPoint, EHitOrientation hitOrientation, EBodyPartColliderType hitPart)
        {
            bool isSideHit = hitOrientation == EHitOrientation.LeftSideHit || hitOrientation == EHitOrientation.RightSideHit;

            if (hitPart == EBodyPartColliderType.RibcageUp || hitPart == EBodyPartColliderType.RibcageLow || hitPart == EBodyPartColliderType.SpineDown || hitPart == EBodyPartColliderType.SpineTop)
            {
                float spineZ = 0.01f;
                float heartL = 0.03f;
                float heartR = -0.015f;
                float heartTop = hitOrientation == EHitOrientation.BackHit ? -0.0435f : -0.063f;
                float heartBottom = hitOrientation == EHitOrientation.BackHit ? -0.028f : -0.05f;

                float dZoneZUpper = hitOrientation == EHitOrientation.BackHit ? 0.11f : 0.115f;
                float dZoneZMid = hitOrientation == EHitOrientation.BackHit ? 0.11f : 0.115f;
    
                float aZoneZUpper = 0.0465f;
                float aZoneZMid = 0.0465f;
                float aZoneXMid = -0.07f; //-0.17f

                float topNeckZ = 0.04f;
                /*float topNeckX = -0.25f;*/
                float rearNeckZ = 0.05f;
                float rearNeckX = -0.2f;

                if (hitOrientation != EHitOrientation.TopHit && hitOrientation != EHitOrientation.BottomHit)
                {
                    if (isSideHit)
                    { 
                        if (Plugin.EnableBallisticsLogging.Value == true)
                        {
                            logger.LogWarning("SIDE HIT");
                        }
                        return EBodyHitZone.DZone;
                    }

                    if (hitPart == EBodyPartColliderType.RibcageUp || hitPart == EBodyPartColliderType.SpineTop)
                    {
                        if (hitOrientation == EHitOrientation.BackHit && localPoint.z > -rearNeckZ && localPoint.z < rearNeckZ && localPoint.x < rearNeckX)
                        {
                            if (Plugin.EnableBallisticsLogging.Value == true)
                            {
                                logger.LogWarning("NECK: BACK HIT (Counting As A-Zone)");
                            }
                            return EBodyHitZone.AZone;
                        }

                        if (localPoint.z <= heartL && localPoint.z >= heartR && localPoint.x >= heartTop && localPoint.x <= heartBottom && !isSideHit)
                        {
                            if (Plugin.EnableBallisticsLogging.Value == true)
                            {
                                logger.LogWarning("HEART HIT");
                            }
                            return EBodyHitZone.Heart;
                        }

                        if (hitSpine(localPoint, isSideHit, spineZ, logger))
                        {
                            return EBodyHitZone.Spine;
                        }

                        if (localPoint.z < -dZoneZUpper || localPoint.z > dZoneZUpper)
                        {
                            if (Plugin.EnableBallisticsLogging.Value == true)
                            {
                                logger.LogWarning("D-ZONE HIT: UPPER TORSO");
                            }
                            return EBodyHitZone.DZone;

                        }
                        else if (localPoint.z > -aZoneZUpper && localPoint.z < aZoneZUpper)
                        {
                            if (Plugin.EnableBallisticsLogging.Value == true)
                            {
                                logger.LogWarning("A-ZONE HIT: UPPER TORSO");
                            }
                            return EBodyHitZone.AZone;
                        }
                        else
                        {
                            if (Plugin.EnableBallisticsLogging.Value == true)
                            {
                                logger.LogWarning("C-ZONE HIT: UPPER TORSO");
                            }
                            return EBodyHitZone.CZone;
                        }
                    }

                    if (hitSpine(localPoint, isSideHit, spineZ, logger))
                    {
                        return EBodyHitZone.Spine;
                    }

                    if (hitPart == EBodyPartColliderType.RibcageLow || hitPart == EBodyPartColliderType.SpineDown)
                    {
                        if (localPoint.z < -dZoneZMid || localPoint.z > dZoneZMid)
                        {
                            if (Plugin.EnableBallisticsLogging.Value == true)
                            {
                                logger.LogWarning("D-ZONE HIT: MID TORSO");
                            }
                            return EBodyHitZone.DZone;
                        }
                        else if (localPoint.z > -aZoneZMid && localPoint.z < aZoneZMid && localPoint.x < aZoneXMid)
                        {
                            if (Plugin.EnableBallisticsLogging.Value == true)
                            {
                                logger.LogWarning("A-ZONE HIT: MID TORSO");
                            }
                            return EBodyHitZone.AZone;
                        }
                        else
                        {
                            if (Plugin.EnableBallisticsLogging.Value == true)
                            {
                                logger.LogWarning("C-ZONE HIT: MID TORSO");
                            }
                            return EBodyHitZone.CZone;
                        }
                    }

                    if (Plugin.EnableBallisticsLogging.Value == true)
                    {
                        logger.LogWarning("COULDN'T FIND HIT ZONE");
                    }
                    return EBodyHitZone.Unknown;
                }

                if (hitOrientation == EHitOrientation.TopHit && (hitPart == EBodyPartColliderType.RibcageUp || hitPart == EBodyPartColliderType.SpineTop))
                {
                    if (localPoint.z > -spineZ && localPoint.z < spineZ)
                    {
                        if (Plugin.EnableBallisticsLogging.Value == true)
                        {
                            logger.LogWarning("SPINE: TOP HIT");
                        }
                        return EBodyHitZone.Spine;
                    }
                    if (localPoint.z > -topNeckZ && localPoint.z < topNeckZ) // && localPoint.x < topNeckX
                    {
                        if (Plugin.EnableBallisticsLogging.Value == true)
                        {
                            logger.LogWarning("NECK: TOP HIT (Counting As A-Zone)");
                        }
                        return EBodyHitZone.AZone;
                    }
                    if (Plugin.EnableBallisticsLogging.Value == true)
                    {
                        logger.LogWarning("D-ZONE: TOP SHOULDERS HIT");
                    }
                    return EBodyHitZone.DZone;
                }
            }
            if (Plugin.EnableBallisticsLogging.Value == true)
            {
                logger.LogWarning("COULDN'T FIND HIT ZONE");
            }
            return EBodyHitZone.Unknown;
        }
    }
}
