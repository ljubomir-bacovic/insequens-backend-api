using Insequens.Application.Abstractions;
using Insequens.Domain.Entities;
using Insequens.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Insequens.Infrastructure.Persistence;

public class InsequensContext(DbContextOptions<InsequensContext> options)
    : IdentityDbContext<ApplicationUser>(options), IApplicationDbContext
{
    public DbSet<ToDoItem> ToDoItems => Set<ToDoItem>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(InsequensContext).Assembly);
    }
}
