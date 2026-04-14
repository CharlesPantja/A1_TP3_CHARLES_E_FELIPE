using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestauranteApp.Models;
using RestauranteApp.Services;

namespace RestauranteApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CardapioController : ControllerBase
    {
        private readonly CardapioService _cardapioService;

        public CardapioController(CardapioService cardapioService)
        {
            _cardapioService = cardapioService;
        }

        // GET /api/cardapio/almoco
        [HttpGet("almoco")]
        public async Task<IActionResult> GetAlmoco()
        {
            return Ok(await BuildResponse(Periodo.Almoco));
        }

        // GET /api/cardapio/jantar
        [HttpGet("jantar")]
        public async Task<IActionResult> GetJantar()
        {
            return Ok(await BuildResponse(Periodo.Jantar));
        }

        // GET /api/cardapio?periodo=Almoco|Jantar  (alias para compatibilidade)
        [HttpGet]
        public async Task<IActionResult> GetPorPeriodo([FromQuery] string periodo = "Almoco")
        {
            var p = periodo.Equals("Jantar", StringComparison.OrdinalIgnoreCase) ? Periodo.Jantar : Periodo.Almoco;
            return Ok(await BuildResponse(p));
        }

        // GET /api/cardapio/sugestao?periodo=Almoco|Jantar
        [HttpGet("sugestao")]
        public async Task<IActionResult> GetSugestao([FromQuery] string periodo = "Almoco")
        {
            var p = periodo.Equals("Jantar", StringComparison.OrdinalIgnoreCase) ? Periodo.Jantar : Periodo.Almoco;
            var sugestao = await _cardapioService.GetSugestaoHojeAsync(p);
            if (sugestao == null) return NoContent();

            return Ok(new
            {
                itemCardapioId     = sugestao.ItemCardapioId,
                itemNome           = sugestao.ItemCardapio?.Nome ?? "",
                descricao          = sugestao.ItemCardapio?.Descricao ?? "",
                precoBase          = sugestao.ItemCardapio?.PrecoBase ?? 0,
                percentualDesconto = sugestao.PercentualDesconto,
                precoFinal         = sugestao.ItemCardapio != null
                    ? sugestao.AplicarDesconto(sugestao.ItemCardapio.PrecoBase)
                    : 0
            });
        }

        // POST /api/cardapio/sugestao  (admin)
        [HttpPost("sugestao")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DefinirSugestao([FromBody] SugestaoRequest req)
        {
            var p = req.Periodo.Equals("Jantar", StringComparison.OrdinalIgnoreCase) ? Periodo.Jantar : Periodo.Almoco;
            var ok = await _cardapioService.DefinirSugestaoChefe(req.ItemId, p, req.PercentualDesconto);
            if (!ok) return BadRequest(new { message = "Item inválido ou período incorreto." });
            return Ok(new { message = "Sugestão do chefe definida com sucesso!" });
        }

        private async Task<object> BuildResponse(Periodo p)
        {
            var itens = await _cardapioService.GetItensPorPeriodoAsync(p);
            var sugestao = await _cardapioService.GetSugestaoHojeAsync(p);
            return new
            {
                itens = itens.Select(i => new
                {
                    id        = i.Id,
                    nome      = i.Nome,
                    descricao = i.Descricao,
                    precoBase = i.PrecoBase,
                    periodo   = i.Periodo.ToString()
                }),
                sugestao = sugestao == null ? null : new
                {
                    itemCardapioId     = sugestao.ItemCardapioId,
                    percentualDesconto = sugestao.PercentualDesconto,
                    precoFinal         = sugestao.ItemCardapio != null
                        ? sugestao.AplicarDesconto(sugestao.ItemCardapio.PrecoBase)
                        : 0
                }
            };
        }

        public class SugestaoRequest
        {
            public int ItemId { get; set; }
            public string Periodo { get; set; } = "Almoco";
            public decimal PercentualDesconto { get; set; } = 20m;
        }
    }
}
