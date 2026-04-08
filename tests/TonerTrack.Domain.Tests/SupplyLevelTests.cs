using FluentAssertions;
using TonerTrack.Domain.ValueObjects;
using Xunit;

namespace TonerTrack.Domain.Tests;

public sealed class SupplyLevelTests
{
    [Theory]
    [InlineData(80,  100, "80%",  false, false)]
    [InlineData(20,  100, "20%",  false, false)]
    [InlineData(19,  100, "19%",  true,  false)]
    [InlineData(10,  100, "10%",  true,  false)]
    [InlineData(9,   100, "9%",   true,  true)]
    [InlineData(0,   100, "0%",   true,  true)]
    [InlineData(100, 100, "100%", false, false)]
    public void FromRaw_ComputesCorrectly(
        int level, int max, string expectedDisplay, bool isLow, bool isCritical)
    {
        var sl = SupplyLevel.FromRaw(level, max);

        sl.Display.Should().Be(expectedDisplay);
        sl.IsLow.Should().Be(isLow);
        sl.IsCritical.Should().Be(isCritical);
    }

    [Fact]
    public void FromRaw_Level_Minus2_ReturnsUnknown()
        => SupplyLevel.FromRaw(-2, 100).Should().Be(SupplyLevel.Unknown);

    [Fact]
    public void FromRaw_Level_Minus3_ReturnsOk()
        => SupplyLevel.FromRaw(-3, 100).Should().Be(SupplyLevel.Ok);

    [Fact]
    public void FromRaw_ZeroMax_ReturnsUnknown()
        => SupplyLevel.FromRaw(50, 0).Should().Be(SupplyLevel.Unknown);

    [Theory]
    [InlineData("75%",  75)]
    [InlineData("100%", 100)]
    [InlineData("0%",   0)]
    [InlineData("19%",  19)]
    public void FromDisplay_ParsesPercentageString(string display, int expectedPct)
    {
        var sl = SupplyLevel.FromDisplay(display);
        sl.Percentage.Should().Be(expectedPct);
        sl.Display.Should().Be(display);
    }

    [Fact]
    public void FromDisplay_UnknownString_ReturnsUnknownSingleton()
        => SupplyLevel.FromDisplay("Unknown").Should().Be(SupplyLevel.Unknown);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void FromDisplay_NullOrWhitespace_ReturnsUnknown(string? display)
        => SupplyLevel.FromDisplay(display).Should().Be(SupplyLevel.Unknown);

    [Fact]
    public void Unknown_IsNotLowOrCritical()
    {
        SupplyLevel.Unknown.IsLow.Should().BeFalse();
        SupplyLevel.Unknown.IsCritical.Should().BeFalse();
    }

    [Fact]
    public void SamePercentage_AreEqual()
    {
        var a = SupplyLevel.FromRaw(50, 100);
        var b = SupplyLevel.FromDisplay("50%");
        a.Should().Be(b);
    }
}
