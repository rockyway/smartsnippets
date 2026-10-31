using System;
using System.Diagnostics;
using Microsoft.UI.Xaml.Input;
using SmartSnippets.Presentation.Services;

namespace SmartSnippets.UnoProbe;

internal sealed partial class CompanionSurfaceProbe
{
    private void RowEntered(object sender, PointerRoutedEventArgs args) => Guard(() =>
    {
        Require(_stage == 1);
        _pointer!.RowEntered();
    });

    private void RowExited(object sender, PointerRoutedEventArgs args) => Guard(() =>
    {
        Require(_stage == 2 && _surface is { IsVisible: true, IsWarmupCloaked: false });
        _pointer!.RowExited();
    });

    private void SurfaceEntered(object sender, PointerRoutedEventArgs args) => Guard(() =>
    {
        Require(_stage == 2);
        _pointer!.SurfaceEntered();
        _handoffUntil = Stopwatch.GetTimestamp() + Stopwatch.Frequency / 4;
        _stage = 3;
    });

    private void SurfaceExited(object sender, PointerRoutedEventArgs args) => Guard(() =>
    {
        Require(_stage == 4);
        _pointer!.SurfaceExited();
        _stage = 5;
    });

    private void PointerTick()
    {
        switch (_stage)
        {
            case 0 when _mainPage.IsLoaded && _mainPage.PointerRow.ActualHeight > 0:
                _mainSurface = ProbeWindowSurface.Bind(_main, _main.Title);
                _work = _mainSurface.WorkArea;
                Require(_work.Width >= 900 && _work.Height >= 480);
                _mainSurface.SetFrame(new(_work.X + 16, _work.Bottom - 416, 520, 400));
                var owner = _mainSurface.Frame;
                var placement = HoverDetailsPlacement.PlaceBesideWindow(_work.X, _work.Y, _work.Right, _work.Bottom,
                    owner.X, owner.Right, owner.Y, 360, owner.Height);
                Require(placement.HasValue);
                var p = placement!.Value;
                _placed = new(p.X, p.Y, p.Width, p.Height);
                _stage = 1;
                ReceiptWindow("ui_companion_pointer_ready", _main);
                break;
            case 1 when _opens == 1 && _details is { IsReady: true }:
                RevealChecked();
                _stage = 2;
                ReceiptWindow("ui_companion_pointer_opened", _companion!);
                break;
            case 3 when Stopwatch.GetTimestamp() >= _handoffUntil:
                Require(_pointer!.IsOpen && !_pointer.IsHandingOff && _surface is { IsVisible: true, IsWarmupCloaked: false });
                VerifyGeometry();
                Require(!_surface!.IsForeground && ProbeWindowSurface.ForegroundOwnerToken == _foregroundBefore);
                _stage = 4;
                PointerReceipt("ui_companion_pointer_held geometry=EXTERNAL foreground=UNCHANGED");
                break;
            case 5 when !_pointer!.IsOpen:
                Require(_pointer.Complete && _hides == 1 && _surface is { IsVisible: false });
                DetachPointerCallbacks();
                PointerReceipt("ui_companion_pointer_hidden");
                _absent = _surface!.CreateAbsenceCheck(_title);
                _surface = null;
                _expectedClose = true;
                _stage = 6;
                _companion!.Close();
                break;
            case 6 when _closed == 1 && _absent!():
                PointerReceipt("ui_companion_pointer_native_closed");
                _stage = 7;
                _main.Close();
                break;
        }
    }

    private static void PointerReceipt(string receipt)
    {
        Console.WriteLine(receipt);
        Console.Out.Flush();
    }

    private static void ReceiptWindow(string receipt, Microsoft.UI.Xaml.Window window)
    {
#if WINDOWS
        Console.WriteLine($"{receipt} hwnd={WinRT.Interop.WindowNative.GetWindowHandle(window).ToInt64()}");
#else
        Console.WriteLine(receipt);
#endif
        Console.Out.Flush();
    }

    private void DetachPointerCallbacks()
    {
        _mainPage.PointerRow.PointerEntered -= RowEntered;
        _mainPage.PointerRow.PointerExited -= RowExited;
        if (_details is null) return;
        _details.PointerRoot.PointerEntered -= SurfaceEntered;
        _details.PointerRoot.PointerExited -= SurfaceExited;
    }
}
