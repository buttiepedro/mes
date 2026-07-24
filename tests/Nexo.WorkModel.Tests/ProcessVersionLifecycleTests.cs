using FluentAssertions;
using Nexo.WorkModel.Domain;
using Xunit;

namespace Nexo.WorkModel.Tests;

/// <summary>
/// Version immutability (W10) and "one published version per process" (CB15): the invariants that make
/// a published version safe to instantiate.
/// </summary>
public class ProcessVersionLifecycleTests
{
    [Fact]
    public void PublishedVersion_CannotBeEdited()
    {
        var (process, version) = WorkModelTestFactory.NewDraftWithTasks("A", "B");
        version.SetGraph(new[] { new TaskEdgeSpec("A", "B") }).IsSuccess.Should().BeTrue();

        process.PublishVersion(version).IsSuccess.Should().BeTrue();
        version.IsPublished.Should().BeTrue();
        version.IsEditable.Should().BeFalse();

        var addTask = version.AddTask(new WorkTaskSpec("C", "Task C", Guid.NewGuid()));
        addTask.IsFailure.Should().BeTrue();
        addTask.Error.Code.Should().Be("WorkModel.Version.NotEditableConflict");

        var setGraph = version.SetGraph(Array.Empty<TaskEdgeSpec>());
        setGraph.IsFailure.Should().BeTrue();
        setGraph.Error.Code.Should().Be("WorkModel.Version.NotEditableConflict");

        var removeTask = version.RemoveTask(version.Tasks.First().Id);
        removeTask.IsFailure.Should().BeTrue();
        removeTask.Error.Code.Should().Be("WorkModel.Version.NotEditableConflict");
    }

    [Fact]
    public void Publish_RaisesDomainEvent_AndSetsCurrentVersion()
    {
        var (process, version) = WorkModelTestFactory.NewDraftWithTasks("A", "B");
        version.SetGraph(new[] { new TaskEdgeSpec("A", "B") }).IsSuccess.Should().BeTrue();

        process.PublishVersion(version).IsSuccess.Should().BeTrue();

        process.CurrentVersionId.Should().Be(version.Id);
        process.HasPublishedVersion.Should().BeTrue();
        version.DomainEvents.OfType<ProcessVersionPublishedDomainEvent>().Should().ContainSingle()
            .Which.VersionNo.Should().Be(version.VersionNo);
    }

    [Fact]
    public void Process_CannotHaveTwoPublishedVersions()
    {
        var (process, first) = WorkModelTestFactory.NewDraftWithTasks("A", "B");
        first.SetGraph(new[] { new TaskEdgeSpec("A", "B") }).IsSuccess.Should().BeTrue();
        process.PublishVersion(first).IsSuccess.Should().BeTrue();
        process.CurrentVersionId.Should().Be(first.Id);

        // A second draft is derived (the only legal way a published version evolves, W10) and it is a
        // valid, publishable graph on its own — but the process already has a version in force (CB15).
        var derived = process.DeriveVersion(first, VersionBump.Minor);
        derived.IsSuccess.Should().BeTrue();
        var second = derived.Value;

        var publishSecond = process.PublishVersion(second);

        publishSecond.IsFailure.Should().BeTrue();
        publishSecond.Error.Code.Should().Be("WorkModel.Process.PublishedVersionAlreadyExistsConflict");
        process.CurrentVersionId.Should().Be(first.Id, "the version in force must not change on a rejected publish");
    }

    [Fact]
    public void SuspendingTheVersionInForce_FreesTheProcessToPublishAnother()
    {
        var (process, first) = WorkModelTestFactory.NewDraftWithTasks("A", "B");
        first.SetGraph(new[] { new TaskEdgeSpec("A", "B") }).IsSuccess.Should().BeTrue();
        process.PublishVersion(first).IsSuccess.Should().BeTrue();

        process.SuspendVersion(first, "recall").IsSuccess.Should().BeTrue();
        process.CurrentVersionId.Should().BeNull();

        var derived = process.DeriveVersion(first, VersionBump.Minor);
        var second = derived.Value;

        process.PublishVersion(second).IsSuccess.Should().BeTrue();
        process.CurrentVersionId.Should().Be(second.Id);
    }
}
