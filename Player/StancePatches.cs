using Aki.Reflection.Patching;
using EFT.InventoryLogic;
using EFT;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using static EFT.Player;
using UnityEngine;
using Comfort.Common;
using System.Linq;
using EFT.Ballistics;
using System.ComponentModel;
using Random = System.Random;
using CastingClass = GClass646;
using HackShotResult = GClass1673;
using CollisionLayerClass = GClass2987;
using EFT.Animations;
using ChartAndGraph;
/*using LightStruct = GStruct155;*/

namespace RealismMod
{
  /*  public class LaserLateUpdatePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(LaserBeam).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static bool Prefix(LaserBeam __instance)
        {
            if (Utils.IsReady)
            {
                if ((StanceController.IsHighReady || StanceController.IsLowReady) && !Plugin.IsAiming)
                {
                    Vector3 playerPos = Singleton<GameWorld>.Instance.AllAlivePlayersList[0].Transform.position;
                    Vector3 lightPos = __instance.gameObject.transform.position;
                    float distanceFromPlayer = Vector3.Distance(lightPos, playerPos);
                    if (distanceFromPlayer <= 1.8f)
                    {
                        return false;
                    }
                    return true;
                }
                return true;
            }
            else
            {
                return true;
            }
        }
    }
*/
    public class OnWeaponDrawPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(SkillManager).GetMethod("OnWeaponDraw", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(SkillManager __instance, Item item)
        {
            if (item?.Owner?.ID != null && item.Owner.ID == Singleton<GameWorld>.Instance.MainPlayer.ProfileId)
            {
                StanceController.DidWeaponSwap = true;
            }
        }
    }

    public class SetFireModePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(FirearmsAnimator).GetMethod("SetFireMode", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static bool Prefix(FirearmsAnimator __instance, Weapon.EFireMode fireMode, bool skipAnimation = false)
        {
            __instance.ResetLeftHand();
            skipAnimation = StanceController.IsHighReady && PlayerStats.IsSprinting ? true : skipAnimation;
            WeaponAnimationSpeedControllerClass.SetFireMode(__instance.Animator, (float)fireMode);
            if (!skipAnimation)
            {
                WeaponAnimationSpeedControllerClass.TriggerFiremodeSwitch(__instance.Animator);
            }
            return false;
        }
    }

    public class WeaponLengthPatch : ModulePatch
    {
        private static FieldInfo playerField;
        private static FieldInfo weapLn;

        protected override MethodBase GetTargetMethod()
        {
            playerField = AccessTools.Field(typeof(EFT.Player.FirearmController), "_player");
            weapLn = AccessTools.Field(typeof(EFT.Player.FirearmController), "WeaponLn");
            return typeof(Player.FirearmController).GetMethod("method_7", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(Player.FirearmController __instance)
        {
            Player player = (Player)playerField.GetValue(__instance);
            float length = (float)weapLn.GetValue(__instance);
            if (player.IsYourPlayer)
            {
                WeaponStats.BaseWeaponLength = length;
                WeaponStats.NewWeaponLength = length >= 0.9f ? length * 1.15f : length;
            }
        }
    }

    public class OperateStationaryWeaponPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player).GetMethod("OperateStationaryWeapon", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(Player __instance)
        {
            if (__instance.IsYourPlayer)
            {
                StanceController.CancelAllStances();
                StanceController.StanceBlender.Target = 0f;
                StanceController.StanceTargetPosition = Vector3.zero;

            }
        }
    }

    public class CollisionPatch : ModulePatch
    {
        private static FieldInfo playerField;
        private static FieldInfo hitIgnoreField;

        private static int timer = 0;
        private static MaterialType[] allowedMats = { MaterialType.Helmet, MaterialType.BodyArmor, MaterialType.Body, MaterialType.Glass, MaterialType.GlassShattered, MaterialType.GlassVisor };

        private static Vector3 startLeftDir = new Vector3(0.1f, 0f, 0f);
        private static Vector3 startRightDir = new Vector3(-0.1f, 0f, 0f);
        private static Vector3 startDownDir = new Vector3(0f, 0f, -0.12f);


        private static Vector3 wiggleLeftDir = new Vector3(2.5f, 7.5f, -10f);
        private static Vector3 wiggleRightDir = new Vector3(2.5f, -7.5f, -10f);
        private static Vector3 wiggleDownDir = new Vector3(7.5f, 2.5f, -10f);

        private static Vector3 offsetLeftDir = new Vector3(0.005f, 0, 0f);
        private static Vector3 offsetRightDir = new Vector3(-0.005f, 0, 0);
        private static Vector3 offsetDownDir = new Vector3(0, -0.005f, 0);

        private static void setMountingStatus(EBracingDirection coverDir, string weapClass)
        {
            if (!StanceController.IsMounting)
            {
                StanceController.BracingDirection = coverDir;
            }
            StanceController.IsBracing = true;
        }

        private static Vector3 getWiggleDir(EBracingDirection coverDir) 
        {
            {
                switch(coverDir) 
                {
                    case EBracingDirection.Right:
                        return wiggleRightDir;
                    case EBracingDirection.Left:
                        return wiggleLeftDir;
                    case EBracingDirection.Top:
                        return wiggleDownDir;
                    default: return Vector3.zero;
                }
            }
        }

        private static Vector3 getCoverOffset(EBracingDirection coverDir)
        {
            {
                switch (coverDir)
                {
                    case EBracingDirection.Right:
                        return offsetRightDir;
                    case EBracingDirection.Left:
                        return offsetLeftDir;
                    case EBracingDirection.Top:
                        return offsetDownDir;
                    default: return Vector3.zero;
                }
            }
        }

        private static bool checkForCoverCollision(EBracingDirection coverDir, Vector3 start, Vector3 direction, out RaycastHit raycastHit, RaycastHit[] raycastArr, Func<RaycastHit, bool> isHitIgnoreTest, string weapClass)
        {
            if (CastingClass.Linecast(start, direction, out raycastHit, EFTHardSettings.Instance.WEAPON_OCCLUSION_LAYERS, false, raycastArr, isHitIgnoreTest))
            {
                setMountingStatus(coverDir, weapClass);
                StanceController.CoverWiggleDirection = getWiggleDir(coverDir);
                StanceController.CoverOffset = getCoverOffset(coverDir);
                return true;
            }
            return false;
        }

