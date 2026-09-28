using Cooking.Domain.Entities.Common;

namespace Cooking.Domain.Entities.Users;

/// <summary>
/// Одноразовое приглашение в семью. Code уходит в deep link вида t.me/&lt;bot&gt;?start=&lt;Code&gt;.
/// </summary>
public class FamilyInvite : BaseEntity
{
    public required string Code { get; set; }

    public Guid FamilyId { get; set; }
    public Family Family { get; set; } = null!;

    public Guid CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;

    public DateTime ExpiresAt { get; set; }

    /// <summary>null — приглашение ещё не использовано.</summary>
    public DateTime? UsedAt { get; set; }
}
