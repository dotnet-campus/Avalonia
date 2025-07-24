using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Avalonia.Platform;
using static Avalonia.X11.XLib;

namespace Avalonia.X11.Screens;

internal partial class X11Screens
{
    internal unsafe class X11Screen(MonitorInfo info, X11Info x11, IScalingProvider? scalingProvider, int id) : PlatformScreen(new PlatformHandle(info.Name, "XRandRMonitorName"))
    {
        public Size? PhysicalSize { get; set; }
        // Length of a EDID-Block-Length(128 bytes), XRRGetOutputProperty multiplies offset and length by 4
        private const int EDIDStructureLength = 32;

        public virtual void Refresh(MonitorInfo newInfo)
        {
            if (scalingProvider == null)
                return;

            var namePtr = XGetAtomName(x11.Display, newInfo.Name);
            var name = Marshal.PtrToStringAnsi(namePtr);
            XFree(namePtr);
            IsPrimary = newInfo.IsPrimary;
            Bounds = new PixelRect(newInfo.X, newInfo.Y, newInfo.Width, newInfo.Height);
            Size? pSize = null;
            string? displayProductName = null;
            for (var o = 0; o < newInfo.Outputs.Length; o++)
            {
                if (TryGetEdidInfo(newInfo.Outputs[o], out var edid))
                {
                    if (pSize == null)
                    {
                        var outputSize = GetPhysicalMonitorSizeFromEDID(edid);
                        if (outputSize != null)
                        {
                            pSize = outputSize;
                        }
                    }
                    if (displayProductName == null)
                    {
                        var nameFromEdid = GetDisplayProductNameFromEDID(edid);
                        if (nameFromEdid != null)
                        {
                            displayProductName = nameFromEdid;
                        }
                    }
                    if (pSize != null && displayProductName != null)
                    {
                        break;
                    }
                }
            }
            DisplayName = displayProductName ?? name;
            PhysicalSize = pSize;
            UpdateWorkArea();
            Scaling = scalingProvider.GetScaling(this, id);
        }

        private bool TryGetEdidInfo(IntPtr rrOutput, out byte[] edid)
        {
            edid = [];
            if (rrOutput == IntPtr.Zero)
            {
                return false;
            }
            var properties = XRRListOutputProperties(x11.Display, rrOutput, out var propertyCount);
            var hasEDID = false;
            for (var pc = 0; pc < propertyCount; pc++)
            {
                if (properties[pc] == x11.Atoms.EDID)
                    hasEDID = true;
            }

            if (!hasEDID)
            {
                return false;
            }
            XRRGetOutputProperty(x11.Display, rrOutput, x11.Atoms.EDID, 0, EDIDStructureLength, false, false,
                x11.Atoms.AnyPropertyType, out IntPtr actualType, out int actualFormat, out int bytesAfter, out _,
                out IntPtr prop);
            if (actualType != x11.Atoms.XA_INTEGER)
            {
                return false;
            }
            if (actualFormat != 8) // Expecting an byte array
            {
                return false;
            }

            edid = new byte[bytesAfter];
            Marshal.Copy(prop, edid, 0, bytesAfter);
            XFree(prop);
            XFree(new IntPtr(properties));
            return true;
        }

        private unsafe Size? GetPhysicalMonitorSizeFromEDID(byte[] edid)
        {
            if (edid.Length < 22)
                return null;
            var width = edid[21]; // 0x15 1 Max. Horizontal Image Size cm. 
            var height = edid[22]; // 0x16 1 Max. Vertical Image Size cm. 
            if (width == 0 && height == 0)
                return null;
            return new Size(width * 10, height * 10);
        }

        private string? GetDisplayProductNameFromEDID(byte[] edid)
        {
            if (edid == null || edid.Length < 128)
                throw new ArgumentException("Invalid EDID data.");

            // EDID 的 Descriptor Blocks 从第 54 字节开始，每个块 18 字节
            for (int i = 54; i <= 108; i += 18)
            {
                // 检查 Descriptor Block 的类型是否为 0xFC (Display Product Name)
                if (edid[i] == 0x00 && edid[i + 1] == 0x00 && edid[i + 2] == 0x00 && edid[i + 3] == 0xFC)
                {
                    // 提取名称字符串（最多 13 字节，可能以 0x0A 结尾）
                    var nameBytes = new byte[13];
                    Array.Copy(edid, i + 5, nameBytes, 0, 13);
                    var name = Encoding.ASCII.GetString(nameBytes).TrimEnd('\0', '\n', '\r');
                    return name;
                }
            }
            return null; // 未找到 Display Product Name
        }