        private static void doMelee(Player.FirearmController fc, float ln, Player player)
        {
            if (!PlayerStats.IsSprinting && StanceController.IsMeleeAttack && StanceController.CanDoMeleeDetection)
            {
                Transform weapTransform = player.ProceduralWeaponAnimation.HandsContainer.WeaponRootAnim;
                RaycastHit[] raycastArr = AccessTools.StaticFieldRefAccess<EFT.Player.FirearmController, RaycastHit[]>("raycastHit_0");
                Func<RaycastHit, bool> isHitIgnoreTest = (Func<RaycastHit, bool>)hitIgnoreField.GetValue(fc);
                Vector3 linecastDirection = weapTransform.TransformDirection(Vector3.up);
                Vector3 startMeleeDir = new Vector3(0, -0.5f, -0.025f); 
                Vector3 meleeStart = weapTransform.position + weapTransform.TransformDirection(startMeleeDir);
                Vector3 meleeDir = meleeStart - linecastDirection * (ln - (WeaponStats.HasBayonet ? 0.1f : 0.25f));

                if (Plugin.EnableLogging.Value) 
                {
                    DebugGizmos.SingleObjects.Line(meleeStart, meleeDir, Color.red, 0.02f, true, 0.3f, true);
                }

                BallisticCollider hitBalls = null;
                RaycastHit raycastHit;
                if (CastingClass.Linecast(meleeStart, meleeDir, out raycastHit, CollisionLayerClass.HitMask, false, raycastArr, isHitIgnoreTest))
                {
                    Collider col = raycastHit.collider;
                    BaseBallistic baseballComp = col.GetComponent<BaseBallistic>();
                    if (baseballComp != null)
                    {
                        hitBalls = baseballComp.Get(raycastHit.point);
                    }
                    float damage = 8f + WeaponStats.BaseMeleeDamage * (1f + player.Skills.StrengthBuffMeleePowerInc) * (1f - (WeaponStats.ErgoFactor / 300f));
                    damage = player.Physical.HandsStamina.Exhausted ? damage * Singleton<BackendConfigSettingsClass>.Instance.Stamina.ExhaustedMeleeDamageMultiplier : damage;
                    float pen = 15f + WeaponStats.BaseMeleePen * (1f - (WeaponStats.ErgoFactor / 300f));
                    bool shouldSkipHit = false;

                    if (hitBalls as BodyPartCollider != null)
                    {
                        player.Skills.FistfightAction.Complete(1f);
                    }

                    if (hitBalls.TypeOfMaterial == MaterialType.Glass || hitBalls.TypeOfMaterial == MaterialType.GlassShattered)
                    {
                        Random rnd = new Random();
                        int rndNum = rnd.Next(1, 10);
                        if (rndNum > (4f + WeaponStats.BaseMeleeDamage))
                        {
                            shouldSkipHit = true;
                        }
                    }

                    if (WeaponStats.HasBayonet || (allowedMats.Contains(hitBalls.TypeOfMaterial) && !shouldSkipHit))
                    {
                        Vector3 position = fc.CurrentFireport.position;
                        Vector3 vector = fc.WeaponDirection;
                        Vector3 shotPosition = position;
                        fc.AdjustShotVectors(ref shotPosition, ref vector);
                        Vector3 shotDirection = vector;
                        DamageInfo damageInfo = new DamageInfo
                        {
                            DamageType = EDamageType.Melee,
                            Damage = damage,
                            PenetrationPower = pen,
                            ArmorDamage = 10f + (damage / 10f),
                            Direction = shotDirection.normalized,
                            HitCollider = col,
                            HitPoint = raycastHit.point,
                            Player = Singleton<GameWorld>.Instance.GetAlivePlayerBridgeByProfileID(player.ProfileId),
                            HittedBallisticCollider = hitBalls,
                            HitNormal = raycastHit.normal,
                            Weapon = fc.Item as Item,
                            IsForwardHit = true,
                            StaminaBurnRate = 5f
                        };
                        HackShotResult result = Singleton<GameWorld>.Instance.HackShot(damageInfo);
                    }
                    float vol = WeaponStats.HasBayonet ? 10f : 12f;
                    Singleton<BetterAudio>.Instance.PlayDropItem(baseballComp.SurfaceSound, JsonType.EItemDropSoundType.Rifle, raycastHit.point, vol);
/*                  StanceController.DoWiggleEffects(player, player.ProceduralWeaponAnimation, fc, new Vector3(-10f, 10f, 0f), true, 1.5f);
*/                  player.Physical.ConsumeAsMelee(0.3f + (WeaponStats.ErgoFactor / 100f));

                    StanceController.CanDoMeleeDetection = false;
                    return;
                }
            }
        }

