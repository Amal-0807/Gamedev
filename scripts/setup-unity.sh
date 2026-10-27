#!/usr/bin/env bash
set -euo pipefail
project_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
editor_version="$(awk '/^m_EditorVersion:/{print $2}' "$project_dir/ProjectSettings/ProjectVersion.txt")"
hub_path="$project_dir/Tools/UnityHubSetup-x86_64.AppImage"
editor_path="$HOME/Unity/Hub/Editor/$editor_version/Editor/Unity"

case "${1:-hub}" in
  hub|editor) ;;
  *) echo 'Usage: bash scripts/setup-unity.sh [hub|editor]' >&2; exit 2 ;;
esac
command -v curl >/dev/null || { echo 'curl is required to download Unity Hub.' >&2; exit 1; }
if [[ ! -s "$hub_path" ]]; then
  mkdir -p "$project_dir/Tools"
  echo 'Downloading the official Unity Hub AppImage...'
  curl --fail --location --retry 2 --connect-timeout 15 \
    'https://public-cdn.cloud.unity3d.com/hub/prod/UnityHubSetup-x86_64.AppImage' \
    --output "$hub_path.part"
  mv -- "$hub_path.part" "$hub_path"
  chmod +x "$hub_path"
fi

if [[ "${1:-hub}" == hub ]]; then
  echo 'In Unity Hub, sign in and activate your eligible license (Personal for eligible students).'
  echo 'Then close Hub and run: bash scripts/setup-unity.sh editor'
  exec "$hub_path" --appimage-extract-and-run
fi

if [[ -x "$editor_path" && -d "$HOME/Unity/Hub/Editor/$editor_version/Editor/Data/PlaybackEngines/AndroidPlayer/SDK" ]]; then
  echo "Unity with Android tools is already installed: $editor_path"
  exit 0
fi
mkdir -p "$HOME/Unity/Hub/Editor"
"$hub_path" --appimage-extract-and-run --headless install-path -s "$HOME/Unity/Hub/Editor"
if [[ -x "$editor_path" ]]; then
  "$hub_path" --appimage-extract-and-run --headless install-modules --version "$editor_version" \
    -m android android-sdk-ndk-tools android-open-jdk --childModules
else
  "$hub_path" --appimage-extract-and-run --headless install --version "$editor_version" \
    --changeset 92dee566b325 -m android android-sdk-ndk-tools android-open-jdk --childModules
fi
echo 'Editor installed. Build with: bash scripts/build-android.sh'

