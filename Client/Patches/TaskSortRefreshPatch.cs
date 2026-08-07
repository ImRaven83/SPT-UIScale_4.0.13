using System.Collections;
using System.Reflection;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;

namespace UIScale.Client.Patches
{
    /// <summary>
    /// Keeps the task sort headers aligned with the task list, no more vodka for you!
    /// </summary>
    public class TaskSortRefreshPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(TasksPanel), "method_3");
        }

        [PatchPostfix]
        public static void PatchPostfix(MonoBehaviour __instance)
        {
            if (!Plugin.Enabled.Value || !Plugin.FixTaskSortHeader.Value)
                return;

            __instance.StartCoroutine(AlignAfterSortRefresh(__instance.transform));
        }

        private static IEnumerator AlignAfterSortRefresh(Transform root)
        {
            // Bcause initial fix broke when clicking lol
            yield return null;
            yield return null;
            TaskSortAlignmentPatch.TryAlign(root);
        }
    }
}
