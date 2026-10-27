using System.Drawing;
using SmartSnippets.Core.Logging;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.WindowsAndMessaging;

namespace SmartSnippets.App.Services;

internal sealed unsafe class TrayIcon : IDisposable
{
    private const uint CallbackMessage = PInvoke.WM_APP + 1;
    private const uint IconId = 1;
    private const nuint CommandOpen = 1;
    private const nuint CommandSettings = 2;
    private const nuint CommandRestartAdmin = 3;
    private const nuint CommandDiagnostics = 4;
    private const nuint CommandExit = 5;
    private const string Tooltip = "Smart Snippets — running in background";

    private readonly MessageWindow _window;
    private readonly Action _onOpen;
    private readonly Action _onSettings;
    private readonly Action _onRestartAdmin;
    private readonly Action _onDiagnostics;
    private readonly Action _onExit;
    private readonly bool _showRestartAdmin;
    private readonly uint _taskbarCreatedMessage;
    private bool _added;

    public TrayIcon(
        MessageWindow window,
        Action onOpen,
        Action onSettings,
        Action onRestartAdmin,
        Action onDiagnostics,
        Action onExit,
        bool showRestartAdmin)
    {
        _window = window;
        _onOpen = onOpen;
        _onSettings = onSettings;
        _onRestartAdmin = onRestartAdmin;
        _onDiagnostics = onDiagnostics;
        _onExit = onExit;
        _showRestartAdmin = showRestartAdmin;

        fixed (char* name = "TaskbarCreated")
        {
            _taskbarCreatedMessage = PInvoke.RegisterWindowMessage(name);
        }

        // An elevated process drops messages from the (medium-integrity) shell unless they are explicitly allowed.
        if (_taskbarCreatedMessage != 0)
        {
            PInvoke.ChangeWindowMessageFilterEx(_window.Handle, _taskbarCreatedMessage, WINDOW_MESSAGE_FILTER_ACTION.MSGFLT_ALLOW, null);
        }

        _window.MessageReceived += OnMessage;
    }

    public bool Add()
    {
        var data = CreateData();
        data.uFlags = NOTIFY_ICON_DATA_FLAGS.NIF_MESSAGE | NOTIFY_ICON_DATA_FLAGS.NIF_ICON | NOTIFY_ICON_DATA_FLAGS.NIF_TIP;
        data.uCallbackMessage = CallbackMessage;
        data.hIcon = PInvoke.LoadIcon(HINSTANCE.Null, PInvoke.IDI_APPLICATION);
        data.szTip = Tooltip;
        _added = PInvoke.Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_ADD, in data);
        AppLog.Info(_added ? "tray_added" : "tray_add_failed");
        return _added;
    }

    /// <summary>Shows a tray balloon notification (title up to 63 and text up to 255 characters).</summary>
    public void ShowBalloon(string title, string text)
    {
        if (!_added)
        {
            return;
        }

        var data = CreateData();
        data.uFlags = NOTIFY_ICON_DATA_FLAGS.NIF_INFO;
        data.szInfoTitle = Truncate(title, 63);
        data.szInfo = Truncate(text, 255);
        data.dwInfoFlags = NOTIFY_ICON_INFOTIP_FLAGS.NIIF_INFO;
        if (!PInvoke.Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_MODIFY, in data))
        {
            AppLog.Info("tray_balloon_failed");
        }
    }

    public void Dispose()
    {
        _window.MessageReceived -= OnMessage;
        if (_added)
        {
            var data = CreateData();
            PInvoke.Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_DELETE, in data);
            _added = false;
        }
    }

    private static string Truncate(string value, int max) => value.Length <= max ? value : value[..max];

    private NOTIFYICONDATAW CreateData()
    {
        var data = default(NOTIFYICONDATAW);
        data.cbSize = (uint)sizeof(NOTIFYICONDATAW);
        data.hWnd = _window.Handle;
        data.uID = IconId;
        return data;
    }

    private void OnMessage(uint message, nuint wParam, nint lParam)
    {
        if (_taskbarCreatedMessage != 0 && message == _taskbarCreatedMessage)
        {
            // Explorer restarted: its tray no longer holds our icon.
            _added = false;
            if (Add())
            {
                AppLog.Info("tray_readded");
            }

            return;
        }

        if (message != CallbackMessage)
        {
            return;
        }

        switch ((uint)lParam & 0xFFFF)
        {
            case PInvoke.WM_LBUTTONUP:
                _onOpen();
                break;
            case PInvoke.WM_RBUTTONUP:
            case PInvoke.WM_CONTEXTMENU:
                ShowMenu();
                break;
        }
    }

    private void ShowMenu()
    {
        Point cursor;
        PInvoke.GetCursorPos(&cursor);

        var menu = PInvoke.CreatePopupMenu();
        if (menu.IsNull)
        {
            return;
        }

        try
        {
            fixed (char* open = "Open Smart Snippets")
            fixed (char* settings = "Settings")
            fixed (char* restart = "Restart as administrator")
            fixed (char* diagnostics = "Diagnostics")
            fixed (char* exit = "Exit")
            {
                PInvoke.AppendMenu(menu, MENU_ITEM_FLAGS.MF_STRING, CommandOpen, open);
                PInvoke.AppendMenu(menu, MENU_ITEM_FLAGS.MF_STRING, CommandSettings, settings);
                PInvoke.AppendMenu(menu, MENU_ITEM_FLAGS.MF_SEPARATOR, 0, null);
                if (_showRestartAdmin)
                {
                    PInvoke.AppendMenu(menu, MENU_ITEM_FLAGS.MF_STRING, CommandRestartAdmin, restart);
                }

                PInvoke.AppendMenu(menu, MENU_ITEM_FLAGS.MF_STRING, CommandDiagnostics, diagnostics);
                PInvoke.AppendMenu(menu, MENU_ITEM_FLAGS.MF_SEPARATOR, 0, null);
                PInvoke.AppendMenu(menu, MENU_ITEM_FLAGS.MF_STRING, CommandExit, exit);
            }

            // Documented quirk: the menu only dismisses on outside click if our window is foreground.
            PInvoke.SetForegroundWindow(_window.Handle);
            var flags = (uint)(TRACK_POPUP_MENU_FLAGS.TPM_RIGHTBUTTON | TRACK_POPUP_MENU_FLAGS.TPM_RETURNCMD
                | TRACK_POPUP_MENU_FLAGS.TPM_BOTTOMALIGN | TRACK_POPUP_MENU_FLAGS.TPM_LEFTALIGN);
            var command = (nuint)PInvoke.TrackPopupMenuEx(menu, flags, cursor.X, cursor.Y, _window.Handle, null).Value;
            PInvoke.PostMessage(_window.Handle, PInvoke.WM_NULL, 0, 0);

            switch (command)
            {
                case CommandOpen:
                    _onOpen();
                    break;
                case CommandSettings:
                    _onSettings();
                    break;
                case CommandRestartAdmin:
                    _onRestartAdmin();
                    break;
                case CommandDiagnostics:
                    _onDiagnostics();
                    break;
                case CommandExit:
                    _onExit();
                    break;
            }
        }
        finally
        {
            PInvoke.DestroyMenu(menu);
        }
    }
}
