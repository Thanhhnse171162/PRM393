using CourtGo.Infrastructure.Auth;

namespace CourtGo.UnitTests;

public class PasswordHasherTests
{
    private readonly PasswordHasher _hasher = new();

    [Fact]
    public void Hash_DoesNotContainRawPassword_AndVerifies()
    {
        var hash = _hasher.Hash("S3cret-pass");

        Assert.DoesNotContain("S3cret-pass", hash);
        Assert.True(_hasher.Verify("S3cret-pass", hash));
        Assert.False(_hasher.Verify("wrong", hash));
    }
}
