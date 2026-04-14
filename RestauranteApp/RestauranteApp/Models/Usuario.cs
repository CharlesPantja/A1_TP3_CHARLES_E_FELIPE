using Microsoft.AspNetCore.Identity;

namespace RestauranteApp.Models
{
    public class Usuario : IdentityUser
    {
        public string NomeCompleto { get; set; } = string.Empty;
        public ICollection<Endereco> Enderecos { get; set; } = new List<Endereco>();
        public ICollection<Pedido> Pedidos { get; set; } = new List<Pedido>();
        public ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();
    }
}
