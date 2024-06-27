using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RealismMod
{
    public static class HazardZoneLocations
    {
        //Gas
        private static Dictionary<string, (float spawnChance, float strength, Vector3 position, Vector3 rotation, Vector3 size)> FactoryGasZones = new Dictionary<string, (float spawnChance, float strength, Vector3, Vector3, Vector3)>
        {
            { "FactoryTent", (0.8f, 20f, new Vector3(-17.5f, 0.35f, -41.4f), new Vector3(0f, 0f, 0f), new Vector3(6.5f, 4f, 38f)) },
            { "FactoryBasement", (0.25f, 100f, new Vector3(-6f, -3.6f, -20.5f), new Vector3(0f, 0f, 0f), new Vector3(25f, 3f, 36f)) },
            { "FactoryTanks", (0.25f, 30f, new Vector3(7f, -2.5f, 17f), new Vector3(0f, 0f, 0f), new Vector3(35f, 3f, 12f)) },
            { "FactoryVitamins", (1f, 10f, new Vector3(24.7f, 8.3f, 38.4f), new Vector3(0f, -2.5f, 0f), new Vector3(4.5f, 2f, 4f)) },
            { "FactoryPumpRoom", (1f, 10f, new Vector3(40f, 0.1f, -11.5f), new Vector3(0f, 0f, 0f), new Vector3(5.5f, 2f, 9.5f)) },
            { "FactoryCrashRoom", (0.05f, 150f, new Vector3(7f, -0.5f, -19f), new Vector3(0f, 0f, 0f), new Vector3(60f, 5.5f, 35.5f)) },
            { "FactoryMechanicTunnel", (0.2f, 100f, new Vector3(-16.5f, -2.5f, 34f), new Vector3(0f, 0f, 0f), new Vector3(60f, 3f, 22f)) },
            { "FactoryExtractRoom", (0.05f, 200f, new Vector3(56f, 3f, -7f), new Vector3(0f, 0f, 0f), new Vector3(38f, 7f, 150f)) }, //main extract
            { "FactorySpawn", (0.05f, 200f, new Vector3(-22f, 2f, 49f), new Vector3(0f, 0f, 0f), new Vector3(65f, 9f, 30f)) },
        };

        private static Dictionary<string, (float spawnChance, float strength, Vector3 position, Vector3 rotation, Vector3 size)> CustomsGasZones = new Dictionary<string, (float spawnChance, float strength, Vector3, Vector3, Vector3)>
        {
            { "CustomsSwitchBasement", (0.5f, 60f, new Vector3(335f, -3.1f, -60.3f), new Vector3(0f, 0f, 0f), new Vector3(25f, 6.5f, 50f)) },
            { "CustomsWarehouse5", (1f, 70f, new Vector3(474.5f, 2.6f, -67f), new Vector3(0f, 9f, 0f), new Vector3(24f, 2.5f, 49f)) }, //quest location
            { "CustomsGasTrain1", (1f, 50f, new Vector3(460f, 2f, 185f), new Vector3(0f, 40f, 0f), new Vector3(10f, 10f, 45f)) },
            { "CustomsGasTrain2", (1f, 25f, new Vector3(466f, 0f, 208f), new Vector3(0f, 10f, 0f), new Vector3(5f, 3f, 10f)) },
            { "CustomsCrackDen", (0.75f, 30f, new Vector3(88f, 5f, -157f), new Vector3(0f, -12f, 0f), new Vector3(15f, 2f, 27f)) },
            { "CustomsPumpWarehouse", (0.25f, 85f, new Vector3(557f, 1.5f, -120.5f), new Vector3(0f, 7f, 0f), new Vector3(40f, 15f, 23f)) },
            { "CustomsPumpRoom", (0.75f, 75f, new Vector3(612f, 1.5f, -129.8f), new Vector3(0f, 6f, 0f), new Vector3(23.5f, 6.5f, 15f)) },
            { "CustomsWarehouse3", (0.5f, 75f, new Vector3(391.5f, 1f, -97f), new Vector3(0f, 8.5f, 0f), new Vector3(53.5f, 6.5f, 29f)) },
            { "CustomsRiverContainer", (1f, 12.5f, new Vector3(-96f, -10f, -16.5f), new Vector3(0f, 25f, 0f), new Vector3(12f, 4f, 25f)) }, //quest location
            { "CustomsAquarius", (1f, 500f, new Vector3(234f, 3.5f, 138f), new Vector3(0f, 9f, 0f), new Vector3(7.5f, 2f, 3.1f)) }, //she's a sneaky bitch
        };

        private static Dictionary<string, (float spawnChance, float strength, Vector3 position, Vector3 rotation, Vector3 size)> GZGasZones = new Dictionary<string, (float spawnChance, float strength, Vector3, Vector3, Vector3)>
        {
            { "GZUnderground1", (0.35f, 250f, new Vector3(80f, 15f, -20f), new Vector3(0f, 0f, 0f), new Vector3(50f, 4f, 120f)) },
            { "GZUnderground2", (0.35f, 250f, new Vector3(82f, 15f, 132f), new Vector3(0f, 0f, 0f), new Vector3(45f, 4f, 95f)) },
            { "GZTerragroupBuilding1", (0.7f, 175f, new Vector3(-45f, 25f, 19f), new Vector3(0f, 0f, 0f), new Vector3(45f, 7f, 100f)) }
        };

        private static Dictionary<string, (float spawnChance, float strength, Vector3 position, Vector3 rotation, Vector3 size)> ShorelineGasZones = new Dictionary<string, (float spawnChance, float strength, Vector3, Vector3, Vector3)>
        {
            { "ShorelinePool", (1f, 100f, new Vector3(-185f, -9.4f, -84f), new Vector3(0f, 0f, 0f), new Vector3(100f, 8f, 40f)) },
            { "ShorelineTunnel", (1f, 50f, new Vector3(385f, -59.9f, 310f), new Vector3(0f, 0f, 0f), new Vector3(35f, 8f, 15f)) },
            { "ShorelineSwamp1", (1f, 100f, new Vector3(273f, -55f, -125f), new Vector3(0f, 0f, 0f), new Vector3(18f, 5f, 20f)) }, //small next to graveyard
            { "ShorelineSwamp2", (1f, 100f, new Vector3(240f, -55f, -86f), new Vector3(0f, 8f, 0f), new Vector3(45f, 5f, 23f)) }, //next to church
            { "ShorelineSwamp3", (1f, 100f, new Vector3(240f, -54f, -165f), new Vector3(0f, 20f, 0f), new Vector3(40f, 5f, 75f)) },//big near church
            { "ShorelineSwamp4", (1f, 100f, new Vector3(305f, -55.3f, -150f), new Vector3(0f, -7f, 0f), new Vector3(25f, 5f, 75f)) },//middle
            { "ShorelineSwamp5", (1f, 100f, new Vector3(295, -53f, -85f), new Vector3(0f, 10f, 0f), new Vector3(28f, 5f, 45f)) }, //middle
            { "ShorelineSwamp6", (1f, 100f, new Vector3(360f, -55f, -155f), new Vector3(0f, 0f, 0f), new Vector3(45f, 5f, 30f)) }, //near truck
            { "ShorelineSwamp7", (1f, 100f, new Vector3(374f, -54f, -107f), new Vector3(0f, 0f, 0f), new Vector3(35f, 5f, 30f)) }, //tree island
            { "ShorelineSwamp8", (1f, 100f, new Vector3(340f, -55f, -90f), new Vector3(0f, 45f, 0f), new Vector3(30f, 5f, 60f)) }, //actual center
            { "ShorelineSanitarOffice", (1f, 20f, new Vector3(-321f, -3.6f, -77f), new Vector3(0f, 0f, 0f), new Vector3(8f, 2f, 6f)) },
            { "ShorelineTrench1", (1f, 100f, new Vector3(-615f, -29.6f, -250f), new Vector3(0f, 20f, 0f), new Vector3(7f, 1f, 90f)) },
            { "ShorelineTrench2", (1f, 100f, new Vector3(-555f, -29.6f, -212f), new Vector3(0f, -80f, 0f), new Vector3(7f, 1f, 90f)) },
            { "ShorelineVitamins", (1f, 20f, new Vector3(-188.3f, -3.7f, -88.5f), new Vector3(0f, 0f, 0f), new Vector3(5.8f, 1.5f, 6f)) },
            { "Shoreline108", (1f, 100f, new Vector3(-337f, -3f, -83f), new Vector3(0f, 0f, 0f), new Vector3(40f, 3f, 17.5f)) }, //cargo x
        };

        private static Dictionary<string, (float spawnChance, float strength, Vector3 position, Vector3 rotation, Vector3 size)> StreetsGasZones = new Dictionary<string, (float spawnChance, float strength, Vector3, Vector3, Vector3)>
        {
            { "StreetsDrugs1", (1f, 17f, new Vector3(93f, 2.6f, 320f), new Vector3(0f, 0f, 0f), new Vector3(10.5f, 3f, 8f)) },
            { "StreetsPigs", (1f, 25f, new Vector3(137.6f, 3.8f, 314f), new Vector3(0f, 0f, 0f), new Vector3(3.5f, 2f, 5.5f)) },
            { "StreetsSewer", (1f, 70f, new Vector3(-262f, -2.7f, 211f), new Vector3(0f, 0f, 0f), new Vector3(20f, 2f, 40f)) },
            { "StreetsFactoryCourtyard", (0.75f, 90f, new Vector3(-111f, 2.2f, 275f), new Vector3(0f, 0f, 0f), new Vector3(30f, 3f, 16f)) },
            { "StreetsFactoryMain", (0.75f, 50f, new Vector3(-120f, 2.2f, 288.5f), new Vector3(0f, 0f, 0f), new Vector3(34f, 4.8f, 11f)) },
            { "StreetsFactoryUpper", (0.75f, 60f, new Vector3(-120f, 10f, 288.5f), new Vector3(0f, 0f, 0f), new Vector3(34f, 4f, 11f)) },
            { "StreetsFactoryBigRoom", (0.75f, 120f, new Vector3(-147f, 6f, 292), new Vector3(0f, 29f, 0f), new Vector3(20f, 7f, 12.5f)) },
            { "StreetsFactoryBigRoomBack", (0.75f, 120f, new Vector3(-89.5f, 6f, 278f), new Vector3(0f, 0f, 0f), new Vector3(11f, 7f, 23f)) },
            { "StreetsFactoryMarkedBuilding", (0.75f, 120f, new Vector3(-135f, 3f, 269f), new Vector3(0f, 0f, 0f), new Vector3(13f, 8f, 12.5f)) },
            { "StreetsTerragroupOffice1", (1f, 100f, new Vector3(53f, 1f, -74f), new Vector3(0f, 0f, 0f), new Vector3(22f, 2f, 13f)) },
            { "StreetsTerragroupOffice2", (1f, 100f, new Vector3(60f, 1f, -62f), new Vector3(0f, 0f, 0f), new Vector3(15f, 2f, 9f)) },
            { "StreetsTerragroupOffice3", (1f, 100f, new Vector3(45.5f, 1f, -53.5f), new Vector3(0f, 0f, 0f), new Vector3(8.5f, 2f, 14f)) },
            { "StreetsUndergroundCarpark", (0.35f, 350f, new Vector3(208f, 0f, 375f), new Vector3(0f, 0f, 0f), new Vector3(140f, 3f, 70f)) },
        };

        private static Dictionary<string, (float spawnChance, float strength, Vector3 position, Vector3 rotation, Vector3 size)> LabsGasZones = new Dictionary<string, (float spawnChance, float strength, Vector3, Vector3, Vector3)>
        {
            { "Labs", (1f, 380f, new Vector3(-193.5f, -4.1f, -342.9f), new Vector3(0f, 0f, 0f), new Vector3(200f, 25f, 200f)) },
        };

        private static Dictionary<string, (float spawnChance, float strength, Vector3 position, Vector3 rotation, Vector3 size)> InterchangeGasZones = new Dictionary<string, (float spawnChance, float strength, Vector3, Vector3, Vector3)>
        {
            { "InterchangeSaferoom", (1f, 10f, new Vector3(-48.4f, 22f, 43.6f), new Vector3(0f, 0f, 0f), new Vector3(10f, 3f, 4.5f)) },
            { "InterchangeMantis", (0.75f, 50f, new Vector3(17.5f, 27.1f, -72.3f), new Vector3(0f, 0f, 0f), new Vector3(28f, 2f, 21f)) },
            { "InterchangeMed1", (1f, 10f, new Vector3(23f, 27.1f, -106.6f), new Vector3(0f, 0f, 0f), new Vector3(22f, 2f, 12f)) },
            { "InterchangeMed2", (1f, 10f, new Vector3(10.5f, 27.5f, -105.6f), new Vector3(0f, 45f, 0f), new Vector3(13f, 2f, 7f)) },
            { "InterchangeMed3", (1f, 10f, new Vector3(11.6f, 27.9f, -102.5f), new Vector3(0f, 0f, 0f), new Vector3(15.5f, 2f, 3.5f)) },
            { "InterchangeMedOpp", (0.5f, 50f, new Vector3(23f, 28.1f, -134.8f), new Vector3(0f, 0f, 0f), new Vector3(22f, 2f, 15f)) },
            { "InterchangeBasementGoshan", (0.4f, 125f, new Vector3(-18f, 22f, -55f), new Vector3(0f, 0f, 0f), new Vector3(70f, 4f, 50f)) },
        };

        private static Dictionary<string, (float spawnChance, float strength, Vector3 position, Vector3 rotation, Vector3 size)> LighthouseGasZones = new Dictionary<string, (float spawnChance, float strength, Vector3, Vector3, Vector3)>
        {
            { "LighthouseTunnel", (1f, 100f, new Vector3(-67f, 6f, 330f), new Vector3(0f, 8f, 0f), new Vector3(25f, 8f, 30f)) },
            { "LighthouseTrench", (0.75f, 100f, new Vector3(-98f, 1f, -584f), new Vector3(0f, 0f, 0f), new Vector3(80f, 3f, 6f)) },
            { "LighthouseWarehouse1", (0.5f, 100f, new Vector3(33f, 5f, -610f), new Vector3(0f, 0f, 0f), new Vector3(10f, 4f, 40.8f)) },
            { "LighthouseWarehouse2", (0.5f, 100f, new Vector3(51f, 5f, -610f), new Vector3(0f, 0f, 0f), new Vector3(10f, 4f, 40.8f)) },
            { "LighthouseWarehouse3", (0.5f, 100f, new Vector3(-89.1f, 5f, -733f), new Vector3(0f, 0f, 0f), new Vector3(27f, 4f, 10f)) },
            { "LighthouseWarehouse4", (0.5f, 100f, new Vector3(-95f, 5f, -750f), new Vector3(0f, 0f, 0f), new Vector3(25f, 4f, 15f)) },
            { "LighthouseWarehouse5", (0.5f, 100f, new Vector3(-182f, 5f, -676f), new Vector3(0f, 0f, 0f), new Vector3(17f, 4f, 32f)) },
            { "LighthouseTank1", (1f, 100f, new Vector3(-22f, 6f, -670.2f), new Vector3(0f, 15f, 0f), new Vector3(50f, 4f, 5f)) },
            { "LighthouseTank2", (1f, 100f, new Vector3(-95.7f, 6f, -670.2f), new Vector3(0f, -15f, 0f), new Vector3(50f, 4f, 5f)) },
            { "LighthouseTank3", (1f, 100f, new Vector3(-21.7f, 6f, -615.2f), new Vector3(0f, -45f, 0f), new Vector3(45f, 4f, 5f)) },
            { "LighthouseDrugLab", (1f, 100f, new Vector3(-119f, 10.5f, -841f), new Vector3(0f, 0f, 0f), new Vector3(15f, 4f, 10f)) },
            { "LighthouseTrainWarehouse", (0.5f, 250f, new Vector3(46f, 12f, -850.5f), new Vector3(0f, 0f, 0f), new Vector3(23.5f, 5f, 48f)) },
        };

        private static Dictionary<string, (float spawnChance, float strength, Vector3 position, Vector3 rotation, Vector3 size)> WoodsGasZones = new Dictionary<string, (float spawnChance, float strength, Vector3, Vector3, Vector3)>
        {
            { "WoodsBloodSample", (1f, 10f, new Vector3(-96f, -15f, 220f), new Vector3(0f, 0f, 0f), new Vector3(3f, 3f, 4f)) },
            { "WoodsScavBunker", (0.75f, 50f, new Vector3(230f, 20f, -708f), new Vector3(0f, 5f, 0f), new Vector3(25f, 3f, 10f)) },
            { "WoodsBodiesContainer", (1f, 25f, new Vector3(-201.8f, -1f, 233.7f), new Vector3(0f, -27f, 0f), new Vector3(2f, 2.5f, 12f)) },
            { "WoodsMedCampTent", (0.25f, 100f, new Vector3(-185, -1f, 218f), new Vector3(0f, 0f, 0f), new Vector3(10f, 2f, 25f)) },
            { "WoodsMedContainers", (0.25f, 100f, new Vector3(-205f, -1f, 215f), new Vector3(0f, 0f, 0f), new Vector3(12f, 2f, 27f)) },
            { "WoodsMedAmbulances", (0.25f, 100f, new Vector3(-185f, -1f, 244f), new Vector3(0f, 0f, 0f), new Vector3(20f, 2.5f, 20f)) },
        };

        private static Dictionary<string, (float spawnChance, float strength, Vector3 position, Vector3 rotation, Vector3 size)> ReserveGasZones = new Dictionary<string, (float spawnChance, float strength, Vector3, Vector3, Vector3)>
        {
            { "ReserveHallway1", (1f, 100f, new Vector3(-68.4f, -15f, 162f), new Vector3(0f, 30f, 0f), new Vector3(35f, 3f, 25f)) },
            { "ReserveHallway2", (1f, 100f, new Vector3(-85.5f, -15f, 132f), new Vector3(0f, 30f, 0f), new Vector3(4f, 3f, 20f)) },
            { "ReserveShaft", (1f, 50f, new Vector3(-58.9f, -15.9f, 179.9f), new Vector3(0f, 0f, 0f), new Vector3(14f, 55f, 16f)) },
            { "ReserveStairsToD2", (1f, 100f, new Vector3(-70f, -16f, 78f), new Vector3(0f, 20f, 0f), new Vector3(11f, 8f, 38f)) },
            { "ReserveD2Rat", (1f, 40f, new Vector3(-63f, -18.6f, 139f), new Vector3(0f, 30f, 0f), new Vector3(50f, 15f, 14f)) },
            { "ReserveD2Tank", (1f, 45f, new Vector3(-78.5f, -19.8f, 113f), new Vector3(0f, 30f, 0f), new Vector3(55f, 10f, 15f)) },
            { "ReserveBunker", (0.35f, 225f, new Vector3(-105f, -14.5f, 40f), new Vector3(0f, 15f, 0f), new Vector3(65f, 4.5f, 45f)) },
            { "ReserveStorage", (0.15f, 500f, new Vector3(50f, -13.5f, -110f), new Vector3(0f, 0f, 0f), new Vector3(90f, 6f, 190f)) },
            { "ReserveStorageSide1", (0.85f, 80f, new Vector3(76f, -12f, -136f), new Vector3(0f, 15f, 0f), new Vector3(16f, 3f, 29f)) },
            { "ReserveStorageSide2", (0.85f, 80f, new Vector3(40f, -12f, -126f), new Vector3(0f, 15f, 0f), new Vector3(16f, 3f, 29f)) },
            { "ReserveWhiteBishop", (0.55f, 190f, new Vector3(-66f, -9f, -32f), new Vector3(0f, 15f, 0f), new Vector3(35f, 3f, 20f)) }, //basement
            { "ReserveBlackBishop", (0.55f, 200f, new Vector3(-138f, -9f, -9f), new Vector3(0f, 15f, 0f), new Vector3(69f, 3f, 23.5f)) }, //basement
            { "ReserveBlackBishopHall", (0.4f, 220f, new Vector3(-134f, -12f, 14f), new Vector3(0f, 15f, 0f), new Vector3(50f, 4f, 23f)) }, //basemment
            { "ReserveBlackPawnHall", (0.4f, 220f, new Vector3(-146f, -11f, 49f), new Vector3(0f, 15f, 0f), new Vector3(30f, 4f, 50f)) }, //basemment
            { "ReserveWhitePawnHall", (0.4f, 220f, new Vector3(-103f, -10f, 72.5f), new Vector3(0f, 15f, 0f), new Vector3(70f, 4f, 25f)) }, //basemment
        }; 

        //Radiation
        private static Dictionary<string, (float spawnChance, float strength, Vector3 position, Vector3 rotation, Vector3 size)> FactoryRadZones = new Dictionary<string, (float spawnChance, float strength, Vector3, Vector3, Vector3)>
        {
            { "FactoryGate0", (1f, 20f, new Vector3(-60f, 1f, 56.5f), new Vector3(0f, 0f, 0f), new Vector3(13f, 4f, 10f)) },
            { "FactoryBarrels", (1f, 10f, new Vector3(56.5f, -1f, -28f), new Vector3(0f, 0f, 0f), new Vector3(9f, 11f, 5f)) },
            { "FactoryCellars", (1f, 27f, new Vector3(71f, -4f, -28.8f), new Vector3(0f, 0f, 0f), new Vector3(20f, 7f, 2.5f)) },
        };

        private static Dictionary<string, (float spawnChance, float strength, Vector3 position, Vector3 rotation, Vector3 size)> CustomsRadZones = new Dictionary<string, (float spawnChance, float strength, Vector3, Vector3, Vector3)>
        {
            { "CustomsRadTrain1", (1f, 150f, new Vector3(434f, 3.6f, 150f), new Vector3(0f, 40f, 0f), new Vector3(9f, 10f, 35f)) },
            { "CustomsRadTrain2", (1f, 100f, new Vector3(465f, 3.6f, 200f), new Vector3(0f, 10f, 0f), new Vector3(15f, 5f, 10f)) },
            { "CustomsRadTrain3", (1f, 150f, new Vector3(484f, 3.6f, 219f), new Vector3(0f, -33f, 0f), new Vector3(25f, 10f, 10f)) },
            { "CustomsTrainExtract", (1f, 50f, new Vector3(479f, 1.4f, 229f), new Vector3(0f, 20f, 0f), new Vector3(25f, 10f, 8f)) },
            { "CustomsBigRed", (1f, 300f, new Vector3(-213f, 1f, -122f), new Vector3(0f, 3f, 0f), new Vector3(24f, 5f, 59f)) },
            { "CustomsGarrage", (1f, 90f, new Vector3(108f, 0f, -92f), new Vector3(0f, -12f, 0f), new Vector3(19f, 10f, 11.5f)) },
            { "CustomsZB013", (1f, 80f, new Vector3(199f, -2.8f, -155f), new Vector3(0f, -10f, 0f), new Vector3(17.5f, 5f, 35f)) },
            { "CustomsOldGasExit", (1f, 50f, new Vector3(311f, -2f, -180f), new Vector3(0f, -12f, 0f), new Vector3(10f, 5f, 15f)) },
            { "CustomsZB", (1f, 50f, new Vector3(465f, -2f, -112f), new Vector3(0f, 0f, 0f), new Vector3(15f, 2f, 5f)) },
            { "CustomsMarkedRoom", (1f, 15f, new Vector3(183.5f, 7f, 182f), new Vector3(0f, 7f, 0f), new Vector3(6.5f, 3f, 6f)) },
        };

        private static Dictionary<string, (float spawnChance, float strength, Vector3 position, Vector3 rotation, Vector3 size)> GZRadZones = new Dictionary<string, (float spawnChance, float strength, Vector3, Vector3, Vector3)>
        {
            { "GZTerragroupRubble", (1f, 140f, new Vector3(-58f, 25f, 22f), new Vector3(0f, 0f, 0f), new Vector3(25f, 10f, 30f)) }
        };

        private static Dictionary<string, (float spawnChance, float strength, Vector3 position, Vector3 rotation, Vector3 size)> ShorelineRadZones = new Dictionary<string, (float spawnChance, float strength, Vector3, Vector3, Vector3)>
        {
           { "ShorelineTruck1", (1f, 50f, new Vector3(-770f, -59.3f, 461f), new Vector3(0f, 0f, 0f), new Vector3(15f, 1.5f, 7f)) },
           { "ShorelineChurch", (1f, 70f, new Vector3(-256, -54f, -105f), new Vector3(0f, 0f, 0f), new Vector3(17f, 4f, 12f)) },
           { "ShorelineMarked", (1f, 40f, new Vector3(-337.5f, 3f, -88f), new Vector3(0f, 0f, 0f), new Vector3(6.4f, 2f, 8f)) },
        };

        private static Dictionary<string, (float spawnChance, float strength, Vector3 position, Vector3 rotation, Vector3 size)> StreetsRadZones = new Dictionary<string, (float spawnChance, float strength, Vector3, Vector3, Vector3)>
        {
             { "StreetsMarkedFactory", (1f, 35f, new Vector3(-131f, 8.8f, 269f), new Vector3(0f, 0f, 0f), new Vector3(6f, 3f, 13.5f)) },
             { "StreetsMarked1", (1f, 75f, new Vector3(182f, 0f, 226.5f), new Vector3(0f, 0f, 0f), new Vector3(18f, 3f, 10f)) },
             { "StreetsMarked2", (1f, 50f, new Vector3(203f, 0f, 228f), new Vector3(0f, 0f, 0f), new Vector3(15f, 2f, 8.5f)) },
             { "StreetsXray", (1f, 75f, new Vector3(185.5f, 2f, 102f), new Vector3(0f, 0f, 0f), new Vector3(8f, 2.5f, 5.5f)) },
        };

        private static Dictionary<string, (float spawnChance, float strength, Vector3 position, Vector3 rotation, Vector3 size)> LabsRadZones = new Dictionary<string, (float spawnChance, float strength, Vector3, Vector3, Vector3)>
        {
            { "LabsGreenGoo", (1f, 20f, new Vector3(-173f, 2f, -371), new Vector3(0f, 0f, 0f), new Vector3(12f, 5f, 14f)) },
            { "LabsAreaAroundGoo", (1f, 150f, new Vector3(-174f, 2f, -367), new Vector3(0f, 0f, 0f), new Vector3(43f, 4f, 37f)) },
            { "LabsGooBarrels", (1f, 20f, new Vector3(-174f, 2f, -355f), new Vector3(0f, 0f, 0f), new Vector3(5f, 5f, 5f)) },
            { "LabsGreen", (1f, 80f, new Vector3(-131f, 6f, -366f), new Vector3(0f, 0f, 0f), new Vector3(17.5f, 5f, 25f)) },
            { "LabsBlack", (1f, 50f, new Vector3(-131f, 1f, -357.5f), new Vector3(0f, 0f, 0f), new Vector3(19f, 4.5f, 12f)) },
            { "LabsBlue1", (1f, 38f, new Vector3(-133f, 1.5f, -400.5f), new Vector3(0f, 0f, 0f), new Vector3(14f, 3f, 11f)) },
            { "LabsBlue2", (1f, 28f, new Vector3(-124.5f, 1.5f, -397f), new Vector3(0f, 0f, 0f), new Vector3(6.5f, 3f, 7f)) },
        };

        private static Dictionary<string, (float spawnChance, float strength, Vector3 position, Vector3 rotation, Vector3 size)> InterchangeRadZones = new Dictionary<string, (float spawnChance, float strength, Vector3, Vector3, Vector3)>
        {
             { "InterchangeWaterGoshan", (1f, 50f, new Vector3(-58f, 19.3f, -55f), new Vector3(0f, 0f, 0f), new Vector3(20f, 4f, 20f)) },
        };

        private static Dictionary<string, (float spawnChance, float strength, Vector3 position, Vector3 rotation, Vector3 size)> LighthouseRadZones = new Dictionary<string, (float spawnChance, float strength, Vector3, Vector3, Vector3)>
        {
            { "LighthouseLighthouseBasement", (1f, 25f, new Vector3(440f, 20.5f, 460f), new Vector3(0f, 15f, 0f), new Vector3(13f, 6f, 10f)) },
            { "LighthouseLighthouse1", (1f, 150f, new Vector3(440f, 24f, 460f), new Vector3(0f, 15f, 0f), new Vector3(8f, 2f, 8f)) },
            { "LighthouseCultRoom1", (1f, 50f, new Vector3(444f, 29f, 463f), new Vector3(0f, 15f, 0f), new Vector3(6f, 2f, 7f)) },
            { "LighthouseBeachCargo", (1f, 25f, new Vector3(136.9f, 1f, 47.8f), new Vector3(0f, 0f, 0f), new Vector3(7f, 3f, 7f)) }, // cargo x
        };

        private static Dictionary<string, (float spawnChance, float strength, Vector3 position, Vector3 rotation, Vector3 size)> WoodsRadZones = new Dictionary<string, (float spawnChance, float strength, Vector3, Vector3, Vector3)>
        {
             { "WooodsMarked1", (1f, 50f, new Vector3(-89f, 12f, -717f), new Vector3(0f, 0f, 0f), new Vector3(5f, 3f, 5f)) },
             { "WooodsMarkedShed", (1f, 50f, new Vector3(-80f, 12f, -726f), new Vector3(0f, 25f, 0f), new Vector3(5f, 5f, 5f)) },
             { "WooodsMarked2", (1f, 50f, new Vector3(195.6f, 0.2f, -7f), new Vector3(0f, 25f, 0f), new Vector3(7f, 3f, 7f)) },
             { "WoodsMountainBunker1", (1f, 40f, new Vector3(-164.1f, 46f, -235.5f), new Vector3(0f, -20f, 0f), new Vector3(15f, 2.5f, 5f)) },
             { "WoodsMountainBunkerDoor", (1f, 50f, new Vector3(-156f, 51f, -273f), new Vector3(0f, -20f, 0f), new Vector3(10f, 2.5f, 5f)) }
        };

        private static Dictionary<string, (float spawnChance, float strength, Vector3 position, Vector3 rotation, Vector3 size)> ReserveRadZones = new Dictionary<string, (float spawnChance, float strength, Vector3, Vector3, Vector3)>
        {
            { "ReserveD2Extract", (1f, 100f, new Vector3(-109f, -18.4f, 161f), new Vector3(0f, 0f, 0f), new Vector3(53f, 5f, 27f)) },
            { "ReserveWater", (1f, 75f, new Vector3(-99f, -15f, 5.5f), new Vector3(0f, 15f, 0f), new Vector3(29.5f, 2f, 14.5f)) },
            { "ReserveHermetic", (1f, 100f, new Vector3(62f, -7f, -195f), new Vector3(0f, 15f, 0f), new Vector3(18f, 2f, 16f)) },
            { "ReserveMarkedTrain", (1f, 50f, new Vector3(191.5f, -7f, -226.3f), new Vector3(0f, 15f, 0f), new Vector3(4f, 3f, 4f)) }, 
            { "ReserveMarkedBunker", (1f, 50f, new Vector3(-123f, -14f, 28.5f), new Vector3(0f, 15f, 0f), new Vector3(6f, 4f, 6f)) }, 
            { "ReserveMarkedPawn", (1f, 50f, new Vector3(-154f, -9f, 74f), new Vector3(0f, 15f, 0f), new Vector3(7f, 4f, 7.5f)) }, //black pawn
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
                    return StreetsGasZones;
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
                case "tarkovstreets":
                    return StreetsRadZones;
                default:
                    return null;
            }
        }
    }
}
