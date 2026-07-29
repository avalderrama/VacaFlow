using BigSolutions.VacaFlow.Domain.Primitives;

namespace BigSolutions.VacaFlow.Domain.UnitTests.Primitives;

public sealed class EqualityTests
{
    private sealed class SampleEntity(Guid id) : Entity<Guid>(id);

    private sealed class OtherEntity(Guid id) : Entity<Guid>(id);

    private sealed class SampleValue(string first, int second) : ValueObject
    {
        protected override IEnumerable<object?> GetAtomicValues()
        {
            yield return first;
            yield return second;
        }
    }

    [Fact]
    public void Entities_With_The_Same_Id_Should_Be_Equal()
    {
        var id = Guid.NewGuid();

        Assert.Equal(new SampleEntity(id), new SampleEntity(id));
    }

    [Fact]
    public void Entities_Of_Different_Types_Should_Never_Be_Equal()
    {
        var id = Guid.NewGuid();

        Assert.False(new SampleEntity(id).Equals(new OtherEntity(id)));
    }

    [Fact]
    public void Value_Objects_Should_Compare_By_Their_Values()
    {
        Assert.Equal(new SampleValue("a", 1), new SampleValue("a", 1));
        Assert.NotEqual(new SampleValue("a", 1), new SampleValue("a", 2));
    }

    [Fact]
    public void Equal_Value_Objects_Should_Share_A_Hash_Code()
    {
        Assert.Equal(
            new SampleValue("a", 1).GetHashCode(),
            new SampleValue("a", 1).GetHashCode());
    }
}
