// Models/Pedido.cs
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ManoelLoja.Models 
{
    public class Pedido
    {
        [JsonPropertyName("pedido_id")]
        public int PedidoId { get; set; }

        [JsonPropertyName("produtos")]
        public List<Produto> Produtos { get; set; }

        public Pedido()
        {
            Produtos = new List<Produto>();
        }
    }
}