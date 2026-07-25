using FluentAssertions;
using Nexo.Execution.Domain;
using Xunit;

namespace ExecutionTests;

public sealed class ExecutionLifecycleTests
{
    private static Execution TwoTasksFs(out Guid a, out Guid b)
    {
        a = Guid.NewGuid();
        b = Guid.NewGuid();
        var snapshot = ExecutionTestFactory.Batch(
            new[] { ExecutionTestFactory.Task(a, "A"), ExecutionTestFactory.Task(b, "B") },
            ExecutionTestFactory.Fs(a, b));

        return Execution.Create("RUN", snapshot, ExecutionTestFactory.Manual, target: ExecutionTestFactory.Target()).Value;
    }

    [Fact]
    public void A_task_gated_by_an_unfinished_finish_to_start_predecessor_cannot_be_started()
    {
        var execution = TwoTasksFs(out _, out _);

        var result = execution.StartTask(execution.Run("B").Id, operatorId: Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        execution.Run("B").Status.Should().Be(TaskRunStatus.Pending);
    }

    [Fact]
    public void Completing_a_predecessor_enables_its_successor_and_the_run_can_finish_and_close()
    {
        var execution = TwoTasksFs(out _, out _);

        execution.StartTask(execution.Run("A").Id, operatorId: Guid.NewGuid()).IsSuccess.Should().BeTrue();
        execution.Status.Should().Be(ExecutionStatus.InProgress, "the first started task starts the run");

        execution.CompleteTask(execution.Run("A").Id, force: false, reason: null).IsSuccess.Should().BeTrue();
        execution.Run("B").Status.Should().Be(TaskRunStatus.Ready, "A is finished, so its successor is enabled");

        execution.StartTask(execution.Run("B").Id, operatorId: Guid.NewGuid()).IsSuccess.Should().BeTrue();
        execution.CompleteTask(execution.Run("B").Id, force: false, reason: null).IsSuccess.Should().BeTrue();

        var closed = execution.Close(CloseKind.Normal, reason: null);

        closed.IsSuccess.Should().BeTrue();
        execution.Status.Should().Be(ExecutionStatus.Closed);
        execution.ProgressPct.Should().Be(100m);
    }

    [Fact]
    public void A_normal_close_rejects_open_mandatory_tasks_but_a_forced_close_overrides_it()
    {
        var execution = Execution.Create(
            "RUN",
            ExecutionTestFactory.Batch(new[] { ExecutionTestFactory.Task(Guid.NewGuid(), "A") }),
            ExecutionTestFactory.Manual,
            target: ExecutionTestFactory.Target()).Value;

        execution.Close(CloseKind.Normal, reason: null).IsFailure.Should().BeTrue("A is a mandatory task still open");

        var forced = execution.Close(CloseKind.Forced, reason: "operator left the shift");

        forced.IsSuccess.Should().BeTrue();
        execution.Status.Should().Be(ExecutionStatus.Closed);
    }

    [Fact]
    public void A_task_that_requires_evidence_cannot_be_completed_until_the_evidence_is_attached()
    {
        var taskId = Guid.NewGuid();
        var execution = Execution.Create(
            "RUN",
            ExecutionTestFactory.Batch(new[]
            {
                ExecutionTestFactory.Task(taskId, "A", requiredEvidence: EvidenceKind.Photo, minEvidence: 1)
            }),
            ExecutionTestFactory.Manual,
            target: ExecutionTestFactory.Target()).Value;

        var run = execution.Run("A");
        execution.StartTask(run.Id, operatorId: Guid.NewGuid()).IsSuccess.Should().BeTrue();

        execution.CompleteTask(run.Id, force: false, reason: null)
            .IsFailure.Should().BeTrue("the mandatory photo has not been attached");

        execution.AttachEvidence(run.Id, EvidenceKind.Photo, EvidenceStatus.Materialized, mediaRef: "s3://tenant/evidence/1.jpg")
            .IsSuccess.Should().BeTrue();

        execution.CompleteTask(run.Id, force: false, reason: null)
            .IsSuccess.Should().BeTrue("the evidence requirement is now satisfied");
        run.Status.Should().Be(TaskRunStatus.Completed);
    }
}
