#!/usr/bin/env bash
set -e

echo "==> Building Astro client..."
cd client
npm install
npm run build
cd ..

echo "==> Publishing .NET BFF (win-x64, single file)..."
cd server
dotnet publish \
  -c Release \
  -r win-x64 \
  --self-contained true \
  /p:PublishSingleFile=true \
  /p:IncludeNativeLibrariesForSelfExtract=true \
  -o ../dist
cd ..

echo ""
echo "==> Build complete."
echo "    Artifact: ./dist/server.exe"
echo "    Copy dist/server.exe to the target Windows machine and run it."
echo "    Open http://localhost:5000 in a browser."
