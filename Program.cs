using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.EntityFrameworkCore;
using Swilago.Data;
using Swilago.Interfaces;
using Swilago.Services;
using Microsoft.AspNetCore.SpaServices.AngularCli;

/// <summary>
/// �츮 DEV���� ���ɸ��� ���� ������ ������ �귿 ���α׷��� ����� �����ϵ��� ����.
/// 1. ������ �츮�� ���� ������ �⺻��� �Ǿ� �ִ�.(SQL DB ���)
/// 2. ������ ������ �߰� �� �� �ִ�.(CRUD)
/// 3. ����� ����ġ ������ �ٽ� ���� �� �ִ�.
/// 4. ���� ����� �ش� ���ڿ� �Բ� ����Ǿ� ���� ���ǥ�� ����� �� �ֵ��� �Ѵ�.
/// 5. �� �Ŀ� ������� �����Ͱ� �׿����� ������ ��õ ������ ȭ�� ��ܿ� �ߵ��� �Ѵ�.(ó���� �ܼ����, ���Ŀ� �ӽŷ���)
/// </summary>
const string clientId = "233e7892-1986-41e9-a8f6-7b524d103403";
const string clientSecret = "m5Y7Q~YWVsX_O0fRGOmAbrREJU5UM5VNEi-v0";
const string tenantId = "785087ba-1e72-4e7d-b1d1-4a9639137a66";
//const string secretName = "NoboribetsuSqlServerConnectionString";
//const string secretName = "BreakSqlServerConnectionString";
const string secretName = "PamukkaleSqlServerConnectionString";

/// <summary>
/// Ȯ������ ������ �����ϴ� ������ �� ���ϴ�. �̷��� ��... Ȯ�� �غ���.
/// (
/// ���� : https://docs.microsoft.com/ko-kr/azure/key-vault/general/developers-guide#authenticate-to-key-vault-in-code
/// KeyVault�� Azure AD ������ ����ϸ�, Azure AD ���� ��ü�� �׼��� ������ �ο��ؾ� �մϴ�./// 
/// )
/// TokenCredential : OAuth ��ū�� ������ �� �ִ� �ڰ� ������ ��Ÿ���ϴ�.
/// ChaninedTokenCredential : TokenCredential�� GetToken�޼��� �߿��� �ϳ��� �⺻�� �ƴ� AccessToken���� ��ȯ���� �� �ʿ��� ���� TokenCredental ������ ������ �ݴϴ�.(����ڰ� ���� �ڰ� ������ �����ϴ� ����� ���� ���� �帧�� ������ �� �ֽ��ϴ�.)
/// ManagedIdentityCredential : �����Ǵ� ID�� ���ҽ��� ������ �� �ִ� ManagedIdentityCredential�� �ν��Ͻ��� ����ϴ�.(TokenCredential�� inherit ��)
/// EnvironmentCredential : EnvironmentCredential Ŭ������ �ν��Ͻ��� ����� ȯ�� �������� Ŭ���̾�Ʈ ��� ���� ������ �н��ϴ�. ���� ����Ǵ� ȯ�� ������ ã�� �� ���� ��� GetToken �޼���� ȣ��� �� �⺻ Azure.Core.AccessToken�� ��ȯ�մϴ�.(TokenCredential�� inherit ��)
/// ClientSecretCredential : Ŭ���̾�Ʈ ��ȣ�� ����Ͽ� Azure Active Directory�� ���� �����ϴ� �� �ʿ��� ���� ������ ����Ͽ� ClientSecretCredential�� �ν��Ͻ��� ����ϴ�.(TokenCredential�� inherit ��)
/// 
/// DefaultAzureCredential : ��κ��� Azure SDK ���� �ó������� ó���� �� �ִ� �⺻ �ڰ� �����Դϴ�. �̰� ����ص� ������ �ʿ��� Data�� �� ���� �̾� ����Ͽ����ϴ�.(���� �� ��������... �̷��� ����... ������)
/// </summary>
TokenCredential tokenCredential = new ChainedTokenCredential(// new DefaultAzureCredential(),
                                        new ClientSecretCredential(tenantId, clientId, clientSecret),
                                        new EnvironmentCredential(),
                                        new ManagedIdentityCredential());

var keyVaultUri = new Uri("https://yang.vault.azure.net/");
var client = new SecretClient(keyVaultUri, tokenCredential);
var secret = client.GetSecret(secretName);

Dictionary<string, string> sqlServerConnectionString = new()
{
    { "SqlServerConnection", secret.Value.Value }
};

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromDays(1);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Configuration.AddInMemoryCollection(sqlServerConnectionString);

builder.Services.AddDbContext<PamukkaleContext>(options =>
    options.UseSqlServer(builder.Configuration["SqlServerConnection"]).EnableSensitiveDataLogging());

builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy",
        build => build.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader());
});

builder.Services.AddTransient<IRestaurantService, RestaurantService>();

// Add services to the container.

builder.Services.AddControllersWithViews();

builder.Services.AddSpaStaticFiles(configuration =>
{
    configuration.RootPath = "ClientApp/dist";
});

var app = builder.Build();

app.UseCors("CorsPolicy");
//app.UseMvc();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
    app.UseDeveloperExceptionPage();
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseSession();

if (!app.Environment.IsDevelopment())
    app.UseSpaStaticFiles();

app.UseRouting();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllerRoute(
              name: "default",
              pattern: "{controller}/{action=Index}/{id?}");
});

app.UseSpa(spa =>
{
    spa.Options.SourcePath = "ClientApp";
    if (app.Environment.IsDevelopment())
        spa.UseAngularCliServer(npmScript: "start");
});

//app.MapFallbackToFile("index.html");

app.Run();
