using System;

namespace muzzstream.Models;

public class PlaylistModel {
    public int playlist_id { get; set; }
    public int user_id { get; set; }
    public string playlist_name { get; set; } = string.Empty;
    public bool is_public { get; set; }
}

public class ListeningHistoryModel {
    public int history_id { get; set; }
    public int user_id { get; set; }
    public int track_id { get; set; }
    public DateTime listened_at { get; set; }
    public int duration { get; set; }
}
