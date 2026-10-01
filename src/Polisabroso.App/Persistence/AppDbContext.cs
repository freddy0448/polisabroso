using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Polisabroso.Modules.Auth.Domain;

namespace Polisabroso.Persistence;

public class AppDbContext(DbContextOptions options) : IdentityDbContext<AppUser, AppRole, Guid>(options);