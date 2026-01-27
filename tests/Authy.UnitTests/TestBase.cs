using Authy.Presentation.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;

namespace Authy.UnitTests;

[TestClass]
public abstract class TestBase
{
    protected AuthyDbContext DbContext { get; private set; } = null!;
    protected FakeTimeProvider TimeProvider { get; private set; } = new();
    protected CancellationToken CancellationToken => CancellationToken.None;

    public TestContext TestContext { get; set; }

    [TestInitialize]
    public virtual void Setup()
    {
        var options = new DbContextOptionsBuilder<AuthyDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        DbContext = new AuthyDbContext(options);
    }

    [TestCleanup]
    public void Cleanup()
    {
        DbContext.Dispose();
    }
}
