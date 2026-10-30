"""Fetch exact upstream managed source and apply a narrow source-level correction.

Never changes the NuGet cache, native host, published package or production code.
"""
import argparse
import hashlib
import json
from pathlib import Path
import sys
import urllib.request

HERE = Path(__file__).resolve().parent
PREFIX = "src/Uno.UI.Runtime.Skia.MacOS/"


def git_blob_sha1(data):
    return hashlib.sha1(b"blob " + str(len(data)).encode("ascii") + b"\0" + data).hexdigest()


REPLACEMENTS = {
        "UI/Xaml/Controls/WebView/MacOSNativeWebView.WebResourceRequested.cs": (
            "JsonSerializer.Serialize(headers)",
            "JsonSerializer.Serialize(headers, MacOSWebViewJsonContext.Default.DictionaryStringString)"),
        "UI/Xaml/Controls/WebView/MacOSNativeWebView.cs": (
            "JsonSerializer.Serialize(httpRequestMessage.Headers)",
            "JsonSerializer.Serialize(httpRequestMessage.Headers, MacOSWebViewJsonContext.Default.HttpRequestHeaders)"),
}


def patch_source(root):
    changed = []
    for relative, (old, new) in REPLACEMENTS.items():
        path = root / PREFIX / relative
        original = path.read_bytes()
        if original.count(old.encode()) != 1:
            raise ValueError("Unexpected upstream serializer call count")
        path.write_bytes(original.replace(old.encode(), new.encode()))
        changed.append({"path": PREFIX + relative, "sha256": hashlib.sha256(path.read_bytes()).hexdigest()})
    return changed


def verify_layout(root, lock, *, complete=True):
    if root.is_symlink() or any(path.is_symlink() for path in root.rglob("*")):
        raise ValueError("Source tree must not contain symlinks")
    expected = {item["path"] for item in lock["files"]} | {".smartsnippets-uno018-source"}
    actual = {path.relative_to(root).as_posix() for path in root.rglob("*") if path.is_file()}
    if actual - expected - {"source-receipt.json"} or (complete and expected - actual):
        raise ValueError("Source tree file set differs from pinned lock")


def verify_source(root, lock):
    verify_layout(root, lock)
    marker = root / ".smartsnippets-uno018-source"
    if not marker.is_file() or marker.read_text() != lock["commit"]:
        raise ValueError("Wrong source identity")
    for item in lock["files"]:
        data = (root / item["path"]).read_bytes()
        relative = item["path"].removeprefix(PREFIX)
        if relative in REPLACEMENTS:
            old, new = REPLACEMENTS[relative]
            if data.count(new.encode()) != 1 or old.encode() in data:
                raise ValueError("Wrong patched source")
            data = data.replace(new.encode(), old.encode())
        if git_blob_sha1(data) != item["gitBlobSha1"]:
            raise ValueError("Prepared source differs from pinned blobs and allowed patch")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", required=True, type=Path)
    parser.add_argument("--verify", action="store_true")
    args = parser.parse_args()
    lock = json.loads((HERE / "source-lock.json").read_text(encoding="utf-8"))
    if args.output.is_symlink():
        raise ValueError("Source root must not be a symlink")
    root = args.output.resolve()
    if args.verify:
        verify_source(root, lock)
        print("pinned_source_verified")
        return 0
    marker = root / ".smartsnippets-uno018-source"
    if root.exists() and (not marker.is_file() or marker.read_text() != lock["commit"]):
        raise ValueError("Refusing a directory not owned by this source experiment")
    if root.exists():
        verify_layout(root, lock, complete=False)
    root.mkdir(parents=True, exist_ok=True, mode=0o700)
    marker.write_text(lock["commit"], encoding="ascii")
    for item in lock["files"]:
        relative = Path(item["path"])
        if relative.is_absolute() or ".." in relative.parts:
            raise ValueError("Invalid source-lock path")
        target = root / relative
        url = "https://raw.githubusercontent.com/unoplatform/uno/" + lock["commit"] + "/" + item["path"]
        with urllib.request.urlopen(url, timeout=40) as response:
            data = response.read()
        if git_blob_sha1(data) != item["gitBlobSha1"]:
            raise ValueError("Pinned upstream blob verification failed")
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_bytes(data)
    changes = patch_source(root)
    verify_source(root, lock)
    receipt = {"commit": lock["commit"], "filesVerified": len(lock["files"]), "changed": changes}
    (root / "source-receipt.json").write_text(json.dumps(receipt, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(receipt))
    return 0


if __name__ == "__main__":
    try:
        sys.exit(main())
    except (OSError, ValueError):
        print("Source preparation failed; no build is authorized from an unverified source set.", file=sys.stderr)
        sys.exit(1)
