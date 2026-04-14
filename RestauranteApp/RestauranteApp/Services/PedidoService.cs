using Microsoft.EntityFrameworkCore;
using RestauranteApp.Data;
using RestauranteApp.Models;

namespace RestauranteApp.Services
{
    public class PedidoService
    {
        private readonly ApplicationDbContext _context;
        private readonly CardapioService _cardapioService;

        public PedidoService(ApplicationDbContext context, CardapioService cardapioService)
        {
            _context = context;
            _cardapioService = cardapioService;
        }

        public async Task<(bool sucesso, string mensagem, Pedido? pedido)> CriarPedidoAsync(
            string? usuarioId,
            Periodo periodo,
            List<(int itemId, int quantidade)> itensSolicitados,
            Atendimento atendimento)
        {
            var idsItens = itensSolicitados.Select(i => i.itemId).ToList();
            var itensDb = await _context.ItensCardapio
                .Where(i => idsItens.Contains(i.Id))
                .ToListAsync();

            var itensWrongPeriod = itensDb.Where(i => i.Periodo != periodo).ToList();
            if (itensWrongPeriod.Any())
            {
                var nomes = string.Join(", ", itensWrongPeriod.Select(i => i.Nome));
                return (false, $"Os itens a seguir não pertencem ao período {periodo}: {nomes}", null);
            }

            var sugestao = await _cardapioService.GetSugestaoHojeAsync(periodo);

            _context.Atendimentos.Add(atendimento);
            await _context.SaveChangesAsync();

            var pedido = new Pedido
            {
                UsuarioId    = usuarioId,
                PeriodoPedido = periodo,
                AtendimentoId = atendimento.Id,
                DataHora     = DateTime.Now,
                Status       = StatusPedido.Pendente
            };

            foreach (var (itemId, quantidade) in itensSolicitados)
            {
                var item = itensDb.First(i => i.Id == itemId);
                bool ehSugestao = sugestao?.ItemCardapioId == itemId;
                decimal precoFinal = ehSugestao
                    ? sugestao!.AplicarDesconto(item.PrecoBase)
                    : item.PrecoBase;

                pedido.PedidoItens.Add(new PedidoItem
                {
                    ItemCardapioId = itemId,
                    Quantidade     = quantidade,
                    PrecoBase      = item.PrecoBase,
                    PrecoFinal     = precoFinal,
                    FoiSugestaoChefe = ehSugestao
                });
            }

            _context.Pedidos.Add(pedido);
            await _context.SaveChangesAsync();

            return (true, "Pedido criado com sucesso!", pedido);
        }

        public async Task<List<Pedido>> GetPedidosUsuarioAsync(string usuarioId)
        {
            return await _context.Pedidos
                .Where(p => p.UsuarioId == usuarioId)
                .Include(p => p.PedidoItens)
                    .ThenInclude(pi => pi.ItemCardapio)
                .Include(p => p.Atendimento)
                .OrderByDescending(p => p.DataHora)
                .ToListAsync();
        }

        public async Task<List<Pedido>> GetTodosPedidosAsync(int pagina = 1, int tamanho = 20)
        {
            return await _context.Pedidos
                .Include(p => p.PedidoItens)
                    .ThenInclude(pi => pi.ItemCardapio)
                .Include(p => p.Atendimento)
                .Include(p => p.Usuario)
                .OrderByDescending(p => p.DataHora)
                .Skip((pagina - 1) * tamanho)
                .Take(tamanho)
                .ToListAsync();
        }

        public async Task<(bool sucesso, string mensagem)> CancelarPedidoAsync(int pedidoId, string usuarioId)
        {
            var pedido = await _context.Pedidos
                .FirstOrDefaultAsync(p => p.Id == pedidoId && p.UsuarioId == usuarioId);

            if (pedido == null)
                return (false, "Pedido não encontrado.");

            if (pedido.Status != StatusPedido.Pendente)
                return (false, "Apenas pedidos com status 'Pendente' podem ser cancelados.");

            pedido.Status = StatusPedido.Cancelado;
            await _context.SaveChangesAsync();

            return (true, "Pedido cancelado com sucesso.");
        }

        public async Task<(bool sucesso, string mensagem)> AtualizarStatusAsync(int pedidoId, StatusPedido novoStatus)
        {
            var pedido = await _context.Pedidos.FindAsync(pedidoId);

            if (pedido == null)
                return (false, "Pedido não encontrado.");

            pedido.Status = novoStatus;
            await _context.SaveChangesAsync();

            return (true, $"Status atualizado para: {CalculadoraPedido.DescricaoStatus(novoStatus)}");
        }
    }
}
