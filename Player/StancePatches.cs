using SPT.Reflection.Patching;
using SPT.Reflection.Utils;
using Comfort.Common;
using EFT;
using EFT.Animations;
using EFT.Ballistics;
using EFT.InventoryLogic;
using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static EFT.Player;
using CollisionLayerClass = GClass3008;
/*using LightStruct = GStruct155;*/

namespace RealismMod
{
    public class MountingPatch : ModulePatch
    {
        private static FieldInfo playerField;
        private static FieldInfo fcField;
        private static float mountClamp = 0f;

        protected override MethodBase GetTargetMethod()
        {
            playerField = AccessTools.Field(typeof(FirearmController), "_player");
            fcField = AccessTools.Field(typeof(ProceduralWeaponAnimation), "_firearmController");

            return typeof(ProceduralWeaponAnimation).GetMethod("AvoidObstacles", BindingFlags.Instance | BindingFlags.Public);
        }



        [PatchPostfix]
        private static void PatchPostfix(ProceduralWeaponAnimation __instance)
        {
            FirearmController firearmController = (FirearmController)fcField.GetValue(__instance);
            if (firearmController == null)
            {
                return;
            }

            Player player = (Player)playerField.GetValue(firearmController);
            if (player != null && player.IsYourPlayer && player.MovementContext.CurrentState.Name != EPlayerState.Stationary)
            {
                if (StanceController.IsMounting)
                {
                    mountClamp = Mathf.Lerp(mountClamp, 2.5f, 0.1f);
                }
                else
                {
                    mountClamp = Mathf.Lerp(mountClamp, 0f, 0.1f);
                }

                StanceController.MountingPivotUpdate(player, __instance, mountClamp, StanceController.GetDeltaTime());
            }
        }
    }



