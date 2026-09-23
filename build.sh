#!/usr/bin/env bash
set -euo pipefail

# Target runtime; override with e.g. RID=linux-arm64 ./build.sh
RID="${RID:-linux-x64}"

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DIST="$ROOT/dist"

for tool in node npm dotnet; do
  command -v "$tool" >/dev/null 2>&1 || { echo "error: '$tool' not found on PATH" >&2; exit 1; }
done

echo "==> Cleaning previous output..."
rm -rf "$DIST" "$ROOT/server/wwwroot"

echo "==> Building Astro client..."
cd "$ROOT/client"
npm install
npm run build

echo "==> Publishing .NET BFF ($RID, single file)..."
cd "$ROOT/server"
dotnet publish \
  -c Release \
  -r "$RID" \
  --self-contained true \
  /p:PublishSingleFile=true \
  /p:IncludeNativeLibrariesForSelfExtract=true \
  -o "$DIST"

chmod +x "$DIST/server"

echo ""
echo "==> Build complete."
echo "    Artifact: ./dist/server"
echo "    Run it with: cd dist && ./server"
echo "    Open http://localhost:5000 in a browser."
