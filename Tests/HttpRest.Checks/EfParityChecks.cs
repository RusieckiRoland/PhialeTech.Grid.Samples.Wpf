using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using PhialeTech.Grid.Samples.HttpRest.Server;

internal static class EfParityChecks
{
    public static async Task RunAsync(string database, string efAddress)
    {
        await using var sql = StockServer.Create("http://127.0.0.1:0", database, provider: "sql");
        await sql.StartAsync();
        try {
            var sqlAddress = sql.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
            var cases = new (string Name, object Request)[] {
                ("AND text and number", new { conditions = new object[] {
                    new { field = "category", comparison = "eq", value = "Cables" },
                    new { field = "onHand", comparison = "gt", value = 500m }
                } }),
                ("AND two text values", new { conditions = new[] {
                    new { field = "category", comparison = "eq", value = "Cables" },
                    new { field = "productCode", comparison = "startsWith", value = "PRZ-" }
                } }),
                ("OR text and number", new { match = "any", conditions = new object[] {
                    new { field = "category", comparison = "eq", value = "Cables" },
                    new { field = "onHand", comparison = "gt", value = 500m }
                } }),
                ("sort identity", new { orderBy = new[] { new { field = "id", direction = "asc" } } }),
                ("sort productCode descending", new { orderBy = new[] { new { field = "productCode", direction = "desc" } } }),
                ("sort unit and category", new { orderBy = new[] {
                    new { field = "unitCode", direction = "desc" }, new { field = "category", direction = "asc" }
                } }),
                ("default page", new { take = 10 }),
                ("middle page", new { skip = 12345, take = 5 }),
                ("last page", new { skip = 999995, take = 5 }),
                ("past last page", new { skip = 1000000, take = 5 }),
                ("numeric onHand descending", new { orderBy = new[] { new { field = "onHand", direction = "desc" } } }),
                ("numeric reserved ascending", new { orderBy = new[] { new { field = "reserved", direction = "asc" } } }),
                ("computed available descending", new { orderBy = new[] { new { field = "available", direction = "desc" } } }),
                ("multiple text sorts", new { orderBy = new[] { new { field = "productName", direction = "asc" }, new { field = "warehouseName", direction = "desc" } } }),
                ("category equality", Filter("category", "eq", "Cables")),
                ("quantity equality", Filter("onHand", "eq", 101.10m)),
                ("quantity less than", Filter("reserved", "lt", 10m)),
                ("quantity range", new { conditions = new[] { new { field = "onHand", comparison = "between", value = 100m, upper = 200m } } }),
                ("text contains", Filter("productName", "contains", "przewód")),
                ("uppercase Polish input", Filter("productName", "contains", "PRZEWÓD")),
                ("literal wildcard prefix", Filter("productName", "startsWith", "%")),
                ("literal underscore prefix", Filter("productCode", "startsWith", "_")),
                ("prefix", Filter("productCode", "startsWith", "PRZ-")),
                ("text greater than", Filter("productCode", "gt", "PRZ-00100")),
                ("text range", new { conditions = new[] { new { field = "category", comparison = "between", value = "Cables", upper = "Packaging" } } }),
                ("OR conditions", new { match = "any", conditions = new[] {
                    new { field = "category", comparison = "eq", value = "Cables" },
                    new { field = "productCode", comparison = "startsWith", value = "ODZ-" }
                } }),
                ("SQL injection text", Filter("productName", "contains", "' OR 1=1 --"))
            };
            var checks = 0;
            foreach (var (name, request) in cases.OrderByDescending(c => c.Name is "text greater than" or "text range" or "uppercase Polish input" or "OR conditions")) {
                using var efResponse = await client.PostAsJsonAsync(efAddress + "/api/stocks/search", request);
                using var sqlResponse = await client.PostAsJsonAsync(sqlAddress + "/api/stocks/search", request);
                efResponse.EnsureSuccessStatusCode(); sqlResponse.EnsureSuccessStatusCode();
                var efPage = (await efResponse.Content.ReadFromJsonAsync<StockPage>())!;
                var sqlPage = (await sqlResponse.Content.ReadFromJsonAsync<StockPage>())!;
                if (efPage.Total != sqlPage.Total || !efPage.Stocks.SequenceEqual(sqlPage.Stocks))
                    throw new Exception($"EF/SQL mismatch: {name}: EF {efPage.Total}, SQL {sqlPage.Total}.");
                checks++; Console.WriteLine("PASS EF/SQL parity: " + name);
            }
            foreach (var body in new[] {
                "{\"take\":1000}", "{\"orderBy\":[{\"field\":\"onHand\",\"direction\":\"desc; DROP TABLE products\"}]}",
                "{\"conditions\":[{\"field\":\"onHand\",\"comparison\":\"contains\",\"value\":\"2\"}]}",
                "{\"conditions\":[{\"field\":\"category\",\"comparison\":\"eq\",\"value\":null}]}"
            }) {
                using var document = JsonDocument.Parse(body);
                foreach (var address in new[] { efAddress, sqlAddress }) {
                    using var response = await client.PostAsJsonAsync(address + "/api/stocks/search", document.RootElement);
                    if (response.StatusCode != HttpStatusCode.BadRequest) throw new Exception("Invalid request was not rejected by " + address);
                }
                checks++; Console.WriteLine("PASS EF/SQL parity: invalid request rejected");
            }
            Console.WriteLine($"PASS {checks} EF/SQL comparison checks");
        } finally { await sql.StopAsync(); }
    }

    private static object Filter(string field, string comparison, object value) =>
        new { conditions = new[] { new { field, comparison, value } } };
}