        protected override MethodBase GetTargetMethod()
        {
            playerField = AccessTools.Field(typeof(EFT.Player.FirearmController), "_player");
            hitIgnoreField = AccessTools.Field(typeof(EFT.Player.FirearmController), "func_2");

            return typeof(Player.FirearmController).GetMethod("method_8", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static void PatchPrefix(Player.FirearmController __instance, Vector3 origin, float ln, Vector3? weaponUp = null)
        {
            Player player = (Player)playerField.GetValue(__instance);
            if (player.IsYourPlayer)
            {
                doMelee(__instance, ln, player);
  
                timer += 1;
                if (timer >= 60)
                {
                    timer = 0;
                    RaycastHit[] raycastArr = AccessTools.StaticFieldRefAccess<EFT.Player.FirearmController, RaycastHit[]>("raycastHit_0");
                    Func<RaycastHit, bool> isHitIgnoreTest = (Func<RaycastHit, bool>)hitIgnoreField.GetValue(__instance);
                    Transform weapTransform = player.ProceduralWeaponAnimation.HandsContainer.WeaponRootAnim;
                    Vector3 linecastDirection = weapTransform.TransformDirection(Vector3.up);

                    string weapClass = __instance.Item.WeapClass;

                    Vector3 startDown = weapTransform.position + weapTransform.TransformDirection(startDownDir);
                    Vector3 startLeft = weapTransform.position + weapTransform.TransformDirection(startLeftDir);
                    Vector3 startRight = weapTransform.position + weapTransform.TransformDirection(startRightDir);

                    Vector3 forwardDirection = startDown - linecastDirection * ln;
                    Vector3 leftDirection = startLeft - linecastDirection * ln;
                    Vector3 rightDirection = startRight - linecastDirection * ln;

                    /*                    DebugGizmos.SingleObjects.Line(startDown, forwardDirection, Color.red, 0.02f, true, 0.3f, true);
                                        DebugGizmos.SingleObjects.Line(startLeft, leftDirection, Color.green, 0.02f, true, 0.3f, true);
                                        DebugGizmos.SingleObjects.Line(startRight, rightDirection, Color.yellow, 0.02f, true, 0.3f, true);*/
        
                    RaycastHit raycastHit;

                    if (checkForCoverCollision(EBracingDirection.Top, startDown, forwardDirection, out raycastHit, raycastArr, isHitIgnoreTest, weapClass) ||
                        checkForCoverCollision(EBracingDirection.Left, startLeft, leftDirection, out raycastHit, raycastArr, isHitIgnoreTest, weapClass) ||
                        checkForCoverCollision(EBracingDirection.Right, startRight, rightDirection, out raycastHit, raycastArr, isHitIgnoreTest, weapClass)) 
                    {
                        return;
                    }

                    StanceController.IsBracing = false;
                }

                if (StanceController.IsBracing) 
                {
                    float mountOrientationBonus = StanceController.BracingDirection == EBracingDirection.Top ? 0.75f : 1f;
                    float mountingRecoilLimit = __instance.Item.WeapClass == "pistol" ? 0.1f : 0.65f;

                    if (!StanceController.BlockBraceSwayBonus)
                    {
                        StanceController.BracingSwayBonus = Mathf.Lerp(StanceController.BracingSwayBonus, 0.75f * mountOrientationBonus, 0.04f);
                    }
                    StanceController.BracingRecoilBonus = Mathf.Lerp(StanceController.BracingRecoilBonus, 0.85f * mountOrientationBonus, 0.04f);
                    StanceController.MountingRecoilBonus = Mathf.Clamp(mountingRecoilLimit * mountOrientationBonus, 0.1f, 1f);
                    StanceController.MountingSwayBonus = Mathf.Lerp(StanceController.MountingSwayBonus, 0.5f * mountOrientationBonus, 0.1f);
                }
                else 
                {
                    if (!StanceController.BlockBraceSwayBonus)
                    {
                        StanceController.BracingSwayBonus = Mathf.Lerp(StanceController.BracingSwayBonus, 1f, 0.05f);
                    }

                    StanceController.BracingRecoilBonus = Mathf.Lerp(StanceController.BracingRecoilBonus, 1f, 0.05f);
                }
            }
        }
    }

    public class WeaponOverlapViewPatch : ModulePatch
    {
        private static FieldInfo playerField;

        protected override MethodBase GetTargetMethod()
        {
            playerField = AccessTools.Field(typeof(EFT.Player.FirearmController), "_player");
            return typeof(Player.FirearmController).GetMethod("WeaponOverlapView", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static bool PatchPrefix(Player.FirearmController __instance)
        {
            Player player = (Player)playerField.GetValue(__instance);

            if (player.IsYourPlayer && StanceController.IsMounting)
            {
                return false;
            }
            return true;
        }
    }

    public class WeaponOverlappingPatch : ModulePatch
    {
        private static FieldInfo playerField;
        private static FieldInfo weaponLnField;

        protected override MethodBase GetTargetMethod()
        {
            playerField = AccessTools.Field(typeof(EFT.Player.FirearmController), "_player");
            weaponLnField = AccessTools.Field(typeof(EFT.Player.FirearmController), "WeaponLn");
            return typeof(Player.FirearmController).GetMethod("WeaponOverlapping", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static void Prefix(Player.FirearmController __instance)
        {
            Player player = (Player)playerField.GetValue(__instance);

            if (player.IsYourPlayer)
            {
                if (__instance.Item.WeapClass == "pistol")
                {
                    if (StanceController.PistolIsCompressed)
                    {
                        weaponLnField.SetValue(__instance, WeaponStats.NewWeaponLength * 0.75f);
                    }
                    else
                    {
                        weaponLnField.SetValue(__instance, WeaponStats.NewWeaponLength * 0.85f);
                    }
                    return;
                }
                else
                {
                    if (StanceController.IsHighReady || StanceController.IsLowReady || StanceController.IsShortStock)
                    {
                        weaponLnField.SetValue(__instance, WeaponStats.NewWeaponLength * 0.8f);
                        return;
                    }
                    if (StanceController.WasShortStock && StanceController.IsAiming)
                    {
                        weaponLnField.SetValue(__instance, WeaponStats.NewWeaponLength * 0.7f);
                        return;
                    }
                }
                weaponLnField.SetValue(__instance, WeaponStats.NewWeaponLength);
                return;
            }
        }
    }


    public class InitTransformsPatch : ModulePatch
    {
        private static FieldInfo playerField;
        private static FieldInfo fcField;

        protected override MethodBase GetTargetMethod()
        {
            playerField = AccessTools.Field(typeof(EFT.Player.FirearmController), "_player");
            fcField = AccessTools.Field(typeof(ProceduralWeaponAnimation), "_firearmController");
            return typeof(EFT.Animations.ProceduralWeaponAnimation).GetMethod("InitTransforms", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(EFT.Animations.ProceduralWeaponAnimation __instance)
        {
            FirearmController firearmController = (FirearmController)fcField.GetValue(__instance);
            if (firearmController == null)
            {
                return;
            }
            Player player = (Player)playerField.GetValue(firearmController);
            if (player != null && player.MovementContext.CurrentState.Name != EPlayerState.Stationary && player.IsYourPlayer)
            {
                StanceController.WeaponOffsetPosition = __instance.HandsContainer.WeaponRoot.localPosition += new Vector3(Plugin.WeapOffsetX.Value, Plugin.WeapOffsetY.Value, Plugin.WeapOffsetZ.Value);
                __instance.HandsContainer.WeaponRoot.localPosition += new Vector3(Plugin.WeapOffsetX.Value, Plugin.WeapOffsetY.Value, Plugin.WeapOffsetZ.Value);
            }
        }
    }


    public class ZeroAdjustmentsPatch : ModulePatch
    {
        private static FieldInfo blindfireStrengthField;
        private static FieldInfo blindfireRotationField;
        private static PropertyInfo overlappingBlindfireField;
        private static FieldInfo blindfirePositionField;
        private static FieldInfo firearmControllerField;
        private static FieldInfo playerField;

        private static Vector3 targetPosition = Vector3.zero;

        protected override MethodBase GetTargetMethod()
        {
            blindfireStrengthField = AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "_blindfireStrength");
            blindfireRotationField = AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "_blindFireRotation");
            overlappingBlindfireField = AccessTools.Property(typeof(EFT.Animations.ProceduralWeaponAnimation), "Single_3");
            blindfirePositionField = AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "_blindFirePosition");
            firearmControllerField = AccessTools.Field(typeof(ProceduralWeaponAnimation), "_firearmController");
            playerField = AccessTools.Field(typeof(FirearmController), "_player");

            return typeof(EFT.Animations.ProceduralWeaponAnimation).GetMethod("ZeroAdjustments", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static bool PatchPrefix(EFT.Animations.ProceduralWeaponAnimation __instance)
        {
            FirearmController firearmController = (FirearmController)firearmControllerField.GetValue(__instance);
            if (firearmController == null)
            {
                return true;
            }
            Player player = (Player)playerField.GetValue(firearmController);
            if (player != null && player.IsYourPlayer) // player.MovementContext.CurrentState.Name != EPlayerState.Stationary && player.IsYourPlayer
            {
                float collidingModifier = (float)overlappingBlindfireField.GetValue(__instance);
                Vector3 blindfirePosition = (Vector3)blindfirePositionField.GetValue(__instance);

                __instance.PositionZeroSum.y = (__instance._shouldMoveWeaponCloser ? 0.05f : 0f);
                __instance.RotationZeroSum.y = __instance.SmoothedTilt * __instance.PossibleTilt;
                float stanceBlendValue = StanceController.StanceBlender.Value;
                float blindFireBlendValue = __instance.BlindfireBlender.Value;
                if (Mathf.Abs(blindFireBlendValue) > 0f)
                {
                    StanceController.IsBlindFiring = true;
                    float strength = ((Mathf.Abs(__instance.Pitch) < 45f) ? 1f : ((90f - Mathf.Abs(__instance.Pitch)) / 45f));
                    blindfireStrengthField.SetValue(__instance, strength);
                    __instance.BlindFireEndPosition = ((blindFireBlendValue > 0f) ? __instance.BlindFireOffset : __instance.SideFireOffset);
                    __instance.BlindFireEndPosition *= strength;
                }
                else 
                {
                    StanceController.IsBlindFiring = false;
                    blindfirePositionField.SetValue(__instance, Vector3.zero);
                    blindfireRotationField.SetValue(__instance, Vector3.zero);
                }

    
                if (Mathf.Abs(stanceBlendValue) > 0f)
                {
                    float strength = ((Mathf.Abs(__instance.Pitch) < 45f) ? 1f : ((90f - Mathf.Abs(__instance.Pitch)) / 45f));
                    blindfireStrengthField.SetValue(__instance, strength);
                    targetPosition = StanceController.StanceTargetPosition * stanceBlendValue;
                    __instance.HandsContainer.HandsPosition.Zero = __instance.PositionZeroSum + (float)blindfireStrengthField.GetValue(__instance) * targetPosition;
                    __instance.HandsContainer.HandsRotation.Zero = __instance.RotationZeroSum;
                    return false;
                }
                else
                {
                    targetPosition = Vector3.zero;
                }

                __instance.HandsContainer.HandsPosition.Zero = __instance.PositionZeroSum + targetPosition + (Vector3)blindfirePositionField.GetValue(__instance) * (float)blindfireStrengthField.GetValue(__instance) * collidingModifier;
                __instance.HandsContainer.HandsRotation.Zero = __instance.RotationZeroSum;
                return false;
            }
            return true;
        }
    }

    public class SetTiltPatch : ModulePatch
    {
        private static FieldInfo movementContextField;
        private static FieldInfo playerField;

        protected override MethodBase GetTargetMethod()
        {
            movementContextField = AccessTools.Field(typeof(MovementState), "MovementContext");
            playerField = AccessTools.Field(typeof(MovementContext), "_player");
            return typeof(MovementState).GetMethod("SetTilt", BindingFlags.Instance | BindingFlags.Public);
        }

        public static float currentTilt = 0f;
        public static float currentPoseLevel = 0f;
        public static bool wasProne = false;

        [PatchPrefix]
        private static void Prefix(MovementState __instance, float tilt)
        {
            MovementContext movementContext = (MovementContext)movementContextField.GetValue(__instance);
            Player player = (Player)playerField.GetValue(movementContext);

            if (player.IsYourPlayer)
            {
                if (!StanceController.IsMounting)
                {
                    currentTilt = tilt;
                    currentPoseLevel = movementContext.PoseLevel;
                    wasProne = movementContext.IsInPronePose;
                }

                if (currentTilt != tilt || currentPoseLevel != movementContext.PoseLevel || !movementContext.IsGrounded || wasProne != movementContext.IsInPronePose)
                {
                    StanceController.IsMounting = false;
                }
            }
        }
    }

    public class ApplySimpleRotationPatch : ModulePatch
    {
        private static FieldInfo aimSpeedField;
        private static FieldInfo blindFireStrength;
        private static FieldInfo aimingQuatField;
        private static FieldInfo weapRotationField;
        private static FieldInfo isAimingField;
        private static FieldInfo weaponPositionField;
        private static FieldInfo currentRotationField;
        private static FieldInfo firearmControllerField;
        private static FieldInfo playerField;

        private static bool hasResetActiveAim = true;
        private static bool hasResetLowReady = true;
        private static bool hasResetHighReady = true;
        private static bool hasResetShortStock = true;
        private static bool hasResetPistolPos = true;
        private static bool hasResetMelee = true;

        private static bool isResettingActiveAim = false;
        private static bool isResettingLowReady = false;
        private static bool isResettingHighReady = false;
        private static bool isResettingShortStock = false;
        private static bool isResettingPistol = false;
        private static bool isResettingMelee = false;

        private static Quaternion currentRotation = Quaternion.identity;
        private static Quaternion stanceRotation = Quaternion.identity;
        private static Vector3 mountWeapPosition = Vector3.zero;

        private static Vector3 lowReadyTargetRotation = new Vector3(18.0f, 5.0f, -1.0f);
        private static Quaternion lowReadyTargetQuaternion = Quaternion.Euler(lowReadyTargetRotation);
        private static Vector3 lowReadyTargetPostion = new Vector3(0.06f, 0.04f, 0.0f);
        private static Vector3 highReadyTargetRotation = new Vector3(-15.0f, 3.0f, 3.0f);
        private static Quaternion highReadyTargetQuaternion = Quaternion.Euler(highReadyTargetRotation);
        private static Vector3 highReadyTargetPostion = new Vector3(0.05f, 0.1f, -0.12f);
        private static Vector3 activeAimTargetRotation = new Vector3(0.0f, -40.0f, 0.0f);
        private static Quaternion activeAimTargetQuaternion = Quaternion.Euler(activeAimTargetRotation);
        private static Vector3 activeAimTargetPostion = new Vector3(0.0f, 0.0f, 0.0f);
        private static Vector3 shortStockTargetRotation = new Vector3(0.0f, -28.0f, 0.0f);
        private static Quaternion shortStockTargetQuaternion = Quaternion.Euler(shortStockTargetRotation);
        private static Vector3 shortStockTargetPostion = new Vector3(0.05f, 0.18f, -0.2f);
        private static Vector3 tacPistolTargetRotation = new Vector3(0.0f, -20.0f, 0.0f);
        private static Quaternion tacPistolTargetQuaternion = Quaternion.Euler(tacPistolTargetRotation);
        private static Vector3 tacPistolTargetPosition = new Vector3(-0.1f, 0.1f, -0.05f);
        private static Vector3 normalPistolTargetRotation = new Vector3(0f, -5.0f, 0.0f);
        private static Quaternion normalPistolTargetQuaternion = Quaternion.Euler(normalPistolTargetRotation);
        private static Vector3 normalPistolTargetPosition = new Vector3(-0.05f, 0.0f, 0.0f);

        private static float stanceRotationSpeed = 1f;

        protected override MethodBase GetTargetMethod()
        {
            aimSpeedField = AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "_aimingSpeed");
            blindFireStrength = AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "_blindfireStrength");
            weaponPositionField = AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "_temporaryPosition");
            aimingQuatField = AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "_targetScopeRotation");
            weapRotationField = AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "_temporaryRotation");
            isAimingField = AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "_isAiming");
            currentRotationField = AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "_cameraIdenity");
            firearmControllerField = AccessTools.Field(typeof(ProceduralWeaponAnimation), "_firearmController");
            playerField = AccessTools.Field(typeof(FirearmController), "_player");

            return typeof(EFT.Animations.ProceduralWeaponAnimation).GetMethod("ApplySimpleRotation", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void Postfix(EFT.Animations.ProceduralWeaponAnimation __instance, float dt)
        {
            FirearmController firearmController = (FirearmController)firearmControllerField.GetValue(__instance);
            if (firearmController == null)
            {
                return;
            }
            Player player = (Player)playerField.GetValue(firearmController);
            if (player != null)
            {
                float pitch = (float)blindFireStrength.GetValue(__instance);
                Quaternion aimingQuat = (Quaternion)aimingQuatField.GetValue(__instance);
                Vector3 weaponPosition = (Vector3)weaponPositionField.GetValue(__instance);
                Quaternion weapRotation = (Quaternion)weapRotationField.GetValue(__instance);

                if (player.IsYourPlayer)
                {
                    StanceController.IsInThirdPerson = true;

                    float aimSpeed = (float)aimSpeedField.GetValue(__instance);
                    bool isAiming = (bool)isAimingField.GetValue(__instance);

                    bool isPistol = firearmController.Weapon.WeapClass == "pistol";
                    bool allStancesReset = hasResetActiveAim && hasResetLowReady && hasResetHighReady && hasResetShortStock && hasResetPistolPos;
                    bool isInStance = StanceController.IsHighReady || StanceController.IsLowReady || StanceController.IsShortStock || StanceController.IsActiveAiming || StanceController.IsMeleeAttack;
                    bool isInShootableStance = StanceController.IsShortStock || StanceController.IsActiveAiming || isPistol || StanceController.IsMeleeAttack;
                    bool cancelBecauseShooting = StanceController.IsFiringFromStance && !isInShootableStance;
                    bool doStanceRotation = (isInStance || !allStancesReset || StanceController.PistolIsCompressed) && !cancelBecauseShooting;
                    bool allowActiveAimReload = Plugin.ActiveAimReload.Value && PlayerStats.IsInReloadOpertation && !PlayerStats.IsAttemptingToReloadInternalMag && !PlayerStats.IsQuickReloading;
                    bool cancelStance = (StanceController.CancelActiveAim && StanceController.IsActiveAiming && !allowActiveAimReload) || (StanceController.CancelHighReady && StanceController.IsHighReady) || (StanceController.CancelLowReady && StanceController.IsLowReady) || (StanceController.CancelShortStock && StanceController.IsShortStock) || (StanceController.CancelPistolStance && StanceController.PistolIsCompressed);

                    StanceController.DoMounting(player, __instance, firearmController, ref weaponPosition, ref mountWeapPosition, dt, __instance.HandsContainer.WeaponRoot.position);
                    weaponPositionField.SetValue(__instance, weaponPosition);

                    currentRotation = Quaternion.Slerp(currentRotation, __instance.IsAiming && allStancesReset ? aimingQuat : doStanceRotation ? stanceRotation : Quaternion.identity, doStanceRotation ? stanceRotationSpeed * Plugin.StanceRotationSpeedMulti.Value : __instance.IsAiming ? 8f * aimSpeed * dt : 8f * dt);

                    __instance.HandsContainer.WeaponRootAnim.SetPositionAndRotation(weaponPosition, weapRotation * currentRotation);

                    if (isPistol && !WeaponStats.HasShoulderContact && Plugin.EnableAltPistol.Value && !StanceController.IsPatrolStance)
                    {
                        if (StanceController.PistolIsCompressed && !StanceController.IsAiming && !isResettingPistol && !StanceController.IsBlindFiring)
                        {
                            StanceController.StanceBlender.Target = 1f;
                        }
                        else
                        {
                            StanceController.StanceBlender.Target = 0f;
                        }

                        if ((!StanceController.PistolIsCompressed && !StanceController.IsAiming && !isResettingPistol) || (StanceController.IsBlindFiring))
                        {
                            StanceController.StanceTargetPosition = Vector3.Lerp(StanceController.StanceTargetPosition, Vector3.zero, 5f * dt);
                        }

                        hasResetActiveAim = true;
                        hasResetHighReady = true;
                        hasResetLowReady = true;
                        hasResetShortStock = true;
                        StanceController.DoPistolStances(true, __instance, ref stanceRotation, dt, ref hasResetPistolPos, player, ref stanceRotationSpeed, ref isResettingPistol, firearmController);
                    }
                    else if (!isPistol || WeaponStats.HasShoulderContact)
                    {
                        if ((!isInStance && allStancesReset) || (cancelBecauseShooting && !isInShootableStance) || StanceController.IsAiming || cancelStance || StanceController.IsBlindFiring)
                        {
                            StanceController.StanceBlender.Target = 0f;
                        }
                        else if (isInStance)
                        {
                            StanceController.StanceBlender.Target = 1f;
                        }

                        if (((!isInStance && allStancesReset) && !cancelBecauseShooting && !StanceController.IsAiming) || (StanceController.IsBlindFiring))
                        {
                            StanceController.StanceTargetPosition = Vector3.Lerp(StanceController.StanceTargetPosition, Vector3.zero, 5f * dt);
                        }

                        hasResetPistolPos = true;
                        StanceController.DoRifleStances(player, firearmController, true, __instance, ref stanceRotation, dt, ref isResettingShortStock, ref hasResetShortStock, ref hasResetLowReady, ref hasResetActiveAim, ref hasResetHighReady, ref isResettingHighReady, ref isResettingLowReady, ref isResettingActiveAim, ref stanceRotationSpeed, ref hasResetMelee, ref isResettingMelee);
                    }

                    StanceController.HasResetActiveAim = hasResetActiveAim;
                    StanceController.HasResetHighReady = hasResetHighReady;
                    StanceController.HasResetLowReady = hasResetLowReady;
                    StanceController.HasResetShortStock = hasResetShortStock;
                    StanceController.HasResetPistolPos = hasResetPistolPos;
                    StanceController.HasResetMelee = hasResetMelee;

                }
                else if (player.IsAI)
                {
                    Quaternion targetRotation = Quaternion.identity;
                    Quaternion currentRotation = (Quaternion)currentRotationField.GetValue(__instance);
                    aimSpeedField.SetValue(__instance, 1f);

                    FaceShieldComponent fsComponent = player.FaceShieldObserver.Component;
                    NightVisionComponent nvgComponent = player.NightVisionObserver.Component;
                    bool nvgIsOn = nvgComponent != null && (nvgComponent.Togglable == null || nvgComponent.Togglable.On);
                    bool fsIsON = fsComponent != null && (fsComponent.Togglable == null || fsComponent.Togglable.On);

                    float lastDistance = player.AIData.BotOwner.AimingData.LastDist2Target;
                    Vector3 distanceVect = player.AIData.BotOwner.AimingData.RealTargetPoint - player.AIData.BotOwner.MyHead.position;
                    float realDistance = distanceVect.magnitude;

                    bool isTacBot = StanceController.botsToUseTacticalStances.Contains(player.AIData.BotOwner.Profile.Info.Settings.Role.ToString());
                    bool isPeace = player.AIData.BotOwner.Memory.IsPeace;
                    bool notShooting = !player.AIData.BotOwner.ShootData.Shooting && Time.time - player.AIData.BotOwner.ShootData.LastTriggerPressd > 15f;
                    bool isInStance = false;
                    float stanceSpeed = 1f;

                    ////peaceful positon//// (player.AIData.BotOwner.Memory.IsPeace == true && !StanceController.botsToUseTacticalStances.Contains(player.AIData.BotOwner.Profile.Info.Settings.Role.ToString()) && !player.IsSprintEnabled && !__instance.IsAiming && !player.AIData.BotOwner.ShootData.Shooting && (Time.time - player.AIData.BotOwner.ShootData.LastTriggerPressd) > 20f)

                    if (player.AIData.BotOwner.GetPlayer.MovementContext.BlindFire == 0)
                    {
                        if (isPeace && !player.IsSprintEnabled && player.MovementContext.StationaryWeapon == null && !__instance.IsAiming && !firearmController.IsInReloadOperation() && !firearmController.IsInventoryOpen() && !firearmController.IsInInteractionStrictCheck() && !firearmController.IsInSpawnOperation() && !firearmController.IsHandsProcessing()) // && player.AIData.BotOwner.WeaponManager.IsWeaponReady &&  player.AIData.BotOwner.WeaponManager.InIdleState()
                        {
                            isInStance = true;
                            player.MovementContext.SetPatrol(true);
                        }
                        else
                        {
                            player.MovementContext.SetPatrol(false);
                            if (firearmController.Weapon.WeapClass != "pistol")
                            {
                                ////low ready//// 
                                if (!isTacBot && !firearmController.IsInReloadOperation() && !player.IsSprintEnabled && !__instance.IsAiming && notShooting && (lastDistance >= 25f || lastDistance == 0f))    // (Time.time - player.AIData.BotOwner.Memory.LastEnemyTimeSeen) > 1f
                                {
                                    isInStance = true;
                                    stanceSpeed = 4f * dt * 3f;
                                    targetRotation = lowReadyTargetQuaternion;
                                    __instance.HandsContainer.HandsPosition.Zero = __instance.PositionZeroSum + pitch * lowReadyTargetPostion;
                                }

                                ////high ready////
                                if (isTacBot && !firearmController.IsInReloadOperation() && !__instance.IsAiming && notShooting && (lastDistance >= 25f || lastDistance == 0f))
                                {
                                    isInStance = true;
                                    player.BodyAnimatorCommon.SetFloat(PlayerAnimator.WEAPON_SIZE_MODIFIER_PARAM_HASH, 2);
                                    stanceSpeed = 4f * dt * 2.7f;
                                    targetRotation = highReadyTargetQuaternion;
                                    __instance.HandsContainer.HandsPosition.Zero = __instance.PositionZeroSum + pitch * highReadyTargetPostion;
                                }
                                else
                                {
                                    player.BodyAnimatorCommon.SetFloat(PlayerAnimator.WEAPON_SIZE_MODIFIER_PARAM_HASH, (float)firearmController.Item.CalculateCellSize().X);
                                }

                                ///active aim//// 
                                if (isTacBot && (((nvgIsOn || fsIsON) && !player.IsSprintEnabled && !firearmController.IsInReloadOperation() && lastDistance < 25f && lastDistance > 2f && lastDistance != 0f) || (__instance.IsAiming && (nvgIsOn && __instance.CurrentScope.IsOptic || fsIsON))))
                                {
                                    isInStance = true;
                                    stanceSpeed = 4f * dt * 1.5f;
                                    targetRotation = activeAimTargetQuaternion;
                                    __instance.HandsContainer.HandsPosition.Zero = __instance.PositionZeroSum + pitch * activeAimTargetPostion;
                                }

                                ///short stock//// 
                                if (isTacBot && !player.IsSprintEnabled && !firearmController.IsInReloadOperation() && lastDistance <= 2f && lastDistance != 0f)
                                {
                                    isInStance = true;
                                    stanceSpeed = 4f * dt * 3f;
                                    targetRotation = shortStockTargetQuaternion;
                                    __instance.HandsContainer.HandsPosition.Zero = __instance.PositionZeroSum + pitch * shortStockTargetPostion;
                                }
                            }
                            else
                            {
                                if (!isTacBot && !player.IsSprintEnabled && !__instance.IsAiming && notShooting)
                                {
                                    isInStance = true;
                                    stanceSpeed = 4f * dt * 1.5f;
                                    targetRotation = normalPistolTargetQuaternion;
                                    __instance.HandsContainer.HandsPosition.Zero = __instance.PositionZeroSum + pitch * normalPistolTargetPosition;
                                }

                                if (isTacBot && !player.IsSprintEnabled && !__instance.IsAiming && notShooting)
                                {
                                    isInStance = true;
                                    stanceSpeed = 4f * dt * 1.5f;
                                    targetRotation = tacPistolTargetQuaternion;
                                    __instance.HandsContainer.HandsPosition.Zero = __instance.PositionZeroSum + pitch * tacPistolTargetPosition;
                                }
                            }
                        }
                    }

                    currentRotation = Quaternion.Slerp(currentRotation, __instance.IsAiming && !isInStance ? aimingQuat : isInStance ? targetRotation : Quaternion.identity, isInStance ? stanceSpeed : 8f * dt);
                    __instance.HandsContainer.WeaponRootAnim.SetPositionAndRotation(weaponPosition, weapRotation * currentRotation);
                    currentRotationField.SetValue(__instance, currentRotation);
                }
            }
        }
    }

    public class ApplyComplexRotationPatch : ModulePatch
    {
        private static FieldInfo aimSpeedField;
        private static FieldInfo fovScaleField;
        private static FieldInfo displacementStrField;
        private static FieldInfo aimingQuatField;
        private static FieldInfo weapTempRotationField;
        private static FieldInfo weapTempPositionField;
        private static FieldInfo isAimingField;
        private static FieldInfo firearmControllerField;
        private static FieldInfo playerField;

        private static bool hasResetActiveAim = true;
        private static bool hasResetLowReady = true;
        private static bool hasResetHighReady = true;
        private static bool hasResetShortStock = true;
        private static bool hasResetPistolPos = true;
        private static bool hasResetMelee = true;

        private static bool isResettingActiveAim = false;
        private static bool isResettingLowReady = false;
        private static bool isResettingHighReady = false;
        private static bool isResettingShortStock = false;
        private static bool isResettingPistol = false;
        private static bool isResettingMelee = false;

        private static Quaternion currentRotation = Quaternion.identity;
        private static Quaternion stanceRotation = Quaternion.identity;
        private static Vector3 mountWeapPosition = Vector3.zero;
        private static Vector3 currentRecoil = Vector3.zero;
        private static Vector3 targetRecoil = Vector3.zero;

        private static float stanceRotationSpeed = 1f;

        protected override MethodBase GetTargetMethod()
        {
            aimSpeedField = AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "_aimingSpeed");
            fovScaleField = AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "_compensatoryScale");
            displacementStrField = AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "_displacementStr");
            aimingQuatField = AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "_targetScopeRotation");
            weapTempPositionField = AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "_temporaryPosition");
            weapTempRotationField = AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "_temporaryRotation");
            isAimingField = AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "_isAiming");
            firearmControllerField = AccessTools.Field(typeof(ProceduralWeaponAnimation), "_firearmController");
            playerField = AccessTools.Field(typeof(FirearmController), "_player");

            return typeof(EFT.Animations.ProceduralWeaponAnimation).GetMethod("ApplyComplexRotation", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void Postfix(EFT.Animations.ProceduralWeaponAnimation __instance, float dt)
        {
            FirearmController firearmController = (FirearmController)firearmControllerField.GetValue(__instance);
            if (firearmController == null)
            {
                return;
            }
            Player player = (Player)playerField.GetValue(firearmController);
            if (player != null && player.IsYourPlayer)
            {
                FirearmController fc = player.HandsController as FirearmController;

                StanceController.IsInThirdPerson = false;

                float aimSpeed = (float)aimSpeedField.GetValue(__instance);
                float fovScale = (float)fovScaleField.GetValue(__instance);
                float displacementStr = (float)displacementStrField.GetValue(__instance);
                Quaternion aimingQuat = (Quaternion)aimingQuatField.GetValue(__instance);
                Vector3 weapTempPosition = (Vector3)weapTempPositionField.GetValue(__instance);
                Quaternion weapTempRotation = (Quaternion)weapTempRotationField.GetValue(__instance);
                bool isAiming = (bool)isAimingField.GetValue(__instance);

                Vector3 handsRotation = __instance.HandsContainer.HandsRotation.Get();
                Vector3 sway = __instance.HandsContainer.SwaySpring.Value;
                handsRotation += displacementStr * (isAiming ? __instance.AimingDisplacementStr : 1f) * new Vector3(sway.x, 0f, sway.z);
                handsRotation += sway;
                Vector3 rotationCenter = __instance._shouldMoveWeaponCloser ? __instance.HandsContainer.RotationCenterWoStock : __instance.HandsContainer.RotationCenter;
                Vector3 weapRootPivot = __instance.HandsContainer.WeaponRootAnim.TransformPoint(rotationCenter);

                StanceController.DoMounting(player, __instance, fc, ref weapTempPosition, ref mountWeapPosition, dt, __instance.HandsContainer.WeaponRoot.position);
                weapTempPositionField.SetValue(__instance, weapTempPosition);

                __instance.DeferredRotateWithCustomOrder(__instance.HandsContainer.WeaponRootAnim, weapRootPivot, handsRotation);
                weapTempPosition = (Vector3)weapTempPositionField.GetValue(__instance);

                Vector3 recoilVector = __instance.Shootingg.CurrentRecoilEffect.GetHandRotationRecoil();
                if (recoilVector.magnitude > 1E-45f)
                {
                    if (fovScale < 1f && __instance.ShotNeedsFovAdjustments)
                    {
                        recoilVector.x = Mathf.Atan(Mathf.Tan(recoilVector.x * 0.017453292f) * fovScale) * 57.29578f;
                        recoilVector.z = Mathf.Atan(Mathf.Tan(recoilVector.z * 0.017453292f) * fovScale) * 57.29578f;
                    }
                    Vector3 recoilPivot = weapTempPosition + weapTempRotation * __instance.HandsContainer.RecoilPivot;
                    __instance.DeferredRotate(__instance.HandsContainer.WeaponRootAnim, recoilPivot, weapTempRotation * recoilVector);
                    weapTempPosition = (Vector3)weapTempPositionField.GetValue(__instance);
                }

                __instance.ApplyAimingAlignment(dt);

                bool isPistol = fc.Item.WeapClass == "pistol";
                bool allStancesReset = hasResetActiveAim && hasResetLowReady && hasResetHighReady && hasResetShortStock && hasResetPistolPos;
                bool isInStance = StanceController.IsHighReady || StanceController.IsLowReady || StanceController.IsShortStock || StanceController.IsActiveAiming || StanceController.IsMeleeAttack;
                bool isInShootableStance = StanceController.IsShortStock || StanceController.IsActiveAiming || isPistol || StanceController.IsMeleeAttack;
                bool cancelBecauseShooting = StanceController.IsFiringFromStance && !isInShootableStance;
                bool doStanceRotation = (isInStance || !allStancesReset || StanceController.PistolIsCompressed) && !cancelBecauseShooting;
                bool allowActiveAimReload = Plugin.ActiveAimReload.Value && PlayerStats.IsInReloadOpertation && !PlayerStats.IsAttemptingToReloadInternalMag && !PlayerStats.IsQuickReloading;
                bool cancelStance = (StanceController.CancelActiveAim && StanceController.IsActiveAiming && !allowActiveAimReload) || (StanceController.CancelHighReady && StanceController.IsHighReady) || (StanceController.CancelLowReady && StanceController.IsLowReady) || (StanceController.CancelShortStock && StanceController.IsShortStock) || (StanceController.CancelPistolStance && StanceController.PistolIsCompressed);

                currentRotation = Quaternion.Slerp(currentRotation, __instance.IsAiming && allStancesReset ? aimingQuat : doStanceRotation ? stanceRotation : Quaternion.identity, doStanceRotation ? stanceRotationSpeed * Plugin.StanceRotationSpeedMulti.Value : __instance.IsAiming ? 8f * aimSpeed * dt : 8f * dt);

                RecoilController.DoVisualRecoil(ref targetRecoil, ref currentRecoil, ref weapTempRotation, Logger);

                __instance.HandsContainer.WeaponRootAnim.SetPositionAndRotation(weapTempPosition, weapTempRotation * currentRotation);

                if (isPistol && !StanceController.IsPatrolStance)
                {
                    if (StanceController.PistolIsCompressed && !StanceController.IsAiming && !isResettingPistol && !StanceController.IsBlindFiring && !__instance.LeftStance)
                    {
                        StanceController.StanceBlender.Target = 1f;
                    }
                    else
                    {
                        StanceController.StanceBlender.Target = 0f;
                    }

                    if ((!StanceController.PistolIsCompressed && !StanceController.IsAiming && !isResettingPistol) || StanceController.IsBlindFiring || __instance.LeftStance)
                    {
                        StanceController.StanceTargetPosition = Vector3.Lerp(StanceController.StanceTargetPosition, Vector3.zero, 5f * dt);
                    }

                    hasResetActiveAim = true;
                    hasResetHighReady = true;
                    hasResetLowReady = true;
                    hasResetShortStock = true;
                    StanceController.DoPistolStances(false, __instance, ref stanceRotation, dt, ref hasResetPistolPos, player, ref stanceRotationSpeed, ref isResettingPistol, fc);
                }
                else if (!isPistol || WeaponStats.HasShoulderContact)
                {
                    if ((!isInStance && allStancesReset) || (cancelBecauseShooting && !isInShootableStance) || StanceController.IsAiming || cancelStance || StanceController.IsBlindFiring || __instance.LeftStance)
                    {
                        StanceController.StanceBlender.Target = 0f;
                    }
                    else if (isInStance)
                    {
                        StanceController.StanceBlender.Target = 1f;
                    }

                    if (((!isInStance && allStancesReset) && !cancelBecauseShooting && !StanceController.IsAiming) || StanceController.IsBlindFiring || __instance.LeftStance)
                    {
                        StanceController.StanceTargetPosition = Vector3.Lerp(StanceController.StanceTargetPosition, Vector3.zero, 5f * dt);
                    }

                    hasResetPistolPos = true;
                    StanceController.DoRifleStances(player, fc, false, __instance, ref stanceRotation, dt, ref isResettingShortStock, ref hasResetShortStock, ref hasResetLowReady, ref hasResetActiveAim, ref hasResetHighReady, ref isResettingHighReady, ref isResettingLowReady, ref isResettingActiveAim, ref stanceRotationSpeed, ref hasResetMelee, ref isResettingMelee);
                }

                StanceController.HasResetActiveAim = hasResetActiveAim;
                StanceController.HasResetHighReady = hasResetHighReady;
                StanceController.HasResetLowReady = hasResetLowReady;
                StanceController.HasResetShortStock = hasResetShortStock;
                StanceController.HasResetPistolPos = hasResetPistolPos;
                StanceController.HasResetMelee = hasResetMelee;
            }
        }
    }
}
