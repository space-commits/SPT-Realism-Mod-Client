using SPT.Reflection.Patching;
using SPT.Reflection.Utils;
using Comfort.Common;
using EFT;
using EFT.Ballistics;
using EFT.HealthSystem;
using EFT.Interactive;
using EFT.InventoryLogic;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using EFTSlot = GClass3113;
using ArmorSlot = EFT.InventoryLogic.Slot;
using Diz.Skinning;
using EFT.UI;
using static EFT.Player;
using DamageInfo = DamageInfoStruct;
using SkillManagerClass = EFT.SkillManager.GClass1981;
using PastTimeClass = GClass1629;

namespace RealismMod
{
    //Fix weapon accuracy stat not being used properly by BSG
    public class InitiateShotPatch : ModulePatch
    {
        private static FieldInfo _playerfield;
        private static FieldInfo _accField;
        private static FieldInfo _buckFeld;
        private static FieldInfo _prefabField;
        private static FieldInfo _skillField;
        private static FieldInfo _soundField;
        private static FieldInfo _recoilField;

        protected override MethodBase GetTargetMethod()
        {
            _playerfield = AccessTools.Field(typeof(FirearmController), "_player");
            _accField = AccessTools.Field(typeof(FirearmController), "float_3");
            _buckFeld = AccessTools.Field(typeof(FirearmController), "float_4");
            _prefabField = AccessTools.Field(typeof(FirearmController), "weaponPrefab_0");
            _skillField = AccessTools.Field(typeof(FirearmController), "gclass1981_0");
            _soundField = AccessTools.Field(typeof(FirearmController), "weaponSoundPlayer_0");
            _recoilField = AccessTools.Field(typeof(FirearmController), "float_5");
            return typeof(Player.FirearmController).GetMethod("method_57", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static bool Prefix(Player.FirearmController __instance, Weapon weapon, AmmoItemClass ammo, int chamberIndex, bool multiShot = false)
        {

            Player player = (Player)_playerfield.GetValue(__instance);
            if (player == null || !player.IsYourPlayer) return true;

            float accuracy = (float)_accField.GetValue(__instance);
            float accuracyBuck = (float)_buckFeld.GetValue(__instance);
            WeaponPrefab weaponprefab = (WeaponPrefab)_prefabField.GetValue(__instance);
            SkillManagerClass skillFactor = (SkillManagerClass)_skillField.GetValue(__instance);
            WeaponSoundPlayer WeaponSoundPlayer = (WeaponSoundPlayer)_soundField.GetValue(__instance);

            Transform original = __instance.CurrentFireport.Original;
            Vector3 position = __instance.CurrentFireport.position;
            Vector3 baseShotDirection = __instance.WeaponDirection;
            Vector3 shotPosition = position;
            float ammoFactor = ammo.AmmoFactor;
            float heatFactor = 1f;

            BackendConfigSettingsClass instance = Singleton<BackendConfigSettingsClass>.Instance;
            __instance.AdjustShotVectors(ref shotPosition, ref baseShotDirection);
            ammo.buckshotDispersion = accuracyBuck;
            __instance.CurrentChamberIndex = chamberIndex;
            weapon.OnShot(ammo.DurabilityBurnModificator, ammo.HeatFactor, player.Skills.WeaponDurabilityLosOnShotReduce.Value, instance.Overheat, PastTimeClass.PastTime);

            if (weapon.MalfState.LastShotOverheat >= 1f) //force overheat inaccuracy to start much earlier
            {
                heatFactor = Mathf.Lerp(1f, instance.Overheat.MaxCOIIncreaseMult, (weapon.MalfState.LastShotOverheat - 1f) / (instance.Overheat.MaxOverheat - 1f));
                heatFactor = Mathf.Pow(heatFactor, 2f);
            }
            int doubleShot;
      
            if (multiShot)
            {
                if ((doubleShot = ((chamberIndex > 0) ? 1 : 0)) != 0)
                {
                    float x = UnityEngine.Random.Range(weaponprefab.DupletAccuracyPenaltyX.x, weaponprefab.DupletAccuracyPenaltyX.y);
                    float y = UnityEngine.Random.Range(weaponprefab.DupletAccuracyPenaltyY.x, weaponprefab.DupletAccuracyPenaltyY.y);
                    Vector3 vector2 = new Vector3(x, y);
                    float angle = vector2.y * -1f;
                    baseShotDirection = Quaternion.AngleAxis(vector2.x, original.forward) * baseShotDirection;
                    baseShotDirection = Quaternion.AngleAxis(angle, original.right) * baseShotDirection;
                }
            }
            else
            {
                doubleShot = 0;
            }
            float doubleActionFactor = weapon.CylinderHammerClosed ? (weapon.DoubleActionAccuracyPenalty * (1f - skillFactor.DoubleActionRecoilReduce) * weapon.StockDoubleActionAccuracyPenaltyMult) : 0f;
            float barrelDeviation = __instance.method_54(weapon);
            double accuracyBuff = weapon.GetItemComponent<BuffComponent>().WeaponSpread;
       
            if (accuracyBuff.ApproxEquals(0.0))
            {
                accuracyBuff = 1.0;
            }
            float ramdomnessFactor = (accuracy + doubleActionFactor) * ammoFactor * heatFactor * barrelDeviation * (float)accuracyBuff * 0.012f;
            Vector3 randomSphere = UnityEngine.Random.insideUnitSphere * ramdomnessFactor;
            Vector3 shotDirection = baseShotDirection + randomSphere;
  
            __instance.InitiateShot(weapon, ammo, shotPosition, shotDirection.normalized, position, chamberIndex, weapon.MalfState.LastShotOverheat);
            float recoilFactor = (doubleShot != 0) ? 1.5f : 1f;

            _recoilField.SetValue(__instance, recoilFactor + (float)ammo.ammoRec / 100f);
            __instance.method_58(WeaponSoundPlayer, ammo, shotPosition, shotDirection, multiShot);
            if (ammo.AmmoTemplate.IsLightAndSoundShot)
            {
                __instance.method_61(position, baseShotDirection);
                __instance.LightAndSoundShot(position, baseShotDirection, ammo.AmmoTemplate);
            }

            return false;
        }
    }


    //for making player meshes invisible for testing
    public class SetSkinPatch : ModulePatch
     {
         protected override MethodBase GetTargetMethod()
         {
             return typeof(PlayerBody).GetMethod("SetSkin", BindingFlags.Instance | BindingFlags.Public);
         }

         [PatchPostfix]
         private static void Prefix(PlayerBody __instance, KeyValuePair<EBodyModelPart, ResourceKey> part, Skeleton skeleton)
         {
             __instance.BodySkins[part.Key].Unskin();
         }
    }

    //modify bullet energy loss over distance
    public class VelocityPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GClass3369).GetMethod("Initialize", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void Prefix(GClass3369 __instance)
        {
            float bcFactor = (float)AccessTools.Field(typeof(GClass3369), "float_3").GetValue(__instance);
            AccessTools.Field(typeof(GClass3369), "float_3").SetValue(__instance, bcFactor *= PluginConfig.DragModifier.Value);
        }
    }

