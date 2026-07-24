using FluentAssertions;
using Nexo.WorkModel.Domain;
using Xunit;

namespace Nexo.WorkModel.Tests;

/// <summary>
/// The point of the whole slice: the DAG defended by <see cref="ProcessVersion.SetGraph"/> over
/// <see cref="TaskGraph.FindCycle"/> (barriers B1 and B2, docs/design/03-data-schema.md §2.6.3).
/// </summary>
public class TaskGraphTests
{
    [Fact]
    public void SetGraph_WithSimpleCycle_AtoBtoA_ShouldFail()
    {
        var (_, version) = WorkModelTestFactory.NewDraftWithTasks("A", "B");

        var result = version.SetGraph(new[]
        {
            new TaskEdgeSpec("A", "B"),
            new TaskEdgeSpec("B", "A")
        });

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("WorkModel.Graph.CycleInvalid");
        version.Dependencies.Should().BeEmpty("a rejected graph must not leave partial precedences");
    }

    [Fact]
    public void SetGraph_WithLongCycle_AtoBtoCtoA_ShouldFail()
    {
        var (_, version) = WorkModelTestFactory.NewDraftWithTasks("A", "B", "C");

        var result = version.SetGraph(new[]
        {
            new TaskEdgeSpec("A", "B"),
            new TaskEdgeSpec("B", "C"),
            new TaskEdgeSpec("C", "A")
        });

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("WorkModel.Graph.CycleInvalid");
        version.Dependencies.Should().BeEmpty();
    }

    [Fact]
    public void SetGraph_WithTrivialEdge_AtoA_ShouldFail()
    {
        var (_, version) = WorkModelTestFactory.NewDraftWithTasks("A");

        var result = version.SetGraph(new[] { new TaskEdgeSpec("A", "A") });

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("WorkModel.Graph.SelfDependencyInvalid");
        version.Dependencies.Should().BeEmpty();
    }

    [Fact]
    public void SetGraph_WithParallelBranches_ShouldSucceed()
    {
        // Diamond: A fans out to B and C (parallel branches) and both converge on D.
        var (_, version) = WorkModelTestFactory.NewDraftWithTasks("A", "B", "C", "D");

        var result = version.SetGraph(new[]
        {
            new TaskEdgeSpec("A", "B"),
            new TaskEdgeSpec("A", "C"),
            new TaskEdgeSpec("B", "D"),
            new TaskEdgeSpec("C", "D")
        });

        result.IsSuccess.Should().BeTrue();
        version.Dependencies.Should().HaveCount(4);
    }

    [Fact]
    public void SetGraph_WithDuplicatedEdge_ShouldFail()
    {
        var (_, version) = WorkModelTestFactory.NewDraftWithTasks("A", "B");

        var result = version.SetGraph(new[]
        {
            new TaskEdgeSpec("A", "B"),
            new TaskEdgeSpec("A", "B")
        });

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("WorkModel.Graph.DuplicateEdgeInvalid");
    }

    [Fact]
    public void SetGraph_WithUnknownTaskCode_ShouldFail()
    {
        var (_, version) = WorkModelTestFactory.NewDraftWithTasks("A", "B");

        var result = version.SetGraph(new[] { new TaskEdgeSpec("A", "Z") });

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("WorkModel.Task.NotFound");
    }
}
