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
}
