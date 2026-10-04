#!/usr/bin/env python3
"""Validates the structure of the assembled Unity package.

Usage: validate_package.py <package-dir> [--native-env scripts/native.env]

Checks what can go wrong without a Unity Editor: package.json fields and
version, asmdef files, the native bridges and dependency declarations, and that
every asset has a .meta with a unique GUID (and no meta is orphaned).
"""
import json
import os
import re
import sys

BUNDLE_EXT = (".xcframework", ".framework", ".bundle")
errors = []


def fail(msg):
    errors.append(msg)


def read(path):
    with open(path, encoding="utf-8") as fh:
        return fh.read()


def hidden(name):
    return name.endswith("~") or name.startswith(".")


def main():
    root = sys.argv[1]
    native_env = {}
    env_path = os.path.join(os.path.dirname(os.path.abspath(__file__)), "native.env")
    if "--native-env" in sys.argv:
        env_path = sys.argv[sys.argv.index("--native-env") + 1]
    for line in read(env_path).splitlines():
        if "=" in line and not line.startswith("#"):
            k, v = line.split("=", 1)
            native_env[k.strip()] = v.strip()

    # package.json
    pkg = json.loads(read(os.path.join(root, "package.json")))
    for key in ("name", "version", "displayName", "description", "unity"):
        if not pkg.get(key):
            fail(f"package.json: missing '{key}'")
    if pkg.get("name") != "com.appdiscoverysdk.sdk":
        fail(f"package.json: unexpected name {pkg.get('name')}")
    if not re.fullmatch(r"\d+\.\d+\.\d+", pkg.get("version", "")):
        fail(f"package.json: version {pkg.get('version')} is not MAJOR.MINOR.PATCH")
    for sample in pkg.get("samples", []):
        if not os.path.isdir(os.path.join(root, sample["path"])):
            fail(f"package.json: sample path {sample['path']} is missing")

    # asmdefs
    runtime = json.loads(read(os.path.join(root, "Runtime", "AppDiscovery.Runtime.asmdef")))
    editor = json.loads(read(os.path.join(root, "Editor", "AppDiscovery.Editor.asmdef")))
    if runtime["name"] != "AppDiscovery.Runtime":
        fail("Runtime asmdef has the wrong name")
    if "AppDiscovery.Runtime" not in editor.get("references", []):
        fail("Editor asmdef does not reference AppDiscovery.Runtime")
    if editor.get("includePlatforms") != ["Editor"]:
        fail("Editor asmdef must be Editor-only")

    # version constants
    sdk_cs = read(os.path.join(root, "Runtime", "Scripts", "AppDiscoverySDK.cs"))
    m = re.search(r'SDK_VERSION\s*=\s*"([^"]+)"', sdk_cs)
    if not m or m.group(1) != pkg["version"]:
        fail(f"SDK_VERSION ({m.group(1) if m else None}) differs from package.json ({pkg['version']})")

    # native dependencies
    deps = read(os.path.join(root, "Editor", "AppDiscoveryDependencies.xml"))
    want = f'com.github.appdiscoverysdk:appdiscovery-android:{native_env["APPDISCOVERY_ANDROID_VERSION"]}"'
    if want not in deps:
        fail(f"Editor/AppDiscoveryDependencies.xml does not depend on appdiscovery-android:{native_env['APPDISCOVERY_ANDROID_VERSION']}")
    if "https://jitpack.io" not in deps:
        fail("Editor/AppDiscoveryDependencies.xml does not declare the JitPack repository")
    for rel in ("Runtime/Plugins/Android/AppDiscoveryUnityBridge.java", "Runtime/Plugins/iOS/AppDiscoveryUnityBridge.swift"):
        if not os.path.isfile(os.path.join(root, rel)):
            fail(f"missing {rel}")
    xcf = os.path.join(root, "Runtime", "Plugins", "iOS", "AppDiscoverySDK.xcframework")
    if not os.path.isfile(os.path.join(xcf, "Info.plist")):
        fail("Runtime/Plugins/iOS/AppDiscoverySDK.xcframework is missing or incomplete")

    # the receiver contract between C# and the bridges
    receiver = read(os.path.join(root, "Runtime", "Scripts", "Internal", "AppDiscoveryCallbackReceiver.cs"))
    java = read(os.path.join(root, "Runtime", "Plugins", "Android", "AppDiscoveryUnityBridge.java"))
    swift = read(os.path.join(root, "Runtime", "Plugins", "iOS", "AppDiscoveryUnityBridge.swift"))
    obj = re.search(r'GAME_OBJECT_NAME\s*=\s*"([^"]+)"', receiver).group(1)
    for label, text in (("Java", java), ("Swift", swift)):
        if f'"{obj}"' not in text:
            fail(f"{label} bridge does not address the receiver object {obj}")
        for method in re.findall(r"public void (On\w+)\(string", receiver):
            if f'"{method}"' not in text:
                fail(f"{label} bridge never calls receiver method {method}")
    ios_cs = read(os.path.join(root, "Runtime", "Scripts", "Internal", "AppDiscoveryIOS.cs"))
    for fn in re.findall(r"static extern void (\w+)\(", ios_cs):
        if f'@_cdecl("{fn}")' not in swift:
            fail(f"iOS bridge does not export {fn}")
    android_cs = read(os.path.join(root, "Runtime", "Scripts", "Internal", "AppDiscoveryAndroid.cs"))
    for fn in set(re.findall(r'CallStatic\("(\w+)"', android_cs)):
        if not re.search(rf"public static void {fn}\(", java):
            fail(f"Android bridge has no static method {fn}")

    # metas
    guids = {}
    expected_metas = set()
    for dirpath, dirnames, filenames in os.walk(root):
        rel_dir = os.path.relpath(dirpath, root)
        if any(p.lower().endswith(BUNDLE_EXT) for p in rel_dir.split(os.sep)):
            dirnames[:] = []
            continue
        dirnames[:] = [d for d in dirnames if not hidden(d)]
        for name in list(dirnames) + [f for f in filenames if not f.endswith(".meta") and not hidden(f)]:
            rel = name if rel_dir == "." else f"{rel_dir}/{name}"
            meta = os.path.join(root, rel + ".meta")
            expected_metas.add(rel + ".meta")
            if not os.path.isfile(meta):
                fail(f"missing {rel}.meta")
                continue
            g = re.search(r"^guid: ([0-9a-f]{32})$", read(meta), re.M)
            if not g:
                fail(f"{rel}.meta has no valid guid")
            elif g.group(1) in guids:
                fail(f"{rel}.meta repeats the guid of {guids[g.group(1)]}")
            else:
                guids[g.group(1)] = rel
        # dirs ending with a bundle extension: their .meta is expected, contents are not walked
        dirnames[:] = [d for d in dirnames]
    for dirpath, dirnames, filenames in os.walk(root):
        rel_dir = os.path.relpath(dirpath, root)
        if any(hidden(p) or p.lower().endswith(BUNDLE_EXT) for p in rel_dir.split(os.sep) if p != "."):
            dirnames[:] = []
            continue
        dirnames[:] = [d for d in dirnames if not hidden(d)]
        for f in filenames:
            if f.endswith(".meta"):
                rel = f if rel_dir == "." else f"{rel_dir}/{f}"
                if rel not in expected_metas:
                    fail(f"orphan meta {rel}")

    if errors:
        print("FAIL: package validation:")
        for e in errors:
            print("  " + e)
        sys.exit(1)
    print(f"OK: package {pkg['name']}@{pkg['version']} is structurally valid ({len(guids)} assets with unique GUIDs)")


main()
