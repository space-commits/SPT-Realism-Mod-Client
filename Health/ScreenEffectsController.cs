using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static HBAO_Core;

namespace RealismMod.Health
{
    public static class ScreenEffectsController
    {
        public static PrismEffects PrismEffects { get; set; }
        private static float _radiationEffectStrength = 0;
        private static float _blurEffectStrength = 0;
        private static float _chromaEffectStrength = 0;
        public static void EffectsUpdate() 
        {
            if (PrismEffects != null) 
            {
                if (PluginConfig.ShowRadEffects.Value) DoRadiationEffects();
                DoAdrenalineAndOverdose();
            }
        }

        public static void DoAdrenalineAndOverdose()
        {
            PrismEffects.useChromaticAberration = true;
            PrismEffects.useChromaticBlur = true;
            PrismEffects.chromaticDistanceOne = -0.1f; // -0.1f
            PrismEffects.chromaticDistanceTwo = 0.7f; //0.9f

            bool hasAdrenaline = Plugin.RealHealthController.HasNegativeAdrenalineEffect;
            bool hasOverdoed = Plugin.RealHealthController.HasOverdosed;
            float blur = hasAdrenaline || (PluginConfig.EnableLogging.Value && PluginConfig.test9.Value > 100f) ? Mathf.Lerp(0.85f, 9f, Mathf.PingPong(Time.time * 0.5f, 1f)) : 0f;
            float chroma = hasOverdoed || (PluginConfig.EnableLogging.Value && PluginConfig.test10.Value > 100f) ? Mathf.Lerp(0.05f, 0.2f, Mathf.PingPong(Time.time * 0.2f, 1f)) : 0f;

            _blurEffectStrength = Mathf.Lerp(_blurEffectStrength, blur, 0.025f);
            _chromaEffectStrength = Mathf.Lerp(_chromaEffectStrength, chroma, 0.01f);

            PrismEffects.chromaticBlurWidth = _blurEffectStrength;
            PrismEffects.SetChromaticIntensity(_chromaEffectStrength);
        }

        public static void DoRadiationEffects() 
        {
            float effectStrength = (HazardTracker.TotalRadiationRate * 80f) + (HazardTracker.TotalRadiation * 0.25f);
            _radiationEffectStrength = Mathf.Lerp(_radiationEffectStrength, effectStrength, 0.01f);
            if (_radiationEffectStrength > 0.1f)
            {
                PrismEffects.noiseIntensity = _radiationEffectStrength; 
                PrismEffects.useNoise = true;
            }
            else
            {
                PrismEffects.noiseIntensity = 0f;
                PrismEffects.useNoise = false;
            }
        }
    }
}
