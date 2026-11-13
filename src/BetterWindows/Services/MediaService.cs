using BetterWindows.Core;
using BetterWindows.Logic.Apps;
using BetterWindows.Logic.Messaging;
using global::Windows.Media.Control;
using global::Windows.Storage.Streams;

namespace BetterWindows.Services;

/// <summary>
/// Právě hrající hudba / video přes Windows API pro ovládání médií
/// (GlobalSystemMediaTransportControlsSessionManager) – totéž, co ukazuje Windows u hlasitosti.
/// Funguje s aplikacemi, které se do něj hlásí (Spotify, prohlížeče, Hudba Groove, VLC…).
/// Události WinRT chodí z jiných vláken -> vše se přesouvá do UI vlákna.
/// </summary>
internal sealed class MediaService : IDisposable
{
    private readonly System.Windows.Threading.Dispatcher _dispatcher;
    private readonly AppIdentityResolver _identity;
    private GlobalSystemMediaTransportControlsSessionManager? _manager;
    private GlobalSystemMediaTransportControlsSession? _session;
    private string? _thumbKey;
    private string? _thumbData;
    private bool _refreshing;
    private bool _refreshAgain;

    public MediaStatePayload Current { get; private set; } = Empty;
    /// <summary>AUMID aplikace, která přehrává (jak ho hlásí Windows; např. "Spotify.exe").</summary>
    public string? SourceAumid { get; private set; }
    public event Action<MediaStatePayload>? Changed;

    private static readonly MediaStatePayload Empty = new(false, null, null, null, null, false, null, null, false, false);

    public MediaService(System.Windows.Threading.Dispatcher dispatcher, AppIdentityResolver identity)
    {
        _dispatcher = dispatcher;
        _identity = identity;
    }

    public void Start() => Safe.Fire("Média", async () =>
    {
        _manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
        _manager.CurrentSessionChanged += (_, _) => OnUi(SwitchSession);
        SwitchSession();
        Log.Info("Média: ovládání médií Windows připojeno.");
    });

    public void Command(string command) => Safe.Fire("Média – ovládání", async () =>
    {
        var s = _session;
        if (s is null)
            return;
        var ok = command switch
        {
            "playPause" => await s.TryTogglePlayPauseAsync(),
            "next" => await s.TrySkipNextAsync(),
            "previous" => await s.TrySkipPreviousAsync(),
            _ => false,
        };
        if (!ok)
            Log.Warn($"Média: příkaz {command} aplikace odmítla.");
    });

    private void SwitchSession()
    {
        if (_session is not null)
        {
            _session.MediaPropertiesChanged -= OnSessionChanged;
            _session.PlaybackInfoChanged -= OnSessionChanged;
            _session.TimelinePropertiesChanged -= OnSessionChanged;
        }
        _session = _manager?.GetCurrentSession();
        if (_session is not null)
        {
            _session.MediaPropertiesChanged += OnSessionChanged;
            _session.PlaybackInfoChanged += OnSessionChanged;
            _session.TimelinePropertiesChanged += OnSessionChanged;
        }
        Refresh();
    }

    private void OnSessionChanged<T>(GlobalSystemMediaTransportControlsSession sender, T args) => OnUi(Refresh);

    private void OnUi(Action action) => _dispatcher.InvokeAsync(() => Safe.Run("Média", action));

    private void Refresh()
    {
        // Události chodí v dávkách – jeden běžící dotaz, případně ještě jeden po něm.
        if (_refreshing)
        {
            _refreshAgain = true;
            return;
        }
        Safe.Fire("Média – načtení", RefreshAsync);
    }

