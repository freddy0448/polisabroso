using Polisabroso.Shared.UI.Components;

namespace Polisabroso.Auth.Tests;

public class TextMaskTests
{
    [Theory]
    [InlineData("0000 0000", "71234567", "7123 4567")]
    [InlineData("0000 0000", "712", "712")]
    [InlineData("000-000", "123456789", "123-456")]
    [InlineData("AA-000", "ab123", "ab-123")]
    [InlineData("*** ***", "a1b2c3", "a1b 2c3")]
    [InlineData("+591 0000 0000", "51234567", "+591 5123 4567")]
    [InlineData("\\A-000", "123", "A-123")]
    [InlineData("(000)", "123", "(123)")]
    [InlineData("(000)", "12", "(12")]
    [InlineData("+591 0000 0000", "", "")]
    [InlineData("000", "ab1!2x3", "123")]
    public void Format_MaskPattern_FormatsValueAndRoundTrips(string mask, string raw, string expected)
    {
        var formatted = TextMask.Format(raw, mask);
        Assert.Equal(expected, formatted);
        Assert.Equal(expected, TextMask.Format(TextMask.Unmask(formatted, mask), mask));
    }

    [Theory]
    [InlineData("0000 0000", "7123 4567", "71234567")]
    [InlineData("+591 0000 0000", "+591 5123 4567", "51234567")]
    [InlineData("000-000", "123456", "123456")]
    [InlineData("AA-000", "ab-123", "ab123")]
    public void Unmask_FormattedOrPastedValue_RemovesLiterals(string mask, string input, string expected)
    {
        Assert.Equal(expected, TextMask.Unmask(input, mask));
    }

    [Theory]
    [InlineData("")]
    [InlineData("literal")]
    [InlineData("000\\")]
    public void Validate_InvalidPattern_Throws(string mask)
    {
        Assert.Throws<ArgumentException>(() => TextMask.Validate(mask));
    }
}
