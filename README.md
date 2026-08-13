# SPT-UIScale

BepInEx client plugin for SPT 4.1.2 that unlocks UI scaling for EFT's screens. Overrides EFT's hardcoded 1080p canvas scaling with a configurable percentage, and can stretch supported screens edge-to-edge for ultrawide monitors.

## Features

- Configurable UI scale as a percentage of vanilla (50–150%)
- Automatically adjusts when changing resolution in-game
- Optional **Anchor To Edge**: stretches supported screens (inventory, trader, hideout, flea market, etc.) edge-to-edge instead of leaving vanilla margins — built for ultrawide monitors
- Tasks screen: sort headers track the rendered quest-list columns
- Works with any resolution (1440p, 4K, ultrawide, etc.)

## Installation

1. Download the latest release ZIP
2. Extract into your SPT installation directory — the DLL goes to `BepInEx/plugins/`
3. Launch SPT

## Configuration

After first launch, edit `BepInEx/config/com.vonbraunz.uiscale.cfg`:

| Setting | Default | Description |
|---------|---------|-------------|
| **Enabled** | `true` | Toggle the mod on/off without uninstalling |
| **Scale Percent** | `100` | UI scale as a percentage of vanilla. `100` = no change, `75` = 75% size (more grid space), `50` = half size. Range: 50–150 |
| **Anchor To Edge** | `false` | Stretch supported screens edge-to-edge instead of leaving vanilla margins. Built for ultrawide monitors. Only resizes each screen's own background/root — does not reflow individual panels (gear grid, stash grid, etc.) within it. |
| **Align Sort Header** | `true` | Align Tasks screen sort headers with their rendered columns when UI scaling is active |
| **Log Canvas Names** | `false` | Debug logging to BepInEx console |

### Recommended values

| Resolution | Scale Percent | Effect |
|------------|--------------|--------|
| 1080p | 100 | No change (vanilla) |
| 1440p | 75–85 | More inventory/stash space |
| 4K | 50–75 | Significantly more grid space |
| Ultrawide (21:9, 32:9) | 100 + Anchor To Edge on | Removes letterboxing on supported screens |

## How It Works

EFT uses a central UI scale manager (`UICanvasScalerController`) that forces all canvases to a 1080p reference resolution via `ConstantPixelSize` scaling. It watches for resolution changes and calculates `Min(screenWidth/1920, screenHeight/1080)`, applying that to all registered `CanvasScaler` components.

This mod patches that pipeline:

1. **CanvasScalerPatch** — intercepts `UICanvasScalerController.ChangeCanvasScalerRestriction` and multiplies the game's auto-calculated scale factor by your configured percentage
2. **AnchorToEdgePatch** — hooks the shared `UIScreen.ShowGameObject(bool)`, called by every screen type, and stretches the root RectTransform of an explicit allow-list of full-screen backgrounds (inventory, trader, hideout, flea market, etc.) to fill the canvas edge-to-edge when Anchor To Edge is on. It doesn't reflow the panels inside those screens — earlier versions tried per-panel pixel offsets, but those broke on every SPT update and conflicted with other UI-fixing mods, so this only touches each screen's own background.

## Building

Requires the SPT 4.1.2 client installed at `C:\SPT\ModTest\` (or override `TarkovDir` in the `.csproj`).

```
dotnet build Client/UIScale.Client.csproj -c Release
```

Output: `Client/bin/Release/UIScale.Client.dll` and `Client/release/UIScale.zip`

## Compatibility

- SPT 4.1.2
- BepInEx 5.x
- No server-side component required
