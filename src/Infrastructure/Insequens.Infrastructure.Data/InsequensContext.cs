using Insequens.Domain.Entities;
using Insequens.Infrastructure.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Insequens.Domain.Data;

public class InsequensContext : IdentityDbContext<ApplicationUser>
{
    public required DbSet<ToDoItem> ToDoItems { get; set; }


    public InsequensContext(DbContextOptions<InsequensContext> options) : base(options)
    {

    }
    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<ToDoItem>().HasKey(e => e.Id);
        builder.Entity<ToDoItem>().ToTable(nameof(ToDoItem));

        base.OnModelCreating(builder);
    }

}
