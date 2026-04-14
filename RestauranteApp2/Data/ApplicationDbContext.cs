using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RestauranteApp.Models;

namespace RestauranteApp.Data
{
    public class ApplicationDbContext : IdentityDbContext<Usuario>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Endereco> Enderecos { get; set; }
        public DbSet<ItemCardapio> ItensCardapio { get; set; }
        public DbSet<Ingrediente> Ingredientes { get; set; }
        public DbSet<ItemCardapioIngrediente> ItemCardapioIngredientes { get; set; }
        public DbSet<SugestaoChefe> SugestoesChefe { get; set; }
        public DbSet<Atendimento> Atendimentos { get; set; }
        public DbSet<Pedido> Pedidos { get; set; }
        public DbSet<PedidoItem> PedidoItens { get; set; }
        public DbSet<Mesa> Mesas { get; set; }
        public DbSet<Reserva> Reservas { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Chave composta N-N ItemCardapio <-> Ingrediente
            builder.Entity<ItemCardapioIngrediente>()
                .HasKey(x => new { x.ItemCardapioId, x.IngredienteId });

            builder.Entity<ItemCardapioIngrediente>()
                .HasOne(x => x.ItemCardapio)
                .WithMany(x => x.ItemCardapioIngredientes)
                .HasForeignKey(x => x.ItemCardapioId);

            builder.Entity<ItemCardapioIngrediente>()
                .HasOne(x => x.Ingrediente)
                .WithMany(x => x.ItemCardapioIngredientes)
                .HasForeignKey(x => x.IngredienteId);

            // Herança de Atendimento - TPH (Table Per Hierarchy)
            builder.Entity<Atendimento>()
                .HasDiscriminator<string>("TipoAtendimento")
                .HasValue<AtendimentoPresencial>("Presencial")
                .HasValue<AtendimentoDeliveryProprio>("DeliveryProprio")
                .HasValue<AtendimentoDeliveryAplicativo>("DeliveryAplicativo");

            // Precisão decimal
            builder.Entity<ItemCardapio>()
                .Property(x => x.PrecoBase)
                .HasColumnType("decimal(10,2)");

            builder.Entity<PedidoItem>()
                .Property(x => x.PrecoBase)
                .HasColumnType("decimal(10,2)");

            builder.Entity<PedidoItem>()
                .Property(x => x.PrecoFinal)
                .HasColumnType("decimal(10,2)");

            builder.Entity<AtendimentoDeliveryProprio>()
                .Property(x => x.TaxaFixa)
                .HasColumnType("decimal(10,2)");

            // Sugestão do chefe: regra de só 1 por período por dia
            // enforced via index único
            builder.Entity<SugestaoChefe>()
                .HasIndex(x => new { x.Data, x.Periodo })
                .IsUnique();

            // Seed de Mesas
            builder.Entity<Mesa>().HasData(
                new Mesa { Id = 1, Numero = 1, Capacidade = 2 },
                new Mesa { Id = 2, Numero = 2, Capacidade = 4 },
                new Mesa { Id = 3, Numero = 3, Capacidade = 4 },
                new Mesa { Id = 4, Numero = 4, Capacidade = 6 },
                new Mesa { Id = 5, Numero = 5, Capacidade = 8 }
            );

            // Seed de Ingredientes
            builder.Entity<Ingrediente>().HasData(
                new Ingrediente { Id = 1, Nome = "Arroz" },
                new Ingrediente { Id = 2, Nome = "Feijão" },
                new Ingrediente { Id = 3, Nome = "Frango" },
                new Ingrediente { Id = 4, Nome = "Carne Bovina" },
                new Ingrediente { Id = 5, Nome = "Alface" },
                new Ingrediente { Id = 6, Nome = "Tomate" },
                new Ingrediente { Id = 7, Nome = "Macarrão" },
                new Ingrediente { Id = 8, Nome = "Molho de Tomate" },
                new Ingrediente { Id = 9, Nome = "Queijo" },
                new Ingrediente { Id = 10, Nome = "Presunto" }
            );

            // Seed de Itens de Cardápio (20 almoço + 20 jantar)
            var itens = new List<ItemCardapio>();
            // Almoço
            itens.AddRange(new[]
            {
                new ItemCardapio { Id = 1,  Nome = "Frango Grelhado",       Descricao = "Frango grelhado com salada",         PrecoBase = 35.90m, Periodo = Periodo.Almoco },
                new ItemCardapio { Id = 2,  Nome = "PF Completo",           Descricao = "Arroz, feijão, carne e salada",      PrecoBase = 28.00m, Periodo = Periodo.Almoco },
                new ItemCardapio { Id = 3,  Nome = "Macarrão ao Sugo",      Descricao = "Macarrão com molho de tomate",       PrecoBase = 25.00m, Periodo = Periodo.Almoco },
                new ItemCardapio { Id = 4,  Nome = "Peixe Assado",          Descricao = "Filé de peixe assado com legumes",   PrecoBase = 42.00m, Periodo = Periodo.Almoco },
                new ItemCardapio { Id = 5,  Nome = "Salada Caesar",         Descricao = "Salada com croutons e molho caesar", PrecoBase = 22.00m, Periodo = Periodo.Almoco },
                new ItemCardapio { Id = 6,  Nome = "Bife Acebolado",        Descricao = "Bife com cebola caramelizada",       PrecoBase = 38.00m, Periodo = Periodo.Almoco },
                new ItemCardapio { Id = 7,  Nome = "Risoto de Frango",      Descricao = "Risoto cremoso de frango",           PrecoBase = 32.00m, Periodo = Periodo.Almoco },
                new ItemCardapio { Id = 8,  Nome = "Feijoada",              Descricao = "Feijoada completa",                  PrecoBase = 45.00m, Periodo = Periodo.Almoco },
                new ItemCardapio { Id = 9,  Nome = "Moqueca de Tilápia",    Descricao = "Moqueca com leite de coco",          PrecoBase = 48.00m, Periodo = Periodo.Almoco },
                new ItemCardapio { Id = 10, Nome = "Wrap de Frango",        Descricao = "Wrap com frango e legumes",          PrecoBase = 27.00m, Periodo = Periodo.Almoco },
                new ItemCardapio { Id = 11, Nome = "Espaguete Carbonara",   Descricao = "Espaguete com molho cremoso",        PrecoBase = 33.00m, Periodo = Periodo.Almoco },
                new ItemCardapio { Id = 12, Nome = "Frango à Parmegiana",   Descricao = "Frango empanado com molho",          PrecoBase = 36.00m, Periodo = Periodo.Almoco },
                new ItemCardapio { Id = 13, Nome = "Strogonoff de Carne",   Descricao = "Strogonoff com arroz e batata",      PrecoBase = 39.00m, Periodo = Periodo.Almoco },
                new ItemCardapio { Id = 14, Nome = "Hambúrguer Artesanal",  Descricao = "Hambúrguer com queijo e bacon",      PrecoBase = 34.00m, Periodo = Periodo.Almoco },
                new ItemCardapio { Id = 15, Nome = "Omelete de Legumes",    Descricao = "Omelete recheado com legumes",       PrecoBase = 22.00m, Periodo = Periodo.Almoco },
                new ItemCardapio { Id = 16, Nome = "Caldo de Feijão",       Descricao = "Caldo grosso com torresmo",          PrecoBase = 18.00m, Periodo = Periodo.Almoco },
                new ItemCardapio { Id = 17, Nome = "Carne Assada",          Descricao = "Carne assada com mandioca",          PrecoBase = 41.00m, Periodo = Periodo.Almoco },
                new ItemCardapio { Id = 18, Nome = "Panqueca de Carne",     Descricao = "Panqueca recheada com molho",        PrecoBase = 29.00m, Periodo = Periodo.Almoco },
                new ItemCardapio { Id = 19, Nome = "Filé de Frango",        Descricao = "Filé com purê de batatas",           PrecoBase = 37.00m, Periodo = Periodo.Almoco },
                new ItemCardapio { Id = 20, Nome = "Vegetariano do Dia",    Descricao = "Opção vegetariana variada",          PrecoBase = 24.00m, Periodo = Periodo.Almoco },
            });
            // Jantar
            itens.AddRange(new[]
            {
                new ItemCardapio { Id = 21, Nome = "Picanha Grelhada",      Descricao = "Picanha ao ponto com vinagrete",     PrecoBase = 75.00m, Periodo = Periodo.Jantar },
                new ItemCardapio { Id = 22, Nome = "Salmão ao Molho",       Descricao = "Salmão com molho de ervas",          PrecoBase = 68.00m, Periodo = Periodo.Jantar },
                new ItemCardapio { Id = 23, Nome = "Fraldinha ao Vinho",    Descricao = "Fraldinha marinada em vinho tinto",  PrecoBase = 62.00m, Periodo = Periodo.Jantar },
                new ItemCardapio { Id = 24, Nome = "Camarão na Manteiga",   Descricao = "Camarão grelhado na manteiga",       PrecoBase = 72.00m, Periodo = Periodo.Jantar },
                new ItemCardapio { Id = 25, Nome = "Risoto de Camarão",     Descricao = "Risoto cremoso de camarão",          PrecoBase = 65.00m, Periodo = Periodo.Jantar },
                new ItemCardapio { Id = 26, Nome = "Cordeiro Assado",       Descricao = "Carré de cordeiro com legumes",      PrecoBase = 89.00m, Periodo = Periodo.Jantar },
                new ItemCardapio { Id = 27, Nome = "Nhoque de Batata",      Descricao = "Nhoque artesanal ao sugo",           PrecoBase = 45.00m, Periodo = Periodo.Jantar },
                new ItemCardapio { Id = 28, Nome = "Filé Mignon",           Descricao = "Filé mignon ao molho madeira",      PrecoBase = 85.00m, Periodo = Periodo.Jantar },
                new ItemCardapio { Id = 29, Nome = "Bacalhau Gratinado",    Descricao = "Bacalhau com batatas e azeitonas",   PrecoBase = 78.00m, Periodo = Periodo.Jantar },
                new ItemCardapio { Id = 30, Nome = "Frango Recheado",       Descricao = "Frango recheado com farofa",         PrecoBase = 52.00m, Periodo = Periodo.Jantar },
                new ItemCardapio { Id = 31, Nome = "Polvo Grelhado",        Descricao = "Polvo com azeite e batata",          PrecoBase = 82.00m, Periodo = Periodo.Jantar },
                new ItemCardapio { Id = 32, Nome = "Magret de Pato",        Descricao = "Pato com geleia de laranja",         PrecoBase = 91.00m, Periodo = Periodo.Jantar },
                new ItemCardapio { Id = 33, Nome = "Steak de Atum",         Descricao = "Atum selado com gergelim",           PrecoBase = 74.00m, Periodo = Periodo.Jantar },
                new ItemCardapio { Id = 34, Nome = "Costela ao Forno",      Descricao = "Costela bovina assada lentamente",   PrecoBase = 67.00m, Periodo = Periodo.Jantar },
                new ItemCardapio { Id = 35, Nome = "Ravioli de Funghi",     Descricao = "Ravioli recheado com funghi",        PrecoBase = 55.00m, Periodo = Periodo.Jantar },
                new ItemCardapio { Id = 36, Nome = "Peixe ao Creme",        Descricao = "Peixe branco com molho de creme",    PrecoBase = 59.00m, Periodo = Periodo.Jantar },
                new ItemCardapio { Id = 37, Nome = "Lula Recheada",         Descricao = "Lula recheada ao forno",             PrecoBase = 63.00m, Periodo = Periodo.Jantar },
                new ItemCardapio { Id = 38, Nome = "Carré de Porco",        Descricao = "Carré com crosta de ervas",          PrecoBase = 70.00m, Periodo = Periodo.Jantar },
                new ItemCardapio { Id = 39, Nome = "Duo de Proteínas",      Descricao = "Combinação de carne e frango",       PrecoBase = 79.00m, Periodo = Periodo.Jantar },
                new ItemCardapio { Id = 40, Nome = "Jantar Vegetariano",    Descricao = "Prato vegetariano elaborado",        PrecoBase = 48.00m, Periodo = Periodo.Jantar },
            });
            builder.Entity<ItemCardapio>().HasData(itens);
        }
    }
}
