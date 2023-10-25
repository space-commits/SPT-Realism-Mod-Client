using Aki.Reflection.Patching;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using EFT.InventoryLogic;
using HarmonyLib;
using System.Linq;
using UnityEngine.UI;
using EFT;
using BepInEx.Logging;
using System.IO;

namespace RealismMod
{
    public class MountingUI : MonoBehaviour
    {
        public static GameObject ActiveUIScreen;
        private static GameObject mountingUIGameObject;
        private static Image mountingUIImage;
        private static RectTransform mountingUIRect;

        public static void DestroyGameObjects()
        {
            if (mountingUIGameObject != null)
            {
                Destroy(mountingUIGameObject);
            }
        }

        public static void CreateGameObjects(UnityEngine.Transform parent)
        {
            mountingUIGameObject = new GameObject("MountingUI");
            mountingUIRect = mountingUIGameObject.AddComponent<RectTransform>();
            mountingUIRect.anchoredPosition = new Vector2(100, 100);
            mountingUIImage = mountingUIGameObject.AddComponent<Image>();
            mountingUIGameObject.transform.SetParent(parent);
            mountingUIImage.sprite = Plugin.LoadedSprites["mounting.png"];
            mountingUIImage.raycastTarget = false;
            mountingUIImage.color = Color.clear;
            mountingUIRect.sizeDelta = new Vector2(100, 100);
        }

        public void Update()
        {
            if (ActiveUIScreen != null && Plugin.EnableMountUI.Value)
            {
                if (StanceController.IsBracingLeftSide)
                {
                    mountingUIImage.sprite = Plugin.LoadedSprites["mountingleft.png"];
                }
                else if (StanceController.IsBracingRightSide)
                {
                    mountingUIImage.sprite = Plugin.LoadedSprites["mountingright.png"];
                }
                else
                {
                    mountingUIImage.sprite = Plugin.LoadedSprites["mounting.png"];
                }

                if (StanceController.IsMounting)
                {
                    mountingUIImage.color = Color.white;
                    float scaleAmount = Mathf.Lerp(1f, 1.15f, Mathf.PingPong(Time.time * 0.9f, 1f));
                    mountingUIRect.sizeDelta = new Vector2(90f, 90f) * scaleAmount;

                }
                else if (StanceController.IsBracing)
                {
                    mountingUIRect.sizeDelta = new Vector2(90f, 90f);
                    float alpha = Mathf.Lerp(0.2f, 1f, Mathf.PingPong(Time.time * 1f, 1f));
                    Color lerpedColor = new Color(1f, 1f, 1f, alpha);
                    mountingUIImage.color = lerpedColor;
                }
                else
                {
                    mountingUIImage.color = Color.clear;
                }
                mountingUIRect.localPosition = new Vector3(650f, -460f, 0f);
            }
        }
    }

    public class BattleUIScreenPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(EFT.UI.BattleUIScreen).GetMethod("Show", BindingFlags.Instance | BindingFlags.NonPublic);
        }
        [PatchPostfix]
        private static void PatchPostFix(EFT.UI.BattleUIScreen __instance)
        {
            if (MountingUI.ActiveUIScreen == __instance.gameObject)
            {
                return;
            }
            MountingUI.ActiveUIScreen = __instance.gameObject;
            MountingUI.DestroyGameObjects();
            MountingUI.CreateGameObjects(__instance.transform);
        }
    }
}
