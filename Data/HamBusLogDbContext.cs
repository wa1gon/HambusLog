using HamBlocks.Library.Models;
using Microsoft.EntityFrameworkCore;

namespace HamBusLog.Data;

/// <summary>
/// EF Core DbContext for HamBusLog. Supports SQLite and PostgreSQL.
/// Configure the provider via <see cref="HamBusLogDbContextFactory"/>.
/// </summary>
public sealed class HamBusLogDbContext : DbContext
{
    public HamBusLogDbContext(DbContextOptions<HamBusLogDbContext> options)
        : base(options) { }

    public DbSet<Qso> Qsos => Set<Qso>();
    public DbSet<QsoDetail> QsoDetails => Set<QsoDetail>();
    public DbSet<QsoQslInfo> QsoQslInfos => Set<QsoQslInfo>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Qso>(qso =>
        {
            qso.ToTable("Qsos");
            qso.HasKey(q => q.Id);

            qso.Property(q => q.Id).ValueGeneratedOnAdd();
            qso.Property(q => q.Call).HasMaxLength(20).IsRequired();
            qso.Property(q => q.MyCall).HasMaxLength(20);
            qso.Property(q => q.Band).HasMaxLength(10);
            qso.Property(q => q.Mode).HasMaxLength(20);
            qso.Property(q => q.Freq).HasPrecision(12, 6);
            qso.Property(q => q.RstSent).HasMaxLength(10);
            qso.Property(q => q.RstRcvd).HasMaxLength(10);
            qso.Property(q => q.Country).HasMaxLength(100);
            qso.Property(q => q.State).HasMaxLength(10);
            qso.Property(q => q.ContestId).HasMaxLength(50);

            qso.HasMany(q => q.Details)
               .WithOne(d => d.Qso)
               .HasForeignKey(d => d.QsoId)
               .OnDelete(DeleteBehavior.Cascade);

            qso.HasMany(q => q.QslInfo)
               .WithOne(s => s.Qso)
               .HasForeignKey(s => s.QsoId)
               .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<QsoDetail>(detail =>
        {
            detail.ToTable("QsoDetails");
            detail.HasKey(d => d.Id);
            detail.Property(d => d.Id).ValueGeneratedOnAdd();
            detail.Property(d => d.FieldName).HasMaxLength(100).IsRequired();
            detail.Property(d => d.FieldValue).HasMaxLength(500);
        });

        modelBuilder.Entity<QsoQslInfo>(qsl =>
        {
            qsl.ToTable("QsoQslInfos");
            qsl.HasKey(q => q.Id);
            qsl.Property(q => q.Id).ValueGeneratedOnAdd();
            qsl.Property(q => q.QslService).HasMaxLength(50);
        });
    }
}

