using System.Collections;
using System.Linq;
using System.Reflection;
using SPT.Reflection.Patching;
using HarmonyLib;
using EFT.UI;
using UnityEngine;
using UnityEngine.UI;

namespace UIScale.Client.Patches
{
    /// <summary>
    /// Patches InventoryScreen.Show() to fix the inventory layout
    /// at non-1080p resolutions.
    ///
    /// Problem: 'LeftSide' (gear+containers) uses proportional anchors
    /// (0.48-0.79) designed for 1920px. At higher resolutions it shifts
    /// right and becomes too wide. 'Stash Panel' is a fixed 680px from
    /// the right edge and never expands.
    ///
    /// Fix: Pin LeftSide to the left at its original 1200px width.
    /// Make Stash Panel fill from after LeftSide to the right edge.
    ///
    /// LeftSide's own children ('Left Panel', 'Containers Panel') are
    /// sized by a HorizontalLayoutGroup on LeftSide itself. That group
    /// only recomputes child sizes on Unity's next internal layout pass,
    /// which can land a frame or more after we resize LeftSide, leaving
    /// the children visually stuck at their pre-resize width. Forcing an
    /// immediate rebuild closes that gap.
    /// </summary>
    public class InventoryStretchPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(InventoryScreen)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .First(m => m.Name == "Show" && m.GetParameters().Length == 10);
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
            // Wait for method_4 coroutine to finish setting up panels
            yield return PatchUtil.WaitFrames(3);

            // Stretch root InventoryScreen to fill canvas
            if (root is RectTransform rootRt)
                PatchUtil.StretchToFillParent(rootRt);

            // Find Items Panel → LeftSide and Stash Panel by walking hierarchy
            foreach (RectTransform rt in root.GetComponentsInChildren<RectTransform>(true))
            {
                string name = rt.gameObject.name;

                if (name == "LeftSide" && IsItemsPanelChild(rt))
                {
                    // Expand gear panel to fill from left margin to the stash.
                    // Stash is 692px from right edge (680px + 12px margin).
                    // Leave a 10px gap between gear and stash.
                    // anchorMax.x=1 so it grows with resolution.
                    rt.anchorMin = new Vector2(0f, 0f);
                    rt.anchorMax = new Vector2(1f, 1f);
                    rt.offsetMin = new Vector2(12f, 130f);
                    rt.offsetMax = new Vector2(-702f, -48f);

                    // Force LeftSide's HorizontalLayoutGroup to recompute its
                    // children's widths now, instead of racing Unity's next
                    // layout pass.
                    LayoutRebuilder.ForceRebuildLayoutImmediate(rt);

                    if (Plugin.DebugLog.Value)
                        Plugin.Log.LogInfo($"[UIScale] LeftSide: 12px left margin, expands to stash");
                }
                else if (name == "Stash Panel" && IsItemsPanelChild(rt))
                {
                    // Keep stash at original 680px width, anchored to the right.
                    // This matches the original game layout.
                    rt.anchorMin = new Vector2(1f, 0f);
                    rt.anchorMax = new Vector2(1f, 1f);
                    rt.offsetMin = new Vector2(-692f, 132f);
                    rt.offsetMax = new Vector2(-12f, -75f);

                    if (Plugin.DebugLog.Value)
                        Plugin.Log.LogInfo($"[UIScale] Stash Panel: 680px anchored right");
                }
            }
        }

        /// <summary>
        /// Check if this RectTransform is a direct child of 'Items Panel'
        /// to avoid hitting identically-named elements elsewhere in the tree.
        /// </summary>
        private static bool IsItemsPanelChild(RectTransform rt)
        {
            var parent = rt.parent;
            return parent != null && parent.name == "Items Panel";
        }
    }
}
