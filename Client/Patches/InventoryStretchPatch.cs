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

            // TEMP DIAGNOSTIC (4.1.2 port): confirmed LeftSide's own rect is
            // being set and holds steady (doesn't get reset). But LeftSide
            // now carries a HorizontalLayoutGroup it didn't have in 4.0.13 —
            // that governs how its *children* are sized inside it, separate
            // from LeftSide's own anchors. Dump the layout group's settings
            // and each direct child's size/LayoutElement so we know whether
            // (and which) children need to be told to expand.
            if (Plugin.DebugLog.Value)
            {
                var leftSide = FindItemsPanelChild(root, "LeftSide");
                var stashPanel = FindItemsPanelChild(root, "Stash Panel");

                LogRect("LeftSide", leftSide);
                LogRect("Stash Panel", stashPanel);
                LogLayoutGroup(leftSide);
                LogChildren(leftSide);
            }
        }

        private static RectTransform? FindItemsPanelChild(Transform root, string name)
        {
            return root.GetComponentsInChildren<RectTransform>(true)
                .FirstOrDefault(rt => rt.gameObject.name == name && IsItemsPanelChild(rt));
        }

        private static void LogRect(string label, RectTransform? rt)
        {
            if (rt == null)
            {
                Plugin.Log.LogInfo($"[UIScale] {label}: not found");
                return;
            }

            Plugin.Log.LogInfo($"[UIScale] {label}: anchorMin={rt.anchorMin}, anchorMax={rt.anchorMax}, " +
                                $"offsetMin={rt.offsetMin}, offsetMax={rt.offsetMax}, rect={rt.rect}");
        }

        private static void LogLayoutGroup(RectTransform? rt)
        {
            if (rt == null)
                return;

            var hlg = rt.GetComponent<HorizontalLayoutGroup>();
            if (hlg == null)
            {
                Plugin.Log.LogInfo("[UIScale] LeftSide has no HorizontalLayoutGroup");
                return;
            }

            Plugin.Log.LogInfo($"[UIScale] LeftSide HorizontalLayoutGroup: " +
                                $"childControlWidth={hlg.childControlWidth}, childControlHeight={hlg.childControlHeight}, " +
                                $"childForceExpandWidth={hlg.childForceExpandWidth}, childForceExpandHeight={hlg.childForceExpandHeight}, " +
                                $"childScaleWidth={hlg.childScaleWidth}, spacing={hlg.spacing}, " +
                                $"padding=(l:{hlg.padding.left},r:{hlg.padding.right},t:{hlg.padding.top},b:{hlg.padding.bottom})");
        }

        private static void LogChildren(RectTransform? rt)
        {
            if (rt == null)
                return;

            for (var i = 0; i < rt.childCount; i++)
            {
                if (rt.GetChild(i) is not RectTransform child)
                    continue;

                var le = child.GetComponent<LayoutElement>();
                string leInfo = le == null
                    ? "no LayoutElement"
                    : $"LayoutElement(minW={le.minWidth}, prefW={le.preferredWidth}, flexW={le.flexibleWidth}, ignoreLayout={le.ignoreLayout})";

                Plugin.Log.LogInfo($"[UIScale] LeftSide child[{i}] '{child.gameObject.name}': " +
                                    $"active={child.gameObject.activeSelf}, rect={child.rect}, {leInfo}");
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
