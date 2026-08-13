using System.Collections;
using System.Reflection;
using SPT.Reflection.Patching;
using HarmonyLib;
using EFT.UI;
using EFT.UI.Screens;
using EFT.UI.Settings;
using EFT.UI.Ragfair;
using EFT.UI.Matchmaker;
using EFT.HandBook;
using EFT.Hideout;
using UnityEngine;

namespace UIScale.Client.Patches
{
    /// <summary>
    /// Stretches the root RectTransform of allow-listed screens edge-to-edge
    /// when 'Anchor To Edge' is enabled. Built for ultrawide monitors, where
    /// vanilla EFT leaves fixed-width margins/letterboxing on screens instead
    /// of filling the display.
    ///
    /// This only stretches each screen's own root container -- it doesn't
    /// reflow the panels inside it (gear grid, stash grid, trader panels,
    /// etc.), which keep their vanilla proportions and anchoring. That's a
    /// deliberate trade-off: reflowing individual panels requires hardcoded,
    /// version-fragile pixel offsets that broke on every SPT update and
    /// conflicted with other UI-fixing mods (see git history for the
    /// InventoryStretchPatch/TraderStretchPatch this replaces). Stretching
    /// just the background/root is simpler and far more durable.
    ///
    /// Hooks the shared UIScreen.ShowGameObject(bool) so one patch covers
    /// every screen type, but only acts on types in the allow-list below --
    /// UIScreen is also the base class for modal dialogs and popups that
    /// should stay their normal size, not stretch to fill the screen.
    /// </summary>
    public class AnchorToEdgePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(UIScreen), "ShowGameObject");
        }

        [PatchPostfix]
        public static void PatchPostfix(MonoBehaviour __instance, bool __0)
        {
            if (!Plugin.Enabled.Value || !Plugin.AnchorToEdge.Value)
                return;

            if (!__0 || !IsAllowListed(__instance))
                return;

            __instance.StartCoroutine(StretchDelayed(__instance.transform));
        }

        /// <summary>
        /// Screens confirmed to be full-bleed backgrounds, safe to stretch.
        /// UIScreen also backs modal dialogs/popups/login forms that are
        /// meant to stay centered and fixed-size -- add to this list only
        /// after confirming a screen is a full-screen background, not one
        /// of those.
        /// </summary>
        private static bool IsAllowListed(MonoBehaviour instance)
        {
            return instance is InventoryScreen
                or TraderScreensGroup
                or TraderDialogScreen
                or TradingScreen
                or RagfairScreen
                or HandbookScreen
                or ScavengerInventoryScreen
                or TransferItemsScreen
                or TransferItemsInRaidScreen
                or SettingsScreen
                or HideoutScreenOverlay
                or MatchmakerInsuranceScreen;
        }

        private static IEnumerator StretchDelayed(Transform root)
        {
            yield return PatchUtil.WaitFrames(3);

            if (root is RectTransform rootRt)
                PatchUtil.StretchToFillParent(rootRt);

            if (Plugin.DebugLog.Value)
                Plugin.Log.LogInfo($"[UIScale] AnchorToEdge: stretched '{root.name}' to fill parent");
        }
    }
}
