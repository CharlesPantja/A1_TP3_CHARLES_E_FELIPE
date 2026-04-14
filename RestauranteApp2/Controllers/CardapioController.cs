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

        [HttpGet("almoco")]
        public async Task<IActionResult> GetAlmoco()
        {
            var itens = await _cardapioService.GetItensPorPeriodoAsync(Periodo.Almoco);
            var sugestao = await _cardapioService.GetSugestaoHojeAsync(Periodo.Almoco);
            return Ok(new { itens, sugestaoId = sugestao?.ItemCardapioId });
        }

        [HttpGet("jantar")]
        public async Task<IActionResult> GetJantar()
        {
            var itens = await _cardapioService.GetItensPorPeriodoAsync(Periodo.Jantar);
            var sugestao = await _cardapioService.GetSugestaoHojeAsync(Periodo.Jantar);
            return Ok(new { itens, sugestaoId = sugestao?.ItemCardapioId });
        }

        [HttpPost("sugestao")]
        public async Task<IActionResult> DefinirSugestao([FromBody] SugestaoRequest req)
        {
            var ok = await _cardapioService.DefinirSugestaoChefe(req.ItemId, req.Periodo);
            if (!ok) return BadRequest("Item inválido ou período incorreto.");
            return Ok("Sugestão do chefe definida com sucesso!");
        }

        public class SugestaoRequest
        {
            public int ItemId { get; set; }
            public Periodo Periodo { get; set; }
        }
    }
}
