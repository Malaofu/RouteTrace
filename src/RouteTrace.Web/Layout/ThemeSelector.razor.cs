using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace RouteTrace.Web.Layout;

public partial class ThemeSelector : IAsyncDisposable
{
    [Inject] private IJSRuntime JavaScript { get; set; } = null!;

    private static readonly ThemeOption[] Options =
    [
        new("light", "Light", "☀"),
        new("dark", "Dark", "☾"),
        new("auto", "Auto", "◐")
    ];

    private IJSObjectReference? module;
    private string preference = "auto";

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        module = await JavaScript.InvokeAsync<IJSObjectReference>(
            "import",
            "./generated/theme.js");
        preference = await module.InvokeAsync<string>("initialize");
        StateHasChanged();
    }

    private async Task SetThemeAsync(string value)
    {
        preference = value;
        if (module is not null)
        {
            await module.InvokeVoidAsync("setPreference", value);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (module is null)
        {
            return;
        }

        try
        {
            await module.InvokeVoidAsync("dispose");
            await module.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
            // The browser has already disconnected, so there is nothing to clean up.
        }
    }

    private sealed record ThemeOption(string Value, string Label, string Icon);
}
