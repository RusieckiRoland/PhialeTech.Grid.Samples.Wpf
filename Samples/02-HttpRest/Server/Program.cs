using PhialeTech.Grid.Samples.HttpRest.Server;

// EF Core is the default. Pass --sql to compare the same API using direct SQL.
var provider = args.Contains("--sql", StringComparer.Ordinal) ? "sql" : "ef";
await StockServer.Create(provider: provider).RunAsync();
