using System.Security.Claims;
using EBill.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace EBill.Data
{
    public class ApplicationDbContext : DbContext
    {
        // Used to stamp "created by <user>" on the ledger rows the invoice hook writes.
        private readonly IHttpContextAccessor? _httpContextAccessor;

        // Guards the second save the invoice-ledger hook performs against re-entering itself.
        private bool _syncingInvoiceLedger;

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IHttpContextAccessor? httpContextAccessor = null) : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public DbSet<Company> Companies => Set<Company>();
        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<Invoice> Invoices => Set<Invoice>();
        public DbSet<Quotation> Quotations => Set<Quotation>();
        public DbSet<Tax> Taxes => Set<Tax>();
        public DbSet<AccountingLink> AccountingLinks => Set<AccountingLink>();
        public DbSet<Settings> Settings => Set<Settings>();
        public DbSet<AppUser> AppUsers => Set<AppUser>();
        public DbSet<LedgerDetails> LedgerDetails => Set<LedgerDetails>();
        public DbSet<LedgerBalance> LedgerBalances => Set<LedgerBalance>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Company>()
                .HasIndex(c => c.Email)
                .IsUnique();

            modelBuilder.Entity<AppUser>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<AppUser>()
                .HasOne(u => u.Company)
                .WithMany()
                .HasForeignKey(u => u.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);

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

            // Nullable - a manually-entered line item (ProductController.Add/
            // AddToQuotation) never has one, and SQL Server's default unique-index
            // behavior already allows any number of NULLs alongside it. Only
            // non-null values (dispatch-sourced line items) are required to be
            // distinct, which is what stops the same dispatched item being billed
            // twice.
            modelBuilder.Entity<Product>()
                .HasIndex(p => p.DispatchProductId)
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

            modelBuilder.Entity<LedgerDetails>()
                .HasOne(l => l.Customer)
                .WithMany()
                .HasForeignKey(l => l.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<LedgerDetails>()
                .HasOne(l => l.Invoice)
                .WithMany()
                .HasForeignKey(l => l.InvoiceId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<LedgerDetails>()
                .HasOne<Company>()
                .WithMany()
                .HasForeignKey(l => l.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<LedgerDetails>()
                .HasIndex(l => new { l.CompanyId, l.CustomerId });

            modelBuilder.Entity<LedgerBalance>()
                .HasOne(l => l.Customer)
                .WithMany()
                .HasForeignKey(l => l.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<LedgerBalance>()
                .HasOne<Company>()
                .WithMany()
                .HasForeignKey(l => l.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);

            // One running-balance row per customer within a company.
            modelBuilder.Entity<LedgerBalance>()
                .HasIndex(l => new { l.CompanyId, l.CustomerId })
                .IsUnique();

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
            return SaveChangesAsync().GetAwaiter().GetResult();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            ApplyAuditInfo();

            if (_syncingInvoiceLedger)
            {
                return await base.SaveChangesAsync(cancellationToken);
            }

            // Invoices being deleted: drop their automatic ledger credit (and take it out
            // of the customer's balance) and unlink any debits, so the FK doesn't block it.
            await DetachLedgerFromDeletedInvoicesAsync(cancellationToken);

            var invoices = CollectInvoicesNeedingCredit();
            if (invoices.Count == 0)
            {
                return await base.SaveChangesAsync(cancellationToken);
            }

            // Two saves (the invoice first, so a new one has its Id) inside one transaction.
            var ownsTransaction = Database.CurrentTransaction is null;
            await using var tx = ownsTransaction ? await Database.BeginTransactionAsync(cancellationToken) : null;

            var result = await base.SaveChangesAsync(cancellationToken);

            _syncingInvoiceLedger = true;
            try
            {
                await SyncInvoiceCreditsAsync(invoices, cancellationToken);
                await base.SaveChangesAsync(cancellationToken);
            }
            finally
            {
                _syncingInvoiceLedger = false;
            }

            if (tx is not null)
            {
                await tx.CommitAsync(cancellationToken);
            }

            return result;
        }

        // Invoices that were just added, or whose TotalAmount just changed.
        private List<(Invoice Invoice, bool IsNew)> CollectInvoicesNeedingCredit()
        {
            var list = new List<(Invoice, bool)>();
            foreach (var entry in ChangeTracker.Entries<Invoice>())
            {
                if (entry.State == EntityState.Added)
                {
                    list.Add((entry.Entity, true));
                }
                else if (entry.State == EntityState.Modified)
                {
                    var total = entry.Property(i => i.TotalAmount);
                    if (!Equals(total.OriginalValue, total.CurrentValue))
                    {
                        list.Add((entry.Entity, false));
                    }
                }
            }
            return list;
        }

        // Keeps one credit row per invoice in the ledger: created with the invoice, and
        // its amount follows the invoice's TotalAmount when that changes. The customer's
        // LedgerBalance moves by the difference.
        private async Task SyncInvoiceCreditsAsync(List<(Invoice Invoice, bool IsNew)> invoices, CancellationToken ct)
        {
            const string updatedMarker = "; amount updated by ";
            var userName = CurrentUserName();
            var now = DateTime.UtcNow;

            foreach (var (invoice, isNew) in invoices)
            {
                var companyId = await Customers.AsNoTracking()
                    .Where(c => c.Id == invoice.CustomerId)
                    .Select(c => (int?)c.CompanyId)
                    .FirstOrDefaultAsync(ct);
                if (companyId is null)
                {
                    continue;
                }

                // The automatic credit row is the invoice's non-debit ledger row
                // (manual debits linked to the invoice have DebitedAmount > 0).
                var credit = await Set<LedgerDetails>()
                    .FirstOrDefaultAsync(l => l.InvoiceId == invoice.Id && l.DebitedAmount == 0, ct);

                decimal delta;
                if (credit is null)
                {
                    credit = new LedgerDetails
                    {
                        Id = Guid.NewGuid(),
                        CompanyId = companyId.Value,
                        CustomerId = invoice.CustomerId,
                        InvoiceId = invoice.Id,
                        CreditedAmount = invoice.TotalAmount,
                        CreditedBy = userName,
                        Comments = $"Credited by Invoice {(isNew ? "creation" : "update")} by {userName}",
                        CreatedAt = now,
                        CreatedBy = userName
                    };
                    Set<LedgerDetails>().Add(credit);
                    delta = invoice.TotalAmount;
                }
                else
                {
                    delta = invoice.TotalAmount - credit.CreditedAmount;
                    if (delta == 0)
                    {
                        continue;
                    }

                    var baseComment = (credit.Comments ?? string.Empty).Split(updatedMarker)[0];
                    credit.CreditedAmount = invoice.TotalAmount;
                    credit.Comments = baseComment + updatedMarker + userName;
                    credit.UpdatedAt = now;
                    credit.UpdatedBy = userName;
                }

                var balance = Set<LedgerBalance>().Local
                                  .FirstOrDefault(b => b.CompanyId == companyId && b.CustomerId == invoice.CustomerId)
                              ?? await Set<LedgerBalance>()
                                  .FirstOrDefaultAsync(b => b.CompanyId == companyId && b.CustomerId == invoice.CustomerId, ct);
                if (balance is null)
                {
                    balance = new LedgerBalance
                    {
                        Id = Guid.NewGuid(),
                        CompanyId = companyId.Value,
                        CustomerId = invoice.CustomerId,
                        CreatedAt = now,
                        CreatedBy = userName
                    };
                    Set<LedgerBalance>().Add(balance);
                }
                else
                {
                    balance.UpdatedAt = now;
                    balance.UpdatedBy = userName;
                }

                balance.TotalCredited += delta;
                balance.TotalRemaining = balance.TotalCredited - balance.TotalDebited;
            }
        }

        private async Task DetachLedgerFromDeletedInvoicesAsync(CancellationToken ct)
        {
            var deletedIds = ChangeTracker.Entries<Invoice>()
                .Where(e => e.State == EntityState.Deleted)
                .Select(e => e.Entity.Id)
                .ToList();
            if (deletedIds.Count == 0)
            {
                return;
            }

            var userName = CurrentUserName();
            var now = DateTime.UtcNow;

            var rows = await Set<LedgerDetails>()
                .Where(l => l.InvoiceId != null && deletedIds.Contains(l.InvoiceId.Value))
                .ToListAsync(ct);

            foreach (var row in rows)
            {
                if (row.DebitedAmount == 0)
                {
                    var balance = await Set<LedgerBalance>()
                        .FirstOrDefaultAsync(b => b.CompanyId == row.CompanyId && b.CustomerId == row.CustomerId, ct);
                    if (balance is not null)
                    {
                        balance.TotalCredited -= row.CreditedAmount;
                        balance.TotalRemaining = balance.TotalCredited - balance.TotalDebited;
                        balance.UpdatedAt = now;
                        balance.UpdatedBy = userName;
                    }
                    Set<LedgerDetails>().Remove(row);
                }
                else
                {
                    row.InvoiceId = null;
                }
            }
        }

        private string CurrentUserName()
        {
            return _httpContextAccessor?.HttpContext?.User.FindFirst(ClaimTypes.Name)?.Value ?? "System";
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