        protected unsafe void UpdateWorkArea()
        {
            var rect = default(PixelRect);
            //Fallback value
            rect = rect.Union(Bounds);
            WorkingArea = Bounds;

            var res = XGetWindowProperty(x11.Display,
                x11.RootWindow,
                x11.Atoms._NET_WORKAREA,
                IntPtr.Zero,
                new IntPtr(128),
                false,
                x11.Atoms.AnyPropertyType,
                out var type,
                out var format,
                out var count,
                out var bytesAfter,
                out var prop);

            if (res != (int)Status.Success || type == IntPtr.Zero ||
                format == 0 || bytesAfter.ToInt64() != 0 || count.ToInt64() % 4 != 0)
                return;

            var pwa = (IntPtr*)prop;
            var wa = new PixelRect(pwa[0].ToInt32(), pwa[1].ToInt32(), pwa[2].ToInt32(), pwa[3].ToInt32());

            WorkingArea = Bounds.Intersect(wa);
            if (WorkingArea.Width <= 0 || WorkingArea.Height <= 0)
                WorkingArea = Bounds;
            XFree(prop);
        }
    }

    internal class FallBackScreen : X11Screen
    {
        public FallBackScreen(PixelRect pixelRect, X11Info x11) : base(default, x11, null, 0)
        {
            Bounds = pixelRect;
            DisplayName = "Default";
            IsPrimary = true;
            PhysicalSize = pixelRect.Size.ToSize(Scaling);
            UpdateWorkArea();
        }
        public override void Refresh(MonitorInfo newInfo)
        {
        }
    }

    internal interface IX11RawScreenInfoProvider
    {
        nint[] ScreenKeys { get; }
        event Action? Changed;
        X11Screen CreateScreenFromKey(nint key);
        MonitorInfo GetMonitorInfoByKey(nint key);
    }

    internal unsafe struct MonitorInfo
    {
        public IntPtr Name;
        public bool IsPrimary;
        public int X;
        public int Y;
        public int Width;
        public int Height;
        public IntPtr[] Outputs;
    }

    private class Randr15ScreensImpl : IX11RawScreenInfoProvider
    {
        private MonitorInfo[]? _cache;
        private readonly X11Info _x11;
        private readonly IntPtr _window;
        private readonly IScalingProvider _scalingProvider;

        public nint[] ScreenKeys => MonitorInfos.Select(x => x.Name).ToArray();

        public event Action? Changed;

        public Randr15ScreensImpl(AvaloniaX11Platform platform)
        {
            _x11 = platform.Info;
            _window = CreateEventWindow(platform, OnEvent);
            _scalingProvider = GetScalingProvider(platform);
            XRRSelectInput(_x11.Display, _window, RandrEventMask.RRScreenChangeNotify);
            if (_scalingProvider is IScalingProviderWithChanges scalingWithChanges)
                scalingWithChanges.SettingsChanged += () => Changed?.Invoke();
        }

        private void OnEvent(ref XEvent ev)
        {
            if ((int)ev.type == _x11.RandrEventBase + (int)RandrEvent.RRScreenChangeNotify)
            {
                _cache = null;
                Changed?.Invoke();
            }
        }

        private unsafe MonitorInfo[] MonitorInfos
        {
            get
            {
                if (_cache != null)
                    return _cache;
                var monitors = XRRGetMonitors(_x11.Display, _window, true, out var count);

                var screens = new MonitorInfo[count];
                for (var c = 0; c < count; c++)
                {
                    var mon = monitors[c];
                    var outputs = new nint[mon.NOutput];

                    for (int i = 0; i < outputs.Length; i++)
                    {
                        outputs[i] = mon.Outputs[i];
                    }

                    screens[c] = new MonitorInfo()
                    {
                        Name = mon.Name,
                        IsPrimary = mon.Primary != 0,
                        X = mon.X,
                        Y = mon.Y,
                        Width = mon.Width,
                        Height = mon.Height,
                        Outputs = outputs
                    };
                }

                XFree(new IntPtr(monitors));

                return screens;
            }
        }

        public X11Screen CreateScreenFromKey(nint key)
        {
            var infos = MonitorInfos;
            for (var i = 0; i < infos.Length; i++)
            {
                if (infos[i].Name == key)
                {
                    return new X11Screen(infos[i], _x11, _scalingProvider, i);
                }
            }

            throw new ArgumentOutOfRangeException(nameof(key));
        }

        public MonitorInfo GetMonitorInfoByKey(nint key)
        {
            var infos = MonitorInfos;
            for (var i = 0; i < infos.Length; i++)
            {
                if (infos[i].Name == key)
                {
                    return infos[i];
                }
            }

            throw new ArgumentOutOfRangeException(nameof(key));
        }
    }

    private class FallbackScreensImpl : IX11RawScreenInfoProvider
    {
        private readonly X11Info _info;
        private XGeometry _geo;

        public event Action? Changed
        {
            add { }
            remove { }
        }

        public FallbackScreensImpl(AvaloniaX11Platform platform)
        {
            _info = platform.Info;
            if (UpdateRootWindowGeometry())
                platform.Globals.RootGeometryChangedChanged += () => UpdateRootWindowGeometry();
        }

        private bool UpdateRootWindowGeometry() => XGetGeometry(_info.Display, _info.RootWindow, out _geo);

        public X11Screen CreateScreenFromKey(nint key)
        {
            return new FallBackScreen(new PixelRect(0, 0, _geo.width, _geo.height), _info);
        }

        public MonitorInfo GetMonitorInfoByKey(nint key)
        {
            return default;
        }

        public nint[] ScreenKeys => new[] { IntPtr.Zero };
    }
}
