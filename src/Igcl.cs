using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Crt
{
    internal static class IgclResult
    {
        public const uint Success = 0x00000000;
        public const uint NotInitialized = 0x40000001;
        public const uint InsufficientPermissions = 0x40000006;
        public const uint NotAvailable = 0x40000007;
        public const uint UnsupportedVersion = 0x40000009;
        public const uint UnsupportedFeature = 0x4000000A;
        public const uint InvalidArgument = 0x4000000B;
        public const uint InvalidNullHandle = 0x4000000D;
        public const uint InvalidNullPointer = 0x4000000E;
        public const uint LoadFailure = 0x40000026;
        public const uint Unknown = 0x4000FFFF;
        public const uint DisplayNotAttached = 0x48000003;
        public const uint CustomModeStandardExists = 0x4800001A;
        public const uint CustomModeMatchingExists = 0x4800001B;
        public const uint CustomModeInsufficientMemory = 0x4800001C;

        public static string Describe(uint code)
        {
            switch (code)
            {
                case Success: return "success";
                case NotInitialized: return "IGCL is not initialized";
                case InsufficientPermissions: return "access denied (run elevated)";
                case NotAvailable: return "resource is not available";
                case UnsupportedVersion: return "unsupported struct/API version";
                case UnsupportedFeature: return "feature not supported by this driver";
                case InvalidArgument: return "invalid argument";
                case InvalidNullHandle: return "invalid handle";
                case InvalidNullPointer: return "invalid pointer";
                case LoadFailure: return "ControlLib.dll could not be loaded";
                case DisplayNotAttached: return "display output is not attached";
                case CustomModeStandardExists: return "this is already a standard mode";
                case CustomModeMatchingExists: return "a matching mode already exists";
                case CustomModeInsufficientMemory: return "driver is out of custom-mode slots";
                case Unknown: return "unknown IGCL error";
                default: return "IGCL error";
            }
        }
    }

    internal static class IgclConst
    {
        public const int DisplayActive = 1 << 0;
        public const int DisplayAttached = 1 << 1;

        public const int OutputInvalid = 0;
        public const int OutputDisplayPort = 1;
        public const int OutputHdmi = 2;
        public const int OutputDvi = 3;
        public const int OutputMipi = 4;
        public const int OutputCrt = 5;

        public const uint EncoderInternalDisplay = 1u << 0;

        public const int ScalingIdentity = 1 << 0;
        public const int ScalingCentered = 1 << 1;
        public const int ScalingStretched = 1 << 2;
        public const int ScalingAspectCentered = 1 << 3;
        public const int ScalingCustom = 1 << 4;

        public const int CustomModeGet = 0;
        public const int CustomModeAdd = 1;
        public const int CustomModeRemove = 2;
    }

    internal sealed class IgclException : Exception
    {
        public uint Result { get; private set; }
        public string Operation { get; private set; }

        public IgclException(uint result, string operation)
            : base(string.Format("{0} failed: 0x{1:X8} ({2})", operation, result, IgclResult.Describe(result)))
        {
            Result = result;
            Operation = operation;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct CtlInitArgs
    {
        public uint Size;
        public byte Version;
        public uint AppVersion;
        public int Flags;
        public uint SupportedVersion;
        public Guid ApplicationUID;
    }

    [StructLayout(LayoutKind.Explicit, Size = 200)]
    internal struct CtlDisplayProperties
    {
        [FieldOffset(0)] public uint Size;
        [FieldOffset(4)] public byte Version;
        [FieldOffset(8)] public uint WindowsDisplayEncoderID;
        [FieldOffset(24)] public int Type;
        [FieldOffset(28)] public int AttachedDisplayMuxType;
        [FieldOffset(32)] public int ProtocolConverterOutput;
        [FieldOffset(40)] public uint SupportedOutputBpcFlags;
        [FieldOffset(44)] public uint ProtocolConverterType;
        [FieldOffset(48)] public int DisplayConfigFlags;
        [FieldOffset(52)] public uint FeatureEnabledFlags;
        [FieldOffset(56)] public uint FeatureSupportedFlags;
        [FieldOffset(60)] public uint AdvancedFeatureEnabledFlags;
        [FieldOffset(64)] public uint AdvancedFeatureSupportedFlags;

        // ctl_display_timing_t starts at offset 72 (8-byte aligned)
        [FieldOffset(72)] public uint TimingSize;
        [FieldOffset(76)] public byte TimingVersion;
        [FieldOffset(80)] public ulong PixelClock;
        [FieldOffset(88)] public uint HActive;
        [FieldOffset(92)] public uint VActive;
        [FieldOffset(96)] public uint HTotal;
        [FieldOffset(100)] public uint VTotal;
        [FieldOffset(104)] public uint HBlank;
        [FieldOffset(108)] public uint VBlank;
        [FieldOffset(112)] public uint HSync;
        [FieldOffset(116)] public uint VSync;
        [FieldOffset(120)] public float RefreshRate;
        [FieldOffset(124)] public int SignalStandard;
        [FieldOffset(128)] public byte VicId;
    }

    [StructLayout(LayoutKind.Explicit, Size = 120)]
    internal struct CtlEncoderProperties
    {
        [FieldOffset(0)] public uint Size;
        [FieldOffset(4)] public byte Version;
        [FieldOffset(8)] public uint WindowsDisplayEncoderID;
        [FieldOffset(24)] public int Type;
        [FieldOffset(28)] public byte IsOnBoardProtocolConverterOutputPresent;
        [FieldOffset(32)] public byte SupportedSpecMajor;
        [FieldOffset(33)] public byte SupportedSpecMinor;
        [FieldOffset(34)] public byte SupportedSpecRevision;
        [FieldOffset(36)] public uint SupportedOutputBpcFlags;
        [FieldOffset(40)] public uint EncoderConfigFlags;
        [FieldOffset(44)] public uint FeatureSupportedFlags;
        [FieldOffset(48)] public uint AdvancedFeatureSupportedFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct CtlScalingCaps
    {
        public uint Size;
        public byte Version;
        public int SupportedScaling;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct CtlScalingSettings
    {
        public uint Size;
        public byte Version;
        public byte Enable;
        public int ScalingType;
        public uint CustomScalingX;
        public uint CustomScalingY;
        public byte HardwareModeSet;
        public int PreferredScalingType;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct CtlGetSetCustomModeArgs
    {
        public uint Size;
        public byte Version;
        public int CustomModeOpType;
        public uint NumOfModes;
        public IntPtr pCustomSrcModeList;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct CtlCustomSrcMode
    {
        public uint SourceX;
        public uint SourceY;
    }

    internal static class IgclNative
    {
        private const string Dll = "ControlLib.dll";

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern uint ctlInit(ref CtlInitArgs args, out IntPtr apiHandle);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern uint ctlClose(IntPtr apiHandle);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern uint ctlEnumerateDevices(IntPtr apiHandle, ref uint count, IntPtr[] devices);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern uint ctlEnumerateDisplayOutputs(IntPtr adapter, ref uint count, IntPtr[] outputs);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern uint ctlGetDisplayProperties(IntPtr output, ref CtlDisplayProperties properties);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern uint ctlGetAdaperDisplayEncoderProperties(IntPtr output, ref CtlEncoderProperties properties);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern uint ctlGetSupportedScalingCapability(IntPtr output, ref CtlScalingCaps caps);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern uint ctlGetCurrentScaling(IntPtr output, ref CtlScalingSettings settings);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern uint ctlSetCurrentScaling(IntPtr output, ref CtlScalingSettings settings);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern uint ctlGetSetCustomMode(IntPtr output, ref CtlGetSetCustomModeArgs args);
    }

    internal sealed class CrtOutput
    {
        public int Index;
        public IntPtr Handle;
        public IntPtr AdapterHandle;
        public CtlDisplayProperties Properties;
        public bool HasProperties;
        public CtlEncoderProperties Encoder;
        public bool HasEncoder;
        public WindowsDisplay Windows;
        public bool WindowsInternal;
        public List<CtlCustomSrcMode> CustomModes = new List<CtlCustomSrcMode>();
        public string CustomModeError;
    }

    internal sealed class IgclSession : IDisposable
    {
        private IntPtr _apiHandle;
        private readonly List<IntPtr> _adapters = new List<IntPtr>();
        private bool _disposed;

        public uint ApiVersion { get; private set; }
        public IList<IntPtr> Adapters { get { return _adapters; } }

        public IgclSession()
        {
            CtlInitArgs args = new CtlInitArgs();
            args.Size = (uint)Marshal.SizeOf(typeof(CtlInitArgs));
            args.Version = 0;
            args.AppVersion = (1u << 16) | 1u;
            args.Flags = 0;
            args.SupportedVersion = 0;
            args.ApplicationUID = Guid.Empty;

            IntPtr handle;
            uint result = IgclNative.ctlInit(ref args, out handle);
            if (result != IgclResult.Success)
            {
                throw new IgclException(result, "ctlInit");
            }

            _apiHandle = handle;
            ApiVersion = args.SupportedVersion;

            uint count = 0;
            result = IgclNative.ctlEnumerateDevices(_apiHandle, ref count, null);
            if (result != IgclResult.Success)
            {
                throw new IgclException(result, "ctlEnumerateDevices(count)");
            }

            if (count == 0)
            {
                return;
            }

            IntPtr[] adapters = new IntPtr[count];
            result = IgclNative.ctlEnumerateDevices(_apiHandle, ref count, adapters);
            if (result != IgclResult.Success)
            {
                throw new IgclException(result, "ctlEnumerateDevices(list)");
            }

            for (int i = 0; i < count; i++)
            {
                _adapters.Add(adapters[i]);
            }
        }

        public List<CrtOutput> GetOutputs()
        {
            List<CrtOutput> outputs = new List<CrtOutput>();

            foreach (IntPtr adapter in _adapters)
            {
                uint count = 0;
                uint result = IgclNative.ctlEnumerateDisplayOutputs(adapter, ref count, null);
                if (result != IgclResult.Success)
                {
                    throw new IgclException(result, "ctlEnumerateDisplayOutputs(count)");
                }

                if (count == 0)
                {
                    continue;
                }

                IntPtr[] handles = new IntPtr[count];
                result = IgclNative.ctlEnumerateDisplayOutputs(adapter, ref count, handles);
                if (result != IgclResult.Success)
                {
                    throw new IgclException(result, "ctlEnumerateDisplayOutputs(list)");
                }

                for (int i = 0; i < count; i++)
                {
                    CrtOutput output = new CrtOutput();
                    output.Index = outputs.Count;
                    output.Handle = handles[i];
                    output.AdapterHandle = adapter;

                    CtlDisplayProperties properties = new CtlDisplayProperties();
                    if (TryGetDisplayProperties(output.Handle, ref properties))
                    {
                        output.Properties = properties;
                        output.HasProperties = true;
                    }

                    CtlEncoderProperties encoder = new CtlEncoderProperties();
                    if (TryGetEncoderProperties(output.Handle, ref encoder))
                    {
                        output.Encoder = encoder;
                        output.HasEncoder = true;
                    }

                    outputs.Add(output);
                }
            }

            return outputs;
        }

        private static bool TryGetDisplayProperties(IntPtr output, ref CtlDisplayProperties properties)
        {
            // The header says 200 bytes on x64. Older driver builds may expose a
            // smaller struct, so try the known sizes from largest to smallest.
            uint[] sizes = new uint[] { 200, 192, 184, 176, 168, 160, 152, 144, 136 };

            foreach (uint size in sizes)
            {
                CtlDisplayProperties attempt = new CtlDisplayProperties();
                attempt.Size = size;
                attempt.Version = 0;

                uint result = IgclNative.ctlGetDisplayProperties(output, ref attempt);
                if (result == IgclResult.Success)
                {
                    properties = attempt;
                    return true;
                }

                if (result != IgclResult.UnsupportedVersion &&
                    result != IgclResult.InvalidArgument)
                {
                    return false;
                }
            }

            return false;
        }

        private static bool TryGetEncoderProperties(IntPtr output, ref CtlEncoderProperties properties)
        {
            uint[] sizes = new uint[] { 120, 112, 104, 96, 88, 80, 72, 64, 56, 52 };

            foreach (uint size in sizes)
            {
                CtlEncoderProperties attempt = new CtlEncoderProperties();
                attempt.Size = size;
                attempt.Version = 0;

                uint result = IgclNative.ctlGetAdaperDisplayEncoderProperties(output, ref attempt);
                if (result == IgclResult.Success)
                {
                    properties = attempt;
                    return true;
                }

                if (result != IgclResult.UnsupportedVersion &&
                    result != IgclResult.InvalidArgument)
                {
                    return false;
                }
            }

            return false;
        }

        public List<CtlCustomSrcMode> GetCustomModes(IntPtr output)
        {
            List<CtlCustomSrcMode> modes = new List<CtlCustomSrcMode>();

            CtlGetSetCustomModeArgs args = new CtlGetSetCustomModeArgs();
            args.Size = (uint)Marshal.SizeOf(typeof(CtlGetSetCustomModeArgs));
            args.Version = 0;
            args.CustomModeOpType = IgclConst.CustomModeGet;
            args.NumOfModes = 0;
            args.pCustomSrcModeList = IntPtr.Zero;

            uint result = IgclNative.ctlGetSetCustomMode(output, ref args);
            if (result != IgclResult.Success)
            {
                throw new IgclException(result, "ctlGetSetCustomMode(GET count)");
            }

            uint count = args.NumOfModes;
            if (count == 0)
            {
                return modes;
            }

            int itemSize = Marshal.SizeOf(typeof(CtlCustomSrcMode));
            IntPtr buffer = Marshal.AllocHGlobal(itemSize * (int)count);

            try
            {
                args.NumOfModes = count;
                args.pCustomSrcModeList = buffer;

                result = IgclNative.ctlGetSetCustomMode(output, ref args);
                if (result != IgclResult.Success)
                {
                    throw new IgclException(result, "ctlGetSetCustomMode(GET list)");
                }

                for (int i = 0; i < count; i++)
                {
                    IntPtr item = new IntPtr(buffer.ToInt64() + (i * itemSize));
                    CtlCustomSrcMode mode = (CtlCustomSrcMode)Marshal.PtrToStructure(item, typeof(CtlCustomSrcMode));
                    modes.Add(mode);
                }
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }

            return modes;
        }

        public void AddCustomMode(IntPtr output, uint width, uint height)
        {
            int itemSize = Marshal.SizeOf(typeof(CtlCustomSrcMode));
            IntPtr buffer = Marshal.AllocHGlobal(itemSize);

            try
            {
                CtlCustomSrcMode mode = new CtlCustomSrcMode();
                mode.SourceX = width;
                mode.SourceY = height;
                Marshal.StructureToPtr(mode, buffer, false);

                CtlGetSetCustomModeArgs args = new CtlGetSetCustomModeArgs();
                args.Size = (uint)Marshal.SizeOf(typeof(CtlGetSetCustomModeArgs));
                args.Version = 0;
                args.CustomModeOpType = IgclConst.CustomModeAdd;
                args.NumOfModes = 1;
                args.pCustomSrcModeList = buffer;

                uint result = IgclNative.ctlGetSetCustomMode(output, ref args);
                if (result != IgclResult.Success)
                {
                    throw new IgclException(result, "ctlGetSetCustomMode(ADD)");
                }
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        public void RemoveCustomModes(IntPtr output, List<CtlCustomSrcMode> modes)
        {
            if (modes == null || modes.Count == 0)
            {
                return;
            }

            int itemSize = Marshal.SizeOf(typeof(CtlCustomSrcMode));
            IntPtr buffer = Marshal.AllocHGlobal(itemSize * modes.Count);

            try
            {
                for (int i = 0; i < modes.Count; i++)
                {
                    IntPtr item = new IntPtr(buffer.ToInt64() + (i * itemSize));
                    Marshal.StructureToPtr(modes[i], item, false);
                }

                CtlGetSetCustomModeArgs args = new CtlGetSetCustomModeArgs();
                args.Size = (uint)Marshal.SizeOf(typeof(CtlGetSetCustomModeArgs));
                args.Version = 0;
                args.CustomModeOpType = IgclConst.CustomModeRemove;
                args.NumOfModes = (uint)modes.Count;
                args.pCustomSrcModeList = buffer;

                uint result = IgclNative.ctlGetSetCustomMode(output, ref args);
                if (result != IgclResult.Success)
                {
                    throw new IgclException(result, "ctlGetSetCustomMode(REMOVE)");
                }
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        public int GetSupportedScaling(IntPtr output)
        {
            CtlScalingCaps caps = new CtlScalingCaps();
            caps.Size = (uint)Marshal.SizeOf(typeof(CtlScalingCaps));
            caps.Version = 0;

            uint result = IgclNative.ctlGetSupportedScalingCapability(output, ref caps);
            if (result != IgclResult.Success)
            {
                throw new IgclException(result, "ctlGetSupportedScalingCapability");
            }

            return caps.SupportedScaling;
        }

        public CtlScalingSettings GetScaling(IntPtr output)
        {
            CtlScalingSettings settings = new CtlScalingSettings();
            settings.Size = (uint)Marshal.SizeOf(typeof(CtlScalingSettings));
            settings.Version = 1;

            uint result = IgclNative.ctlGetCurrentScaling(output, ref settings);
            if (result != IgclResult.Success)
            {
                throw new IgclException(result, "ctlGetCurrentScaling");
            }

            return settings;
        }

        public void SetScaling(IntPtr output, int scalingType, bool hardwareModeSet)
        {
            CtlScalingSettings settings = new CtlScalingSettings();
            settings.Size = (uint)Marshal.SizeOf(typeof(CtlScalingSettings));
            settings.Version = 1;
            settings.Enable = 1;
            settings.ScalingType = scalingType;
            settings.CustomScalingX = 0;
            settings.CustomScalingY = 0;
            settings.HardwareModeSet = hardwareModeSet ? (byte)1 : (byte)0;
            settings.PreferredScalingType = 0;

            uint result = IgclNative.ctlSetCurrentScaling(output, ref settings);
            if (result != IgclResult.Success)
            {
                throw new IgclException(result, "ctlSetCurrentScaling");
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            if (_apiHandle != IntPtr.Zero)
            {
                IgclNative.ctlClose(_apiHandle);
                _apiHandle = IntPtr.Zero;
            }
        }
    }
}
