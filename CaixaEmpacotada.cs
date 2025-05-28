// Models/CaixaEmpacotada.cs
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ManoelLoja.Models
{
    // Representa uma caixa específica que foi usada para empacotar produtos de um pedido.
    public class CaixaEmpacotada
    {
        [JsonPropertyName("caixa")]
        public Caixa Caixa { get; set; } // Referência ao objeto Caixa completo do seu DB

        [JsonPropertyName("produtos_alocados")]
        public List<Produto> ProdutosAlocados { get; set; } // A lista de produtos que foram colocados NESSA caixa

        public CaixaEmpacotada()
        {
            Caixa = new Caixa(); // Inicializa para evitar null
            ProdutosAlocados = new List<Produto>(); // Inicializa a lista
        }
    }
}