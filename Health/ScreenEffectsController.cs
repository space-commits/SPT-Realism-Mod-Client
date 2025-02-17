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
                DoRadiationEffects();
                DoAdrenalineAndOverdose();
            }
        }

        public static void DoAdrenalineAndOverdose()
        {
            //if no adrenaline, should lerp and increase the chroma distance, if adrenaline reduce it back to default value

            PrismEffects.useChromaticAberration = true;
            PrismEffects.useChromaticBlur = true;
            PrismEffects.chromaticDistanceOne = -0.1f; // -0.1f
            PrismEffects.chromaticDistanceTwo = 0.7f; //0.9f

            bool hasAdrenaline = Plugin.RealHealthController.HasNegativeAdrenalineEffect;
            bool hasOverdoed = Plugin.RealHealthController.HasOverdosedOnStim;
            float blur = hasAdrenaline ? Mathf.Lerp(0.8f, 7f, Mathf.PingPong(Time.time * 0.5f, 1f)) : 0f;
            float chroma = hasOverdoed ? Mathf.Lerp(0.05f, 0.2f, Mathf.PingPong(Time.time * 0.2f, 1f)) : 0f;

            _blurEffectStrength = Mathf.Lerp(_blurEffectStrength, blur, 0.025f);
            _chromaEffectStrength = Mathf.Lerp(_chromaEffectStrength, chroma, 0.01f);

            PrismEffects.chromaticBlurWidth = _blurEffectStrength;
            PrismEffects.SetChromaticIntensity(_chromaEffectStrength);
        }

        public static void DoRadiationEffects() 
        {
            float effectStrength = (HazardTracker.TotalRadiationRate * 40f) + (HazardTracker.TotalRadiation * 0.15f);
            effectStrength = effectStrength <= 2f ? 0f : effectStrength;
            _radiationEffectStrength = Mathf.Lerp(_radiationEffectStrength, effectStrength, 0.01f);
            if (PluginConfig.ShowRadEffects.Value)
            {
                PrismEffects.noiseIntensity = Mathf.Max(_radiationEffectStrength, 0f); 
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
