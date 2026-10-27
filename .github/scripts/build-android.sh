#!/usr/bin/env bash
# Runs inside the unityci/editor image with the project mounted as the working directory.
# Signs in with the Unity account to take a Personal seat, builds Aron.apk and Aron.aab
# into build/Android, then hands the seat back.
set -euo pipefail

: "${UNITY_EMAIL:?UNITY_EMAIL is not set}"
: "${UNITY_PASSWORD:?UNITY_PASSWORD is not set}"

licensing=/opt/unity/Editor/Data/Resources/Licensing/Client/Unity.Licensing.Client

cleanup() {
  rm -f aron-upload.keystore
  "$licensing" --deactivate-all --username "$UNITY_EMAIL" --password "$UNITY_PASSWORD" > /dev/null 2>&1 || true
}

"$licensing" --activate-all --include-personal --username "$UNITY_EMAIL" --password "$UNITY_PASSWORD"
trap cleanup EXIT

args=(-batchmode -nographics -projectPath "$PWD" -buildTarget Android
      -executeMethod Aron.EditorTools.CI.PrepareAndBuild
      -customBuildPath build/Android -logFile -)

if [ -n "${ANDROID_KEYSTORE_BASE64:-}" ]; then
  echo "$ANDROID_KEYSTORE_BASE64" | base64 -d > aron-upload.keystore
  args+=(-androidKeystoreName aron-upload.keystore
         -androidKeystorePass "${ANDROID_KEYSTORE_PASS:-}"
         -androidKeyaliasName "${ANDROID_KEYALIAS_NAME:-}"
         -androidKeyaliasPass "${ANDROID_KEYALIAS_PASS:-}")
fi

if [ "${GITHUB_REF_TYPE:-}" = "tag" ]; then
  args+=(-buildVersion "${GITHUB_REF_NAME#v}" -androidVersionCode "${GITHUB_RUN_NUMBER:-1}")
fi

mkdir -p build/Android
unity-editor "${args[@]}"

ls -l build/Android
