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

        // Horários válidos para reserva de jantar
        private static readonly string[] HorariosValidos = { "19:00", "19:30", "20:00", "20:30", "21:00", "21:30", "22:00" };

        public ReservaController(ReservaService reservaService)
        {
            _reservaService = reservaService;
        }

        // GET /api/reserva/horarios — retorna horários disponíveis para reserva
        [HttpGet("horarios")]
        [AllowAnonymous]
        public IActionResult GetHorarios()
        {
            return Ok(HorariosValidos);
        }

        // GET /api/reserva/mesas-disponiveis?data=2025-01-15&horario=19:00
        [HttpGet("mesas-disponiveis")]
        public async Task<IActionResult> MesasDisponiveis(
            [FromQuery] DateTime data,
            [FromQuery] string horario)
        {
            if (!HorariosValidos.Contains(horario))
                return BadRequest(new { message = $"Horário inválido. Escolha entre: {string.Join(", ", HorariosValidos)}" });

            var mesas = await _reservaService.GetMesasDisponiveisAsync(data, horario);
            return Ok(mesas.Select(m => new
            {
                id         = m.Id,
                numero     = m.Numero,
                capacidade = m.Capacidade
            }));
        }

        // POST /api/reserva — cria reserva (somente jantar, mínimo 1 dia de antecedência)
        [HttpPost]
        public async Task<IActionResult> CriarReserva([FromBody] ReservaRequest req)
        {
            if (!HorariosValidos.Contains(req.Horario))
                return BadRequest(new { message = $"Horário inválido. Escolha entre: {string.Join(", ", HorariosValidos)}" });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var (sucesso, mensagem, reserva) = await _reservaService.CriarReservaAsync(
                userId, req.MesaId, req.DataReserva, req.Horario, req.NumeroPessoas);

            if (!sucesso) return BadRequest(new { message = mensagem });

            return Ok(new
            {
                reservaId         = reserva!.Id,
                mensagem,
                codigoConfirmacao = reserva.CodigoConfirmacao,
                data              = reserva.DataReserva.ToString("dd/MM/yyyy"),
                horario           = reserva.HorarioInicio,
                status            = reserva.Status,
                statusDesc        = "✅ Confirmada"
            });
        }

        // GET /api/reserva/minhas-reservas
        [HttpGet("minhas-reservas")]
        public async Task<IActionResult> MinhasReservas()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var reservas = await _reservaService.GetReservasUsuarioAsync(userId);

            return Ok(reservas.Select(r => new
            {
                reservaId         = r.Id,
                data              = r.DataReserva.ToString("dd/MM/yyyy"),
                horario           = r.HorarioInicio,
                numeroPessoas     = r.NumeroPessoas,
                codigoConfirmacao = r.CodigoConfirmacao,
                status            = (int)r.Status,
                statusDesc        = r.Status switch
                {
                    Models.StatusReserva.Confirmada => "✅ Confirmada",
                    Models.StatusReserva.Cancelada  => "❌ Cancelada",
                    Models.StatusReserva.Concluida  => "✔️ Concluída",
                    _ => "Desconhecido"
                },
                numeroMesa        = r.Mesa?.Numero,
                capacidadeMesa    = r.Mesa?.Capacidade
            }));
        }

        // DELETE /api/reserva/{id} — cancela reserva do usuário logado
        [HttpDelete("{id}")]
        public async Task<IActionResult> CancelarReserva(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var (sucesso, mensagem) = await _reservaService.CancelarReservaAsync(id, userId);
            if (!sucesso) return BadRequest(new { message = mensagem });
            return Ok(new { message = mensagem });
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
