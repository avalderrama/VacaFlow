using BigSolutions.VacaFlow.Domain.Primitives;

namespace BigSolutions.VacaFlow.Domain.UnitTests.Primitives;

public sealed class ResultTests
{
    private static readonly Error SampleError = new("VF-TST-001", "Something expected went wrong.");

    [Fact]
    public void Success_Should_Carry_No_Error()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(Error.None, result.Error);
    }

    [Fact]
    public void Failure_Should_Carry_The_Error()
    {
        var result = Result.Failure(SampleError);

        Assert.True(result.IsFailure);
        Assert.Equal(SampleError, result.Error);
    }

    [Fact]
    public void Success_With_Value_Should_Expose_The_Value()
    {
        var result = Result.Success(42);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void Value_Should_Throw_When_The_Result_Is_A_Failure()
    {
        var result = Result.Failure<int>(SampleError);

        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void Errors_Should_Compare_By_Value()
    {
        var first = new Error("VF-REQ-003", "Only Draft requests can be edited.");
        var second = new Error("VF-REQ-003", "Only Draft requests can be edited.");

        Assert.Equal(first, second);
    }
}
