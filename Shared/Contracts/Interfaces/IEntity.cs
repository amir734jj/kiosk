using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace Shared.Contracts.Interfaces;

public interface IEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    private static readonly JsonSerializerSettings JsonSerializerSettings = new()
    {
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
        Formatting = Formatting.Indented
    };

    public string ToJsonString()
    {
        return JsonConvert.SerializeObject(this, JsonSerializerSettings);
    }
}