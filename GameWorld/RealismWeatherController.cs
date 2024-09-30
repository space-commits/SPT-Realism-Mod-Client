using EFT.Weather;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace RealismMod
{
    public class RealismWeatherController : MonoBehaviour
    {
        private static FieldInfo FogField = AccessTools.Field(typeof(WeatherDebug), "Fog");
        private static FieldInfo LighteningThunderField = AccessTools.Field(typeof(WeatherDebug), "LightningThunderProbability");
        private static FieldInfo RainField = AccessTools.Field(typeof(WeatherDebug), "Rain");
        private static FieldInfo TemperatureField = AccessTools.Field(typeof(WeatherDebug), "Temperature");

        private WeatherController wc;
        public bool DoExplosionEffect { get; set; }
        public float TargetCloudDensity { get; set; }
        public float TargetWindMagnitude { get; set; }
        public float TargetFog { get; set; }
        public float TargetLighteningThunder { get; set; }
        public float TargetRain { get; set; }
        public Vector2 TargetTopWindDirection { get; set; }
        public WeatherDebug.Direction TargetWindDirection { get; set; }

        private bool ModifyWeather
        { 
            get 
            {
                return HazardTracker.IsPreExplosion || HazardTracker.HasExploded || GameWorldController.DoMapGasEvent;
            } 
        }

        private float _elapsedTime = 0f;
        private float _gasFogTimer = 0f;
        private float _targetGasStrength = 0.1f;
        private float _targetGasCloudStrength = 0.5f;

        void Awake()
        {
        }

        //do not run on labs or factory
        void Update() 
        {
            if (!GameWorldController.MapWithDynamicWeather || !GameWorldController.GameStarted || !ModifyWeather) return;
            if (wc == null) wc = WeatherController.Instance; //keep trying to get instance
            if (wc != null)
            {
                //HazardTracker.IsPreExplosion = true;
                if (HazardTracker.IsPreExplosion && !HazardTracker.HasExploded) DoPreExplosionWeather();
                else if (HazardTracker.HasExploded) DoExplosionWeather();
                else if (GameWorldController.DoMapGasEvent) DoMapGasEventWeather();

                wc.WeatherDebug.Enabled = true;
                wc.WeatherDebug.CloudDensity = TargetCloudDensity;
                wc.WeatherDebug.WindMagnitude = TargetWindMagnitude;
                wc.WeatherDebug.TopWindDirection = TargetTopWindDirection;
                wc.WeatherDebug.WindDirection = TargetWindDirection;
                FogField.SetValue(wc.WeatherDebug, TargetFog);
                LighteningThunderField.SetValue(wc.WeatherDebug, TargetLighteningThunder);
                RainField.SetValue(wc.WeatherDebug, TargetRain);
            }       
        }

        private void DoMapGasEventWeather() 
        {
            _gasFogTimer += Time.deltaTime;

            if (_gasFogTimer >= 300f)
            {
                _targetGasStrength = UnityEngine.Random.Range(0.025f, 0.12f);
                _targetGasCloudStrength = UnityEngine.Random.Range(0.1f, 1f);
                _gasFogTimer = 0f;
            }

            TargetRain = Mathf.Lerp(TargetRain, 0f, 0.025f * Time.deltaTime);
            TargetFog = Mathf.Lerp(TargetFog, _targetGasStrength, 0.025f * Time.deltaTime);
            TargetCloudDensity = Mathf.Lerp(TargetCloudDensity, _targetGasCloudStrength, 0.025f * Time.deltaTime);
            TargetLighteningThunder = Mathf.Lerp(TargetLighteningThunder, 0f, 0.1f * Time.deltaTime);
            TargetWindMagnitude = Mathf.Lerp(TargetWindMagnitude, 0f, 0.05f * Time.deltaTime);
        }

        private void DoExplosionWeather()
        {
            float delay = 200f;
            _elapsedTime += Time.deltaTime;

            TargetWindDirection = WeatherDebug.Direction.South;
            TargetTopWindDirection = Vector2.up;

            if (_elapsedTime >= delay)
            {
                TargetRain = Mathf.Lerp(TargetRain, 2f, 0.025f * Time.deltaTime);
                TargetFog = Mathf.Lerp(TargetFog, 0.075f, 0.025f * Time.deltaTime);
                TargetCloudDensity = Mathf.Lerp(TargetCloudDensity, 1f, 0.025f * Time.deltaTime);
                TargetLighteningThunder = Mathf.Lerp(TargetLighteningThunder, 1f, 0.1f * Time.deltaTime);
                TargetWindMagnitude = Mathf.Lerp(TargetWindMagnitude, 0.1f, 0.05f * Time.deltaTime);
            }
            else if (_elapsedTime >= 10f && _elapsedTime < delay)
            {
                TargetFog = Mathf.Lerp(TargetFog, 0f, 0.05f * Time.deltaTime);
                TargetCloudDensity = Mathf.Lerp(TargetCloudDensity, -0.75f, 0.25f * Time.deltaTime);
                TargetWindMagnitude = Mathf.Lerp(TargetWindMagnitude, 1.2f, 0.25f * Time.deltaTime);
            }
       
        }

        private void DoPreExplosionWeather()
        {
            TargetCloudDensity = 1;
            TargetFog = 0.05f;
            TargetRain = 0.1f;
            TargetWindMagnitude = 0;
            TargetLighteningThunder = 0;
            TargetWindDirection = WeatherDebug.Direction.East;
            TargetTopWindDirection = Vector2.down;
        }
    }
}
