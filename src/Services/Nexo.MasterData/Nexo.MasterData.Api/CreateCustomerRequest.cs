namespace Nexo.MasterData.Api;

/// <summary>Request body for <c>POST /v1/customers</c> (code + legal name + contact).</summary>
public sealed record CreateCustomerRequest(
    string Code,
    string LegalName,
    string? TaxId = null,
    string? Contact = null,
    string? Notes = null,
    string? ExternalRef = null);
