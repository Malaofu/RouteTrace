# Route Trace

Working project notes for a browser-based tool that converts route images into
portable cycling-route files.

`Route Trace` is a working name only. Renaming the solution does not require an
architecture decision.

## Working with Codex

Codex should begin with:

1. [`AGENTS.md`](AGENTS.md)
2. [`docs/STATUS.md`](docs/STATUS.md)
3. [`docs/CURRENT_PBI.md`](docs/CURRENT_PBI.md)

Those files define the current scope. Codex should only open the broader
project documents when the current PBI requires them.

## Project documents

- [`docs/PROJECT.md`](docs/PROJECT.md): product scope and principles.
- [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md): intended solution structure
  and technical boundaries.
- [`docs/ROADMAP.md`](docs/ROADMAP.md): delivery phases and ordering.
- [`docs/BACKLOG.md`](docs/BACKLOG.md): candidate PBIs and acceptance criteria.
- [`docs/CURRENT_PBI.md`](docs/CURRENT_PBI.md): the implementation-scope
  identifier for the current work session.
- [`docs/FIXTURES.md`](docs/FIXTURES.md): test-data inventory and the mandatory
  fixture check before implementation.
- [`docs/STATUS.md`](docs/STATUS.md): concise record of current progress.
- [`docs/DECISIONS.md`](docs/DECISIONS.md): durable technical and product
  decisions.

## Weekend workflow

1. Select one PBI from `docs/BACKLOG.md`.
2. Put only its identifier, for example `PBI-030`, in
   `docs/CURRENT_PBI.md`.
3. Ask Codex to plan and implement only that PBI.
4. Codex reports any required fixtures before beginning fixture-dependent
   work.
5. Provide the requested fixtures, approve a synthetic substitute, or defer
   the affected work.
6. Verify the acceptance criteria.
7. Update `docs/STATUS.md` and mark the PBI complete in `docs/BACKLOG.md`.
8. Select another PBI only in a later, explicit step.

## Build and test

Install the .NET SDK version selected by [`global.json`](global.json) and a
current Node.js LTS release. Restore the frontend dependencies first:

```powershell
cd src/RouteTrace.Web
npm ci
cd ../..
```

Then run these commands from the repository root:

```powershell
dotnet restore RouteTrace.slnx
dotnet format RouteTrace.slnx --verify-no-changes --no-restore
dotnet build RouteTrace.slnx -c Release --no-restore
dotnet test RouteTrace.slnx -c Release --no-build
dotnet publish src/RouteTrace.Web/RouteTrace.Web.csproj -c Release --no-restore
```

The .NET test stack is xUnit.net v3, Shouldly, FakeItEasy, and Coverlet. Collect
Cobertura coverage reports with:

```powershell
dotnet test RouteTrace.slnx -c Release --collect:"XPlat Code Coverage"
```

The static site is published to
`src/RouteTrace.Web/bin/Release/net10.0/publish/wwwroot`. Serve that directory
from a static file server rather than opening `index.html` directly. For
example:

```powershell
dotnet tool install --global dotnet-serve
dotnet serve --directory src/RouteTrace.Web/bin/Release/net10.0/publish/wwwroot
```

The GitHub Actions workflow runs formatting, build, test, and publish checks on
pushes to `main` and on pull requests. It does not deploy the application.

### Frontend styling

Global styles are authored under `src/RouteTrace.Web/Styles`, with `app.scss`
as the entry point and partials grouped by purpose. Component styles live beside
their components as `.razor.scss` files.

`npm run styles` discovers all `.razor.scss` files automatically. It creates
the ignored browser stylesheet at `wwwroot/generated/app.css` and component
CSS intermediates under `.generated/scopedcss-input`, preserving their
project-relative paths. A single wildcard maps those intermediates back to
their Razor components before Blazor's CSS-isolation processing. The staging
tree sits outside `wwwroot`, so only Blazor's processed style bundle is
published. A clean build therefore requires only `npm ci`; generated CSS
should not be edited or committed.

`npm run icons` uses `wwwroot/images/route-trace-icon.svg` as the authored icon
and generates the ignored 192px and 512px PNG variants required for Apple touch
icons and broad PWA compatibility.

## Manual map verification

Run the Web project and open the displayed local URL in a desktop browser:

```powershell
dotnet run --project src/RouteTrace.Web/RouteTrace.Web.csproj
```

Verify the following after map-shell changes:

1. The map fills the area below the application header.
2. Dragging pans the map, and the mouse wheel and `+`/`−` controls zoom it.
3. Resizing the browser does not leave gaps or misalign map controls.
4. The initial view fits Denmark and surrounding countries.
5. The OpenStreetMap attribution remains visible in the bottom-right corner.
6. Light and Dark immediately change the application chrome, and the selected
   preference survives a reload.
7. Auto follows the operating-system colour preference, including changes made
   while the application is open.

## License

Route Trace is available under the [MIT License](LICENSE).
