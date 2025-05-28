// Models/Produto.cs
using System.Text.Json.Serialization;

namespace ManoelLoja.Models
{
    public class Produto
    {
        public int Id { get; set; } // <<<<<<<< ADICIONADO: Chave primária para o banco de dados

        [JsonPropertyName("produto_id")]
        public string ProdutoId { get; set; } = string.Empty; // Usado para identificação externa/negócio

        [JsonPropertyName("dimensoes")]
        public Dimensoes Dimensoes { get; set; }

        public decimal Volume => Dimensoes.Altura * Dimensoes.Largura * Dimensoes.Comprimento;

        public Produto()
        {
            Dimensoes = new Dimensoes();
            // ProdutoId já está com = string.Empty;
        }
    }

    public class Dimensoes
    {
        [JsonPropertyName("altura")]
        public decimal Altura { get; set; }

        [JsonPropertyName("largura")]
        public decimal Largura { get; set; }

        [JsonPropertyName("comprimento")]
        public decimal Comprimento { get; set; }

        public Dimensoes()
        {
        }
    }
}