"""Package an existing Native AOT executable, never a CoreCLR launcher.

Mac-only, build/sign/inspect operations; does not launch the app or change TCC.
"""
import argparse
from pathlib import Path
import plistlib
import shutil
import subprocess
import sys
import tempfile
from verify_evidence import inspect_executable

ENTRY = "SmartSnippets.UnoProbe"


def bundle_info():
    # Current .NET 10 supported macOS baseline, not the lower Mach-O link floor.
    # Source: dotnet/core supported-os.md at 44927bc821d37f596e317c4316335632bd38c79b.
    return {"CFBundleIdentifier": "org.smartsnippets.feasibility018",
            "CFBundleExecutable": ENTRY, "CFBundleName": "Smart Snippets Feasibility Probe",
            "CFBundleDisplayName": "Smart Snippets Feasibility Probe", "CFBundlePackageType": "APPL",
            "CFBundleShortVersionString": "0.1.0", "CFBundleVersion": "1",
            "CFBundleIconFile": "ProbeIcon.icns", "NSHighResolutionCapable": True,
            "NSPrincipalClass": "NSApplication", "LSMinimumSystemVersion": "15.0"}


def run(args):
    result = subprocess.run(args, capture_output=True, text=True, timeout=60)
    if result.returncode:
        raise ValueError("Native packaging tool failed: " + Path(args[0]).name)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--publish-dir", required=True, type=Path)
    parser.add_argument("--icon-dir", required=True, type=Path)
    parser.add_argument("--source-root", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    args = parser.parse_args()
    if sys.platform != "darwin":
        raise ValueError("Native app packaging requires macOS")
    publish, output = args.publish_dir.resolve(), args.output.resolve()
    if output.exists() or output.is_relative_to(publish) or publish.is_relative_to(output):
        raise ValueError("Use a fresh bundle outside the publish directory")
    if not (publish / ENTRY).is_file() or (publish / (ENTRY + ".dll")).exists():
        raise ValueError("A native entry is required, not a managed apphost")
    if inspect_executable((publish / ENTRY).read_bytes(), "osx-arm64"):
        raise ValueError("Expected a native arm64 entry, not an apphost")
    if any(path.is_symlink() for path in publish.rglob("*")):
        raise ValueError("Unexpected publish symlink")
    if any(path.name.lower() in {"libcoreclr.dylib", "libclrjit.dylib", "libmonosgen-2.0.dylib"}
           for path in publish.rglob("*")):
        raise ValueError("JIT runtime payload is not permitted")
    contents = output / "Contents"
    macos, resources = contents / "MacOS", contents / "Resources"
    macos.mkdir(parents=True)
    resources.mkdir()
    runtime_resources = resources / "Runtime"
    runtime_resources.mkdir()
    # Only native code belongs in Contents/MacOS. Resource links preserve Uno's
    # AppContext.BaseDirectory-relative asset/marker lookups without asking
    # codesign to treat .uprimarker/font/image files as executable subcomponents.
    for source in publish.iterdir():
        if source.suffix in (".pdb", ".dbg", ".dSYM"):
            continue
        if source.is_file() and (source.name == ENTRY or source.suffix == ".dylib"):
            shutil.copy2(source, macos / source.name)
        else:
            destination = runtime_resources / source.name
            if source.is_dir():
                shutil.copytree(source, destination)
            else:
                shutil.copy2(source, destination)
            (macos / source.name).symlink_to(Path("../Resources/Runtime") / source.name)
    # SDK Extras 6.3.1's app bundler links a CoreCLR launcher even for Native AOT.
    # Use Apple's conventional bundle around the already-compiled native entry.
    with tempfile.TemporaryDirectory(prefix="ss-uno018-icon-") as temporary:
        iconset = Path(temporary) / "ProbeIcon.iconset"
        iconset.mkdir()
        for size in (16, 32, 128, 256, 512):
            for scale in (1, 2):
                source = args.icon_dir / f"icon{size}x{size}@{scale}x.png"
                suffix = "@2x" if scale == 2 else ""
                shutil.copyfile(source, iconset / f"icon_{size}x{size}{suffix}.png")
        run(["/usr/bin/iconutil", "-c", "icns", str(iconset), "-o", str(resources / "ProbeIcon.icns")])
    info = bundle_info()
    with (contents / "Info.plist").open("wb") as stream:
        plistlib.dump(info, stream)
    notices = resources / "ThirdPartyNotices"
    notices.mkdir()
    for name in ("License.md", "THIRD-PARTY-NOTICES.md"):
        shutil.copyfile(args.source_root / name, notices / name)
    (notices / "ProbeCorrection.txt").write_text(
        "Uno 6.7.135, source commit da217df2312e9e39676880f9cb4b8d519cc297e0.\n"
        "Probe-only managed runtime rebuild: two JSON calls use generated type contracts.\n"
        "Native host and callbacks retained; not an upstream release or notarized product.\n", encoding="utf-8")
    for path in sorted(macos.rglob("*.dylib")):
        run(["/usr/bin/codesign", "--force", "--sign", "-", "--timestamp=none", str(path)])
    run(["/usr/bin/codesign", "--force", "--sign", "-", "--timestamp=none", str(output)])
    run(["/usr/bin/codesign", "--verify", "--strict", "--deep", str(output)])
    print("native_aot_bundle_created adhoc_signature_verified runtime=NOT_RUN")
    return 0


if __name__ == "__main__":
    try:
        sys.exit(main())
    except (OSError, ValueError, subprocess.SubprocessError):
        print("Native bundle packaging failed; inspect private build evidence.", file=sys.stderr)
        sys.exit(1)
