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
        public decimal TicketMedio => TotalPedidos > 0 ? TotalFaturado / TotalPedidos : 0;
    }

    public class RelatorioItemVendido
    {
        public string NomeItem { get; set; } = string.Empty;
        public string Periodo { get; set; } = string.Empty;
        public int QuantidadeVendida { get; set; }
        public int VendasComoSugestao { get; set; }
        public decimal ReceitaTotal { get; set; }
    }

    public class RelatorioTopCliente
    {
        public string Nome { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int TotalPedidos { get; set; }
        public decimal TotalGasto { get; set; }
        public decimal TicketMedio => TotalPedidos > 0 ? TotalGasto / TotalPedidos : 0;
    }

    public class RelatorioHorarioPico
    {
        public int Hora { get; set; }
        public string FaixaHorario => $"{Hora:D2}h–{Hora + 1:D2}h";
        public int TotalPedidos { get; set; }
        public decimal TotalFaturado { get; set; }
    }

    public class RelatorioService
    {
        private readonly ApplicationDbContext _context;

        public RelatorioService(ApplicationDbContext context)
        {
            _context = context;
        }

        // ── Faturamento por tipo de atendimento ──────────────────────────────
        public async Task<List<RelatorioFaturamento>> GetFaturamentoPorTipoAsync(
            DateTime dataInicio, DateTime dataFim)
        {
            var pedidos = await _context.Pedidos
                .Include(p => p.PedidoItens)
                .Include(p => p.Atendimento)
                .Where(p => p.DataHora.Date >= dataInicio.Date
                         && p.DataHora.Date <= dataFim.Date
                         && p.Status != StatusPedido.Cancelado)
                .ToListAsync();

            return pedidos
                .GroupBy(p => p.Atendimento!.TipoAtendimento)
                .Select(g => new RelatorioFaturamento
                {
                    TipoAtendimento = g.Key,
                    TotalPedidos    = g.Count(),
                    TotalFaturado   = g.Sum(p => p.CalcularTotal())
                })
                .OrderByDescending(r => r.TotalFaturado)
                .ToList();
        }

        // ── Itens mais vendidos ───────────────────────────────────────────────
        public async Task<List<RelatorioItemVendido>> GetItensMaisVendidosAsync(
            DateTime dataInicio, DateTime dataFim)
        {
            var itens = await _context.PedidoItens
                .Include(pi => pi.ItemCardapio)
                .Include(pi => pi.Pedido)
                .Where(pi => pi.Pedido!.DataHora.Date >= dataInicio.Date
                          && pi.Pedido.DataHora.Date <= dataFim.Date
                          && pi.Pedido.Status != StatusPedido.Cancelado)
                .ToListAsync();

            return itens
                .GroupBy(pi => pi.ItemCardapioId)
                .Select(g => new RelatorioItemVendido
                {
                    NomeItem         = g.First().ItemCardapio!.Nome,
                    Periodo          = g.First().ItemCardapio!.Periodo.ToString(),
                    QuantidadeVendida= g.Sum(pi => pi.Quantidade),
                    VendasComoSugestao = g.Count(pi => pi.FoiSugestaoChefe),
                    ReceitaTotal     = g.Sum(pi => pi.PrecoFinal * pi.Quantidade)
                })
                .OrderByDescending(r => r.QuantidadeVendida)
                .ToList();
        }

        // ── Top clientes ─────────────────────────────────────────────────────
        public async Task<List<RelatorioTopCliente>> GetTopClientesAsync(
            DateTime dataInicio, DateTime dataFim, int top = 10)
        {
            var pedidos = await _context.Pedidos
                .Include(p => p.PedidoItens)
                .Include(p => p.Atendimento)
                .Include(p => p.Usuario)
                .Where(p => p.DataHora.Date >= dataInicio.Date
                         && p.DataHora.Date <= dataFim.Date
                         && p.Status != StatusPedido.Cancelado)
                .ToListAsync();

            return pedidos
                .GroupBy(p => p.UsuarioId)
                .Select(g => new RelatorioTopCliente
                {
                    Nome         = g.First().Usuario?.NomeCompleto ?? "—",
                    Email        = g.First().Usuario?.Email ?? "—",
                    TotalPedidos = g.Count(),
                    TotalGasto   = g.Sum(p => p.CalcularTotal())
                })
                .OrderByDescending(r => r.TotalGasto)
                .Take(top)
                .ToList();
        }

        // ── Horário de pico ───────────────────────────────────────────────────
        public async Task<List<RelatorioHorarioPico>> GetHorarioPicoAsync(
            DateTime dataInicio, DateTime dataFim)
        {
            var pedidos = await _context.Pedidos
                .Include(p => p.PedidoItens)
                .Include(p => p.Atendimento)
                .Where(p => p.DataHora.Date >= dataInicio.Date
                         && p.DataHora.Date <= dataFim.Date
                         && p.Status != StatusPedido.Cancelado)
                .ToListAsync();

            return pedidos
                .GroupBy(p => p.DataHora.Hour)
                .Select(g => new RelatorioHorarioPico
                {
                    Hora          = g.Key,
                    TotalPedidos  = g.Count(),
                    TotalFaturado = g.Sum(p => p.CalcularTotal())
                })
                .OrderByDescending(r => r.TotalPedidos)
                .ToList();
        }
    }
}
