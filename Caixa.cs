// Models/Caixa.cs
using System.Text.Json.Serialization;
namespace ManoelLoja.Models 
{
    public class Caixa
    {
        public int Id { get; set; } 

        [JsonPropertyName("caixa_id")] 
        public string Nome { get; set; } = string.Empty;

        public decimal Altura { get; set; }
        public decimal Largura { get; set; }
        public decimal Comprimento { get; set; }

        public decimal Volume => Altura * Largura * Comprimento;

        public Caixa() { }
    }
}