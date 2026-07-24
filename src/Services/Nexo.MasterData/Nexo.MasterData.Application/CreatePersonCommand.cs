using Nexo.BuildingBlocks.Application;

namespace Nexo.MasterData.Application;

/// <summary>
/// Creates an operational person (employee number, preferred role, default scope, calendar).
/// <b>No hourly rate</b> in the MVP — cost is deferred to V1. Returns the id of the created person.
/// </summary>
public sealed record CreatePersonCommand(
    string Code,
    string FullName,
    Guid? DefaultRoleId = null,
    Guid? SiteId = null,
    Guid? LineId = null,
    Guid? UserId = null,
    string? Calendar = null,
    string? ExternalRef = null) : ICommand<Guid>;
