namespace RestauranteApp.Models
{
    public enum StatusReserva
    {
        Confirmada = 0,
        Cancelada  = 1,
        Concluida  = 2
    }

    public class Reserva
    {
        public int Id { get; set; }
        public DateTime DataReserva { get; set; }
        public string HorarioInicio { get; set; } = string.Empty;
        public int NumeroPessoas { get; set; }
        public string CodigoConfirmacao { get; set; } = string.Empty;
        public StatusReserva Status { get; set; } = StatusReserva.Confirmada;

        public string UsuarioId { get; set; } = string.Empty;
        public Usuario? Usuario { get; set; }

        public int MesaId { get; set; }
        public Mesa? Mesa { get; set; }
    }

    public class Mesa
    {
        public int Id { get; set; }
        public int Numero { get; set; }
        public int Capacidade { get; set; }
        public bool Ativa { get; set; } = true;
        public ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();
    }
}
