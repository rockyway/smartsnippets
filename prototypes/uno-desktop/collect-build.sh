#!/usr/bin/env bash
# Apple Silicon, build/sign/inspect only. Never launches the Uno app.
set -euo pipefail
if [[ "$(uname -s)" != Darwin || "$(uname -m)" != arm64 ]]; then
  printf 'This collector requires an Apple Silicon Mac.\n' >&2
  exit 1
fi
base=$(cd "$(dirname "$0")" && pwd)
cd "$base"
umask 077
private=$(mktemp -d /tmp/ss-uno018-native.XXXXXX)
run=${private##*.}
printf 'Private build evidence token: %s\n' "$run"
trap 'printf "Build stage failed; inspect private evidence token: %s\n" "$run" >&2' ERR
flat="$base/bin/probe/mac-$run"
bundle="$base/bin/probe/bundle-$run/SmartSnippets.UnoProbe.app"
common=(-p:ProbePatchedMacOSRuntime=true -p:NoWarn=1701 -p:SuppressTrimAnalysisWarnings=false -p:TrimmerSingleWarn=false)
python3 vendor/prepare_source.py --verify --output vendor/obj/upstream > "$private/source-verify.log"
dotnet restore SmartSnippets.UnoProbe.csproj -r osx-arm64 -p:Configuration=Release "${common[@]}" > "$private/restore.log" 2>&1
names=TargetFramework,RuntimeIdentifier,PublishAot,PublishTrimmed,PublishSingleFile,SuppressTrimAnalysisWarnings,TrimmerSingleWarn,NoWarn,EnableTrimAnalyzer,EnableAotAnalyzer,TreatWarningsAsErrors,NETCoreSdkVersion,MSBuildVersion,UnoVersion,SupportedOSPlatformVersion,TargetPlatformMinVersion,TargetPlatformVersion,ProbePatchedMacOSRuntime
dotnet msbuild SmartSnippets.UnoProbe.csproj -p:TargetFramework=net10.0-desktop -p:RuntimeIdentifier=osx-arm64 -p:Configuration=Release -p:_IsPublishing=true "${common[@]}" "-getProperty:$names" > "$private/properties.json" 2>&1
dotnet clean SmartSnippets.UnoProbe.csproj -f net10.0-desktop -r osx-arm64 -c Release "${common[@]}" > "$private/clean.log" 2>&1
set +e
dotnet publish SmartSnippets.UnoProbe.csproj -f net10.0-desktop -r osx-arm64 -c Release --no-restore "${common[@]}" -o "$flat" > "$private/publish.log" 2>&1
result=$?
set -e
printf 'probe_publish_exit=%s\n' "$result" >> "$private/publish.log"
if [[ "$result" -ne 0 ]]; then
  printf 'Native publish failed; private evidence token: %s\n' "$run" >&2
  exit 1
fi
python3 build_native_bundle.py --publish-dir "$flat" \
  --icon-dir obj/Release/net10.0-desktop/osx-arm64/unoresizetizer/r/Assets.xcassets/icon.appiconset \
  --source-root vendor/obj/upstream --output "$bundle" > "$private/bundle.log" 2>&1
python3 verify_evidence.py --properties "$private/properties.json" --log "$private/publish.log" \
  --artifact-dir "$bundle" --executable "$bundle/Contents/MacOS/SmartSnippets.UnoProbe" --rid osx-arm64
printf 'Native bundle built; private evidence token: %s; no app launch.\n' "$run"
