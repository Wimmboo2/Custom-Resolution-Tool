# crt — Intel Custom Resolution CLI

`crt` is a small Windows command-line tool that adds custom resolutions to an **external** monitor through Intel's official IGCL API (`ControlLib.dll`). It was built for stretched 4:3 gaming resolutions such as **1440x1080** on a 1920x1080 panel.

It does **not** use EDID overrides, does **not** restart the display driver, and does **not** touch Valorant, Vanguard, or any game files.

---

## Current setup on this machine

| Display | Role | Connection | Resolution | Notes |
|---|---|---|---|---|
| `CMN14D4` | Internal laptop panel | eDP | 1920x1080 @ 60Hz | Blocked by default |
| `P24FBA-RAGL` | External monitor | HDMI | 1920x1080 @ 100Hz | Target display |

`1440x1080` is already installed on the external monitor and is exposed at:

```text
1440x1080 @ 50Hz
1440x1080 @ 59Hz
1440x1080 @ 60Hz
1440x1080 @ 75Hz
1440x1080 @ 100Hz
```

The desktop is currently `1920x1080 @ 100Hz`. The custom mode is only an available mode; it is not the active desktop resolution.

---

## Requirements

- Windows 10 or Windows 11
- Intel graphics with IGCL support (Alder Lake / Raptor Lake or newer, recent driver)
- An external monitor connected over HDMI or DisplayPort
- .NET Framework 4.8 (included with Windows)

Intel does not support custom resolutions on internal laptop panels. `crt` refuses the internal panel by default.

---

## Build

No installs, no NuGet, no Node. The build uses the in-box .NET Framework compiler.

```powershell
cd "C:\Users\Rafandra Aydin\Documents\Wimmboo\Custom-Resolution-Tool"
powershell -ExecutionPolicy Bypass -File .\build.ps1
```

Output:

```text
bin\crt.exe
```

You only need to build again if the source changes. The existing `bin\crt.exe` is ready to use.

---

## Quick start — step by step

### 1. Open PowerShell

Press `Win`, type `PowerShell`, and open it.

### 2. Go to the project folder

```powershell
cd "C:\Users\Rafandra Aydin\Documents\Wimmboo\Custom-Resolution-Tool"
```

### 3. List your displays

```powershell
.\bin\crt.exe list
```

Expected output on this machine:

```text
[0] internal DisplayPort CMN14D4     \\.\DISPLAY1 1920x1080 @ 60Hz
[1] external HDMI     P24FBA-RAGL   \\.\DISPLAY2 1920x1080 @ 100Hz
```

The external monitor is **display 1**.

### 4. Check the tool and driver

```powershell
.\bin\crt.exe doctor
```

Look for:

```text
IGCL: initialized
Struct sizes: init=36 display=200 encoder=120 scaling=28 caps=12 custom=24
scaling: ... preferred=stretched
```

### 5. Add 1440x1080

```powershell
.\bin\crt.exe add 1440x1080 --display 1
```

This adds the mode to the external monitor's mode list. It does **not** change your desktop resolution.

Expected output:

```text
Added custom mode 1440x1080.
Desktop refresh is 100Hz; the custom mode inherits the active refresh.
Scaling: current=identity, preferred=stretched.
```

### 6. Verify the mode exists

```powershell
.\bin\crt.exe modes --display 1
```

To filter for just 1440x1080:

```powershell
.\bin\crt.exe modes --display 1 | Select-String "1440x1080"
```

Expected:

```text
1440x1080 @ 50Hz  [custom]
1440x1080 @ 59Hz  [custom]
1440x1080 @ 60Hz  [custom]
1440x1080 @ 75Hz  [custom]
1440x1080 @ 100Hz [custom]
```

### 7. Use it in Valorant

1. Launch Valorant.
2. Go to **Settings → Video**.
3. Set **Resolution** to **1440x1080**.
4. Set **Display Mode** to **Fullscreen**.
5. Make sure the refresh rate is **100Hz**.

Do not change any Valorant config files. The game reads the mode list from Windows.

### 8. Optional — test it on the desktop first

If you want to see the resolution applied to the desktop before using it in-game:

```powershell
.\bin\crt.exe set 1440x1080 --display 1 --hz 100 --stretch --timeout 15
```

The screen switches to 1440x1080 @ 100Hz with stretched scaling. Press `Enter` or `Y` to keep it. Press `N`, `Esc`, or wait 15 seconds to revert automatically.

