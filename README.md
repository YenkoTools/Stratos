# Stratos

AstroJS static frontend + .NET 10 BFF (Backend for Frontend) packaged as a single self-contained Windows executable.

## Architecture

```
Browser  ──►  server.exe (Kestrel :5000)
                │
                ├── GET  /            → serves Astro static output from wwwroot/
                ├── POST /internal/environment/{env}  → switches active APIM cluster
                ├── GET  /internal/environment        → returns current environment
                └── /api/**           → YARP proxy → Azure APIM (active cluster)
```

- **Astro** builds a fully static site (`output: 'static'`). All data fetching is client-side via React island components calling the BFF.
- **YARP** proxies `/api/**` requests to the APIM cluster for the currently active environment and injects the correct `Ocp-Apim-Subscription-Key` header.
- **DPAPI** (`ProtectedData`) encrypts APIM subscription keys to `secrets.dat` on first run (Windows only). On Linux in development, keys are read directly from `appsettings.json`.

---

## Development (Linux)

Open two terminals:

```bash
# Terminal 1 — Astro dev server (hot-reload, port 4321)
cd client
npm install
npm run dev
```

```bash
# Terminal 2 — .NET BFF (port 5000)
cd server
dotnet run
```

- The Astro dev server runs on `http://localhost:4321` with its own dev proxy; use it for UI iteration.
- The BFF runs on `http://localhost:5000`; for an integrated end-to-end test, run `build.sh` and test on Windows.
- DPAPI is disabled on Linux; APIM keys are read directly from `appsettings.json`.

---

## Production Build

```bash
chmod +x build.sh
./build.sh
```

This:
1. Runs `npm install && npm run build` in `client/` — output lands in `server/wwwroot/`
2. Publishes the .NET BFF as a self-contained single-file Windows executable to `dist/`

Artifact: `./dist/server.exe` (~25–35 MB).

---

## First Run on Windows

1. Copy `server.exe` (and `appsettings.json`) to the target machine.
2. Edit `appsettings.json` — replace the placeholder keys with real APIM subscription keys:

```json
"Keys": {
  "dev":   "your-real-dev-key",
  "qa":    "your-real-qa-key",
  "stage": "your-real-stage-key",
  "prod":  "your-real-prod-key"
}
```

3. Run `server.exe`.
   - On first run DPAPI encrypts the keys from `appsettings.json` into `secrets.dat` alongside the exe.
   - On subsequent runs, keys are loaded from `secrets.dat` (plaintext values in `appsettings.json` are no longer used for key lookup).
4. Open `http://localhost:5000` in any browser.
5. Use the **DEV / QA / STAGE / PROD** buttons in the navigation bar to switch the active APIM backend.

### Rotating keys

1. Update `appsettings.json` with new key values.
2. Delete `secrets.dat`.
3. Restart `server.exe` — DPAPI will re-encrypt the updated keys on startup.

---

## Replacing APIM endpoints

Update `Apim:Clusters` in `appsettings.json`:

```json
"Clusters": {
  "dev":   "https://your-apim-dev.azure-api.net",
  "qa":    "https://your-apim-qa.azure-api.net",
  "stage": "https://your-apim-stage.azure-api.net",
  "prod":  "https://your-apim-prod.azure-api.net"
}
```

---

## Replacing the data grid

`client/src/components/ExampleGrid.tsx` contains a plain HTML table with a `TODO` comment marking the integration point for a commercial grid. Drop in AG Grid or Telerik KendoReact there — both accept `rows` as `rowData` / `data` and derive column definitions from `columns`.

---

## Adding Auth0 (future)

Auth middleware is intentionally omitted. The BFF pipeline is a straightforward place to add OIDC/Auth0 — add `builder.Services.AddAuthentication(...)` and `app.UseAuthentication()` / `app.UseAuthorization()` in `Program.cs` before the proxy middleware.
