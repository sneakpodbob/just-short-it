using JustShortIt.Service;
using Microsoft.Extensions.Logging.Abstractions;

namespace JustShortIt.Tests;

public class LoginAttemptServiceTests
{
    [Test]
    public async Task RegisterFailure_WhenCalledRepeatedly_IncreasesBackoffForSameUsernameAndIp()
    {
        var service = CreateService();

        var firstDelay = service.RegisterFailure("test", "127.0.0.1");
        var secondDelay = service.RegisterFailure("test", "127.0.0.1");
        var thirdDelay = service.RegisterFailure("test", "127.0.0.1");

        await Assert.That(firstDelay).IsEqualTo(TimeSpan.FromSeconds(1));
        await Assert.That(secondDelay).IsEqualTo(TimeSpan.FromSeconds(2));
        await Assert.That(thirdDelay).IsEqualTo(TimeSpan.FromSeconds(4));
    }

    [Test]
    public async Task RegisterSuccess_AfterFailures_ResetsBackoffForSameUsernameAndIp()
    {
        var service = CreateService();

        _ = service.RegisterFailure("test", "127.0.0.1");
        _ = service.RegisterFailure("test", "127.0.0.1");

        service.RegisterSuccess("test", "127.0.0.1");
        var delayAfterReset = service.RegisterFailure("test", "127.0.0.1");

        await Assert.That(delayAfterReset).IsEqualTo(TimeSpan.FromSeconds(1));
    }

    private static LoginAttemptService CreateService() =>
        new(NullLogger<LoginAttemptService>.Instance);
}