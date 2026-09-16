using KeepAlive.Core;

namespace KeepAlive.Tests.Core;

public sealed class SessionDurationPolicyTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(481)]
    public void ValidateRejectsDurationsOutsideAllowedRange(int totalMinutes)
    {
        TimeSpan duration = TimeSpan.FromMinutes(totalMinutes);

        Assert.Throws<ArgumentOutOfRangeException>(() => SessionDurationPolicy.Validate(duration));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(120)]
    [InlineData(480)]
    public void ValidateAcceptsDurationsInsideAllowedRange(int totalMinutes)
    {
        SessionDurationPolicy.Validate(TimeSpan.FromMinutes(totalMinutes));
    }

    [Fact]
    public void PresetsMatchProductPlan()
    {
        Assert.Equal(
            [
                TimeSpan.FromMinutes(15),
                TimeSpan.FromMinutes(30),
                TimeSpan.FromHours(1),
                TimeSpan.FromHours(2),
                TimeSpan.FromHours(4),
            ],
            SessionDurationPolicy.Presets);
    }
}
