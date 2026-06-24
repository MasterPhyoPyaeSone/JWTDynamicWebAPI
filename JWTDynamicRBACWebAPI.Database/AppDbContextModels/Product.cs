using System;
using System.Collections.Generic;

namespace JWTDynamicRBACWebAPI.Database.AppDbContextModels;

public partial class Product
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public decimal Price { get; set; }

    public string? Description { get; set; }
}
