namespace RouteTrace.Web.Features.Commands;

public sealed class ApplicationCommand(Func<bool> canExecute, Func<Task> execute)
{
    public bool CanExecute => canExecute();

    public async Task<bool> TryExecuteAsync()
    {
        if (!CanExecute)
        {
            return false;
        }

        await execute();
        return true;
    }
}
