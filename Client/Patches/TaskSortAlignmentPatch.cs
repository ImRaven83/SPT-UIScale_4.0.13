using System.Collections;
using System.Linq;
using System.Reflection;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;

namespace UIScale.Client.Patches
{
    /// <summary>
    /// "We have alignment-after-clicking-sorting at home" she said
    /// </summary>
    public class TaskSortAlignmentPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(TasksPanel).GetMethod("Show", BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPostfix]
        public static void PatchPostfix(MonoBehaviour __instance)
        {
            if (!Plugin.Enabled.Value || !Plugin.FixTaskSortHeader.Value)
                return;

            __instance.StartCoroutine(AlignAfterTaskListRenders(__instance.transform));
        }

        private static IEnumerator AlignAfterTaskListRenders(Transform root)
        {
            for (var frame = 0; frame < 300; frame++)
            {
                yield return null;

                if (!TryAlign(root))
                    continue;

                if (Plugin.DebugLog.Value)
                    Plugin.Log.LogInfo("[UIScale] Aligned Tasks sort header to task-list columns with last few neurons");

                yield break;
            }
        }

        internal static bool TryAlign(Transform root)
        {
            var sortPanel = root.GetComponentInChildren<QuestsSortPanel>(true);
            var taskRow = root.GetComponentsInChildren<NotesTask>(true)
                .FirstOrDefault(row => row.gameObject.activeInHierarchy);
            if (sortPanel == null || taskRow == null)
                return false;

            var headers = sortPanel.GetComponentsInChildren<FilterButton>(true)
                .Select(button => button.transform as RectTransform)
                .OfType<RectTransform>()
                .Where(rect => rect.gameObject.activeInHierarchy)
                .OrderBy(GetCenterX)
                .ToList();

            var columns = new[]
            {
                GetRectTransform(taskRow, "_traderAvatar"),
                GetRectTransform(taskRow, "_typeIcon"),
                GetRectTransform(taskRow, "_taskLabel"),
                GetRectTransform(taskRow, "_locationLabel"),
                GetRectTransform(taskRow, "_statusLabel"),
                GetRectTransform(taskRow, "_progressView")
            };

            if (headers.Count != columns.Length)
                return false;

            for (var i = 0; i < headers.Count; i++)
            {
                var column = columns[i];
                if (column == null)
                    return false;

                var headerCenter = GetCenterX(headers[i]);
                var columnCenter = GetCenterX(column);
                headers[i].position += new Vector3(columnCenter - headerCenter, 0f, 0f);
            }

            return true;
        }

        private static RectTransform? GetRectTransform(NotesTask taskRow, string fieldName)
        {
            if (AccessTools.Field(typeof(NotesTask), fieldName)?.GetValue(taskRow) is Component component)
                return component.transform as RectTransform;

            return null;
        }

        private static float GetCenterX(RectTransform rect)
        {
            return rect.TransformPoint(rect.rect.center).x;
        }
    }
}
