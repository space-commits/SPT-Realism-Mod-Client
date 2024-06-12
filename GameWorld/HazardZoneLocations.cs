using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RealismMod
{
    public static class HazardZoneLocations
    {
        private static Dictionary<string, (float spawnChance, float strength, Vector3 position, Vector3 rotation, Vector3 size)> FactoryGasZones = new Dictionary<string, (float spawnChance, float strength, Vector3, Vector3, Vector3)>
        {
            { "FactoryTent", (0f, 25f, new Vector3(-17.5f, 0.35f, -41.4f), new Vector3(0f, 0f, 0f), new Vector3(6.5f, 4f, 38f)) },
            { "FactoryBasement", (0f, 50f, new Vector3(-6f, -3.6f, -20.5f), new Vector3(0f, 0f, 0f), new Vector3(25f, 3f, 36f)) },
            { "FactoryTanks", (0f, 40f, new Vector3(7f, -2.5f, 17f), new Vector3(0f, 0f, 0f), new Vector3(35f, 3f, 12f)) },
            { "FactoryVitamins", (10f, 15f, new Vector3(24.7f, 8.3f, 38.4f), new Vector3(0f, -2.5f, 0f), new Vector3(4.5f, 2f, 4f)) },
            { "FactoryPumpRoom", (10f, 15f, new Vector3(40f, 0.1f, -11.5f), new Vector3(0f, 0f, 0f), new Vector3(5.5f, 2f, 9.5f)) },
        };

        private static Dictionary<string, (float spawnChance, float strength, Vector3 position, Vector3 rotation, Vector3 size)> CustomsGasZones = new Dictionary<string, (float spawnChance, float strength, Vector3, Vector3, Vector3)>
        {
            { "CustomsSwitchBasement", (0f, 60f, new Vector3(335f, -3.1f, -60.3f), new Vector3(0f, 0f, 0f), new Vector3(25f, 6.5f, 50f)) },
            { "CustomsZB013", (0f, 20f, new Vector3(199f, -2.8f, -145f), new Vector3(0f, 0f, 0f), new Vector3(12f, 5f, 16f)) },
            { "CustomsWarehouse5", (0f, 80f, new Vector3(474.5f, 2.6f, -67f), new Vector3(0f, 9f, 0f), new Vector3(24f, 2.5f, 49f)) },
        };

        private static Dictionary<string, (float spawnChance, float strength, Vector3 position, Vector3 rotation, Vector3 size)> GZGasZones = new Dictionary<string, (float spawnChance, float strength, Vector3, Vector3, Vector3)>
        {
            { "GZUnderground", (-3f, 300f, new Vector3(80f, 15f, -20f), new Vector3(0f, 0f, 0f), new Vector3(50f, 3f, 110f)) },
        };

        private static Dictionary<string, (float spawnChance, float strength, Vector3 position, Vector3 rotation, Vector3 size)> ShorelineGasZones = new Dictionary<string, (float spawnChance, float strength, Vector3, Vector3, Vector3)>
        {
            { "ShorelinePool", (5f, 100f, new Vector3(-185f, -9.4f, -84f), new Vector3(0f, 0f, 0f), new Vector3(100f, 8f, 40f)) },
            { "ShorelineTruck1", (0f, 50f, new Vector3(-770f, -59.3f, 461f), new Vector3(0f, 0f, 0f), new Vector3(15f, 1.5f, 7f)) },
            { "ShorelineTunnel", (0f, 50f, new Vector3(385f, -59.9f, 310f), new Vector3(0f, 0f, 0f), new Vector3(35f, 8f, 15f)) },
            { "ShorelineSwamp1", (0f, 100f, new Vector3(273f, -55f, -125f), new Vector3(0f, 0f, 0f), new Vector3(18f, 5f, 20f)) }, //small next to graveyard
            { "ShorelineSwamp2", (0f, 100f, new Vector3(240f, -55f, -86f), new Vector3(0f, 8f, 0f), new Vector3(45f, 5f, 23f)) }, //next to church
            { "ShorelineSwamp3", (0f, 100f, new Vector3(240f, -54f, -165f), new Vector3(0f, 20f, 0f), new Vector3(40f, 5f, 75f)) },//big near church
            { "ShorelineSwamp4", (0f, 100f, new Vector3(305f, -55.3f, -150f), new Vector3(0f, -7f, 0f), new Vector3(25f, 5f, 75f)) },//middle
            { "ShorelineSwamp5", (0f, 100f, new Vector3(295, -53f, -85f), new Vector3(0f, 10f, 0f), new Vector3(28f, 5f, 45f)) }, //middle
            { "ShorelineSwamp6", (0f, 100f, new Vector3(360f, -55f, -155f), new Vector3(0f, 0f, 0f), new Vector3(45f, 5f, 30f)) }, //near truck
            { "ShorelineSwamp7", (0f, 100f, new Vector3(374f, -54f, -107f), new Vector3(0f, 0f, 0f), new Vector3(35f, 5f, 30f)) }, //tree island
            { "ShorelineSwamp8", (0f, 100f, new Vector3(340f, -55f, -90f), new Vector3(0f, 45f, 0f), new Vector3(30f, 5f, 60f)) }, //actual center
            { "ShorelineSanitarOffice", (10f, 20f, new Vector3(-321f, -3.6f, -77f), new Vector3(0f, 0f, 0f), new Vector3(8f, 2f, 6f)) },
            { "ShorelineTrench1", (5f, 100f, new Vector3(-615f, -29.6f, -250f), new Vector3(0f, 20f, 0f), new Vector3(7f, 1f, 90f)) },
            { "ShorelineTrench2", (5f, 100f, new Vector3(-555f, -29.6f, -212f), new Vector3(0f, -80f, 0f), new Vector3(7f, 1f, 90f)) },
            { "ShorelineVitamins", (10f, 20f, new Vector3(-188.3f, -3.7f, -88.5f), new Vector3(0f, 0f, 0f), new Vector3(5.8f, 1.5f, 6f)) },
        };

        private static Dictionary<string, (float spawnChance, float strength, Vector3 position, Vector3 rotation, Vector3 size)> StreetsZones = new Dictionary<string, (float spawnChance, float strength, Vector3, Vector3, Vector3)>
        {
            { "StreetsDrugs1", (8f, 25f, new Vector3(93f, 2.6f, 320f), new Vector3(0f, 0f, 0f), new Vector3(10.5f, 3f, 8f)) },
            { "StreetsPigs", (5f, 25f, new Vector3(137.6f, 3.8f, 314f), new Vector3(0f, 0f, 0f), new Vector3(3.5f, 2f, 5.5f)) },
            { "StreetsSewer", (5f, 50f, new Vector3(-262f, -2.7f, 211f), new Vector3(0f, 0f, 0f), new Vector3(20f, 2f, 40f)) },
            { "StreetsFactoryCourtyard", (5f, 120f, new Vector3(-112f, 2.2f, 211f), new Vector3(0f, 0f, 0f), new Vector3(40f, 2f, 16f)) },
            { "StreetsFactoryMain", (5f, 120f, new Vector3(-120f, 2.2f, 288.5f), new Vector3(0f, 0f, 0f), new Vector3(34f, 4.8f, 11f)) },
            { "StreetsFactoryUpper", (5f, 120f, new Vector3(-120f, 4f, 288.5f), new Vector3(0f, 0f, 0f), new Vector3(34f, 4.8f, 11f)) },
        };

        private static Dictionary<string, (float spawnChance, float strength, Vector3 position, Vector3 rotation, Vector3 size)> LabsGasZones = new Dictionary<string, (float spawnChance, float strength, Vector3, Vector3, Vector3)>
        {
            { "Labs", (10f, 350f, new Vector3(-193.5f, -4.1f, -342.9f), new Vector3(0f, 0f, 0f), new Vector3(200f, 25f, 200f)) },
        };

        private static Dictionary<string, (float spawnChance, float strength, Vector3 position, Vector3 rotation, Vector3 size)> InterchangeGasZones = new Dictionary<string, (float spawnChance, float strength, Vector3, Vector3, Vector3)>
        {
            { "InterchangeSaferoom", (10f, 10f, new Vector3(-48.4f, 22f, 43.6f), new Vector3(0f, 0f, 0f), new Vector3(10f, 3f, 4.5f)) },
            { "InterchangeMantis", (5f, 50f, new Vector3(17.5f, 27.1f, -72.3f), new Vector3(0f, 0f, 0f), new Vector3(28f, 2f, 21f)) },
            { "InterchangeMed1", (10f, 10f, new Vector3(23f, 27.1f, -106.6f), new Vector3(0f, 0f, 0f), new Vector3(22f, 2f, 12f)) },
            { "InterchangeMed2", (10f, 10f, new Vector3(10.5f, 27.5f, -105.6f), new Vector3(0f, 45f, 0f), new Vector3(13f, 2f, 7f)) },
            { "InterchangeMed3", (10f, 10f, new Vector3(11.6f, 27.9f, -102.5f), new Vector3(0f, 0f, 0f), new Vector3(15.5f, 2f, 3.5f)) },
            { "InterchangeMedOpp", (0f, 50f, new Vector3(23f, 28.1f, -134.8f), new Vector3(0f, 0f, 0f), new Vector3(22f, 2f, 15f)) },
        };

        private static Dictionary<string, (float spawnChance, float strength, Vector3 position, Vector3 rotation, Vector3 size)> LighthouseGasZones = new Dictionary<string, (float spawnChance, float strength, Vector3, Vector3, Vector3)>
        {
            { "LighthouseTunnel", (0f, 100f, new Vector3(-67f, 6f, 330f), new Vector3(0f, 8f, 0f), new Vector3(25f, 8f, 30f)) },
            { "LighthouseTrench", (0f, 100f, new Vector3(-98f, 1f, -584f), new Vector3(0f, 0f, 0f), new Vector3(80f, 3f, 6f)) },
            { "LighthouseWarehouse1", (0f, 100f, new Vector3(33f, 5f, -610f), new Vector3(0f, 0f, 0f), new Vector3(10f, 4f, 40.8f)) },
            { "LighthouseWarehouse2", (0f, 100f, new Vector3(51f, 5f, -610f), new Vector3(0f, 0f, 0f), new Vector3(10f, 4f, 40.8f)) },
            { "LighthouseWarehouse3", (0f, 100f, new Vector3(-89.1f, 5f, -733f), new Vector3(0f, 0f, 0f), new Vector3(27f, 4f, 10f)) },
            { "LighthouseWarehouse4", (0f, 100f, new Vector3(-95f, 5f, -750f), new Vector3(0f, 0f, 0f), new Vector3(25f, 4f, 15f)) },
            { "LighthouseWarehouse5", (0f, 100f, new Vector3(-182f, 5f, -676f), new Vector3(0f, 0f, 0f), new Vector3(17f, 4f, 32f)) },
            { "LighthouseTank1", (0f, 100f, new Vector3(-22f, 6f, -670.2f), new Vector3(0f, 15f, 0f), new Vector3(50f, 4f, 5f)) },
            { "LighthouseTank2", (0f, 100f, new Vector3(-95.7f, 6f, -670.2f), new Vector3(0f, -15f, 0f), new Vector3(50f, 4f, 5f)) },
            { "LighthouseTank3", (0f, 100f, new Vector3(-21.7f, 6f, -615.2f), new Vector3(0f, -45f, 0f), new Vector3(45f, 4f, 5f)) },
        };

        private static Dictionary<string, (float spawnChance, float strength, Vector3 position, Vector3 rotation, Vector3 size)> WoodsGasZones = new Dictionary<string, (float spawnChance, float strength, Vector3, Vector3, Vector3)>
        {
            { "WoodsBloodSample", (0f, 10f, new Vector3(-96f, -15f, 220f), new Vector3(0f, 0f, 0f), new Vector3(3f, 3f, 4f)) },
            { "WoodsScavBunker", (0f, 50f, new Vector3(230f, 20f, -708f), new Vector3(0f, 5f, 0f), new Vector3(25f, 3f, 10f)) }
        };

        private static Dictionary<string, (float spawnChance, float strength, Vector3 position, Vector3 rotation, Vector3 size)> ReserveGasZones = new Dictionary<string, (float spawnChance, float strength, Vector3, Vector3, Vector3)>
        {
            { "ReserveStorage", (0f, 500f, new Vector3(50f, -13.5f, -110f), new Vector3(0f, 0f, 0f), new Vector3(90f, 6f, 190f)) },
            { "ReserveShaft", (0f, 50f, new Vector3(-58.9f, -15.9f, 179.9f), new Vector3(0f, 0f, 0f), new Vector3(14f, 55f, 16f)) },
            { "ReserveD2Extract", (0f, 40f, new Vector3(-109f, -18.4f, 161f), new Vector3(0f, 0f, 0f), new Vector3(50f, 5f, 25f)) },
            { "ReserveD2Rat", (0f, 50f, new Vector3(-67f, -18.6f, 141f), new Vector3(0f, 15f, 0f), new Vector3(45f, 4.5f, 15f)) },
            { "ReserveD2Tank", (0f, 50f, new Vector3(-78.5f, -19.8f, 113f), new Vector3(0f, 15f, 0f), new Vector3(55f, 4.5f, 15f)) },
            { "ReserveBunker", (0f, 250f, new Vector3(-105.5f, -14.5f, 42.5f), new Vector3(0f, 9f, 0f), new Vector3(65f, 4.5f, 45f)) }
        };


        private static Dictionary<string, (float spawnChance, float strength, Vector3 position, Vector3 rotation, Vector3 size)> FactoryRadZones = new Dictionary<string, (float spawnChance, float strength, Vector3, Vector3, Vector3)>
        {
            { "FactoryTent", (0f, 25f, new Vector3(-17.5f, 0.35f, -41.4f), new Vector3(0f, 0f, 0f), new Vector3(6.5f, 4f, 38f)) },

        };

        private static Dictionary<string, (float spawnChance, float strength, Vector3 position, Vector3 rotation, Vector3 size)> CustomsRadZones = new Dictionary<string, (float spawnChance, float strength, Vector3, Vector3, Vector3)>
        {

        };

        private static Dictionary<string, (float spawnChance, float strength, Vector3 position, Vector3 rotation, Vector3 size)> GZRadZones = new Dictionary<string, (float spawnChance, float strength, Vector3, Vector3, Vector3)>
        {

        };

        private static Dictionary<string, (float spawnChance, float strength, Vector3 position, Vector3 rotation, Vector3 size)> ShorelineRadZones = new Dictionary<string, (float spawnChance, float strength, Vector3, Vector3, Vector3)>
        {

        };

        private static Dictionary<string, (float spawnChance, float strength, Vector3 position, Vector3 rotation, Vector3 size)> StreetsRadZones = new Dictionary<string, (float spawnChance, float strength, Vector3, Vector3, Vector3)>
        {

        };

        private static Dictionary<string, (float spawnChance, float strength, Vector3 position, Vector3 rotation, Vector3 size)> LabsRadZones = new Dictionary<string, (float spawnChance, float strength, Vector3, Vector3, Vector3)>
        {

        };

        private static Dictionary<string, (float spawnChance, float strength, Vector3 position, Vector3 rotation, Vector3 size)> InterchangeRadZones = new Dictionary<string, (float spawnChance, float strength, Vector3, Vector3, Vector3)>
        {

        };

        private static Dictionary<string, (float spawnChance, float strength, Vector3 position, Vector3 rotation, Vector3 size)> LighthouseRadZones = new Dictionary<string, (float spawnChance, float strength, Vector3, Vector3, Vector3)>
        {

        };

        private static Dictionary<string, (float spawnChance, float strength, Vector3 position, Vector3 rotation, Vector3 size)> WoodsRadZones = new Dictionary<string, (float spawnChance, float strength, Vector3, Vector3, Vector3)>
        {

        };

        private static Dictionary<string, (float spawnChance, float strength, Vector3 position, Vector3 rotation, Vector3 size)> ReserveRadZones = new Dictionary<string, (float spawnChance, float strength, Vector3, Vector3, Vector3)>
        {

        };

        public static Dictionary<string, (float spawnChance, float strength, Vector3 position, Vector3 rotation, Vector3 size)> GetGasZones(string map)
        {
            switch (map)
            {
                case "rezervbase":
                    return ReserveGasZones;
                case "bigmap":
                    return CustomsGasZones;
                case "factory4_night":
                case "factory4_day":
                    return FactoryGasZones;
                case "interchange":
                    return InterchangeGasZones;
                case "laboratory":
                    return LabsGasZones;
                case "shoreline":
                    return ShorelineGasZones;
                case "sandbox":
                    return GZGasZones;
                case "woods":
                    return WoodsGasZones;
                case "lighthouse":
                    return LighthouseGasZones;
                case "tarkovstreets":
                    return StreetsZones;
                default:
                    return null;
            }
        }

        public static Dictionary<string, (float spawnChance, float strength, Vector3 position, Vector3 rotation, Vector3 size)> GetRadZones(string map)
        {
            switch (map)
            {
                case "rezervbase":
                    return ReserveRadZones;
                case "bigmap":
                    return CustomsRadZones;
                case "factory4_night":
                case "factory4_day":
                    return FactoryRadZones;
                case "interchange":
                    return InterchangeRadZones;
                case "laboratory":
                    return LabsRadZones;
                case "shoreline":
                    return ShorelineRadZones;
                case "sandbox":
                    return GZRadZones;
                case "woods":
                    return WoodsRadZones;
                case "lighthouse":
                    return LighthouseRadZones;
                default:
                    return null;
            }
        }
    }
}
