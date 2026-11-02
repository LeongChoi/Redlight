# Red Light Compact

Personal compact fork of [depoledna/RedLightForWindows](https://github.com/depoledna/RedLightForWindows), based on the v1.1.0 source. The original MIT license is preserved in LICENSE.txt. This controls its own Windows full-screen color filter; it does not adjust Windows Night light.

## Compact controls

- Click the tray icon to show a 490 × 132 popup with On, Warmth, Dim, Start with Windows, and Quit. The colour dropdown and previous filter modes have been removed.
- **Warmth** goes from approximately 6500 K (unchanged) to 3400 K. At maximum it leaves about 50% of green and 25% of blue, based on f.lux's published 3400 K balance. It is a simple approximation; monitor calibration can make f.lux look different on the same screen.
- **Dim** reduces all three colour channels evenly from 0% to 70%. Warmth and Dim are combined in one colour effect, so both controls work simultaneously. This darkens the image; it does not change the monitor's backlight or physical brightness setting.
- Click either slider bar to jump directly to that position. Scroll over the Dim row to adjust Dim. Scroll anywhere else over the open popup to adjust Warmth. A standard wheel notch changes either slider by two positions. Both sliders have no hover tooltip.
- On/off, warmth and dim settings are saved for the next launch. Your previous slider position is carried over on upgrade, with Dim initially at 0%; the warmth colour may look different at the same position.
- Start with Windows creates a shortcut to the running EXE in the **current user's** Startup folder (`shell:startup`). It also removes this app's old Run-key entry so Windows does not see two startup routes. No administrator access is needed. Keep the EXE in a permanent place; after moving it, turn the option off and back on to update the shortcut.
- No automatic sunset schedule or time-zone setting is used. Quit resets the screen to its normal colors.

## Build on Windows 11

Install the [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0), then double-click `Build-Windows.cmd` in this folder (or run `Build-Windows.ps1` from PowerShell). The output is `publish/RedLightCompact.exe`; place it in a permanent location before checking **Start with Windows**. Close the original RedLight app before running this fork.

When updating an earlier build, click **Quit** in the running tray app before rebuilding. Replace the EXE in the location you normally run. The new app will show **Start with Windows** unchecked until you enable the new Startup-folder shortcut; enabling it removes the older registry entry. Windows Settings → Apps → Startup can disable the shortcut separately, so check that Windows has it enabled after sign-in.

The build uses the official NuGet feed (`https://api.nuget.org/v3/index.json`) to restore the Windows runtime packages on the first run. An internet connection is required. If restore fails with NU1100, check access to that address; the error occurs before C# compilation. This is a Windows-only WPF app, so it must be built and tested on Windows.

Settings are saved in `HKCU\Software\RedLightCompact`. Startup uses `RedLightCompact.lnk` in the current user's Startup folder.
