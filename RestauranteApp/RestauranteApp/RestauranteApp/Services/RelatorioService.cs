using Microsoft.EntityFrameworkCore;
using RestauranteApp.Data;
using RestauranteApp.Models;

namespace RestauranteApp.Services
{
    public class RelatorioFaturamento
    {
        public string TipoAtendimento { get; set; } = string.Empty;
        public int TotalPedidos { get; set; }
        public decimal TotalFaturado { get; set; }
    }

    public class RelatorioItemVendido
    {
        public string NomeItem { get; set; } = string.Empty;
        public string Periodo { get; set; } = string.Empty;
        public int QuantidadeVendida { get; set; }
        public int VendasComoSugestao { get; set; }
        public decimal ReceitaTotal { get; set; }
    }

    public class RelatorioService
    {
        private readonly ApplicationDbContext _context;

        public RelatorioService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<RelatorioFaturamento>> GetFaturamentoPorTipoAsync(
            DateTime dataInicio, DateTime dataFim)
        {
            var pedidos = await _context.Pedidos
                .Include(p => p.PedidoItens)
                .Include(p => p.Atendimento)
                .Where(p => p.DataHora >= dataInicio && p.DataHora <= dataFim)
                .ToListAsync();

            return pedidos
                .GroupBy(p => p.Atendimento!.TipoAtendimento)
                .Select(g => new RelatorioFaturamento
                {
                    TipoAtendimento = g.Key,
                    TotalPedidos = g.Count(),
                    TotalFaturado = g.Sum(p => p.CalcularTotal())
                })
                .OrderByDescending(r => r.TotalFaturado)
                .ToList();
        }

        public async Task<List<RelatorioItemVendido>> GetItensMaisVendidosAsync(
            DateTime dataInicio, DateTime dataFim)
        {
            var itens = await _context.PedidoItens
                .Include(pi => pi.ItemCardapio)
                .Include(pi => pi.Pedido)
                .Where(pi => pi.Pedido!.DataHora >= dataInicio && pi.Pedido.DataHora <= dataFim)
                .ToListAsync();

            return itens
                .GroupBy(pi => pi.ItemCardapioId)
                .Select(g => new RelatorioItemVendido
                {
                    NomeItem = g.First().ItemCardapio!.Nome,
                    Periodo = g.First().ItemCardapio!.Periodo.ToString(),
                    QuantidadeVendida = g.Sum(pi => pi.Quantidade),
                    VendasComoSugestao = g.Count(pi => pi.FoiSugestaoChefe),
                    ReceitaTotal = g.Sum(pi => pi.PrecoFinal * pi.Quantidade)
                })
                .OrderByDescending(r => r.QuantidadeVendida)
                .ToList();
        }
    }
}
