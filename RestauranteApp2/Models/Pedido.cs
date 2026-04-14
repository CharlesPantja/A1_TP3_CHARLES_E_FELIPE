namespace RestauranteApp.Models
{
    public class Pedido
    {
        public int Id { get; set; }
        public DateTime DataHora { get; set; } = DateTime.Now;
        public Periodo PeriodoPedido { get; set; }

        public string UsuarioId { get; set; } = string.Empty;
        public Usuario? Usuario { get; set; }

        public int AtendimentoId { get; set; }
        public Atendimento? Atendimento { get; set; }

        public ICollection<PedidoItem> PedidoItens { get; set; } = new List<PedidoItem>();

        public decimal CalcularSubtotal()
        {
            return PedidoItens.Sum(pi => pi.PrecoFinal * pi.Quantidade);
        }

        public decimal CalcularTotal()
        {
            decimal subtotal = CalcularSubtotal();
            decimal taxa = Atendimento?.CalcularTaxa(subtotal) ?? 0m;
            return subtotal + taxa;
        }
    }

    public class PedidoItem
    {
        public int Id { get; set; }
        public int Quantidade { get; set; } = 1;
        public decimal PrecoBase { get; set; }
        public decimal PrecoFinal { get; set; } // já com desconto se for sugestão
        public bool FoiSugestaoChefe { get; set; } = false;

        public int PedidoId { get; set; }
        public Pedido? Pedido { get; set; }

        public int ItemCardapioId { get; set; }
        public ItemCardapio? ItemCardapio { get; set; }
    }
}
