using Microsoft.AspNetCore.Identity;
using Shared.Contracts.Interfaces;

namespace Api.Data.Entities;

public class AppRole : IdentityRole<int>, IEntity;