    //adjust how arm penetration is handled
    public class IsPenetratedPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(BodyPartCollider).GetMethod("IsPenetrated", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static bool Prefix(BodyPartCollider __instance, EftBulletClass shot, ref  bool __result)
        {
            if (shot.HittedBallisticCollider as BodyPartCollider != null)
            {
                BodyPartCollider bodyPartCollider = (BodyPartCollider)shot.HittedBallisticCollider;
                EBodyPartColliderType bodyPart = bodyPartCollider.BodyPartColliderType;
                bool isArm = bodyPart.ToString().ToLower().Contains("arm");
                if (isArm && shot.PenetrationPower > 10)
                {
                    __result = true;
                    return false;
                }
            }
            return true;
        }
    }

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

    public class DamageInfoPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(DamageInfo).GetConstructor(new Type[] { typeof(EDamageType), typeof(EftBulletClass) });
        }

        [PatchPrefix]
        private static bool Prefix(ref DamageInfo __instance, EDamageType damageType, EftBulletClass shot)
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
            if (__instance.DamageType == EDamageType.Blunt || __instance.DamageType == EDamageType.Bullet)
            {
                if (PluginConfig.EnableLogging.Value) 
                {
                    Logger.LogWarning("================shot velocity============== " + shot.VelocityMagnitude);
                }
                __instance.ArmorDamage = shot.VelocityMagnitude;
            }
            else
            {
                __instance.ArmorDamage = shot.ArmorDamage;
            }
            __instance.DeflectedBy = shot.DeflectedBy;
            __instance.BlockedBy = shot.BlockedBy;
            __instance.MasterOrigin = shot.MasterOrigin;
            __instance.IsForwardHit = shot.IsForwardHit;
            __instance.SourceId = shot.Ammo.TemplateId;

