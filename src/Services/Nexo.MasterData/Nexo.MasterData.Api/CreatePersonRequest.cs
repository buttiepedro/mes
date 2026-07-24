namespace Nexo.MasterData.Api;

/// <summary>Request body for <c>POST /v1/people</c>. No hourly rate in the MVP — cost is deferred to V1.</summary>
public sealed record CreatePersonRequest(
    string Code,
    string FullName,
    Guid? DefaultRoleId = null,
    Guid? SiteId = null,
    Guid? LineId = null,
    Guid? UserId = null,
    string? Calendar = null,
    string? ExternalRef = null);
