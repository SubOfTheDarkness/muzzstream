namespace muzzstream.Models;

public class RoleModel {
    public int role_id { get; set; }
    public string role_name { get; set; } = string.Empty;
}

public class GenreModel {
    public int genre_id { get; set; }
    public string genre_name { get; set; } = string.Empty;
}

public class GroupRoleModel {
    public int group_role_id { get; set; }
    public string role_name { get; set; } = string.Empty; 
}

public class AuthorStatusModel {
    public int status_id { get; set; }
    public string status_text { get; set; } = string.Empty; 
}
