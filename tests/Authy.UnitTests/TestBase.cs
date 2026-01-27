using Authy.Presentation.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Authy.UnitTests;

[TestClass]
public abstract class TestBase
{
    protected AuthyDbContext DbContext { get; private set; } = null!;
    protected CancellationToken CancellationToken => CancellationToken.None;

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
