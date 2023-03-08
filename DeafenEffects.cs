using EFT;
using System;
using UnityEngine;
using System.Linq;
using Comfort.Common;
using System.Reflection;
using EFT.InventoryLogic;
using System.Threading.Tasks;
using Aki.Reflection.Patching;
using static Val;
using HarmonyLib;
using Aki.Reflection.Utils;
using UnityEngine.Rendering.PostProcessing;
using notGreg.UniformAim;
using static EFT.Interactive.BetterPropagationGroups;
using BepInEx.Logging;

namespace RealismMod
{
    public class VignettePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(EffectsController).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [PatchPostfix]
        private static void PatchPostfix(EffectsController __instance)
        {
            CC_FastVignette vig = (CC_FastVignette)AccessTools.Field(typeof(EffectsController), "cc_FastVignette_0").GetValue(__instance);
            Plugin.Vignette = vig;
        }
    }

    public class UpdatePhonesPatch : ModulePatch
    {

        private static float HelmDeafFactor(EquipmentClass equipment)
        {
            string deafStrength = "None";
            LootItemClass headwearItem = equipment.GetSlot(EquipmentSlot.Headwear).ContainedItem as LootItemClass;

            if (headwearItem != null)
            {
                ArmorClass helmet = equipment.GetSlot(EquipmentSlot.Headwear).ContainedItem as ArmorClass;
                if (helmet?.Armor?.Deaf != null)
                {
                    deafStrength = helmet.Armor.Deaf.ToString();
                }

            }
            return HelmDeafFactorHelper(deafStrength);
        }

        private static float HelmDeafFactorHelper(string deafStr)
        {
            switch (deafStr)
            {
                case "Low":
                    return 0.9f;
                case "High":
                    return 0.8f;
                default:
                    return 1f;
            }
        }

        private static float HeadsetDeafFactor(EquipmentClass equipment)
        {
            float protectionFactor;

            LootItemClass headwear = equipment.GetSlot(EquipmentSlot.Headwear).ContainedItem as LootItemClass;
            GClass2295 headset = (equipment.GetSlot(EquipmentSlot.Earpiece).ContainedItem as GClass2295) ?? ((headwear != null) ? headwear.GetAllItemsFromCollection().OfType<GClass2295>().FirstOrDefault<GClass2295>() : null);

            if (headset != null)
            {
                Plugin.HasHeadSet = true;
                GClass2202 headphone = headset.Template;
                protectionFactor = ((headphone.DryVolume / 100f) + 1f) * 1.3f;
            }
            else
            {
                Plugin.HasHeadSet = false;
                protectionFactor = 1f;
            }

            return protectionFactor;
        }


        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player).GetMethod("UpdatePhones", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [PatchPostfix]
        private static void PatchPostfix(Player __instance)
        {
            if (__instance.IsYourPlayer)
            {
                EquipmentClass equipment = (EquipmentClass)AccessTools.Property(typeof(Player), "Equipment").GetValue(__instance);
                Plugin.EarProtectionFactor = HeadsetDeafFactor(equipment) * HelmDeafFactor(equipment);
            }

        }
    }

    public static class Deafening
    {

        //player
        public static float Volume = 0f;
        public static float Distortion = 0f;
        public static float VignetteDarkness = 0f;

        public static float VolumeLimit = -30f;
        public static float DistortionLimit = 70f;
        public static float VignetteDarknessLimit = 12f;


        //bot
        public static float BotVolume = 0f;
        public static float BotDistortion = 0f;
        public static float BotVignetteDarkness = 0f;

        //grenade
        public static float GrenadeVolume = 0f;
        public static float GrenadeDistortion = 0f;
        public static float GrenadeVignetteDarkness = 0f;

        public static float GrenadeVolumeLimit = -40f;
        public static float GrenadeDistortionLimit = 50f;
        public static float GrenadeVignetteDarknessLimit = 10f;

        public static float GrenadeVolumeDecreaseRate = 0.028f;
        public static float GrenadeDistortionIncreaseRate = 0.5f;
        public static float GrenadeVignetteDarknessIncreaseRate = 0.75f;

        public static float GrenadeVolumeResetRate = 0.03f;
        public static float GrenadeDistortionResetRate = 0.1f;
        public static float GrenadeVignetteDarknessResetRate = 0.45f;

        public static bool valuesAreReset = false;

        public static void DoDeafening()
        {
            float enviroMulti = PlayerProperties.enviroType == EnvironmentType.Indoor ? 1.2f : 1f;
            float deafFactor = Plugin.AmmoDeafFactor * Plugin.WeaponDeafFactor * Plugin.EarProtectionFactor;
            float botDeafFactor = Plugin.BotDeafFactor * Plugin.EarProtectionFactor;
            float grenadeDeafFactor = Plugin.GrenadeDeafFactor * Plugin.EarProtectionFactor;

            if (Plugin.IsFiring == true)
            {
                ChangeDeafValues(deafFactor, ref VignetteDarkness, Plugin.VigRate.Value, VignetteDarknessLimit, ref Volume, Plugin.DeafRate.Value, VolumeLimit, ref Distortion, Plugin.DistRate.Value, DistortionLimit, enviroMulti);
            }
            else if (!valuesAreReset)
            {
                ReseDeaftValues(deafFactor, ref VignetteDarkness, Plugin.VigReset.Value, VignetteDarknessLimit, ref Volume, Plugin.DeafReset.Value, VolumeLimit, ref Distortion, Plugin.DistReset.Value, DistortionLimit);
            }

            if (Plugin.IsBotFiring == true)
            {
                ChangeDeafValues(botDeafFactor, ref BotVignetteDarkness, Plugin.VigRate.Value, VignetteDarknessLimit, ref BotVolume, Plugin.DeafRate.Value, VolumeLimit, ref BotDistortion, Plugin.DistRate.Value, DistortionLimit, enviroMulti);
            }
            else if (!valuesAreReset)
            {
                ReseDeaftValues(botDeafFactor, ref BotVignetteDarkness, Plugin.VigReset.Value, VignetteDarknessLimit, ref BotVolume, Plugin.DeafReset.Value, VolumeLimit, ref BotDistortion, Plugin.DistReset.Value, DistortionLimit);
            }

            if (Plugin.GrenadeExploded == true)
            {
                ChangeDeafValues(grenadeDeafFactor, ref GrenadeVignetteDarkness, GrenadeVignetteDarknessIncreaseRate, GrenadeVignetteDarknessLimit, ref GrenadeVolume, GrenadeVolumeDecreaseRate, GrenadeVolumeLimit, ref GrenadeDistortion, GrenadeDistortionIncreaseRate, GrenadeDistortionLimit, enviroMulti);
            }
            else if (!valuesAreReset)
            {
                ReseDeaftValues(grenadeDeafFactor, ref GrenadeVignetteDarkness, GrenadeVignetteDarknessResetRate, GrenadeVignetteDarknessLimit, ref GrenadeVolume, GrenadeVolumeResetRate, GrenadeVolumeLimit, ref GrenadeDistortion, GrenadeDistortionResetRate, GrenadeDistortionLimit);
            }


            float totalVolume = Mathf.Clamp(Volume + BotVolume + GrenadeVolume, -40.0f, 0.0f);
            float totalDistortion = Mathf.Clamp(Distortion + BotDistortion + GrenadeDistortion, 0.0f, 70.0f);
            float totalVignette = Mathf.Clamp(VignetteDarkness + BotVignetteDarkness + GrenadeVignetteDarkness, 0.0f, 60.0f);

            //for some reason this prevents the values from being fully reset to 0
            if (totalVolume != 0.0f || totalDistortion != 0.0f || totalVignette != 0.0f)
            {
                Plugin.Vignette.darkness = totalVignette;
                Singleton<BetterAudio>.Instance.Master.SetFloat("GunsVolume", totalVolume + Plugin.GunsVolume);
                Singleton<BetterAudio>.Instance.Master.SetFloat("OcclusionVolume", totalVolume + Plugin.MainVolume);
                Singleton<BetterAudio>.Instance.Master.SetFloat("EnvironmentVolume", totalVolume + Plugin.MainVolume);
                Singleton<BetterAudio>.Instance.Master.SetFloat("AmbientVolume", totalVolume + Plugin.AmbientVolume);
                Singleton<BetterAudio>.Instance.Master.SetFloat("AmbientOccluded", totalVolume + Plugin.AmbientOccluded);


/*                logger.LogWarning("==========================");
                logger.LogWarning("Deaf Factor = " + deafFactor);
                logger.LogWarning("Volume = " + totalVolume);
                logger.LogWarning("Distorion = " + totalDistortion);
                logger.LogWarning("Vignette = " + Plugin.Vignette.darkness);
                logger.LogWarning("==========================");*/

                if (!Plugin.HasHeadSet)
                {
                    Singleton<BetterAudio>.Instance.Master.SetFloat("CompressorResonance", totalDistortion + Plugin.CompressorResonance);
                    Singleton<BetterAudio>.Instance.Master.SetFloat("Compressor", totalDistortion + Plugin.Compressor);
                    Singleton<BetterAudio>.Instance.Master.SetFloat("CompressorDistortion", totalDistortion + Plugin.CompressorDistortion);
                    Singleton<BetterAudio>.Instance.Master.SetFloat("CompressorLowpass", totalDistortion + Plugin.CompressorDistortion);
                }
                else
                {
                    Singleton<BetterAudio>.Instance.Master.SetFloat("CompressorMakeup", Plugin.RealTimeGain.Value * Plugin.GainReduc.Value);
                }
                valuesAreReset = false;
            }
            else
            {
                if (Plugin.HasHeadSet == true)
                {
                    Singleton<BetterAudio>.Instance.Master.SetFloat("CompressorMakeup", Plugin.RealTimeGain.Value);
                }
                Plugin.Vignette.enabled = false;
                valuesAreReset = true;
            }
        }

        private static void ChangeDeafValues(float deafFactor, ref float vigValue, float vigIncRate, float vigLimit, ref float volValue, float volDecRate, float volLimit, ref float distValue, float distIncRate, float distLimit, float enviroMulti)
        {

            Plugin.Vignette.enabled = true;
            vigValue = Mathf.Clamp(vigValue + (vigIncRate * deafFactor * enviroMulti), 0.0f, vigLimit * deafFactor * enviroMulti);
            volValue = Mathf.Clamp(volValue - (volDecRate * deafFactor * enviroMulti), volLimit, 0.0f);
            distValue = Mathf.Clamp(distValue + (distIncRate * deafFactor), 0.0f, distLimit);
        }

        private static void ReseDeaftValues(float deafFactor, ref float vigValue, float vigResetRate, float vigLimit, ref float volValue, float volResetRate, float volLimit, ref float distValue, float distResetRate, float distLimit)
        {

            vigValue = Mathf.Clamp(vigValue - vigResetRate, 0.0f, vigLimit * deafFactor);
            volValue = Mathf.Clamp(volValue + volResetRate, volLimit, 0.0f);
            distValue = Mathf.Clamp(distValue - distResetRate, 0.0f, distLimit);
        }
    }

    public class SetCompressorPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(BetterAudio).GetMethod("SetCompressor", BindingFlags.Instance | BindingFlags.Public);

        }
        [PatchPrefix]
        private static bool Prefix(GClass2202 template, BetterAudio __instance)
        {

            bool hasHeadsetTemplate = template != null;
            bool isNotHeadset = template?._id == null; //using both bools is redundant now.

            Plugin.MainVolume = hasHeadsetTemplate && !isNotHeadset ? template.DryVolume : 0f;
            Plugin.Compressor = hasHeadsetTemplate && !isNotHeadset ? template.CompressorVolume : -80f;
            Plugin.AmbientVolume = hasHeadsetTemplate && !isNotHeadset ? template.AmbientVolume : 0f;
            Plugin.AmbientOccluded = hasHeadsetTemplate && !isNotHeadset ? (template.AmbientVolume - 15f) : -5f;
            Plugin.GunsVolume = hasHeadsetTemplate && !isNotHeadset ? (template.DryVolume) : -5f;

            Plugin.CompressorDistortion = hasHeadsetTemplate && !isNotHeadset ? template.Distortion : 0.277f;
            Plugin.CompressorResonance = hasHeadsetTemplate && !isNotHeadset ? template.Resonance : 2.47f;
            Plugin.CompressorLowpass = hasHeadsetTemplate && !isNotHeadset ? template.LowpassFreq : 22000f;
            Plugin.CompressorGain = hasHeadsetTemplate && !isNotHeadset ? Plugin.RealTimeGain.Value : 10f;

            __instance.Master.SetFloat("Compressor", Plugin.Compressor);
            __instance.Master.SetFloat("OcclusionVolume", Plugin.MainVolume);
            __instance.Master.SetFloat("EnvironmentVolume", Plugin.MainVolume);
            __instance.Master.SetFloat("AmbientVolume", Plugin.AmbientVolume);
            __instance.Master.SetFloat("AmbientOccluded", Plugin.AmbientOccluded);
            __instance.Master.SetFloat("GunsVolume", Plugin.GunsVolume);

            __instance.Master.SetFloat("CompressorAttack", hasHeadsetTemplate && !isNotHeadset ? template.CompressorAttack : 35f);
            __instance.Master.SetFloat("CompressorMakeup", Plugin.CompressorGain);
            __instance.Master.SetFloat("CompressorRelease", hasHeadsetTemplate && !isNotHeadset ? template.CompressorRelease : 215f);
            __instance.Master.SetFloat("CompressorTreshold", hasHeadsetTemplate && !isNotHeadset ? template.CompressorTreshold : -20f);
            __instance.Master.SetFloat("CompressorDistortion", Plugin.CompressorDistortion);
            __instance.Master.SetFloat("CompressorResonance", Plugin.CompressorResonance);
            __instance.Master.SetFloat("CompressorCutoff", hasHeadsetTemplate && !isNotHeadset ? template.CutoffFreq : 245f);
            __instance.Master.SetFloat("CompressorLowpass", Plugin.CompressorLowpass);

            return false;
        }
    }

    public class RegisterShotPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player.FirearmController).GetMethod("RegisterShot", BindingFlags.Instance | BindingFlags.NonPublic);
        }


        private static float GetMuzzleLoudness(Mod[] mods)
        {
            float loudness = 0f;
            for (int i = 0; i < mods.Length; i++)
            {
                if (mods[i].Slots.Length > 0 && mods[i].Slots[0].ContainedItem != null && Helper.IsSilencer((Mod)mods[i].Slots[0].ContainedItem))
                {
                    return 0.75f;
                }
                else
                {
                    loudness += mods[i].Template.Loudness;
                }
            }
            return (loudness / 100) + 1f;
        }

        private static float CalcAmmoFactor(AmmoTemplate ammTemp)
        {
            return Math.Min(((ammTemp.ammoRec / 100f) * 2f) + 1f, 2f);
        }

        private static float CalcVelocityFactor(Weapon weap)
        {
           return ((weap.SpeedFactor - 1f) * -3f) + 1f;
        }

        [PatchPostfix]
        private static void PatchPostfix(Player.FirearmController __instance, Item weapon, GClass2620 shot)
        {
            Player player = (Player)AccessTools.Field(typeof(EFT.Player.FirearmController), "_player").GetValue(__instance);

            IWeapon iWeap = weapon as IWeapon;
            if (!iWeap.IsUnderbarrelWeapon)
            {
                Weapon weap = weapon as Weapon;

                if (player.IsYourPlayer == true)
                {
                    BulletClass bullet = shot.Ammo as BulletClass;
                    AmmoTemplate currentAmmoTemplate = bullet.Template as AmmoTemplate;

                    float ammoFactor = CalcAmmoFactor(currentAmmoTemplate);
                    float velocityFactor = CalcVelocityFactor(weap);
                    float ammoDeafFactor = ammoFactor * velocityFactor;

                    if (bullet.InitialSpeed * weap.SpeedFactor <= 335f)
                    {
                        ammoDeafFactor *= 0.6f;
                    }

                    Plugin.AmmoDeafFactor = ammoDeafFactor == 0f ? 1f : ammoDeafFactor;

/*                    Logger.LogWarning("==============");
                    Logger.LogWarning("Player Shot");
                    Logger.LogWarning("velocityFactor = " + velocityFactor);
                    Logger.LogWarning("ammoFactor = " + ammoFactor);
                    Logger.LogWarning("AmmoDeafFactor = " + Plugin.AmmoDeafFactor);
                    Logger.LogWarning("==============");*/
                }
                else
                {
                    float distanceFromPlayer = Vector3.Distance(__instance.gameObject.transform.position, Singleton<GameWorld>.Instance.AllPlayers[0].Transform.position);
                    if (distanceFromPlayer <= 30f)
                    {
                        Plugin.IsBotFiring = true;
                        BulletClass bullet = shot.Ammo as BulletClass;
                        AmmoTemplate currentAmmoTemplate = bullet.Template as AmmoTemplate;
                        float velocityFactor = CalcVelocityFactor(weap);
                        float ammoFactor = CalcAmmoFactor(currentAmmoTemplate);
                        float muzzleFactor = GetMuzzleLoudness(weap.Mods);
                        float calFactor = StatCalc.CalibreLoudnessFactor(weap.AmmoCaliber);
                        float ammoDeafFactor = ammoFactor * velocityFactor;
                        if (bullet.InitialSpeed * weap.SpeedFactor <= 335f)
                        {
                            ammoDeafFactor *= 0.6f;
                        }
                        float muzzleLoudness = muzzleFactor * calFactor * ammoDeafFactor;
                        Plugin.BotDeafFactor = muzzleLoudness * ((-distanceFromPlayer / 100f) + 1f);
/*                        Logger.LogWarning("==============");
                        Logger.LogWarning("Bot Shot");
                        Logger.LogWarning("velocityFactor = " + velocityFactor);
                        Logger.LogWarning("Muzzle Factor = " + muzzleFactor);
                        Logger.LogWarning("ammoFactor = " + ammoFactor);
                        Logger.LogWarning("Bot Calibre = " + calFactor);
                        Logger.LogWarning("distance = " + distanceFromPlayer);
                        Logger.LogWarning("BotDeafFactor = " + Plugin.BotDeafFactor);
                        Logger.LogWarning("==============");*/

                    }
                }
            }
        }
    }

    public class ExplosionPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(EFT.Grenade).GetMethod("Explosion", BindingFlags.Static | BindingFlags.Public);
        }

        [PatchPrefix]
        static void PreFix(IExplosiveItem grenadeItem, Vector3 grenadePosition)
        {
            float distanceFromPlayer = Vector3.Distance(grenadePosition, Singleton<GameWorld>.Instance.AllPlayers[0].Transform.position);
            if (distanceFromPlayer <= 50f)
            {
                Plugin.GrenadeExploded = true;
                Plugin.GrenadeDeafFactor = grenadeItem.Contusion.z * ((-distanceFromPlayer / 100f) + 1f);
/*                Logger.LogWarning("==============");
                Logger.LogWarning("Explosion");
                Logger.LogWarning("distance = " + distanceFromPlayer);
                Logger.LogWarning("GrenadeDeafFactor = " + Plugin.GrenadeDeafFactor);
                Logger.LogWarning("==============");*/
            }
        }
    }
}
