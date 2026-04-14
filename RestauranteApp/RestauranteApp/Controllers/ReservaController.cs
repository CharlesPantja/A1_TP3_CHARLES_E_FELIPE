using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestauranteApp.Services;
using System.Security.Claims;

namespace RestauranteApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ReservaController : ControllerBase
    {
        private readonly ReservaService _reservaService;

        public ReservaController(ReservaService reservaService)
        {
            _reservaService = reservaService;
        }

        [HttpGet("mesas-disponiveis")]
        public async Task<IActionResult> MesasDisponiveis([FromQuery] DateTime data, [FromQuery] string horario)
        {
            if (!TimeSpan.TryParse(horario, out var ts))
                return BadRequest("Horário inválido. Use formato HH:mm");

            var mesas = await _reservaService.GetMesasDisponiveisAsync(data, ts);
            return Ok(mesas);
        }

        [HttpGet("minhas-reservas")]
        public async Task<IActionResult> MinhasReservas()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var reservas = await _reservaService.GetReservasUsuarioAsync(userId);
            return Ok(reservas);
        }

        [HttpPost]
        public async Task<IActionResult> CriarReserva([FromBody] CriarReservaRequest req)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            if (!TimeSpan.TryParse(req.Horario, out var horario))
                return BadRequest("Horário inválido. Use formato HH:mm");

            var (sucesso, mensagem, reserva) = await _reservaService.CriarReservaAsync(
                userId, req.MesaId, req.DataReserva, horario, req.NumeroPessoas);

            if (!sucesso) return BadRequest(mensagem);
            return Ok(new { mensagem, codigoConfirmacao = reserva!.CodigoConfirmacao });
        }

        public class CriarReservaRequest
        {
            public int MesaId { get; set; }
            public DateTime DataReserva { get; set; }
            public string Horario { get; set; } = string.Empty;
            public int NumeroPessoas { get; set; }
        }
    }
}
