using System.Collections;
using System.Reflection;
using SPT.Reflection.Patching;
using HarmonyLib;
using EFT.Hideout;
using UnityEngine;

namespace UIScale.Client.Patches
{
    /// <summary>
    /// TEMP DIAGNOSTIC: the Hideout module screen isn't stretched by this mod
    /// yet. Dump HideoutScreenOverlay's RectTransform hierarchy (name/anchors/
    /// rect, a few levels deep) so we can identify the module-list panel and
    /// bottom hotbar that need reanchoring, the same way LeftSide/Stash Panel
    /// were identified for the inventory screen.
    /// </summary>
    public class HideoutStretchPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(HideoutScreenOverlay), "Show");
        }

        [PatchPostfix]
        public static void PatchPostfix(MonoBehaviour __instance)
        {
            if (!Plugin.Enabled.Value || !Plugin.DebugLog.Value)
                return;

            __instance.StartCoroutine(DumpHierarchyDelayed(__instance.transform));
        }

        private static IEnumerator DumpHierarchyDelayed(Transform root)
        {
            yield return PatchUtil.WaitFrames(3);

            Plugin.Log.LogInfo("[UIScale] HideoutScreenOverlay hierarchy dump:");
            DumpNode(root, 0, 3);
        }

        private static void DumpNode(Transform t, int depth, int maxDepth)
        {
            if (t is RectTransform rt)
            {
                var indent = new string(' ', depth * 2);
                Plugin.Log.LogInfo($"[UIScale] {indent}{rt.gameObject.name}: active={rt.gameObject.activeSelf}, " +
                                    $"anchorMin={rt.anchorMin}, anchorMax={rt.anchorMax}, " +
                                    $"offsetMin={rt.offsetMin}, offsetMax={rt.offsetMax}, rect={rt.rect}");
            }

            if (depth >= maxDepth)
                return;

            for (var i = 0; i < t.childCount; i++)
                DumpNode(t.GetChild(i), depth + 1, maxDepth);
        }
    }
}
