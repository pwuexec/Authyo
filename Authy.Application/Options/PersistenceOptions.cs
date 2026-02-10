using System.ComponentModel.DataAnnotations;

namespace Authy.Application.Options;

public enum PersistenceProvider
{
    Sqlite,
}

public class PersistenceOptions
{
    [Required]
    [MinLength(1)]
    [MaxLength(256)]
    public required string ConnectionString { get; init; }

    [Required]
    [EnumDataType(typeof(PersistenceProvider))]
    public required PersistenceProvider Provider { get; init; }
    
    [Required]
    public required bool Recreate { get; init; }
}