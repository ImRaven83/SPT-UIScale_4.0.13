using System.Collections;
using System.Linq;
using System.Reflection;
using SPT.Reflection.Patching;
using HarmonyLib;
using EFT.UI;
using UnityEngine;

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

            // TEMP DIAGNOSTIC (4.1.2 port): the values above were reported as
            // not visually taking effect. Log component types on the target
            // panels (a LayoutGroup/ContentSizeFitter would explain a silent
            // override) and re-check the rects for a few frames to see if
            // something resets them after we set them.
            if (Plugin.DebugLog.Value)
            {
                var leftSide = FindItemsPanelChild(root, "LeftSide");
                var stashPanel = FindItemsPanelChild(root, "Stash Panel");
                var itemsPanel = leftSide != null ? leftSide.parent as RectTransform : null;

                LogComponents("LeftSide", leftSide);
                LogComponents("Stash Panel", stashPanel);
                LogComponents("Items Panel", itemsPanel);
                LogRect("LeftSide [frame +0]", leftSide);
                LogRect("Stash Panel [frame +0]", stashPanel);

                for (var frame = 0; frame < 10; frame++)
                {
                    yield return null;
                    LogRect($"LeftSide [frame +{frame + 1}]", leftSide);
                    LogRect($"Stash Panel [frame +{frame + 1}]", stashPanel);
                }
            }
        }

        private static RectTransform FindItemsPanelChild(Transform root, string name)
        {
            return root.GetComponentsInChildren<RectTransform>(true)
                .FirstOrDefault(rt => rt.gameObject.name == name && IsItemsPanelChild(rt));
        }

        private static void LogComponents(string label, RectTransform? rt)
        {
            if (rt == null)
            {
                Plugin.Log.LogInfo($"[UIScale] {label}: not found");
                return;
            }

            var components = rt.GetComponents<Component>().Select(c => c.GetType().Name);
            Plugin.Log.LogInfo($"[UIScale] {label} components: {string.Join(", ", components)}");
        }

        private static void LogRect(string label, RectTransform? rt)
        {
            if (rt == null)
                return;

            Plugin.Log.LogInfo($"[UIScale] {label}: anchorMin={rt.anchorMin}, anchorMax={rt.anchorMax}, " +
                                $"offsetMin={rt.offsetMin}, offsetMax={rt.offsetMax}, rect={rt.rect}");
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
