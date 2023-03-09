using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ProudPoppy.Models;

namespace ProudPoppy.Data
{
    public class ProudPoppyContext : DbContext
    {
        public ProudPoppyContext (DbContextOptions<ProudPoppyContext> options)
            : base(options)
        {
        }

        public DbSet<User> User { get; set; } = default!;
        public DbSet<ProductDetails> ProductDetails { get; set; } = default!;
    }
}
