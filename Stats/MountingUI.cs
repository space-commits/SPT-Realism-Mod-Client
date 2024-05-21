using Aki.Reflection.Patching;
using EFT;
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace RealismMod
{
    public class MountingUI : MonoBehaviour
    {
        public GameObject ActiveUIScreen;
        private GameObject mountingUIGameObject;
        private Image mountingUIImage;
        private RectTransform mountingUIRect;
        private Vector2 iconSize = new Vector2(80, 80);

        public void DestroyGameObject()
        {
            if (mountingUIGameObject != null)
            {
                Destroy(mountingUIGameObject);
            }
        }

        public void CreateGameObject(UnityEngine.Transform parent)
        {
            mountingUIGameObject = new GameObject("MountingUI");
            mountingUIRect = mountingUIGameObject.AddComponent<RectTransform>();
            mountingUIRect.anchoredPosition = new Vector2(100, 100);
            mountingUIImage = mountingUIGameObject.AddComponent<Image>();
            mountingUIGameObject.transform.SetParent(parent);
            mountingUIImage.sprite = Plugin.LoadedSprites["mounting.png"];
            mountingUIImage.raycastTarget = false;
            mountingUIImage.color = Color.clear;
            mountingUIRect.sizeDelta = iconSize;
        }

        public void Update()
        {
            if (ActiveUIScreen != null && Plugin.EnableMountUI.Value)
            {
                if (StanceController.BracingDirection == EBracingDirection.Left)
                {
                    mountingUIImage.sprite = Plugin.LoadedSprites["mountingleft.png"];
                }
                else if (StanceController.BracingDirection == EBracingDirection.Right)
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
                    mountingUIRect.sizeDelta = iconSize * scaleAmount;

                }
                else if (StanceController.IsBracing && !PlayerState.IsSprinting)
                {
                    mountingUIRect.sizeDelta = iconSize;
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
            return typeof(EFT.UI.BattleUIScreen).GetMethod("Show", new Type[] { typeof(GamePlayerOwner) });
        }
        [PatchPostfix]
        private static void PatchPostFix(EFT.UI.BattleUIScreen __instance)
        {
            MountingUI mountingUI = Plugin.Hook.GetComponent<MountingUI>();

            if (mountingUI != null) 
            {
                if (mountingUI.ActiveUIScreen == __instance.gameObject)
                {
                    return;
                }
                mountingUI.ActiveUIScreen = __instance.gameObject;
                mountingUI.DestroyGameObject();
                mountingUI.CreateGameObject(__instance.transform);
            }
        }
    }
}
