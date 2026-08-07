using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using UIScale.Client.Patches;

namespace UIScale.Client
{
    [BepInPlugin("com.vonbraunz.uiscale", "UIScale", "1.1.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static ConfigEntry<bool> Enabled = null!;
        public static ConfigEntry<int> ScalePercent = null!;
        public static ConfigEntry<bool> FixTaskSortHeader = null!;
        public static ConfigEntry<bool> DebugLog = null!;
        public static ManualLogSource Log = null!;

        private void Awake()
        {
            Log = Logger;

            Enabled = Config.Bind(
                "General", "Enabled", true,
                "Enable or disable the UI scale override");

            ScalePercent = Config.Bind(
                "General", "Scale Percent", 100,
                new ConfigDescription(
                    "UI scale as a percentage of the game's default for your resolution. " +
                    "100 = vanilla (no change). " +
                    "75 = UI is 75% of default size (more grid space). " +
                    "Automatically adjusts when you change resolution in-game.",
                    new AcceptableValueRange<int>(50, 150)));

            FixTaskSortHeader = Config.Bind(
                "Task Screen", "Align Sort Header", true,
                "Align the Tasks sort header to the rendered task-list columns when UI scaling is enabled.");

            DebugLog = Config.Bind(
                "Debug", "Log Canvas Names", false,
                "Log canvas scaler info to BepInEx console.");

            new CanvasScalerPatch().Enable();
            new InventoryStretchPatch().Enable();
            new TraderStretchPatch().Enable();
            new TaskSortAlignmentPatch().Enable();
            new TaskSortRefreshPatch().Enable();

            Logger.LogInfo("[UIScale] Client plugin loaded");
        }
    }
}
