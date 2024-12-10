using Accounts.CreateAccount;
using Microsoft.EntityFrameworkCore;

namespace Accounts;

public class AccountsDbContext : DbContext
{
    public AccountsDbContext(DbContextOptions<AccountsDbContext> options)
        : base(options)
    {
    }

    public DbSet<Account> Accounts { get; set; }
}