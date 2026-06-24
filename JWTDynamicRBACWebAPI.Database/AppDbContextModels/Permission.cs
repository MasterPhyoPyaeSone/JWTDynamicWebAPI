using System;
using System.Collections.Generic;

namespace JWTDynamicRBACWebAPI.Database.AppDbContextModels;

public partial class Permission
{
    public int Id { get; set; }

    public string PermissionName { get; set; } = null!;

    public virtual ICollection<Role> Roles { get; set; } = new List<Role>();
}
