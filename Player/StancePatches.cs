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
using System.Collections.Generic;
/*using LightStruct = GStruct155;*/

namespace RealismMod
{
    public class MountingPatch : ModulePatch
    {
        private static FieldInfo _playerField;
        private static FieldInfo _fcField;
        private static float _mountClamp = 0f;
        private static float _collidingModifier = 1f;
        private static float _collisionTimer = 0f;
        private static float _collisionResetTimer = 0f;
        private static float _slowDownTarget = 0.1f;
        private static float _previousOverlapValue = 0f;
        private static float _currentOverlapValue = 0f;
        private static float _smoothedOverlapValue = 0f;

        private static Vector3 _collisionPos = Vector3.zero;
        private static Vector3 _collisionRot = Vector3.zero;

        protected override MethodBase GetTargetMethod()
        {
            _playerField = AccessTools.Field(typeof(FirearmController), "_player");
            _fcField = AccessTools.Field(typeof(ProceduralWeaponAnimation), "_firearmController");

            return typeof(ProceduralWeaponAnimation).GetMethod("AvoidObstacles", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(ProceduralWeaponAnimation __instance)
        {
            FirearmController firearmController = (FirearmController)_fcField.GetValue(__instance);
            if (firearmController == null)
            {
                return;
            }

            Player player = (Player)_playerField.GetValue(firearmController);
            if (player != null && player.IsYourPlayer && player.MovementContext.CurrentState.Name != EPlayerState.Stationary)
            {
                //to stop it spazzing out on railings: speed starts off at very low value like 0.1-0.5, collision detected = slowly then quickly ramp up speeds, when no collision quickly reset speeds back to low value
                //speed should also be modified by stamina, injury, ergo

                //what I actually need is for the weapon to stay in compressed state for X seconds before resetting, that would mean keeping speed modifier at 0 for X time


                _currentOverlapValue = firearmController.OverlapValue;
                float _smoothingFactor = 0.1f; //0.1f
                _smoothedOverlapValue = _smoothedOverlapValue + _smoothingFactor * (_currentOverlapValue - _smoothedOverlapValue);

                bool isIncreasing = _smoothedOverlapValue > _previousOverlapValue;
                bool isDecreasing = _smoothedOverlapValue < _previousOverlapValue;
                bool isStable = Utils.AreFloatsEqual(_smoothedOverlapValue, _previousOverlapValue, 0.0001f);
                float normalSpeed = 0.1f; //0.1f
                float delaySpeed = 0.2f; //0.2f
                float resetTime = 2f; //2
                float delayTime = 0.1f; //0.1
                float slowDown = 0.1f; //0.05

                if (isStable)
                {
                    Logger.LogWarning("=======stable.");
                    _collisionTimer = 0;
                    _collisionResetTimer = 0f;
                    _collidingModifier = Mathf.MoveTowards(_collidingModifier, 1f, normalSpeed);

                }
                else if (isIncreasing)
                {
                    Logger.LogWarning(">>>>>>increasing.");

                    _collisionTimer += Time.deltaTime;
                    if (_collisionTimer <= delayTime)
                    {
                        Logger.LogWarning("==delay");
                        _collidingModifier = Mathf.MoveTowards(_collidingModifier, slowDown, delaySpeed);
                    }
                    else
                    {
                       Logger.LogWarning("==continue");
                        _collidingModifier = Mathf.MoveTowards(_collidingModifier, 1f, normalSpeed);
                    }

                    //_collidingModifier = Mathf.MoveTowards(_collidingModifier, PluginConfig.test6.Value, PluginConfig.test7.Value); this was here by accident when it felt good, it used the same values as non-delay
                    _collisionResetTimer = 0f;
                }
                else if (isDecreasing)
                {
                    Logger.LogWarning("<<<<<decreasing.");

                    _collisionTimer = 0;
                    _collisionResetTimer += Time.deltaTime;
                    if (_collisionResetTimer <= resetTime)
                    {
                        Logger.LogWarning("==pause");
                        _collidingModifier = Mathf.MoveTowards(_collidingModifier, slowDown, delaySpeed);
                    }
                    else
                    {
                        Logger.LogWarning("==reset");
                        _collidingModifier = Mathf.MoveTowards(_collidingModifier, 1f, normalSpeed);
                    }
                }

                _previousOverlapValue = _smoothedOverlapValue;


                AccessTools.Field(typeof(TurnAwayEffector), "_blendSpeed").SetValue(__instance.TurnAway, 4.5f * _collidingModifier); //4.5
                AccessTools.Field(typeof(TurnAwayEffector), "_smoothTimeIn").SetValue(__instance.TurnAway, 7f * _collidingModifier); //7
                AccessTools.Field(typeof(TurnAwayEffector), "_smoothTimeOut").SetValue(__instance.TurnAway, 4f * _collidingModifier); //4

                if (StanceController.IsMounting)
                {
                    _mountClamp = Mathf.Lerp(_mountClamp, 2.5f, 0.1f);
                }
                else
                {
                    _mountClamp = Mathf.Lerp(_mountClamp, 0f, 0.1f);
                }

                StanceController.MountingPivotUpdate(player, __instance, _mountClamp, StanceController.GetDeltaTime());
            }
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

        private static Vector3 _startLeftDir = new Vector3(0.143f, 0f, 0f);
        private static Vector3 _startRightDir = new Vector3(-0.143f, 0f, 0f);
        private static Vector3 _startDownDir = new Vector3(0f, 0f, -0.19f);

        private static Vector3 _wiggleLeftDir = new Vector3(2.5f, 7.5f, -5) * 0.5f;
        private static Vector3 _wiggleRightDir = new Vector3(2.5f, -7.5f, -5f) * 0.5f;
        private static Vector3 _wiggleDownDir = new Vector3(7.5f, 2.5f, -5f) * 0.5f;


        private static void SetMountingStatus(EBracingDirection coverDir)
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

        private static bool IsBracingProne(Player player) 
        {
            if (player.IsInPronePose) 
            {
                SetMountingStatus(EBracingDirection.Top);
                return true;
            }
            return false;
        }

        private static bool CheckForCoverCollision(EBracingDirection coverDir, Vector3 start, Vector3 direction, out RaycastHit raycastHit, RaycastHit[] raycastArr, Func<RaycastHit, bool> isHitIgnoreTest, string weapClass)
        {
            if (EFTPhysicsClass.Linecast(start, direction, out raycastHit, EFTHardSettings.Instance.WEAPON_OCCLUSION_LAYERS, false, raycastArr, isHitIgnoreTest))
            {
                SetMountingStatus(coverDir);
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

                if (PluginConfig.EnableLogging.Value) 
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

                    if (IsBracingProne(player) ||
                        CheckForCoverCollision(EBracingDirection.Top, startDown, forwardDirection, out raycastHit, raycastArr, isHitIgnoreTest, weapClass) ||
                        CheckForCoverCollision(EBracingDirection.Left, startLeft, leftDirection, out raycastHit, raycastArr, isHitIgnoreTest, weapClass) ||
                        CheckForCoverCollision(EBracingDirection.Right, startRight, rightDirection, out raycastHit, raycastArr, isHitIgnoreTest, weapClass)) 
                    {
                        return;
                    }

                    StanceController.IsBracing = false;
                }

                if (StanceController.IsBracing || StanceController.IsMounting) 
                {
                    float mountOrientationBonus = StanceController.BracingDirection == EBracingDirection.Top ? 0.75f : 1f;
                    float mountingRecoilLimit = StanceController.TreatWeaponAsPistolStance ? 0.25f : 0.75f;
                    float recoilBonus = 
                        StanceController.IsMounting && __instance.Weapon.IsBeltMachineGun && WeaponStats.HasBipod ? 0.5f :
                        StanceController.IsMounting && __instance.Weapon.IsBeltMachineGun ? 0.65f :
                        StanceController.IsMounting && WeaponStats.HasBipod ? 0.7f :
                        StanceController.IsMounting ? 0.85f :
                        0.95f;
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
                WeaponStats.NewWeaponLength = length >= 0.92f ? length * 1.12f : length;
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
                if (StanceController.CurrentStance == EStance.PatrolStance) 
                {
                    weaponLnField.SetValue(__instance, WeaponStats.NewWeaponLength * 0.15f);
                    return;
                }

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
                    if (Plugin.FikaPresent) //collisions acts funky with stances from another client's perspective
                    {
                        weaponLnField.SetValue(__instance, WeaponStats.NewWeaponLength * 0.6f);
                        return;
                    }
                    if (StanceController.CurrentStance == EStance.ShortStock)
                    {
                        weaponLnField.SetValue(__instance, WeaponStats.NewWeaponLength * 0.65f);
                        return;
                    }
                    if (StanceController.CurrentStance == EStance.HighReady)
                    {
                        weaponLnField.SetValue(__instance, WeaponStats.NewWeaponLength * 0.75f);
                        return;
                    }
                    if (StanceController.CurrentStance == EStance.LowReady)
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
                weaponLnField.SetValue(__instance, WeaponStats.NewWeaponLength);
                return;
            }
        }
    }

    public class ShouldMoveWeapCloserPatch : ModulePatch
    {
        private static FieldInfo _playerField;
        private static FieldInfo _fcField;

        protected override MethodBase GetTargetMethod()
        {
            _playerField = AccessTools.Field(typeof(EFT.Player.FirearmController), "_player");
            _fcField = AccessTools.Field(typeof(ProceduralWeaponAnimation), "_firearmController");
            return typeof(EFT.Animations.ProceduralWeaponAnimation).GetMethod("CheckShouldMoveWeaponCloser", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(ProceduralWeaponAnimation __instance, ref bool ____shouldMoveWeaponCloser)
        {
            FirearmController firearmController = (FirearmController)_fcField.GetValue(__instance);
            if (firearmController == null) return;
            Player player = (Player)_playerField.GetValue(firearmController);
            if (player != null && player.MovementContext.CurrentState.Name != EPlayerState.Stationary && player.IsYourPlayer) 
            { 
                ____shouldMoveWeaponCloser = false;
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
                Vector3 newPos = PluginConfig.EnableAltRifle.Value ? new Vector3(0.08f, -0.075f, 0f) : new Vector3(PluginConfig.WeapOffsetX.Value, PluginConfig.WeapOffsetY.Value, PluginConfig.WeapOffsetZ.Value);
                newPos += baseOffset;
                if (!PluginConfig.EnableAltRifle.Value) newPos += __instance.HandsContainer.WeaponRoot.localPosition;
                StanceController.WeaponOffsetPosition = newPos;
                __instance.HandsContainer.WeaponRoot.localPosition = newPos;
                if (!Plugin.FOVFixPresent) __instance.HandsContainer.CameraOffset = new Vector3(0.04f, 0.04f, 0.025f);
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
                        StanceController.TreatWeaponAsPistolStance || 
                        StanceController.CurrentStance == EStance.Melee;
                    bool cancelBecauseShooting = StanceController.IsFiringFromStance && !isInShootableStance;
                    bool doStanceRotation = (isInStance || !allStancesReset || StanceController.CurrentStance == EStance.PistolCompressed) && !cancelBecauseShooting;
                    bool allowActiveAimReload = PluginConfig.ActiveAimReload.Value && PlayerState.IsInReloadOpertation && !PlayerState.IsAttemptingToReloadInternalMag && !PlayerState.IsQuickReloading;
                    bool cancelStance = 
                        (StanceController.CancelActiveAim && StanceController.CurrentStance == EStance.ActiveAiming && !allowActiveAimReload) || 
                        (StanceController.CancelHighReady && StanceController.CurrentStance == EStance.HighReady) ||
                        (StanceController.CancelLowReady && StanceController.CurrentStance == EStance.LowReady) || 
                        (StanceController.CancelShortStock && StanceController.CurrentStance == EStance.ShortStock); //|| (StanceController.CancelPistolStance && StanceController.PistolIsCompressed)

                    currentRotation = Quaternion.Slerp(currentRotation, __instance.IsAiming && allStancesReset ? scopeRotation : doStanceRotation ? stanceRotation : Quaternion.identity, doStanceRotation ? stanceRotationSpeed * PluginConfig.StanceRotationSpeedMulti.Value : __instance.IsAiming ? 8f * aimSpeed * dt : 8f * dt);

                    __instance.HandsContainer.WeaponRootAnim.SetPositionAndRotation(weaponPosition, weapRotation * currentRotation);

                    if (StanceController.TreatWeaponAsPistolStance && PluginConfig.EnableAltPistol.Value) // && StanceController.CurrentStance != EStance.PatrolStance
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
                    else if (!StanceController.TreatWeaponAsPistolStance || WeaponStats.HasShoulderContact)
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
        private static FieldInfo _aimSpeedField;
        private static FieldInfo _compensatoryField;
        private static FieldInfo _displacementStrField;
        private static FieldInfo _scopeRotationField;
        private static FieldInfo _weapTempRotationField;
        private static FieldInfo _weapTempPositionField;
        private static FieldInfo _isAimingField;
        private static FieldInfo _firearmControllerField;
        private static FieldInfo _playerField;

        private static bool _hasResetActiveAim = true;
        private static bool _hasResetLowReady = true;
        private static bool _hasResetHighReady = true;
        private static bool _hasResetShortStock = true;
        private static bool _hasResetPistolPos = true;
        private static bool _hasResetMelee = true;

        private static bool _isResettingActiveAim = false;
        private static bool _isResettingLowReady = false;
        private static bool _isResettingHighReady = false;
        private static bool _isResettingShortStock = false;
        private static bool _isResettingPistol = false;
        private static bool _isResettingMelee = false;
        private static bool _didHalfMeleeAnim = false;

        private static Quaternion _currentRotation = Quaternion.identity;
        private static Quaternion _stanceRotation = Quaternion.identity;
        private static Vector3 _mountWeapPosition = Vector3.zero;
        private static Vector3 _currentRecoil = Vector3.zero;
        private static Vector3 _targetRecoil = Vector3.zero;
        private static Vector3 _posePosOffest = Vector3.zero;
        private static Vector3 _poseRotOffest = Vector3.zero;
        private static Vector3 _patrolPos = Vector3.zero;
        private static Vector3 _patrolRot = Vector3.zero;

        private static Vector3 _riflePatrolPos = new Vector3(0.2f, 0.025f, 0.1f);
        private static Vector3 _riflePatrolRot = new Vector3(0.05f, -0.05f, -0.5f);
        private static Vector3 _pistolPatrolPos = new Vector3(0.05f, 0f, 0f);
        private static Vector3 _pistolPatrolRot = new Vector3(0.1f, -0.1f, -0.1f);

        private static float _stanceRotationSpeed = 1f;

        protected override MethodBase GetTargetMethod()
        {
            _aimSpeedField = AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "_aimingSpeed");
            _compensatoryField = AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "_compensatoryScale");
            _displacementStrField = AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "_displacementStr");
            _scopeRotationField = AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "_targetScopeRotation");
            _weapTempPositionField = AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "_temporaryPosition");
            _weapTempRotationField = AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "_temporaryRotation");
            _isAimingField = AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "_isAiming");
            _firearmControllerField = AccessTools.Field(typeof(ProceduralWeaponAnimation), "_firearmController");
            _playerField = AccessTools.Field(typeof(FirearmController), "_player");

            return typeof(EFT.Animations.ProceduralWeaponAnimation).GetMethod("ApplyComplexRotation", BindingFlags.Instance | BindingFlags.Public);
        }