    public class MuzzleSmokePatch : ModulePatch
    {
        private static Vector3 target = Vector3.zero;
        protected override MethodBase GetTargetMethod()
        {
            return typeof(MuzzleSmoke).GetMethod("LateUpdateValues", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static void Prefix(MuzzleSmoke __instance)
        {
            if (WeaponStats._WeapClass == "pistol" && (!WeaponStats.HasShoulderContact || (Plugin.WeapOffsetX.Value != 0f && WeaponStats.HasShoulderContact)))
            {
                target = new Vector3(-0.2f, -0.2f, -0.2f);
            }
            else
            {
                target = new Vector3(0f, 0f, -0.2f);
            }

            Transform transform = (Transform)AccessTools.Field(typeof(MuzzleSmoke), "transform_0").GetValue(__instance);
            Vector3 pos = (Vector3)AccessTools.Field(typeof(MuzzleSmoke), "vector3_0").GetValue(__instance);
            pos = Vector3.Slerp(pos, transform.position + target, 0.125f); // left/right, up/down, in/out
            AccessTools.Field(typeof(MuzzleSmoke), "vector3_0").SetValue(__instance, pos);
        }
    }

    public class OnWeaponDrawPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(SkillManager).GetMethod("OnWeaponDraw", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(SkillManager __instance, Item item)
        {
            if (item?.Owner?.ID != null && item.Owner.ID == Singleton<GameWorld>.Instance.MainPlayer.ProfileId && StanceController.CurrentStance == EStance.PistolCompressed)
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
            skipAnimation = StanceController.CurrentStance == EStance.HighReady && PlayerState.IsSprinting ? true : skipAnimation;
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
                WeaponStats.NewWeaponLength = length >= 0.9f ? length * 1.1f : length;
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
        private static FieldInfo _playerField;
        private static FieldInfo _hitIgnoreField;

        private static int _timer = 0;
        private static MaterialType[] _allowedMats = { MaterialType.Helmet, MaterialType.BodyArmor, MaterialType.Body, MaterialType.Glass, MaterialType.GlassShattered, MaterialType.GlassVisor };

        private static Vector3 _startLeftDir = new Vector3(0.12f, 0f, 0f);
        private static Vector3 _startRightDir = new Vector3(-0.12f, 0f, 0f);
        private static Vector3 _startDownDir = new Vector3(0f, 0f, -0.19f);

        private static Vector3 _wiggleLeftDir = new Vector3(2.5f, 7.5f, -5) * 0.5f;
        private static Vector3 _wiggleRightDir = new Vector3(2.5f, -7.5f, -5f) * 0.5f;
        private static Vector3 _wiggleDownDir = new Vector3(7.5f, 2.5f, -5f) * 0.5f;

        private static Vector3 _offsetLeftDir = new Vector3(0.004f, 0, 0f);
        private static Vector3 _offsetRightDir = new Vector3(-0.004f, 0, 0);
        private static Vector3 _offsetDownDir = new Vector3(0, -0.004f, 0);

        private static void SetMountingStatus(EBracingDirection coverDir, string weapClass)
        {
            if (!StanceController.IsMounting)
            {
                StanceController.BracingDirection = coverDir;
            }
            StanceController.IsBracing = true;
        }

        private static Vector3 GetWiggleDir(EBracingDirection coverDir) 
        {
            {
                switch(coverDir) 
                {
                    case EBracingDirection.Right:
                        return _wiggleRightDir;
                    case EBracingDirection.Left:
                        return _wiggleLeftDir;
                    case EBracingDirection.Top:
                        return _wiggleDownDir;
                    default: return Vector3.zero;
                }
            }
        }

        private static Vector3 GetCoverOffset(EBracingDirection coverDir)
        {
            {
                switch (coverDir)
                {
                    case EBracingDirection.Right:
                        return _offsetRightDir;
                    case EBracingDirection.Left:
                        return _offsetLeftDir;
                    case EBracingDirection.Top:
                        return _offsetDownDir;
                    default: return Vector3.zero;
                }
            }
        }

        private static bool CheckForCoverCollision(EBracingDirection coverDir, Vector3 start, Vector3 direction, out RaycastHit raycastHit, RaycastHit[] raycastArr, Func<RaycastHit, bool> isHitIgnoreTest, string weapClass)
        {
            if (EFTPhysicsClass.Linecast(start, direction, out raycastHit, EFTHardSettings.Instance.WEAPON_OCCLUSION_LAYERS, false, raycastArr, isHitIgnoreTest))
            {
                SetMountingStatus(coverDir, weapClass);
                StanceController.CoverWiggleDirection = GetWiggleDir(coverDir);
                return true;
            }
            return false;
        }

        private static void DoMelee(Player.FirearmController fc, float ln, Player player)
        {
            if (!PlayerState.IsSprinting && StanceController.CurrentStance == EStance.Melee && StanceController.CanDoMeleeDetection && !StanceController.MeleeHitSomething)
            {
                Transform weapTransform = player.ProceduralWeaponAnimation.HandsContainer.WeaponRootAnim;
                RaycastHit[] raycastArr = AccessTools.StaticFieldRefAccess<EFT.Player.FirearmController, RaycastHit[]>("raycastHit_0");
                Func<RaycastHit, bool> isHitIgnoreTest = (Func<RaycastHit, bool>)_hitIgnoreField.GetValue(fc);
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
                if (EFTPhysicsClass.Linecast(meleeStart, meleeDir, out raycastHit, CollisionLayerClass.HitMask, false, raycastArr, isHitIgnoreTest))
                {
                    Collider col = raycastHit.collider;
                    BaseBallistic baseballComp = col.GetComponent<BaseBallistic>();
                    if (baseballComp != null)
                    {
                        hitBalls = baseballComp.Get(raycastHit.point);
                    }
                    float weaponWeight = fc.Weapon.GetSingleItemTotalWeight();
                    float damage = 8f + WeaponStats.BaseMeleeDamage * (1f + player.Skills.StrengthBuffMeleePowerInc) * (1f + (weaponWeight / 10f));
                    damage = player.Physical.HandsStamina.Exhausted ? damage * Singleton<BackendConfigSettingsClass>.Instance.Stamina.ExhaustedMeleeDamageMultiplier : damage;
                    float pen = 15f + WeaponStats.BaseMeleePen * (1f + (weaponWeight / 10f));
                    bool shouldSkipHit = false;

                    if (hitBalls as BodyPartCollider != null)
                    {
                        player.ExecuteSkill(new Action(() => player.Skills.FistfightAction.Complete(1f)));
                    }

                    if (hitBalls.TypeOfMaterial == MaterialType.Glass || hitBalls.TypeOfMaterial == MaterialType.GlassShattered)
                    {
                        int rndNum = UnityEngine.Random.Range(1, 11);
                        if (rndNum > (4f + WeaponStats.BaseMeleeDamage))
                        {
                            shouldSkipHit = true;
                        }
                    }

                    if (WeaponStats.HasBayonet || (_allowedMats.Contains(hitBalls.TypeOfMaterial) && !shouldSkipHit))
                    {
                        Vector3 position = fc.CurrentFireport.position;
                        Vector3 vector = fc.WeaponDirection;
                        Vector3 shotPosition = position;
                        fc.AdjustShotVectors(ref shotPosition, ref vector);
                        Vector3 shotDirection = vector;
                        DamageInfo damageInfo = new DamageInfo
                        {
                            SourceId = fc.Weapon.Id,
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
                        ShotInfoClass result = Singleton<GameWorld>.Instance.HackShot(damageInfo);
                    }
                    float vol = WeaponStats.HasBayonet ? 10f : 12f;
                    Singleton<BetterAudio>.Instance.PlayDropItem(baseballComp.SurfaceSound, JsonType.EItemDropSoundType.Rifle, raycastHit.point, vol);
/*                  StanceController.DoWiggleEffects(player, player.ProceduralWeaponAnimation, fc, new Vector3(-10f, 10f, 0f), true, 1.5f);
*/                  player.Physical.ConsumeAsMelee(0.3f + (weaponWeight / 10f));

                    StanceController.CanDoMeleeDetection = false;
                    StanceController.MeleeHitSomething = true;
                    return;
                }
            }
        }

        protected override MethodBase GetTargetMethod()
        {
            _playerField = AccessTools.Field(typeof(EFT.Player.FirearmController), "_player");
            _hitIgnoreField = AccessTools.Field(typeof(EFT.Player.FirearmController), "func_2");

            return typeof(Player.FirearmController).GetMethod("method_8", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static void PatchPrefix(Player.FirearmController __instance, Vector3 origin, float ln, Vector3? weaponUp = null)
        {
            Player player = (Player)_playerField.GetValue(__instance);
            if (player.IsYourPlayer)
            {
                DoMelee(__instance, ln, player);
  
                _timer += 1;
                if (_timer >= 60)
                {
                    _timer = 0;
                    RaycastHit[] raycastArr = AccessTools.StaticFieldRefAccess<EFT.Player.FirearmController, RaycastHit[]>("raycastHit_0");
                    Func<RaycastHit, bool> isHitIgnoreTest = (Func<RaycastHit, bool>)_hitIgnoreField.GetValue(__instance);
                    Transform weapTransform = player.ProceduralWeaponAnimation.HandsContainer.WeaponRootAnim;
                    Vector3 linecastDirection = weapTransform.TransformDirection(Vector3.up);

                    string weapClass = __instance.Item.WeapClass;

                    Vector3 downDir = WeaponStats.HasBipod ? new Vector3(_startDownDir.x, _startDownDir.y, _startDownDir.z + -0.15f) : _startDownDir;

                    Vector3 startDown = weapTransform.position + weapTransform.TransformDirection(downDir);
                    Vector3 startLeft = weapTransform.position + weapTransform.TransformDirection(_startLeftDir);
                    Vector3 startRight = weapTransform.position + weapTransform.TransformDirection(_startRightDir);

                    Vector3 forwardDirection = startDown - linecastDirection * ln;
                    Vector3 leftDirection = startLeft - linecastDirection * ln;
                    Vector3 rightDirection = startRight - linecastDirection * ln;

                    /*                    DebugGizmos.SingleObjects.Line(startDown, forwardDirection, Color.red, 0.02f, true, 0.3f, true);
                                        DebugGizmos.SingleObjects.Line(startLeft, leftDirection, Color.green, 0.02f, true, 0.3f, true);
                                        DebugGizmos.SingleObjects.Line(startRight, rightDirection, Color.yellow, 0.02f, true, 0.3f, true);*/
        
                    RaycastHit raycastHit;

                    if (CheckForCoverCollision(EBracingDirection.Top, startDown, forwardDirection, out raycastHit, raycastArr, isHitIgnoreTest, weapClass) ||
                        CheckForCoverCollision(EBracingDirection.Left, startLeft, leftDirection, out raycastHit, raycastArr, isHitIgnoreTest, weapClass) ||
                        CheckForCoverCollision(EBracingDirection.Right, startRight, rightDirection, out raycastHit, raycastArr, isHitIgnoreTest, weapClass)) 
                    {
                        return;
                    }

                    StanceController.IsBracing = false;
                }

                if (StanceController.IsBracing) 
                {
                    float mountOrientationBonus = StanceController.BracingDirection == EBracingDirection.Top ? 0.75f : 1f;
                    float mountingRecoilLimit = WeaponStats.IsStocklessPistol ? 0.25f : 0.75f;
                    float recoilBonus = StanceController.IsMounting && __instance.Weapon.IsBeltMachineGun ? 0.6f : StanceController.IsMounting ? 0.8f : 0.95f;
                    recoilBonus = StanceController.IsMounting && WeaponStats.HasBipod ? recoilBonus * 0.8f : recoilBonus;
                    float swayBonus = StanceController.IsMounting ? 0.35f : 0.65f;
                    swayBonus = StanceController.IsMounting && WeaponStats.HasBipod ? swayBonus * 0.8f : swayBonus;

                    StanceController.BracingRecoilBonus = Mathf.Lerp(StanceController.BracingRecoilBonus, recoilBonus * mountOrientationBonus, 0.04f);
                    StanceController.BracingSwayBonus = Mathf.Lerp(StanceController.BracingSwayBonus, swayBonus * mountOrientationBonus, 0.04f);
                }
                else
                {
                    StanceController.BracingSwayBonus = Mathf.Lerp(StanceController.BracingSwayBonus, 1f, 0.05f);
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
                    if (StanceController.CurrentStance == EStance.PistolCompressed)
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
                    if (Plugin.IsUsingFika) //collisions acts funky with stances from another client's perspective
                    {
                        weaponLnField.SetValue(__instance, WeaponStats.NewWeaponLength * 0.7f);
                        return;
                    }
                    if (StanceController.CurrentStance == EStance.HighReady || StanceController.CurrentStance == EStance.LowReady || StanceController.CurrentStance == EStance.ShortStock)
                    {
                        weaponLnField.SetValue(__instance, WeaponStats.NewWeaponLength * 0.8f);
                        return;
                    }
                    if (StanceController.StoredStance == EStance.ShortStock && StanceController.IsAiming)
                    {
                        weaponLnField.SetValue(__instance, WeaponStats.NewWeaponLength * 0.75f);
                        return;
                    }
                    if (StanceController.IsAiming)
                    {
                        weaponLnField.SetValue(__instance, WeaponStats.NewWeaponLength * 0.85f);
                        return;
                    }
                }
                weaponLnField.SetValue(__instance, WeaponStats.NewWeaponLength * 0.95f);
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
            if (firearmController == null) return;
            Player player = (Player)playerField.GetValue(firearmController);
            if (player != null && player.MovementContext.CurrentState.Name != EPlayerState.Stationary && player.IsYourPlayer)
            {
                Vector3 baseOffset = StanceController.GetWeaponOffsets().TryGetValue(firearmController.Weapon.TemplateId, out Vector3 offset) ? offset : Vector3.zero;
                __instance.HandsContainer.WeaponRoot.localPosition += new Vector3(Plugin.WeapOffsetX.Value, Plugin.WeapOffsetY.Value, Plugin.WeapOffsetZ.Value) + baseOffset;
                StanceController.WeaponOffsetPosition = __instance.HandsContainer.WeaponRoot.localPosition += new Vector3(Plugin.WeapOffsetX.Value, Plugin.WeapOffsetY.Value, Plugin.WeapOffsetZ.Value) + baseOffset;
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

    public class ChangePosePatch : ModulePatch
    {
        private static FieldInfo movementContextField;
        private static FieldInfo playerField;

        protected override MethodBase GetTargetMethod()
        {
            movementContextField = AccessTools.Field(typeof(MovementState), "MovementContext");
            playerField = AccessTools.Field(typeof(MovementContext), "_player");
            return typeof(MovementState).GetMethod("ChangePose", BindingFlags.Instance | BindingFlags.Public);
        }


        [PatchPrefix]
        private static void Prefix(MovementState __instance)
        {
            MovementContext movementContext = (MovementContext)movementContextField.GetValue(__instance);
            Player player = (Player)playerField.GetValue(movementContext);

            if (player.IsYourPlayer)
            {
                StanceController.IsMounting = false;
            }
        }
    }


    public class SetTiltPatch : ModulePatch
    {
        private static FieldInfo movementContextField;
        private static FieldInfo playerField;
        public static float tiltBeforeMount = 0f;

        protected override MethodBase GetTargetMethod()
        {
            movementContextField = AccessTools.Field(typeof(MovementState), "MovementContext");
            playerField = AccessTools.Field(typeof(MovementContext), "_player");
            return typeof(MovementState).GetMethod("SetTilt", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static void Prefix(MovementState __instance, float tilt)
        {
            MovementContext movementContext = (MovementContext)movementContextField.GetValue(__instance);
            Player player = (Player)playerField.GetValue(movementContext);

            if (player.IsYourPlayer)
            {
                if (!StanceController.IsMounting)
                {
                    tiltBeforeMount = tilt;
                }
                else if (Math.Abs(tiltBeforeMount - tilt) > 2.5f)
                {
                    StanceController.IsMounting = false;
                    tiltBeforeMount = 0f;
                }
            }
        }
    }

    public class ApplySimpleRotationPatch : ModulePatch
    {
        private static FieldInfo aimSpeedField;
        private static FieldInfo blindFireStrength;
        private static FieldInfo scopeRotationField;
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
        private static bool didHalfMeleeAnim = false;

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
            scopeRotationField = AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "_targetScopeRotation");
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
            if (player != null && player.MovementContext.CurrentState.Name != EPlayerState.Stationary)
            {
                float pitch = (float)blindFireStrength.GetValue(__instance);
                Quaternion scopeRotation = (Quaternion)scopeRotationField.GetValue(__instance);
                Vector3 weaponPosition = (Vector3)weaponPositionField.GetValue(__instance);
                Quaternion weapRotation = (Quaternion)weapRotationField.GetValue(__instance);

                if (player.IsYourPlayer)
                {
                    StanceController.IsInThirdPerson = true;

                    float aimSpeed = (float)aimSpeedField.GetValue(__instance);
                    bool isAiming = (bool)isAimingField.GetValue(__instance);

                    bool allStancesReset = hasResetActiveAim && hasResetLowReady && hasResetHighReady && hasResetShortStock && hasResetPistolPos;
                    bool isInStance = 
                        StanceController.CurrentStance == EStance.HighReady || 
                        StanceController.CurrentStance == EStance.LowReady || 
                        StanceController.CurrentStance == EStance.ShortStock || 
                        StanceController.CurrentStance == EStance.ActiveAiming ||
                        StanceController.CurrentStance == EStance.Melee;
                    bool isInShootableStance = 
                        StanceController.CurrentStance == EStance.ShortStock || 
                        StanceController.CurrentStance == EStance.ActiveAiming ||
                        WeaponStats.IsStocklessPistol || 
                        StanceController.CurrentStance == EStance.Melee;
                    bool cancelBecauseShooting = StanceController.IsFiringFromStance && !isInShootableStance;
                    bool doStanceRotation = (isInStance || !allStancesReset || StanceController.CurrentStance == EStance.PistolCompressed) && !cancelBecauseShooting;
                    bool allowActiveAimReload = Plugin.ActiveAimReload.Value && PlayerState.IsInReloadOpertation && !PlayerState.IsAttemptingToReloadInternalMag && !PlayerState.IsQuickReloading;
                    bool cancelStance = 
                        (StanceController.CancelActiveAim && StanceController.CurrentStance == EStance.ActiveAiming && !allowActiveAimReload) || 
                        (StanceController.CancelHighReady && StanceController.CurrentStance == EStance.HighReady) ||
                        (StanceController.CancelLowReady && StanceController.CurrentStance == EStance.LowReady) || 
                        (StanceController.CancelShortStock && StanceController.CurrentStance == EStance.ShortStock); //|| (StanceController.CancelPistolStance && StanceController.PistolIsCompressed)

                    currentRotation = Quaternion.Slerp(currentRotation, __instance.IsAiming && allStancesReset ? scopeRotation : doStanceRotation ? stanceRotation : Quaternion.identity, doStanceRotation ? stanceRotationSpeed * Plugin.StanceRotationSpeedMulti.Value : __instance.IsAiming ? 8f * aimSpeed * dt : 8f * dt);

                    __instance.HandsContainer.WeaponRootAnim.SetPositionAndRotation(weaponPosition, weapRotation * currentRotation);

                    if (WeaponStats.IsStocklessPistol && Plugin.EnableAltPistol.Value && StanceController.CurrentStance != EStance.PatrolStance)
                    {
                        if (StanceController.CurrentStance == EStance.PistolCompressed && !StanceController.IsAiming && !isResettingPistol && !StanceController.IsBlindFiring)
                        {
                            StanceController.StanceBlender.Target = 1f;
                        }
                        else
                        {
                            StanceController.StanceBlender.Target = 0f;
                        }

                        if ((StanceController.CurrentStance != EStance.PistolCompressed && !StanceController.IsAiming && !isResettingPistol) || (StanceController.IsBlindFiring))
                        {
                            StanceController.StanceTargetPosition = Vector3.Lerp(StanceController.StanceTargetPosition, Vector3.zero, 5f * dt);
                        }

                        hasResetActiveAim = true;
                        hasResetHighReady = true;
                        hasResetLowReady = true;
                        hasResetShortStock = true;
                        StanceController.DoPistolStances(true, __instance, ref stanceRotation, dt, ref hasResetPistolPos, player, ref stanceRotationSpeed, ref isResettingPistol, firearmController);
                    }
                    else if (!WeaponStats.IsStocklessPistol || WeaponStats.HasShoulderContact)
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
                        StanceController.DoRifleStances(player, firearmController, true, __instance, ref stanceRotation, dt, ref isResettingShortStock, ref hasResetShortStock, ref hasResetLowReady, ref hasResetActiveAim, ref hasResetHighReady, ref isResettingHighReady, ref isResettingLowReady, ref isResettingActiveAim, ref stanceRotationSpeed, ref hasResetMelee, ref isResettingMelee, ref didHalfMeleeAnim);
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

                    bool isTacBot = StanceController.botsToUseTacticalStances.IndexOf(player.AIData.BotOwner.Profile.Info.Settings.Role.ToString()) != -1;
                    bool isPeace = player.AIData.BotOwner.Memory.IsPeace;
                    bool notShooting = !player.AIData.BotOwner.ShootData.Shooting && Time.time - player.AIData.BotOwner.ShootData.LastTriggerPressd > 15f;
                    bool isInStance = false;
                    float stanceSpeed = 1f;

                    if (player.MovementContext.BlindFire == 0 && player.MovementContext.StationaryWeapon == null)
                    {
                        if (isPeace && !player.IsSprintEnabled && !__instance.IsAiming && !firearmController.IsInReloadOperation() && !firearmController.IsInventoryOpen() && !firearmController.IsInInteractionStrictCheck() && !firearmController.IsInSpawnOperation() && !firearmController.IsHandsProcessing()) // && player.AIData.BotOwner.WeaponManager.IsWeaponReady &&  player.AIData.BotOwner.WeaponManager.InIdleState()
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
                                    stanceSpeed = 12f * dt;
                                    targetRotation = lowReadyTargetQuaternion;
                                    __instance.HandsContainer.HandsPosition.Zero = __instance.PositionZeroSum + pitch * lowReadyTargetPostion;
                                }

                                ////high ready////
                                if (isTacBot && !firearmController.IsInReloadOperation() && !__instance.IsAiming && notShooting && (lastDistance >= 25f || lastDistance == 0f))
                                {
                                    isInStance = true;
                                    player.BodyAnimatorCommon.SetFloat(PlayerAnimator.WEAPON_SIZE_MODIFIER_PARAM_HASH, 2);
                                    stanceSpeed = 10.8f * dt;
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
                                    stanceSpeed = 6f * dt;
                                    targetRotation = activeAimTargetQuaternion;
                                    __instance.HandsContainer.HandsPosition.Zero = __instance.PositionZeroSum + pitch * activeAimTargetPostion;
                                }

                                ///short stock//// 
                                if (isTacBot && !player.IsSprintEnabled && !firearmController.IsInReloadOperation() && lastDistance <= 2f && lastDistance != 0f)
                                {
                                    isInStance = true;
                                    stanceSpeed = 12f * dt;
                                    targetRotation = shortStockTargetQuaternion;
                                    __instance.HandsContainer.HandsPosition.Zero = __instance.PositionZeroSum + pitch * shortStockTargetPostion;
                                }
                            }
                            else
                            {
                                if (!isTacBot && !player.IsSprintEnabled && !__instance.IsAiming && notShooting)
                                {
                                    isInStance = true;
                                    stanceSpeed = 6f * dt;
                                    targetRotation = normalPistolTargetQuaternion;
                                    __instance.HandsContainer.HandsPosition.Zero = __instance.PositionZeroSum + pitch * normalPistolTargetPosition;
                                }

                                if (isTacBot && !player.IsSprintEnabled && !__instance.IsAiming && notShooting)
                                {
                                    isInStance = true;
                                    stanceSpeed = 6f * dt;
                                    targetRotation = tacPistolTargetQuaternion;
                                    __instance.HandsContainer.HandsPosition.Zero = __instance.PositionZeroSum + pitch * tacPistolTargetPosition;
                                }
                            }
                        }
                    }

                    currentRotation = Quaternion.Slerp(currentRotation, __instance.IsAiming && !isInStance ? scopeRotation : isInStance ? targetRotation : Quaternion.identity, isInStance ? stanceSpeed : 8f * dt);
                    __instance.HandsContainer.WeaponRootAnim.SetPositionAndRotation(weaponPosition, weapRotation * currentRotation);
                    currentRotationField.SetValue(__instance, currentRotation);
                }
            }
        }
    }

    public class ApplyComplexRotationPatch : ModulePatch
    {
        private static FieldInfo aimSpeedField;
        private static FieldInfo compensatoryField;
        private static FieldInfo displacementStrField;
        private static FieldInfo scopeRotationField;
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
        private static bool didHalfMeleeAnim = false;

        private static Quaternion currentRotation = Quaternion.identity;
        private static Quaternion stanceRotation = Quaternion.identity;
        private static Vector3 mountWeapPosition = Vector3.zero;
        private static Vector3 currentRecoil = Vector3.zero;
        private static Vector3 targetRecoil = Vector3.zero;

        private static float stanceRotationSpeed = 1f;

        protected override MethodBase GetTargetMethod()
        {
            aimSpeedField = AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "_aimingSpeed");
            compensatoryField = AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "_compensatoryScale");
            displacementStrField = AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "_displacementStr");
            scopeRotationField = AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "_targetScopeRotation");
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
            if (player != null && player.IsYourPlayer && player.MovementContext.CurrentState.Name != EPlayerState.Stationary)
            {
                FirearmController fc = player.HandsController as FirearmController;

                StanceController.IsInThirdPerson = false;

                float aimSpeed = (float)aimSpeedField.GetValue(__instance);
                float compensatoryScale = (float)compensatoryField.GetValue(__instance);
                float displacementStr = (float)displacementStrField.GetValue(__instance);
                Quaternion aimingQuat = (Quaternion)scopeRotationField.GetValue(__instance);
                Vector3 weapTempPosition = (Vector3)weapTempPositionField.GetValue(__instance);
                Quaternion weapTempRotation = (Quaternion)weapTempRotationField.GetValue(__instance);
                bool isAiming = (bool)isAimingField.GetValue(__instance);

                Vector3 handsRotation = __instance.HandsContainer.HandsRotation.Get();
                Vector3 sway = __instance.HandsContainer.SwaySpring.Value;
                handsRotation += displacementStr * (isAiming ? __instance.AimingDisplacementStr : 1f) * new Vector3(sway.x, 0f, sway.z);
                handsRotation += sway;
                Vector3 rotationCenter = __instance._shouldMoveWeaponCloser ? __instance.HandsContainer.RotationCenterWoStock : __instance.HandsContainer.RotationCenter;
                Vector3 weapRootPivot = __instance.HandsContainer.WeaponRootAnim.TransformPoint(rotationCenter);

                StanceController.IsLeftShoulder = __instance.LeftStance;

                bool allStancesAreReset = hasResetActiveAim && hasResetLowReady && hasResetHighReady && hasResetShortStock && hasResetPistolPos;
                bool isInStance = 
                    StanceController.CurrentStance == EStance.HighReady || 
                    StanceController.CurrentStance == EStance.LowReady || 
                    StanceController.CurrentStance == EStance.ShortStock ||
                    StanceController.CurrentStance == EStance.ActiveAiming || 
                    StanceController.CurrentStance == EStance.Melee;
                bool isInShootableStance = 
                    StanceController.CurrentStance == EStance.ShortStock || 
                    StanceController.CurrentStance == EStance.ActiveAiming ||
                    WeaponStats.IsStocklessPistol || 
                    StanceController.CurrentStance == EStance.Melee;
                bool cancelBecauseShooting = StanceController.IsFiringFromStance && !isInShootableStance;
                bool doStanceRotation = (isInStance || !allStancesAreReset || StanceController.CurrentStance == EStance.PistolCompressed) && !cancelBecauseShooting;
                bool allowActiveAimReload = Plugin.ActiveAimReload.Value && PlayerState.IsInReloadOpertation && !PlayerState.IsAttemptingToReloadInternalMag && !PlayerState.IsQuickReloading;
                bool cancelStance = 
                    (StanceController.CancelActiveAim && StanceController.CurrentStance == EStance.ActiveAiming && !allowActiveAimReload) ||
                    (StanceController.CancelHighReady && StanceController.CurrentStance == EStance.HighReady) || 
                    (StanceController.CancelLowReady && StanceController.CurrentStance == EStance.LowReady) || 
                    (StanceController.CancelShortStock && StanceController.CurrentStance == EStance.ShortStock); // || (StanceController.CancelPistolStance && StanceController.PistolIsCompressed)

                currentRotation = Quaternion.Slerp(currentRotation, __instance.IsAiming && allStancesAreReset ? aimingQuat : doStanceRotation ? stanceRotation : Quaternion.identity, doStanceRotation ? stanceRotationSpeed * Plugin.StanceRotationSpeedMulti.Value : __instance.IsAiming ? 8f * aimSpeed * dt : 8f * dt);

                if (Plugin.EnableAdditionalRec.Value)
                {
                    RecoilController.DoVisualRecoil(ref targetRecoil, ref currentRecoil, ref weapTempRotation, Logger);
                }

                __instance.HandsContainer.WeaponRootAnim.SetPositionAndRotation(weapTempPosition, weapTempRotation * currentRotation);

                if (!Plugin.ServerConfig.enable_stances) return;

                if (WeaponStats.IsStocklessPistol && StanceController.CurrentStance != EStance.PatrolStance)
                {
                    if (StanceController.CurrentStance == EStance.PistolCompressed && !StanceController.IsAiming && !isResettingPistol && !StanceController.IsBlindFiring && !__instance.LeftStance)
                    {
                        StanceController.StanceBlender.Target = 1f;
                    }
                    else
                    {
                        StanceController.StanceBlender.Target = 0f;
                    }

                    if ((StanceController.CurrentStance != EStance.PistolCompressed && !StanceController.IsAiming && !isResettingPistol) || StanceController.IsBlindFiring || __instance.LeftStance)
                    {
                        StanceController.StanceTargetPosition = Vector3.Lerp(StanceController.StanceTargetPosition, Vector3.zero, 5f * dt);
                    }

                    hasResetActiveAim = true;
                    hasResetHighReady = true;
                    hasResetLowReady = true;
                    hasResetShortStock = true;
                    hasResetMelee = true;
                    StanceController.DoPistolStances(false, __instance, ref stanceRotation, dt, ref hasResetPistolPos, player, ref stanceRotationSpeed, ref isResettingPistol, fc);
                }
                else if (!WeaponStats.IsStocklessPistol || WeaponStats.HasShoulderContact)
                {
                    if ((!isInStance && allStancesAreReset) || (cancelBecauseShooting && !isInShootableStance) || StanceController.IsAiming || cancelStance || StanceController.IsBlindFiring || __instance.LeftStance)
                    {
                        StanceController.StanceBlender.Target = 0f;
                    }
                    else if (isInStance)
                    {
                        StanceController.StanceBlender.Target = 1f;
                    }

                    if (((!isInStance && allStancesAreReset) && !cancelBecauseShooting && !StanceController.IsAiming) || StanceController.IsBlindFiring || __instance.LeftStance)
                    {
                        StanceController.StanceTargetPosition = Vector3.Lerp(StanceController.StanceTargetPosition, Vector3.zero, 5f * dt);
                    }

                    hasResetPistolPos = true;
                    StanceController.DoRifleStances(player, fc, false, __instance, ref stanceRotation, dt, ref isResettingShortStock, ref hasResetShortStock, ref hasResetLowReady, ref hasResetActiveAim, ref hasResetHighReady, ref isResettingHighReady, ref isResettingLowReady, ref isResettingActiveAim, ref stanceRotationSpeed, ref hasResetMelee, ref isResettingMelee, ref didHalfMeleeAnim);
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
