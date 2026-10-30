using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Crt
{
    internal sealed class Options
    {
        public string Display;
        public int Hz = -1;
        public int Timeout = 15;
        public bool Keep;
        public bool DryRun;
        public bool All;
        public bool ForceInternal;
        public bool Verbose;
        public string Scaling;
        public List<string> Positional = new List<string>();
    }

    internal struct Resolution
    {
        public int Width;
        public int Height;
    }

    internal sealed class HelpException : Exception
    {
    }

    internal static class Commands
    {
        public const string Version = "0.1.0";

        public static int Run(string[] args)
        {
            if (args.Length == 0)
            {
                PrintHelp();
                return 2;
            }

            string command = args[0].ToLowerInvariant();

            if (command == "help" || command == "--help" || command == "-h")
            {
                PrintHelp();
                return 0;
            }

            if (command == "version" || command == "--version")
            {
                Console.WriteLine("crt " + Version);
                return 0;
            }

            Options options;
            try
            {
                options = ParseOptions(args, 1);
            }
            catch (HelpException)
            {
                PrintHelp();
                return 0;
            }
            catch (ArgumentException ex)
            {
                Console.Error.WriteLine("error: " + ex.Message);
                return 2;
            }

            switch (command)
            {
                case "list":
                    return ListCommand(options);
                case "modes":
                    return ModesCommand(options);
                case "add":
                    return AddCommand(options);
                case "remove":
                    return RemoveCommand(options);
                case "scaling":
                    return ScalingCommand(options);
                case "set":
                    return SetCommand(options);
                case "reset":
                    return ResetCommand(options);
                case "doctor":
                    return DoctorCommand(options);
                default:
                    Console.Error.WriteLine("error: unknown command '" + command + "'");
                    PrintHelp();
                    return 2;
            }
        }

        private static Options ParseOptions(string[] args, int start)
        {
            Options options = new Options();

            for (int i = start; i < args.Length; i++)
            {
                string arg = args[i];

                if (arg == "--display" || arg == "-d")
                {
                    options.Display = NextValue(args, ref i, arg);
                }
                else if (arg == "--hz")
                {
                    options.Hz = ParseInt(NextValue(args, ref i, arg), arg);
                }
                else if (arg == "--timeout")
                {
                    options.Timeout = ParseInt(NextValue(args, ref i, arg), arg);
                }
                else if (arg == "--keep")
                {
                    options.Keep = true;
                }
                else if (arg == "--dry-run")
                {
                    options.DryRun = true;
                }
                else if (arg == "--all")
                {
                    options.All = true;
                }
                else if (arg == "--force-internal")
                {
                    options.ForceInternal = true;
                }
                else if (arg == "--verbose" || arg == "-v")
                {
                    options.Verbose = true;
                }
                else if (arg == "--stretch")
                {
                    options.Scaling = "stretched";
                }
                else if (arg == "--aspect")
                {
                    options.Scaling = "aspect";
                }
                else if (arg == "--center")
                {
                    options.Scaling = "centered";
                }
                else if (arg == "--identity")
                {
                    options.Scaling = "identity";
                }
                else if (arg == "--help" || arg == "-h")
                {
                    throw new HelpException();
                }
                else if (arg.StartsWith("-"))
                {
                    throw new ArgumentException("unknown option '" + arg + "'");
                }
                else
                {
                    options.Positional.Add(arg);
                }
            }

            return options;
        }

        private static string NextValue(string[] args, ref int index, string option)
        {
            if (index + 1 >= args.Length)
            {
                throw new ArgumentException(option + " requires a value");
            }

            index++;
            return args[index];
        }

        private static int ParseInt(string text, string option)
        {
            int value;
            if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
            {
                throw new ArgumentException(option + " expects a number, got '" + text + "'");
            }

            return value;
        }

        private static Resolution ParseResolution(Options options)
        {
            if (options.Positional.Count == 0)
            {
                throw new ArgumentException("missing resolution (for example 1440x1080)");
            }

            Resolution resolution;
            if (!TryParseResolution(options.Positional[0], out resolution))
            {
                throw new ArgumentException("invalid resolution '" + options.Positional[0] + "' (use WxH)");
            }

            if (resolution.Width < 320 || resolution.Width > 16384 ||
                resolution.Height < 200 || resolution.Height > 16384)
            {
                throw new ArgumentException("resolution out of range: " + options.Positional[0]);
            }

            return resolution;
        }

        private static bool TryParseResolution(string text, out Resolution resolution)
        {
            resolution = new Resolution();

            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            string[] parts = text.ToLowerInvariant().Split(new char[] { 'x', '*' });
            if (parts.Length != 2)
            {
                return false;
            }

            int width;
            int height;
            if (!int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out width) ||
                !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out height))
            {
                return false;
            }

            resolution.Width = width;
            resolution.Height = height;
            return true;
        }

        private static List<CrtOutput> LoadOutputs(IgclSession session)
        {
            List<CrtOutput> outputs = DisplayMap.LoadOutputs(session);
            if (outputs.Count == 0)
            {
                throw new InvalidOperationException("no Intel display outputs found");
            }

            return outputs;
        }

        private static CrtOutput ResolveOutput(List<CrtOutput> outputs, Options options)
        {
            CrtOutput output = DisplayMap.FindOutput(outputs, options.Display);
            if (output == null)
            {
                Console.Error.WriteLine("error: display '" + (options.Display == null ? string.Empty : options.Display) + "' not found.");
                Console.Error.WriteLine("available outputs:");
                PrintOutputs(outputs);
                throw new InvalidOperationException("display not found");
            }

            return output;
        }

        private static bool EnsureExternal(CrtOutput output, Options options)
        {
            if (DisplayMap.IsInternal(output) && !options.ForceInternal)
            {
                Console.Error.WriteLine("error: this is the internal laptop panel.");
                Console.Error.WriteLine("Intel does not support custom resolutions on internal displays.");
                Console.Error.WriteLine("hint: select the external monitor, or pass --force-internal to try anyway.");
                return false;
            }

            return true;
        }

        private static void PrintOutputs(List<CrtOutput> outputs)
        {
            foreach (CrtOutput output in outputs)
            {
                Console.WriteLine("  " + DisplayMap.DescribeOutput(output));
            }
        }

        private static int ListCommand(Options options)
        {
            using (IgclSession session = new IgclSession())
            {
                List<CrtOutput> outputs = LoadOutputs(session);
                Console.WriteLine("Displays:");

                foreach (CrtOutput output in outputs)
                {
                    Console.WriteLine("  " + DisplayMap.DescribeOutput(output));

                    if (output.Windows != null)
                    {
                        Console.WriteLine("      monitor: " + output.Windows.Label);
                        if (!string.IsNullOrEmpty(output.Windows.MonitorId))
                        {
                            Console.WriteLine("      id:      " + output.Windows.MonitorId);
                        }

                        Console.WriteLine("      current: " + output.Windows.Width + "x" + output.Windows.Height +
                                          " @ " + output.Windows.Refresh + "Hz");
                    }
                    else
                    {
                        Console.WriteLine("      monitor: unmapped");
                    }

                    if (output.HasEncoder)
                    {
                        Console.WriteLine("      encoder: " + (DisplayMap.IsInternal(output) ? "internal" : "external") +
                                          " (flags 0x" + output.Encoder.EncoderConfigFlags.ToString("X") + ")");
                    }

                    if (output.CustomModes.Count > 0)
                    {
                        Console.WriteLine("      custom:  " + JoinModes(output.CustomModes));
                    }
                    else
                    {
                        Console.WriteLine("      custom:  none");
                    }

                    if (!string.IsNullOrEmpty(output.CustomModeError))
                    {
                        Console.WriteLine("      custom error: " + output.CustomModeError);
                    }
                }

                return 0;
            }
        }

        private static int ModesCommand(Options options)
        {
            using (IgclSession session = new IgclSession())
            {
                List<CrtOutput> outputs = LoadOutputs(session);
                CrtOutput output = ResolveOutput(outputs, options);

                Console.WriteLine("Modes for " + DisplayMap.DescribeOutput(output));

                if (output.Windows == null)
                {
                    Console.Error.WriteLine("error: could not map this output to a Windows display");
                    return 1;
                }

                List<DisplayMode> modes = DisplayApi.GetModes(output.Windows.DeviceName);

                foreach (DisplayMode mode in modes)
                {
                    bool custom = false;
                    foreach (CtlCustomSrcMode item in output.CustomModes)
                    {
                        if (item.SourceX == (uint)mode.Width && item.SourceY == (uint)mode.Height)
                        {
                            custom = true;
                            break;
                        }
                    }

                    Console.WriteLine("  " + mode.Width + "x" + mode.Height + " @ " + mode.Refresh + "Hz" +
                                      (custom ? "  [custom]" : string.Empty));
                }

                foreach (CtlCustomSrcMode item in output.CustomModes)
                {
                    bool exposed = false;
                    foreach (DisplayMode mode in modes)
                    {
                        if (mode.Width == (int)item.SourceX && mode.Height == (int)item.SourceY)
                        {
                            exposed = true;
                            break;
                        }
                    }

                    if (!exposed)
                    {
                        Console.WriteLine("  " + item.SourceX + "x" + item.SourceY +
                                          "  [custom, not currently exposed by Windows]");
                    }
                }

                return 0;
            }
        }

        private static int AddCommand(Options options)
        {
            Resolution resolution = ParseResolution(options);

            using (IgclSession session = new IgclSession())
            {
                List<CrtOutput> outputs = LoadOutputs(session);
                CrtOutput output = ResolveOutput(outputs, options);

                if (!EnsureExternal(output, options))
                {
                    return 1;
                }

                if (output.Windows == null)
                {
                    Console.Error.WriteLine("error: could not map this output to a Windows display");
                    return 1;
                }

                DisplayMode original = DisplayApi.GetCurrentMode(output.Windows.DeviceName);
                bool changedRefresh = false;

                if (options.Hz > 0 && original != null && original.Refresh != options.Hz)
                {
                    DisplayMode native = DisplayApi.FindMode(output.Windows, original.Width, original.Height, options.Hz);
                    if (native == null)
                    {
                        native = DisplayApi.FindBestModeAtHz(output.Windows, options.Hz);
                    }

                    if (native == null)
                    {
                        Console.Error.WriteLine("error: no mode at " + options.Hz + "Hz is available on this display");
                        return 1;
                    }

                    if (options.DryRun)
                    {
                        Console.WriteLine("[dry-run] would set native mode " + native +
                                          " before adding " + resolution.Width + "x" + resolution.Height);
                    }
                    else
                    {
                        int result = DisplayApi.ApplyMode(output.Windows.DeviceName, native.Width, native.Height, native.Refresh, 32);
                        if (result != DisplayApi.DispChangeSuccessful)
                        {
                            Console.Error.WriteLine("error: could not set " + native + ": " + DisplayApi.DispChangeName(result));
                            return 1;
                        }

                        changedRefresh = true;
                    }
                }

                bool exists = DisplayMap.HasCustomMode(output, (uint)resolution.Width, (uint)resolution.Height);

                if (exists)
                {
                    Console.WriteLine("Custom mode " + resolution.Width + "x" + resolution.Height + " already exists.");
                }
                else if (options.DryRun)
                {
                    Console.WriteLine("[dry-run] would add custom mode " + resolution.Width + "x" + resolution.Height);
                }
                else
                {
                    try
                    {
                        session.AddCustomMode(output.Handle, (uint)resolution.Width, (uint)resolution.Height);
                        Console.WriteLine("Added custom mode " + resolution.Width + "x" + resolution.Height + ".");
                    }
                    catch (IgclException ex)
                    {
                        if (ex.Result == IgclResult.CustomModeStandardExists ||
                            ex.Result == IgclResult.CustomModeMatchingExists)
                        {
                            Console.WriteLine("Mode " + resolution.Width + "x" + resolution.Height +
                                              " is already available (" + IgclResult.Describe(ex.Result) + ").");
                        }
                        else
                        {
                            if (changedRefresh && original != null)
                            {
                                DisplayApi.RestoreMode(output.Windows.DeviceName, original.Width, original.Height, original.Refresh, original.BitsPerPel);
                            }

                            throw;
                        }
                    }
                }

                if (changedRefresh && original != null)
                {
                    IniFile ini = StateStore.Load();
                    ini.Set(StateStore.Section, "gdi", output.Windows.DeviceName);
                    ini.Set(StateStore.Section, "monitor", output.Windows.Label);
                    ini.SetInt(StateStore.Section, "width", original.Width);
                    ini.SetInt(StateStore.Section, "height", original.Height);
                    ini.SetInt(StateStore.Section, "hz", original.Refresh);
                    ini.SetInt(StateStore.Section, "bits", original.BitsPerPel);
                    ini.SetInt(StateStore.Section, "scaling_changed", 0);
                    ini.Set(StateStore.Section, "added", resolution.Width + "x" + resolution.Height);
                    ini.SetInt(StateStore.Section, "added_by_set", 0);
                    ini.Set(StateStore.Section, "phase", "added");
                    StateStore.Save(ini);
                }

                Console.WriteLine();
                Console.WriteLine("Next: launch Valorant and pick " + resolution.Width + "x" + resolution.Height + ".");

                if (output.Windows.Refresh > 0)
                {
                    Console.WriteLine("Desktop refresh is " + output.Windows.Refresh +
                                      "Hz; the custom mode inherits the active refresh.");
                }

                try
                {
                    CtlScalingSettings scaling = session.GetScaling(output.Handle);
                    Console.WriteLine("Scaling: current=" + DisplayApi.ScalingName(scaling.ScalingType) +
                                      ", preferred=" + DisplayApi.ScalingName(scaling.PreferredScalingType) + ".");

                    if (scaling.ScalingType != IgclConst.ScalingStretched &&
                        scaling.PreferredScalingType != IgclConst.ScalingStretched)
                    {
                        Console.WriteLine("hint: GPU scaling preference is not stretched. Run `crt scaling stretched --display " +
                                          output.Index + "` if the image does not fill the panel.");
                    }
                }
                catch (IgclException)
                {
                }

                return 0;
            }
        }

        private static int RemoveCommand(Options options)
        {
            Resolution resolution = ParseResolution(options);

            using (IgclSession session = new IgclSession())
            {
                List<CrtOutput> outputs = LoadOutputs(session);
                CrtOutput output = ResolveOutput(outputs, options);

                if (!EnsureExternal(output, options))
                {
                    return 1;
                }

                bool exists = DisplayMap.HasCustomMode(output, (uint)resolution.Width, (uint)resolution.Height);

                if (!exists)
                {
                    Console.WriteLine("Custom mode " + resolution.Width + "x" + resolution.Height +
                                      " is not present on " + DisplayMap.DescribeOutput(output) + ".");
                    return 0;
                }

                if (options.DryRun)
                {
                    Console.WriteLine("[dry-run] would remove custom mode " + resolution.Width + "x" + resolution.Height);
                    return 0;
                }

                List<CtlCustomSrcMode> remove = new List<CtlCustomSrcMode>();
                CtlCustomSrcMode mode = new CtlCustomSrcMode();
                mode.SourceX = (uint)resolution.Width;
                mode.SourceY = (uint)resolution.Height;
                remove.Add(mode);

                session.RemoveCustomModes(output.Handle, remove);
                Console.WriteLine("Removed custom mode " + resolution.Width + "x" + resolution.Height + ".");
                return 0;
            }
        }

        private static int ScalingCommand(Options options)
        {
            string name = options.Scaling;

            if (string.IsNullOrEmpty(name) && options.Positional.Count > 0)
            {
                name = options.Positional[0];
            }

            if (string.IsNullOrEmpty(name))
            {
                Console.Error.WriteLine("error: missing scaling type (stretched, aspect, centered, identity)");
                return 2;
            }

            int type = ParseScaling(name);

            using (IgclSession session = new IgclSession())
            {
                List<CrtOutput> outputs = LoadOutputs(session);
                CrtOutput output = ResolveOutput(outputs, options);

                if (!EnsureExternal(output, options))
                {
                    return 1;
                }

                int caps = session.GetSupportedScaling(output.Handle);
                if ((caps & type) == 0)
                {
                    Console.Error.WriteLine("error: " + name + " scaling is not supported on this output.");
                    Console.Error.WriteLine("supported: " + DescribeScalingCaps(caps));
                    return 1;
                }

                CtlScalingSettings current = session.GetScaling(output.Handle);

                if (options.DryRun)
                {
                    Console.WriteLine("[dry-run] would set scaling to " + name +
                                      " (currently " + DisplayApi.ScalingName(current.ScalingType) + ")");
                    return 0;
                }

                if (current.ScalingType == type)
                {
                    Console.WriteLine("Scaling is already " + name + ".");
                    return 0;
                }

                session.SetScaling(output.Handle, type, true);
                CtlScalingSettings applied = session.GetScaling(output.Handle);
                Console.WriteLine("Scaling set to " + DisplayApi.ScalingName(applied.ScalingType) + ".");
                return 0;
            }
        }

        private static int SetCommand(Options options)
        {
            Resolution resolution = ParseResolution(options);

            using (IgclSession session = new IgclSession())
            {
                List<CrtOutput> outputs = LoadOutputs(session);
                CrtOutput output = ResolveOutput(outputs, options);

                if (!EnsureExternal(output, options))
                {
                    return 1;
                }

                if (output.Windows == null)
                {
                    Console.Error.WriteLine("error: could not map this output to a Windows display");
                    return 1;
                }

                DisplayMode original = DisplayApi.GetCurrentMode(output.Windows.DeviceName);
                if (original == null)
                {
                    Console.Error.WriteLine("error: could not read the current display mode");
                    return 1;
                }

                CtlScalingSettings originalScaling = session.GetScaling(output.Handle);
                int preferredScaling = originalScaling.PreferredScalingType != 0
                    ? originalScaling.PreferredScalingType
                    : originalScaling.ScalingType;
                int targetHz = options.Hz > 0 ? options.Hz : original.Refresh;
                int scalingType = string.IsNullOrEmpty(options.Scaling)
                    ? preferredScaling
                    : ParseScaling(options.Scaling);
                bool addMode = !DisplayMap.HasCustomMode(output, (uint)resolution.Width, (uint)resolution.Height);

                if (options.DryRun)
                {
                    Console.WriteLine("[dry-run] display: " + DisplayMap.DescribeOutput(output));
                    Console.WriteLine("[dry-run] current: " + original.Width + "x" + original.Height + " @ " +
                                      original.Refresh + "Hz, scaling " + DisplayApi.ScalingName(originalScaling.ScalingType));
                    Console.WriteLine("[dry-run] target:  " + resolution.Width + "x" + resolution.Height + " @ " +
                                      targetHz + "Hz, scaling " + DisplayApi.ScalingName(scalingType));
                    if (addMode)
                    {
                        Console.WriteLine("[dry-run] would add custom mode");
                    }

                    if (original.Refresh != targetHz)
                    {
                        Console.WriteLine("[dry-run] would switch the native mode to " + targetHz + "Hz first");
                    }

                    return 0;
                }

                IniFile ini = new IniFile();
                ini.Set(StateStore.Section, "gdi", output.Windows.DeviceName);
                ini.Set(StateStore.Section, "monitor", output.Windows.Label);
                ini.SetInt(StateStore.Section, "width", original.Width);
                ini.SetInt(StateStore.Section, "height", original.Height);
                ini.SetInt(StateStore.Section, "hz", original.Refresh);
                ini.SetInt(StateStore.Section, "bits", original.BitsPerPel);
                ini.SetInt(StateStore.Section, "scaling", preferredScaling);
                ini.SetInt(StateStore.Section, "scaling_changed", 0);
                ini.Set(StateStore.Section, "added", addMode ? resolution.Width + "x" + resolution.Height : string.Empty);
                ini.SetInt(StateStore.Section, "added_by_set", 0);
                ini.Set(StateStore.Section, "phase", "testing");
                StateStore.Save(ini);

                try
                {
                    if (original.Refresh != targetHz)
                    {
                        DisplayMode native = DisplayApi.FindMode(output.Windows, original.Width, original.Height, targetHz);
                        if (native == null)
                        {
                            native = DisplayApi.FindBestModeAtHz(output.Windows, targetHz);
                        }

                        if (native == null)
                        {
                            throw new InvalidOperationException("no mode at " + targetHz + "Hz is available");
                        }

                        int result = DisplayApi.ApplyMode(output.Windows.DeviceName, native.Width, native.Height, native.Refresh, 32);
                        if (result != DisplayApi.DispChangeSuccessful)
                        {
                            throw new InvalidOperationException("could not switch to " + targetHz + "Hz: " + DisplayApi.DispChangeName(result));
                        }
                    }

                    if (addMode)
                    {
                        bool addedBySet = false;
                        try
                        {
                            session.AddCustomMode(output.Handle, (uint)resolution.Width, (uint)resolution.Height);
                            addedBySet = true;
                        }
                        catch (IgclException ex)
                        {
                            if (ex.Result != IgclResult.CustomModeStandardExists &&
                                ex.Result != IgclResult.CustomModeMatchingExists)
                            {
                                throw;
                            }
                        }

                        ini.SetInt(StateStore.Section, "added_by_set", addedBySet ? 1 : 0);
                        StateStore.Save(ini);
                    }

                    if (originalScaling.ScalingType != scalingType)
                    {
                        int caps = session.GetSupportedScaling(output.Handle);
                        if ((caps & scalingType) == 0)
                        {
                            throw new InvalidOperationException("scaling " + DisplayApi.ScalingName(scalingType) + " is not supported");
                        }

                        session.SetScaling(output.Handle, scalingType, true);
                        ini.SetInt(StateStore.Section, "scaling_changed", 1);
                        StateStore.Save(ini);
                    }

                    List<DisplayMode> modes = DisplayApi.GetModes(output.Windows.DeviceName);
                    DisplayMode target = FindBestMode(modes, resolution.Width, resolution.Height, targetHz);
                    if (target == null)
                    {
                        throw new InvalidOperationException("the custom mode did not appear in the Windows mode list");
                    }

                    int apply = DisplayApi.ApplyMode(output.Windows.DeviceName, target.Width, target.Height, target.Refresh, 32);
                    if (apply != DisplayApi.DispChangeSuccessful)
                    {
                        throw new InvalidOperationException("could not apply " + target + ": " + DisplayApi.DispChangeName(apply));
                    }

                    Console.WriteLine("Applied " + target.Width + "x" + target.Height + " @ " + target.Refresh +
                                      "Hz, scaling " + DisplayApi.ScalingName(scalingType) + ".");
                    Console.WriteLine("The display will revert automatically unless you confirm.");

                    bool keep = options.Keep || options.Timeout <= 0 || ConfirmKeep(options.Timeout);

                    if (keep)
                    {
                        ini.Set(StateStore.Section, "phase", "applied");
                        StateStore.Save(ini);
                        Console.WriteLine("Kept. Use `crt reset` to restore the previous mode.");
                        return 0;
                    }

                    Console.WriteLine("Reverting...");
                    RevertState(session, output, ini);
                    return 0;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("error: " + ex.Message);
                    Console.Error.WriteLine("reverting...");
                    RevertState(session, output, ini);
                    return 1;
                }
            }
        }

        private static int ResetCommand(Options options)
        {
            IniFile ini = StateStore.Load();

            using (IgclSession session = new IgclSession())
            {
                List<CrtOutput> outputs = LoadOutputs(session);
                CrtOutput output = null;

                string gdi = ini.Get(StateStore.Section, "gdi", string.Empty);
                if (!string.IsNullOrEmpty(gdi))
                {
                    output = DisplayMap.FindOutput(outputs, gdi);
                }

                if (output == null && !string.IsNullOrEmpty(options.Display))
                {
                    output = DisplayMap.FindOutput(outputs, options.Display);
                }

                if (output == null)
                {
                    output = DisplayMap.FindOutput(outputs, null);
                }

                if (ini.HasSection(StateStore.Section))
                {
                    if (options.DryRun)
                    {
                        Console.WriteLine("[dry-run] would restore " +
                                          ini.Get(StateStore.Section, "width", "?") + "x" +
                                          ini.Get(StateStore.Section, "height", "?") + " @ " +
                                          ini.Get(StateStore.Section, "hz", "?") + "Hz");
                        return 0;
                    }

                    RevertState(session, output, ini);
                    Console.WriteLine("Restored the previous mode.");
                    return 0;
                }

                if (options.All)
                {
                    if (output == null || output.Windows == null)
                    {
                        Console.Error.WriteLine("error: could not resolve a display to reset");
                        return 1;
                    }

                    if (options.DryRun)
                    {
                        Console.WriteLine("[dry-run] would remove " + output.CustomModes.Count +
                                          " custom mode(s) and restore the registry mode");
                        return 0;
                    }

                    if (output.CustomModes.Count > 0)
                    {
                        session.RemoveCustomModes(output.Handle, output.CustomModes);
                        Console.WriteLine("Removed " + output.CustomModes.Count + " custom mode(s).");
                    }
                    else
                    {
                        Console.WriteLine("No custom modes to remove.");
                    }

                    DisplayMode registry = DisplayApi.GetRegistryMode(output.Windows.DeviceName);
                    if (registry != null)
                    {
                        int result = DisplayApi.ApplyMode(output.Windows.DeviceName, registry.Width, registry.Height, registry.Refresh, registry.BitsPerPel);
                        if (result == DisplayApi.DispChangeSuccessful)
                        {
                            Console.WriteLine("Restored " + registry + ".");
                        }
                        else
                        {
                            Console.Error.WriteLine("warning: could not restore " + registry + ": " + DisplayApi.DispChangeName(result));
                        }
                    }

                    return 0;
                }

                Console.WriteLine("Nothing to reset. Use `crt remove <WxH>` or `crt reset --all`.");
                return 0;
            }
        }

        private static int DoctorCommand(Options options)
        {
            Console.WriteLine("crt " + Version + " doctor");
            Console.WriteLine("OS: " + Environment.OSVersion.VersionString +
                              " (" + (Environment.Is64BitOperatingSystem ? "x64" : "x86") + ")");

            string dll = Path.Combine(Environment.SystemDirectory, "ControlLib.dll");
            if (File.Exists(dll))
            {
                FileVersionInfo info = FileVersionInfo.GetVersionInfo(dll);
                Console.WriteLine("ControlLib.dll: " + info.FileVersion + " (" + dll + ")");
            }
            else
            {
                Console.WriteLine("ControlLib.dll: NOT FOUND at " + dll);
            }

            Console.WriteLine("Struct sizes: init=" + Marshal.SizeOf(typeof(CtlInitArgs)) +
                              " display=" + Marshal.SizeOf(typeof(CtlDisplayProperties)) +
                              " encoder=" + Marshal.SizeOf(typeof(CtlEncoderProperties)) +
                              " scaling=" + Marshal.SizeOf(typeof(CtlScalingSettings)) +
                              " caps=" + Marshal.SizeOf(typeof(CtlScalingCaps)) +
                              " custom=" + Marshal.SizeOf(typeof(CtlGetSetCustomModeArgs)));

            using (IgclSession session = new IgclSession())
            {
                Console.WriteLine("IGCL: initialized, api version 0x" + session.ApiVersion.ToString("X8") +
                                  ", adapters=" + session.Adapters.Count);

                List<CrtOutput> outputs = DisplayMap.LoadOutputs(session);
                Console.WriteLine("Outputs: " + outputs.Count);

                foreach (CrtOutput output in outputs)
                {
                    Console.WriteLine();
                    Console.WriteLine("  " + DisplayMap.DescribeOutput(output));

                    if (output.HasProperties)
                    {
                        Console.WriteLine("    igcl: type=" + DisplayApi.OutputTypeName(output.Properties.Type) +
                                          " flags=0x" + output.Properties.DisplayConfigFlags.ToString("X") +
                                          " active=" + ((output.Properties.DisplayConfigFlags & IgclConst.DisplayActive) != 0) +
                                          " attached=" + ((output.Properties.DisplayConfigFlags & IgclConst.DisplayAttached) != 0) +
                                          " encoderId=" + output.Properties.WindowsDisplayEncoderID);
                        Console.WriteLine("    timing: " + output.Properties.HActive + "x" + output.Properties.VActive +
                                          " @ " + output.Properties.RefreshRate.ToString("0.##") + "Hz" +
                                          " pixelclock=" + output.Properties.PixelClock + "Hz");
                    }

                    if (output.HasEncoder)
                    {
                        Console.WriteLine("    encoder flags=0x" + output.Encoder.EncoderConfigFlags.ToString("X") +
                                          " internal=" + ((output.Encoder.EncoderConfigFlags & IgclConst.EncoderInternalDisplay) != 0));
                    }

                    if (output.Windows != null)
                    {
                        Console.WriteLine("    windows: " + output.Windows.DeviceName + "  " +
                                          output.Windows.Label + "  " + output.Windows.MonitorId);
                        Console.WriteLine("    current: " + output.Windows.Width + "x" + output.Windows.Height +
                                          " @ " + output.Windows.Refresh + "Hz " + output.Windows.BitsPerPel + "bpp");
                        Console.WriteLine("    modes:   " + output.Windows.Modes.Count);
                    }
                    else
                    {
                        Console.WriteLine("    windows: unmapped");
                    }

                    try
                    {
                        int caps = session.GetSupportedScaling(output.Handle);
                        CtlScalingSettings scaling = session.GetScaling(output.Handle);
                        Console.WriteLine("    scaling: supported=" + DescribeScalingCaps(caps) +
                                          " current=" + DisplayApi.ScalingName(scaling.ScalingType) +
                                          " preferred=" + DisplayApi.ScalingName(scaling.PreferredScalingType));
                    }
                    catch (IgclException ex)
                    {
                        Console.WriteLine("    scaling: " + ex.Message);
                    }

                    Console.WriteLine("    custom:  " + (output.CustomModes.Count == 0 ? "none" : JoinModes(output.CustomModes)));

                    if (!string.IsNullOrEmpty(output.CustomModeError))
                    {
                        Console.WriteLine("    custom error: " + output.CustomModeError);
                    }
                }

                Console.WriteLine();
                Console.WriteLine("CCD paths:");
                List<CcdDisplay> ccd = DisplayApi.GetCcdDisplays();
                foreach (CcdDisplay display in ccd)
                {
                    Console.WriteLine("  " + display.GdiName + " -> " + display.MonitorName +
                                      " [" + DisplayApi.TechnologyName(display.OutputTechnology) + "]" +
                                      " targetId=" + display.TargetId +
                                      " internal=" + display.IsInternal);
                }

                Console.WriteLine();
                Console.WriteLine("Windows displays:");
                List<WindowsDisplay> windows = DisplayApi.GetDisplays();
                foreach (WindowsDisplay display in windows)
                {
                    Console.WriteLine("  " + display.DeviceName + "  " + display.Label + "  " +
                                      display.Width + "x" + display.Height + " @ " + display.Refresh + "Hz" +
                                      "  modes=" + display.Modes.Count +
                                      (display.IsPrimary ? "  primary" : string.Empty));
                }
            }

            return 0;
        }

        private static void RevertState(IgclSession session, CrtOutput output, IniFile ini)
        {
            string gdi = ini.Get(StateStore.Section, "gdi",
                output != null && output.Windows != null ? output.Windows.DeviceName : string.Empty);
            int width = ini.GetInt(StateStore.Section, "width", 0);
            int height = ini.GetInt(StateStore.Section, "height", 0);
            int hz = ini.GetInt(StateStore.Section, "hz", 0);
            int bits = ini.GetInt(StateStore.Section, "bits", 32);
            int scaling = ini.GetInt(StateStore.Section, "scaling", 0);
            bool scalingChanged = ini.GetInt(StateStore.Section, "scaling_changed", 0) != 0;
            string added = ini.Get(StateStore.Section, "added", string.Empty);
            bool addedBySet = ini.GetInt(StateStore.Section, "added_by_set", 0) != 0;

            if (width > 0 && height > 0 && !string.IsNullOrEmpty(gdi))
            {
                int result = DisplayApi.RestoreMode(gdi, width, height, hz, bits);
                if (result != DisplayApi.DispChangeSuccessful)
                {
                    Console.Error.WriteLine("warning: could not restore " + width + "x" + height +
                                            " @ " + hz + "Hz: " + DisplayApi.DispChangeName(result));
                }
            }

            if (scalingChanged && scaling > 0 && output != null)
            {
                try
                {
                    session.SetScaling(output.Handle, scaling, true);
                }
                catch (IgclException ex)
                {
                    Console.Error.WriteLine("warning: could not restore scaling: " + ex.Message);
                }
            }

            if (addedBySet && !string.IsNullOrEmpty(added) && output != null)
            {
                Resolution resolution;
                if (TryParseResolution(added, out resolution))
                {
                    try
                    {
                        List<CtlCustomSrcMode> modes = new List<CtlCustomSrcMode>();
                        CtlCustomSrcMode mode = new CtlCustomSrcMode();
                        mode.SourceX = (uint)resolution.Width;
                        mode.SourceY = (uint)resolution.Height;
                        modes.Add(mode);
                        session.RemoveCustomModes(output.Handle, modes);
                    }
                    catch (IgclException ex)
                    {
                        Console.Error.WriteLine("warning: could not remove custom mode: " + ex.Message);
                    }
                }
            }

            StateStore.Clear();
        }

        private static DisplayMode FindBestMode(List<DisplayMode> modes, int width, int height, int hz)
        {
            DisplayMode best = null;

            foreach (DisplayMode mode in modes)
            {
                if (mode.Width != width || mode.Height != height)
                {
                    continue;
                }

                if (mode.Refresh == hz)
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

        private static bool ConfirmKeep(int timeoutSeconds)
        {
            if (timeoutSeconds <= 0)
            {
                return true;
            }

            Console.WriteLine();
            Console.Write("Keep this mode? [Y/n]  Auto-revert in " + timeoutSeconds + "s...");

            DateTime deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
            bool interactive = !Console.IsInputRedirected;
            int lastShown = timeoutSeconds;

            while (DateTime.UtcNow < deadline)
            {
                if (interactive && Console.KeyAvailable)
                {
                    ConsoleKeyInfo key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.Enter || key.Key == ConsoleKey.Y)
                    {
                        Console.WriteLine();
                        return true;
                    }

                    if (key.Key == ConsoleKey.N || key.Key == ConsoleKey.Escape)
                    {
                        Console.WriteLine();
                        return false;
                    }
                }

                int remaining = (int)Math.Ceiling((deadline - DateTime.UtcNow).TotalSeconds);
                if (remaining != lastShown)
                {
                    lastShown = remaining;
                    Console.Write("\rKeep this mode? [Y/n]  Auto-revert in " + remaining + "s...");
                }

                System.Threading.Thread.Sleep(100);
            }

            Console.WriteLine();
            return false;
        }

        private static int ParseScaling(string name)
        {
            switch (name.ToLowerInvariant())
            {
                case "stretched":
                case "stretch":
                case "full":
                    return IgclConst.ScalingStretched;
                case "aspect":
                case "aspect-ratio":
                case "preserve":
                    return IgclConst.ScalingAspectCentered;
                case "centered":
                case "center":
                    return IgclConst.ScalingCentered;
                case "identity":
                case "none":
                    return IgclConst.ScalingIdentity;
                default:
                    throw new ArgumentException("unknown scaling type '" + name + "'");
            }
        }

        private static string DescribeScalingCaps(int caps)
        {
            List<string> names = new List<string>();

            if ((caps & IgclConst.ScalingIdentity) != 0) names.Add("identity");
            if ((caps & IgclConst.ScalingCentered) != 0) names.Add("centered");
            if ((caps & IgclConst.ScalingStretched) != 0) names.Add("stretched");
            if ((caps & IgclConst.ScalingAspectCentered) != 0) names.Add("aspect");
            if ((caps & IgclConst.ScalingCustom) != 0) names.Add("custom");

            if (names.Count == 0)
            {
                return "none";
            }

            return string.Join(", ", names.ToArray());
        }

        private static string JoinModes(List<CtlCustomSrcMode> modes)
        {
            StringBuilder builder = new StringBuilder();

            foreach (CtlCustomSrcMode mode in modes)
            {
                if (builder.Length > 0)
                {
                    builder.Append(", ");
                }

                builder.Append(mode.SourceX);
                builder.Append("x");
                builder.Append(mode.SourceY);
            }

            return builder.ToString();
        }

        private static void PrintHelp()
        {
            Console.WriteLine("crt " + Version + " - Intel custom resolution tool");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  crt list");
            Console.WriteLine("  crt modes [--display N]");
            Console.WriteLine("  crt add <WxH> [--display N] [--hz N] [--dry-run]");
            Console.WriteLine("  crt remove <WxH> [--display N] [--dry-run]");
            Console.WriteLine("  crt scaling <stretched|aspect|centered|identity> [--display N]");
            Console.WriteLine("  crt set <WxH> [--display N] [--hz N] [--stretch|--aspect|--center]");
            Console.WriteLine("               [--timeout 15] [--keep] [--dry-run]");
            Console.WriteLine("  crt reset [--display N] [--all] [--dry-run]");
            Console.WriteLine("  crt doctor");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  --display N       output index (see `crt list`), GDI name, or monitor name");
            Console.WriteLine("  --hz N            target refresh rate");
            Console.WriteLine("  --timeout N       auto-revert delay in seconds (0 = keep)");
            Console.WriteLine("  --keep            skip the auto-revert countdown");
            Console.WriteLine("  --dry-run         print the plan without changing anything");
            Console.WriteLine("  --force-internal  allow the internal laptop panel (Intel does not support it)");
            Console.WriteLine();
            Console.WriteLine("Typical Valorant workflow:");
            Console.WriteLine("  crt add 1440x1080 --display 1");
            Console.WriteLine("  crt scaling stretched --display 1");
            Console.WriteLine("  launch Valorant and pick 1440x1080");
        }
    }
}
