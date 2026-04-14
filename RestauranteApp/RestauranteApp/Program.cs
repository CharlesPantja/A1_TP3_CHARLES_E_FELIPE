using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RestauranteApp.Data;
using RestauranteApp.Models;
using RestauranteApp.Services;

var builder = WebApplication.CreateBuilder(args);

// Entity Framework + SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity (login/cadastro/roles)
builder.Services.AddIdentity<Usuario, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Cookie auth — rotas /api retornam 401 JSON, nunca redirecionam para login
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/login.html";
    options.AccessDeniedPath = "/login.html";
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.Events.OnRedirectToLogin = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        }
        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        }
        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
});

// Services
builder.Services.AddScoped<CardapioService>();
builder.Services.AddScoped<PedidoService>();
builder.Services.AddScoped<ReservaService>();
builder.Services.AddScoped<RelatorioService>();

// Controllers + CORS para o frontend
builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
        policy.WithOrigins("http://localhost:5500", "http://127.0.0.1:5500")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

// Swagger para testar a API
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Aplica migrations e seed automaticamente ao iniciar
using (var scope = app.Services.CreateScope())
{
    var db          = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Usuario>>();

    db.Database.Migrate();

    // Cria a role Admin se não existir
    if (!await roleManager.RoleExistsAsync("Admin"))
        await roleManager.CreateAsync(new IdentityRole("Admin"));

    // Cria o usuário admin padrão se não existir
    const string adminEmail = "admin@bellavista.com";
    const string adminSenha = "Admin123!";

    var admin = await userManager.FindByEmailAsync(adminEmail);
    if (admin == null)
    {
        admin = new Usuario
        {
            UserName      = adminEmail,
            Email         = adminEmail,
            NomeCompleto  = "Administrador",
            EmailConfirmed = true
        };
        var result = await userManager.CreateAsync(admin, adminSenha);
        if (result.Succeeded)
            await userManager.AddToRoleAsync(admin, "Admin");
    }
    else if (!await userManager.IsInRoleAsync(admin, "Admin"))
    {
        await userManager.AddToRoleAsync(admin, "Admin");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
}

app.UseCors("FrontendPolicy");
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Rota raiz serve welcome.html
app.MapFallbackToFile("welcome.html");

app.Run();
