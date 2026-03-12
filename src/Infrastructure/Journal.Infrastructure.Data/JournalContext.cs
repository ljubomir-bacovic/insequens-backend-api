using Journal.Domain.Entities;
using Journal.Infrastructure.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Journal.Domain.Data;

public class JournalContext : IdentityDbContext<ApplicationUser>
{
    public required DbSet<ToDoItem> ToDoItems { get; set; }


    public JournalContext(DbContextOptions<JournalContext> options) : base(options)
    {

    }
    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<ToDoItem>().HasKey(e => e.Id);
        builder.Entity<ToDoItem>().ToTable(nameof(ToDoItem));

        base.OnModelCreating(builder);
    }

}