        private static void DoPatrolStance(ProceduralWeaponAnimation pwa, Player player) 
        {
            Vector3 patrolPos = StanceController.CurrentStance != EStance.PatrolStance ? Vector3.zero : WeaponStats.IsStocklessPistol || WeaponStats.IsMachinePistol ? _pistolPatrolPos : _riflePatrolPos;
            _patrolPos = Vector3.Lerp(_patrolPos, patrolPos, 6f * Time.deltaTime);
            pwa.HandsContainer.WeaponRoot.localPosition += _patrolPos;

            Vector3 patrolRot = StanceController.CurrentStance != EStance.PatrolStance ? Vector3.zero : WeaponStats.IsStocklessPistol || WeaponStats.IsMachinePistol ? _pistolPatrolRot : _riflePatrolRot;
            _patrolRot = Vector3.Lerp(_patrolRot, patrolRot, 6f * Time.deltaTime);

            Quaternion newRot = Quaternion.identity;
            newRot.x = _patrolRot.x;
            newRot.y = _patrolRot.y;
            newRot.z = _patrolRot.z;
            pwa.HandsContainer.WeaponRoot.localRotation *= newRot;

            if (Vector3.Distance(_patrolPos, Vector3.zero) <= 0.05f) StanceController.FinishedUnPatrolStancing = true;
            else 
            {
                StanceController.FinishedUnPatrolStancing = false;
            }
        }

