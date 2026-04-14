namespace RestauranteApp.Models
{
    public class SugestaoChefe
    {
        public int Id { get; set; }
        public DateTime Data { get; set; }
        public Periodo Periodo { get; set; }

        public int ItemCardapioId { get; set; }
        public ItemCardapio? ItemCardapio { get; set; }

        public decimal PercentualDesconto { get; set; } = 20m;

        public decimal AplicarDesconto(decimal preco)
        {
            return preco * (1 - PercentualDesconto / 100);
        }
    }
}
