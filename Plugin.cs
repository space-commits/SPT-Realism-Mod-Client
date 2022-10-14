using BepInEx;
using HarmonyLib;
using Aki.Reflection.Patching;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using EFT.InputSystem;
using Aki.Reflection.Utils;
using BepInEx.Logging;
using EFT.Animations;
using System.Runtime.InteropServices;
using System.Drawing;
using Aki.Common.Http;
using Aki.Common.Utils;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace RealismMod
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {

        public static float timer = 0.0f;

        public static bool isFiring;
        public static bool isAiming;
        public static bool statsReset;
        public static float shotCount = 0;
        public static float prevShotCount = shotCount;

        public static float startingRecoilAngle;

        public static float startingSens;
        public static float currentSens;

        public static float startingDispersion;
        public static float currentDispersion;
        public static float dispersionProportionK;

        public static float startingDamping;
        public static float currentDamping;
        public static float dampingProporitonK;

        public static float startingConvergence;
        public static float currentConvergence;
        public static float convergenceProporitonK;

        public static float startingCamRecoilX;
        public static float startingCamRecoilY;
        public static float currentCamRecoilX;
        public static float currentCamRecoilY;

        public static float startingVRecoilX;
        public static float startingVRecoilY;
        public static float currentVRecoilX;
        public static float currentVRecoilY;

        void Awake()
        {
            new COIDeltaPatch().Enable();
            new GetDurabilityLossOnShotPatch().Enable();
            new AutoFireRatePatch().Enable();
            new SingleFireRatePatch().Enable();
            new ErgoDeltaPatch().Enable();
            new OnMagInsertedPatch().Enable();
            new QuickReloadMagPatch().Enable();
            new ReloadMagPatch().Enable();
            new ErgoWeightPatch().Enable();
            new SyncWithCharacterSkillsPatch().Enable();
            new OnWeaponParametersChangedPatch().Enable();
            new UpdateWeaponVariablesPatch().Enable();
            new AimingSensitivityPatch().Enable();
            new UpdateSensitivityPatch().Enable();
            new ProcessPatch().Enable();
            new ShootPatch().Enable();
            new IsAimingPatch().Enable();
            new IsKnownMalfTypePatch().Enable();
            new ModConstructorPatch().Enable();
            new HRecoilDisplayValuePatch().Enable();
            new HRecoilDisplayDeltaPatch().Enable();
            new VRecoilDisplayValuePatch().Enable();
            new VRecoilDisplayDeltaPatch().Enable();
            new ModVRecoilStatDisplayPatch().Enable();
            new ModVRecoilStatDisplayPatch2().Enable();
            new ErgoDisplayDeltaPatch().Enable();
            new ErgoDisplayValuePatch().Enable();
            new COIDisplayDeltaPatch().Enable();
            new COIDisplayValuePatch().Enable();
            new FireRateDisplayStringPatch().Enable();
            new FireRateDisplayStringPatch().Enable();
            new method_17Patch().Enable();
            new UpdateSwayFactorsPatch().Enable();
        }

        void Update()
        {
            if (Helper.isReady())
            {
                if (isAiming == true)
                {
                    if (shotCount > prevShotCount)
                    {
                        if (shotCount >= 1 && shotCount <= 5)
                        {
                            if (currentVRecoilX < startingVRecoilX * WeaponProperties.vRecoilLimit)
                            {
                                currentVRecoilX *= 1.12f;
                                currentVRecoilY *= 1.12f;
                            }
                            if (currentConvergence > startingConvergence * WeaponProperties.convergenceLimit)
                            {
                                currentConvergence = Mathf.Min((convergenceProporitonK / currentVRecoilX), currentConvergence);
                            }
                        }
                        if (shotCount > 5 && shotCount <= 10)
                        {
                            if (currentVRecoilX < startingVRecoilX * WeaponProperties.vRecoilLimit)
                            {
                                currentVRecoilX *= 1.07f;
                                currentVRecoilY *= 1.07f;
                            }
                            if (currentConvergence > startingConvergence * WeaponProperties.convergenceLimit)
                            {
                                currentConvergence = Mathf.Min((convergenceProporitonK / currentVRecoilX), currentConvergence);
                            }
                        }

                        if (shotCount > 10 && shotCount <= 15)
                        {
                            if (currentVRecoilX < startingVRecoilX * WeaponProperties.vRecoilLimit)
                            {
                                currentVRecoilX *= 1.04f;
                                currentVRecoilY *= 1.04f;
                            }
                            if (currentConvergence > startingConvergence * WeaponProperties.convergenceLimit)
                            {
                                currentConvergence = Mathf.Min((convergenceProporitonK / currentVRecoilX), currentConvergence);
                            }
                            if (currentDamping > startingDamping * WeaponProperties.dampingLimit)
                            {
                                currentDamping *= 0.98f;
                            }
                        }

                        if (shotCount > 15 && shotCount <= 20)
                        {
                            if (currentVRecoilX < startingVRecoilX * WeaponProperties.vRecoilLimit)
                            {
                                currentVRecoilX *= 1.02f;
                                currentVRecoilY *= 1.02f;
                            }
                            if (currentConvergence > startingConvergence * WeaponProperties.convergenceLimit)
                            {
                                currentConvergence = Mathf.Min((convergenceProporitonK / currentVRecoilX), currentConvergence);
                            }
                            if (currentDamping > startingDamping * WeaponProperties.dampingLimit)
                            {
                                currentDamping *= 0.98f;
                            }
                        }

                        if (shotCount > 20 && shotCount <= 25)
                        {
                            if (currentVRecoilX < startingVRecoilX * WeaponProperties.vRecoilLimit)
                            {
                                currentVRecoilX *= 1.02f;
                                currentVRecoilY *= 1.02f;
                            }
                            if (currentConvergence > startingConvergence * WeaponProperties.convergenceLimit)
                            {
                                currentConvergence = Mathf.Min((convergenceProporitonK / currentVRecoilX), currentConvergence);
                            }
                            if (currentDamping > startingDamping * WeaponProperties.dampingLimit)
                            {
                                currentDamping *= 0.99f;
                            }
                        }

                        if (shotCount > 25 && shotCount <= 30)
                        {
                            if (currentVRecoilX < startingVRecoilX * WeaponProperties.vRecoilLimit)
                            {
                                currentVRecoilX *= 1.02f;
                                currentVRecoilY *= 1.02f;
                            }
                            if (currentConvergence > startingConvergence * WeaponProperties.convergenceLimit)
                            {
                                currentConvergence = Mathf.Min((convergenceProporitonK / currentVRecoilX), currentConvergence);
                            }
                            if (currentDamping > startingDamping * WeaponProperties.dampingLimit)
                            {
                                currentDamping *= 0.99f;
                            }
                        }

                        if (shotCount > 30 && shotCount <= 35)
                        {
                            if (currentVRecoilX < startingVRecoilX * WeaponProperties.vRecoilLimit)
                            {
                                currentVRecoilX *= 1.02f;
                                currentVRecoilY *= 1.02f;
                            }
                            if (currentConvergence > startingConvergence * WeaponProperties.convergenceLimit)
                            {
                                currentConvergence = Mathf.Min((convergenceProporitonK / currentVRecoilX), currentConvergence);
                            }
                            if (currentDamping > startingDamping * WeaponProperties.dampingLimit)
                            {
                                currentDamping *= 0.99f;
                            }
                        }

                        if (shotCount > 35)
                        {
                            if (currentVRecoilX < startingVRecoilX * WeaponProperties.vRecoilLimit)
                            {
                                currentVRecoilX *= 1.02f;
                                currentVRecoilY *= 1.02f;
                            }
                            if (currentConvergence > startingConvergence * WeaponProperties.convergenceLimit)
                            {
                                currentConvergence = Mathf.Min((convergenceProporitonK / currentVRecoilX), currentConvergence);
                            }
                            if (currentDamping > startingDamping * WeaponProperties.dampingLimit)
                            {
                                currentDamping *= 0.995f;
                            }
                        }

                        if (currentSens > startingSens * WeaponProperties.sensLimit)
                        {
                            currentSens *= WeaponProperties.sensChangeRate;
                        }
                        if (currentCamRecoilX > startingCamRecoilX * WeaponProperties.camRecoilLimit)
                        {
                            currentCamRecoilX *= WeaponProperties.camRecoilChangeRate;
                            currentCamRecoilY *= WeaponProperties.camRecoilChangeRate;
                        }
                        prevShotCount = shotCount;
                        isFiring = true;
                    }
                }

                if (shotCount == prevShotCount)
                {
                    timer += Time.deltaTime;
                    if (timer >= 0.15f)
                    {
                        isFiring = false;
                        shotCount = 0;
                        prevShotCount = 0;
                        timer = 0f;
                    }
                }

                if (isFiring == false)
                {
                    if (startingSens <= currentSens && startingConvergence <= currentConvergence && startingVRecoilX >= currentVRecoilX)
                    {
                        statsReset = true;
                    }
                    else
                    {
                        statsReset = false;
                    }
                }

                if (statsReset == false && isFiring == false)
                {
                    if (startingSens > currentSens)
                    {
                        currentSens *= WeaponProperties.sensResetRate;
                    }
                    if (startingSens > currentSens)
                    {
                        currentSens *= WeaponProperties.sensResetRate;
                    }
                    if (startingConvergence > currentConvergence)
                    {
                        currentConvergence *= WeaponProperties.convergenceResetRate;
                    }
                    if (startingDamping > currentDamping)
                    {
                        currentDamping *= WeaponProperties.dampingResetRate;
                    }
                    if (startingCamRecoilX > currentCamRecoilX)
                    {
                        currentCamRecoilX *= WeaponProperties.camRecoilResetRate;
                        currentCamRecoilY *= WeaponProperties.camRecoilResetRate;
                    }
                    if (startingVRecoilX < currentVRecoilX)
                    {
                        currentVRecoilX *= WeaponProperties.vRecoilResetRate;
                        currentVRecoilY *= WeaponProperties.vRecoilResetRate;
                    }
                }
            }
        }
    }
}