        private static void DoExtraPosAndRot(ProceduralWeaponAnimation pwa, Player player) 
        {
            //position
            float stockOffset = !WeaponStats.IsPistol && !WeaponStats.HasShoulderContact ? -0.04f : 0f;
            float stockPosOffset = WeaponStats.StockPosition * 0.01f;
            float posOffsetMulti = WeaponStats.HasShoulderContact ? -0.04f : 0.04f;
            float posePosOffset = (1f - player.MovementContext.PoseLevel) * posOffsetMulti;

            float targetPosXOffset = pwa.IsAiming ? 0f : 0f;
            float targetPosYOffset = pwa.IsAiming ? 0f : 0f;
            float targetPosZOffset = pwa.IsAiming ? 0f : Mathf.Clamp(posePosOffset + stockOffset + stockPosOffset, -0.05f, 0.05f);
            Vector3 targetPos = new Vector3(targetPosXOffset, targetPosYOffset, targetPosZOffset);

            _posePosOffest = Vector3.Lerp(_posePosOffest, targetPos, 5f * Time.deltaTime);
            pwa.HandsContainer.WeaponRoot.localPosition += _posePosOffest;

            //rotation
            bool doMaskOffset = (GearController.HasGasMask || GearController.FSIsActive) && pwa.IsAiming && WeaponStats.HasShoulderContact && !WeaponStats.IsStocklessPistol && !WeaponStats.IsMachinePistol;
            bool doLongMagOffset = WeaponStats.HasLongMag && player.IsInPronePose;
            float magOffset = doLongMagOffset && !pwa.IsAiming ? -0.35f : doLongMagOffset && pwa.IsAiming ? -0.12f : 0f;
            float ergoOffset = WeaponStats.ErgoFactor * -0.001f;
            float poseRotOffset = (1f - player.MovementContext.PoseLevel) * -0.03f;
            poseRotOffset += player.IsInPronePose ? -0.03f : 0f;
            float maskFactor = doMaskOffset? -0.025f + ergoOffset : 0f;
            float baseRotOffset = pwa.IsAiming || StanceController.IsMounting ? 0f : poseRotOffset + ergoOffset;

            float rotX = 0f;
            float rotY = Mathf.Clamp(baseRotOffset + maskFactor + magOffset, -0.5f, 0f);
            float rotZ = 0f;
            Vector3 targetRot = new Vector3(rotX, rotY, rotZ);

            _poseRotOffest = Vector3.Lerp(_poseRotOffest, targetRot, 5f * Time.deltaTime);

            Quaternion newRot = Quaternion.identity;
            newRot.x = _poseRotOffest.x;
            newRot.y = _poseRotOffest.y;
            newRot.z = _poseRotOffest.z;
            pwa.HandsContainer.WeaponRoot.localRotation *= newRot;
        }