For Valorant, you normally **do not** need `set`. `add` is enough.

---

## Command reference

All commands assume you are in the project folder and use `.\bin\crt.exe`.

### `list`

```powershell
.\bin\crt.exe list
```

Shows each Intel display output, its monitor name, GDI name, current resolution, refresh rate, and custom modes.

---

### `modes`

```powershell
.\bin\crt.exe modes [--display N]
```

Lists every mode Windows exposes for the selected display. Custom modes are marked `[custom]`.

Examples:

```powershell
.\bin\crt.exe modes
.\bin\crt.exe modes --display 1
.\bin\crt.exe modes --display P24FBA
```

---

### `add`

```powershell
.\bin\crt.exe add <WxH> [--display N] [--hz N] [--dry-run]
```

Adds a custom source mode. Does **not** change the current desktop resolution.

- `--hz N` — switches the native mode to that refresh first, so the custom mode inherits it. The desktop refresh changes; the resolution does not.
- `--dry-run` — prints what would happen without changing anything.

Examples:

```powershell
.\bin\crt.exe add 1440x1080 --display 1
.\bin\crt.exe add 1440x1080 --display 1 --hz 100
.\bin\crt.exe add 1440x1080 --display 1 --dry-run
```

---

### `remove`

```powershell
.\bin\crt.exe remove <WxH> [--display N] [--dry-run]
```

Removes a custom mode from the display.

Examples:

```powershell
.\bin\crt.exe remove 1440x1080 --display 1
.\bin\crt.exe remove 1440x1080 --display 1 --dry-run
```

---

### `scaling`

```powershell
.\bin\crt.exe scaling <stretched|aspect|centered|identity> [--display N]
```

Sets the GPU scaling method for the display.

- `stretched` — fills the panel, ignores aspect ratio (what you want for 4:3 stretch).
- `aspect` — keeps aspect ratio, may pillarbox.
- `centered` — no scaling, centered.
- `identity` — lets the display handle scaling.

Examples:

```powershell
.\bin\crt.exe scaling stretched --display 1
.\bin\crt.exe scaling aspect --display 1
```

On this machine the Intel Graphics Software preference is already `stretched`, so this command is normally not needed.

---

### `set`

```powershell
.\bin\crt.exe set <WxH> [--display N] [--hz N]
    [--stretch|--aspect|--center|--identity]
    [--timeout 15] [--keep] [--dry-run]
```

Adds the mode if missing, sets scaling, applies the mode to the desktop, and starts an auto-revert countdown.

- `--hz N` — target refresh rate. Defaults to the current refresh.
- `--stretch` / `--aspect` / `--center` / `--identity` — scaling method. Defaults to the preferred scaling.
- `--timeout N` — auto-revert delay in seconds. `0` means keep.
- `--keep` — skip the countdown and keep the mode.
- `--dry-run` — print the plan only.

Examples:

```powershell
.\bin\crt.exe set 1440x1080 --display 1 --hz 100 --stretch
.\bin\crt.exe set 1440x1080 --display 1 --hz 100 --stretch --timeout 5
.\bin\crt.exe set 1440x1080 --display 1 --hz 100 --keep
.\bin\crt.exe set 1440x1080 --display 1 --dry-run
```

`set` is intended for testing. For Valorant, use `add`.

---

### `reset`

```powershell
.\bin\crt.exe reset [--display N] [--all] [--dry-run]
```

Restores the mode and scaling saved by the last `set` or `add --hz`.

- `--all` — removes every custom mode on the selected display and restores the Windows registry mode.
- `--dry-run` — print what would be restored.

Examples:

```powershell
.\bin\crt.exe reset
.\bin\crt.exe reset --all --display 1
.\bin\crt.exe reset --all --display 1 --dry-run
```

---

### `doctor`

```powershell
.\bin\crt.exe doctor
```

Read-only diagnostics:

- Windows and architecture
- `ControlLib.dll` version
- IGCL API version
- Struct sizes
- Adapters and display outputs
- Monitor mapping (GDI name, monitor name, internal/external)
- Current timing and refresh
- Scaling capabilities and preferred scaling
- Custom modes

Use this first if something does not work.

---

### `help` / `version`

```powershell
.\bin\crt.exe help
.\bin\crt.exe version
```

---

## Selecting a display

`--display` accepts any of these:

