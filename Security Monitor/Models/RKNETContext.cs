using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Security_Monitor.Models
{
    public class RKNETContext : DbContext
    {
        public DbSet<FactoryPerson> FactoryPerson { get; set; }
        public DbSet<FactoryDepartment> FactoryDepartment { get; set; }
        public DbSet<FactoryWorkshop> FactoryWorkshop { get; set; }
        public DbSet<FactoryJobTitle> FactoryJobTitle { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer("Data Source=RKSQL.shzhleb.ru\\SQL2019;Initial Catalog=RKNET;User ID=rk7;Password=wZSbs6NKl2SF;TrustServerCertificate=True");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<FactoryPerson>()
                .ToTable(tb => tb.UseSqlOutputClause(false));
        }

    }

    public class FactoryPerson
    {
        public int Id { get; set; }
        public string Surname { get; set; } = "";
        public string Name { get; set; } = "";
        public string? Patronymic { get; set; }
        public DateTime Birthdate { get; set; }
        public string Passport { get; set; } = "";
        public int FactoryDepartment { get; set; }
        public int FactoryWorkshop { get; set; }
        public int FactoryJobTitle { get; set; }
        public string? PassCardNumber { get; set; }
        public string? Photo { get; set; }
        public bool? Fake { get; set; }
    }

    public class FactoryDepartment
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    public class FactoryWorkshop
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    public class FactoryJobTitle
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }
}

