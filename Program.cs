using Microsoft.EntityFrameworkCore;
using ManoelLoja.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration; // Necess�rio para GetConnectionString

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

// --- NOVO C�DIGO AQUI: APLICA AS MIGRA��ES DO BANCO DE DADOS ---
// Isso garante que o banco de dados e as tabelas sejam criados/atualizados
// quando a aplica��o inicia, especialmente �til em ambientes de desenvolvimento
// com Docker onde o DB pode ser novo ou recriado.
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<LojaManoelDbContext>();
    // Tenta aplicar as migra��es. Se o DB n�o existir, ele ser� criado.
    // Se j� existir, as migra��es pendentes ser�o aplicadas.
    try
    {
        dbContext.Database.Migrate();
        Console.WriteLine("Migra��es do banco de dados aplicadas com sucesso.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Erro ao aplicar migra��es do banco de dados: {ex.Message}");
        // Voc� pode querer logar mais detalhes do erro aqui
    }
}
// --- FIM DO NOVO C�DIGO ---


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection(); // Redireciona HTTP para HTTPS

app.UseAuthorization();

app.MapControllers(); // Mapeia os controllers para as rotas

app.Run(); // Inicia a aplica��o