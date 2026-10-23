using LMHull.Core.Validation;
using Xunit;

namespace LMHull.Tests.Validation;

public class NumericTextRulesTests
{
    [Theory]
    [InlineData("")]
    [InlineData("12")]
    [InlineData("12.5")]
    [InlineData("12,5")]
    [InlineData("12.")]
    [InlineData(".5")]
    public void IsAcceptable_ValidNumbers(string text)
    {
        Assert.True(NumericTextRules.IsAcceptable(text, decimalPlaces: 2, allowNegative: false));
    }

    [Theory]
    [InlineData("12a")]
    [InlineData("abc")]
    [InlineData("1.2.3")]
    [InlineData("1,2.3")]
    [InlineData("12.345")]
    [InlineData("-3")]
    [InlineData("1e5")]
    [InlineData(" 12")]
    public void IsAcceptable_RejectsInvalidText(string text)
    {
        Assert.False(NumericTextRules.IsAcceptable(text, decimalPlaces: 2, allowNegative: false));
    }

    [Theory]
    [InlineData("-")]
    [InlineData("-3")]
    [InlineData("-3.25")]
    public void IsAcceptable_NegativeWhenAllowed(string text)
    {
        Assert.True(NumericTextRules.IsAcceptable(text, decimalPlaces: 2, allowNegative: true));
    }

    [Fact]
    public void IsAcceptable_NoSeparatorWhenZeroDecimals()
    {
        Assert.True(NumericTextRules.IsAcceptable("15", decimalPlaces: 0, allowNegative: false));
        Assert.False(NumericTextRules.IsAcceptable("15.", decimalPlaces: 0, allowNegative: false));
    }

    [Theory]
    [InlineData("12.5", 12.5)]
    [InlineData("12,5", 12.5)]
    [InlineData("-3", -3.0)]
    [InlineData("7.", 7.0)]
    public void TryParse_ParsesBothSeparators(string text, double expected)
    {
        Assert.True(NumericTextRules.TryParse(text, out double value));
        Assert.Equal(expected, value, 6);
    }

    [Theory]
    [InlineData("")]
    [InlineData("-")]
    [InlineData("abc")]
    public void TryParse_FailsOnIncompleteOrInvalid(string text)
    {
        Assert.False(NumericTextRules.TryParse(text, out _));
    }

    [Fact]
    public void Format_UsesInvariantDecimalPoint()
    {
        Assert.Equal("12.50", NumericTextRules.Format(12.5, 2));
    }
}
