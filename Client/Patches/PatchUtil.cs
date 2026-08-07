using System.Collections;
using UnityEngine;

namespace UIScale.Client.Patches
{
    /// <summary>
    /// Small helpers shared by the layout-stretching patches.
    /// </summary>
    internal static class PatchUtil
    {
        /// <summary>
        /// Waits the given number of frames. Screens rebuild their panels
        /// over a few frames after Show(), so callers use this to let that
        /// settle before applying layout overrides.
        /// </summary>
        public static IEnumerator WaitFrames(int count)
        {
            for (var i = 0; i < count; i++)
                yield return null;
        }

        /// <summary>
        /// Stretches a RectTransform to fill its parent, with an optional
        /// offset from the far (top-right) edge.
        /// </summary>
        public static void StretchToFillParent(RectTransform rt, Vector2 offsetMax = default)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = offsetMax;
        }
    }
}
