using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shared.Contracts.Interfaces;

namespace Api.Data.Entities;

public class Office : IEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public string UnitNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<string> Names { get; set; } = [];
    public string? PhoneNumber { get; set; }
    public string? Note { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
