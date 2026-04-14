using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestauranteApp.Services;

namespace RestauranteApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]   // Apenas administradores acessam relatórios
    public class RelatorioController : ControllerBase
    {
        private readonly RelatorioService _relatorioService;

        public RelatorioController(RelatorioService relatorioService)
        {
            _relatorioService = relatorioService;
        }

        [HttpGet("faturamento")]
        public async Task<IActionResult> Faturamento(
            [FromQuery] DateTime dataInicio,
            [FromQuery] DateTime dataFim)
        {
            var resultado = await _relatorioService.GetFaturamentoPorTipoAsync(dataInicio, dataFim);
            return Ok(resultado);
        }

        [HttpGet("itens-mais-vendidos")]
        public async Task<IActionResult> ItensMaisVendidos(
            [FromQuery] DateTime dataInicio,
            [FromQuery] DateTime dataFim)
        {
            var resultado = await _relatorioService.GetItensMaisVendidosAsync(dataInicio, dataFim);
            return Ok(resultado);
        }

        [HttpGet("top-clientes")]
        public async Task<IActionResult> TopClientes(
            [FromQuery] DateTime dataInicio,
            [FromQuery] DateTime dataFim,
            [FromQuery] int top = 10)
        {
            var resultado = await _relatorioService.GetTopClientesAsync(dataInicio, dataFim, top);
            return Ok(resultado);
        }

        [HttpGet("horario-pico")]
        public async Task<IActionResult> HorarioPico(
            [FromQuery] DateTime dataInicio,
            [FromQuery] DateTime dataFim)
        {
            var resultado = await _relatorioService.GetHorarioPicoAsync(dataInicio, dataFim);
            return Ok(resultado);
        }
    }
}
