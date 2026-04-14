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

        /// <summary>
        /// Retorna os pedidos do usuário logado
        /// </summary>
        [HttpGet("meus-pedidos")]
        [Authorize]
        public async Task<IActionResult> MeusPedidos()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var pedidos = await _pedidoService.GetPedidosUsuarioAsync(userId);

            var resultado = pedidos.Select(p => new
            {
                id          = p.Id,
                dataHora    = p.DataHora,
                periodo     = p.PeriodoPedido.ToString(),
                status      = p.Status.ToString(),
                statusDesc  = CalculadoraPedido.DescricaoStatus(p.Status),
                tipoAtendimento = p.Atendimento?.TipoAtendimento ?? "",
                subtotal    = p.CalcularSubtotal(),
                total       = p.CalcularTotal(),
                itens       = p.PedidoItens.Select(pi => new
                {
                    nome         = pi.ItemCardapio?.Nome ?? "",
                    quantidade   = pi.Quantidade,
                    precoFinal   = pi.PrecoFinal,
                    foiSugestao  = pi.FoiSugestaoChefe
                })
            });

            return Ok(resultado);
        }

        /// <summary>
        /// Cria um novo pedido.
        /// Pedidos presenciais: qualquer pessoa pode fazer (sem login).
        /// Delivery: exige login.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CriarPedido([FromBody] CriarPedidoRequest req)
        {
            bool isDelivery = req.TipoAtendimento is "DeliveryProprio" or "DeliveryAplicativo";

            // Delivery exige autenticação
            if (isDelivery && !User.Identity!.IsAuthenticated)
                return Unauthorized("Faça login para pedidos de delivery.");

            // Para presencial sem login, userId fica null (sem FK para AspNetUsers)
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            Atendimento atendimento = req.TipoAtendimento switch
            {
                "Presencial" => new AtendimentoPresencial
                {
                    NumeroMesa = req.NumeroMesa,
                    DataHora   = DateTime.Now
                },
                "DeliveryProprio" => new AtendimentoDeliveryProprio
                {
                    EnderecoEntrega = req.EnderecoEntrega,
                    DataHora        = DateTime.Now
                },
                "DeliveryAplicativo" => new AtendimentoDeliveryAplicativo
                {
                    NomeAplicativo  = req.NomeAplicativo ?? "iFood",
                    EnderecoEntrega = req.EnderecoEntrega,
                    DataHora        = DateTime.Now
                },
                _ => throw new ArgumentException("Tipo de atendimento inválido.")
            };

            var itensSolicitados = req.Itens.Select(i => (i.ItemId, i.Quantidade)).ToList();

            var (sucesso, mensagem, pedido) = await _pedidoService.CriarPedidoAsync(
                userId, req.Periodo, itensSolicitados, atendimento);

            if (!sucesso)
                return BadRequest(mensagem);

            return Ok(new
            {
                mensagem,
                pedidoId = pedido!.Id,
                status   = pedido.Status.ToString(),
                subtotal = pedido.CalcularSubtotal(),
                total    = pedido.CalcularTotal()
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
