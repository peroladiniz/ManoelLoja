// Controllers/EmpacotamentoController.cs
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using ManoelLoja.Models;
using Microsoft.EntityFrameworkCore;
using ManoelLoja.Data;
using System; // Adicionar para usar Func
using System.Threading.Tasks; // Adicionar para usar Task

namespace ManoelLoja.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class EmpacotamentoController : ControllerBase
    {
        private readonly LojaManoelDbContext _context;

        public EmpacotamentoController(LojaManoelDbContext context)
        {
            _context = context;
        }

        // --- CLASSE AUXILIAR INTERNA PARA A LÓGICA DE EMPACOTAMENTO ---
        // Esta classe gerencia o estado de uma caixa "em uso" durante o empacotamento do pedido.
        private class TempCaixaEmUso
        {
            public Caixa CaixaOriginal { get; set; } // Referência à caixa do banco de dados
            public List<Produto> ProdutosAlocados { get; set; } // Lista de produtos que já foram colocados nesta caixa
            public decimal VolumeOcupado { get; set; } // Volume total dos produtos já alocados nesta caixa

            public TempCaixaEmUso(Caixa caixa)
            {
                CaixaOriginal = caixa;
                ProdutosAlocados = new List<Produto>();
                VolumeOcupado = 0;
            }

            // Tenta alocar um novo produto nesta caixa.
            // Retorna true se o produto coube e foi alocado, false caso contrário.
            // A função 'canFitFunc' é o seu método CanProdutoFitInCaixa, passado como parâmetro.
            public bool TentarAlocarProduto(Produto produto, Func<Dimensoes, decimal, decimal, decimal, bool> canFitFunc)
            {
                // 1. Verifica se o produto cabe nas dimensões da caixa (considerando rotação)
                // Usamos a função de rotação que você já tem para isso.
                if (!canFitFunc(produto.Dimensoes, CaixaOriginal.Altura, CaixaOriginal.Largura, CaixaOriginal.Comprimento))
                {
                    return false; // O produto é maior que a caixa, mesmo com rotação
                }

                // 2. Verifica se o produto cabe no volume restante da caixa
                if (VolumeOcupado + produto.Volume <= CaixaOriginal.Volume)
                {
                    // Se coube, adiciona o produto e atualiza o volume ocupado
                    ProdutosAlocados.Add(produto);
                    VolumeOcupado += produto.Volume;
                    return true;
                }
                return false; // Não há volume suficiente na caixa para este produto
            }
        }
        // --- FIM DA CLASSE AUXILIAR ---


        // NOVO/AJUSTADO: Endpoint principal para empacotar múltiplos pedidos
        // Agora, ele vai receber uma lista de Pedidos dentro de uma RequisicaoEmpacotamento
        // E retornar uma RespostaEmpacotamento com os resultados.
        [HttpPost("empacotar")] // Mantemos a rota original para não mudar o front-end
        public async Task<ActionResult<RespostaEmpacotamento>> Empacotar([FromBody] RequisicaoEmpacotamento request)
        {
            // 1. Validação inicial da requisição
            if (request == null || request.Pedidos == null || !request.Pedidos.Any())
            {
                return BadRequest("A requisição deve conter pelo menos um pedido válido para empacotamento.");
            }

            // 2. Obter todas as caixas disponíveis do banco de dados
            // CORREÇÃO AQUI: Trazemos as caixas para a memória primeiro com ToListAsync()
            // e depois ordenamos pelo Volume.
            var todasAsCaixasDisponiveis = (await _context.Caixas.ToListAsync())
                                                  .OrderBy(c => c.Volume)
                                                  .ToList();

            if (!todasAsCaixasDisponiveis.Any())
            {
                return BadRequest("Nenhuma caixa disponível no sistema para empacotamento. Por favor, adicione caixas primeiro.");
            }

            // Objeto de resposta final que conterá os resultados de todos os pedidos
            RespostaEmpacotamento respostaFinal = new RespostaEmpacotamento();
            respostaFinal.Resultados = new List<ResultadoEmpacotamentoPedido>(); // Garante que a lista não é nula

            // 3. Processar cada pedido individualmente
            foreach (var pedido in request.Pedidos)
            {
                // Validação do ID do pedido
                if (pedido.PedidoId <= 0)
                {
                    // Se o ID do pedido for inválido, adicione um resultado com erro e pule para o próximo pedido
                    respostaFinal.Resultados.Add(new ResultadoEmpacotamentoPedido
                    {
                        PedidoId = pedido.PedidoId,
                        CaixasUtilizadas = new List<CaixaEmpacotada>(), // Nenhuma caixa usada
                                                                        // Observação sobre erro no pedido
                                                                        // Você precisará adicionar uma propriedade "Observacao" ou similar em ResultadoEmpacotamentoPedido
                                                                        // Ou lidar com isso no cliente
                    });
                    // Para este desafio, vamos retornar um BadRequest geral se um pedido é inválido para simplificar.
                    return BadRequest($"Pedido inválido: ID do pedido ({pedido.PedidoId}) é obrigatório e deve ser um número positivo.");
                }

                // Se o pedido não tem produtos, não há o que empacotar para ele
                if (pedido.Produtos == null || !pedido.Produtos.Any())
                {
                    respostaFinal.Resultados.Add(new ResultadoEmpacotamentoPedido
                    {
                        PedidoId = pedido.PedidoId,
                        CaixasUtilizadas = new List<CaixaEmpacotada>() // Pedido sem produtos não usa caixas
                    });
                    continue; // Pula para o próximo pedido na lista
                }

                // Ordem para o algoritmo First Fit Decreasing (FFD): produtos do maior volume para o menor.
                // Isso ajuda a tentar preencher as caixas de forma mais eficiente.
                var produtosOrdenadosPorVolume = pedido.Produtos
                                                             .OrderByDescending(p => p.Volume)
                                                             .ToList();

                // Lista temporária de caixas "em uso" para o pedido atual.
                // Usamos TempCaixaEmUso para controlar o estado da caixa (volume ocupado, produtos nela).
                var caixasAtualmenteEmUso = new List<TempCaixaEmUso>();

                // Itera sobre cada produto do pedido (do maior para o menor)
                foreach (var produto in produtosOrdenadosPorVolume)
                {
                    // Validação de cada produto
                    if (produto.Dimensoes == null ||
                        produto.Dimensoes.Altura <= 0 || produto.Dimensoes.Largura <= 0 || produto.Dimensoes.Comprimento <= 0)
                    {
                        return BadRequest($"Produto inválido no Pedido {pedido.PedidoId}. ProdutoId '{produto.ProdutoId}' com dimensões inválidas (Altura, Largura ou Comprimento <= 0).");
                    }

                    bool produtoAlocado = false;

                    // 4. Tenta alocar o produto em uma das caixas JÁ EM USO para este pedido
                    // Procura a primeira caixa existente que pode acomodar o produto.
                    foreach (var caixaEmUso in caixasAtualmenteEmUso)
                    {
                        // Chama o método TentarAlocarProduto da TempCaixaEmUso, passando o CanProdutoFitInCaixa
                        if (caixaEmUso.TentarAlocarProduto(produto, CanProdutoFitInCaixa))
                        {
                            produtoAlocado = true;
                            break; // Produto alocado, passa para o próximo produto do pedido
                        }
                    }

                    // 5. Se o produto não couber em nenhuma caixa já em uso, ele precisa de uma NOVA caixa
                    if (!produtoAlocado)
                    {
                        // Encontra a *menor* caixa disponível (da lista de todas as caixas) que pode acomodar este produto.
                        // Isso é o "First Fit" para a nova caixa.
                        Caixa? caixaBaseParaNovoProduto = null;
                        decimal menorVolumeEncontrado = decimal.MaxValue;

                        foreach (var caixaDisponivel in todasAsCaixasDisponiveis)
                        {
                            if (CanProdutoFitInCaixa(produto.Dimensoes, caixaDisponivel.Altura, caixaDisponivel.Largura, caixaDisponivel.Comprimento))
                            {
                                // Se esta caixa for menor que as já consideradas e ainda puder acomodar o produto
                                if (caixaDisponivel.Volume < menorVolumeEncontrado)
                                {
                                    menorVolumeEncontrado = caixaDisponivel.Volume;
                                    caixaBaseParaNovoProduto = caixaDisponivel;
                                }
                            }
                        }

                        // Se encontrou uma nova caixa adequada para o produto
                        if (caixaBaseParaNovoProduto != null)
                        {
                            var novaCaixaEmUso = new TempCaixaEmUso(caixaBaseParaNovoProduto);
                            novaCaixaEmUso.TentarAlocarProduto(produto, CanProdutoFitInCaixa); // Adiciona o produto a esta nova caixa
                            caixasAtualmenteEmUso.Add(novaCaixaEmUso); // Adiciona a nova caixa à lista de caixas em uso do pedido
                            produtoAlocado = true;
                        }
                        else
                        {
                            // Se o produto não cabe em NENHUMA caixa disponível (nem mesmo uma nova vazia),
                            // isso significa que não podemos empacotar este pedido.
                            return NotFound($"Produto '{produto.ProdutoId}' (ID: {produto.Id}) do Pedido {pedido.PedidoId} não cabe em nenhuma caixa disponível. Empacotamento falhou para este pedido.");
                        }
                    }
                } // Fim do loop de produtos do pedido

                // 6. Após todos os produtos de UM pedido serem alocados, prepare o resultado para este pedido.
                // Converta as caixas temporárias em uso (TempCaixaEmUso) para o formato final de saída (CaixaEmpacotada).
                var resultadoPedidoAtual = new ResultadoEmpacotamentoPedido
                {
                    PedidoId = pedido.PedidoId,
                    CaixasUtilizadas = caixasAtualmenteEmUso.Select(tempCaixa => new CaixaEmpacotada
                    {
                        Caixa = tempCaixa.CaixaOriginal,
                        ProdutosAlocados = tempCaixa.ProdutosAlocados
                    }).ToList()
                };
                respostaFinal.Resultados.Add(resultadoPedidoAtual);

            } // Fim do loop de TODOS os pedidos

            // 7. Retorna o resultado do empacotamento para todos os pedidos processados.
            return Ok(respostaFinal);
        }


        // --- SEUS OUTROS ENDPOINTS EXISTENTES (MANTIDOS SEM ALTERAÇÕES) ---

        // Endpoint GET para listar todas as caixas
        [HttpGet("listar-caixas")]
        public async Task<ActionResult<IEnumerable<Caixa>>> ListarCaixas()
        {
            var caixas = await _context.Caixas.ToListAsync();
            return Ok(caixas);
        }

        // Endpoint POST para adicionar uma nova caixa
        [HttpPost("adicionar-caixa")]
        public async Task<ActionResult<Caixa>> AdicionarCaixa([FromBody] Caixa novaCaixa)
        {
            // Validações básicas
            if (novaCaixa == null || novaCaixa.Altura <= 0 || novaCaixa.Largura <= 0 || novaCaixa.Comprimento <= 0)
            {
                return BadRequest("Dados da caixa inválidos. Altura, Largura e Comprimento devem ser maiores que zero.");
            }

            _context.Caixas.Add(novaCaixa);
            await _context.SaveChangesAsync();

            // Retorna 201 CreatedAtAction com a caixa criada
            return CreatedAtAction(nameof(ObterCaixaPorId), new { id = novaCaixa.Id }, novaCaixa); // Correção do nameof
        }

        // Endpoint GET para obter uma caixa por ID
        [HttpGet("{id}")] // A rota agora aceita um ID na URL (ex: /Empacotamento/1)
        public async Task<ActionResult<Caixa>> ObterCaixaPorId(int id)
        {
            // Busca a caixa pelo ID no banco de dados
            var caixa = await _context.Caixas.FindAsync(id);

            // Verifica se a caixa foi encontrada
            if (caixa == null)
            {
                // Se não encontrar, retorna 404 Not Found
                return NotFound($"Caixa com ID {id} não encontrada.");
            }

            // Se encontrar, retorna a caixa com um status 200 OK
            return Ok(caixa);
        }

        // Endpoint PUT para atualizar uma caixa existente
        [HttpPut("{id}")] // A rota aceita um ID na URL (ex: /Empacotamento/1)
        public async Task<IActionResult> AtualizarCaixa(int id, [FromBody] Caixa caixaAtualizada)
        {
            // 1. Verifica se o ID da URL corresponde ao ID no corpo da requisição
            if (id != caixaAtualizada.Id)
            {
                return BadRequest("O ID da rota não corresponde ao ID da caixa no corpo da requisição.");
            }

            // 2. Validações básicas (altura, largura, comprimento devem ser positivos)
            if (caixaAtualizada.Altura <= 0 || caixaAtualizada.Largura <= 0 || caixaAtualizada.Comprimento <= 0)
            {
                return BadRequest("Dados da caixa inválidos. Altura, Largura e Comprimento devem ser maiores que zero.");
            }

            // 3. Tenta encontrar a caixa existente no banco de dados
            var caixaExistente = await _context.Caixas.FindAsync(id);

            if (caixaExistente == null)
            {
                // Se não encontrar, retorna 404 Not Found
                return NotFound($"Caixa com ID {id} não encontrada para atualização.");
            }

            // 4. Atualiza as propriedades da caixa existente com os novos valores
            caixaExistente.Nome = caixaAtualizada.Nome;
            caixaExistente.Altura = caixaAtualizada.Altura;
            caixaExistente.Largura = caixaAtualizada.Largura;
            caixaExistente.Comprimento = caixaAtualizada.Comprimento;

            try
            {
                // 5. Marca o objeto como modificado e salva as mudanças no banco de dados
                _context.Entry(caixaExistente).State = EntityState.Modified;
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Caixas.Any(e => e.Id == id))
                {
                    return NotFound($"Erro de concorrência: Caixa com ID {id} não encontrada ou removida.");
                }
                else
                {
                    throw;
                }
            }
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> ExcluirCaixa(int id)
        {
            var caixaParaExcluir = await _context.Caixas.FindAsync(id);

            if (caixaParaExcluir == null)
            {
                return NotFound($"Caixa com ID {id} não encontrada para exclusão.");
            }

            _context.Caixas.Remove(caixaParaExcluir);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // Endpoint POST para adicionar um novo produto
        [HttpPost("adicionar-produto")]
        public async Task<ActionResult<Produto>> AdicionarProduto([FromBody] Produto novoProduto)
        {
            if (novoProduto == null || novoProduto.Dimensoes == null ||
                novoProduto.Dimensoes.Altura <= 0 || novoProduto.Dimensoes.Largura <= 0 || novoProduto.Dimensoes.Comprimento <= 0 ||
                string.IsNullOrWhiteSpace(novoProduto.ProdutoId))
            {
                return BadRequest("Dados do produto inválidos. ProdutoId e todas as dimensões devem ser maiores que zero.");
            }

            _context.Produtos.Add(novoProduto);
            await _context.SaveChangesAsync();

            // Ajustado para apontar para o ObterProdutoPorId
            return CreatedAtAction(nameof(ObterProdutoPorId), new { id = novoProduto.Id }, novoProduto);
        }

        // Endpoint GET para listar todos os produtos
        [HttpGet("listar-produtos")]
        public async Task<ActionResult<IEnumerable<Produto>>> ListarProdutos()
        {
            var produtos = await _context.Produtos.ToListAsync();
            return Ok(produtos);
        }

        // Endpoint GET para obter um produto por ID
        [HttpGet("produtos/{id}")]
        public async Task<ActionResult<Produto>> ObterProdutoPorId(int id)
        {
            var produto = await _context.Produtos.FindAsync(id);

            if (produto == null)
            {
                return NotFound($"Produto com ID {id} não encontrado.");
            }
            return Ok(produto);
        }

        // Endpoint PUT para atualizar um produto existente
        [HttpPut("produtos/{id}")]
        public async Task<IActionResult> AtualizarProduto(int id, [FromBody] Produto produtoAtualizado)
        {
            if (id != produtoAtualizado.Id)
            {
                return BadRequest("O ID da rota não corresponde ao ID do produto no corpo da requisição.");
            }

            if (produtoAtualizado.Dimensoes == null ||
                produtoAtualizado.Dimensoes.Altura <= 0 || produtoAtualizado.Dimensoes.Largura <= 0 || produtoAtualizado.Dimensoes.Comprimento <= 0 ||
                string.IsNullOrWhiteSpace(produtoAtualizado.ProdutoId))
            {
                return BadRequest("Dados do produto inválidos. ProdutoId e todas as dimensões devem ser maiores que zero.");
            }

            var produtoExistente = await _context.Produtos.FindAsync(id);

            if (produtoExistente == null)
            {
                return NotFound($"Produto com ID {id} não encontrado para atualização.");
            }

            produtoExistente.ProdutoId = produtoAtualizado.ProdutoId;
            produtoExistente.Dimensoes.Altura = produtoAtualizado.Dimensoes.Altura;
            produtoExistente.Dimensoes.Largura = produtoAtualizado.Dimensoes.Largura;
            produtoExistente.Dimensoes.Comprimento = produtoAtualizado.Dimensoes.Comprimento;

            try
            {
                _context.Entry(produtoExistente).State = EntityState.Modified;
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Produtos.Any(e => e.Id == id))
                {
                    return NotFound($"Erro de concorrência: Produto com ID {id} não encontrado ou removido.");
                }
                else
                {
                    throw;
                }
            }
            return NoContent();
        }

        // Endpoint DELETE para excluir um produto
        [HttpDelete("produtos/{id}")]
        public async Task<IActionResult> ExcluirProduto(int id)
        {
            var produtoParaExcluir = await _context.Produtos.FindAsync(id);

            if (produtoParaExcluir == null)
            {
                return NotFound($"Produto com ID {id} não encontrado para exclusão.");
            }

            _context.Produtos.Remove(produtoParaExcluir);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // Endpoint POST para empacotar UM produto em uma caixa (mantido para testes individuais)
        [HttpPost("empacotar-produto")]
        public async Task<ActionResult<Caixa>> EmpacotarProduto([FromBody] Produto produtoParaEmpacotar)
        {
            if (produtoParaEmpacotar == null || produtoParaEmpacotar.Dimensoes == null ||
                produtoParaEmpacotar.Dimensoes.Altura <= 0 || produtoParaEmpacotar.Dimensoes.Largura <= 0 || produtoParaEmpacotar.Dimensoes.Comprimento <= 0)
            {
                return BadRequest("Dados do produto para empacotar inválidos. Todas as dimensões devem ser maiores que zero.");
            }

            var caixasDisponiveis = await _context.Caixas.ToListAsync(); // Traz todas as caixas para a memória

            Caixa? caixaIdeal = null;
            decimal menorVolumeAdequado = decimal.MaxValue;

            foreach (var caixa in caixasDisponiveis)
            {
                bool podeAcomodar = CanProdutoFitInCaixa(produtoParaEmpacotar.Dimensoes, caixa.Altura, caixa.Largura, caixa.Comprimento);

                if (podeAcomodar)
                {
                    // Agora você pode usar .Volume porque as caixas estão em memória.
                    if (caixa.Volume < menorVolumeAdequado)
                    {
                        menorVolumeAdequado = caixa.Volume;
                        caixaIdeal = caixa;
                    }
                }
            }

            if (caixaIdeal != null)
            {
                return Ok(caixaIdeal);
            }
            else
            {
                return NotFound("Nenhuma caixa adequada encontrada para o produto especificado.");
            }
        }

        // Método auxiliar para verificar se o produto cabe na caixa em qualquer orientação (com rotação)
        private bool CanProdutoFitInCaixa(Dimensoes produtoDimensoes, decimal caixaAltura, decimal caixaLargura, decimal caixaComprimento)
        {
            decimal pA = produtoDimensoes.Altura;
            decimal pL = produtoDimensoes.Largura;
            decimal pC = produtoDimensoes.Comprimento;

            // Testa as 6 possíveis orientações do produto dentro da caixa
            return
                // pA, pL, pC (orientação original)
                (pA <= caixaAltura && pL <= caixaLargura && pC <= caixaComprimento) ||
                (pA <= caixaAltura && pC <= caixaLargura && pL <= caixaComprimento) ||
                // pL, pA, pC
                (pL <= caixaAltura && pA <= caixaLargura && pC <= caixaComprimento) ||
                (pL <= caixaAltura && pC <= caixaLargura && pA <= caixaComprimento) ||
                // pC, pA, pL
                (pC <= caixaAltura && pA <= caixaLargura && pL <= caixaComprimento) ||
                (pC <= caixaAltura && pL <= caixaLargura && pA <= caixaComprimento);
        }
    }
}