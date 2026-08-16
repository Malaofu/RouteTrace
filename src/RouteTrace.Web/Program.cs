using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using RouteTrace.Web;
using RouteTrace.Web.Features.Map;
using RouteTrace.Web.Features.Workspaces;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<IWorkspaceStore, IndexedDbWorkspaceStore>();
builder.Services.Configure<MapMarkerOptions>(builder.Configuration.GetSection(MapMarkerOptions.SectionName));

await builder.Build().RunAsync();
