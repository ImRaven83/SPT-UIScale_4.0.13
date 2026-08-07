using System.Reflection;
using SPT.Reflection.Patching;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace UIScale.Client.Patches
{
    /// <summary>
    /// Patches GClass3825.smethod_2 — the single chokepoint where EFT applies
    /// its scale factor to every registered CanvasScaler.
    ///
    /// Original flow:
    ///   smethod_0() polls resolution each frame
    ///   → computes Float_0 = Min(screenW/1920, screenH/1080)
    ///   → smethod_1() iterates all registered scalers
    ///   → smethod_2(scaler) calls scaler.SetCanvasRestriction(Float_0)
    ///
    /// This patch reads the game's auto-calculated Float_0 (which updates
    /// when you change resolution in-game) and multiplies it by the user's
    /// scale percentage. 100% = vanilla, 75% = smaller UI / more grid space.
    /// </summary>
    public class CanvasScalerPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GClass3825).GetMethod(
                "smethod_2",
                BindingFlags.Public | BindingFlags.Static);
        }

        [PatchPrefix]
        public static bool PatchPrefix(CanvasScaler scaler)
        {
            if (!Plugin.Enabled.Value || scaler == null)
                return true;

            // Read the game's auto-calculated scale for the current resolution.
            // Float_0 = Min(screenW/1920, screenH/1080), updates on resolution change.
            float gameScale = GClass3825.Float_0;

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

            // Replicate SetCanvasRestriction with our adjusted scale
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.referencePixelsPerUnit = 100f;
            scaler.scaleFactor = finalScale;

            return false; // skip original
        }
    }
}
