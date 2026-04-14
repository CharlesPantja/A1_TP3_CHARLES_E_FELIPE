namespace RestauranteApp.Models
{
    public enum Periodo
    {
        Almoco = 1,
        Jantar = 2
    }

    public class Ingrediente
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public ICollection<ItemCardapioIngrediente> ItemCardapioIngredientes { get; set; } = new List<ItemCardapioIngrediente>();
    }

    public class ItemCardapio
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public decimal PrecoBase { get; set; }
        public Periodo Periodo { get; set; }

        public ICollection<ItemCardapioIngrediente> ItemCardapioIngredientes { get; set; } = new List<ItemCardapioIngrediente>();
        public ICollection<PedidoItem> PedidoItens { get; set; } = new List<PedidoItem>();
        public ICollection<SugestaoChefe> SugestoesChefe { get; set; } = new List<SugestaoChefe>();
    }

    // Tabela N-N entre ItemCardapio e Ingrediente
    public class ItemCardapioIngrediente
    {
        public int ItemCardapioId { get; set; }
        public ItemCardapio? ItemCardapio { get; set; }

        public int IngredienteId { get; set; }
        public Ingrediente? Ingrediente { get; set; }
    }
}
