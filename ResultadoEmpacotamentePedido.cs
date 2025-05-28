// Models/ResultadoEmpacotamentoPedido.cs
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ManoelLoja.Models
{ 
    public class ResultadoEmpacotamentoPedido
    {
        [JsonPropertyName("pedido_id")]
        public int PedidoId { get; set; }

        [JsonPropertyName("caixas")] // Mapeia "caixas" na saída
        public List<CaixaEmpacotada> CaixasUtilizadas { get; set; }

        public ResultadoEmpacotamentoPedido()
        {
            CaixasUtilizadas = new List<CaixaEmpacotada>();
        }
    }
}