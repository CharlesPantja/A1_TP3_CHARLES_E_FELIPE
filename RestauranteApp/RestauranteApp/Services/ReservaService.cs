using Microsoft.EntityFrameworkCore;
using RestauranteApp.Data;
using RestauranteApp.Models;

namespace RestauranteApp.Services
{
    public class ReservaService
    {
        private readonly ApplicationDbContext _context;

        public ReservaService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Mesa>> GetMesasDisponiveisAsync(DateTime dataReserva, string horario)
        {
            var mesasOcupadas = await _context.Reservas
                .Where(r => r.DataReserva.Date == dataReserva.Date
                         && r.HorarioInicio == horario
                         && r.Status == StatusReserva.Confirmada)
                .Select(r => r.MesaId)
                .ToListAsync();

            return await _context.Mesas
                .Where(m => m.Ativa && !mesasOcupadas.Contains(m.Id))
                .OrderBy(m => m.Numero)
                .ToListAsync();
        }

        public async Task<(bool sucesso, string mensagem, Reserva? reserva)> CriarReservaAsync(
            string usuarioId, int mesaId, DateTime dataReserva, string horario, int numeroPessoas)
        {
            // Reservas devem ser feitas com pelo menos 1 dia de antecedência
            if (dataReserva.Date <= DateTime.Today)
                return (false, "Reservas devem ser feitas com pelo menos 1 dia de antecedência.", null);

            // Verifica se mesa existe
            var mesa = await _context.Mesas.FindAsync(mesaId);
            if (mesa == null || !mesa.Ativa)
                return (false, "Mesa não encontrada ou inativa.", null);

            // Verifica se mesa já está reservada nesse horário
            var conflito = await _context.Reservas
                .AnyAsync(r => r.MesaId == mesaId
                            && r.DataReserva.Date == dataReserva.Date
                            && r.HorarioInicio == horario
                            && r.Status == StatusReserva.Confirmada);
            if (conflito)
                return (false, "Esta mesa já está reservada neste horário.", null);

            // Verifica capacidade
            if (numeroPessoas > mesa.Capacidade)
                return (false, $"Mesa {mesa.Numero} comporta no máximo {mesa.Capacidade} pessoas.", null);

            var reserva = new Reserva
            {
                UsuarioId          = usuarioId,
                MesaId             = mesaId,
                DataReserva        = dataReserva,
                HorarioInicio      = horario,
                NumeroPessoas      = numeroPessoas,
                CodigoConfirmacao  = Guid.NewGuid().ToString("N")[..8].ToUpper(),
                Status             = StatusReserva.Confirmada
            };

            _context.Reservas.Add(reserva);
            await _context.SaveChangesAsync();

            return (true, $"Reserva confirmada! Código: {reserva.CodigoConfirmacao}", reserva);
        }

        public async Task<List<Reserva>> GetReservasUsuarioAsync(string usuarioId)
        {
            return await _context.Reservas
                .Include(r => r.Mesa)
                .Where(r => r.UsuarioId == usuarioId)
                .OrderByDescending(r => r.DataReserva)
                .ToListAsync();
        }

        public async Task<(bool sucesso, string mensagem)> CancelarReservaAsync(int reservaId, string usuarioId)
        {
            var reserva = await _context.Reservas
                .FirstOrDefaultAsync(r => r.Id == reservaId && r.UsuarioId == usuarioId);

            if (reserva == null)
                return (false, "Reserva não encontrada.");

            if (reserva.Status == StatusReserva.Cancelada)
                return (false, "Reserva já foi cancelada.");

            if (reserva.DataReserva.Date <= DateTime.Today)
                return (false, "Não é possível cancelar reservas para hoje ou datas passadas.");

            reserva.Status = StatusReserva.Cancelada;
            await _context.SaveChangesAsync();

            return (true, "Reserva cancelada com sucesso.");
        }
    }
}
