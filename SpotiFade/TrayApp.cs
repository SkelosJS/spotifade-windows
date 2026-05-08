using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace SpotiFade;

/// <summary>
/// Tray-icon lifetime owner. Wires the SMTC monitor to the audio muter and
/// keeps the user informed via the icon tooltip + a status menu item.
/// </summary>
internal sealed class TrayApp : ApplicationContext
{
    private readonly NotifyIcon _icon;
    private readonly ToolStripMenuItem _statusItem;
    private readonly AudioMuter _muter;
    private readonly SpotifyMonitor _monitor;

    public TrayApp()
    {
        _muter = new AudioMuter();
        _monitor = new SpotifyMonitor(_muter);
        _monitor.StatusChanged += OnStatusChanged;

        _statusItem = new ToolStripMenuItem("Initialisation…") { Enabled = false };
        var startupItem = new ToolStripMenuItem("Lancer au démarrage")
        {
            CheckOnClick = true,
            Checked = Startup.IsEnabled(),
        };
        startupItem.CheckedChanged += (_, _) =>
        {
            if (startupItem.Checked) Startup.Enable();
            else Startup.Disable();
        };

        var menu = new ContextMenuStrip();
        menu.Items.Add(_statusItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(startupItem);
        menu.Items.Add("Ouvrir le log de debug", null, (_, _) => OpenLog());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Quitter SpotiFade", null, (_, _) => Exit());

        _icon = new NotifyIcon
        {
            Icon = LoadAppIcon(),
            Visible = true,
            Text = "SpotiFade",
            ContextMenuStrip = menu,
        };
        _icon.DoubleClick += (_, _) => menu.Show(Cursor.Position);

        _ = _monitor.StartAsync();
    }

    private void OnStatusChanged(string status)
    {
        if (_statusItem.GetCurrentParent() is { } host && host.InvokeRequired)
        {
            host.BeginInvoke(() => OnStatusChanged(status));
            return;
        }

        _statusItem.Text = status;
        // NotifyIcon.Text is hard-capped at 63 characters by Windows.
        var tooltip = $"SpotiFade — {status}";
        _icon.Text = tooltip.Length > 63 ? tooltip[..63] : tooltip;
    }

    private static Icon LoadAppIcon()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith("app.ico", StringComparison.OrdinalIgnoreCase));

        if (resourceName != null)
        {
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream != null) return new Icon(stream);
        }

        var fallback = Path.Combine(AppContext.BaseDirectory, "app.ico");
        return File.Exists(fallback) ? new Icon(fallback) : SystemIcons.Application;
    }

    private static void OpenLog()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = Logger.FilePath,
                UseShellExecute = true,
            });
        }
        catch
        {
            // ignore
        }
    }

    private void Exit()
    {
        try
        {
            _monitor.Dispose();
            _muter.RestoreIfMuted();
        }
        finally
        {
            _icon.Visible = false;
            _icon.Dispose();
            ExitThread();
        }
    }
}
