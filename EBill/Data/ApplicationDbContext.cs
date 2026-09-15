using EBill.Models;
using Microsoft.EntityFrameworkCore;

namespace EBill.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Company> Companies => Set<Company>();
        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<Invoice> Invoices => Set<Invoice>();
        public DbSet<Quotation> Quotations => Set<Quotation>();
        public DbSet<Tax> Taxes => Set<Tax>();
        public DbSet<AccountingLink> AccountingLinks => Set<AccountingLink>();
        public DbSet<Settings> Settings => Set<Settings>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Company>()
                .HasIndex(c => c.Email)
                .IsUnique();

            modelBuilder.Entity<Customer>()
                .HasIndex(c => c.CustomerNumber)
                .IsUnique();

            modelBuilder.Entity<Customer>()
                .HasOne(c => c.Company)
                .WithMany()
                .HasForeignKey(c => c.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);

            // Phone number is mandatory (see Customer.cs) and unique per company - the
            // same number can belong to different customers of different companies,
            // just not twice within one company's own customer list.
            modelBuilder.Entity<Customer>()
                .HasIndex(c => new { c.CompanyId, c.PhoneNumber })
                .IsUnique();

            // One Settings row per Ebill company - Stock Management's connection info,
            // if any (see Models/Settings.cs).
            modelBuilder.Entity<Settings>()
                .HasIndex(s => s.CompanyId)
                .IsUnique();

            modelBuilder.Entity<Settings>()
                .HasOne(s => s.Company)
                .WithOne(c => c.Settings)
                .HasForeignKey<Settings>(s => s.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Invoice>()
                .HasIndex(i => i.BillNumber)
                .IsUnique();

            modelBuilder.Entity<Quotation>()
                .HasIndex(q => q.QuotationNumber)
                .IsUnique();

            modelBuilder.Entity<Invoice>()
                .Property(i => i.PaymentStatus)
                .HasConversion<string>()
                .HasMaxLength(20);

            modelBuilder.Entity<Customer>()
                .HasMany(c => c.Products)
                .WithOne(p => p.Customer)
                .HasForeignKey(p => p.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Customer>()
                .HasMany(c => c.Invoices)
                .WithOne(i => i.Customer)
                .HasForeignKey(i => i.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Customer>()
                .HasMany(c => c.Taxes)
                .WithOne(t => t.Customer)
                .HasForeignKey(t => t.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Customer>()
                .HasMany(c => c.Quotations)
                .WithOne(q => q.Customer)
                .HasForeignKey(q => q.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Invoice>()
                .HasMany(i => i.Products)
                .WithOne(p => p.Invoice)
                .HasForeignKey(p => p.InvoiceId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Invoice>()
                .HasMany(i => i.Taxes)
                .WithOne(t => t.Invoice)
                .HasForeignKey(t => t.InvoiceId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Quotation>()
                .HasMany(q => q.Products)
                .WithOne(p => p.Quotation)
                .HasForeignKey(p => p.QuotationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Quotation>()
                .HasMany(q => q.Invoices)
                .WithOne(i => i.Quotation)
                .HasForeignKey(i => i.QuotationId)
                .OnDelete(DeleteBehavior.Restrict);

            // Id is generated in application code (Guid.NewGuid()), not by the database.
            modelBuilder.Entity<AccountingLink>()
                .Property(a => a.Id)
                .ValueGeneratedNever();

            foreach (var property in modelBuilder.Model.GetEntityTypes()
                .SelectMany(t => t.GetProperties())
                .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
            {
                property.SetColumnType("decimal(18,2)");
            }
        }

        public override int SaveChanges()
        {
            ApplyAuditInfo();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            ApplyAuditInfo();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void ApplyAuditInfo()
        {
            var now = DateTime.UtcNow;

            foreach (var entry in ChangeTracker.Entries<IAuditable>())
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedDate = now;
                    entry.Entity.UpdatedDate = now;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Entity.UpdatedDate = now;
                }
            }

            foreach (var entry in ChangeTracker.Entries<Customer>())
            {
                if (entry.State == EntityState.Added && string.IsNullOrEmpty(entry.Entity.CustomerNumber))
                {
                    var nextId = (Customers.Any() ? Customers.Max(c => c.Id) : 0) + 1;
                    entry.Entity.CustomerNumber = $"C{nextId:D5}";
                }
            }

            foreach (var entry in ChangeTracker.Entries<Invoice>())
            {
                if (entry.State == EntityState.Added && string.IsNullOrEmpty(entry.Entity.BillNumber))
                {
                    var nextId = (Invoices.Any() ? Invoices.Max(i => i.Id) : 0) + 1;
                    entry.Entity.BillNumber = $"INV-{nextId:D5}";
                }
            }

            foreach (var entry in ChangeTracker.Entries<Quotation>())
            {
                if (entry.State == EntityState.Added && string.IsNullOrEmpty(entry.Entity.QuotationNumber))
                {
                    var nextId = (Quotations.Any() ? Quotations.Max(q => q.Id) : 0) + 1;
                    entry.Entity.QuotationNumber = $"QUO-{nextId:D5}";
                }
            }
        }
    }
}