| Form | Example |
|---|---|
| Index from `crt list` | `--display 1` |
| GDI device name | `--display "\\.\DISPLAY2"` |
| Monitor name substring | `--display P24FBA` |
| Monitor ID substring | `--display XMIB016` |

If `--display` is omitted, `crt` prefers the first external display.

---

## Global options

| Option | Meaning |
|---|---|
| `--display N` / `-d N` | Select the display |
| `--hz N` | Target refresh rate |
| `--timeout N` | Auto-revert delay in seconds (`0` = keep) |
| `--keep` | Skip the auto-revert countdown |
| `--dry-run` | Print the plan without changing anything |
| `--all` | Used by `reset` to clear all custom modes |
| `--force-internal` | Allow the internal laptop panel (unsupported by Intel) |
| `--stretch` / `--aspect` / `--center` / `--identity` | Scaling shortcuts |

---

## Valorant workflow

The recommended sequence:

```powershell
cd "C:\Users\Rafandra Aydin\Documents\Wimmboo\Custom-Resolution-Tool"
.\bin\crt.exe list
.\bin\crt.exe add 1440x1080 --display 1
.\bin\crt.exe modes --display 1 | Select-String "1440x1080"
```

Then in Valorant:

1. **Settings → Video**
2. **Resolution:** 1440x1080
3. **Display Mode:** Fullscreen
4. **Refresh Rate:** 100Hz

The desktop stays at 1920x1080 @ 100Hz. Only the game switches to 1440x1080.

---

## Safety and recovery

- `add` does not change the current desktop mode.
- `set` auto-reverts unless you confirm.
- `reset` restores the last saved mode.
- `reset --all` removes all custom modes on the display.

If the screen becomes unreadable:

1. Wait for the auto-revert.
2. Press `Win+Ctrl+Shift+B` to reset the graphics driver.
3. Run `crt reset` from another terminal or the other monitor.
4. Reboot; Windows falls back to a valid mode.
5. In Safe Mode, run `crt reset --all --display 1`.

---

## Troubleshooting

### `ControlLib.dll was not found`

The Intel driver does not expose IGCL on this system. Update the Intel graphics driver. `crt` cannot work without `ControlLib.dll`.

### `access denied (run elevated)`

Open PowerShell as Administrator and retry.

### `this is the internal laptop panel`

Intel does not support custom resolutions on internal displays. Use the external monitor. `--force-internal` exists but is not expected to work.

### `the custom mode did not appear in the Windows mode list`

Try a lower refresh:

```powershell
.\bin\crt.exe add 1440x1080 --display 1 --hz 60
```

Then check:

```powershell
.\bin\crt.exe modes --display 1 | Select-String "1440x1080"
```

### Image is pillarboxed or has black bars

1. Set the monitor OSD aspect ratio to **Full**.
2. Set Intel scaling to stretched:

```powershell
.\bin\crt.exe scaling stretched --display 1
```

3. Confirm the preferred scaling in `crt doctor`.

### `crt doctor` shows an old Windows version

`crt doctor` uses .NET Framework's `Environment.OSVersion`, which reports `6.2.9200.0` without an app manifest. This is cosmetic; the tool works correctly on Windows 11.

---

## Limitations

- Custom modes are **width/height only**. Refresh rate is inherited from the driver's exposed timings.
- Intel does not support custom resolutions on internal laptop panels.
- `crt` is display-only. It never reads or writes Valorant or Vanguard files.
- No EDID overrides, no driver restarts, no custom registry hacks.
- The custom mode persists in the Intel driver until removed.

---

## Files and state

```text
src/
  Igcl.cs          IGCL interop, custom modes, scaling
  DisplayApi.cs    Win32 display APIs, DEVMODE, CCD monitor mapping
  DisplayMap.cs    IGCL output <-> Windows monitor mapping
  StateStore.cs    state file
  Commands.cs      list/modes/add/remove/scaling/set/reset/doctor
  Program.cs       entry point and error handling
build.ps1          in-box csc.exe build
README.md          this file
bin/crt.exe        built executable
```

State file:

```text
%LOCALAPPDATA%\crt\state.ini
```

It is created by `set` and `add --hz`, and deleted by `reset`.

---

## Uninstall / remove everything

Remove the custom mode:

```powershell
.\bin\crt.exe remove 1440x1080 --display 1
```

Remove all custom modes on the external display:

```powershell
.\bin\crt.exe reset --all --display 1
```

Then delete the project folder if you no longer want the tool. No system software was installed.
