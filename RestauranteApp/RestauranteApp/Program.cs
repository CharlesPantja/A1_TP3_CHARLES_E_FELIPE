using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RestauranteApp.Data;
using RestauranteApp.Models;
using RestauranteApp.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<Usuario, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

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

builder.Services.AddScoped<CardapioService>();
builder.Services.AddScoped<PedidoService>();
builder.Services.AddScoped<ReservaService>();
builder.Services.AddScoped<RelatorioService>();

builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
        policy.WithOrigins("http://localhost:5500", "http://127.0.0.1:5500")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ─── SEED ────────────────────────────────────────────────────────────────────
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
            UserName       = adminEmail,
            Email          = adminEmail,
            NomeCompleto   = "Administrador",
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

    // ─── SEED MESAS ──────────────────────────────────────────────────────────
    if (!db.Mesas.Any())
    {
        var mesas = new List<Mesa>
        {
            new Mesa { Numero = 1,  Capacidade = 2,  Ativa = true },
            new Mesa { Numero = 2,  Capacidade = 2,  Ativa = true },
            new Mesa { Numero = 3,  Capacidade = 4,  Ativa = true },
            new Mesa { Numero = 4,  Capacidade = 4,  Ativa = true },
            new Mesa { Numero = 5,  Capacidade = 4,  Ativa = true },
            new Mesa { Numero = 6,  Capacidade = 6,  Ativa = true },
            new Mesa { Numero = 7,  Capacidade = 6,  Ativa = true },
            new Mesa { Numero = 8,  Capacidade = 8,  Ativa = true },
            new Mesa { Numero = 9,  Capacidade = 8,  Ativa = true },
            new Mesa { Numero = 10, Capacidade = 10, Ativa = true },
        };
        db.Mesas.AddRange(mesas);
        await db.SaveChangesAsync();
    }

    // ─── SEED CARDÁPIO (20 almoço + 20 jantar) ───────────────────────────────
    if (!db.ItensCardapio.Any())
    {
        var itens = new List<ItemCardapio>
        {
            // ── ALMOÇO (20 itens) ─────────────────────────────────────────
            new ItemCardapio
            {
                Nome      = "Filé ao Molho Madeira",
                Descricao = "Filé mignon grelhado ao ponto com molho madeira reduzido, acompanhado de arroz branco e batata rústica",
                PrecoBase = 58.90m,
                Periodo   = Periodo.Almoco
            },
            new ItemCardapio
            {
                Nome      = "Frango Grelhado com Legumes",
                Descricao = "Peito de frango grelhado temperado com ervas finas, acompanhado de legumes salteados no azeite",
                PrecoBase = 42.90m,
                Periodo   = Periodo.Almoco
            },
            new ItemCardapio
            {
                Nome      = "Salmão ao Limão Siciliano",
                Descricao = "Posta de salmão grelhada com manteiga de limão siciliano e alcaparras, servida com arroz de ervas",
                PrecoBase = 68.90m,
                Periodo   = Periodo.Almoco
            },
            new ItemCardapio
            {
                Nome      = "Massa Carbonara",
                Descricao = "Tagliatelle fresco ao molho carbonara clássico com pancetta crocante, gema caipira e parmesão",
                PrecoBase = 38.90m,
                Periodo   = Periodo.Almoco
            },
            new ItemCardapio
            {
                Nome      = "Risoto de Cogumelos",
                Descricao = "Arroz arbóreo cremoso com mix de cogumelos frescos, parmesão e finalizado com azeite trufado",
                PrecoBase = 46.90m,
                Periodo   = Periodo.Almoco
            },
            new ItemCardapio
            {
                Nome      = "Salada Caesar com Frango",
                Descricao = "Alface romana, frango grelhado fatiado, croutons artesanais, parmesão e molho caesar clássico",
                PrecoBase = 34.90m,
                Periodo   = Periodo.Almoco
            },
            new ItemCardapio
            {
                Nome      = "Frango à Parmegiana",
                Descricao = "Filé de frango empanado coberto com molho de tomate artesanal e queijo gratinado, servido com arroz e fritas",
                PrecoBase = 45.90m,
                Periodo   = Periodo.Almoco
            },
            new ItemCardapio
            {
                Nome      = "Strogonoff de Carne",
                Descricao = "Tiras de filé ao molho creme com champignon e catchup especial, acompanhado de arroz e batata palha",
                PrecoBase = 48.90m,
                Periodo   = Periodo.Almoco
            },
            new ItemCardapio
            {
                Nome      = "Tilápia Grelhada",
                Descricao = "Filé de tilápia grelhado temperado com limão e ervas, servido com purê de batata e salada verde",
                PrecoBase = 44.90m,
                Periodo   = Periodo.Almoco
            },
            new ItemCardapio
            {
                Nome      = "Penne ao Bolonhesa",
                Descricao = "Macarrão penne com ragú de carne moída ao molho de tomate fresco, finalizado com manjericão",
                PrecoBase = 36.90m,
                Periodo   = Periodo.Almoco
            },
            new ItemCardapio
            {
                Nome      = "Bife Grelhado com Mandioca",
                Descricao = "Bife de alcatra grelhado com alho, servido com mandioca cozida temperada e arroz",
                PrecoBase = 42.90m,
                Periodo   = Periodo.Almoco
            },
            new ItemCardapio
            {
                Nome      = "Costelinha ao Molho Barbecue",
                Descricao = "Costelinha suína caramelizada no molho barbecue artesanal, servida com arroz e couve refogada",
                PrecoBase = 56.90m,
                Periodo   = Periodo.Almoco
            },
            new ItemCardapio
            {
                Nome      = "Moqueca de Frango",
                Descricao = "Frango cozido no leite de coco com azeite de dendê, pimentões e coentro, servido com arroz e farofa",
                PrecoBase = 49.90m,
                Periodo   = Periodo.Almoco
            },
            new ItemCardapio
            {
                Nome      = "Filé de Peixe com Purê",
                Descricao = "Filé de peixe ao molho de maracujá com gengibre, acompanhado de purê de batata baroa",
                PrecoBase = 47.90m,
                Periodo   = Periodo.Almoco
            },
            new ItemCardapio
            {
                Nome      = "Wrap de Frango",
                Descricao = "Tortilha integral com frango grelhado desfiado, cream cheese, alface, tomate e molho pesto",
                PrecoBase = 32.90m,
                Periodo   = Periodo.Almoco
            },
            new ItemCardapio
            {
                Nome      = "Picadinho de Filé",
                Descricao = "Cubos de filé mignon ao molho de alho com cebola caramelizada, servido com arroz e feijão tropeiro",
                PrecoBase = 54.90m,
                Periodo   = Periodo.Almoco
            },
            new ItemCardapio
            {
                Nome      = "Risoto de Frango com Açafrão",
                Descricao = "Arroz arbóreo com frango desfiado ao açafrão e parmesão, finalizado com manteiga e ervas",
                PrecoBase = 43.90m,
                Periodo   = Periodo.Almoco
            },
            new ItemCardapio
            {
                Nome      = "Macarrão ao Alho e Óleo",
                Descricao = "Espaguete ao azeite extra virgem com alho frito crocante, anchova e salsinha, com opção de frango",
                PrecoBase = 33.90m,
                Periodo   = Periodo.Almoco
            },
            new ItemCardapio
            {
                Nome      = "Quiche Lorraine",
                Descricao = "Torta salgada com massa folhada, recheio de queijo, creme e bacon defumado, acompanhada de salada",
                PrecoBase = 31.90m,
                Periodo   = Periodo.Almoco
            },
            new ItemCardapio
            {
                Nome      = "Frango ao Molho de Ervas",
                Descricao = "Coxa e sobrecoxa de frango assado ao molho de ervas da Provença com batata assada e tomate",
                PrecoBase = 39.90m,
                Periodo   = Periodo.Almoco
            },

            // ── JANTAR (20 itens) ─────────────────────────────────────────
            new ItemCardapio
            {
                Nome      = "Picanha na Brasa",
                Descricao = "Picanha prime grelhada na brasa com chimichurri artesanal, farofa de manteiga e vinagrete",
                PrecoBase = 89.90m,
                Periodo   = Periodo.Jantar
            },
            new ItemCardapio
            {
                Nome      = "Polvo à Lagareiro",
                Descricao = "Polvo ao forno com azeite extra virgem, batatas assadas, azeitonas e alho confitado",
                PrecoBase = 92.90m,
                Periodo   = Periodo.Jantar
            },
            new ItemCardapio
            {
                Nome      = "Cordeiro com Ervas",
                Descricao = "Paleta de cordeiro assada lentamente com ervas da Provença, alecrim e legumes rústicos",
                PrecoBase = 98.90m,
                Periodo   = Periodo.Jantar
            },
            new ItemCardapio
            {
                Nome      = "Risoto de Camarão",
                Descricao = "Arroz arbóreo cremoso com camarões ao alho e azeite, finalizado com cream cheese e tomilho",
                PrecoBase = 76.90m,
                Periodo   = Periodo.Jantar
            },
            new ItemCardapio
            {
                Nome      = "Tagliatelle ao Pesto de Rúcula",
                Descricao = "Massa fresca ao pesto de rúcula com tomate seco, nozes e lascas de parmesão",
                PrecoBase = 52.90m,
                Periodo   = Periodo.Jantar
            },
            new ItemCardapio
            {
                Nome      = "Duo de Filés",
                Descricao = "Medalhão de filé mignon e filé de peixe grelhados, molho de manteiga e ervas com purê trufado",
                PrecoBase = 85.90m,
                Periodo   = Periodo.Jantar
            },
            new ItemCardapio
            {
                Nome      = "Chateaubriand ao Béarnaise",
                Descricao = "Corte nobre de filé mignon ao molho béarnaise clássico, com batatas gratinadas e aspargos",
                PrecoBase = 105.90m,
                Periodo   = Periodo.Jantar
            },
            new ItemCardapio
            {
                Nome      = "Lagosta Grelhada",
                Descricao = "Lagosta fresca grelhada na manteiga com ervas finas, servida com arroz de açafrão e salada",
                PrecoBase = 148.90m,
                Periodo   = Periodo.Jantar
            },
            new ItemCardapio
            {
                Nome      = "Filé Wellington",
                Descricao = "Filé mignon envolto em duxelles de cogumelos e massa folhada, ao ponto perfeito com molho de vinho",
                PrecoBase = 118.90m,
                Periodo   = Periodo.Jantar
            },
            new ItemCardapio
            {
                Nome      = "Bacalhau ao Forno",
                Descricao = "Lombo de bacalhau dessalgado assado no azeite com batatas, cebola, azeitonas e ovos cozidos",
                PrecoBase = 88.90m,
                Periodo   = Periodo.Jantar
            },
            new ItemCardapio
            {
                Nome      = "Magret de Pato",
                Descricao = "Peito de pato ao ponto com geleia de frutas vermelhas, batata duchess e brócolis salteado",
                PrecoBase = 96.90m,
                Periodo   = Periodo.Jantar
            },
            new ItemCardapio
            {
                Nome      = "Costela Bovina ao Vinho",
                Descricao = "Costela bovina braseada por 8 horas ao vinho tinto e legumes, servida com polenta cremosa",
                PrecoBase = 82.90m,
                Periodo   = Periodo.Jantar
            },
            new ItemCardapio
            {
                Nome      = "Camarão na Moranga",
                Descricao = "Camarões ao molho de coco com catupiry dentro de uma moranga assada, servida com arroz branco",
                PrecoBase = 79.90m,
                Periodo   = Periodo.Jantar
            },
            new ItemCardapio
            {
                Nome      = "Nhoque ao Gorgonzola",
                Descricao = "Nhoque de batata artesanal ao molho cremoso de gorgonzola com nozes e peras caramelizadas",
                PrecoBase = 59.90m,
                Periodo   = Periodo.Jantar
            },
            new ItemCardapio
            {
                Nome      = "Paleta de Porco ao Forno",
                Descricao = "Paleta suína assada lentamente com laranja, alecrim e mostarda, servida com arroz com amêndoas",
                PrecoBase = 72.90m,
                Periodo   = Periodo.Jantar
            },
            new ItemCardapio
            {
                Nome      = "Salmão Teriyaki",
                Descricao = "Filé de salmão glaceado no molho teriyaki artesanal, servido com arroz de gergelim e edamame",
                PrecoBase = 74.90m,
                Periodo   = Periodo.Jantar
            },
            new ItemCardapio
            {
                Nome      = "Carré de Cordeiro",
                Descricao = "Carré de cordeiro grelhado com crosta de ervas e alho, servido com risoto de queijo e rúcula",
                PrecoBase = 112.90m,
                Periodo   = Periodo.Jantar
            },
            new ItemCardapio
            {
                Nome      = "Fusilli com Frutos do Mar",
                Descricao = "Macarrão fusilli ao molho de tomate fresco com camarão, lula e mexilhão ao vinho branco",
                PrecoBase = 69.90m,
                Periodo   = Periodo.Jantar
            },
            new ItemCardapio
            {
                Nome      = "Ravióli de Ricota",
                Descricao = "Ravióli recheado com ricota e espinafre ao molho sugo de tomate pelado e manjericão fresco",
                PrecoBase = 54.90m,
                Periodo   = Periodo.Jantar
            },
            new ItemCardapio
            {
                Nome      = "Entrecôte ao Molho de Vinho",
                Descricao = "Entrecôte grelhado ao molho de redução de vinho tinto com cebola roxa, servido com fritas artesanais",
                PrecoBase = 94.90m,
                Periodo   = Periodo.Jantar
            },
        };

        db.ItensCardapio.AddRange(itens);
        await db.SaveChangesAsync();
    }
}
// ─────────────────────────────────────────────────────────────────────────────

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
}

app.UseCors("FrontendPolicy");
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapFallbackToFile("welcome.html");

app.Run();
