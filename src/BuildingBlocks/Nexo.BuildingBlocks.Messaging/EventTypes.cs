namespace Nexo.BuildingBlocks.Messaging;

/// <summary>Canonical integration-event type names shared across all Nexo services.</summary>
public static class EventTypes
{
    public const string Tenant_Provisioned = "nexo.tenant.provisioned";

    public const string Reading_Ingested = "nexo.reading.ingested";

    public const string Production_Registered = "nexo.production.registered";

    public const string Production_RunClosed = "nexo.production.run_closed";

    public const string Scrap_Registered = "nexo.scrap.registered";

    public const string Quality_InspectionCompleted = "nexo.quality.inspection_completed";

    public const string Quality_DispositionSet = "nexo.quality.disposition_set";

    public const string Downtime_Started = "nexo.downtime.started";

    public const string Downtime_Ended = "nexo.downtime.ended";

    public const string Device_StatusChanged = "nexo.device.status_changed";

    public const string Process_VersionPublished = "nexo.process.version_published";

    public const string Process_VersionSuspended = "nexo.process.version_suspended";

    public const string MasterData_RecordUpserted = "nexo.masterdata.record_upserted";

    public const string MasterData_RecordArchived = "nexo.masterdata.record_archived";

    public const string MasterData_ImportCompleted = "nexo.masterdata.import_completed";

    public const string Integration_OdooSyncRequested = "nexo.integration.odoo_sync_requested";

    public const string Integration_OdooSyncCompleted = "nexo.integration.odoo_sync_completed";
}
