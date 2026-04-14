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
                .Where(i => i.Periodo == periodo)
                .Include(i => i.ItemCardapioIngredientes)
                    .ThenInclude(ii => ii.Ingrediente)
                .ToListAsync();
        }

        public async Task<SugestaoChefe?> GetSugestaoHojeAsync(Periodo periodo)
        {
            var hoje = DateTime.Today;
            return await _context.SugestoesChefe
                .Include(s => s.ItemCardapio)
                .FirstOrDefaultAsync(s => s.Data == hoje && s.Periodo == periodo);
        }

        public async Task<bool> DefinirSugestaoChefe(int itemId, Periodo periodo)
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
                Data = hoje,
                Periodo = periodo,
                ItemCardapioId = itemId
            });

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
