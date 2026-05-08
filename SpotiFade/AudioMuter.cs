using System.Diagnostics;
using NAudio.CoreAudioApi;

namespace SpotiFade;

/// <summary>
/// Mutes / unmutes Spotify's audio session via the Windows Audio Session API
/// (WASAPI), so only Spotify is silenced — system sounds and other apps keep
/// playing normally. Iterates every render endpoint because Spotify can be
/// routed to a non-default device.
/// </summary>
internal sealed class AudioMuter
{
    private const string SpotifyProcessName = "Spotify";
    private bool _muted;

    public void MuteSpotify()
    {
        if (Apply(true)) _muted = true;
    }

    public void RestoreIfMuted()
    {
        if (!_muted) return;
        Apply(false);
        _muted = false;
    }

    private static bool Apply(bool mute)
    {
        var anyApplied = false;
        try
        {
            using var enumerator = new MMDeviceEnumerator();
            var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
            foreach (var device in devices)
            {
                try
                {
                    var sessions = device.AudioSessionManager.Sessions;
                    for (int i = 0; i < sessions.Count; i++)
                    {
                        var session = sessions[i];
                        if (!IsSpotify(session)) continue;
                        try
                        {
                            session.SimpleAudioVolume.Mute = mute;
                            anyApplied = true;
                        }
                        catch
                        {
                            // session may have torn down between enumeration and access
                        }
                    }
                }
                finally
                {
                    device.Dispose();
                }
            }
        }
        catch
        {
            // No audio device available, or COM glitch — caller treats as no-op.
        }
        return anyApplied;
    }

    private static bool IsSpotify(AudioSessionControl session)
    {
        try
        {
            var pid = (int)session.GetProcessID;
            if (pid <= 0) return false;
            using var process = Process.GetProcessById(pid);
            return string.Equals(
                process.ProcessName,
                SpotifyProcessName,
                StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}
