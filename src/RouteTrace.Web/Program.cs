using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using RouteTrace.Web;
using RouteTrace.Web.Features.Import;
using RouteTrace.Web.Features.Map;
using RouteTrace.Web.Features.Routing;
using RouteTrace.Web.Features.Workspaces;
using RouteTrace.Core.Routing;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped<IWorkspaceStore, IndexedDbWorkspaceStore>();
builder.Services.AddHttpClient<IRoutePlanner, BRouterRoutePlanner>();
builder.Services.AddScoped<GpxImportOperation>();
builder.Services.AddScoped<GpxExportOperation>();
builder.Services.Configure<MapMarkerOptions>(builder.Configuration.GetSection(MapMarkerOptions.SectionName));
builder.Services.Configure<BRouterOptions>(builder.Configuration.GetSection(BRouterOptions.SectionName));

await builder.Build().RunAsync();
