namespace muzzstream.Models;

public class UserModel {
    public int user_id { get; set; }
    public string nickname { get; set; } = string.Empty;
    public string username { get; set; } = string.Empty;
    public string email { get; set; } = string.Empty;
    
    public string token { get; set; } = string.Empty;
    
    private int _role_id;
    public int role_id { 
        get => _role_id;
        set {
            _role_id = value == 3 ? 0 : value;
        }
    } 
}
