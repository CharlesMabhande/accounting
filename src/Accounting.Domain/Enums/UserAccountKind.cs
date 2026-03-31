namespace Accounting.Domain.Enums;

/// <summary>Staff = internal operators; Agent = external / field agents (same RBAC, different classification).</summary>
public enum UserAccountKind : byte
{
    Staff = 0,
    Agent = 1
}
