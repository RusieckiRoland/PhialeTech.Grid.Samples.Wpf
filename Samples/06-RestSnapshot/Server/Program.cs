using PhialeTech.Grid.Samples.PriceReview.Server;
var app = PriceServer.Create();
await PriceServer.InitializeAsync(app);
await app.RunAsync();
