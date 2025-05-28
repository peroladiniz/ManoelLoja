// Models/RespostaEmpacotamento.cs
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ManoelLoja.Models 
{
    public class RespostaEmpacotamento
    {
        [JsonPropertyName("pedidos")]
        public List<ResultadoEmpacotamentoPedido> Resultados { get; set; }

        public RespostaEmpacotamento()
        {
            Resultados = new List<ResultadoEmpacotamentoPedido>();
        }
    }
}