using AspireKeyCloakTemplate.SharedKernel.Features.Mediator;
using Shouldly;
using Xunit;

namespace AspireKeyCloakTemplate.SharedKernel.UnitTests.Features.Mediator;

public class UnitTests
{
    [Fact]
    public void Unit_Value_ShouldBeDefaultInstance()
    {
        // Arrange & Act
        var unit = Unit.Value;

        // Assert
        unit.ShouldBeOfType<Unit>();
    }

    [Fact]
    public void Unit_Equals_ShouldReturnTrueForAnyUnit()
    {
        // Arrange
        var unit1 = Unit.Value;
        var unit2 = Unit.Value;

        // Act & Assert
        unit1.Equals(unit2).ShouldBeTrue();
        (unit1 == unit2).ShouldBeTrue();
        (unit1 != unit2).ShouldBeFalse();
    }

    [Fact]
    public void Unit_GetHashCode_ShouldReturnZero()
    {
        // Arrange
        var unit = Unit.Value;

        // Act
        var hashCode = unit.GetHashCode();

        // Assert
        hashCode.ShouldBe(0);
    }

    [Fact]
    public void Unit_Equals_WithObject_ShouldReturnTrueForUnit()
    {
        // Arrange
        var unit = Unit.Value;
        object otherUnit = Unit.Value;

        // Act & Assert
        unit.Equals(otherUnit).ShouldBeTrue();
    }

    [Fact]
    public void Unit_Equals_WithObject_ShouldReturnFalseForNonUnit()
    {
        // Arrange
        var unit = Unit.Value;
        object notUnit = "not a unit";

        // Act & Assert
        unit.Equals(notUnit).ShouldBeFalse();
    }
}
