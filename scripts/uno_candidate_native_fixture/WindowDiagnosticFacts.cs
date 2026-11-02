namespace SmartSnippets.Uno;

// Raw class names/handles are reduced here and never returned to a logger.
internal readonly record struct WindowDiagnosticFacts(string ClassToken, bool Visible, bool Toolwindow,
    bool Noactivate, bool HasOwner, bool OwnerIsA, bool OwnerIsB, bool IsMessageOnly,
    bool SameThreadAsA, bool Cloaked, bool CloakedKnown)
{
    internal static string Classify(ReadOnlySpan<char> name) => name switch
    {
        "IME" => "ime_default", "MSCTFIME UI" => "msctfime_ui",
        "OleMainThreadWndClass" => "ole_main_thread", "CicMarshalWndClass" => "cicmarshal",
        "UserAdapterWindowClass" => "user_adapter", "tooltips_class32" => "tooltip",
        "#32770" => "dialog_32770", "#32768" => "menu_32768",
        _ => IsTargetClass(name) ? "target018_class" : "other"
    };
    private static bool IsTargetClass(ReadOnlySpan<char> name) => name.StartsWith("SS_Target018_") &&
        name.Length == 45 && Guid.TryParseExact(name[13..], "N", out _);
    internal static WindowDiagnosticFacts Capture(nint window, nint a, nint b,
        Func<nint, string> className, Func<nint, bool> visible, Func<nint, long> extendedStyle,
        Func<nint, nint> owner, Func<nint, bool> messageOnly, Func<nint, uint> thread,
        Func<nint, int?> cloak)
    {
        var style = extendedStyle(window);
        var owned = owner(window);
        var tid = thread(window);
        var cloaked = cloak(window);
        return new(Classify(className(window)), visible(window), (style & 0x80) != 0,
            (style & 0x08000000) != 0, owned != 0, a != 0 && owned == a, b != 0 && owned == b,
            messageOnly(window), tid != 0 && a != 0 && tid == thread(a), cloaked.GetValueOrDefault() != 0,
            cloaked.HasValue);
    }
    internal string Fields()
    {
        // Defend even a wrong/test adapter: no free-form ClassToken can escape.
        var token = ClassToken is "ime_default" or "msctfime_ui" or "ole_main_thread" or "cicmarshal" or
            "user_adapter" or "tooltip" or "dialog_32770" or "menu_32768" or "target018_class" ? ClassToken : "other";
        return $"class_token={token} visible={Visible} toolwindow={Toolwindow} noactivate={Noactivate} has_owner={HasOwner} owner_is_a={OwnerIsA} owner_is_b={OwnerIsB} is_message_only={IsMessageOnly} same_thread_as_a={SameThreadAsA} cloaked={Cloaked} cloaked_known={CloakedKnown}";
    }
}
