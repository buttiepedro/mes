using FluentAssertions;
using Nexo.WorkModel.Domain;

namespace Nexo.WorkModel.Tests;

/// <summary>
/// Builders shared by the domain tests: a process and a draft version pre-populated with tasks, so each
/// test only has to declare the precedences it cares about.
/// </summary>
internal static class WorkModelTestFactory
{
    /// <summary>A process with its version 1.0 in draft and the requested tasks added (no edges yet).</summary>
    public static (Process Process, ProcessVersion Version) NewDraftWithTasks(params string[] taskCodes)
    {
        var process = Process.Create("PRC-1", "Assembly line", ProcessProfile.Repetitive);

        var start = process.StartInitialVersion();
        start.IsSuccess.Should().BeTrue();
        var version = start.Value;

        foreach (var code in taskCodes)
        {
            AddTask(version, code);
        }

        return (process, version);
    }

    /// <summary>Adds a minimal-but-valid task (its own role, no durations, no inputs) to the draft.</summary>
    public static WorkTask AddTask(ProcessVersion version, string code)
    {
        var added = version.AddTask(new WorkTaskSpec(code, $"Task {code}", Guid.NewGuid()));
        added.IsSuccess.Should().BeTrue($"task '{code}' should be added to the draft");

        return added.Value;
    }
}
