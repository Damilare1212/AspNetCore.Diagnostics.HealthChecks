using HealthChecks.UI.Data;
using Microsoft.EntityFrameworkCore;

namespace HealthChecks.UI.Tests
{
    public class mysql_storage_should
    {
        private const string PROVIDER_NAME = "Pomelo.EntityFrameworkCore.MySql";

        [Fact]
        public void register_healthchecksdb_context_with_migrations()
        {
            var customOptionsInvoked = false;

            var hostBuilder = new WebHostBuilder()
                .UseStartup<DefaultStartup>()
                .ConfigureServices(services =>
                {
                    services.AddHealthChecksUI()
                    .AddMySqlStorage("Host=localhost;User Id=root;Password=Password12!;Database=UI", options => customOptionsInvoked = true);
                });

            var services = hostBuilder.Build().Services;
            var context = services.GetRequiredService<HealthChecksDb>();

            context.ShouldNotBeNull();
            context.Database.GetMigrations().Count().ShouldBeGreaterThan(0);
            context.Database.ProviderName.ShouldBe(PROVIDER_NAME);
            customOptionsInvoked.ShouldBeTrue();
        }

        [Fact]
        public async Task seed_database_and_serve_stored_executions()
        {
            var hostReset = new ManualResetEventSlim(false);
            var collectorReset = new ManualResetEventSlim(false);

            var webHostBuilder = HostBuilderHelper.Create(
                   hostReset,
                   collectorReset,
                   configureUI: config => config.AddMySqlStorage(ProviderTestHelper.MySqlConnectionString()));

            using var host = new TestServer(webHostBuilder);

            hostReset.Wait(ProviderTestHelper.DefaultHostTimeout);

            var context = host.Services.GetRequiredService<HealthChecksDb>();
            var configurations = await context.Configurations.ToListAsync().ConfigureAwait(false);
            var host1 = ProviderTestHelper.Endpoints[0];

            configurations[0].Name.ShouldBe(host1.Name);
            configurations[0].Uri.ShouldBe(host1.Uri);

            using var client = host.CreateClient();

            collectorReset.Wait(ProviderTestHelper.DefaultCollectorTimeout);

            var report = await client.GetAsJson<List<HealthCheckExecution>>("/healthchecks-api").ConfigureAwait(false);
            report.First().Name.ShouldBe(host1.Name);
        }
    }
}
