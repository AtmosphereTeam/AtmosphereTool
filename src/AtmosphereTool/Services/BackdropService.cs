using AtmosphereTool.Contracts.Services;

namespace AtmosphereTool.Services;
public class BackdropService : IBackdropService
{
    private readonly WindowEx _window;

    public BackdropService(WindowEx window)
    {
        _window = window;
    }

    public string CurrentBackdrop
    {
        get
        {
            var backdrop = string.Empty;
            //_window.DispatcherQueue.TryEnqueue(() =>
            //{
            if (_window is MainWindow mainWindow)
            {
                backdrop = mainWindow.currentBackdrop;
            }
            //});
            return backdrop;
        }
    }

    public void SetBackdrop(string tag)
    {
        _window.DispatcherQueue.TryEnqueue(() =>
        {
            if (_window is MainWindow mainWindow)
            {
                mainWindow.SetBackdrop(tag);
            }
        });
    }
}

