using Nexo.MasterData.Domain;

namespace Nexo.MasterData.Application;

/// <summary>Read model for a unit of measure.</summary>
public sealed record UomDto(
    Guid Id,
    string Code,
    string Name,
    string Symbol,
    string? Magnitude,
    decimal FactorToBase,
    bool IsBase,
    short Decimals,
    string Governance,
    string Status,
    string? ExternalRef);

/// <summary>Read model for an item (product and/or input).</summary>
public sealed record ItemDto(
    Guid Id,
    string Code,
    string Name,
    Guid BaseUomId,
    IReadOnlyCollection<string> Roles,
    string? Category,
    string? Family,
    string Tracking,
    decimal? IdealCycleTime,
    Guid? DefaultProcessId,
    string Governance,
    string Status,
    string? ExternalRef);

/// <summary>Read model for an operational person (no hourly rate — cost is deferred to V1).</summary>
public sealed record PersonDto(
    Guid Id,
    string Code,
    string FullName,
    Guid? DefaultRoleId,
    Guid? SiteId,
    Guid? LineId,
    Guid? UserId,
    string Governance,
    string Status,
    string? ExternalRef);

/// <summary>Read model for a minimal customer.</summary>
public sealed record CustomerDto(
    Guid Id,
    string Code,
    string LegalName,
    string? TaxId,
    string? Contact,
    string? Notes,
    string Governance,
    string Status,
    string? ExternalRef);

/// <summary>Projections from the aggregates to their read models.</summary>
public static class MasterDataProjections
{
    public static UomDto ToDto(this Uom uom) => new(
        uom.Id,
        uom.Code,
        uom.Name,
        uom.Symbol,
        uom.Magnitude?.ToWireValue(),
        uom.FactorToBase,
        uom.IsBase,
        uom.Decimals,
        uom.Governance.ToWireValue(),
        uom.Status.ToWireValue(),
        uom.ExternalRef);

    public static ItemDto ToDto(this Item item) => new(
        item.Id,
        item.Code,
        item.Name,
        item.BaseUomId,
        item.Roles.Select(role => role.ToWireValue()).ToArray(),
        item.Category,
        item.Family,
        item.Tracking.ToWireValue(),
        item.IdealCycleTime,
        item.DefaultProcessId,
        item.Governance.ToWireValue(),
        item.Status.ToWireValue(),
        item.ExternalRef);

    public static PersonDto ToDto(this Person person) => new(
        person.Id,
        person.Code,
        person.FullName,
        person.DefaultRoleId,
        person.SiteId,
        person.LineId,
        person.UserId,
        person.Governance.ToWireValue(),
        person.Status.ToWireValue(),
        person.ExternalRef);

    public static CustomerDto ToDto(this Customer customer) => new(
        customer.Id,
        customer.Code,
        customer.LegalName,
        customer.TaxId,
        customer.Contact,
        customer.Notes,
        customer.Governance.ToWireValue(),
        customer.Status.ToWireValue(),
        customer.ExternalRef);
}
