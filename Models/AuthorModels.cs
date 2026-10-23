using System;

namespace muzzstream.Models;

public class AuthorModel {
    public int author_id { get; set; }
    public string author_name { get; set; } = string.Empty;
    public string? country { get; set; }
    public int current_status { get; set; }
    public DateTime? status_until { get; set; }
    public int? next_status { get; set; }
    public int? created_by_user_id { get; set; }
}

public class AuthorMemberModel {
    public int author_id { get; set; }
    public int user_id { get; set; }
    public int group_role_id { get; set; }
}
