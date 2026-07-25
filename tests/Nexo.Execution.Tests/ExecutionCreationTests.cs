using FluentAssertions;
using Nexo.Execution.Domain;
using Xunit;

namespace ExecutionTests;

public sealed class ExecutionCreationTests
{
    [Fact]
    public void Creating_a_batch_materializes_task_runs_and_enables_only_the_start_nodes()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var snapshot = ExecutionTestFactory.Batch(
            new[] { ExecutionTestFactory.Task(a, "A"), ExecutionTestFactory.Task(b, "B") },
            ExecutionTestFactory.Fs(a, b));

        var result = Execution.Create("RUN-1", snapshot, ExecutionTestFactory.Manual, target: ExecutionTestFactory.Target());

        result.IsSuccess.Should().BeTrue();
        var execution = result.Value;
        execution.TaskRuns.Should().HaveCount(2);
        execution.Status.Should().Be(ExecutionStatus.Released);
        execution.Run("A").Status.Should().Be(TaskRunStatus.Ready, "it has no predecessors");
        execution.Run("B").Status.Should().Be(TaskRunStatus.Pending, "its finish→start predecessor is not finished");
        execution.DomainEvents.Should().NotBeEmpty();
    }

    [Fact]
    public void A_batch_requires_a_target_product_and_quantity()
    {
        var snapshot = ExecutionTestFactory.Batch(new[] { ExecutionTestFactory.Task(Guid.NewGuid(), "A") });

        var result = Execution.Create("RUN-2", snapshot, ExecutionTestFactory.Manual, target: null);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void A_project_requires_a_commitment()
    {
        var snapshot = ExecutionTestFactory.Project(new[] { ExecutionTestFactory.Task(Guid.NewGuid(), "A") });

        var result = Execution.Create("PRJ-1", snapshot, ExecutionTestFactory.Manual, commitment: null);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void A_project_may_not_declare_a_target_quantity()
    {
        var snapshot = ExecutionTestFactory.Project(new[] { ExecutionTestFactory.Task(Guid.NewGuid(), "A") });

        var result = Execution.Create(
            "PRJ-2",
            snapshot,
            ExecutionTestFactory.Manual,
            target: ExecutionTestFactory.Target(),
            commitment: ExecutionTestFactory.Commitment());

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void An_empty_snapshot_is_rejected()
    {
        var snapshot = ExecutionTestFactory.Batch(Array.Empty<TaskSnapshot>());

        var result = Execution.Create("RUN-3", snapshot, ExecutionTestFactory.Manual, target: ExecutionTestFactory.Target());

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Oee_applies_to_the_batch_flavour_but_not_to_the_project_flavour()
    {
        var batch = Execution.Create(
            "RUN-4",
            ExecutionTestFactory.Batch(new[] { ExecutionTestFactory.Task(Guid.NewGuid(), "A") }),
            ExecutionTestFactory.Manual,
            target: ExecutionTestFactory.Target()).Value;

        var project = Execution.Create(
            "PRJ-3",
            ExecutionTestFactory.Project(new[] { ExecutionTestFactory.Task(Guid.NewGuid(), "A") }),
            ExecutionTestFactory.Manual,
            commitment: ExecutionTestFactory.Commitment()).Value;

        batch.SupportsOee.Should().BeTrue();
        project.SupportsOee.Should().BeFalse();
    }
}
