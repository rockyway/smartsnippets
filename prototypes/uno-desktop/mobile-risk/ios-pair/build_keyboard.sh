#!/bin/bash
set -euo pipefail
cd "$(dirname "$0")"
mkdir -p native/PairKeyboard.appex
sdk="$(xcrun --sdk iphoneos --show-sdk-path)"
xcrun --sdk iphoneos swiftc Keyboard/KeyboardViewController.swift \
  -sdk "$sdk" -target arm64-apple-ios15.0 -parse-as-library \
  -application-extension -warnings-as-errors -emit-executable -module-name PairKeyboard \
  -Xlinker -e -Xlinker _NSExtensionMain \
  -Xlinker -no_adhoc_codesign -o native/PairKeyboard.appex/PairKeyboard
cp Keyboard/Info.plist native/PairKeyboard.appex/Info.plist
