namespace muzzstream.Models;

public class AlbumModel {
    public int album_id { get; set; }
    public string album_name { get; set; } = string.Empty;
    public int release_year { get; set; }
}

public class CompositionModel {
    public int composition_id { get; set; }
    public string composition_name { get; set; } = string.Empty;
    public int release_year { get; set; }
}

public class TrackModel {
    public int track_id { get; set; }
    public int composition_id { get; set; }
    public int? album_id { get; set; }
    public string filepath { get; set; } = string.Empty;
    public int duration { get; set; }
    public string version_type { get; set; } = "Studio";

    public string composition_name { get; set; } = string.Empty;
    public string authors_list { get; set; } = string.Empty;
    public string album_name { get; set; } = string.Empty;

    public string formatted_duration => $"{duration / 60}:{duration % 60:D2}";

    private string _uiPlayIcon = "?";
    public string ui_play_icon {
        get => _uiPlayIcon;
        set {
            _uiPlayIcon = value;
        }
    }
}
