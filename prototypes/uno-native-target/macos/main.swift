import AppKit
import Darwin

// This is an owned native target, not the product app or a keyboard extension.
// No clipboard, event injection, database, startup or permission API is called.
let arguments = CommandLine.arguments
guard arguments.count == 3,
      arguments[1] == "--authorized-probe-target",
      let runID = UUID(uuidString: arguments[2]) else { exit(2) }

@MainActor
final class FixtureDelegate: NSObject, NSApplicationDelegate {
    private var windows: [NSWindow] = []
    private var editors: [NSTextView] = []
    private let runID: UUID

    init(runID: UUID) {
        self.runID = runID
        super.init()
    }

    func applicationDidFinishLaunching(_ notification: Notification) {
        guard let screen = NSScreen.screens.sorted(by: {
            if $0.frame.minX != $1.frame.minX { return $0.frame.minX < $1.frame.minX }
            return $0.frame.minY < $1.frame.minY // AppKit coordinates increase upward.
        }).first else { fail() }
        let area = screen.visibleFrame
        guard area.width >= 940, area.height >= 400 else { fail() }
        configureMenus()
        for index in 0..<2 {
            let frame = NSRect(x: area.minX + 16 + CGFloat(index * 460),
                               y: area.maxY - 366, width: 440, height: 350)
            let window = NSWindow(contentRect: frame,
                                  styleMask: [.titled, .closable, .miniaturizable, .resizable],
                                  backing: .buffered, defer: false)
            window.setFrame(frame, display: false)
            window.isReleasedWhenClosed = false
            window.title = "SS-Target018-\(index == 0 ? "A" : "B")-\(runID.uuidString)"
            window.setAccessibilityIdentifier(index == 0 ? "SSFixtureWindowA" : "SSFixtureWindowB")
            let scroll = NSScrollView(frame: NSRect(origin: .zero, size: window.contentLayoutRect.size))
            scroll.autoresizingMask = [.width, .height]
            scroll.hasVerticalScroller = true
            let editor = NSTextView(frame: scroll.bounds)
            editor.isRichText = false
            editor.isEditable = true
            editor.isSelectable = true
            editor.isAutomaticSpellingCorrectionEnabled = false
            editor.isContinuousSpellCheckingEnabled = false
            editor.isAutomaticTextReplacementEnabled = false
            editor.isAutomaticQuoteSubstitutionEnabled = false
            editor.isAutomaticDashSubstitutionEnabled = false
            editor.isAutomaticLinkDetectionEnabled = false
            editor.isAutomaticDataDetectionEnabled = false
            editor.isAutomaticTextCompletionEnabled = false
            editor.smartInsertDeleteEnabled = false
            editor.autoresizingMask = [.width]
            editor.minSize = NSSize(width: 0, height: 0)
            editor.maxSize = NSSize(width: CGFloat.greatestFiniteMagnitude, height: CGFloat.greatestFiniteMagnitude)
            editor.isVerticallyResizable = true
            editor.isHorizontallyResizable = false
            editor.textContainer?.widthTracksTextView = true
            editor.setAccessibilityIdentifier(index == 0 ? "SSFixtureEditorA" : "SSFixtureEditorB")
            editor.string = ""
            scroll.documentView = editor
            window.contentView = scroll
            window.makeFirstResponder(editor)
            windows.append(window)
            editors.append(editor)
            window.orderFront(nil) // Do not activate the target application during launch.
        }
        print("target_ready pid=\(ProcessInfo.processInfo.processIdentifier) a=\(windows[0].windowNumber) b=\(windows[1].windowNumber)")
        fflush(nil)
        startCommands()
    }

    private func startCommands() {
        // Only stdin is read off-thread. All AppKit state remains main-actor-only.
        DispatchQueue.global(qos: .utility).async { [weak self] in
            while let command = readLine() {
                guard ["close_a", "close_b", "empty_a", "empty_b", "focus_a", "focus_b"].contains(command) else {
                    FileHandle.standardError.write(Data("target_command_refused\n".utf8))
                    continue
                }
                let completed = DispatchSemaphore(value: 0)
                DispatchQueue.main.async { [weak self] in
                    guard let self else { completed.signal(); return }
                    self.executeCommand(command, completed: completed)
                }
                completed.wait() // Serialize commands; never block the main actor.
            }
        }
    }

    private func executeCommand(_ command: String, completed: DispatchSemaphore) {
        Task { @MainActor in
            defer { completed.signal() }
            let index = command.hasSuffix("_a") ? 0 : 1
            let window = windows[index]
            var ok = window.isVisible
            if ok {
                if command.hasPrefix("focus_") {
                    NSApplication.shared.activate(ignoringOtherApps: true)
                    window.makeKeyAndOrderFront(nil)
                    ok = false
                    let deadline = ProcessInfo.processInfo.systemUptime + 2.0
                    while ProcessInfo.processInfo.systemUptime < deadline {
                        if NSApplication.shared.isActive {
                            window.makeKeyAndOrderFront(nil)
                            _ = window.makeFirstResponder(editors[index])
                        }
                        if window.isVisible && NSApplication.shared.isActive && window.isKeyWindow && window.firstResponder === editors[index] {
                            ok = true
                            break
                        }
                        // Yield to AppKit/WindowServer; do not spin or nest a runloop.
                        try? await Task.sleep(nanoseconds: 20_000_000)
                    }
                } else if command.hasPrefix("close_") {
                    window.close()
                    ok = !window.isVisible
                } else {
                    ok = editors[index].string.isEmpty // Never return editor content.
                }
            }
            print("target_command name=\(command) ok=\(ok ? 1 : 0)")
            fflush(nil)
        }
    }

    func applicationShouldTerminateAfterLastWindowClosed(_ sender: NSApplication) -> Bool { true }

    private func configureMenus() {
        let menu = NSMenu()
        let appItem = NSMenuItem()
        menu.addItem(appItem)
        let appMenu = NSMenu()
        appMenu.addItem(withTitle: "Quit fixture", action: #selector(NSApplication.terminate(_:)), keyEquivalent: "q")
        appItem.submenu = appMenu
        let editItem = NSMenuItem()
        menu.addItem(editItem)
        let editMenu = NSMenu(title: "Edit")
        editMenu.addItem(withTitle: "Paste", action: #selector(NSText.paste(_:)), keyEquivalent: "v")
        editItem.submenu = editMenu
        NSApplication.shared.mainMenu = menu
    }

    private func fail() -> Never {
        FileHandle.standardError.write(Data("target_setup_failed\n".utf8))
        exit(1)
    }
}

let application = NSApplication.shared
application.setActivationPolicy(.regular)
let delegate = FixtureDelegate(runID: runID)
application.delegate = delegate
application.run()
