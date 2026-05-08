using Windows.Foundation;
using Windows.Media.Control;

namespace SpotiFade;

/// <summary>
/// Listens to the system media-transport sessions (SMTC), filters in the
/// Spotify session, and toggles the muter according to ad detection on the
/// session's media properties.
/// </summary>
internal sealed class SpotifyMonitor : IDisposable
{
    private static readonly string[] AdTitleKeywords =
    {
        "advertisement",
        "annonce",         // fr
        "anuncio",         // es / pt
        "werbung",         // de
        "pubblicità",      // it
        "reklam",          // tr / sv
        "reklama",         // pl
        "广告",             // zh
        "広告",             // ja
        "광고",             // ko
        "spotify ad",
    };

    private readonly AudioMuter _muter;
    private GlobalSystemMediaTransportControlsSessionManager? _manager;
    private GlobalSystemMediaTransportControlsSession? _session;
    private TypedEventHandler<GlobalSystemMediaTransportControlsSession, MediaPropertiesChangedEventArgs>? _onPropsChanged;
    private bool _isAd;
    private bool _disposed;

    public event Action<string>? StatusChanged;

    public SpotifyMonitor(AudioMuter muter)
    {
        _muter = muter;
    }

    public async Task StartAsync()
    {
        try
        {
            Logger.Write("starting SpotifyMonitor");
            _manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            _manager.SessionsChanged += (_, _) => RebindSession();
            RebindSession();
        }
        catch (Exception ex)
        {
            Logger.Write($"start error: {ex}");
            StatusChanged?.Invoke($"Erreur: {ex.Message}");
        }
    }

    private void RebindSession()
    {
        if (_disposed || _manager == null) return;

        var sessions = _manager.GetSessions();
        GlobalSystemMediaTransportControlsSession? next = null;
        foreach (var s in sessions)
        {
            var id = s.SourceAppUserModelId ?? string.Empty;
            if (id.Contains("Spotify", StringComparison.OrdinalIgnoreCase))
            {
                next = s;
                break;
            }
        }

        if (ReferenceEquals(next, _session)) return;

        DetachSession();
        _session = next;

        if (_session == null)
        {
            Logger.Write("no Spotify session");
            if (_isAd)
            {
                _muter.RestoreIfMuted();
                _isAd = false;
            }
            StatusChanged?.Invoke("En attente de Spotify…");
            return;
        }

        Logger.Write($"bound session: AUMID='{_session.SourceAppUserModelId}'");
        _onPropsChanged = async (s, _) => await HandleMetadataAsync(s);
        _session.MediaPropertiesChanged += _onPropsChanged;
        StatusChanged?.Invoke("Actif — guettant les pubs");
        _ = HandleMetadataAsync(_session);
    }

    private void DetachSession()
    {
        if (_session != null && _onPropsChanged != null)
        {
            try { _session.MediaPropertiesChanged -= _onPropsChanged; } catch { }
        }
        _onPropsChanged = null;
    }

    private async Task HandleMetadataAsync(GlobalSystemMediaTransportControlsSession session)
    {
        try
        {
            var props = await session.TryGetMediaPropertiesAsync();
            if (props == null)
            {
                Logger.Debug("metadata: <null>");
                return;
            }

            var title = props.Title ?? string.Empty;
            var artist = props.Artist ?? string.Empty;
            var album = props.AlbumTitle ?? string.Empty;
            var subtitle = props.Subtitle ?? string.Empty;
            var trackNumber = props.TrackNumber;
            var albumArtist = props.AlbumArtist ?? string.Empty;
            var genres = props.Genres != null ? string.Join(",", props.Genres) : string.Empty;

            Logger.Debug(
                $"metadata: title='{title}' artist='{artist}' album='{album}' " +
                $"subtitle='{subtitle}' albumArtist='{albumArtist}' " +
                $"trackNumber={trackNumber} genres='{genres}'");

            var isAd = LooksLikeAd(title, artist, album, subtitle, albumArtist, trackNumber);

            var displayTrack = string.IsNullOrWhiteSpace(title)
                ? "(titre vide)"
                : $"{Truncate(title, 50)}";

            if (isAd && !_isAd)
            {
                _isAd = true;
                _muter.MuteSpotify();
                Logger.Write($"-> ad detected, muting");
                StatusChanged?.Invoke($"Pub coupée: {displayTrack}");
            }
            else if (!isAd && _isAd)
            {
                _muter.RestoreIfMuted();
                _isAd = false;
                Logger.Write($"-> ad ended, unmuting");
                StatusChanged?.Invoke($"Actif — {displayTrack}");
            }
            else
            {
                StatusChanged?.Invoke(_isAd
                    ? $"Pub coupée: {displayTrack}"
                    : $"Actif — {displayTrack}");
            }
        }
        catch (Exception ex)
        {
            Logger.Write($"metadata error: {ex}");
            StatusChanged?.Invoke($"Erreur metadata: {ex.Message}");
        }
    }

    private static bool LooksLikeAd(
        string title, string artist, string album, string subtitle,
        string albumArtist, int trackNumber)
    {
        // Strongest signal on the Windows client: Spotify's own promo / ad
        // entries set artist (or albumArtist) to literally "Spotify".
        if (IsSpotifyOwned(artist) || IsSpotifyOwned(albumArtist))
        {
            return true;
        }

        // Localized title keywords (covers Android-style "Annonce" /
        // "Advertisement" / etc — useful as a safety net).
        if (TitleHasAdKeyword(title) || TitleHasAdKeyword(artist)
            || TitleHasAdKeyword(album) || TitleHasAdKeyword(subtitle))
        {
            return true;
        }

        // Inter-ad transition: Spotify briefly emits a placeholder with no
        // artist, no album, track number 0 and a title that is empty or just
        // a dash glyph.
        if (string.IsNullOrWhiteSpace(artist)
            && string.IsNullOrWhiteSpace(album)
            && trackNumber == 0
            && IsPlaceholderTitle(title))
        {
            return true;
        }

        return false;
    }

    private static bool IsSpotifyOwned(string s) =>
        string.Equals(s.Trim(), "Spotify", StringComparison.OrdinalIgnoreCase);

    private static bool TitleHasAdKeyword(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return false;
        var lower = s.ToLowerInvariant();
        foreach (var kw in AdTitleKeywords)
        {
            if (lower.Contains(kw)) return true;
        }
        return false;
    }

    private static bool IsPlaceholderTitle(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return true;
        var trimmed = s.Trim();
        // Em dash, en dash, hyphen, ellipsis: Spotify uses any of these as a
        // gap-filler between or before ads.
        return trimmed is "—" or "–" or "-" or "..." or "…";
    }

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s[..max] + "…";

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        DetachSession();
        _session = null;
        _manager = null;
    }
}
