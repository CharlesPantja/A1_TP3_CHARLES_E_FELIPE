namespace RestauranteApp.Models
{
    public class Mesa
    {
        public int Id { get; set; }
        public int Numero { get; set; }
        public int Capacidade { get; set; }

        public ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();
    }

    public class Reserva
    {
        public int Id { get; set; }
        public DateTime DataReserva { get; set; }
        public TimeSpan HorarioInicio { get; set; }
        public int NumeroPessoas { get; set; }
        public string CodigoConfirmacao { get; set; } = GerarCodigo();
        public bool Confirmada { get; set; } = false;

        public string UsuarioId { get; set; } = string.Empty;
        public Usuario? Usuario { get; set; }

        public int MesaId { get; set; }
        public Mesa? Mesa { get; set; }

        private static string GerarCodigo()
        {
            return Guid.NewGuid().ToString("N")[..8].ToUpper();
        }

        // Reservas só são válidas para jantar: 19h às 22h
        public static readonly TimeSpan HorarioAbertura = new TimeSpan(19, 0, 0);
        public static readonly TimeSpan HorarioFechamento = new TimeSpan(22, 0, 0);

        public bool HorarioValido()
        {
            return HorarioInicio >= HorarioAbertura && HorarioInicio <= HorarioFechamento;
        }
    }
}
