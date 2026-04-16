using Microsoft.EntityFrameworkCore;
using RestauranteApp.Data;
using RestauranteApp.Models;

namespace RestauranteApp.Services
{
    public class CardapioService
    {
        private readonly ApplicationDbContext _context;

        public CardapioService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<ItemCardapio>> GetItensPorPeriodoAsync(Periodo periodo)
        {
            return await _context.ItensCardapio
                .Where(i => i.Periodo == periodo && i.Ativo)
                .Include(i => i.ItemCardapioIngredientes)
                    .ThenInclude(ii => ii.Ingrediente)
                .ToListAsync();
        }

        public async Task<List<ItemCardapio>> GetTodosItensAsync()
        {
            return await _context.ItensCardapio
                .OrderBy(i => i.Periodo)
                .ThenBy(i => i.Nome)
                .ToListAsync();
        }

        public async Task<SugestaoChefe?> GetSugestaoHojeAsync(Periodo periodo)
        {
            var hoje = DateTime.Today;
            return await _context.SugestoesChefe
                .Include(s => s.ItemCardapio)
                .FirstOrDefaultAsync(s => s.Data == hoje && s.Periodo == periodo);
        }

        public async Task<bool> DefinirSugestaoChefe(int itemId, Periodo periodo, decimal percentualDesconto = 20m)
        {
            var item = await _context.ItensCardapio.FindAsync(itemId);
            if (item == null || item.Periodo != periodo) return false;

            var hoje = DateTime.Today;
            var sugestaoExistente = await _context.SugestoesChefe
                .FirstOrDefaultAsync(s => s.Data == hoje && s.Periodo == periodo);

            if (sugestaoExistente != null)
                _context.SugestoesChefe.Remove(sugestaoExistente);

            _context.SugestoesChefe.Add(new SugestaoChefe
            {
                Data               = hoje,
                Periodo            = periodo,
                ItemCardapioId     = itemId,
                PercentualDesconto = percentualDesconto
            });

            await _context.SaveChangesAsync();
            return true;
        }

        // ── CRUD admin ────────────────────────────────────────────────────────

        public async Task<ItemCardapio> CriarItemAsync(ItemCardapio item)
        {
            item.Ativo = true;
            _context.ItensCardapio.Add(item);
            await _context.SaveChangesAsync();
            return item;
        }

        public async Task<(bool sucesso, string mensagem)> AtualizarItemAsync(int id, ItemCardapio dados)
        {
            var item = await _context.ItensCardapio.FindAsync(id);
            if (item == null) return (false, "Item não encontrado.");

            item.Nome      = dados.Nome;
            item.Descricao = dados.Descricao;
            item.PrecoBase = dados.PrecoBase;
            item.Periodo   = dados.Periodo;
            item.Ativo     = dados.Ativo;
            if (dados.FotoUrl != null) item.FotoUrl = dados.FotoUrl;

            await _context.SaveChangesAsync();
            return (true, "Item atualizado.");
        }

        public async Task<(bool sucesso, string mensagem)> ExcluirItemAsync(int id)
        {
            var item = await _context.ItensCardapio.FindAsync(id);
            if (item == null) return (false, "Item não encontrado.");

            // Verificar se há pedidos com este item
            bool temPedidos = await _context.PedidoItens.AnyAsync(pi => pi.ItemCardapioId == id);
            if (temPedidos)
            {
                // Apenas desativa, não deleta
                item.Ativo = false;
                await _context.SaveChangesAsync();
                return (true, "Item desativado (possui pedidos vinculados).");
            }

            _context.ItensCardapio.Remove(item);
            await _context.SaveChangesAsync();
            return (true, "Item removido.");
        }

        public async Task<string?> SalvarFotoAsync(int itemId, IFormFile foto, string webRootPath)
        {
            var item = await _context.ItensCardapio.FindAsync(itemId);
            if (item == null) return null;

            var dir = Path.Combine(webRootPath, "imagens");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            var ext = Path.GetExtension(foto.FileName).ToLowerInvariant();
            if (ext is not (".jpg" or ".jpeg" or ".png" or ".webp")) return null;

            var nomeArquivo = $"prato_{itemId}{ext}";
            var caminho = Path.Combine(dir, nomeArquivo);

            using (var stream = new FileStream(caminho, FileMode.Create))
                await foto.CopyToAsync(stream);

            item.FotoUrl = $"/imagens/{nomeArquivo}";
            await _context.SaveChangesAsync();
            return item.FotoUrl;
        }
    }
}
