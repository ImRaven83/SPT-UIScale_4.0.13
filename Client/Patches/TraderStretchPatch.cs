using System.Collections;
using System.Reflection;
using SPT.Reflection.Patching;
using HarmonyLib;
using EFT.UI;
using UnityEngine;

namespace UIScale.Client.Patches
{
    /// <summary>
    /// Patches TraderScreensGroup.Show() to stretch the trader screen layout
    /// at non-1080p resolutions.
    ///
    /// Layout under TraderDealScreen:
    ///   'Left Person'   — trader's items, fixed 642px, anchored left
    ///   'TradeControll'  — buy/sell center panel, fixed 506px, centered
    ///   'Right Person'   — your stash, fixed 642px, anchored right
    ///
    /// Fix: Expand both side panels toward the center, keeping
    /// TradeControll centered at its original 506px width.
    /// </summary>
    public class TraderStretchPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(TraderScreensGroup).GetMethod(
                "Show",
                BindingFlags.Public | BindingFlags.Instance,
                null,
                new[] { typeof(TraderScreensGroup).GetNestedType("GClass3888") },
                null);
        }

        [PatchPostfix]
        public static void PatchPostfix(MonoBehaviour __instance)
        {
            if (!Plugin.Enabled.Value)
                return;

            __instance.StartCoroutine(ApplyStretchDelayed(__instance.transform));
        }

        private static IEnumerator ApplyStretchDelayed(Transform root)
        {
            yield return PatchUtil.WaitFrames(3);

            // Stretch root TraderScreensGroup to fill canvas,
            // but leave top margin for the Tab Bar which extends 57px above the root.
            if (root is RectTransform rootRt)
                PatchUtil.StretchToFillParent(rootRt, new Vector2(0f, -60f));

            foreach (RectTransform rt in root.GetComponentsInChildren<RectTransform>(true))
            {
                string name = rt.gameObject.name;
                string parentName = rt.parent != null ? rt.parent.name : "";

                if (parentName == "TraderDealScreen")
                {
                    if (name == "Left Person")
                    {
                        // Keep original fixed width, anchored to the left.
                        // The grid has fixed cell sizes so stretching just adds empty space.
                        // Original: 642px wide (8 to 650).
                        rt.anchorMin = new Vector2(0f, 0f);
                        rt.anchorMax = new Vector2(0f, 1f);
                        rt.offsetMin = new Vector2(8f, 75f);
                        rt.offsetMax = new Vector2(650f, -205f);

                        if (Plugin.DebugLog.Value)
                            Plugin.Log.LogInfo("[UIScale] Left Person: 642px anchored left");
                    }
                    else if (name == "Right Person")
                    {
                        // Fixed 680px on the right, same as inventory stash.
                        rt.anchorMin = new Vector2(1f, 0f);
                        rt.anchorMax = new Vector2(1f, 1f);
                        rt.offsetMin = new Vector2(-692f, 75f);
                        rt.offsetMax = new Vector2(-8f, -205f);

                        if (Plugin.DebugLog.Value)
                            Plugin.Log.LogInfo("[UIScale] Right Person: 680px anchored right");
                    }
                }
            }
        }
    }
}
