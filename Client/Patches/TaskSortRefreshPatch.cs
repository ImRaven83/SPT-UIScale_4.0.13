using System.Collections;
using System.Reflection;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;

namespace UIScale.Client.Patches
{
    /// <summary>
    /// Re-aligns the task sort headers after the list re-renders (e.g. when
    /// the user changes the sort order), since TaskSortAlignmentPatch only
    /// runs once on Show().
    /// </summary>
    public class TaskSortRefreshPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(TasksPanel), "Sort");
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
            // Clicking a sort header rebuilds the list a couple frames later;
            // wait for that before re-aligning, or the fix from Show() drifts.
            yield return PatchUtil.WaitFrames(2);
            TaskSortAlignmentPatch.TryAlign(root);
        }
    }
}
