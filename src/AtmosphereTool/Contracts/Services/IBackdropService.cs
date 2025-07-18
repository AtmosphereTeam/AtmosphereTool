namespace AtmosphereTool.Contracts.Services;

public interface IBackdropService
{
    string CurrentBackdrop
    {
        get;
    }
    void SetBackdrop(string tag);
}