namespace RestauranteApp.Models
{
    public abstract class Atendimento
    {
        public int Id { get; set; }
        public DateTime DataHora { get; set; } = DateTime.Now;
        public string TipoAtendimento { get; set; } = string.Empty;

        public ICollection<Pedido> Pedidos { get; set; } = new List<Pedido>();

        public abstract decimal CalcularTaxa(decimal valorPedido);

        protected bool IsHorarioNoturno()
        {
            return DataHora.Hour >= 18;
        }
    }

    public class AtendimentoPresencial : Atendimento
    {
        public AtendimentoPresencial()
        {
            TipoAtendimento = "Presencial";
        }

        public int? NumeroMesa { get; set; }

        public override decimal CalcularTaxa(decimal valorPedido) => 0m;
    }

    public class AtendimentoDeliveryProprio : Atendimento
    {
        public AtendimentoDeliveryProprio()
        {
            TipoAtendimento = "DeliveryProprio";
        }

        public decimal TaxaFixa { get; set; } = 5m;

        public string? EnderecoEntrega { get; set; }

        public override decimal CalcularTaxa(decimal valorPedido) => TaxaFixa;
    }

    public class AtendimentoDeliveryAplicativo : Atendimento
    {
        public AtendimentoDeliveryAplicativo()
        {
            TipoAtendimento = "DeliveryAplicativo";
        }

        public string NomeAplicativo { get; set; } = string.Empty;
        public string? EnderecoEntrega { get; set; }

        // 4% dia, 6% noite
        public override decimal CalcularTaxa(decimal valorPedido)
        {
            decimal percentual = IsHorarioNoturno() ? 0.06m : 0.04m;
            return valorPedido * percentual;
        }
    }
}
