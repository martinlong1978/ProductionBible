# ProductionBible

A local web app unifying a video production's storyboard/bible, shoot-day production
plan, and script sheets into one dataset. See `docs/superpowers/specs/` for the design
and `docs/superpowers/plans/` for how it was built.

## Running it

Requires the .NET 10 SDK and Node.js (Angular CLI is invoked via `npx`, no global install
needed).

```bash
# One-time: build the Angular frontend into the API's wwwroot
cd web && npx ng build --output-path=../src/ProductionBible.Api/wwwroot && cd ..

# Run the app (binds to 0.0.0.0:5280 — reachable from other devices on the LAN)
dotnet run --project src/ProductionBible.Api
```

Open `http://localhost:5280` (or `http://<this-machine's-LAN-IP>:5280` from another
device on the network).

## Seeding real data (HalfNut ELS)

The importer is a one-time tool — it does not sync, it populates a fresh database once:

```bash
dotnet run --project src/ProductionBible.Importer -- \
  "D:\Data\source\HalfNutELS-Video\storyboard.html" \
  "D:\Data\source\HalfNutELS-Video\production_plan.md" \
  src/ProductionBible.Api/App_Data/productionbible.db
```

Run this against a fresh (or empty) `productionbible.db` — the app creates and migrates
that file on first run if it doesn't already exist.

## Development

- Backend: `dotnet test ProductionBible.sln` runs every C# test.
- Frontend dev server (hot reload, proxies `/api` to a separately-running backend on
  port 5280): `cd web && npm start`.
- Frontend tests: `cd web && npx ng test --watch=false`.

## Project layout

See `docs/superpowers/plans/2026-09-10-phase1-implementation.md`'s "File Structure"
section for the full layout and the reasoning behind the `Application`/`Api`/`Importer`
project split.
