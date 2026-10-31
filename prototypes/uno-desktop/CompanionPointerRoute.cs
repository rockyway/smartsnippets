using System;
using SmartSnippets.Presentation.Services;

namespace SmartSnippets.UnoProbe;

/// <summary>Shared by the native XAML callbacks and portable tests. No synthetic policy entry.</summary>
internal sealed class CompanionPointerRoute : IDisposable
{
    private readonly HoverDetailsCoordinator _policy;
    private readonly Action<string> _receipt;
    private readonly object _row = new();
    internal int RowEnters { get; private set; }
    internal int RowExits { get; private set; }
    internal int SurfaceEnters { get; private set; }
    internal int SurfaceExits { get; private set; }
    internal bool IsOpen => _policy.IsOpen;
    internal bool IsHandingOff => _policy.IsHandingOff;

    internal CompanionPointerRoute(Action<TimeSpan, Action> schedule, Action show, Action hide, Action<string> receipt)
    {
        _receipt = receipt;
        _policy = new HoverDetailsCoordinator(schedule, show, hide);
    }

    internal void RowEntered()
    {
        Require(++RowEnters == 1 && SurfaceEnters == 0);
        _receipt("ui_companion_pointer_row_entered");
        _policy.Enter(_row);
    }

    internal void RowExited()
    {
        Require(RowEnters == 1 && ++RowExits == 1 && IsOpen);
        _policy.TargetExited();
        _policy.OwnerExited();
        _receipt("ui_companion_pointer_row_exited");
    }

    internal void SurfaceEntered()
    {
        Require(RowExits == 1 && ++SurfaceEnters == 1 && IsOpen && IsHandingOff);
        _policy.SurfaceEntered();
        _receipt("ui_companion_pointer_surface_entered");
    }

    internal void SurfaceExited()
    {
        Require(SurfaceEnters == 1 && ++SurfaceExits == 1 && IsOpen);
        _policy.SurfaceExited();
        _receipt("ui_companion_pointer_surface_exited");
    }

    internal bool Complete => RowEnters == 1 && RowExits == 1 && SurfaceEnters == 1 && SurfaceExits == 1 && !IsOpen;
    public void Dispose() => _policy.Dispose();
    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException("Native pointer sequence failed.");
    }
}
