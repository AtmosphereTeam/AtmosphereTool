using AtmosphereTool.Contracts.Services;

namespace AtmosphereTool.Services;
public class BackdropService(WindowEx window) : IBackdropService
{
    public string CurrentBackdrop
    {
        get
        {
            var backdrop = string.Empty;
            //_window.DispatcherQueue.TryEnqueue(() =>
            //{
            if (window is MainWindow mainWindow)
            {
                backdrop = mainWindow.currentBackdrop;
            }
            //});
            return backdrop;
        }
    }

    public void SetBackdrop(string tag)
    {
        window.DispatcherQueue.TryEnqueue(() =>
        {
            if (window is MainWindow mainWindow)
            {
                mainWindow.SetBackdrop(tag);
            }
        });
    }
}

