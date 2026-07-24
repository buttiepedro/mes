using Nexo.BuildingBlocks.Application;

namespace Nexo.MasterData.Application;

/// <summary>
/// Creates a minimal customer (code + legal name + contact). No commercial terms, no prices, no
/// invoicing. Returns the id of the created customer.
/// </summary>
public sealed record CreateCustomerCommand(
    string Code,
    string LegalName,
    string? TaxId = null,
    string? Contact = null,
    string? Notes = null,
    string? ExternalRef = null) : ICommand<Guid>;
