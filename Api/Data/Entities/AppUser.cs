using Microsoft.AspNetCore.Identity;
using Shared.Contracts.Interfaces;

namespace Api.Data.Entities;

public class AppUser : IdentityUser<int>, IEntity
{
    public bool IsActive { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }
    public int? OfficeId { get; set; }
    public Office? Office { get; set; }
}