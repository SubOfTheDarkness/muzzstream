using System.Collections.Generic;
using System.Linq;

namespace muzzstream.Models;

public static class AppSession {
    public static List<RoleModel> StaticRoles { get; set; } = new();
    public static List<GenreModel> StaticGenres { get; set; } = new();
    public static List<GroupRoleModel> StaticGroupRoles { get; set; } = new();
    public static List<AuthorStatusModel> StaticAuthorStatuses { get; set; } = new();

    public static UserModel? CurrentUser { get; set; }

    public static string RoleText {
        get {
            if (CurrentUser == null) return "Гость";
            int searchId = CurrentUser.role_id == 0 ? 3 : CurrentUser.role_id;
            return StaticRoles.FirstOrDefault(r => r.role_id == searchId)?.role_name ?? "Error";
        }
    }
}
