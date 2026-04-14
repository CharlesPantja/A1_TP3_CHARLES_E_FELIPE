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

        public async Task<(bool sucesso, string mensagem, Reserva? reserva)> CriarReservaAsync(
            string usuarioId, int mesaId, DateTime dataReserva, TimeSpan horario, int numeroPessoas)
        {
            var reservaTemp = new Reserva { HorarioInicio = horario };
            if (!reservaTemp.HorarioValido())
                return (false, "Reservas só podem ser feitas entre 19h e 22h.", null);

            // Regra: reserva deve ser feita com pelo menos 1 dia de antecedência
            if (dataReserva.Date <= DateTime.Today)
                return (false, "Reservas devem ser feitas com pelo menos 1 dia de antecedência.", null);

            // Verifica se a mesa está disponível no dia e horário
            var conflito = await _context.Reservas
                .AnyAsync(r => r.MesaId == mesaId
                            && r.DataReserva.Date == dataReserva.Date
                            && r.HorarioInicio == horario);

            if (conflito)
                return (false, "Mesa já reservada para esse dia e horário.", null);

            var mesa = await _context.Mesas.FindAsync(mesaId);
            if (mesa == null)
                return (false, "Mesa não encontrada.", null);

            if (numeroPessoas > mesa.Capacidade)
                return (false, $"A mesa {mesa.Numero} comporta no máximo {mesa.Capacidade} pessoas.", null);

            var reserva = new Reserva
            {
                UsuarioId = usuarioId,
                MesaId = mesaId,
                DataReserva = dataReserva,
                HorarioInicio = horario,
                NumeroPessoas = numeroPessoas,
                Confirmada = true
            };

            _context.Reservas.Add(reserva);
            await _context.SaveChangesAsync();

            return (true, $"Reserva confirmada! Código: {reserva.CodigoConfirmacao}", reserva);
        }

        public async Task<List<Mesa>> GetMesasDisponiveisAsync(DateTime data, TimeSpan horario)
        {
            var mesasOcupadas = await _context.Reservas
                .Where(r => r.DataReserva.Date == data.Date && r.HorarioInicio == horario)
                .Select(r => r.MesaId)
                .ToListAsync();

            return await _context.Mesas
                .Where(m => !mesasOcupadas.Contains(m.Id))
                .ToListAsync();
        }

        public async Task<List<Reserva>> GetReservasUsuarioAsync(string usuarioId)
        {
            return await _context.Reservas
                .Where(r => r.UsuarioId == usuarioId)
                .Include(r => r.Mesa)
                .OrderByDescending(r => r.DataReserva)
                .ToListAsync();
        }
    }
}
