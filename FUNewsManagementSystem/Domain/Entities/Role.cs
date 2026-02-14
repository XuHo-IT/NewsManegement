using System;
using System.Collections.Generic;

namespace FUNewsManagementSystem.Domain.Entities;

public partial class Role
{
    public int RoleID { get; set; }

    public string RoleName { get; set; } = null!;

    public virtual ICollection<SystemAccount> SystemAccounts { get; set; } = new List<SystemAccount>();
}
