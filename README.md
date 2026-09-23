# Stratos

AstroJS static frontend + .NET 10 BFF (Backend for Frontend) packaged as a single self-contained Linux executable.

## Architecture

```
Browser  ──►  server (Kestrel :5000)
                │
                ├── GET  /            → serves Astro static output from wwwroot/
                ├── POST /internal/environment/{env}  → switches active APIM cluster
                ├── GET  /internal/environment        → returns current environment
                └── /api/**           → YARP proxy → Azure APIM (active cluster)
```

- **Astro** builds a fully static site (`output: 'static'`). All data fetching is client-side via React island components calling the BFF.
- **YARP** proxies `/api/**` requests to the APIM cluster for the currently active environment and injects the correct `Ocp-Apim-Subscription-Key` header.
- **Subscription keys** are read from configuration (`appsettings.json` or environment variables) on Linux. When run on Windows, the code encrypts them with DPAPI (`ProtectedData`) into `secrets.dat` instead.

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
- The BFF runs on `http://localhost:5000`; for an integrated end-to-end test, run `build.sh`, then `cd dist && ./server`.
- APIM keys are read directly from `appsettings.json`.

---

## Production Build

Prerequisites: Node.js (with npm) and the .NET 10 SDK on `PATH`.

```bash
chmod +x build.sh
./build.sh
```

This:
1. Deletes any previous `dist/` and `server/wwwroot/` output
2. Runs `npm install && npm run build` in `client/`; the output goes to `server/wwwroot/`
3. Publishes the .NET BFF as a self-contained single-file Linux executable to `dist/`

Artifact: `./dist/server` (~120 MB). The target machine does not need the .NET runtime installed.

The default target is `linux-x64`. To build for another Linux architecture, set `RID`:

```bash
RID=linux-arm64 ./build.sh
```

---

## Running on Linux

1. Copy the whole `dist/` directory to the target machine. `server` needs `appsettings.json` and `wwwroot/` next to it.
2. Edit `dist/appsettings.json` and replace the placeholder keys with real APIM subscription keys:

```json
"Keys": {
  "dev":   "your-real-dev-key",
  "qa":    "your-real-qa-key",
  "stage": "your-real-stage-key",
  "prod":  "your-real-prod-key"
}
```

3. Start the server **from inside `dist/`**. The app looks for `appsettings.json` and `wwwroot/` in the working directory:

```bash
cd dist
./server
```

4. Open `http://localhost:5000` in a browser.
5. Use the **DEV / QA / STAGE / PROD** buttons in the navigation bar to switch the active APIM backend.

### Securing the keys

DPAPI is Windows-only, so on Linux the keys are read in plain text from `appsettings.json` on every request and `secrets.dat` is never created. Restrict access to the file:

```bash
chmod 600 dist/appsettings.json
```

You can also leave the keys out of the file and supply them as environment variables. .NET maps `__` to `:`:

```bash
export Apim__Keys__dev="your-real-dev-key"
export Apim__Keys__prod="your-real-prod-key"
./server
```

### Rotating keys

Update the values in `appsettings.json` (or the environment variables), then restart `server`.

### Listening address

By default Kestrel binds to `http://127.0.0.1:5000` (see `Kestrel:Endpoints:Http:Url` in `appsettings.json`), so only the local machine can reach it. To accept connections from other machines, change the URL to `http://0.0.0.0:5000`. You can also override it at startup:

```bash
Kestrel__Endpoints__Http__Url=http://0.0.0.0:5000 ./server
```

### Running as a systemd service (optional)

```ini
# /etc/systemd/system/stratos.service
[Unit]
Description=Stratos BFF
After=network.target

[Service]
WorkingDirectory=/opt/stratos
ExecStart=/opt/stratos/server
Restart=on-failure
User=stratos

[Install]
WantedBy=multi-user.target
```

```bash
sudo systemctl daemon-reload
sudo systemctl enable --now stratos
```

`WorkingDirectory` must be the directory that contains `server`, `appsettings.json` and `wwwroot/`.

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
