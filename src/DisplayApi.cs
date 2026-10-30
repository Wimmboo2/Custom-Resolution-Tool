using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Crt
{
    internal sealed class DisplayMode
    {
        public int Width;
        public int Height;
        public int Refresh;
        public int BitsPerPel;

        public override string ToString()
        {
            return string.Format("{0}x{1} @ {2}Hz", Width, Height, Refresh);
        }
    }

    internal sealed class WindowsDisplay
    {
        public string DeviceName;
        public string AdapterName;
        public string MonitorName;
        public string MonitorId;
        public bool IsPrimary;
        public int Width;
        public int Height;
        public int Refresh;
        public int BitsPerPel;
        public List<DisplayMode> Modes = new List<DisplayMode>();

        public string Label
        {
            get
            {
                if (!string.IsNullOrEmpty(MonitorName) &&
                    !string.Equals(MonitorName, "Generic PnP Monitor", StringComparison.OrdinalIgnoreCase))
                {
                    return MonitorName;
                }

                if (!string.IsNullOrEmpty(MonitorId))
                {
                    string[] parts = MonitorId.Split('\\');
                    if (parts.Length >= 2 && !string.IsNullOrEmpty(parts[1]))
                    {
                        return parts[1];
                    }

                    return MonitorId;
                }

                return DeviceName;
            }
        }
    }

    internal sealed class CcdDisplay
    {
        public string GdiName;
        public string MonitorName;
        public string MonitorPath;
        public uint OutputTechnology;
        public uint TargetId;
        public uint AdapterLow;
        public int AdapterHigh;
        public bool IsInternal;
    }

    internal static class DisplayApi
    {
        public const int EnumCurrentSettings = -1;
        public const int EnumRegistrySettings = -2;

        public const uint CdsUpdateRegistry = 0x00000001;
        public const uint CdsTest = 0x00000002;

        public const int DispChangeSuccessful = 0;
        public const int DispChangeRestart = 1;
        public const int DispChangeFailed = -1;
        public const int DispChangeBadMode = -2;
        public const int DispChangeNotUpdated = -3;
        public const int DispChangeBadFlags = -4;
        public const int DispChangeBadParam = -5;
        public const int DispChangeBadDualView = -6;

        public const int DmBitsPerPel = 0x00040000;
        public const int DmPelsWidth = 0x00080000;
        public const int DmPelsHeight = 0x00100000;
        public const int DmDisplayFrequency = 0x00400000;

        public const int DisplayDeviceAttachedToDesktop = 0x00000001;
        public const int DisplayDevicePrimaryDevice = 0x00000004;

        private const uint QdcOnlyActivePaths = 0x00000002;
        private const uint GetSourceName = 1;
        private const uint GetTargetName = 2;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct DevMode
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string dmDeviceName;
            public short dmSpecVersion;
            public short dmDriverVersion;
            public short dmSize;
            public short dmDriverExtra;
            public int dmFields;
            public int dmPositionX;
            public int dmPositionY;
            public int dmDisplayOrientation;
            public int dmDisplayFixedOutput;
            public short dmColor;
            public short dmDuplex;
            public short dmYResolution;
            public short dmTTOption;
            public short dmCollate;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string dmFormName;
            public short dmLogPixels;
            public int dmBitsPerPel;
            public int dmPelsWidth;
            public int dmPelsHeight;
            public int dmDisplayFlags;
            public int dmDisplayFrequency;
            public int dmICMMethod;
            public int dmICMIntent;
            public int dmMediaType;
            public int dmDitherType;
            public int dmReserved1;
            public int dmReserved2;
            public int dmPanningWidth;
            public int dmPanningHeight;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct DisplayDevice
        {
            public int cb;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string DeviceName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceString;
            public int StateFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceID;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceKey;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Luid
        {
            public uint LowPart;
            public int HighPart;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Rational
        {
            public uint Numerator;
            public uint Denominator;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PointL
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Rect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Region2D
        {
            public uint Cx;
            public uint Cy;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PathSourceInfo
        {
            public Luid AdapterId;
            public uint Id;
            public uint ModeInfoIdx;
            public uint StatusFlags;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PathTargetInfo
        {
            public Luid AdapterId;
            public uint Id;
            public uint ModeInfoIdx;
            public uint OutputTechnology;
            public uint Rotation;
            public uint Scaling;
            public Rational RefreshRate;
            public uint ScanLineOrdering;
            public int TargetAvailable;
            public uint StatusFlags;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PathInfo
        {
            public PathSourceInfo SourceInfo;
            public PathTargetInfo TargetInfo;
            public uint Flags;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct VideoSignalInfo
        {
            public ulong PixelRate;
            public Rational HSyncFreq;
            public Rational VSyncFreq;
            public Region2D ActiveSize;
            public Region2D TotalSize;
            public uint VideoStandard;
            public uint ScanLineOrdering;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct TargetMode
        {
            public VideoSignalInfo TargetVideoSignalInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SourceMode
        {
            public uint Width;
            public uint Height;
            public uint PixelFormat;
            public PointL Position;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DesktopImageInfo
        {
            public PointL PathSourceSize;
            public Rect DesktopImageRegion;
            public Rect DesktopImageClip;
        }

        [StructLayout(LayoutKind.Explicit, Size = 48)]
        private struct ModeInfoUnion
        {
            [FieldOffset(0)] public TargetMode TargetMode;
            [FieldOffset(0)] public SourceMode SourceMode;
            [FieldOffset(0)] public DesktopImageInfo DesktopImageInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ModeInfo
        {
            public uint InfoType;
            public uint Id;
            public Luid AdapterId;
            public ModeInfoUnion ModeInfoValue;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DeviceInfoHeader
        {
            public uint Type;
            public uint Size;
            public Luid AdapterId;
            public uint Id;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct SourceDeviceName
        {
            public DeviceInfoHeader Header;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string ViewGdiDeviceName;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct TargetDeviceName
        {
            public DeviceInfoHeader Header;
            public uint Flags;
            public uint OutputTechnology;
            public ushort EdidManufactureId;
            public ushort EdidProductCodeId;
            public uint ConnectorInstance;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            public string MonitorFriendlyDeviceName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string MonitorDevicePath;
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern bool EnumDisplayDevices(string lpDevice, uint iDevNum, ref DisplayDevice lpDisplayDevice, uint dwFlags);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern bool EnumDisplaySettings(string lpszDeviceName, int iModeNum, ref DevMode lpDevMode);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int ChangeDisplaySettingsEx(string lpszDeviceName, ref DevMode lpDevMode, IntPtr hwnd, uint dwflags, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern int GetDisplayConfigBufferSizes(uint flags, out uint numPathArrayElements, out uint numModeInfoArrayElements);

        [DllImport("user32.dll")]
        private static extern int QueryDisplayConfig(uint flags, ref uint numPathArrayElements, [Out] PathInfo[] pathArray, ref uint numModeInfoArrayElements, [Out] ModeInfo[] modeInfoArray, IntPtr currentTopologyId);

        [DllImport("user32.dll")]
        private static extern int DisplayConfigGetDeviceInfo(ref SourceDeviceName deviceInfo);

        [DllImport("user32.dll")]
        private static extern int DisplayConfigGetDeviceInfo(ref TargetDeviceName deviceInfo);

        public static List<WindowsDisplay> GetDisplays()
        {
            List<WindowsDisplay> displays = new List<WindowsDisplay>();
            DisplayDevice adapter = new DisplayDevice();
            adapter.cb = Marshal.SizeOf(typeof(DisplayDevice));

            for (uint i = 0; EnumDisplayDevices(null, i, ref adapter, 0); i++)
            {
                if ((adapter.StateFlags & DisplayDeviceAttachedToDesktop) == 0)
                {
                    continue;
                }

                WindowsDisplay display = new WindowsDisplay();
                display.DeviceName = adapter.DeviceName;
                display.AdapterName = adapter.DeviceString;
                display.IsPrimary = (adapter.StateFlags & DisplayDevicePrimaryDevice) != 0;

                DisplayDevice monitor = new DisplayDevice();
                monitor.cb = Marshal.SizeOf(typeof(DisplayDevice));
                if (EnumDisplayDevices(adapter.DeviceName, 0, ref monitor, 0))
                {
                    display.MonitorName = monitor.DeviceString;
                    display.MonitorId = monitor.DeviceID;
                }

                DisplayMode current = GetCurrentMode(adapter.DeviceName);
                if (current != null)
                {
                    display.Width = current.Width;
                    display.Height = current.Height;
                    display.Refresh = current.Refresh;
                    display.BitsPerPel = current.BitsPerPel;
                }

                display.Modes = GetModes(adapter.DeviceName);
                displays.Add(display);
            }

            return displays;
        }

        public static DisplayMode GetCurrentMode(string deviceName)
        {
            DevMode mode = new DevMode();
            mode.dmSize = (short)Marshal.SizeOf(typeof(DevMode));

            if (!EnumDisplaySettings(deviceName, EnumCurrentSettings, ref mode))
            {
                return null;
            }

            DisplayMode result = new DisplayMode();
            result.Width = mode.dmPelsWidth;
            result.Height = mode.dmPelsHeight;
            result.Refresh = mode.dmDisplayFrequency;
            result.BitsPerPel = mode.dmBitsPerPel;
            return result;
        }

        public static List<DisplayMode> GetModes(string deviceName)
        {
            List<DisplayMode> modes = GetModesInternal(deviceName, true);
            if (modes.Count == 0)
            {
                modes = GetModesInternal(deviceName, false);
            }

            modes.Sort(delegate(DisplayMode a, DisplayMode b)
            {
                int result = a.Width.CompareTo(b.Width);
                if (result != 0) return result;
                result = a.Height.CompareTo(b.Height);
                if (result != 0) return result;
                return a.Refresh.CompareTo(b.Refresh);
            });

            return modes;
        }

        private static List<DisplayMode> GetModesInternal(string deviceName, bool require32)
        {
            List<DisplayMode> modes = new List<DisplayMode>();
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            DevMode mode = new DevMode();
            mode.dmSize = (short)Marshal.SizeOf(typeof(DevMode));

            for (int i = 0; EnumDisplaySettings(deviceName, i, ref mode); i++)
            {
                if (require32 && mode.dmBitsPerPel != 32)
                {
                    continue;
                }

                string key = string.Format("{0}x{1}@{2}", mode.dmPelsWidth, mode.dmPelsHeight, mode.dmDisplayFrequency);
                if (!seen.Add(key))
                {
                    continue;
                }

                DisplayMode item = new DisplayMode();
                item.Width = mode.dmPelsWidth;
                item.Height = mode.dmPelsHeight;
                item.Refresh = mode.dmDisplayFrequency;
                item.BitsPerPel = mode.dmBitsPerPel;
                modes.Add(item);
            }

            return modes;
        }

        public static DisplayMode GetRegistryMode(string deviceName)
        {
            DevMode mode = new DevMode();
            mode.dmSize = (short)Marshal.SizeOf(typeof(DevMode));

            if (!EnumDisplaySettings(deviceName, EnumRegistrySettings, ref mode))
            {
                return null;
            }

            DisplayMode result = new DisplayMode();
            result.Width = mode.dmPelsWidth;
            result.Height = mode.dmPelsHeight;
            result.Refresh = mode.dmDisplayFrequency;
            result.BitsPerPel = mode.dmBitsPerPel;
            return result;
        }

        public static int ApplyMode(string deviceName, int width, int height, int hz, int bits)
        {
            DevMode mode = new DevMode();
            mode.dmSize = (short)Marshal.SizeOf(typeof(DevMode));
            mode.dmPelsWidth = width;
            mode.dmPelsHeight = height;
            mode.dmDisplayFrequency = hz;
            mode.dmBitsPerPel = bits > 0 ? bits : 32;
            mode.dmFields = DmPelsWidth | DmPelsHeight | DmBitsPerPel;

            if (hz > 0)
            {
                mode.dmFields |= DmDisplayFrequency;
            }

            int test = ChangeDisplaySettingsEx(deviceName, ref mode, IntPtr.Zero, CdsTest, IntPtr.Zero);
            if (test != DispChangeSuccessful)
            {
                return test;
            }

            return ChangeDisplaySettingsEx(deviceName, ref mode, IntPtr.Zero, CdsUpdateRegistry, IntPtr.Zero);
        }

        public static int RestoreMode(string deviceName, int width, int height, int hz, int bits)
        {
            return ApplyMode(deviceName, width, height, hz, bits);
        }

        public static DisplayMode FindMode(WindowsDisplay display, int width, int height, int hz)
        {
            DisplayMode best = null;

            foreach (DisplayMode mode in display.Modes)
            {
                if (mode.Width != width || mode.Height != height)
                {
                    continue;
                }

                if (hz > 0 && mode.Refresh == hz)
                {
                    return mode;
                }

                if (best == null || Math.Abs(mode.Refresh - hz) < Math.Abs(best.Refresh - hz))
                {
                    best = mode;
                }
            }

            return best;
        }

        public static DisplayMode FindBestModeAtHz(WindowsDisplay display, int hz)
        {
            DisplayMode best = null;
            long bestArea = -1;

            foreach (DisplayMode mode in display.Modes)
            {
                if (mode.Refresh != hz)
                {
                    continue;
                }

                long area = (long)mode.Width * (long)mode.Height;
                if (area > bestArea)
                {
                    bestArea = area;
                    best = mode;
                }
            }

            return best;
        }

        public static List<CcdDisplay> GetCcdDisplays()
        {
            List<CcdDisplay> displays = new List<CcdDisplay>();

            uint pathCount;
            uint modeCount;
            int result = GetDisplayConfigBufferSizes(QdcOnlyActivePaths, out pathCount, out modeCount);
            if (result != 0 || pathCount == 0)
            {
                return displays;
            }

            PathInfo[] paths = new PathInfo[pathCount];
            ModeInfo[] modes = new ModeInfo[modeCount];

            result = QueryDisplayConfig(QdcOnlyActivePaths, ref pathCount, paths, ref modeCount, modes, IntPtr.Zero);
            if (result != 0)
            {
                return displays;
            }

            for (int i = 0; i < pathCount; i++)
            {
                SourceDeviceName source = new SourceDeviceName();
                source.Header.Type = GetSourceName;
                source.Header.Size = (uint)Marshal.SizeOf(typeof(SourceDeviceName));
                source.Header.AdapterId = paths[i].SourceInfo.AdapterId;
                source.Header.Id = paths[i].SourceInfo.Id;

                if (DisplayConfigGetDeviceInfo(ref source) != 0)
                {
                    continue;
                }

                TargetDeviceName target = new TargetDeviceName();
                target.Header.Type = GetTargetName;
                target.Header.Size = (uint)Marshal.SizeOf(typeof(TargetDeviceName));
                target.Header.AdapterId = paths[i].TargetInfo.AdapterId;
                target.Header.Id = paths[i].TargetInfo.Id;

                if (DisplayConfigGetDeviceInfo(ref target) != 0)
                {
                    continue;
                }

                CcdDisplay display = new CcdDisplay();
                display.GdiName = source.ViewGdiDeviceName;
                display.MonitorName = target.MonitorFriendlyDeviceName;
                display.MonitorPath = target.MonitorDevicePath;
                display.OutputTechnology = target.OutputTechnology;
                display.TargetId = paths[i].TargetInfo.Id;
                display.AdapterLow = paths[i].TargetInfo.AdapterId.LowPart;
                display.AdapterHigh = paths[i].TargetInfo.AdapterId.HighPart;
                display.IsInternal = IsInternalTechnology(target.OutputTechnology);
                displays.Add(display);
            }

            return displays;
        }

        public static bool IsInternalTechnology(uint technology)
        {
            return technology == 0x80000000u
                || technology == 6u
                || technology == 11u
                || technology == 13u;
        }

        public static string TechnologyName(uint technology)
        {
            switch (technology)
            {
                case 0: return "VGA";
                case 1: return "S-Video";
                case 2: return "Composite";
                case 3: return "Component";
                case 4: return "DVI";
                case 5: return "HDMI";
                case 6: return "LVDS";
                case 8: return "D-JPN";
                case 9: return "SDI";
                case 10: return "DisplayPort";
                case 11: return "eDP";
                case 12: return "UDI";
                case 13: return "UDI embedded";
                case 14: return "SDTV dongle";
                case 0x80000000u: return "Internal";
                default: return "Unknown";
            }
        }

        public static string OutputTypeName(int type)
        {
            switch (type)
            {
                case 0: return "invalid";
                case 1: return "DisplayPort";
                case 2: return "HDMI";
                case 3: return "DVI";
                case 4: return "MIPI";
                case 5: return "CRT";
                default: return "unknown";
            }
        }

        public static string ScalingName(int type)
        {
            switch (type)
            {
                case IgclConst.ScalingIdentity: return "identity";
                case IgclConst.ScalingCentered: return "centered";
                case IgclConst.ScalingStretched: return "stretched";
                case IgclConst.ScalingAspectCentered: return "aspect-ratio centered";
                case IgclConst.ScalingCustom: return "custom";
                default: return "unknown";
            }
        }

        public static string DispChangeName(int code)
        {
            switch (code)
            {
                case DispChangeSuccessful: return "success";
                case DispChangeRestart: return "restart required";
                case DispChangeFailed: return "failed";
                case DispChangeBadMode: return "mode not supported";
                case DispChangeNotUpdated: return "not updated";
                case DispChangeBadFlags: return "bad flags";
                case DispChangeBadParam: return "bad parameters";
                case DispChangeBadDualView: return "bad dual-view";
                default: return "unknown";
            }
        }
    }
}
