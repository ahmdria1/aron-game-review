#!/usr/bin/env bash
# Runs inside the unityci/editor iOS image with the project mounted as the working directory.
# Takes a Personal seat with the Unity account, exports the Xcode project into build/iOS/Aron,
# then hands the seat back. Signing happens later on a Mac.
set -euo pipefail

: "${UNITY_EMAIL:?UNITY_EMAIL is not set}"
: "${UNITY_PASSWORD:?UNITY_PASSWORD is not set}"

licensing=/opt/unity/Editor/Data/Resources/Licensing/Client/Unity.Licensing.Client

cleanup() {
  "$licensing" --deactivate-all --username "$UNITY_EMAIL" --password "$UNITY_PASSWORD" > /dev/null 2>&1 || true
}

"$licensing" --activate-all --include-personal --username "$UNITY_EMAIL" --password "$UNITY_PASSWORD"
trap cleanup EXIT

args=(-batchmode -nographics -projectPath "$PWD" -buildTarget iOS
      -executeMethod Aron.EditorTools.CI.PrepareAndBuild
      -customBuildPath build/iOS/Aron -logFile -)

if [ "${GITHUB_REF_TYPE:-}" = "tag" ]; then
  args+=(-buildVersion "${GITHUB_REF_NAME#v}" -androidVersionCode "${GITHUB_RUN_NUMBER:-1}")
fi

mkdir -p build/iOS
unity-editor "${args[@]}"

ls -l build/iOS/Aron
