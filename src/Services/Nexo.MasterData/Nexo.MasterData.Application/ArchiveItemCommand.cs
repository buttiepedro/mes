using Nexo.BuildingBlocks.Application;

namespace Nexo.MasterData.Application;

/// <summary>
/// Logical removal of an item (R4: never a physical delete when events reference it).
/// Emits <c>nexo.masterdata.record_archived</c> and returns the archived item.
/// </summary>
/// <remarks>
/// The impact report of the contract (events / executions / published processes referencing the item)
/// is <b>out of scope for this slice</b>: it needs Traceability, Execution and WorkModel, which do not
/// exist yet. When they do, the report is computed here before flipping the status.
/// </remarks>
public sealed record ArchiveItemCommand(Guid ItemId) : ICommand<ItemDto>;
