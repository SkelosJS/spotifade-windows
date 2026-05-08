using System.Threading;
using System.Windows.Forms;

namespace SpotiFade;

internal static class Program
{
    // Single-instance guard so a user double-clicking the icon doesn't end up
    // with two muters fighting each other.
    private static Mutex? _singleInstance;

    [STAThread]
    private static void Main()
    {
        _singleInstance = new Mutex(initiallyOwned: true, name: "SpotiFade.SingleInstance", out bool createdNew);
        if (!createdNew)
        {
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new TrayApp());

        GC.KeepAlive(_singleInstance);
    }
}
