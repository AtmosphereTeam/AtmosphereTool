using System.Runtime.InteropServices;

namespace AtmosphereTool
{
    public partial class WindowsSystemDispatcherQueueHelper
    {
        [StructLayout(LayoutKind.Sequential)]
        struct DispatcherQueueOptions
        {
            public int dwSize;
            public int threadType;
            public int apartmentType;
        }

        [LibraryImport("CoreMessaging.dll")]
        private static partial int CreateDispatcherQueueController(DispatcherQueueOptions options, out IntPtr dispatcherQueueController);

        private IntPtr _dispatcherQueueController = IntPtr.Zero;

        public void EnsureWindowsSystemDispatcherQueueController()
        {
            if (Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread() != null)
                return;

            DispatcherQueueOptions options = new()
            {
                dwSize = Marshal.SizeOf<DispatcherQueueOptions>(),
                threadType = 2, // DQTYPE_THREAD_CURRENT
                apartmentType = 2 // DQTAT_COM_STA
            };

            CreateDispatcherQueueController(options, out _dispatcherQueueController);
        }
    }
}
