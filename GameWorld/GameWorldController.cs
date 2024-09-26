using System;
using System.Collections.Generic;
using System.Text;

namespace RealismMod
{
    public static class GameWorldController
    {
        public static bool DoMapGasEvent { get; private set; } = false;
        public static bool IsHalloween { get; private set; } = false;
        public static bool GameStarted { get; set; } = false;
        public static string CurrentMap { get; set; } = "";
        public static bool MapWithDynamicWeather { get; set; } = false;

        public static bool MuteAmbientAudio
        {
            get
            {
                return DoMapGasEvent || HazardTracker.IsPreExplosion || HazardTracker.HasExploded;
            }
        }

        public static float GasEventStrength
        {
            get 
            {
                return DoMapGasEvent ? 0.05f : 0f;
            }
        }

        public static void CheckForEvents() 
        {
            CheckIsHalloween();
        }

        private static void CheckIsHalloween()
        {
            DoMapGasEvent = true; //do a % chance if halloween or X quest at raid start, and not factory or labs, ideally server needs to dictate this value so that bots can be given gasmasks
            IsHalloween = true;
            return;
            DateTime currentDate = DateTime.Now;
            DateTime startDate = new DateTime(currentDate.Year, 10, 25);
            DateTime endDate = new DateTime(currentDate.Month, 11, 2);
            if (currentDate >= startDate && currentDate <= endDate) IsHalloween = true;
        }

    }
}
