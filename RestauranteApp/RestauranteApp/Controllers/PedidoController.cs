using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestauranteApp.Models;
using RestauranteApp.Services;
using System.Security.Claims;

namespace RestauranteApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PedidoController : ControllerBase
    {
        private readonly PedidoService _pedidoService;

        public PedidoController(PedidoService pedidoService)
        {
            _pedidoService = pedidoService;
        }

        // GET /api/pedido/meus-pedidos — exige login
        [HttpGet("meus-pedidos")]
        [Authorize]
        public async Task<IActionResult> MeusPedidos()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var pedidos = await _pedidoService.GetPedidosUsuarioAsync(userId);

            var resultado = pedidos.Select(p => new
            {
                pedidoId   = p.Id,
                dataHora   = p.DataHora,
                fmtDH      = p.DataHora.ToString("dd/MM/yyyy HH:mm"),
                status     = p.Status,
                statusDesc = CalculadoraPedido.DescricaoStatus(p.Status),
                subtotal   = p.CalcularSubtotal(),
                total      = p.CalcularTotal(),
                tipoAtendimento = p.Atendimento?.TipoAtendimento ?? "—",
                itens = p.PedidoItens.Select(pi => new
                {
                    nome       = pi.ItemCardapio?.Nome ?? "",
                    quantidade = pi.Quantidade,
                    preco      = pi.PrecoFinal,
                    sugestao   = pi.FoiSugestaoChefe
                })
            });

            return Ok(resultado);
        }

        // POST /api/pedido — presencial sem login, delivery exige login
        [HttpPost]
        public async Task<IActionResult> CriarPedido([FromBody] PedidoCriarRequest req)
        {
            if (req.Itens == null || !req.Itens.Any())
                return BadRequest("Adicione ao menos um item ao pedido.");

            // Delivery exige autenticação
            bool isDelivery = req.TipoAtendimento is "DeliveryProprio" or "DeliveryAplicativo";
            if (isDelivery && !User.Identity!.IsAuthenticated)
                return Unauthorized("Faça login para realizar pedidos de delivery.");

            // Para presencial sem login, userId fica null (sem FK)
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Valida tipo antes do switch expression (não pode usar return dentro dele)
            if (req.TipoAtendimento is not ("Presencial" or "DeliveryProprio" or "DeliveryAplicativo"))
                return BadRequest("Tipo de atendimento inválido.");

            Atendimento atendimento = req.TipoAtendimento switch
            {
                "Presencial" => new AtendimentoPresencial
                {
                    NumeroMesa = req.NumeroMesa
                },
                "DeliveryProprio" => new AtendimentoDeliveryProprio
                {
                    EnderecoEntrega = req.EnderecoEntrega
                },
                _ => new AtendimentoDeliveryAplicativo
                {
                    NomeAplicativo  = req.NomeAplicativo ?? "Aplicativo",
                    EnderecoEntrega = req.EnderecoEntrega
                }
            };

            var itensSolicitados = req.Itens.Select(i => (i.ItemId, i.Quantidade)).ToList();

            var (sucesso, mensagem, pedido) = await _pedidoService.CriarPedidoAsync(
                userId, req.Periodo, itensSolicitados, atendimento);

            if (!sucesso) return BadRequest(mensagem);

            return Ok(new
            {
                pedidoId   = pedido!.Id,
                mensagem,
                statusDesc = CalculadoraPedido.DescricaoStatus(pedido.Status),
                subtotal   = pedido.CalcularSubtotal(),
                total      = pedido.CalcularTotal()
            });
        }

        public class PedidoCriarRequest
        {
            public string TipoAtendimento { get; set; } = "Presencial";
            public Periodo Periodo { get; set; } = Periodo.Almoco;
            public List<ItemPedidoDto> Itens { get; set; } = new();

            // Presencial
            public int? NumeroMesa { get; set; }

            // Delivery
            public string? EnderecoEntrega { get; set; }
            public string? NomeAplicativo { get; set; }
        }

        // DELETE /api/pedido/{id} — cliente cancela pedido Pendente
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> CancelarPedido(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var (sucesso, mensagem) = await _pedidoService.CancelarPedidoAsync(id, userId);
            if (!sucesso) return BadRequest(mensagem);
            return Ok(mensagem);
        }

        // PATCH /api/pedido/{id}/status — admin atualiza status do pedido
        [HttpPatch("{id}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AtualizarStatus(int id, [FromBody] AtualizarStatusRequest req)
        {
            var (sucesso, mensagem) = await _pedidoService.AtualizarStatusAsync(id, req.NovoStatus);
            if (!sucesso) return BadRequest(mensagem);
            return Ok(new { mensagem, statusDesc = CalculadoraPedido.DescricaoStatus(req.NovoStatus) });
        }

        // GET /api/pedido/todos — admin vê todos os pedidos
        [HttpGet("todos")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> TodosPedidos([FromQuery] int pagina = 1, [FromQuery] int tamanho = 20)
        {
            var pedidos = await _pedidoService.GetTodosPedidosAsync(pagina, tamanho);
            var resultado = pedidos.Select(p => new
            {
                pedidoId        = p.Id,
                dataHora        = p.DataHora,
                fmtDH           = p.DataHora.ToString("dd/MM HH:mm"),
                status          = p.Status,
                statusDesc      = CalculadoraPedido.DescricaoStatus(p.Status),
                tipoAtendimento = p.Atendimento?.TipoAtendimento ?? "—",
                numeroMesa      = (p.Atendimento as Models.AtendimentoPresencial)?.NumeroMesa,
                cliente         = p.Usuario?.NomeCompleto ?? "Cliente anônimo",
                total           = p.CalcularTotal(),
                itens           = p.PedidoItens.Select(pi => new
                {
                    nome       = pi.ItemCardapio?.Nome ?? "",
                    quantidade = pi.Quantidade
                })
            });
            return Ok(resultado);
        }

        public class AtualizarStatusRequest
        {
            public Models.StatusPedido NovoStatus { get; set; }
        }

        public class ItemPedidoDto
        {
            public int ItemId { get; set; }
            public int Quantidade { get; set; } = 1;
        }
    }
}