            AmmoItemClass bulletClass;
            if ((bulletClass = (shot.Ammo as AmmoItemClass)) != null)
            {
                __instance.StaminaBurnRate = bulletClass.StaminaBurnRate;
                __instance.HeavyBleedingDelta = bulletClass.HeavyBleedingDelta;
                __instance.LightBleedingDelta = bulletClass.LightBleedingDelta;
            }
            else
            {
                __instance.LightBleedingDelta = 0f;
                __instance.HeavyBleedingDelta = 0f;
                __instance.StaminaBurnRate = 0f;
                KnifeItemClass knifeClass;
                if ((knifeClass = (__instance.Weapon as KnifeItemClass)) != null)
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
        private static FieldInfo inventoryControllerField;

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
                int rndNumber = UnityEngine.Random.Range(1, 81);
                float kineticEnergyFactor = 1f + (kineticEnergy / 1000f);
                float hitArmArmorFactor = hitArmArmor ? 0.5f : 1f;
                float hitLocationModifier = forearm ? 1.5f : 1f;
                float totalChance = Mathf.Round(PluginConfig.DisarmBaseChance.Value * kineticEnergyFactor * hitArmArmorFactor * hitLocationModifier);

                if (rndNumber <= totalChance)
                {
                    var inventoryController = (PlayerOwnerInventoryController)inventoryControllerField.GetValue(player);
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

        private static void TryDoKnockdown(Player player, float kineticEnergy, bool bonusChance, bool isPlayer)
        {
            int rndNumber = UnityEngine.Random.Range(1, 51);
            float kineticEnergyFactor = 1f + (kineticEnergy / 1000f);
            float hitLocationModifier = bonusChance ? 2f : 1f;
            float totalChance = Mathf.Round(PluginConfig.FallBaseChance.Value * kineticEnergyFactor * hitLocationModifier);

            if (rndNumber <= totalChance)
            {
                player.ToggleProne();
                if ((isPlayer && PluginConfig.CanDisarmPlayer.Value) || (!isPlayer && PluginConfig.CanDisarmBot.Value))
                {
                    TryDoDisarm(player, kineticEnergy * 0.25f, false, false);
                }
            }
        }

        private static void DisarmAndKnockdownCheck(Player player, DamageInfo damageInfo, EBodyPart bodyPartType, EBodyPartColliderType partHit, float KE, bool hasArmArmor)
        {
            float totalHPPerc = (player.ActiveHealthController.GetBodyPartHealth(EBodyPart.Common).Current - damageInfo.Damage) / player.ActiveHealthController.GetBodyPartHealth(EBodyPart.Common).Maximum;
            float hitPartHP = player.ActiveHealthController.GetBodyPartHealth(bodyPartType).Current;
            float toBeHP = hitPartHP - damageInfo.Damage;
            bool canDoKnockdown = !player.IsInPronePose && ((!player.IsYourPlayer && PluginConfig.CanFellBot.Value) || (player.IsYourPlayer && PluginConfig.CanFellPlayer.Value));
            bool canDoDisarm = ((!player.IsYourPlayer && PluginConfig.CanDisarmBot.Value) || (player.IsYourPlayer && PluginConfig.CanDisarmPlayer.Value));
            bool hitForearm = partHit == EBodyPartColliderType.LeftForearm || partHit == EBodyPartColliderType.RightForearm;
            bool hitCalf = partHit == EBodyPartColliderType.LeftCalf || partHit == EBodyPartColliderType.RightCalf;
            bool hitThigh = partHit == EBodyPartColliderType.LeftThigh || partHit == EBodyPartColliderType.RightThigh;
            bool isOverdosed = player.IsYourPlayer && Plugin.RealHealthController.HasOverdosed && damageInfo.Damage > 10f;
            bool fell = damageInfo.DamageType == EDamageType.Fall && damageInfo.Damage >= 15f;
            bool doShotLegKnockdown = (hitCalf || hitThigh) && toBeHP <= 5f;
            bool doShotDisarm = hitForearm && toBeHP <= 5f;
            bool doHeadshotKnockdown = bodyPartType == EBodyPart.Head && toBeHP > 0f && toBeHP <= 12f && damageInfo.Damage >= 5;
            bool hasBonusChance = hitCalf || bodyPartType == EBodyPart.Head;
            float chanceModifier = fell ? 50000 : 1f;

            if (canDoDisarm && (doShotDisarm || isOverdosed || fell))
            {
                TryDoDisarm(player, KE * chanceModifier, hasArmArmor, hitForearm);
            }

            if (canDoKnockdown && (doShotLegKnockdown || doHeadshotKnockdown || isOverdosed || fell))
            {
                TryDoKnockdown(player, KE * chanceModifier, hasBonusChance, player.IsYourPlayer);
            }
        }

        protected override MethodBase GetTargetMethod()
        {
            inventoryControllerField = AccessTools.Field(typeof(Player), "_inventoryController");
            return typeof(Player).GetMethod("ApplyDamageInfo", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static void Prefix(Player __instance, ref DamageInfo damageInfo, EBodyPart bodyPartType)
        {
            if (PluginConfig.DevMode.Value && __instance.IsYourPlayer)
            {
                damageInfo.Damage = 0f;
                return;
            }

            if (damageInfo.DamageType == EDamageType.Bullet || damageInfo.DamageType == EDamageType.Melee)
            {
                EBodyPartColliderType partHit = EBodyPartColliderType.None;
                if (damageInfo.BodyPartColliderType == EBodyPartColliderType.None) //for fika value is populated, otherwise it's unused
                {
                    BodyPartCollider bodyPartCollider = (BodyPartCollider)damageInfo.HittedBallisticCollider;
                    partHit = bodyPartCollider.BodyPartColliderType;
                }
                else
                {
                    partHit = damageInfo.BodyPartColliderType;
                }
 
                //if fika, based on collidor type, get refernce to player assetpoolobject, get collidors, get component
                if (PluginConfig.EnableBodyHitZones.Value) BallisticsController.ModifyDamageByZone(__instance, ref damageInfo, partHit);
  
                float KE = 1f;
                AmmoTemplate ammoTemp = null;
                BallisticsController.GetKineticEnergy(damageInfo, ref ammoTemp, ref KE);

                if (ammoTemp != null && ammoTemp.ProjectileCount <= 2 && __instance.IsAI && damageInfo.HittedBallisticCollider != null && !damageInfo.Blunt && PluginConfig.EnableHitSounds.Value)
                {
                    BallisticsController.PlayBodyHitSound(bodyPartType, damageInfo.HittedBallisticCollider.transform.position, UnityEngine.Random.Range(0, 2));
                }

                bool doSpalling = BallisticsController.ShouldDoSpalling(ammoTemp, damageInfo, bodyPartType);

                bool hasArmArmor = false;
                bool hasLegProtection = false;
                int faceProtectionCount = 0;
                ArmorComponent armor = null;
                if (doSpalling || (ammoTemp != null && ammoTemp.ProjectileCount <= 2))
                {
                    BallisticsController.GetArmorComponents(__instance, damageInfo, bodyPartType, ref armor, ref faceProtectionCount, ref doSpalling, ref hasArmArmor, ref hasLegProtection);
                }

                if (doSpalling && armor != null && __instance?.ActiveHealthController != null)
                {
                    var gearStats = StatsData.GetDataObj<Gear>(StatsData.GearStats, armor.Item.TemplateId);
                    if (gearStats.CanSpall) BallisticsController.CalculatSpalling(__instance, ref damageInfo, KE, armor, ammoTemp, faceProtectionCount, hasArmArmor, hasLegProtection);
                }
                 
                if (__instance?.ActiveHealthController != null)
                {
                    DisarmAndKnockdownCheck(__instance, damageInfo, bodyPartType, partHit, KE, hasArmArmor);
                }

                if (PluginConfig.EnableBallisticsLogging.Value)
                {
                    Logger.LogWarning("==========Apply Damage Info=============== ");
                    Logger.LogWarning("Damage " + damageInfo.Damage);
                    Logger.LogWarning("Pen " + damageInfo.PenetrationPower);
                    Logger.LogWarning("========================= ");
                }

            }
        }
    }

    public class AfterPenPlatePatch : ModulePatch
    {
        private static FieldInfo armorCompsField;

        protected override MethodBase GetTargetMethod()
        {
            armorCompsField = AccessTools.Field(typeof(Player), "_preAllocatedArmorComponents");
            return typeof(EftBulletClass).GetMethod("smethod_2", BindingFlags.Static | BindingFlags.Public);
        }

        [PatchPrefix]
        private static bool Prefix(BallisticCollider parentBallisticCollider, bool isForwardHit, EftBulletClass shot)
        {

            if (!isForwardHit)
            {
                return false;
            }
            BodyPartCollider bodyPartCollider;
            if ((bodyPartCollider = (parentBallisticCollider as BodyPartCollider)) == null)
            {
                return false;
            }
            if (bodyPartCollider.playerBridge == null)
            {
                return false;
            }

            Player player = Utils.GetPlayerByProfileId(bodyPartCollider.playerBridge.iPlayer.ProfileId);
            if (player == null) return true;

            List<ArmorComponent> armors = (List<ArmorComponent>)armorCompsField.GetValue(player);

            armors.Clear();
            player.Inventory.GetPutOnArmorsNonAlloc(armors);
            ArmorPlateCollider armorPlateCollider = bodyPartCollider as ArmorPlateCollider;
            EArmorPlateCollider armorPlateCollider2 = (armorPlateCollider == null) ? ((EArmorPlateCollider)0) : armorPlateCollider.ArmorPlateColliderType;
 
            for (int i = 0; i < armors.Count; i++)
            {
                ArmorComponent armorComponent = armors[i];
                if (armorComponent.ShotMatches(bodyPartCollider.BodyPartColliderType, armorPlateCollider2))
                {
                    if (PluginConfig.EnableBallisticsLogging.Value) 
                    {
                        Logger.LogWarning("///AFTER PLATE PEN///");
                        Logger.LogWarning("damage before = " + shot.Damage); 
                        Logger.LogWarning("pen before = " + shot.PenetrationPower);
                    }

                    EFTSlot slot;
                    ArmorSlot softArmorSlot;
                    float softArmorReduction = 1f;
                    if ((slot = (armorComponent.Item.CurrentAddress as EFTSlot)) != null && (softArmorSlot = (slot.Slot as ArmorSlot)) != null && softArmorSlot.BluntDamageReduceFromSoftArmor)
                    {
   
                        softArmorReduction = 0.7f;
                    }
                    BallisticsController.CalcAfterPenStats(armorComponent.Repairable.Durability, armorComponent.ArmorClass, armorComponent.Repairable.TemplateDurability, ref shot.Damage, ref shot.PenetrationPower, softArmorReduction);
                
                    if (PluginConfig.EnableBallisticsLogging.Value)
                    {
                        Logger.LogWarning("damage after = " + shot.Damage);
                        Logger.LogWarning("pen after = " + shot.PenetrationPower);
                        Logger.LogWarning("////");
                    }
                    break;
                }
            }

            return false;
        }
    }

    public class SetPenetrationStatusPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(ArmorComponent).GetMethod(nameof(ArmorComponent.SetPenetrationStatus), BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPrefix]
        private static bool Prefix(EftBulletClass shot, ArmorComponent __instance)
        {
            bool isSteelBodyArmor = __instance.Template.ArmorMaterial == EArmorMaterial.ArmoredSteel && !BallisticsController.ArmorHasSpecificColliders(__instance, BallisticsController.HeadCollidors);
            if (__instance.Repairable.Durability <= 0f && !isSteelBodyArmor)
            {
                return false;
            }

            float penetrationPower = shot.PenetrationPower;
            float armorDuraPercent = __instance.Repairable.Durability / (float)__instance.Repairable.TemplateDurability * 100f;

            if (__instance.Template.ArmorMaterial == EArmorMaterial.ArmoredSteel && isSteelBodyArmor)
            {
                armorDuraPercent = 100f;
            }
            else if (__instance.Template.ArmorMaterial == EArmorMaterial.Titan || __instance.Template.ArmorMaterial == EArmorMaterial.Aluminium || (!isSteelBodyArmor && __instance.Template.ArmorMaterial == EArmorMaterial.ArmoredSteel))
            {
                armorDuraPercent = Mathf.Min(100f, armorDuraPercent * 1.5f);
            }
            else 
            {
                armorDuraPercent = Mathf.Min(100f, armorDuraPercent * 1.1f);
            }

            float armorResist = (float)Singleton<BackendConfigSettingsClass>.Instance.Armor.GetArmorClass(__instance.ArmorClass).Resistance;
            float realResistance = (121f - 5000f / (45f + armorDuraPercent * 2f)) * armorResist * 0.01f;
            bool didPenByChance = ((realResistance >= penetrationPower + 15f) ? 0f : ((realResistance >= penetrationPower) ? (0.4f * (realResistance - penetrationPower - 15f) * (realResistance - penetrationPower - 15f)) : (100f + penetrationPower / (0.9f * realResistance - penetrationPower)))) - shot.Randoms.GetRandomFloat(shot.RandomSeed) * 100f < 0f;
            bool shouldBeBlocked = armorDuraPercent >= 90f && armorResist - shot.PenetrationPower >= 5;
            if (shouldBeBlocked || didPenByChance)
            {
                shot.BlockedBy = __instance.Item.Id;
                if (PluginConfig.EnableBallisticsLogging.Value)
                {
                    Debug.Log(">>> Shot blocked by armor piece");
                    Logger.LogWarning("===========PEN STATUS=============== ");
                    Logger.LogWarning("Blocked");
                    Logger.LogWarning("shouldBeBlocked " + shouldBeBlocked);
                    Logger.LogWarning("========================== ");
                }
            }
            else
            {
                if (PluginConfig.EnableBallisticsLogging.Value == true)
                {
                    Logger.LogWarning("============PEN STATUS============== ");
                    Logger.LogWarning("Penetrated");
                    Logger.LogWarning("shouldBeBlocked " + shouldBeBlocked);
                    Logger.LogWarning("========================== ");
                }
            }
            return false;
        }
    }

    public class ApplyArmorDamagePatch : ModulePatch
    {
        private static void playRicochetSound(Vector3 pos, int rndNum)
        {
            float dist = CameraClass.Instance.Distance(pos);
            string audioClip = rndNum == 0 ? "ric_1.wav" : rndNum == 1 ? "ric_2.wav" : "ric_3.wav";

            Singleton<BetterAudio>.Instance.PlayAtPoint(pos, Plugin.HitAudioClips[audioClip], dist, BetterAudio.AudioSourceGroupType.Impacts, 40, 4.25f, EOcclusionTest.Regular);
        }

        private static void playArmorHitSound(EArmorMaterial mat, Vector3 pos, bool isHelm, int rndNum)
        {
            float dist = CameraClass.Instance.Distance(pos);
            float volClose = 0.5f * PluginConfig.ArmorCloseHitSoundMulti.Value;
            float volDist = 3f * PluginConfig.ArmorFarHitSoundMulti.Value;
            float distThreshold = 30f;

            if (mat == EArmorMaterial.Aramid)
            {
                string audioClip = "aramid_1.wav";
                if (dist >= distThreshold)
                {
                    audioClip = rndNum == 0 ? "impact_dist_1.wav" : rndNum == 1 ? "impact_dist_2.wav" : "impact_dist_3.wav";
                }
                else
                {
                    if (!isHelm)
                    {
                        audioClip = rndNum == 0 ? "aramid_1.wav" : rndNum == 1 ? "aramid_2.wav" : "aramid_3.wav";
                    }
                    else
                    {
                        audioClip = rndNum == 0 ? "uhmwpe_1.wav" : rndNum == 1 ? "uhmwpe_2.wav" : "uhmwpe_3.wav";
                    }
                }

                Singleton<BetterAudio>.Instance.PlayAtPoint(pos, Plugin.HitAudioClips[audioClip], dist, BetterAudio.AudioSourceGroupType.Impacts, 100, dist >= distThreshold ? volDist : volClose, EOcclusionTest.Regular);
            }
            else if (mat == EArmorMaterial.Ceramic)
            {
                string audioClip = "ceramic_1.wav";
                if (dist >= distThreshold)
                {
                    audioClip = rndNum == 0 ? "ceramic_dist_1.wav" : rndNum == 1 ? "ceramic_dist_2.wav" : "ceramic_dist_3.wav";
                }
                else
                {
                    audioClip = rndNum == 0 ? "ceramic_1.wav" : rndNum == 1 ? "ceramic_2.wav" : "ceramic_3.wav";
                }

                Singleton<BetterAudio>.Instance.PlayAtPoint(pos, Plugin.HitAudioClips[audioClip], dist, BetterAudio.AudioSourceGroupType.Impacts, 100, dist >= distThreshold ? volDist * 1.5f : volClose * 1.5f, EOcclusionTest.Regular);
            }
            else if (mat == EArmorMaterial.UHMWPE || mat == EArmorMaterial.Combined)
            {
                string audioClip = "uhmwpe_1.wav";
                if (dist >= distThreshold)
                {
                    audioClip = rndNum == 0 ? "impact_dist_1.wav" : rndNum == 1 ? "impact_dist_2.wav" : "impact_dist_4.wav";
                }
                else
                {
                    audioClip = rndNum == 0 ? "uhmwpe_1.wav" : rndNum == 1 ? "uhmwpe_2.wav" : "uhmwpe_3.wav";
                }

                Singleton<BetterAudio>.Instance.PlayAtPoint(pos, Plugin.HitAudioClips[audioClip], dist, BetterAudio.AudioSourceGroupType.Impacts, 100, dist >= distThreshold ? volDist : volClose, EOcclusionTest.Regular);
            }
            else if (mat == EArmorMaterial.Titan || mat == EArmorMaterial.ArmoredSteel)
            {
                string audioClip = "metal_1.wav";
                if (dist >= distThreshold)
                {
                    audioClip = rndNum == 0 ? "metal_dist_1.wav" : rndNum == 1 ? "metal_dist_2.wav" : "metal_dist_3.wav";
                }
                else
                {
                    audioClip = rndNum == 0 ? "metal_1.wav" : rndNum == 1 ? "metal_2.wav" : "metal_3.wav";
                }

                Singleton<BetterAudio>.Instance.PlayAtPoint(pos, Plugin.HitAudioClips[audioClip], dist, BetterAudio.AudioSourceGroupType.Impacts, 100, dist >= distThreshold ? volDist * 0.75f : volClose * 0.75f, EOcclusionTest.Regular);
            }
            else if (mat == EArmorMaterial.Glass)
            {
                string audioClip = "glass_1.wav";
                if (dist >= distThreshold)
                {
                    audioClip = rndNum == 0 ? "impact_dist_3.wav" : rndNum == 1 ? "impact_dist_4.wav" : "impact_dist_2.wav";
                }
                else
                {
                    audioClip = rndNum == 0 ? "glass_1.wav" : rndNum == 1 ? "glass_2.wav" : "glass_3.wav";
                }
                Singleton<BetterAudio>.Instance.PlayAtPoint(pos, Plugin.HitAudioClips[audioClip], dist, BetterAudio.AudioSourceGroupType.Impacts, 100, dist >= distThreshold ? volDist : volClose, EOcclusionTest.Regular);
            }
            else
            {
                string audioClip = "impact_1.wav";
                if (dist >= distThreshold)
                {
                    audioClip = rndNum == 0 ? "impact_dist_4.wav" : rndNum == 1 ? "impact_dist_1.wav" : "impact_dist_3.wav";
                }
                else
                {
                    audioClip = rndNum == 0 ? "impact_1.wav" : rndNum == 1 ? "impact_2.wav" : "impact_3.wav";
                }
                Singleton<BetterAudio>.Instance.PlayAtPoint(pos, Plugin.HitAudioClips[audioClip], dist, BetterAudio.AudioSourceGroupType.Impacts, 100, dist >= distThreshold ? volDist : volClose, EOcclusionTest.Regular);
            }
        }

  

        protected override MethodBase GetTargetMethod()
        {
            return typeof(ArmorComponent).GetMethod(nameof(ArmorComponent.ApplyDamage), BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPrefix]
        private static bool Prefix(ArmorComponent __instance, ref DamageInfo damageInfo, ref float __result, EBodyPartColliderType colliderType, EArmorPlateCollider armorPlateCollider, bool damageInfoIsLocal, List<ArmorComponent> armorComponents)
        {
            if (PluginConfig.EnableBallisticsLogging.Value)
            {
                Logger.LogWarning("===========ARMOR DAMAGE BEFORE=============== ");
                Logger.LogWarning("Pen " + damageInfo.PenetrationPower);
                Logger.LogWarning("Damage " + damageInfo.Damage);
                Logger.LogWarning("========================== ");
            }

            EDamageType damageType = damageInfo.DamageType;
            bool isHead = BallisticsController.ArmorHasSpecificColliders(__instance, BallisticsController.HeadCollidors);
            bool isSteelBodyArmor = __instance.Template.ArmorMaterial == EArmorMaterial.ArmoredSteel && !isHead;
            bool roundPenetrated = damageInfo.BlockedBy != __instance.Item.Id && damageInfo.DeflectedBy != __instance.Item.Id;
            float startingDamage = damageInfo.Damage;
            float speedFactor = 1f;
            float armorDamageActual = 1f;
            float momentum = 1f;

            if (!damageType.IsWeaponInduced() && damageType != EDamageType.GrenadeFragment)
            {
                __result = 0f;
                return false;
            }

            Player player = (damageInfo.Player != null) ? Singleton<GameWorld>.Instance.GetAlivePlayerByProfileID(damageInfo.Player.iPlayer.ProfileId) : null;
            if (player != null)
            {
                __instance.TryShatter(player, damageInfoIsLocal);
            }

            if (__instance.Repairable.Durability <= 0f && !isSteelBodyArmor)
            {
                __result = 0f;
                return false;
            }

            if (damageType == EDamageType.Sniper || damageType == EDamageType.Landmine)
            {
                return true;
            }

            //armor damage value has been replaced with velocity
            //ammotemplate is used to get stats needed for calcs and get original armor damage value.
            AmmoTemplate ammoTemp = null;
            if (damageType == EDamageType.Melee)
            {
                Weapon weap = damageInfo.Weapon as Weapon;
                bool isBayonet = !damageInfo.Player.IsAI && WeaponStats.HasBayonet && weap.WeapClass != "Knife" ? true : false;
                armorDamageActual = damageInfo.ArmorDamage;
                float meleeDamage = isBayonet ? damageInfo.Damage : damageInfo.Damage * 2f;
                momentum = meleeDamage * 100f;
            }
            else
            {
                ammoTemp = (AmmoTemplate)Singleton<ItemFactoryClass>.Instance.ItemTemplates[damageInfo.SourceId];

                armorDamageActual = ammoTemp.ArmorDamage; // * speedFactor don't think facotring by speedFacotr is a good idea anymore, momentum being based on velocity is enough
                if (damageInfo.ArmorDamage <= 1) //if damageinfo's armor damage is not equal to the velocity of the round, must be Fika
                {
                    momentum = ammoTemp.BulletMassGram * ammoTemp.InitialSpeed;
                }
                else 
                {
                    speedFactor = damageInfo.ArmorDamage / ammoTemp.InitialSpeed;
                    momentum = ammoTemp.BulletMassGram * damageInfo.ArmorDamage;
                }
            }

          
            if (ammoTemp != null && ammoTemp.ProjectileCount <= 2 && PluginConfig.EnableHitSounds.Value && damageInfo.HittedBallisticCollider != null)
            {
                bool isPlayer = __instance.Item.Owner.ID == Singleton<GameWorld>.Instance.MainPlayer.ProfileId;
                if (!isPlayer) 
                {
                    if (damageInfo.DeflectedBy == __instance.Item.Id)
                    {
                        playRicochetSound(damageInfo.HittedBallisticCollider.transform.position, UnityEngine.Random.Range(0, 2));
                    }
                    else
                    {
                        playArmorHitSound(__instance.Template.ArmorMaterial, damageInfo.HittedBallisticCollider.transform.position, isHead, UnityEngine.Random.Range(0, 2));
                    }
                }
            }

            if (damageInfo.DeflectedBy == __instance.Item.Id)
            {
                momentum *= 0.25f;
                armorDamageActual *= 0.25f;
                damageInfo.ArmorDamage *= 0.25f;
                damageInfo.PenetrationPower *= 0.25f;
            }

            float bluntThrput = __instance.Template.BluntThroughput;
            float softArmorStatReduction = 1f;
            EFTSlot slot;
            ArmorSlot softArmorSlot;
            if ((slot = (__instance.Item.CurrentAddress as EFTSlot)) != null && (softArmorSlot = (slot.Slot as ArmorSlot)) != null && softArmorSlot.BluntDamageReduceFromSoftArmor)
            {
                bluntThrput *= 0.6f;
                softArmorStatReduction = 0.7f;
            }

            float penPower = damageInfo.PenetrationPower;
            float factoredPen = penPower / 1.8f;
            float momentumFactor = Mathf.Log10(momentum) / 5;
            float momentumDamageFactor = (Mathf.Exp(momentumFactor * 12)) / 100;
            float duraPercent = __instance.Repairable.Durability / (float)__instance.Repairable.TemplateDurability;
            float scaledArmorclass = __instance.ArmorClass * __instance.ArmorClass;
            float armorDestructibility = Singleton<BackendConfigSettingsClass>.Instance.ArmorMaterials[__instance.Template.ArmorMaterial].Destructibility;
            if ((__instance.Template.ArmorMaterial == EArmorMaterial.ArmoredSteel || __instance.Template.ArmorMaterial == EArmorMaterial.Titan) && isHead) 
            {
                armorDestructibility = 0.25f;
            }
            float factoredArmorClass = Mathf.Clamp(scaledArmorclass * Mathf.Pow(duraPercent, 0.5f), 10f, scaledArmorclass);
            float armorFactorDura = Mathf.Clamp(1 - Mathf.InverseLerp(1f, 100f, factoredArmorClass), 0.1f, 1f);
            float armorFactorDamage = Mathf.Clamp(1 - Mathf.InverseLerp(1f, 100f, factoredArmorClass - factoredPen), 0.1f, 1f);
            float steelArmorFactorDamage = Mathf.Clamp(1 - Mathf.InverseLerp(1f, 100f, scaledArmorclass - factoredPen), 0.1f, 1f);

            float totaldamage = 1f; 
            if (__instance.Template.ArmorMaterial == EArmorMaterial.ArmoredSteel && !isHead)
            {
                totaldamage = Mathf.Clamp(momentumDamageFactor * steelArmorFactorDamage * bluntThrput, 0.4f, damageInfo.Damage);
            }
            else
            {
                totaldamage = Mathf.Clamp(momentumDamageFactor * armorFactorDamage * bluntThrput, 0.4f, damageInfo.Damage);
            }

            float totalDuraLoss = momentumDamageFactor * armorDestructibility * armorDamageActual * armorFactorDura;

            if (damageType == EDamageType.Melee)
            {
                if (damageInfo.PenetrationPower > __instance.ArmorClass * 100f * duraPercent)
                {
                    if (PluginConfig.EnableBallisticsLogging.Value) Logger.LogWarning("Melee Penetrated");
                    damageInfo.Damage *= 0.75f;
                    damageInfo.PenetrationPower *= 0.75f * softArmorStatReduction;
                }
                else
                {
                    if (PluginConfig.EnableBallisticsLogging.Value) Logger.LogWarning("Melee Blocked");
                    if (!isHead) damageInfo.Damage = totaldamage + (damageInfo.Damage / 10f);
                    else damageInfo.Damage = totaldamage;
                    damageInfo.HeavyBleedingDelta = 0f;
                    damageInfo.LightBleedingDelta = 0f;
                }
            }
            else if (roundPenetrated)
            {
                if (armorPlateCollider != (EArmorPlateCollider)0)
                {
                    damageInfo.Damage = 0f;
                }

                float actualDurability = Mathf.Max(__instance.Repairable.Durability - totalDuraLoss, 1);
                BallisticsController.CalcAfterPenStats(actualDurability, __instance.ArmorClass, __instance.Repairable.TemplateDurability, ref damageInfo.Damage, ref damageInfo.PenetrationPower, softArmorStatReduction);
            }
            else
            {
                damageInfo.Damage = totaldamage;
            }

            if (!roundPenetrated)
            {
                damageInfo.HeavyBleedingDelta = 0f;
                damageInfo.LightBleedingDelta = 0f;
            }

            damageInfo.StaminaBurnRate = (totaldamage / 100f) * 2f;
            totalDuraLoss = Math.Max(totalDuraLoss * PluginConfig.ArmorDurabilityModifier.Value, 0.1f);
            __instance.ApplyDurabilityDamage(totalDuraLoss, armorComponents);
            __result = totalDuraLoss;
            damageInfo.Damage = Mathf.Min(damageInfo.Damage, startingDamage);

            if (PluginConfig.EnableBallisticsLogging.Value)
            {
                Logger.LogWarning("===========ARMOR DAMAGE AFTER=============== ");
                Logger.LogWarning("Momentum " + momentum);
                Logger.LogWarning("Momentum Factor " + momentumDamageFactor);
                Logger.LogWarning("Pen " + damageInfo.PenetrationPower);
                Logger.LogWarning("Armor Damage " + armorDamageActual);
                Logger.LogWarning("Speed Factor " + speedFactor);
                Logger.LogWarning("Class " + __instance.ArmorClass);
                Logger.LogWarning("Throughput " + bluntThrput);
                Logger.LogWarning("Material " + __instance.Template.ArmorMaterial);
                Logger.LogWarning("Material Descructibility " + armorDestructibility);
                Logger.LogWarning("Dura percent " + duraPercent);
                Logger.LogWarning("armor Factor Damage = " + armorFactorDamage);
                Logger.LogWarning("armor Factor Dura = " + armorFactorDura);
                Logger.LogWarning("Durability Loss " + totalDuraLoss);
                Logger.LogWarning("Damage " + damageInfo.Damage);
                Logger.LogWarning("========================== ");
            }
            ammoTemp = null;
            return false;
        }

    }

    public class CreateShotPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(EFT.Ballistics.BallisticsCalculator).GetMethod("CreateShot", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static bool Prefix(EFT.Ballistics.BallisticsCalculator __instance, AmmoItemClass ammo, Vector3 origin, Vector3 direction, int fireIndex, string player, Item weapon, ref EftBulletClass __result, float speedFactor, int fragmentIndex = 0)
        {
            float speedFactorDamage = speedFactor > 1f ? Mathf.Pow(speedFactor, 4f) : speedFactor;
            float speedFactorPenetration = speedFactor > 1f ? Mathf.Pow(speedFactor, 3f) : speedFactor < 1f ? Mathf.Pow(speedFactor, 0.45f) : speedFactor;

            int randomNum = UnityEngine.Random.Range(0, 512);
            float velocityFactored = ammo.InitialSpeed * speedFactor;
            float penChanceFactored = ammo.PenetrationChanceObstacle * speedFactorPenetration;
            float damageFactored = ammo.Damage * speedFactorDamage;
            float fragchanceFactored = Mathf.Max(ammo.FragmentationChance * speedFactorDamage, 0);
            float penPowerFactored = EFT.Ballistics.BallisticsCalculator.GetAmmoPenetrationPower(ammo, randomNum, __instance.Randoms) * speedFactorPenetration;

            float bcSpeedFactor = 1f - ((1f - speedFactor) * 0.25f);
            float bcFactored = Mathf.Max(ammo.BallisticCoeficient * bcSpeedFactor, 0.01f);

            if (PluginConfig.EnableBallisticsLogging.Value) 
            {
                Logger.LogWarning("speed factor " + speedFactor);
                Logger.LogWarning("speed factored " + velocityFactored);
            }

            __result = EftBulletClass.Create(ammo, fragmentIndex, randomNum, origin, direction, velocityFactored, velocityFactored, ammo.BulletMassGram, ammo.BulletDiameterMilimeters, (float)damageFactored, penPowerFactored, penChanceFactored, ammo.RicochetChance, fragchanceFactored, 1f, ammo.MinFragmentsCount, ammo.MaxFragmentsCount, EFT.Ballistics.BallisticsCalculator.DefaultHitBody, __instance.Randoms, bcFactored, player, weapon, fireIndex, null);
            return false;

        }
    }

    public class ApplyCorpseImpulsePatch : ModulePatch
    {
        private static FieldInfo lastDamField;
        private static FieldInfo corpseField;
        private static FieldInfo corpseAppliedForceField;

        protected override MethodBase GetTargetMethod()
        {
            lastDamField = AccessTools.Field(typeof(Player), "LastDamageInfo");
            corpseField = AccessTools.Field(typeof(Player), "Corpse");
            corpseAppliedForceField = AccessTools.Field(typeof(Player), "_corpseAppliedForce");

            return typeof(Player).GetMethod("ApplyCorpseImpulse", BindingFlags.Instance | BindingFlags.Public); ;
        }

        [PatchPrefix]
        private static bool Prefix(Player __instance)
        {
            DamageInfo lastDam = (DamageInfo)lastDamField.GetValue(__instance);
            Corpse corpse = (Corpse)corpseField.GetValue(__instance);

            float force;
            if (lastDam.DamageType == EDamageType.Bullet)
            {
                AmmoTemplate ammoTemp = (AmmoTemplate)Singleton<ItemFactoryClass>.Instance.ItemTemplates[lastDam.SourceId];
                AmmoItemClass ammo = new AmmoItemClass(Utils.GenId(), ammoTemp);
                float KE = ((0.5f * ammo.BulletMassGram * lastDam.ArmorDamage * lastDam.ArmorDamage) / 1000);
                force = (-Mathf.Max(1f, KE / 1000f)) * PluginConfig.RagdollForceModifier.Value;
                ammo = null;
                ammoTemp = null;
            }
            else if (lastDam.DamageType == EDamageType.Explosion)
            {
                force = 150f;
            }
            else
            {
                force = 5f;
            }

            corpseAppliedForceField.SetValue(__instance, force);
            corpse.Ragdoll.ApplyImpulse(lastDam.HitCollider, lastDam.Direction, lastDam.HitPoint, force);

            return false;
        }
    }
}
