using AtmosphereTool.Contracts.Services;
using AtmosphereTool.ViewModels;

using Microsoft.UI.Xaml;

namespace AtmosphereTool.Activation;

public class DefaultActivationHandler(INavigationService navigationService) : ActivationHandler<LaunchActivatedEventArgs>
{
    protected override bool CanHandleInternal(LaunchActivatedEventArgs args)
    {
        // None of the ActivationHandlers has handled the activation.
        return navigationService.Frame?.Content == null;
    }

    protected async override Task HandleInternalAsync(LaunchActivatedEventArgs args)
    {
        navigationService.NavigateTo(typeof(MainViewModel).FullName!, args.Arguments);

        await Task.CompletedTask;
    }
}
