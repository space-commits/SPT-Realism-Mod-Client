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
using UnityEngine.Rendering.PostProcessing;
using EFT.Quests;
using System.IO;
using static EFT.Interactive.BetterPropagationGroups;
using HarmonyLib.Tools;
using System.Collections;
using EFT.Interactive;
using static System.Net.Mime.MediaTypeNames;
using static EFT.Player;

namespace RealismMod
{
    public class IsShotDeflectedByHeavyArmorPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player).GetMethod("IsShotDeflectedByHeavyArmor", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static bool Prefix(bool __result)
        {
            __result = false;
            return false;  
        }
    }

    public class IsPenetratedPatch : ModulePatch 
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(EFT.Ballistics.BallisticCollider).GetMethod("IsPenetrated", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static void Prefix(EFT.Ballistics.BallisticCollider __instance, GClass2624 shot, Vector3 hitPoint)
        {
            if (__instance.name == HitBox.LeftUpperArm || __instance.name == HitBox.RightUpperArm || __instance.name == HitBox.LeftForearm || __instance.name == HitBox.RightForearm )
            {
                __instance.PenetrationLevel = 10f;
                __instance.PenetrationChance = 0.5f;
            } 
        }
    }

    public class DamageInfoPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(DamageInfo).GetConstructor(new Type[] { typeof(EDamageType), typeof(GClass2624) });
        }

        private static int playCounter = 0;

        private static void modifyDamageByHitZone(string hitPart, EBodyHitZone hitZone, ref DamageInfo di) 
        {
            bool hitCalf = hitPart == HitBox.LeftCalf || hitPart == HitBox.RightCalf ? true : false;
            bool hitThigh = hitPart == HitBox.LeftThigh || hitPart == HitBox.RightThigh ? true : false;
            bool hitUpperArm = hitPart == HitBox.LeftUpperArm || hitPart == HitBox.RightUpperArm ? true : false;
            bool hitForearm = hitPart == HitBox.LeftForearm || hitPart == HitBox.RightForearm ? true : false;

            if (hitCalf == true)
            {
                di.Damage *= HitZoneModifiers.Calf;
                di.HeavyBleedingDelta *= 0.5f;
                return;
            }
            if (hitThigh == true)
            {
                di.Damage *= HitZoneModifiers.Thigh;
                di.HeavyBleedingDelta *= 1.25f;
                return;
            }
            if (hitForearm == true)
            {
                di.Damage *= HitZoneModifiers.Forearm;
                di.HeavyBleedingDelta *= 0.5f;
                return;
            }
            if (hitUpperArm == true)
            {
                di.Damage *= HitZoneModifiers.UpperArm;
                di.HeavyBleedingDelta *= 0.8f;
                return;
            }
            if (hitZone == EBodyHitZone.AZone) 
            {
                di.Damage *= HitZoneModifiers.AZone;
                di.HeavyBleedingDelta *= 1.5f;
                return;
            }
            if (hitZone == EBodyHitZone.CZone)
            {
                di.Damage *= HitZoneModifiers.CZone;
                return;
            }
            if (hitZone == EBodyHitZone.DZone)
            {
                di.Damage *= HitZoneModifiers.DZone;
                di.HeavyBleedingDelta *= 0.5f;
                return;
            }
            if (hitZone == EBodyHitZone.Neck)
            {
                di.Damage += HitZoneModifiers.Neck;
                di.HeavyBleedingDelta *= 1.5f;
                return;
            }
            if (hitZone == EBodyHitZone.Heart)
            {
                di.Damage += HitZoneModifiers.Heart;
                di.HeavyBleedingDelta *= 1.5f;
                return;
            }
            if (hitZone == EBodyHitZone.Spine)
            {
                di.Damage += HitZoneModifiers.Spine;
                di.HeavyBleedingDelta *= 1.25f;
                return;
            }
        }

        private static void playBodyHitSound(EBodyHitZone hitZone, Vector3 pos, string hitBox) 
        {
            float dist = CameraClass.Instance.Distance(pos);
            float volClose = 2.5f * Plugin.FleshHitSoundMulti.Value;
            float volDist = 4.3f * Plugin.FleshHitSoundMulti.Value;

            if (hitZone == EBodyHitZone.Spine)
            {
                Singleton<BetterAudio>.Instance.PlayAtPoint(pos, Plugin.LoadedAudioClips["spine.wav"], dist, BetterAudio.AudioSourceGroupType.Distant, 100, volClose, EOcclusionTest.Continuous);
                return;
            }
            if (hitBox == HitBox.Head) 
            {
                Singleton<BetterAudio>.Instance.PlayAtPoint(pos, Plugin.LoadedAudioClips["headshot.wav"], dist, BetterAudio.AudioSourceGroupType.Distant, 100, volClose, EOcclusionTest.Continuous);
                return;
            }
            if (hitZone == EBodyHitZone.Heart)
            {
                Singleton<BetterAudio>.Instance.PlayAtPoint(pos, Plugin.LoadedAudioClips["heart.wav"], dist, BetterAudio.AudioSourceGroupType.Distant, 100, volClose, EOcclusionTest.Continuous);
                return;
            }
            if (hitZone == EBodyHitZone.AssZone)
            {
                Singleton<BetterAudio>.Instance.PlayAtPoint(pos, Plugin.LoadedAudioClips["ass_impact.wav"], dist, BetterAudio.AudioSourceGroupType.Distant, 100, 0.5f, EOcclusionTest.Continuous);
                return;
            }

            string audioClip = "flesh_dist_1.wav";
            if (dist >= 40)
            {
                audioClip = playCounter == 0 ? "flesh_dist_1.wav" : playCounter == 1 ? "flesh_dist_2.wav" : "flesh_dist_2.wav";
            }
            else
            {
                audioClip = playCounter == 0 ? "flesh_1.wav" : playCounter == 1 ? "flesh_2.wav" : "flesh_3.wav";
            }

            Singleton<BetterAudio>.Instance.PlayAtPoint(pos, Plugin.LoadedAudioClips[audioClip], dist, BetterAudio.AudioSourceGroupType.Distant, 200, dist >= 40 ? volDist : volClose, EOcclusionTest.Continuous);
        }

        [PatchPrefix]
        private static bool Prefix(ref DamageInfo __instance, EDamageType damageType, GClass2624 shot)
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

            if (Plugin.EnableBodyHitZones.Value && !__instance.Blunt && __instance.DamageType == EDamageType.Bullet) 
            {
                string hitCollider = shot.HittedBallisticCollider.name;
                if (HitBox.HitValidCollider(hitCollider))
                {
                    Collider col = shot.HitCollider;
                    Vector3 localPoint = col.transform.InverseTransformPoint(shot.HitPoint);
   /*                 Vector3 normalizedPoint = localPoint.normalized;*/
                    Vector3 hitNormal = shot.HitNormal;
                    EHitOrientation hitOrientation = BallisticsController.GetHitOrientation(hitNormal, col.transform, Logger);
                    EBodyHitZone hitZone = BallisticsController.GetHitBodyZone(Logger, hitCollider, localPoint, hitOrientation);
                    modifyDamageByHitZone(hitCollider, hitZone, ref __instance);

                    if (Plugin.EnableHitSounds.Value) 
                    {
                        playBodyHitSound(hitZone, col.transform.position, hitCollider);
                        playCounter++;
                        playCounter = playCounter > 2 ? 0 : playCounter;
                    }

/*                    if (!shot.Player.IsYourPlayer)
                    {
                        Logger.LogWarning("=========Player Hit Damage Info==========");
                        Logger.LogWarning("ammo name = " + shot.Ammo.LocalizedName());
                        Logger.LogWarning("ammo id = " + shot.Ammo.TemplateId);
                        Logger.LogWarning("hit collider = " + hitCollider);
                        Logger.LogWarning("hit orientation = " + hitOrientation);
                        Logger.LogWarning("hit zone = " + hitZone);
                        Logger.LogWarning("damage = " + __instance.Damage);
                        Logger.LogWarning("x = " + localPoint.x);
                        Logger.LogWarning("y = " + localPoint.y);
                        Logger.LogWarning("z = " + localPoint.z);
                        Logger.LogWarning("===================");
                    }
                    else
                    {
                        Logger.LogWarning("-------------Bot Hit Damage Info---------------");
                        Logger.LogWarning("ammo name = " + shot.Ammo.LocalizedName());
                        Logger.LogWarning("ammo id = " + shot.Ammo.TemplateId);
                        Logger.LogWarning("hit collider = " + hitCollider);
                        Logger.LogWarning("hit orientation = " + hitOrientation);
                        Logger.LogWarning("hit zone = " + hitZone);
                        Logger.LogWarning("damage = " + __instance.Damage);
                        Logger.LogWarning("x = " + localPoint.x);
                        Logger.LogWarning("y = " + localPoint.y);
                        Logger.LogWarning("z = " + localPoint.z);
                        Logger.LogWarning("------------------");
                    }*/

                    if (Plugin.EnableBallisticsLogging.Value)
                    {
                        Logger.LogWarning("=========Damage Info==========");
                        Logger.LogWarning("hit collider = " + hitCollider);
                        Logger.LogWarning("hit orientation = " + hitOrientation);
                        Logger.LogWarning("hit zone = " + hitZone);
                        Logger.LogWarning("damage = " + __instance.Damage);
                        Logger.LogWarning("x = " + localPoint.x);
                        Logger.LogWarning("y = " + localPoint.y);
                        Logger.LogWarning("z = " + localPoint.z);
                        Logger.LogWarning("===================");
                    }
                }
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

        private static float armorClass;
        private static float currentDura;
        private static float maxDura;

        private static List<EBodyPart> bodyParts = new List<EBodyPart> { EBodyPart.RightArm, EBodyPart.LeftArm, EBodyPart.LeftLeg, EBodyPart.RightLeg, EBodyPart.Head, EBodyPart.Common, EBodyPart.Common };
        
        private static System.Random randNum = new System.Random();

        private static List<ArmorComponent> preAllocatedArmorComponents = new List<ArmorComponent>(10);

        private static void SetArmorStats(ArmorComponent armor)
        {
            armorClass = armor.ArmorClass * 10f;
            currentDura = armor.Repairable.Durability;
            maxDura = armor.Repairable.TemplateDurability;
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

        private static void TryDoDisarm(Player player, float kineticEnergy, bool hitArmArmor, bool forearm) 
        {
            Player.ItemHandsController itemHandsController = player.HandsController as Player.ItemHandsController;
            if (itemHandsController != null && itemHandsController.CurrentCompassState)
            {
                itemHandsController.SetCompassState(false);
                return;
            }

            if (player.MovementContext.StationaryWeapon == null && !player.HandsController.IsPlacingBeacon() && !player.HandsController.IsInInteractionStrictCheck() && player.CurrentStateName != EPlayerState.BreachDoor && !player.IsSprintEnabled) 
            {
                float rndNumber = UnityEngine.Random.Range(0, 101);
                float kineticEnergyFactor = 1f + (kineticEnergy / 1000f);
                float hitArmArmorFactor = hitArmArmor ? 0.5f : 1f;
                float hitLocationModifier = forearm ? 1.5f : 1f;
                float totalChance = Mathf.Round(Plugin.DisarmBaseChance.Value * kineticEnergyFactor * hitArmArmorFactor * hitLocationModifier);

                if (rndNumber <= totalChance)
                {
                    InventoryControllerClass inventoryController = (InventoryControllerClass)AccessTools.Field(typeof(Player), "_inventoryController").GetValue(player);
                    if (player.HandsController as Player.FirearmController != null) 
                    {
                        Player.FirearmController fc = player.HandsController as Player.FirearmController;
                        if (fc.Item != null && inventoryController.CanThrow(fc.Item))
                        {
                            inventoryController.TryThrowItem(fc.Item, null, false);
                        }
                    }
                }
            }
        }

        private static void TryDoFalldown(Player player, float kineticEnergy, bool calf)
        {
            float rndNumber = UnityEngine.Random.Range(0, 101);
            float kineticEnergyFactor = 1f + (kineticEnergy / 1000f);
            float hitLocationModifier = calf ? 2f : 1f;
            float totalChance = Mathf.Round(Plugin.FallBaseChance.Value * kineticEnergyFactor * hitLocationModifier);

            if (rndNumber <= totalChance)
            {
                player.ToggleProne();
                TryDoDisarm(player, kineticEnergy * 0.25f, false, false);
            }
        }

        [PatchPrefix]
        private static void Prefix(Player __instance, DamageInfo damageInfo, EBodyPart bodyPartType, float absorbed, EHeadSegment? headSegment = null)
        {
            if (damageInfo.DamageType == EDamageType.Fall && damageInfo.Damage >= 15f) 
            {
                __instance.ToggleProne();
                TryDoDisarm(__instance, damageInfo.Damage * 50f, false, false);
            }

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
        /*        Vector3 normalizedPoint = localPoint.normalized;*/
                Vector3 hitNormal = damageInfo.HitNormal;
                string hitPart = damageInfo.HittedBallisticCollider.name;
                bool hitCalf = hitPart == HitBox.LeftCalf || hitPart == HitBox.RightCalf ? true : false;
                bool hitThigh = hitPart == HitBox.LeftThigh || hitPart == HitBox.RightThigh ? true : false;
                bool hitUpperArm = hitPart == HitBox.LeftUpperArm || hitPart == HitBox.RightUpperArm ? true : false;
                bool hitForearm = hitPart == HitBox.LeftForearm || hitPart == HitBox.RightForearm ? true : false;

                EHitOrientation hitOrientation = EHitOrientation.UnknownOrientation;

                if (hitPart == HitBox.UpperTorso || hitPart == HitBox.LowerTorso || hitPart == HitBox.Pelvis)
                {
                    hitOrientation = BallisticsController.GetHitOrientation(hitNormal, col.transform, Logger);
                }

                if (Plugin.EnableBallisticsLogging.Value == true)
                {
                    EBodyHitZone hitZone = BallisticsController.GetHitBodyZone(Logger, hitPart, localPoint, hitOrientation);

                    Logger.LogWarning("=============Apply Damage Info============");
                    Logger.LogWarning("hit part = " + hitPart);
                    Logger.LogWarning("hit orientation = " + hitOrientation);
                    Logger.LogWarning("hit zone = " + hitZone);
                    Logger.LogWarning("x = " + localPoint.x);
                    Logger.LogWarning("y = " + localPoint.y);
                    Logger.LogWarning("z = " + localPoint.z);
                    Logger.LogWarning("damage = " + damageInfo.Damage);
                    Logger.LogWarning("damage type = " + damageInfo.DamageType);
                    Logger.LogWarning("=========================");
                }

                bool hasArmArmor = false;
                AmmoTemplate ammoTemp = (AmmoTemplate)Singleton<ItemFactory>.Instance.ItemTemplates[damageInfo.SourceId];
                BulletClass ammo = new BulletClass("newAmmo", ammoTemp);
                float KE = ((0.5f * ammo.BulletMassGram * damageInfo.ArmorDamage * damageInfo.ArmorDamage) / 1000f);

                if (armor != null)
                {
                    bool hitSecondaryArmor = false;
                    bool hasBypassedArmor = false;
                    bool hasSideArmor = GearProperties.HasSideArmor(armor.Item);
                    bool hasStomachArmor = GearProperties.HasStomachArmor(armor.Item);
                    bool hasNeckArmor = GearProperties.HasNeckArmor(armor.Item);
                    hasArmArmor = armor.Template.ArmorZone.Contains(EBodyPart.LeftArm) || armor.Template.ArmorZone.Contains(EBodyPart.RightArm);

                    if (hitPart == HitBox.UpperTorso || hitPart == HitBox.LowerTorso || hitPart == HitBox.Pelvis || hitPart == HitBox.LeftForearm || hitPart == HitBox.RightForearm || hitPart == HitBox.LeftUpperArm || hitPart == HitBox.RightUpperArm)
                    {
                        BallisticsController.GetHitArmorZone(Logger, armor, hitPart, localPoint, hitOrientation, hasSideArmor, hasStomachArmor, hasNeckArmor, ref hasBypassedArmor, ref hitSecondaryArmor);
                    }

                    if (Plugin.EnableBallisticsLogging.Value && __instance.IsYourPlayer)
                    {
                        Logger.LogWarning("==============Apply Damage Info=============");
                        Logger.LogWarning("--hit armored hitbox--");
                        Logger.LogWarning("hit part = " + hitPart);
                        Logger.LogWarning("hit orientation = " + hitOrientation);
                        Logger.LogWarning("hit Secondary Armor = " + hitSecondaryArmor);
                        Logger.LogWarning("has Bypassed Armor = " + hasBypassedArmor);
                        Logger.LogWarning("x = " + localPoint.x);
                        Logger.LogWarning("y = " + localPoint.y);
                        Logger.LogWarning("z = " + localPoint.z);
                        Logger.LogWarning("=========================");
                    }

                    if (damageInfo.Blunt && GearProperties.CanSpall(armor.Item) && (hitPart == HitBox.UpperTorso || hitPart == HitBox.LowerTorso) && !hasBypassedArmor && !hitSecondaryArmor)
                    {
                        SetArmorStats(armor);
                        damageInfo.BleedBlock = false;
                        bool isMetalArmor = armor.Template.ArmorMaterial == EArmorMaterial.ArmoredSteel || armor.Template.ArmorMaterial == EArmorMaterial.Titan ? true : false;
                        float bluntDamage = damageInfo.Damage;
                        float speedFactor = damageInfo.ArmorDamage / ammo.GetBulletSpeed;
                        float fragChance = ammo.FragmentationChance * speedFactor;
                        float lightBleedChance = damageInfo.LightBleedingDelta;
                        float heavyBleedChance = damageInfo.HeavyBleedingDelta;
                        float ricochetChance = ammo.RicochetChance * speedFactor;
                        float spallReduction = GearProperties.SpallReduction(armor.Item);
                        float armorDamageActual = ammo.ArmorDamage * speedFactor;
                        float penPower = damageInfo.PenetrationPower;

                        float duraPercent = currentDura / maxDura;
                        float armorFactor = armorClass * (Mathf.Min(1f, duraPercent * 2f));
                        float penDuraFactoredClass = Mathf.Max(1f, armorFactor - (penPower / 1.8f));
                        float penFactoredClass = Mathf.Max(1f, armorClass - (penPower / 1.8f));
                        float maxPotentialDuraDamage = KE / penDuraFactoredClass;
                        float maxPotentialBluntDamage = KE / penFactoredClass;

                        float maxSpallingDamage = isMetalArmor ? maxPotentialBluntDamage - bluntDamage : maxPotentialDuraDamage - bluntDamage;
                        float factoredSpallingDamage = maxSpallingDamage * (fragChance + 1) * (ricochetChance + 1) * spallReduction * (isMetalArmor ? (1f - duraPercent) + 1f : 1f);

                        int rnd = Math.Max(1, randNum.Next(bodyParts.Count));
                        float splitSpallingDmg = factoredSpallingDamage / bodyParts.Count;


                        if (Plugin.EnableBallisticsLogging.Value)
                        {
                            Logger.LogWarning("===========SPALLING=============== ");
                            Logger.LogWarning("Spall Reduction " + spallReduction);
                            Logger.LogWarning("Dura Percent " + duraPercent);
                            Logger.LogWarning("Max Dura Factored Damage " + maxPotentialDuraDamage);
                            Logger.LogWarning("Max Blunt Damage " + maxPotentialBluntDamage);
                            Logger.LogWarning("Max Spalling Damage " + maxSpallingDamage);
                            Logger.LogWarning("Factored Spalling Damage " + factoredSpallingDamage);
                            Logger.LogWarning("Split Spalling Dmg " + splitSpallingDmg);
                        }

                        foreach (EBodyPart part in bodyParts.OrderBy(x => randNum.Next()).Take(rnd))
                        {

                            if (part == EBodyPart.Common)
                            {
                                return;
                            }

                            float damage = splitSpallingDmg;
                            float bleedFactor = GetBleedFactor(part);

                            if (part == EBodyPart.Head && (hitOrientation == EHitOrientation.LeftSideHit || hitOrientation == EHitOrientation.RightSideHit))
                            {
                                continue;
                            }
                            else if (part == EBodyPart.Head) 
                            {
                                damage = hasNeckArmor == true ? Mathf.Min(10, splitSpallingDmg * 0.5f) : Mathf.Min(10, splitSpallingDmg);
                                bleedFactor = hasNeckArmor == true ? 0f : bleedFactor;
                            }

                            if ((part == EBodyPart.LeftArm || part == EBodyPart.RightArm) && hasArmArmor)
                            {
                                damage *= 0.5f;
                                bleedFactor *= 0.5f;
                            }

                            if (Plugin.EnableBallisticsLogging.Value)
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

                BodyPartCollider bpc = damageInfo.HittedBallisticCollider as BodyPartCollider;
                float hitPartHP = __instance.ActiveHealthController.GetBodyPartHealth(bpc.BodyPartType).Current;
                float toBeHP = hitPartHP - damageInfo.Damage;


                if ((hitUpperArm || hitForearm) && (toBeHP <= 0f) && ((!__instance.IsYourPlayer && Plugin.CanDisarmBot.Value) || (__instance.IsYourPlayer && Plugin.CanDisarmPlayer.Value)))
                {
                    TryDoDisarm(__instance, KE, hasArmArmor, hitForearm);
                }

                if ((hitCalf || hitThigh) && (toBeHP <= 0f) && ((!__instance.IsYourPlayer && Plugin.CanFellBot.Value) || (__instance.IsYourPlayer && Plugin.CanFellPlayer.Value))) 
                {
                    TryDoFalldown(__instance, KE, hitCalf);    
                }

            }
        }
    }


    public class SetPenetrationStatusPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(ArmorComponent).GetMethod(nameof(ArmorComponent.SetPenetrationStatus), BindingFlags.Public | BindingFlags.Instance);
        }

        private static int playCounter = 0;

        private static void playArmorHitSound(EArmorMaterial mat, Vector3 pos, bool isHelm)
        {
            float dist = CameraClass.Instance.Distance(pos);
            float volClose = 4.65f * Plugin.ArmorCloseHitSoundMulti.Value;
            float volDist = 5.5f * Plugin.ArmorFarHitSoundMulti.Value;

            if (mat == EArmorMaterial.Aramid)
            {
                string audioClip = "aramid_1.wav";
                if (dist >= 40)
                {
                    audioClip = playCounter == 0 ? "impact_dist_1.wav" : playCounter == 1 ? "impact_dist_2.wav" : "impact_dist_3.wav";
                }
                else
                {
                    if (!isHelm)
                    {
                        audioClip = playCounter == 0 ? "aramid_1.wav" : playCounter == 1 ? "aramid_2.wav" : "aramid_3.wav";
                    }
                    else 
                    {
                        audioClip = playCounter == 0 ? "uhmwpe_1.wav" : playCounter == 1 ? "uhmwpe_2.wav" : "uhmwpe_3.wav";
                    }
                }

                Singleton<BetterAudio>.Instance.PlayAtPoint(pos, Plugin.LoadedAudioClips[audioClip], dist, BetterAudio.AudioSourceGroupType.Distant, 200, dist >= 40 ? volDist : volClose, EOcclusionTest.Continuous);
            }
            else if (mat == EArmorMaterial.Ceramic)
            {
                string audioClip = "ceramic_1.wav";
                if (dist >= 40) 
                {
                    audioClip = playCounter == 0 ? "ceramic_dist_1.wav" : playCounter == 1 ? "ceramic_dist_2.wav" : "ceramic_dist_3.wav";
                }
                else 
                {
                    audioClip = playCounter == 0 ? "ceramic_1.wav" : playCounter == 1 ? "ceramic_2.wav" : "ceramic_3.wav";
                }

                Singleton<BetterAudio>.Instance.PlayAtPoint(pos, Plugin.LoadedAudioClips[audioClip], dist, BetterAudio.AudioSourceGroupType.Distant, 200, dist >= 40 ? volDist : volClose, EOcclusionTest.Continuous);
            }
            else if (mat == EArmorMaterial.UHMWPE || mat == EArmorMaterial.Combined)
            {
                string audioClip = "uhmwpe_1.wav";
                if (dist >= 40)
                {
                    audioClip = playCounter == 0 ? "impact_dist_1.wav" : playCounter == 1 ? "impact_dist_2.wav" : "impact_dist_4.wav";
                }
                else
                {
                    audioClip = playCounter == 0 ? "uhmwpe_1.wav" : playCounter == 1 ? "uhmwpe_2.wav" : "uhmwpe_3.wav";
                }

                Singleton<BetterAudio>.Instance.PlayAtPoint(pos, Plugin.LoadedAudioClips[audioClip], dist, BetterAudio.AudioSourceGroupType.Distant, 200, dist >= 40 ? volDist : volClose, EOcclusionTest.Continuous);
            }
            else if (mat == EArmorMaterial.Titan || mat == EArmorMaterial.ArmoredSteel)
            {
                string audioClip = "metal_1.wav";
                if (dist >= 40)
                {
                    audioClip = playCounter == 0 ? "metal_dist_1.wav" : playCounter == 1 ? "metal_dist_2.wav" : "metal_dist_3.wav";
                }
                else
                {
                    audioClip = playCounter == 0 ? "metal_1.wav" : playCounter == 1 ? "metal_2.wav" : "metal_3.wav";
                }

                Singleton<BetterAudio>.Instance.PlayAtPoint(pos, Plugin.LoadedAudioClips[audioClip], dist, BetterAudio.AudioSourceGroupType.Distant, 200, dist >= 40 ? 4.3f * Plugin.ArmorFarHitSoundMulti.Value : 2.25f * Plugin.ArmorCloseHitSoundMulti.Value, EOcclusionTest.Continuous);
            }
            else if (mat == EArmorMaterial.Glass)
            {
                string audioClip = "glass_1.wav";
                if (dist >= 40)
                {
                    audioClip = playCounter == 0 ? "impact_dist_3.wav" : playCounter == 1 ? "impact_dist_4.wav" : "impact_dist_2.wav";
                }
                else
                {
                    audioClip = playCounter == 0 ? "glass_1.wav" : playCounter == 1 ? "glass_2.wav" : "glass_3.wav";
                }
                Singleton<BetterAudio>.Instance.PlayAtPoint(pos, Plugin.LoadedAudioClips[audioClip], dist, BetterAudio.AudioSourceGroupType.Distant, 200, dist >= 40 ? volDist: volClose, EOcclusionTest.Continuous);
            }
            else 
            {
                string audioClip = "impact_1.wav";
                if (dist >= 40)
                {
                    audioClip = playCounter == 0 ? "impact_dist_4.wav" : playCounter == 1 ? "impact_dist_1.wav" : "impact_dist_3.wav";
                }
                else
                {
                    audioClip = playCounter == 0 ? "impact_1.wav" : playCounter == 1 ? "impact_2.wav" : "impact_3.wav";
                }
                Singleton<BetterAudio>.Instance.PlayAtPoint(pos, Plugin.LoadedAudioClips[audioClip], dist, BetterAudio.AudioSourceGroupType.Distant, 200, dist >= 40 ? volDist : volClose, EOcclusionTest.Continuous);
            }
        }

        [PatchPrefix]
        private static bool Prefix(GClass2624 shot, ref ArmorComponent __instance)
        {
            if (__instance.Repairable.Durability <= 0f && __instance.Template.ArmorMaterial != EArmorMaterial.ArmoredSteel && !__instance.Template.ArmorZone.Contains(EBodyPart.Head))
            {
                return false;
            }

            bool hitSecondaryArmor = false;
            bool hasBypassedArmor = false;
            bool hasSideArmor = GearProperties.HasSideArmor(__instance.Item);
            bool hasStomachArmor = GearProperties.HasStomachArmor(__instance.Item);
            bool hasNeckArmor = GearProperties.HasNeckArmor(__instance.Item);

            bool isPlayer = __instance.Item.Owner.ID.StartsWith("pmc") || __instance.Item.Owner.ID.StartsWith("scav");

            if (Plugin.EnableArmorHitZones.Value && ((isPlayer && Plugin.EnablePlayerArmorZones.Value) || !isPlayer)) 
            {
                RaycastHit raycast = (RaycastHit)AccessTools.Field(typeof(GClass2624), "raycastHit_0").GetValue(shot);
                Collider col = raycast.collider;
                Vector3 localPoint = col.transform.InverseTransformPoint(raycast.point);
/*                Vector3 normalizedPoint = localPoint.normalized;*/
                Vector3 hitNormal = raycast.normal;
                string hitPart = shot.HittedBallisticCollider.name;

                EHitOrientation hitOrientation = EHitOrientation.UnknownOrientation;

                if (hitPart == HitBox.UpperTorso || hitPart == HitBox.LowerTorso || hitPart == HitBox.Pelvis)
                {
                    hitOrientation = BallisticsController.GetHitOrientation(hitNormal, col.transform, Logger);
                }

                if (hitPart == HitBox.UpperTorso || hitPart == HitBox.LowerTorso || hitPart == HitBox.Pelvis || hitPart == HitBox.LeftForearm || hitPart == HitBox.RightForearm || hitPart == HitBox.LeftUpperArm || hitPart == HitBox.RightUpperArm) 
                {
                    BallisticsController.GetHitArmorZone(Logger, __instance, hitPart, localPoint, hitOrientation, hasSideArmor, hasStomachArmor, hasNeckArmor, ref hasBypassedArmor, ref hitSecondaryArmor);
                }

                if (Plugin.EnableBallisticsLogging.Value)
                {
                    Logger.LogWarning("=============SetPenStatus: Hit Zone ===============");
                    Logger.LogWarning("collider = " + shot.HittedBallisticCollider.name);
                    Logger.LogWarning("orientation = " + hitOrientation);
                    Logger.LogWarning("has bypassed armor = " + hasBypassedArmor);
                    Logger.LogWarning("has secondary armor = " + hitSecondaryArmor);
                    Logger.LogWarning("hit x = " + localPoint.x);
                    Logger.LogWarning("hit y = " + localPoint.y);
                    Logger.LogWarning("hit z = " + localPoint.z);
                    Logger.LogWarning("============================");
                }

                if (hasBypassedArmor == true)
                {
                    return false;
                }
                else 
                {
                    playArmorHitSound(__instance.Template.ArmorMaterial, col.transform.position, __instance.Template.ArmorZone.Contains(EBodyPart.Head));
                    playCounter++;
                    playCounter = playCounter > 2 ? 0 : playCounter;
                }
            }

            float penetrationPower = shot.PenetrationPower;
            float armorDuraPercent = __instance.Repairable.Durability / (float)__instance.Repairable.TemplateDurability * 100f;

            if (__instance.Template.ArmorMaterial == EArmorMaterial.ArmoredSteel)
            {
                armorDuraPercent = 100f;
            }
            else if (__instance.Template.ArmorMaterial == EArmorMaterial.Titan)
            {
                armorDuraPercent = Mathf.Min(100f, armorDuraPercent * 1.8f);
            }
            else 
            {
                armorDuraPercent = Mathf.Min(100f, armorDuraPercent * 1.15f);
            }

            float armorResist = (float)Singleton<BackendConfigSettingsClass>.Instance.Armor.GetArmorClass(__instance.ArmorClass).Resistance;
            armorResist = hitSecondaryArmor == true ? Math.Min(armorResist, 50f) : armorResist;
            float armorFactor = (121f - 5000f / (45f + armorDuraPercent * 2f)) * armorResist * 0.01f;
            if (((armorFactor >= penetrationPower + 15f) ? 0f : ((armorFactor >= penetrationPower) ? (0.4f * (armorFactor - penetrationPower - 15f) * (armorFactor - penetrationPower - 15f)) : (100f + penetrationPower / (0.9f * armorFactor - penetrationPower)))) - shot.Randoms.GetRandomFloat(shot.RandomSeed) * 100f < 0f)
            {
                shot.BlockedBy = __instance.Item.Id;
                Debug.Log(">>> Shot blocked by armor piece");
                if (Plugin.EnableBallisticsLogging.Value == true)
                {
                    Logger.LogWarning("===========PEN STATUS=============== ");
                    Logger.LogWarning("Blocked");
                    Logger.LogWarning("========================== ");
                }
            }
            else
            {
                if (Plugin.EnableBallisticsLogging.Value == true)
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

        protected override MethodBase GetTargetMethod()
        {
            var result = typeof(ArmorComponent).GetMethod(nameof(ArmorComponent.ApplyDamage), BindingFlags.Public | BindingFlags.Instance);

            return result;
        }

        private static int playCounter = 0;

        private static void playRicochetSound(Vector3 pos)
        {
            float dist = CameraClass.Instance.Distance(pos);
            string audioClip = playCounter == 0 ? "ric_1.wav" : playCounter == 1 ? "ric_2.wav" : "ric_3.wav";

            Singleton<BetterAudio>.Instance.PlayAtPoint(pos, Plugin.LoadedAudioClips[audioClip], dist, BetterAudio.AudioSourceGroupType.Distant, 200, 4.25f, EOcclusionTest.Continuous);
        }

        [PatchPrefix]
        private static bool Prefix(ref DamageInfo damageInfo, bool damageInfoIsLocal, ref ArmorComponent __instance, ref float __result)
        {
            EDamageType damageType = damageInfo.DamageType;

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

            if (damageType == EDamageType.Sniper || damageType == EDamageType.Melee || damageType == EDamageType.Landmine)
            {
                return true;
            }

            bool hitSecondaryArmor = false;
            bool hasBypassedArmor = false;

            bool isPlayer = __instance.Item.Owner.ID.StartsWith("pmc") || __instance.Item.Owner.ID.StartsWith("scav");

            if (Plugin.EnableArmorHitZones.Value && ((isPlayer && Plugin.EnablePlayerArmorZones.Value) || !isPlayer)) 
            {
                string hitPart = damageInfo.HittedBallisticCollider.name;
                Collider col = damageInfo.HitCollider;
                Vector3 localPoint = col.transform.InverseTransformPoint(damageInfo.HitPoint);
      /*          Vector3 normalizedPoint = localPoint.normalized;*/
                Vector3 hitNormal = damageInfo.HitNormal;

                bool hasSideArmor = GearProperties.HasSideArmor(__instance.Item);
                bool hasStomachArmor = GearProperties.HasStomachArmor(__instance.Item);
                bool hasNeckArmor = GearProperties.HasNeckArmor(__instance.Item);
                EHitOrientation hitOrientation = EHitOrientation.UnknownOrientation;

                if (hitPart == HitBox.UpperTorso || hitPart == HitBox.LowerTorso || hitPart == HitBox.Pelvis)
                {
                    hitOrientation = BallisticsController.GetHitOrientation(hitNormal, col.transform, Logger);
                }

                if (hitPart == HitBox.UpperTorso || hitPart == HitBox.LowerTorso || hitPart == HitBox.Pelvis || hitPart == HitBox.LeftForearm || hitPart == HitBox.RightForearm || hitPart == HitBox.LeftUpperArm || hitPart == HitBox.RightUpperArm)
                {
                    BallisticsController.GetHitArmorZone(Logger, __instance, hitPart, localPoint, hitOrientation, hasSideArmor, hasStomachArmor, hasNeckArmor, ref hasBypassedArmor, ref hitSecondaryArmor);
                }

                if (damageInfo.DeflectedBy == __instance.Item.Id) 
                {
                    playRicochetSound(col.transform.position);
                    playCounter++;
                    playCounter = playCounter > 2 ? 0 : playCounter;
                }

                if (Plugin.EnableBallisticsLogging.Value)
                {
                    Logger.LogWarning("============ApplyDamagePatch: Hit Zone ============== ");
                    Logger.LogWarning("collider = " + hitPart);
                    Logger.LogWarning("Has Bypassed Armor = " + hasBypassedArmor);
                    Logger.LogWarning("Has Hit Secondary Armor = " + hitSecondaryArmor);
                    Logger.LogWarning("hit x = " + localPoint.x);
                    Logger.LogWarning("hit y = " + localPoint.y);
                    Logger.LogWarning("hit z = " + localPoint.z);
                    Logger.LogWarning("========================== ");
                }
            }

            AmmoTemplate ammoTemp = (AmmoTemplate)Singleton<ItemFactory>.Instance.ItemTemplates[damageInfo.SourceId];
            BulletClass ammo = new BulletClass("newAmmo", ammoTemp);

            float speedFactor = ammo.GetBulletSpeed / damageInfo.ArmorDamage;
            float armorDamageActual = ammo.ArmorDamage * speedFactor;

            if (hasBypassedArmor) 
            {
                __result = 0f;
                return false;
            }

            float KE = (0.5f * ammo.BulletMassGram * damageInfo.ArmorDamage * damageInfo.ArmorDamage) / 1000f;
            float bluntThrput = hitSecondaryArmor == true ? __instance.Template.BluntThroughput * 1.15f : __instance.Template.BluntThroughput;
            float penPower = damageInfo.PenetrationPower;
            float duraPercent = __instance.Repairable.Durability / (float)__instance.Repairable.TemplateDurability;
            float armorResist = (float)Singleton<BackendConfigSettingsClass>.Instance.Armor.GetArmorClass(__instance.ArmorClass).Resistance;
            armorResist = hitSecondaryArmor == true ? 50f : armorResist;
            float armorDestructibility = Singleton<BackendConfigSettingsClass>.Instance.ArmorMaterials[__instance.Template.ArmorMaterial].Destructibility;

            float armorFactor = armorResist * (Mathf.Min(1f, duraPercent * 1.65f));
  /*          float throughputDuraFactored = Mathf.Min(1f, bluntThrput * (1f + ((duraPercent - 1f) * -1f)));*/
            float penDuraFactoredClass = Mathf.Max(1f, armorFactor - (penPower / 1.8f));
            float penFactoredClass = Mathf.Max(1f, armorResist - (penPower / 1.8f));
            float maxPotentialDuraDamage = KE / penDuraFactoredClass;
            float maxPotentialBluntDamage = KE / penFactoredClass;

            float throughputFactoredDamage = Math.Min(damageInfo.Damage, maxPotentialDuraDamage * bluntThrput) * (armorDamageActual <= 2f ? 0.5f : 1f);

            float armorStatReductionFactor = Mathf.Max((1 - (penDuraFactoredClass / 100f)), 0.1f);

            if (damageInfo.DeflectedBy == __instance.Item.Id)
            {
                damageInfo.Damage *= 0.5f * armorStatReductionFactor;
                armorDamageActual *= 0.5f * armorStatReductionFactor;
                damageInfo.ArmorDamage *= 0.5f * armorStatReductionFactor;
                damageInfo.PenetrationPower *= 0.5f * armorStatReductionFactor;
            }

            if (__instance.Template.ArmorMaterial == EArmorMaterial.ArmoredSteel && !__instance.Template.ArmorZone.Contains(EBodyPart.Head))
            {
                throughputFactoredDamage = Math.Min(damageInfo.Damage, maxPotentialBluntDamage * bluntThrput) * (armorDamageActual <= 2f ? 0.1f : 1f);
            }
            if ((__instance.Template.ArmorMaterial == EArmorMaterial.ArmoredSteel || __instance.Template.ArmorMaterial == EArmorMaterial.Titan) && __instance.Template.ArmorZone.Contains(EBodyPart.Head))
            {
                armorDestructibility = 0.1f;
            }


            float durabilityLoss = (maxPotentialBluntDamage / 24f) * Mathf.Clamp(ammo.BulletDiameterMilimeters / 7.62f, 1f, 2f) * armorDamageActual * armorDestructibility * (hitSecondaryArmor ? 0.25f : 1f);

            if (!(damageInfo.BlockedBy == __instance.Item.Id) && !(damageInfo.DeflectedBy == __instance.Item.Id) && !hasBypassedArmor)
            {
                durabilityLoss *= (1 - (penPower / 100f));
                damageInfo.Damage *= armorStatReductionFactor;
                damageInfo.PenetrationPower *= armorStatReductionFactor;
            }
            else if(!hasBypassedArmor)
            {
                damageInfo.Damage = throughputFactoredDamage;
                damageInfo.StaminaBurnRate = (throughputFactoredDamage / 100f) * 2f;
            }

            if ((damageInfo.BlockedBy == __instance.Item.Id || damageInfo.DeflectedBy == __instance.Item.Id) && !hasBypassedArmor) 
            {
                damageInfo.HeavyBleedingDelta =  0f;
                damageInfo.LightBleedingDelta = 0f;
            }
            
            durabilityLoss = Math.Max(durabilityLoss, 0.05f);
            __instance.ApplyDurabilityDamage(durabilityLoss);
            __result = durabilityLoss;

            if (Plugin.EnableBallisticsLogging.Value)
            {
                Logger.LogWarning("===========ARMOR DAMAGE=============== ");
                Logger.LogWarning("KE " + KE);
                Logger.LogWarning("Pen " + penPower);
                Logger.LogWarning("Armor Damage " + armorDamageActual);
                Logger.LogWarning("Class " + armorResist);
                Logger.LogWarning("Throughput " + bluntThrput);
                Logger.LogWarning("Material Descructibility " + armorDestructibility);
                Logger.LogWarning("Dura percent " + duraPercent);
                Logger.LogWarning("Durability Loss " + durabilityLoss);
                Logger.LogWarning("Max potential blunt damage " + maxPotentialDuraDamage);
                Logger.LogWarning("Max potential dura damage " + maxPotentialBluntDamage);
                Logger.LogWarning("Damage " + damageInfo.Damage);
                Logger.LogWarning("Throughput Facotred Damage " + throughputFactoredDamage);
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
        private static bool Prefix(EFT.Ballistics.BallisticsCalculator __instance, BulletClass ammo, Vector3 origin, Vector3 direction, int fireIndex, Player player, Item weapon, ref GClass2624 __result, float speedFactor, int fragmentIndex = 0)
        {
            
   /*         Logger.LogWarning("!!!!!!!!!!! Shot Created!! !!!!!!!!!!!!!!");
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

/*            Logger.LogWarning("========================AFTER SPEED FACTOR============================");
            Logger.LogWarning("Round ID = " + ammo.TemplateId);
            Logger.LogWarning("Round Damage = " + damageFactored);
            Logger.LogWarning("Round Penetration Power = " + penPowerFactored);
            Logger.LogWarning("Round Penetration Chance = " + penChanceFactored);
            Logger.LogWarning("Round Frag Chance = " + fragchanceFactored);
            Logger.LogWarning("Round Factored Speed = " + velocityFactored);
            Logger.LogWarning("Round Factored BC = " + bcFactored);
            Logger.LogWarning("==============================================================");*/

            __result = GClass2624.Create(ammo, fragmentIndex, randomNum, origin, direction, velocityFactored, velocityFactored, ammo.BulletMassGram, ammo.BulletDiameterMilimeters, (float)damageFactored, penPowerFactored, penChanceFactored, ammo.RicochetChance, fragchanceFactored, 1f, ammo.MinFragmentsCount, ammo.MaxFragmentsCount, EFT.Ballistics.BallisticsCalculator.DefaultHitBody, __instance.Randoms, bcFactored, player, weapon, fireIndex, null);
            return false;

        }
    }

    public class ApplyCorpseImpulsePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            var result = typeof(Player).GetMethod("ApplyCorpseImpulse", BindingFlags.Instance | BindingFlags.NonPublic);

            return result;
        }

        [PatchPrefix]
        private static bool Prefix(Player __instance)
        {
            DamageInfo lastDam = (DamageInfo)AccessTools.Field(typeof(Player), "LastDamageInfo").GetValue(__instance);
            Corpse corpse = (Corpse)AccessTools.Field(typeof(Player), "Corpse").GetValue(__instance);

            float force;
            if (lastDam.DamageType == EDamageType.Bullet)
            {
                AmmoTemplate ammoTemp = (AmmoTemplate)Singleton<ItemFactory>.Instance.ItemTemplates[lastDam.SourceId];
                BulletClass ammo = new BulletClass("newAmmo", ammoTemp);
                float KE = ((0.5f * ammo.BulletMassGram * lastDam.ArmorDamage * lastDam.ArmorDamage) / 1000);
                force = (-Mathf.Max(1f, KE / 1000f)) * Plugin.RagdollForceModifier.Value;
            }
            else if (lastDam.DamageType == EDamageType.Explosion)
            {
                force = 150f;
            }
            else 
            {
                force = 5f;
            }

            AccessTools.Field(typeof(Player), "_corpseAppliedForce").SetValue(__instance, force);
            corpse.Ragdoll.ApplyImpulse(lastDam.HitCollider, lastDam.Direction, lastDam.HitPoint, force);

            return false;
        }
    }

    public class RagdollPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            var result = typeof(RagdollClass).GetMethod("method_8", BindingFlags.Instance | BindingFlags.NonPublic);

            return result;
        }

        [PatchPrefix]
        private static bool Prefix(RagdollClass __instance, RigidbodySpawner ___rigidbodySpawner_1)
        {
            Logger.LogWarning("mass " + ___rigidbodySpawner_1.Rigidbody.mass);
            Logger.LogWarning("drag " + ___rigidbodySpawner_1.Rigidbody.mass);

            ___rigidbodySpawner_1.Rigidbody.mass = 100f;
            return true;
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
            private static bool Prefix(GClass2624 shot, ref ArmorComponent __instance)
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
                    if (Plugin.EnableBallisticsLogging.Value == true)
                    {
                        Logger.LogWarning("Blocked");
                    }
                }
                else
                {
                    if (Plugin.EnableBallisticsLogging.Value == true)
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