        [PatchPostfix]
        private static void Postfix(EFT.Animations.ProceduralWeaponAnimation __instance, float dt)
        {
            FirearmController firearmController = (FirearmController)_firearmControllerField.GetValue(__instance);
            if (firearmController == null)
            {
                return;
            }
            Player player = (Player)_playerField.GetValue(firearmController);
            if (player != null && player.IsYourPlayer && player.MovementContext.CurrentState.Name != EPlayerState.Stationary)
            {
                FirearmController fc = player.HandsController as FirearmController;

                StanceController.IsInThirdPerson = false;

                float aimSpeed = (float)_aimSpeedField.GetValue(__instance);
                float compensatoryScale = (float)_compensatoryField.GetValue(__instance);
                float displacementStr = (float)_displacementStrField.GetValue(__instance);
                Quaternion aimingQuat = (Quaternion)_scopeRotationField.GetValue(__instance);
                Vector3 weapTempPosition = (Vector3)_weapTempPositionField.GetValue(__instance);
                Quaternion weapTempRotation = (Quaternion)_weapTempRotationField.GetValue(__instance);
                bool isAiming = (bool)_isAimingField.GetValue(__instance);

                Vector3 handsRotation = __instance.HandsContainer.HandsRotation.Get();
                Vector3 sway = __instance.HandsContainer.SwaySpring.Value;
                handsRotation += displacementStr * (isAiming ? __instance.AimingDisplacementStr : 1f) * new Vector3(sway.x, 0f, sway.z);
                handsRotation += sway;
                Vector3 rotationCenter = __instance._shouldMoveWeaponCloser ? __instance.HandsContainer.RotationCenterWoStock : __instance.HandsContainer.RotationCenter;
                Vector3 weapRootPivot = __instance.HandsContainer.WeaponRootAnim.TransformPoint(rotationCenter);

                bool allStancesAreReset = _hasResetActiveAim && _hasResetLowReady && _hasResetHighReady && _hasResetShortStock && _hasResetPistolPos && !StanceController.DoLeftShoulderTransition; 
                bool isInStance = 
                    StanceController.CurrentStance == EStance.HighReady || 
                    StanceController.CurrentStance == EStance.LowReady || 
                    StanceController.CurrentStance == EStance.ShortStock ||
                    StanceController.CurrentStance == EStance.ActiveAiming || 
                    StanceController.CurrentStance == EStance.Melee ||
                    StanceController.DoLeftShoulderTransition;
                bool isInShootableStance = 
                    StanceController.CurrentStance == EStance.ShortStock || 
                    StanceController.CurrentStance == EStance.ActiveAiming ||
                    StanceController.TreatWeaponAsPistolStance || 
                    StanceController.CurrentStance == EStance.Melee;
                bool cancelBecauseShooting = StanceController.IsFiringFromStance && !isInShootableStance;
                bool doStanceRotation = (isInStance || !allStancesAreReset || StanceController.CurrentStance == EStance.PistolCompressed) && !cancelBecauseShooting;
                bool allowActiveAimReload = PluginConfig.ActiveAimReload.Value && PlayerState.IsInReloadOpertation && !PlayerState.IsAttemptingToReloadInternalMag && !PlayerState.IsQuickReloading;
                bool cancelStance = 
                    (StanceController.CancelActiveAim && StanceController.CurrentStance == EStance.ActiveAiming && !allowActiveAimReload) ||
                    (StanceController.CancelHighReady && StanceController.CurrentStance == EStance.HighReady) || 
                    (StanceController.CancelLowReady && StanceController.CurrentStance == EStance.LowReady) || 
                    (StanceController.CancelShortStock && StanceController.CurrentStance == EStance.ShortStock); // || (StanceController.CancelPistolStance && StanceController.PistolIsCompressed)

                _currentRotation = Quaternion.Slerp(_currentRotation, __instance.IsAiming && allStancesAreReset ? aimingQuat : doStanceRotation ? _stanceRotation : Quaternion.identity, doStanceRotation ? _stanceRotationSpeed * PluginConfig.StanceRotationSpeedMulti.Value : __instance.IsAiming ? 7f * aimSpeed * dt : 8f * dt); //__instance.IsAiming ? 8f * aimSpeed * dt

                if (PluginConfig.EnableAdditionalRec.Value)
                {
                    RecoilController.DoVisualRecoil(ref _targetRecoil, ref _currentRecoil, ref weapTempRotation, Logger);
                }

                __instance.HandsContainer.WeaponRootAnim.SetPositionAndRotation(weapTempPosition, weapTempRotation * _currentRotation);

                if (!Plugin.ServerConfig.enable_stances) return;

                if (StanceController.TreatWeaponAsPistolStance)//&& StanceController.CurrentStance != EStance.PatrolStance
                {
                    if (StanceController.CurrentStance == EStance.PistolCompressed && !StanceController.IsAiming && !_isResettingPistol && !StanceController.IsBlindFiring) //&& !__instance.LeftStance
                    {
                        StanceController.StanceBlender.Target = 1f;
                    }
                    else
                    {
                        StanceController.StanceBlender.Target = 0f;
                    }

                    if ((StanceController.CurrentStance != EStance.PistolCompressed && !StanceController.IsAiming && !_isResettingPistol) || StanceController.IsBlindFiring) // || __instance.LeftStance
                    {
                        StanceController.StanceTargetPosition = Vector3.Lerp(StanceController.StanceTargetPosition, Vector3.zero, 5f * dt);
                    }

                    _hasResetActiveAim = true;
                    _hasResetHighReady = true;
                    _hasResetLowReady = true;
                    _hasResetShortStock = true;
                    _hasResetMelee = true;
                    StanceController.DoPistolStances(false, __instance, ref _stanceRotation, dt, ref _hasResetPistolPos, player, ref _stanceRotationSpeed, ref _isResettingPistol, fc);
                }
                else if (!StanceController.TreatWeaponAsPistolStance || WeaponStats.HasShoulderContact)
                {
                    if ((!isInStance && allStancesAreReset) || (cancelBecauseShooting && !isInShootableStance) || StanceController.IsAiming || cancelStance || StanceController.IsBlindFiring || StanceController.IsLeftShoulder)
                    {
                        StanceController.StanceBlender.Target = 0f;
                    }
                    else if (isInStance)
                    {
                        StanceController.StanceBlender.Target = 1f;
                    }

                    if (((!isInStance && allStancesAreReset) && !cancelBecauseShooting && !StanceController.IsAiming) || StanceController.IsBlindFiring || StanceController.IsLeftShoulder)
                    {
                        StanceController.StanceTargetPosition = Vector3.Lerp(StanceController.StanceTargetPosition, Vector3.zero, 5f * dt);
                    }

                    _hasResetPistolPos = true;
                    StanceController.DoRifleStances(player, fc, false, __instance, ref _stanceRotation, dt, ref _isResettingShortStock, ref _hasResetShortStock, ref _hasResetLowReady, ref _hasResetActiveAim, ref _hasResetHighReady, ref _isResettingHighReady, ref _isResettingLowReady, ref _isResettingActiveAim, ref _stanceRotationSpeed, ref _hasResetMelee, ref _isResettingMelee, ref _didHalfMeleeAnim);
                }

                if (PluginConfig.EnableExtraProcEffects.Value) DoExtraPosAndRot(__instance, player);
                DoPatrolStance(__instance, player);

                StanceController.HasResetActiveAim = _hasResetActiveAim;
                StanceController.HasResetHighReady = _hasResetHighReady;
                StanceController.HasResetLowReady = _hasResetLowReady;
                StanceController.HasResetShortStock = _hasResetShortStock;
                StanceController.HasResetPistolPos = _hasResetPistolPos;
                StanceController.HasResetMelee = _hasResetMelee;
            }
        }
    }
}
