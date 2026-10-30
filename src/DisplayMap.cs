using System;
using System.Collections.Generic;

namespace Crt
{
    internal static class DisplayMap
    {
        public static List<CrtOutput> LoadOutputs(IgclSession session)
        {
            List<CrtOutput> all = session.GetOutputs();
            List<CrtOutput> outputs = new List<CrtOutput>();

            foreach (CrtOutput output in all)
            {
                if (output.HasProperties &&
                    (output.Properties.DisplayConfigFlags & IgclConst.DisplayAttached) == 0)
                {
                    continue;
                }

                output.Index = outputs.Count;
                outputs.Add(output);
            }

            List<WindowsDisplay> windows = DisplayApi.GetDisplays();
            List<CcdDisplay> ccd = DisplayApi.GetCcdDisplays();

            Map(outputs, windows, ccd);

            foreach (CrtOutput output in outputs)
            {
                try
                {
                    output.CustomModes = session.GetCustomModes(output.Handle);
                }
                catch (IgclException ex)
                {
                    output.CustomModeError = ex.Message;
                    output.CustomModes = new List<CtlCustomSrcMode>();
                }
            }

            return outputs;
        }

        public static void Map(List<CrtOutput> outputs, List<WindowsDisplay> windows, List<CcdDisplay> ccd)
        {
            bool[] used = new bool[windows.Count];

            foreach (CrtOutput output in outputs)
            {
                int match = -1;

                // 1. IGCL encoder id matches a CCD target id.
                if (output.HasProperties)
                {
                    for (int i = 0; i < ccd.Count; i++)
                    {
                        if (ccd[i].TargetId != output.Properties.WindowsDisplayEncoderID)
                        {
                            continue;
                        }

                        int windowIndex = FindWindowsIndex(windows, ccd[i].GdiName);
                        if (windowIndex >= 0 && !used[windowIndex])
                        {
                            match = windowIndex;
                            ApplyCcdInfo(output, windows[windowIndex], ccd[i]);
                            break;
                        }
                    }
                }

                // 2. Internal/external flag matches a CCD path.
                if (match < 0)
                {
                    bool outputInternal = IsInternal(output);

                    for (int i = 0; i < ccd.Count; i++)
                    {
                        if (ccd[i].IsInternal != outputInternal)
                        {
                            continue;
                        }

                        int windowIndex = FindWindowsIndex(windows, ccd[i].GdiName);
                        if (windowIndex >= 0 && !used[windowIndex])
                        {
                            match = windowIndex;
                            ApplyCcdInfo(output, windows[windowIndex], ccd[i]);
                            break;
                        }
                    }
                }

                // 3. Current timing matches a Windows display.
                if (match < 0 && output.HasProperties)
                {
                    for (int i = 0; i < windows.Count; i++)
                    {
                        if (used[i])
                        {
                            continue;
                        }

                        if (windows[i].Width == (int)output.Properties.HActive &&
                            windows[i].Height == (int)output.Properties.VActive)
                        {
                            match = i;
                            break;
                        }
                    }
                }

                // 4. Last resort: first unused display.
                if (match < 0)
                {
                    for (int i = 0; i < windows.Count; i++)
                    {
                        if (!used[i])
                        {
                            match = i;
                            break;
                        }
                    }
                }

                if (match >= 0)
                {
                    output.Windows = windows[match];
                    used[match] = true;
                }
            }
        }

        private static int FindWindowsIndex(List<WindowsDisplay> windows, string gdiName)
        {
            if (string.IsNullOrEmpty(gdiName))
            {
                return -1;
            }

            for (int i = 0; i < windows.Count; i++)
            {
                if (string.Equals(windows[i].DeviceName, gdiName, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            return -1;
        }

        private static void ApplyCcdInfo(CrtOutput output, WindowsDisplay display, CcdDisplay ccd)
        {
            output.WindowsInternal = ccd.IsInternal;

            if (!string.IsNullOrEmpty(ccd.MonitorName))
            {
                display.MonitorName = ccd.MonitorName;
            }
        }

        public static bool IsInternal(CrtOutput output)
        {
            if (output.WindowsInternal)
            {
                return true;
            }

            if (output.HasEncoder &&
                (output.Encoder.EncoderConfigFlags & IgclConst.EncoderInternalDisplay) != 0)
            {
                return true;
            }

            if (output.HasProperties && output.Properties.Type == IgclConst.OutputMipi)
            {
                return true;
            }

            return false;
        }

        public static CrtOutput FindOutput(List<CrtOutput> outputs, string selector)
        {
            if (outputs == null || outputs.Count == 0)
            {
                return null;
            }

            if (string.IsNullOrEmpty(selector))
            {
                foreach (CrtOutput output in outputs)
                {
                    if (!IsInternal(output))
                    {
                        return output;
                    }
                }

                return outputs[0];
            }

            int index;
            if (int.TryParse(selector, out index))
            {
                foreach (CrtOutput output in outputs)
                {
                    if (output.Index == index)
                    {
                        return output;
                    }
                }

                return null;
            }

            foreach (CrtOutput output in outputs)
            {
                if (output.Windows == null)
                {
                    continue;
                }

                if (string.Equals(output.Windows.DeviceName, selector, StringComparison.OrdinalIgnoreCase))
                {
                    return output;
                }

                if (Contains(output.Windows.MonitorName, selector))
                {
                    return output;
                }

                if (Contains(output.Windows.MonitorId, selector))
                {
                    return output;
                }

                if (Contains(output.Windows.Label, selector))
                {
                    return output;
                }
            }

            return null;
        }

        private static bool Contains(string text, string value)
        {
            return !string.IsNullOrEmpty(text) &&
                   text.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static bool HasCustomMode(CrtOutput output, uint width, uint height)
        {
            foreach (CtlCustomSrcMode mode in output.CustomModes)
            {
                if (mode.SourceX == width && mode.SourceY == height)
                {
                    return true;
                }
            }

            return false;
        }

        public static CtlCustomSrcMode FindCustomMode(CrtOutput output, uint width, uint height)
        {
            foreach (CtlCustomSrcMode mode in output.CustomModes)
            {
                if (mode.SourceX == width && mode.SourceY == height)
                {
                    return mode;
                }
            }

            return default(CtlCustomSrcMode);
        }

        public static string DescribeOutput(CrtOutput output)
        {
            string kind = IsInternal(output) ? "internal" : "external";
            string type = output.HasProperties
                ? DisplayApi.OutputTypeName(output.Properties.Type)
                : "unknown";
            string monitor = output.Windows != null ? output.Windows.Label : "unmapped";
            string gdi = output.Windows != null ? output.Windows.DeviceName : "?";
            string timing = output.HasProperties
                ? string.Format("{0}x{1} @ {2:0.##}Hz",
                    output.Properties.HActive,
                    output.Properties.VActive,
                    output.Properties.RefreshRate)
                : "unknown timing";

            return string.Format("[{0}] {1,-8} {2,-8} {3,-16} {4,-12} {5}",
                output.Index, kind, type, monitor, gdi, timing);
        }
    }
}
