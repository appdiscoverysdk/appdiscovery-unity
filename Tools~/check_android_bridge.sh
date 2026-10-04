#!/bin/bash
# Compiles the Android bridge against the real, published native SDK, so a wrong
# call into the SDK fails here and not in a customer's Gradle build.
#
# Needs: curl, unzip, a JDK (17+) and the Android SDK (ANDROID_HOME) for android.jar.
# Usage: scripts/check_android_bridge.sh   (Tools~/check_android_bridge.sh in the public repository)
set -euo pipefail

HERE="$(cd "$(dirname "$0")" && pwd)"
ROOT="$(cd "$HERE/.." && pwd)"
# shellcheck disable=SC1091
. "$HERE/native.env"
V="$APPDISCOVERY_ANDROID_VERSION"

W="$(mktemp -d)"
trap 'rm -rf "$W"' EXIT

curl -fsSL --retry 5 --max-time 600 -o "$W/sdk.aar" \
  "https://jitpack.io/com/github/appdiscoverysdk/appdiscovery-android/$V/appdiscovery-android-$V.aar"
unzip -q -o "$W/sdk.aar" classes.jar -d "$W"
curl -fsSL --retry 3 -o "$W/kotlin-stdlib.jar" \
  "https://repo1.maven.org/maven2/org/jetbrains/kotlin/kotlin-stdlib/1.9.0/kotlin-stdlib-1.9.0.jar"

SDK_HOME="${ANDROID_HOME:-${ANDROID_SDK_ROOT:-}}"
[ -n "$SDK_HOME" ] || { echo "ANDROID_HOME is not set" >&2; exit 1; }
ANDROID_JAR="$(ls -d "$SDK_HOME"/platforms/android-*/android.jar | sort -V | tail -1)"

# Stand-in for the one Unity class the bridge uses.
mkdir -p "$W/stub/com/unity3d/player"
cat > "$W/stub/com/unity3d/player/UnityPlayer.java" <<'JAVA'
package com.unity3d.player;
public class UnityPlayer {
    public static void UnitySendMessage(String object, String method, String message) {}
}
JAVA

mkdir -p "$W/out"
javac --release 17 -Xlint:all -d "$W/out" \
  -cp "$ANDROID_JAR:$W/classes.jar:$W/kotlin-stdlib.jar" \
  "$W/stub/com/unity3d/player/UnityPlayer.java" \
  "$ROOT/Runtime/Plugins/Android/AppDiscoveryUnityBridge.java"
test -f "$W/out/com/appdiscoverysdk/unity/AppDiscoveryUnityBridge.class"
echo "OK: the Android bridge compiles against appdiscovery-android $V"
