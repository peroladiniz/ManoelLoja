using Microsoft.EntityFrameworkCore;
using ManoelLoja.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration; // Necessário para GetConnectionString

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });

// Registrar o DbContext
builder.Services.AddDbContext<LojaManoelDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// --- NOVO CÓDIGO AQUI: APLICA AS MIGRAÇÕES DO BANCO DE DADOS ---
// Isso garante que o banco de dados e as tabelas sejam criados/atualizados
// quando a aplicação inicia, especialmente útil em ambientes de desenvolvimento
// com Docker onde o DB pode ser novo ou recriado.
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<LojaManoelDbContext>();
    // Tenta aplicar as migrações. Se o DB não existir, ele será criado.
    // Se já existir, as migrações pendentes serão aplicadas.
    try
    {
        dbContext.Database.Migrate();
        Console.WriteLine("Migrações do banco de dados aplicadas com sucesso.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Erro ao aplicar migrações do banco de dados: {ex.Message}");
        // Você pode querer logar mais detalhes do erro aqui
    }
}
// --- FIM DO NOVO CÓDIGO ---


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection(); // Redireciona HTTP para HTTPS

app.UseAuthorization();

app.MapControllers(); // Mapeia os controllers para as rotas

app.Run(); // Inicia a aplicação