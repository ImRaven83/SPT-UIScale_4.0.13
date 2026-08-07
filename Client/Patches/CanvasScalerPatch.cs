using System.Reflection;
using SPT.Reflection.Patching;
using HarmonyLib;
using EFT.UI;
using UnityEngine;
using UnityEngine.UI;

namespace UIScale.Client.Patches
{
    /// <summary>
    /// Patches UICanvasScalerController.ChangeCanvasScalerRestriction — the
    /// single chokepoint where EFT applies its scale factor to every
    /// registered CanvasScaler.
    ///
    /// Flow: UICanvasScalerController watches for resolution changes,
    /// computes ReferenceScaleFactor = Min(screenW/1920, screenH/1080),
    /// then calls ChangeCanvasScalerRestriction(scaler) for each registered
    /// scaler.
    ///
    /// This patch reads the game's auto-calculated ReferenceScaleFactor
    /// (which updates when you change resolution in-game) and multiplies it
    /// by the user's scale percentage. 100% = vanilla, 75% = smaller UI /
    /// more grid space.
    /// </summary>
    public class CanvasScalerPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(UICanvasScalerController).GetMethod(
                "ChangeCanvasScalerRestriction",
                BindingFlags.Public | BindingFlags.Static);
        }

        [PatchPrefix]
        public static bool PatchPrefix(CanvasScaler scaler)
        {
            if (!Plugin.Enabled.Value || scaler == null)
                return true;

            // Read the game's auto-calculated scale for the current resolution.
            // Updates on resolution change.
            float gameScale = UICanvasScalerController.ReferenceScaleFactor;

            // Apply user's percentage adjustment
            float userScale = Plugin.ScalePercent.Value / 100f;
            float finalScale = gameScale * userScale;

            if (Plugin.DebugLog.Value)
            {
                Plugin.Log.LogInfo($"[UIScale] Scaler: '{scaler.gameObject.name}', " +
                                   $"gameScale={gameScale:F3}, " +
                                   $"userPercent={Plugin.ScalePercent.Value}%, " +
                                   $"final={finalScale:F3}");
            }

            // Replicate ChangeCanvasScalerRestriction with our adjusted scale
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.referencePixelsPerUnit = 100f;
            scaler.scaleFactor = finalScale;

            return false; // skip original
        }
    }
}
