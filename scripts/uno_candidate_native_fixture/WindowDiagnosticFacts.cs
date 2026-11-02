namespace SmartSnippets.Uno;

// Raw class names/handles are reduced here and never returned to a logger.
internal readonly record struct WindowDiagnosticFacts(string ClassToken, bool Visible, bool Toolwindow,
    bool Noactivate, bool HasOwner, bool OwnerIsA, bool OwnerIsB, bool IsMessageOnly,
    bool SameThreadAsA, bool Cloaked, bool CloakedKnown)
{
    internal static string ClassSha8(string name)
    {
        // Preserve UTF-16 code units, including unpaired surrogates (same as Python surrogatepass).
        var bytes = new byte[name.Length * 2];
        for (var i = 0; i < name.Length; i++)
        {
            bytes[i * 2] = (byte)name[i];
            bytes[i * 2 + 1] = (byte)(name[i] >> 8);
        }
        return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes)).ToLowerInvariant()[..8];
    }
    internal static string Classify(ReadOnlySpan<char> name) => name switch
    {
        "IME" => "ime_default",
        "MSCTFIME UI" => "msctfime_ui",
        "OleMainThreadWndClass" => "ole_main_thread",
        "CicMarshalWndClass" => "cicmarshal",
        "UserAdapterWindowClass" => "user_adapter",
        "tooltips_class32" => "tooltip",
        "#32770" => "dialog_32770",
        "#32768" => "menu_32768",
        "Edit" => "system_0",
        "Static" => "system_1",
        "Button" => "system_2",
        "ComboBox" => "system_3",
        "ComboLBox" => "system_4",
        "ListBox" => "system_5",
        "ScrollBar" => "system_6",
        "ConsoleWindowClass" => "system_7",
        "PseudoConsoleWindow" => "system_8",
        "Ghost" => "system_9",
        "SysShadow" => "system_10",
        "#32769" => "system_11",
        "#32771" => "system_12",
        "DDEMLEvent" => "system_13",
        "DDEMLMom" => "system_14",
        "GDI+ Hook Window Class" => "system_15",
        "GDI+ Window" => "system_16",
        "Windows.UI.Core.CoreWindow" => "system_17",
        "ApplicationFrameWindow" => "system_18",
        "Xaml_WindowedPopupClass" => "system_19",
        "ThumbnailDeviceHelperWnd" => "system_20",
        "MSCTFIME Composition" => "system_21",
        "TaskManagerWindow" => "system_22",
        "CicLoaderWndClass" => "system_23",
        "IPTip_Main_Window" => "system_24",
        "Shell_TrayWnd" => "system_25",
        "WorkerW" => "system_26",
        "Progman" => "system_27",
        "Message" => "uia_message",
        _ => IsTargetClass(name) ? "target018_class" :
            name.StartsWith(".NET-BroadcastEventWindow") ? "dotnet_broadcast" :
            name.StartsWith("Microsoft.UI.Content.") ? "microsoft_content" :
            name.StartsWith("SoftKeyboard") ? "soft_keyboard" : "other"
    };
    internal string ClassSha { get; init; } = ClassSha8("");
    internal bool Sampled { get; init; }
    internal bool WsPopup { get; init; }
    internal bool WsCaption { get; init; }
    internal bool WsChild { get; init; }
    internal bool WsDisabled { get; init; }
    internal bool WsExTopmost { get; init; }
    internal bool WsExLayered { get; init; }
    internal bool WsExTransparent { get; init; }
    internal bool WsExAppwindow { get; init; }
    internal bool ZeroArea { get; init; }
    internal bool Offscreen { get; init; }
    internal bool EqualsARect { get; init; }
    internal bool OnBottomLeftWork { get; init; }
    internal bool TitleNonempty { get; init; }
    internal WindowDiagnosticFacts WithWindowDetails(long style, long extendedStyle, bool titleNonempty,
        bool? zeroArea, bool? offscreen, bool? equalsARect, bool? onBottomLeftWork) => this with
    {
        WsPopup = (style & 0x80000000) != 0, WsCaption = (style & 0x00c00000) != 0,
        WsChild = (style & 0x40000000) != 0, WsDisabled = (style & 0x08000000) != 0,
        WsExTopmost = (extendedStyle & 8) != 0, WsExLayered = (extendedStyle & 0x80000) != 0,
        WsExTransparent = (extendedStyle & 0x20) != 0, WsExAppwindow = (extendedStyle & 0x40000) != 0,
        ZeroArea = zeroArea.GetValueOrDefault(), Offscreen = offscreen.GetValueOrDefault(),
        EqualsARect = equalsARect.GetValueOrDefault(), OnBottomLeftWork = onBottomLeftWork.GetValueOrDefault(),
        TitleNonempty = titleNonempty
    };
    private static bool IsTargetClass(ReadOnlySpan<char> name) => name.StartsWith("SS_Target018_") &&
        name.Length == 45 && Guid.TryParseExact(name[13..], "N", out _);
    internal static WindowDiagnosticFacts Capture(nint window, nint a, nint b,
        Func<nint, string> className, Func<nint, bool> visible, Func<nint, long> extendedStyle,
        Func<nint, nint> owner, Func<nint, bool> messageOnly, Func<nint, uint> thread,
        Func<nint, int?> cloak)
    {
        var name = className(window);
        var style = extendedStyle(window);
        var owned = owner(window);
        var tid = thread(window);
        var cloaked = cloak(window);
        return new(Classify(name), visible(window), (style & 0x80) != 0,
            (style & 0x08000000) != 0, owned != 0, a != 0 && owned == a, b != 0 && owned == b,
            messageOnly(window), tid != 0 && a != 0 && tid == thread(a), cloaked.GetValueOrDefault() != 0,
            cloaked.HasValue) { ClassSha = ClassSha8(name), Sampled = a != 0 && b != 0 };
    }
    private string SafeSha() => ClassSha is { Length: 8 } && ClassSha.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f') ? ClassSha : ClassSha8("");
    internal string Json()
    {
        // Only allowlisted tokens, hex and booleans; never free-form strings.
        var fields = Fields().Split(' ');
        var pairs = fields.Select(f => f.Split('=')).Where(f => f.Length == 2)
            .Select(f => "\"" + f[0] + "\":" + (f[0] is "class_token" or "class_sha8" ? "\"" + f[1] + "\"" : f[1].ToLowerInvariant()));
        return "{" + string.Join(",", pairs) + ",\"sampled\":" + Sampled.ToString().ToLowerInvariant() + "}";
    }
    internal string Fields()
    {
        // Defend even a wrong/test adapter: no free-form ClassToken can escape.
        var token = ClassToken is "ime_default" or "msctfime_ui" or "ole_main_thread" or "cicmarshal" or "user_adapter" or "tooltip" or "dialog_32770" or "menu_32768" or "system_0" or "system_1" or "system_2" or "system_3" or "system_4" or "system_5" or "system_6" or "system_7" or "system_8" or "system_9" or "system_10" or "system_11" or "system_12" or "system_13" or "system_14" or "system_15" or "system_16" or "system_17" or "system_18" or "system_19" or "system_20" or "system_21" or "system_22" or "system_23" or "system_24" or "system_25" or "system_26" or "system_27" or "uia_message" or "dotnet_broadcast" or "microsoft_content" or "soft_keyboard" or "target018_class" ? ClassToken : "other";
        return $"class_token={token} visible={Visible} toolwindow={Toolwindow} noactivate={Noactivate} has_owner={HasOwner} owner_is_a={OwnerIsA} owner_is_b={OwnerIsB} is_message_only={IsMessageOnly} same_thread_as_a={SameThreadAsA} cloaked={Cloaked} cloaked_known={CloakedKnown} class_sha8={SafeSha()} ws_popup={WsPopup} ws_caption={WsCaption} ws_child={WsChild} foreground_was_child={WsChild} ws_disabled={WsDisabled} ws_ex_topmost={WsExTopmost} ws_ex_layered={WsExLayered} ws_ex_transparent={WsExTransparent} ws_ex_appwindow={WsExAppwindow} zero_area={ZeroArea} offscreen={Offscreen} equals_a_rect={EqualsARect} on_bottom_left_work={OnBottomLeftWork} title_nonempty={TitleNonempty}";
    }
}
