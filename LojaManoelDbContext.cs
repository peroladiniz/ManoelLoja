// Data/LojaManoelDbContext.cs
using ManoelLoja.Models; // MUITO IMPORTANTE: Este using deve apontar para o namespace das suas classes de modelo no projeto ManoelLoja
using Microsoft.EntityFrameworkCore; // Este using está correto


namespace ManoelLoja.Data // MUITO IMPORTANTE: O namespace deve ser o nome do seu NOVO projeto + .Data
{
    public class LojaManoelDbContext : DbContext
    {
        // Construtor que aceita DbContextOptions, necessário para injeção de dependência
        public LojaManoelDbContext(DbContextOptions<LojaManoelDbContext> options) : base(options)
        {
        }

        // DbSet para a entidade Caixa. Isso criará uma tabela 'Caixas' no banco.
        public DbSet<Caixa> Caixas { get; set; }
        public DbSet<Produto> Produtos { get; set; } // <<<<<<<< ADICIONE ESTA LINHA AQUI

        // Método OnModelCreating para configurar as propriedades do modelo, se necessário
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configuração inicial das caixas disponíveis como "seed data"
            // Isso irá inserir essas caixas no banco de dados na primeira Migration
            modelBuilder.Entity<Caixa>().HasData(
                new Caixa { Id = 1, Nome = "Caixa 1", Altura = 30, Largura = 40, Comprimento = 80 },
                new Caixa { Id = 2, Nome = "Caixa 2", Altura = 80, Largura = 50, Comprimento = 40 },
                new Caixa { Id = 3, Nome = "Caixa 3", Altura = 50, Largura = 80, Comprimento = 60 }
            );

            // Importante: Para que Dimensoes seja salva como parte do Produto,
            // e não em uma tabela separada (Owned Entity Type), é bom configurá-lo.
            // Isso informa ao EF Core que Dimensoes é uma parte do Produto.
            modelBuilder.Entity<Produto>().OwnsOne(p => p.Dimensoes);

            base.OnModelCreating(modelBuilder);
        }
    }
}