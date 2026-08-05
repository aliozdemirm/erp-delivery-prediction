using App.Domain.Prediction;

namespace App.Domain.Tests.Prediction;

public class OperationTests
{
    [Fact]
    public void Constructor_Parameterless_LeavesReferenceNullAndDurationZero()
    {
        var operation = new Operation();

        Assert.Null(operation.OperationReference);
        Assert.Equal(0, operation.DurationMinutes);
    }

    [Fact]
    public void Constructor_WithReferenceAndDuration_ExposesBoth()
    {
        var operation = new Operation("OP-1", 45);

        Assert.Equal("OP-1", operation.OperationReference);
        Assert.Equal(45, operation.DurationMinutes);
    }

    [Fact]
    public void Constructor_WithNullReference_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new Operation(null!, 10));
    }

    [Fact]
    public void Constructor_WithNegativeDuration_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Operation("OP-1", -1));
    }
}
