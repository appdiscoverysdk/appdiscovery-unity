#!/usr/bin/env python3
"""Fail if a tree, an archive or a built artifact carries branded strings.

Usage:
  check_neutral.py PATH [PATH ...]   scan files, directories and archives
  check_neutral.py --selftest        prove the scan can fail

What is scanned: every path and every byte of each file, descending into
.zip/.aar/.jar/.nupkg/.tgz/.tar.gz archives, for the old brand names
(case-insensitive) after removing the documented wire-contract exceptions
below. Text sources are also checked for a pre-release ("be"+"ta") flag, which the
SDK does not have.

The brand names are assembled from fragments so that this script does not
trip its own scan.
"""
import io
import pathlib
import re
import sys
import tarfile
import tempfile
import zipfile

_A, _B, _C = "per", "kox", "kwall"

# Wire contract with the offerwall web app, present inside the native SDKs.
# Remove an entry only after the web app stops using it.
ALLOWED = [(b"Per" + _B.encode() + b"Native"), (b"PER" + _B.upper().encode() + b"_REWARD:")]
BRANDED = re.compile((_A + "(?:" + _B + "|" + _C + ")").encode(), re.IGNORECASE)
_FW = "be" + "ta"
FLAG = re.compile(rb"\b" + _FW.encode() + rb"\b", re.IGNORECASE)

SKIP_DIRS = {".git", "node_modules", ".dart_tool", ".gradle"}
ARCHIVES = (".zip", ".aar", ".jar", ".nupkg")
TARS = (".tgz", ".tar.gz")
# The flag is checked in these text files only (not in binaries, not in lock files,
# whose pre-release version strings legitimately contain it).
TEXT_EXT = {
    ".dart", ".ts", ".tsx", ".js", ".mjs", ".cjs", ".json", ".kt", ".kts", ".java",
    ".swift", ".m", ".mm", ".h", ".cs", ".md", ".yml", ".yaml", ".gradle", ".podspec",
    ".xml", ".plist", ".sh", ".py", ".swiftinterface", ".modulemap", ".asmdef",
    ".xcprivacy", ".meta", ".properties", ".env",
}
FLAG_SKIP_NAMES = {"package-lock.json", "pubspec.lock", "yarn.lock"}


def _ctx(data, m):
    return data[max(0, m.start() - 20): m.end() + 20].decode("latin-1")


def scan_bytes(label, data, findings, seen, check_flag):
    for token in ALLOWED:
        if token in data:
            seen.add(token.decode())
        data = data.replace(token, b"")
    for m in BRANDED.finditer(data):
        findings.append(f"{label}: ...{_ctx(data, m)!r}...")
    if check_flag:
        for m in FLAG.finditer(data):
            findings.append(f"{label}: pre-release flag ...{_ctx(data, m)!r}...")


def scan_blob(label, name, data, findings, seen):
    lower = name.lower()
    if BRANDED.search(label.encode()):
        findings.append(f"{label}: path")
    if lower.endswith(ARCHIVES):
        try:
            with zipfile.ZipFile(io.BytesIO(data)) as zf:
                for info in zf.infolist():
                    if not info.is_dir():
                        scan_blob(f"{label}!/{info.filename}", info.filename, zf.read(info), findings, seen)
            return
        except zipfile.BadZipFile:
            pass
    if lower.endswith(TARS):
        try:
            with tarfile.open(fileobj=io.BytesIO(data), mode="r:gz") as tf:
                for member in tf:
                    if member.isfile():
                        scan_blob(f"{label}!/{member.name}", member.name, tf.extractfile(member).read(), findings, seen)
            return
        except tarfile.TarError:
            pass
    base = pathlib.PurePosixPath(name).name
    check_flag = (pathlib.PurePosixPath(name).suffix.lower() in TEXT_EXT) and base not in FLAG_SKIP_NAMES
    scan_bytes(label, data, findings, seen, check_flag)


def scan_path(root, findings, seen):
    root = pathlib.Path(root)
    if root.is_file():
        scan_blob(str(root), root.name, root.read_bytes(), findings, seen)
        return 1
    count = 0
    for path in sorted(root.rglob("*")):
        rel = path.relative_to(root)
        if any(part in SKIP_DIRS for part in rel.parts) or not path.is_file():
            continue
        count += 1
        scan_blob(str(rel), path.name, path.read_bytes(), findings, seen)
    return count


def run(paths):
    findings, seen, scanned = [], set(), 0
    for p in paths:
        if not pathlib.Path(p).exists():
            sys.exit(f"not found: {p}")
        scanned += scan_path(p, findings, seen)
    if scanned == 0:
        sys.exit("sanity check failed: nothing to scan")
    return findings, seen, scanned


def selftest():
    brand = (_A + _B).encode()
    with tempfile.TemporaryDirectory() as t:
        t = pathlib.Path(t)
        (t / "clean").mkdir()
        (t / "clean" / "a.dart").write_text("// AppDiscovery\n")
        (t / "clean" / "b.bin").write_bytes(b"\x00" + ALLOWED[0] + b"\x00" + ALLOWED[1])
        findings, seen, _ = run([t / "clean"])
        assert not findings, f"clean tree flagged: {findings}"
        assert seen == {a.decode() for a in ALLOWED}, "allow-list not recognised"

        cases = {
            "text": ("planted.dart", b"// " + brand.capitalize() + b"\n"),
            "wall": ("planted.kt", b"x = '" + (_A + _C).encode() + b".com'"),
            "flag": ("planted.ts", b"export const " + _FW.encode() + b" = true;"),
            "path": (brand.decode() + "_file.txt", b"hello"),
        }
        for name, (fname, content) in cases.items():
            d = t / name
            d.mkdir()
            (d / fname).write_bytes(content)
            findings, _, _ = run([d])
            assert findings, f"scan did not catch planted case '{name}'"

        zpath = t / "planted.aar"
        with zipfile.ZipFile(zpath, "w") as zf:
            zf.writestr("classes.jar", _zip_bytes({"x.class": b"..." + brand + b"..."}))
        findings, _, _ = run([zpath])
        assert findings, "scan did not descend into a nested jar"

        tpath = t / "planted.tgz"
        with tarfile.open(tpath, "w:gz") as tf:
            data = b"var a = '" + brand + b"';"
            info = tarfile.TarInfo("package/index.js")
            info.size = len(data)
            tf.addfile(info, io.BytesIO(data))
        findings, _, _ = run([tpath])
        assert findings, "scan did not descend into a tarball"

        (t / "lock").mkdir()
        (t / "lock" / "package-lock.json").write_text('{"version": "1.0.0-' + _FW + '.1"}')
        findings, _, _ = run([t / "lock"])
        assert not findings, "lock files must be exempt from the flag check"
    print("selftest OK: the scan fails on a planted brand (text, path, nested jar, tarball) and on a pre-release flag")


def _zip_bytes(entries):
    buf = io.BytesIO()
    with zipfile.ZipFile(buf, "w") as zf:
        for k, v in entries.items():
            zf.writestr(k, v)
    return buf.getvalue()


def main():
    if len(sys.argv) < 2:
        sys.exit(__doc__)
    if sys.argv[1] == "--selftest":
        selftest()
        return
    findings, seen, scanned = run(sys.argv[1:])
    if findings:
        print(f"FAIL: {len(findings)} branded or pre-release string(s):")
        for f in findings[:60]:
            print("  " + f)
        sys.exit(1)
    print(f"OK: no branded strings ({scanned} files scanned)")
    print("Known exceptions present (wire contract with the offerwall web app): "
          + (", ".join(sorted(seen)) or "none"))


main()
