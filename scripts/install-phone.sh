#!/usr/bin/env bash
set -euo pipefail
project_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
apk_path="$project_dir/Builds/BlockBlaster.apk"
if [[ ! -s "$apk_path" ]]; then
  echo 'Builds/BlockBlaster.apk is missing. Run scripts/build-android.sh first.' >&2
  exit 1
fi
command -v adb >/dev/null || { echo 'ADB is not installed.' >&2; exit 1; }
adb_args=()
if [[ -n "${1:-}" ]]; then adb_args=(-s "$1"); fi
adb "${adb_args[@]}" get-state
adb "${adb_args[@]}" install -r "$apk_path"
adb "${adb_args[@]}" shell monkey -p com.amalbasil.blockblaster -c android.intent.category.LAUNCHER 1

