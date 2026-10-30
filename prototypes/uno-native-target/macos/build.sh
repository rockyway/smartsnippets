#!/usr/bin/env bash
# Build/sign only; never launches a target, sends input or accesses clipboard.
set -euo pipefail
[[ "$(uname -s)" == Darwin && "$(uname -m)" == arm64 ]] || { printf 'Apple Silicon required.\n' >&2; exit 1; }
base=$(cd "$(dirname "$0")" && pwd)
umask 077
private=$(mktemp -d "${TMPDIR:-/tmp}/ss-target018-build.XXXXXX")
run=${private##*.}
app="$base/bin/target-$run/SmartSnippets.Target018.app"
mkdir -p "$app/Contents/MacOS"
cp "$base/Info.plist" "$app/Contents/Info.plist"
trap 'printf "Target build failed; private evidence token: %s\n" "$run" >&2' ERR
xcrun swiftc "$base/main.swift" -O -swift-version 6 -warnings-as-errors -framework AppKit \
  -target arm64-apple-macos12.0 -o "$app/Contents/MacOS/SmartSnippets.Target018" > "$private/build.log" 2>&1
codesign --force --sign - --timestamp=none "$app" > "$private/sign.log" 2>&1
codesign --verify --strict --deep "$app" > "$private/verify.log" 2>&1
printf 'target_native_arm64_built signature=ADHOC runtime=NOT_RUN evidence_token=%s\n' "$run"
shasum -a 256 "$app/Contents/MacOS/SmartSnippets.Target018" | cut -d' ' -f1
