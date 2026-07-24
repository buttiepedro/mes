using Nexo.BuildingBlocks.Domain;

namespace Nexo.MasterData.Domain;

/// <summary>
/// Minimal customer (<c>master.customers</c>). Aggregate root.
/// </summary>
/// <remarks>
/// Deliberately poor (docs/design/03-data-schema.md §2.5.4): no commercial terms, no prices, no
/// invoicing — <b>Nexo does not build a CRM</b>. It exists for one reason: the project flavour needs to
/// know <i>who</i> the deliverable is for. There is no <c>orders</c> entity either: the commitment is an
/// attribute of the Execution, not a catalog.
/// </remarks>
public sealed class Customer : MasterRecord
{
    // EF Core materialization constructor.
    private Customer() => LegalName = string.Empty;

    private Customer(
        Guid id,
        string code,
        string legalName,
        string? taxId,
        string? contact,
        string? notes,
        MasterGovernance governance,
        string? externalRef)
        : base(id, code, governance, externalRef)
    {
        LegalName = NormalizeRequired(legalName, nameof(legalName));
        TaxId = Normalize(taxId);
        Contact = Normalize(contact);
        Notes = Normalize(notes);
    }

    public override string Catalog => MasterCatalog.Customers;

    public override string DisplayName => LegalName;

    public string LegalName { get; private set; }

    /// <summary>Tax identifier (CUIT / VAT id).</summary>
    public string? TaxId { get; private set; }

    /// <summary>Contact payload (jsonb): name, e-mail, phone.</summary>
    public string? Contact { get; private set; }

    public string? Notes { get; private set; }

    /// <summary>Creates a minimal customer and raises the upserted domain event.</summary>
    /// <exception cref="ArgumentException">When the code or the legal name are empty.</exception>
    public static Customer Create(
        string code,
        string legalName,
        string? taxId = null,
        string? contact = null,
        string? notes = null,
        MasterGovernance governance = MasterGovernance.Local,
        string? externalRef = null)
    {
        var customer = new Customer(
            UuidV7.NewGuid(),
            code,
            legalName,
            taxId,
            contact,
            notes,
            governance,
            externalRef);

        customer.RaiseUpserted(MasterRecordChange.Created);

        return customer;
    }

    /// <summary>Updates the editable attributes of the customer and raises the upserted domain event.</summary>
    /// <exception cref="InvalidOperationException">When the customer is archived.</exception>
    public void Update(string legalName, string? taxId = null, string? contact = null, string? notes = null)
    {
        if (IsArchived)
        {
            throw new InvalidOperationException($"Customer '{Code}' is archived and cannot be updated.");
        }

        LegalName = NormalizeRequired(legalName, nameof(legalName));
        TaxId = Normalize(taxId);
        Contact = Normalize(contact);
        Notes = Normalize(notes);
        Touch();

        RaiseUpserted(MasterRecordChange.Updated);
    }
}
