using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ManoelLoja.Migrations
{
    /// <inheritdoc />
    public partial class AddProdutoEntityAndOwnedDimensoes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Produtos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProdutoId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Dimensoes_Altura = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Dimensoes_Largura = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Dimensoes_Comprimento = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Produtos", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Produtos");
        }
    }
}
