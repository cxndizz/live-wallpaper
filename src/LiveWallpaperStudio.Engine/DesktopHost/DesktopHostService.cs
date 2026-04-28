namespace LiveWallpaperStudio.Engine.DesktopHost;

public sealed class DesktopHostService
{
    private const uint WorkerWMessage = 0x052C;

    public IntPtr EnsureWorkerW()
    {
        var progman = NativeMethods.FindWindow("Progman", null);
        if (progman == IntPtr.Zero)
        {
            return IntPtr.Zero;
        }

        for (var attempt = 0; attempt < 3; attempt++)
        {
            NativeMethods.SendMessageTimeout(
                progman,
                WorkerWMessage,
                IntPtr.Zero,
                IntPtr.Zero,
                0,
                1000,
                out _);

            var host = FindDesktopHost();
            if (host != IntPtr.Zero)
            {
                ExpandHostToVirtualDesktop(host);
                return host;
            }

            Thread.Sleep(120);
        }

        ExpandHostToVirtualDesktop(progman);
        return progman;
    }

    public DesktopAttachmentResult AttachRendererWindow(IntPtr rendererHandle)
    {
        var host = EnsureWorkerW();
        return AttachRendererWindow(rendererHandle, host);
    }

    public DesktopAttachmentResult AttachRendererWindow(IntPtr rendererHandle, IntPtr host)
    {
        if (rendererHandle == IntPtr.Zero)
        {
            return DesktopAttachmentResult.Failed("Renderer window handle is not ready.");
        }

        if (host == IntPtr.Zero)
        {
            return DesktopAttachmentResult.Failed("Could not find the Windows desktop host. If Explorer was restarted, try Reattach or restart Explorer.");
        }

        StripWindowChrome(rendererHandle);
        var previousParent = NativeMethods.SetParent(rendererHandle, host);
        var currentParent = NativeMethods.GetParent(rendererHandle);
        var isAttached = currentParent == host;

        if (!isAttached)
        {
            return DesktopAttachmentResult.Failed(
                "Windows rejected the renderer desktop attachment.",
                host,
                previousParent,
                currentParent);
        }

        return DesktopAttachmentResult.Attached(host, previousParent);
    }

    public static void SetRendererBounds(IntPtr handle, int x, int y, int width, int height)
    {
        var virtualScreen = GetVirtualDesktopBounds();
        NativeMethods.SetWindowPos(
            handle,
            NativeMethods.HwndTop,
            x - virtualScreen.X,
            y - virtualScreen.Y,
            width,
            height,
            NativeMethods.SwpNoActivate | NativeMethods.SwpFrameChanged | NativeMethods.SwpShowWindow);
    }

    public static void StripWindowChrome(IntPtr handle)
    {
        var style = NativeMethods.GetWindowLongPtr(handle, NativeMethods.GwlStyle).ToInt64();
        style &= ~NativeMethods.WsCaption;
        style &= ~NativeMethods.WsThickFrame;
        style &= ~NativeMethods.WsSysMenu;
        style &= ~NativeMethods.WsMinimizeBox;
        style &= ~NativeMethods.WsMaximizeBox;
        style &= ~NativeMethods.WsPopup;
        style |= NativeMethods.WsChild;
        NativeMethods.SetWindowLongPtr(handle, NativeMethods.GwlStyle, new IntPtr(style));

        var exStyle = NativeMethods.GetWindowLongPtr(handle, NativeMethods.GwlExStyle).ToInt64();
        exStyle &= ~NativeMethods.WsExAppWindow;
        exStyle |= NativeMethods.WsExToolWindow;
        NativeMethods.SetWindowLongPtr(handle, NativeMethods.GwlExStyle, new IntPtr(exStyle));

        NativeMethods.SetWindowPos(
            handle,
            IntPtr.Zero,
            0,
            0,
            0,
            0,
            NativeMethods.SwpNoMove | NativeMethods.SwpNoSize | NativeMethods.SwpNoZOrder | NativeMethods.SwpNoActivate | NativeMethods.SwpFrameChanged);
    }

    private static IntPtr FindDesktopHost()
    {
        var workerBehindIcons = IntPtr.Zero;
        var shellViewHost = IntPtr.Zero;

        NativeMethods.EnumWindows((topHandle, _) =>
        {
            var shellView = NativeMethods.FindWindowEx(topHandle, IntPtr.Zero, "SHELLDLL_DefView", null);
            if (shellView == IntPtr.Zero)
            {
                return true;
            }

            shellViewHost = topHandle;
            workerBehindIcons = NativeMethods.FindWindowEx(IntPtr.Zero, topHandle, "WorkerW", null);
            return false;
        }, IntPtr.Zero);

        // The correct host is the top-level WorkerW that sits BEHIND the desktop icons.
        // This is a separate window created by Explorer after receiving the 0x052C message,
        // positioned behind SHELLDLL_DefView in the z-order.
        if (workerBehindIcons != IntPtr.Zero && NativeMethods.IsWindow(workerBehindIcons))
        {
            return workerBehindIcons;
        }

        // When desktop icons are hidden, Explorer may not expose SHELLDLL_DefView.
        // In that mode Progman is the stable desktop host and still keeps the renderer behind app windows.
        var progman = NativeMethods.FindWindow("Progman", null);
        if (progman != IntPtr.Zero)
        {
            return progman;
        }

        // Last resort: use the SHELLDLL_DefView container itself.
        return shellViewHost;
    }

    private static void ExpandHostToVirtualDesktop(IntPtr host)
    {
        var bounds = GetVirtualDesktopBounds();
        NativeMethods.SetWindowPos(
            host,
            NativeMethods.HwndBottom,
            bounds.X,
            bounds.Y,
            bounds.Width,
            bounds.Height,
            NativeMethods.SwpNoActivate | NativeMethods.SwpFrameChanged | NativeMethods.SwpShowWindow);
    }

    private static VirtualDesktopBounds GetVirtualDesktopBounds()
    {
        return new VirtualDesktopBounds(
            NativeMethods.GetSystemMetrics(NativeMethods.SmXVirtualScreen),
            NativeMethods.GetSystemMetrics(NativeMethods.SmYVirtualScreen),
            NativeMethods.GetSystemMetrics(NativeMethods.SmCxVirtualScreen),
            NativeMethods.GetSystemMetrics(NativeMethods.SmCyVirtualScreen));
    }
}

internal sealed record VirtualDesktopBounds(int X, int Y, int Width, int Height);

public sealed record DesktopAttachmentResult(
    bool IsAttached,
    IntPtr HostHandle,
    IntPtr PreviousParent,
    IntPtr CurrentParent,
    string? ErrorMessage)
{
    public static DesktopAttachmentResult Attached(IntPtr hostHandle, IntPtr previousParent)
    {
        return new DesktopAttachmentResult(true, hostHandle, previousParent, hostHandle, null);
    }

    public static DesktopAttachmentResult Failed(
        string errorMessage,
        IntPtr hostHandle = default,
        IntPtr previousParent = default,
        IntPtr currentParent = default)
    {
        return new DesktopAttachmentResult(false, hostHandle, previousParent, currentParent, errorMessage);
    }
}
