using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shared.Contracts.Interfaces;

namespace Api.Data.Entities;

public class KioskAgentInstance : IEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public string MachineName { get; set; } = string.Empty;
    public string AgentVersion { get; set; } = string.Empty;
    public string DisplayUrl { get; set; } = string.Empty;
    public bool ChromiumRunning { get; set; }
    public string? IpAddress { get; set; }
    public DateTimeOffset FirstSeenAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset LastSeenAt { get; set; } = DateTimeOffset.UtcNow;
}
