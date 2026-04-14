namespace RestauranteApp.Models
{
    /// <summary>
    /// Status do ciclo de vida de um pedido
    /// </summary>
    public enum StatusPedido
    {
        Pendente = 0,
        EmPreparo = 1,
        SaiuParaEntrega = 2,
        ProntoParaRetirada = 3,
        Finalizado = 4,
        Cancelado = 5
    }

    public class Pedido
    {
        public int Id { get; set; }
        public DateTime DataHora { get; set; } = DateTime.Now;
        public Periodo PeriodoPedido { get; set; }
        public StatusPedido Status { get; set; } = StatusPedido.Pendente;

        // Nullable: pedidos presenciais sem login não têm usuário
        public string? UsuarioId { get; set; }
        public Usuario? Usuario { get; set; }

        public int AtendimentoId { get; set; }
        public Atendimento? Atendimento { get; set; }

        public ICollection<PedidoItem> PedidoItens { get; set; } = new List<PedidoItem>();

        // Cálculos delegados à CalculadoraPedido (boas práticas POO)
        public decimal CalcularSubtotal() => CalculadoraPedido.CalcularSubtotal(PedidoItens);
        public decimal CalcularTotal()    => CalculadoraPedido.CalcularTotal(PedidoItens, Atendimento);
    }

    public class PedidoItem
    {
        public int Id { get; set; }
        public int Quantidade { get; set; } = 1;
        public decimal PrecoBase { get; set; }
        public decimal PrecoFinal { get; set; }
        public bool FoiSugestaoChefe { get; set; } = false;

        public int PedidoId { get; set; }
        public Pedido? Pedido { get; set; }

        public int ItemCardapioId { get; set; }
        public ItemCardapio? ItemCardapio { get; set; }
    }

    /// <summary>
    /// Classe responsável pelo cálculo de valores dos pedidos (Single Responsibility)
    /// </summary>
    public static class CalculadoraPedido
    {
        public static decimal CalcularSubtotal(IEnumerable<PedidoItem> itens)
            => itens.Sum(pi => pi.PrecoFinal * pi.Quantidade);

        public static decimal CalcularTotal(IEnumerable<PedidoItem> itens, Atendimento? atendimento)
        {
            var subtotal = CalcularSubtotal(itens);
            var taxa = atendimento?.CalcularTaxa(subtotal) ?? 0m;
            return subtotal + taxa;
        }

        public static string DescricaoStatus(StatusPedido status) => status switch
        {
            StatusPedido.Pendente          => "⏳ Aguardando confirmação",
            StatusPedido.EmPreparo         => "👨‍🍳 Em preparo",
            StatusPedido.SaiuParaEntrega   => "🛵 Saiu para entrega",
            StatusPedido.ProntoParaRetirada=> "✅ Pronto para retirada",
            StatusPedido.Finalizado        => "✔️ Finalizado",
            StatusPedido.Cancelado         => "❌ Cancelado",
            _                              => "Desconhecido"
        };
    }
}
