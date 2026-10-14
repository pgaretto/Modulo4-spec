using Microsoft.EntityFrameworkCore;
using SmartAgenda.Api.Auth;
using SmartAgenda.Api.Domain;

namespace SmartAgenda.Api.Data;

/// <summary>
/// Contexto de datos. Todas las consultas se filtran por el usuario autenticado (RNF-03, Principio II):
/// un usuario nunca ve ni modifica datos de otro, aunque el código de un endpoint se olvide de filtrar.
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUser currentUser) : DbContext(options)
{
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<Category> Categories => Set<Category>();

    // Los filtros leen estas propiedades en cada consulta (no se congela el usuario al construir el modelo).
    private string? CurrentUserId => currentUser.UserId;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Event>(e =>
        {
            e.Property(x => x.UserId).HasMaxLength(128);
            e.Property(x => x.Title).HasMaxLength(Event.TitleMaxLength);
            e.HasIndex(x => new { x.UserId, x.Date });
            e.HasQueryFilter(x => x.UserId == CurrentUserId);
        });

        modelBuilder.Entity<Expense>(e =>
        {
            e.Property(x => x.UserId).HasMaxLength(128);
            e.Property(x => x.Amount).HasColumnType("decimal(18,2)");
            e.HasIndex(x => new { x.UserId, x.Date });
            e.HasOne<Category>().WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
            e.HasQueryFilter(x => x.UserId == CurrentUserId);
        });

        modelBuilder.Entity<Category>(e =>
        {
            e.Property(x => x.UserId).HasMaxLength(128);
            e.Property(x => x.Name).HasMaxLength(Category.NameMaxLength);
            // Único por usuario; en SQL Server la comparación no distingue mayúsculas (collation por defecto).
            e.HasIndex(x => new { x.UserId, x.Name }).IsUnique();
            // Las del sistema (UserId nulo) las ve todo el mundo; las propias, solo su dueño.
            e.HasQueryFilter(x => x.UserId == null || x.UserId == CurrentUserId);
            e.HasData(Category.System);
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        AssertOwnership();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        AssertOwnership();
        return base.SaveChanges();
    }

    /// <summary>Nada se guarda ni se modifica a nombre de otro usuario, ni sin sesión.</summary>
    private void AssertOwnership()
    {
        foreach (var entry in ChangeTracker.Entries().Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
        {
            var owner = entry.Entity switch
            {
                Event ev => ev.UserId,
                Expense ex => ex.UserId,
                Category c => c.UserId,
                _ => null,
            };

            // Las categorías del sistema (UserId nulo) vienen de la migración; en tiempo de ejecución nadie las toca.
            if (CurrentUserId is null || owner != CurrentUserId)
            {
                throw new InvalidOperationException("Operación rechazada: el registro no pertenece al usuario autenticado.");
            }
        }
    }
}
