using Microsoft.Extensions.DependencyInjection;

namespace IndigoSoft.StreamTicker.Tests.Integrations;

public abstract class TestBase(TestFixture fixture) : IClassFixture<TestFixture>
{
    protected readonly ServiceProvider Provider = fixture.Provider;

    protected T Get<T>() where T : notnull
        => Provider.GetRequiredService<T>();
}