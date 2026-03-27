using MedicalSystem.Web.Data;
using MiniOrm.Database;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

var connectionString =
    "Host=ep-cold-thunder-agonl6uv-pooler.c-2.eu-central-1.aws.neon.tech; Database=neondb; Username=neondb_owner; Password=npg_2S5oxbmXDBwq; SSL Mode=VerifyFull; Channel Binding=Require;";

builder.Services.AddScoped<MedicalDbContext>(sp => new MedicalDbContext(connectionString));

builder.Services.AddTransient<IUnitOfWork>(sp =>
    new UnitOfWork(sp.GetRequiredService<MedicalDbContext>()));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();