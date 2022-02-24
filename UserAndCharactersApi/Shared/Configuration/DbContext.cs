using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using UserWithCharacterVisibility.Models;

namespace UserWithCharacterVisibility {

  public abstract class DbContext<TUser, TCharacter> : IdentityDbContext<TUser>
    where TCharacter : Character<TUser, TCharacter>
    where TUser : User<TUser, TCharacter>
  {

    public DbSet<TCharacter> Characters {
      get;
      set;
    }

    public DbSet<VisibilitySettings<TUser, TCharacter>> VisibilitySettings {
      get;
      set;
    } 

    protected DbContext(DbContextOptions options) :
      base(options) {}

    protected override void OnModelCreating(ModelBuilder builder) {
      base.OnModelCreating(builder);
      builder.Entity<TUser>().HasOne(u => u.VisibilityOptions);
      builder.Entity<TCharacter>().HasOne(c => c.VisibilityOptions);
      builder.Entity<VisibilitySettings<TUser, TCharacter>>()
        .HasMany(vs => vs.Whitelist);
      builder.Entity<VisibilitySettings<TUser, TCharacter>>()
        .HasMany(vs => vs.Blacklist);
    }
  }
}
