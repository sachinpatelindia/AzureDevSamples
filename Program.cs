using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();


app.MapGet("/getsecret/{secretName}", (string secretName) =>
{
    //var secretName = ctx.GetRouteValue("secretName").ToString();
    if (string.IsNullOrEmpty(secretName))
        throw new InvalidOperationException();

    var client = SetupKVClient();
    KeyVaultSecret? secret = null;
    if (client is null)
        return Results.Empty;
    try
    {
        secret = client.GetSecret(secretName).Value;
    }
    catch (Exception ex)
    {
        return Results.NotFound(ex.Message);
    }

    if (secret is not null)
        return Results.Ok(secret);
    return Results.Empty;
});

app.MapPost("/createsecret", async (Dictionary<string, string> secret) =>
{
    int count = secret.Count();
    var client = SetupKVClient();

    foreach (var kv in secret)
    {
        await client.SetSecretAsync(new KeyVaultSecret(kv.Key, kv.Value));
    }
});

app.MapDelete("/deletesecret/{secretName}", async (string secretName) =>
{
    //var secretName = ctx.GetRouteValue("secretName").ToString();
    if (string.IsNullOrEmpty(secretName))
        throw new InvalidOperationException();

    var client = SetupKVClient();
    if (client is null)
        return Results.Empty;
    try
    {
        var secret = client.GetSecret(secretName).Value;
        if (secret is not null)
        {
            var operation = await client.StartDeleteSecretAsync(secretName);
            await operation.WaitForCompletionAsync();
            var result = await client.PurgeDeletedSecretAsync(secretName);
            return Results.Ok(result.Content);
        }
    }
    catch (Exception ex)
    {
        return Results.NotFound(ex.Message);
    }
    return Results.NotFound();
});

app.Run();



SecretClient SetupKVClient()
{
    var keyvaultName = builder.Configuration.GetValue<string>("AzureKeyVault:KeyVaultName");
    var url = builder.Configuration.GetValue<string>("AzureKeyVault:url");
    var keyVaultUrl = "https://" + keyvaultName + url;
    var clent = new SecretClient(new Uri(keyVaultUrl),new DefaultAzureCredential());
    return clent;
}
