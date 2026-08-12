using System.Collections;
using System.Linq;
using System.Reflection;
using SPT.Reflection.Patching;
using HarmonyLib;
using EFT.UI.Matchmaker;
using UnityEngine;

namespace UIScale.Client.Patches
{
    /// <summary>
    /// TEMP DIAGNOSTIC: the pre-raid Insurance screen shows the equipment
    /// paperdoll clipped off the left edge, with an empty/blurred void under
    /// the insurance list. This screen (MatchmakerInsuranceScreen) is a
    /// different class from InventoryScreen, so it's unclear yet whether it
    /// reuses the same LeftSide/Items Panel hierarchy InventoryStretchPatch
    /// already touches, or has its own separate layout. Dump its
    /// RectTransform hierarchy to find out.
    /// </summary>
    public class InsuranceStretchPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(MatchmakerInsuranceScreen)
                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .First(m => m.Name == "Show" && m.GetParameters().Length == 4);
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

            Plugin.Log.LogInfo("[UIScale] MatchmakerInsuranceScreen hierarchy dump:");
            DumpNode(root, 0, 4);
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
