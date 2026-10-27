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
    private const nuint CommandExit = 2;

    private readonly MessageWindow _window;
    private readonly Action _onOpen;
    private readonly Action _onExit;
    private bool _added;

    public TrayIcon(MessageWindow window, Action onOpen, Action onExit)
    {
        _window = window;
        _onOpen = onOpen;
        _onExit = onExit;
        _window.MessageReceived += OnMessage;
    }

    public bool Add()
    {
        var data = CreateData();
        data.uFlags = NOTIFY_ICON_DATA_FLAGS.NIF_MESSAGE | NOTIFY_ICON_DATA_FLAGS.NIF_ICON | NOTIFY_ICON_DATA_FLAGS.NIF_TIP;
        data.uCallbackMessage = CallbackMessage;
        data.hIcon = PInvoke.LoadIcon(HINSTANCE.Null, PInvoke.IDI_APPLICATION);
        data.szTip = "SmartSnippets";
        _added = PInvoke.Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_ADD, in data);
        AppLog.Info(_added ? "tray_added" : "tray_add_failed");
        return _added;
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
            fixed (char* open = "Open")
            fixed (char* exit = "Exit")
            {
                PInvoke.AppendMenu(menu, MENU_ITEM_FLAGS.MF_STRING, CommandOpen, open);
                PInvoke.AppendMenu(menu, MENU_ITEM_FLAGS.MF_STRING, CommandExit, exit);
            }

            // Documented quirk: the menu only dismisses on outside click if our window is foreground.
            PInvoke.SetForegroundWindow(_window.Handle);
            var flags = (uint)(TRACK_POPUP_MENU_FLAGS.TPM_RIGHTBUTTON | TRACK_POPUP_MENU_FLAGS.TPM_RETURNCMD
                | TRACK_POPUP_MENU_FLAGS.TPM_BOTTOMALIGN | TRACK_POPUP_MENU_FLAGS.TPM_LEFTALIGN);
            var command = PInvoke.TrackPopupMenuEx(menu, flags, cursor.X, cursor.Y, _window.Handle, null).Value;
            PInvoke.PostMessage(_window.Handle, PInvoke.WM_NULL, 0, 0);

            if (command == (int)CommandOpen)
            {
                _onOpen();
            }
            else if (command == (int)CommandExit)
            {
                _onExit();
            }
        }
        finally
        {
            PInvoke.DestroyMenu(menu);
        }
    }
}
