# MechSoft — Ferrowatt PLM

An inventory, PLM and manufacturing tracker for Ferrowatt Technologies, built with
ASP.NET Core 8 Razor Pages in a Clean Architecture layout (Domain / Application /
Infrastructure / Web). Originally ported from a React inventory dashboard mock-up.

## Features

- **Dashboard** — stock health, open purchase orders, delivery challans, and a
  "Project Details" panel showing machine-build projects and their completion %.
- **Inventory** — stock levels and status (In Stock / Low Stock / Out of Stock) per part.
- **Product Details** — specs, notes, vendor contact details (email/phone), and a
  product photo for each part.
- **Projects** — machine-manufacturing jobs tracked from Planning through Completed,
  with a completion-% progress bar and a progress photo.
- **eDrawings Viewer** — a shared CAD file library. Upload files or whole folders;
  STL, OBJ, STEP, IGES, BREP, PLY, glTF/GLB, FBX, DAE, 3DS and 3MF render in a
  full-screen 3D viewer client-side. Any other CAD format (SolidWorks, DWG, etc.)
  can still be stored and downloaded, and optionally previewed via
  [Autodesk Platform Services](#autodesk-platform-services-optional).
- **Purchase Orders / Delivery Challans / Billing / Quote Preparation** — full
  CRUD with a shared invoice-style document layout and server-side PDF generation
  (QuestPDF). Quotes can be converted directly into invoices.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download) (pinned to `8.0.421` in
  [`global.json`](global.json); any 8.0.x SDK with `rollForward: latestFeature`
  will work)
- SQL Server LocalDB (installed with Visual Studio, or standalone via the
  [SQL Server Express/LocalDB installer](https://learn.microsoft.com/sql/database-engine/configure-windows/sql-server-express-localdb))
- Windows (LocalDB is Windows-only; swap the connection string for a regular SQL
  Server/Docker instance to run elsewhere)

## Getting started

```bash
git clone https://github.com/Revanth0817/mechsoft.git
cd mechsoft
dotnet restore
dotnet run --project src/MechSoft.Web
```

The app starts on **http://localhost:5069** (and **https://localhost:7198**) and
opens a browser automatically. On first run it:

1. Creates the `MechSoftDb` LocalDB instance and the `MechSoftInventory` database.
2. Applies all EF Core migrations.
3. Seeds sample parts, vendors, purchase orders, delivery challans, invoices,
   quotes and machine-build projects, so the app isn't empty on first launch.

No manual database setup is required — everything is created automatically by
[`DbInitializer`](src/MechSoft.Infrastructure/Persistence/Seed/DbInitializer.cs)
on startup.

### Using a different database

The connection string lives in
[`src/MechSoft.Web/appsettings.json`](src/MechSoft.Web/appsettings.json):

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\MechSoftDb;Database=MechSoftInventory;Trusted_Connection=True;TrustServerCertificate=True"
}
```

Point it at any SQL Server instance (local, Docker, or Azure SQL) and re-run the
app — migrations apply automatically either way.

## Autodesk Platform Services (optional)

Proprietary CAD formats the browser can't render natively (SolidWorks `.sldprt`/
`.sldasm`, DWG, Inventor, CATIA, etc.) can be previewed through
[Autodesk Platform Services](https://aps.autodesk.com) instead of just stored.
This is entirely optional — without it, those files are still stored and
downloadable, just not previewable in-browser.

1. Create a **Server-to-Server** (or Traditional Web App) application at
   [aps.autodesk.com](https://aps.autodesk.com), with the **Model Derivative**
   and **Data Management** APIs enabled.
2. Store the credentials with `dotnet user-secrets` (never in `appsettings.json`,
   so they don't end up in source control):

   ```bash
   dotnet user-secrets set "Aps:ClientId" "<your client id>" --project src/MechSoft.Web
   dotnet user-secrets set "Aps:ClientSecret" "<your client secret>" --project src/MechSoft.Web
   ```

3. Restart the app. Unsupported CAD files in the eDrawings Viewer will show a
   "Preview via Autodesk" option once configured.

## Project structure

```
src/
  MechSoft.Domain          Entities and enums — no dependencies on anything else.
  MechSoft.Application     DTOs and service interfaces.
  MechSoft.Infrastructure  EF Core DbContext, migrations, service implementations,
                            PDF generation (QuestPDF), Autodesk integration.
  MechSoft.Web              Razor Pages UI (Tailwind CDN + Alpine.js), Program.cs.
```

## Tech stack

- ASP.NET Core 8 Razor Pages, EF Core 8 (SQL Server)
- Tailwind CSS (CDN) + Alpine.js for interactive UI without a JS build step
- Three.js (client-side STL/OBJ/STEP/... rendering) + OpenCascade WASM
  (`occt-import-js`) for STEP/IGES
- QuestPDF for server-side PDF generation (Community license)
- Autodesk Platform Services (optional) for proprietary CAD preview

## Notes

- There is no authentication; this is a single-tenant internal tool.
- Uploaded CAD files and product/project photos are stored as `varbinary(max)`
  in the database, not on disk, so they're shared by everyone using the app and
  travel with a database backup.
