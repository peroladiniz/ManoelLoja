// Models/RequisicaoEmpacotamento.cs
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ManoelLoja.Models
{
    public class RequisicaoEmpacotamento
    {
        [JsonPropertyName("pedidos")]
        public List<Pedido> Pedidos { get; set; }

        public RequisicaoEmpacotamento()
        {
            Pedidos = new List<Pedido>();
        }
    }
}