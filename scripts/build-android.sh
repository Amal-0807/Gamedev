#!/usr/bin/env bash
set -euo pipefail
project_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
editor_version="$(awk '/^m_EditorVersion:/{print $2}' "$project_dir/ProjectSettings/ProjectVersion.txt")"
unity_editor="${1:-${UNITY_EDITOR:-$HOME/Unity/Hub/Editor/$editor_version/Editor/Unity}}"
if [[ ! -x "$unity_editor" ]]; then
  echo 'Unity Editor is missing. Run scripts/setup-unity.sh hub, then scripts/setup-unity.sh editor.' >&2
  echo 'For another install, pass the full Editor/Unity path as the first argument.' >&2
  exit 1
fi
# Ubuntu 25.10+ dropped libxml2.so.2, which this editor still links against.
export LD_LIBRARY_PATH="$project_dir/Tools/compat-libs${LD_LIBRARY_PATH:+:$LD_LIBRARY_PATH}"
# Unity's bundled .NET 7 IL2CPP converter segfaults intermittently on Linux 6.17 without these.
export DOTNET_EnableWriteXorExecute=0 DOTNET_gcServer=0
mkdir -p "$project_dir/Builds"
echo 'Building Block Blaster for Android. Unity output: Builds/build.log'
if ! "$unity_editor" -batchmode -quit -projectPath "$project_dir" -buildTarget Android \
  -executeMethod BlockBlaster.Editor.ProjectSetup.BuildAndroid -logFile "$project_dir/Builds/build.log"; then
  echo 'Unity build failed. Check Builds/build.log for compilation, license, or Android tool errors.' >&2
  exit 1
fi
if [[ ! -s "$project_dir/Builds/BlockBlaster.apk" ]]; then
  echo 'Unity exited without producing an APK. Check Builds/build.log.' >&2
  exit 1
fi
echo "APK ready: $project_dir/Builds/BlockBlaster.apk"