    private async Task RefreshAsync()
    {
        _refreshing = true;
        try
        {
            do
            {
                _refreshAgain = false;
                var s = _session;
                if (s is null)
                {
                    Publish(Empty);
                    continue;
                }

                var props = await s.TryGetMediaPropertiesAsync();
                var playback = s.GetPlaybackInfo();
                var timeline = s.GetTimelineProperties();

                SourceAumid = s.SourceAppUserModelId;
                var key = $"{props?.Title}|{props?.Artist}|{s.SourceAppUserModelId}";
                if (key != _thumbKey)
                {
                    _thumbKey = key;
                    _thumbData = props?.Thumbnail is null ? null : await ReadThumbnailAsync(props.Thumbnail);
                }

                double? duration = timeline is null ? null : (timeline.EndTime - timeline.StartTime).TotalSeconds;
                if (duration is <= 0) duration = null;
                double? position = null;
                var playing = playback?.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;
                if (timeline is not null && duration is not null)
                {
                    // Pozici aplikace hlásí jen občas – dopočítat uplynulý čas od posledního hlášení.
                    var pos = timeline.Position.TotalSeconds;
                    if (playing)
                        pos += (DateTimeOffset.Now - timeline.LastUpdatedTime).TotalSeconds;
                    position = Math.Clamp(pos, 0, duration.Value);
                }

                var controls = playback?.Controls;
                Publish(new MediaStatePayload(
                    Available: !string.IsNullOrWhiteSpace(props?.Title),
                    Title: props?.Title,
                    Artist: string.IsNullOrWhiteSpace(props?.Artist) ? props?.AlbumArtist : props.Artist,
                    Source: SourceName(s.SourceAppUserModelId),
                    Thumbnail: _thumbData,
                    Playing: playing,
                    Position: position,
                    Duration: duration,
                    CanPrevious: controls?.IsPreviousEnabled ?? false,
                    CanNext: controls?.IsNextEnabled ?? false));
            }
            while (_refreshAgain);
        }
        finally
        {
            _refreshing = false;
        }
    }

    private static async Task<string?> ReadThumbnailAsync(IRandomAccessStreamReference reference)
    {
        try
        {
            using var stream = await reference.OpenReadAsync();
            if (stream.Size == 0 || stream.Size > 1_000_000)
                return null;
            using var reader = new DataReader(stream);
            await reader.LoadAsync((uint)stream.Size);
            var bytes = new byte[stream.Size];
            reader.ReadBytes(bytes);
            return $"data:image/png;base64,{Convert.ToBase64String(Downscale(bytes, 160))}";
        }
        catch (Exception ex)
        {
            Log.Warn($"Média: obal alba nejde načíst ({ex.Message})");
            return null;
        }
    }

    /// <summary>
    /// Obal alba zmenšený na max. 160 px (PNG, desítky kB) – posílá se všem stránkám (lišta, ostrůvek,
    /// zámek) při každé změně stavu, originál může mít i několik MB.
    /// </summary>
    private static byte[] Downscale(byte[] image, int max)
    {
        using var input = new System.IO.MemoryStream(image);
        var frame = System.Windows.Media.Imaging.BitmapFrame.Create(input,
            System.Windows.Media.Imaging.BitmapCreateOptions.None, System.Windows.Media.Imaging.BitmapCacheOption.OnLoad);
        var scale = Math.Min(1.0, (double)max / Math.Max(frame.PixelWidth, frame.PixelHeight));
        System.Windows.Media.Imaging.BitmapSource source = frame;
        if (scale < 1.0)
            source = new System.Windows.Media.Imaging.TransformedBitmap(frame, new System.Windows.Media.ScaleTransform(scale, scale));
        var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
        encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(source));
        using var output = new System.IO.MemoryStream();
        encoder.Save(output);
        return output.ToArray();
    }

    private string SourceName(string? source)
    {
        if (string.IsNullOrEmpty(source))
            return "";
        if (source.Contains('!'))
            return _identity.GetName(AppId.FromAumid(source));
        return source.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? source[..^4] : source;
    }

    private void Publish(MediaStatePayload state)
    {
        if (state == Current)
            return;
        Current = state;
        Changed?.Invoke(state);
    }

    public void Dispose()
    {
        if (_session is not null)
        {
            _session.MediaPropertiesChanged -= OnSessionChanged;
            _session.PlaybackInfoChanged -= OnSessionChanged;
            _session.TimelinePropertiesChanged -= OnSessionChanged;
        }
    }
}
