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
        Neck,
        Heart,
        Spine,
        ArmArmor,
        NeckArmor,
        SidePlate,
        ChestPlate,
        StomachArmor,
        AssZone,
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

        private static string[] humanBodyColliders = { HitBox.Head, HitBox.UpperTorso, HitBox.LowerTorso, HitBox.Pelvis, HitBox.LeftThigh, HitBox.RightThigh, HitBox.LeftCalf, HitBox.RightCalf, HitBox.RightUpperArm, HitBox.LeftUpperArm, HitBox.RightForearm, HitBox.LeftForearm };

        public static bool HitValidCollider(string hitCollider)
        {
            foreach (string s in humanBodyColliders)
            {
                if (s == hitCollider)
                {
                    return true;
                }
            }
            return false;
        }

        public static EBodyPart GetBodyPartFromCol(string hitCollider) 
        {
            switch (hitCollider) 
            {
                case HitBox.Head:
                    return EBodyPart.Head;
                case HitBox.UpperTorso:
                    return EBodyPart.Chest;
                case HitBox.LowerTorso:
                    return EBodyPart.Chest;
                case HitBox.Pelvis:
                    return EBodyPart.Stomach;
                case HitBox.RightForearm:
                    return EBodyPart.RightArm;
                case HitBox.LeftForearm:
                    return EBodyPart.LeftArm;
                case HitBox.RightUpperArm:
                    return EBodyPart.RightArm;
                case HitBox.LeftUpperArm:
                    return EBodyPart.LeftArm;
                case HitBox.LeftThigh:
                    return EBodyPart.LeftLeg;
                case HitBox.RightThigh:
                    return EBodyPart.RightLeg;
                case HitBox.LeftCalf:
                    return EBodyPart.LeftLeg;
                case HitBox.RightCalf:
                    return EBodyPart.RightLeg;
                default: return EBodyPart.Chest;
            }
        }
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

        public static EBodyHitZone GetHitBodyZone(ManualLogSource logger, string hitPart, Vector3 localPoint, EHitOrientation hitOrientation)
        {
            bool isSideHit = hitOrientation == EHitOrientation.LeftSideHit || hitOrientation == EHitOrientation.RightSideHit;

            if (hitPart == HitBox.UpperTorso || hitPart == HitBox.LowerTorso || hitPart == HitBox.Pelvis)
            {
                float spineZ = 0.01f;
                float heartL = 0.03f;
                float heartR = -0.015f;
                float heartTop = hitOrientation == EHitOrientation.BackHit ? -0.0435f : -0.063f;
                float heartBottom = hitOrientation == EHitOrientation.BackHit ? -0.028f : -0.05f;

                float dZoneZUpper = hitOrientation == EHitOrientation.BackHit ? 0.12f : 0.125f;
                float dZoneZMid = hitOrientation == EHitOrientation.BackHit ? 0.12f : 0.125f;
                float dZoneZLower = hitOrientation == EHitOrientation.BackHit ? 0.15f : 0.125f;
                float dZoneXLower = -0.16f;

                float aZoneZUpper = 0.045f;
                float aZoneZMid = 0.045f;
                float aZoneXMid = -0.17f;

                float topNeckZ = 0.04f;
                float topNeckX = -0.25f;

                float rearNeckZ = 0.05f;
                float rearNeckX = -0.2f;

                if (hitOrientation != EHitOrientation.TopHit && hitOrientation != EHitOrientation.BottomHit)
                {
                    if (hitPart == HitBox.UpperTorso)
                    {
                        if (hitOrientation == EHitOrientation.BackHit && localPoint.z > -rearNeckZ && localPoint.z < rearNeckZ && localPoint.x < rearNeckX)
                        {
                            if (Plugin.EnableBallisticsLogging.Value == true)
                            {
                                logger.LogWarning("NECK: Front HIT");
                            }
                            return EBodyHitZone.Neck;
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

                    if (hitPart == HitBox.LowerTorso)
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
                    if (hitPart == HitBox.Pelvis)
                    {
                        if (localPoint.z >= -dZoneZLower && localPoint.z <= dZoneZLower && localPoint.x <= dZoneXLower)
                        {
                            if (Plugin.EnableBallisticsLogging.Value == true)
                            {
                                logger.LogWarning("C-ZONE HIT: Stomach");
                            }
                            return EBodyHitZone.CZone;
                        }
                        else
                        {
                            if (Plugin.EnableBallisticsLogging.Value == true)
                            {
                                logger.LogWarning("D-ZONE HIT: Stomach");
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
                if (hitOrientation == EHitOrientation.TopHit && hitPart == HitBox.UpperTorso)
                {
                    if (localPoint.z > -topNeckZ && localPoint.z < topNeckZ && localPoint.x < topNeckX)
                    {
                        if (Plugin.EnableBallisticsLogging.Value == true)
                        {
                            logger.LogWarning("NECK: TOP HIT");
                        }
                        return EBodyHitZone.Neck;
                    }
                    if (localPoint.z > -spineZ && localPoint.z < spineZ)
                    {
                        if (Plugin.EnableBallisticsLogging.Value == true)
                        {
                            logger.LogWarning("SPINE: TOP HIT");
                        }
                        return EBodyHitZone.Spine;
                    }
                    if (localPoint.z > -topNeckZ && localPoint.z < topNeckZ && localPoint.x > topNeckX)
                    {
                        if (Plugin.EnableBallisticsLogging.Value == true)
                        {
                            logger.LogWarning("A-ZONE: TOP HIT");
                        }
                        return EBodyHitZone.AZone;
                    }
                    if (Plugin.EnableBallisticsLogging.Value == true)
                    {
                        logger.LogWarning("D-ZONE: TOP SHOULDERS HIT");
                    }
                    return EBodyHitZone.DZone;
                }
                if (hitOrientation == EHitOrientation.BottomHit && hitPart == HitBox.Pelvis)
                {
                    if (Plugin.EnableBallisticsLogging.Value == true)
                    {
                        logger.LogWarning("ARSE HIT");
                    }
                    return EBodyHitZone.AssZone;
                }
            }
            if (Plugin.EnableBallisticsLogging.Value == true)
            {
                logger.LogWarning("COULDN'T FIND HIT ZONE");
            }
            return EBodyHitZone.Unknown;
        }

        public static void GetHitArmorZone(ManualLogSource logger, ArmorComponent ac, string hitPart, Vector3 localPoint, EHitOrientation hitOrientation, bool hasSideArmor, bool hasStomachArmor, bool hasNeckArmor, bool hasExtraArmor, bool shouldHaveExtraSides, bool hasArmArmor, ref bool hasBypassedArmor, ref bool hitSecondaryArmor)
        {   
            float topOfPlate = hitOrientation == EHitOrientation.BackHit ? -0.166f : -0.24f;
            float bottomOfPlate = hitOrientation == EHitOrientation.BackHit || hitOrientation == EHitOrientation.LeftSideHit || hitOrientation == EHitOrientation.RightSideHit ? -0.21f : -0.13f;
            float upperSides = hitOrientation == EHitOrientation.BackHit ? 0.13f : 0.13f;
            float midSides = hitOrientation == EHitOrientation.BackHit ? 0.137f : 0.14f;
            float lowerSides = hitOrientation == EHitOrientation.BackHit ? 0.145f : 0.15f;
            float topOfSidePlates = hitOrientation == EHitOrientation.BackHit ? -0.14f : -0.145f;
            float bottomOfSidePlates = hitOrientation == EHitOrientation.BackHit ? -0.22f : -0.175f;
            float sidesOfStomachArmor = hitOrientation == EHitOrientation.BackHit ? 0.11f : 0.12f;
            float bottomOfStomachArmorRear = -0.13f;

            if (hasArmArmor)
            {
                if ((hitPart == HitBox.RightUpperArm || hitPart == HitBox.LeftUpperArm))
                {
                    if (Plugin.EnableBallisticsLogging.Value == true)
                    {
                        logger.LogWarning("ARM ARMOR HIT");
                    }
                    hitSecondaryArmor = true;
                    return;
                }
                if ((hitPart == HitBox.RightForearm || hitPart == HitBox.LeftForearm))
                {
                    if (Plugin.EnableBallisticsLogging.Value == true)
                    {
                        logger.LogWarning("ARM ARMOR BYPASSED");
                    }
                    hasBypassedArmor = true;
                    return;
                }
            }
            else if (hitPart == HitBox.RightUpperArm || hitPart == HitBox.LeftUpperArm || hitPart == HitBox.RightForearm || hitPart == HitBox.LeftForearm)
            {
                if (Plugin.EnableBallisticsLogging.Value == true)
                {
                    logger.LogWarning("ARM HIT DURING ARMROR CHECK WITH NO ARM ARMOR");
                }
                hasBypassedArmor = true;
                return;
            }

            if (hitOrientation != EHitOrientation.TopHit && hitOrientation != EHitOrientation.BottomHit)
            {
                if (hitPart == HitBox.UpperTorso)
                {
                    if (localPoint.x < topOfPlate)
                    {
                        if (hasNeckArmor || hasExtraArmor)
                        {
                            if (Plugin.EnableBallisticsLogging.Value == true)
                            {
                                logger.LogWarning("NECK/SECONDARY ARMOR HIT");
                            }
                            hitSecondaryArmor = true;
                            return;
                        }
                        else
                        {
                            if (Plugin.EnableBallisticsLogging.Value == true)
                            {
                                logger.LogWarning("ARMOR BYPASSED: NECK/TOP OF PLATE");
                            }
                            hasBypassedArmor = true;
                            return;
                        }
                    }

                    if (hasExtraArmor && (localPoint.z > -0.142f && localPoint.z < 0.142f && (localPoint.z < -upperSides || localPoint.z > upperSides)))
                    {
                        if (Plugin.EnableBallisticsLogging.Value == true)
                        {
                            logger.LogWarning("SECONDARY ARMOR HIT: UPPER SIDES");
                        }
                        hitSecondaryArmor = true;
                        return;
                    }

                    if (localPoint.z < -upperSides || localPoint.z > upperSides)
                    {

                        if (Plugin.EnableBallisticsLogging.Value == true)
                        {
                            logger.LogWarning("ARMOR BYPASSED: UPPER SIDES");
                        }
                        hasBypassedArmor = true;
                        return;
                    }
                }

                if (hitPart == HitBox.LowerTorso)
                {
                    if (!hasSideArmor && !hasExtraArmor)
                    {
                        if (localPoint.z < -midSides || localPoint.z > midSides)
                        {
                            if (Plugin.EnableBallisticsLogging.Value == true)
                            {
                                logger.LogWarning("ARMOR BYPASSED: LOWER SIDES");
                            }
                            hasBypassedArmor = true;
                            return;
                        }
                    }
                    else
                    {
                        if ((hasExtraArmor && shouldHaveExtraSides) && !hasSideArmor && localPoint.x > topOfSidePlates && (localPoint.z < -midSides || localPoint.z > midSides))
                        {
                            if (Plugin.EnableBallisticsLogging.Value == true)
                            {
                                logger.LogWarning("SECONDARY ARMOR HIT: LOWER SIDES");
                            }
                            hitSecondaryArmor = true;
                            return;
                        }
                        else if (hasSideArmor && localPoint.x < topOfSidePlates && (localPoint.z < -midSides || localPoint.z > midSides))
                        {
                            if (Plugin.EnableBallisticsLogging.Value == true)
                            {
                                logger.LogWarning("ARMOR BYPASSED: ABOVE SIDE ARMOR PLATE");
                            }
                            hasBypassedArmor = true;
                            return;
                        }
                    }
                }

                if (hitPart == HitBox.Pelvis && (hitOrientation == EHitOrientation.FrontHit || hitOrientation == EHitOrientation.BackHit))
                {
                    if (!hasStomachArmor && !hasExtraArmor)
                    {
                        if (localPoint.x > bottomOfPlate)
                        {
                            if (Plugin.EnableBallisticsLogging.Value == true)
                            {
                                logger.LogWarning("ARMOR BYPASSED: BELOW PLATE, STOMACH");
                            }
                            hasBypassedArmor = true;
                            return;
                        }
                    }
                    else
                    {
                        if ((hasStomachArmor || hasExtraArmor) && localPoint.z > -sidesOfStomachArmor && localPoint.z < sidesOfStomachArmor && ((hitOrientation == EHitOrientation.BackHit && localPoint.x < bottomOfStomachArmorRear && localPoint.x > bottomOfPlate) || (hasStomachArmor && hitOrientation == EHitOrientation.FrontHit && localPoint.x > bottomOfPlate)))
                        {
                            if (Plugin.EnableBallisticsLogging.Value == true)
                            {
                                logger.LogWarning("STOMACH/SECONDARY ARMOR HIT STOMACH");
                            }
                            hitSecondaryArmor = true;
                            return;
                        }
                        else if (localPoint.x > bottomOfPlate)
                        {
                            if (Plugin.EnableBallisticsLogging.Value == true)
                            {
                                logger.LogWarning("ARMOR BYPASSED: STOMACH ARMOR SIDES OR BELLOW PLATE");
                            }
                            hasBypassedArmor = true;
                            return;
                        }
                    }
                }
            }

            if (hitPart == HitBox.Pelvis && (hitOrientation == EHitOrientation.LeftSideHit || hitOrientation == EHitOrientation.RightSideHit))
            {
                if (!hasSideArmor && !hasExtraArmor)
                {
                    if (Plugin.EnableBallisticsLogging.Value == true)
                    {
                        logger.LogWarning("ARMOR BYPASSED: STOMACH SIDES");
                    }
                    hasBypassedArmor = true;
                    return;
                }
                else
                {
                    if (localPoint.x > bottomOfPlate)
                    {
                        if (Plugin.EnableBallisticsLogging.Value == true)
                        {
                            logger.LogWarning("SIDE ARMOR BYPASSED: BELOW SIDE ARMOR PLATE");
                        }
                        hasBypassedArmor = true;
                        return;
                    }
                    else if ((hasExtraArmor && shouldHaveExtraSides) && !hasSideArmor)  
                    {
                        if (Plugin.EnableBallisticsLogging.Value == true)
                        {
                            logger.LogWarning("SECONDARY ARMOR HIT: UPPER SIDES");
                        }
                        hitSecondaryArmor = true;
                        return; 
                    }
                }
            }

            if (hitOrientation == EHitOrientation.TopHit && hitPart == HitBox.UpperTorso)
            {
                float neckArmorTopZ = 0.075f;

                if (!hasNeckArmor && !hasExtraArmor)
                {
                    if (Plugin.EnableBallisticsLogging.Value == true)
                    {
                        logger.LogWarning("ARMOR BYPASSED: TOP HIT");
                    }
                    hasBypassedArmor = true;
                    return;
                }
                else
                {
                    if (hasNeckArmor && localPoint.z > -neckArmorTopZ && localPoint.z < neckArmorTopZ)
                    {
                        if (Plugin.EnableBallisticsLogging.Value == true)
                        {
                            logger.LogWarning("NECK ARMOR TOP HIT");
                        }
                        hitSecondaryArmor = true;
                        return;
                    }
                    else
                    {
                        if (hasExtraArmor && (localPoint.z < -neckArmorTopZ || localPoint.z > neckArmorTopZ)) 
                        {
                            if (Plugin.EnableBallisticsLogging.Value == true)
                            {
                                logger.LogWarning("SECONDARY ARMOR TOP HIT");
                            }
                            hitSecondaryArmor = true;
                            return;
                        }

                        if (Plugin.EnableBallisticsLogging.Value == true)
                        {
                            logger.LogWarning("NECK/SECONDARY ARMOR BYPASSED TOP HIT");
                        }
                        hasBypassedArmor = true;
                        return;
                    }
                }

            }
            if (hitOrientation == EHitOrientation.BottomHit && hitPart == HitBox.Pelvis && !hasStomachArmor && !(hasExtraArmor && shouldHaveExtraSides))
            {
                if (Plugin.EnableBallisticsLogging.Value == true)
                {
                    logger.LogWarning("ARMOR BYPASSED: ARSE");
                }
                hasBypassedArmor = true;
            }
        }
    }
}
