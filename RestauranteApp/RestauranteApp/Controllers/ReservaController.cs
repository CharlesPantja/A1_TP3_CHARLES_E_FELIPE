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

        // GET /api/reserva/mesas-disponiveis?data=2025-01-01&horario=19:00
        [HttpGet("mesas-disponiveis")]
        public async Task<IActionResult> MesasDisponiveis([FromQuery] DateTime data, [FromQuery] string horario)
        {
            var mesas = await _reservaService.GetMesasDisponiveisAsync(data, horario);
            return Ok(mesas.Select(m => new { m.Id, m.Numero, m.Capacidade }));
        }

        // POST /api/reserva
        [HttpPost]
        public async Task<IActionResult> CriarReserva([FromBody] ReservaRequest req)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var (sucesso, mensagem, reserva) = await _reservaService.CriarReservaAsync(
                userId, req.MesaId, req.DataReserva, req.Horario, req.NumeroPessoas);

            if (!sucesso) return BadRequest(mensagem);
            return Ok(new { mensagem, reserva!.CodigoConfirmacao });
        }

        // GET /api/reserva/minhas-reservas
        [HttpGet("minhas-reservas")]
        public async Task<IActionResult> MinhasReservas()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var reservas = await _reservaService.GetReservasUsuarioAsync(userId);
            return Ok(reservas.Select(r => new
            {
                r.Id,
                r.DataReserva,
                r.HorarioInicio,
                r.NumeroPessoas,
                r.CodigoConfirmacao,
                r.Status,
                statusDesc = r.Status switch
                {
                    Models.StatusReserva.Confirmada => "✅ Confirmada",
                    Models.StatusReserva.Cancelada  => "❌ Cancelada",
                    Models.StatusReserva.Concluida  => "✔️ Concluída",
                    _ => "Desconhecido"
                },
                Mesa = r.Mesa == null ? null : new { r.Mesa.Id, r.Mesa.Numero, r.Mesa.Capacidade }
            }));
        }

        // DELETE /api/reserva/{id} — cancela reserva do usuário logado
        [HttpDelete("{id}")]
        public async Task<IActionResult> CancelarReserva(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var (sucesso, mensagem) = await _reservaService.CancelarReservaAsync(id, userId);
            if (!sucesso) return BadRequest(mensagem);
            return Ok(mensagem);
        }

        public class ReservaRequest
        {
            public int MesaId { get; set; }
            public DateTime DataReserva { get; set; }
            public string Horario { get; set; } = string.Empty;
            public int NumeroPessoas { get; set; } = 2;
        }
    }
}
