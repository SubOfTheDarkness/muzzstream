namespace muzzstream.Models;

public class TrackModel
{
    public int track_id { get; set; }
    public string composition_name { get; set; } = string.Empty;
    public string authors_list { get; set; } = string.Empty;
    public string album_name { get; set; } = string.Empty;
    public int duration { get; set; }
}
