using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestauranteApp.Models;
using RestauranteApp.Services;
using System.Security.Claims;

namespace RestauranteApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PedidoController : ControllerBase
    {
        private readonly PedidoService _pedidoService;

        public PedidoController(PedidoService pedidoService)
        {
            _pedidoService = pedidoService;
        }

        [HttpGet("meus-pedidos")]
        public async Task<IActionResult> MeusPedidos()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var pedidos = await _pedidoService.GetPedidosUsuarioAsync(userId);
            return Ok(pedidos);
        }

        [HttpPost]
        public async Task<IActionResult> CriarPedido([FromBody] CriarPedidoRequest req)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            Atendimento atendimento = req.TipoAtendimento switch
            {
                "Presencial" => new AtendimentoPresencial
                {
                    NumeroMesa = req.NumeroMesa,
                    DataHora = DateTime.Now
                },
                "DeliveryProprio" => new AtendimentoDeliveryProprio
                {
                    EnderecoEntrega = req.EnderecoEntrega,
                    DataHora = DateTime.Now
                },
                "DeliveryAplicativo" => new AtendimentoDeliveryAplicativo
                {
                    NomeAplicativo = req.NomeAplicativo ?? "iFood",
                    EnderecoEntrega = req.EnderecoEntrega,
                    DataHora = DateTime.Now
                },
                _ => return BadRequest("Tipo de atendimento inválido.")
            };

            var itensSolicitados = req.Itens.Select(i => (i.ItemId, i.Quantidade)).ToList();
            var (sucesso, mensagem, pedido) = await _pedidoService.CriarPedidoAsync(
                userId, req.Periodo, itensSolicitados, atendimento);

            if (!sucesso) return BadRequest(mensagem);

            return Ok(new
            {
                mensagem,
                pedidoId = pedido!.Id,
                total = pedido.CalcularTotal()
            });
        }

        public class CriarPedidoRequest
        {
            public Periodo Periodo { get; set; }
            public string TipoAtendimento { get; set; } = string.Empty;
            public int? NumeroMesa { get; set; }
            public string? EnderecoEntrega { get; set; }
            public string? NomeAplicativo { get; set; }
            public List<ItemRequest> Itens { get; set; } = new();
        }

        public class ItemRequest
        {
            public int ItemId { get; set; }
            public int Quantidade { get; set; } = 1;
        }
    }
}
