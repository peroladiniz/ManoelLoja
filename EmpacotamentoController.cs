// Controllers/EmpacotamentoController.cs
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using ManoelLoja.Models;
using Microsoft.EntityFrameworkCore;
using ManoelLoja.Data;
using System;
using System.Threading.Tasks;

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

        
        private class TempCaixaEmUso
        {
            public Caixa CaixaOriginal { get; set; } 
            public List<Produto> ProdutosAlocados { get; set; } 
            public decimal VolumeOcupado { get; set; } 

            public TempCaixaEmUso(Caixa caixa)
            {
                CaixaOriginal = caixa;
                ProdutosAlocados = new List<Produto>();
                VolumeOcupado = 0;
            }
            
            public bool TentarAlocarProduto(Produto produto, Func<Dimensoes, decimal, decimal, decimal, bool> canFitFunc)
            {
                if (!canFitFunc(produto.Dimensoes, CaixaOriginal.Altura, CaixaOriginal.Largura, CaixaOriginal.Comprimento))
                {
                    return false; 
                }
                
                if (VolumeOcupado + produto.Volume <= CaixaOriginal.Volume)
                {
                    
                    ProdutosAlocados.Add(produto);
                    VolumeOcupado += produto.Volume;
                    return true;
                }
                return false;
            }
        }

        
        [HttpPost("empacotar")] 
        public async Task<ActionResult<RespostaEmpacotamento>> Empacotar([FromBody] RequisicaoEmpacotamento request)
        {
            
            if (request == null || request.Pedidos == null || !request.Pedidos.Any())
            {
                return BadRequest("A requisição deve conter pelo menos um pedido válido para empacotamento.");
            }
        
            var todasAsCaixasDisponiveis = (await _context.Caixas.ToListAsync())
                                                  .OrderBy(c => c.Volume)
                                                  .ToList();

            if (!todasAsCaixasDisponiveis.Any())
            {
                return BadRequest("Nenhuma caixa disponível no sistema para empacotamento. Por favor, adicione caixas primeiro.");
            }

            RespostaEmpacotamento respostaFinal = new RespostaEmpacotamento();
            respostaFinal.Resultados = new List<ResultadoEmpacotamentoPedido>(); 
                foreach (var pedido in request.Pedidos)
            {
                if (pedido.PedidoId <= 0)
                {
                    respostaFinal.Resultados.Add(new ResultadoEmpacotamentoPedido
                    {
                        PedidoId = pedido.PedidoId,
                        CaixasUtilizadas = new List<CaixaEmpacotada>(), 
                    });
                  
                    return BadRequest($"Pedido inválido: ID do pedido ({pedido.PedidoId}) é obrigatório e deve ser um número positivo.");
                }

              
                if (pedido.Produtos == null || !pedido.Produtos.Any())
                {
                    respostaFinal.Resultados.Add(new ResultadoEmpacotamentoPedido
                    {
                        PedidoId = pedido.PedidoId,
                        CaixasUtilizadas = new List<CaixaEmpacotada>() 
                    });
                    continue; 
                }

                var produtosOrdenadosPorVolume = pedido.Produtos
                                                             .OrderByDescending(p => p.Volume)
                                                             .ToList();

                var caixasAtualmenteEmUso = new List<TempCaixaEmUso>();
                
                foreach (var produto in produtosOrdenadosPorVolume)
                {
                    // Validação de cada produto
                    if (produto.Dimensoes == null ||
                        produto.Dimensoes.Altura <= 0 || produto.Dimensoes.Largura <= 0 || produto.Dimensoes.Comprimento <= 0)
                    {
                        return BadRequest($"Produto inválido no Pedido {pedido.PedidoId}. ProdutoId '{produto.ProdutoId}' com dimensões inválidas (Altura, Largura ou Comprimento <= 0).");
                    }

                    bool produtoAlocado = false;

                    foreach (var caixaEmUso in caixasAtualmenteEmUso)
                    {
                       
                        if (caixaEmUso.TentarAlocarProduto(produto, CanProdutoFitInCaixa))
                        {
                            produtoAlocado = true;
                            break; 
                        }
                    }

                    if (!produtoAlocado)
                    {
                       
                        Caixa? caixaBaseParaNovoProduto = null;
                        decimal menorVolumeEncontrado = decimal.MaxValue;

                        foreach (var caixaDisponivel in todasAsCaixasDisponiveis)
                        {
                            if (CanProdutoFitInCaixa(produto.Dimensoes, caixaDisponivel.Altura, caixaDisponivel.Largura, caixaDisponivel.Comprimento))
                            {
                               
                                if (caixaDisponivel.Volume < menorVolumeEncontrado)
                                {
                                    menorVolumeEncontrado = caixaDisponivel.Volume;
                                    caixaBaseParaNovoProduto = caixaDisponivel;
                                }
                            }
                        }

                     
                        if (caixaBaseParaNovoProduto != null)
                        {
                            var novaCaixaEmUso = new TempCaixaEmUso(caixaBaseParaNovoProduto);
                            novaCaixaEmUso.TentarAlocarProduto(produto, CanProdutoFitInCaixa); 
                            caixasAtualmenteEmUso.Add(novaCaixaEmUso); 
                            produtoAlocado = true;
                        }
                        else
                        {
                          
                            return NotFound($"Produto '{produto.ProdutoId}' (ID: {produto.Id}) do Pedido {pedido.PedidoId} não cabe em nenhuma caixa disponível. Empacotamento falhou para este pedido.");
                        }
                    }
                } 
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

            } 
      
            return Ok(respostaFinal);
        }


    
        [HttpGet("listar-caixas")]
        public async Task<ActionResult<IEnumerable<Caixa>>> ListarCaixas()
        {
            var caixas = await _context.Caixas.ToListAsync();
            return Ok(caixas);
        }

       
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

            return CreatedAtAction(nameof(ObterCaixaPorId), new { id = novaCaixa.Id }, novaCaixa); 
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Caixa>> ObterCaixaPorId(int id)
        {
            
            var caixa = await _context.Caixas.FindAsync(id);

    
            if (caixa == null)
            {
               
                return NotFound($"Caixa com ID {id} não encontrada.");
            }

            
            return Ok(caixa);
        }

        
        [HttpPut("{id}")] 
        public async Task<IActionResult> AtualizarCaixa(int id, [FromBody] Caixa caixaAtualizada)
        {
            // 1. Verifica se o ID da URL corresponde ao ID no corpo da requisição
            if (id != caixaAtualizada.Id)
            {
                return BadRequest("O ID da rota não corresponde ao ID da caixa no corpo da requisição.");
            }

          
            if (caixaAtualizada.Altura <= 0 || caixaAtualizada.Largura <= 0 || caixaAtualizada.Comprimento <= 0)
            {
                return BadRequest("Dados da caixa inválidos. Altura, Largura e Comprimento devem ser maiores que zero.");
            }

           
            var caixaExistente = await _context.Caixas.FindAsync(id);

            if (caixaExistente == null)
            {
              
                return NotFound($"Caixa com ID {id} não encontrada para atualização.");
            }

          
            caixaExistente.Nome = caixaAtualizada.Nome;
            caixaExistente.Altura = caixaAtualizada.Altura;
            caixaExistente.Largura = caixaAtualizada.Largura;
            caixaExistente.Comprimento = caixaAtualizada.Comprimento;

            try
            {
              
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

          
            return CreatedAtAction(nameof(ObterProdutoPorId), new { id = novoProduto.Id }, novoProduto);
        }

      
        [HttpGet("listar-produtos")]
        public async Task<ActionResult<IEnumerable<Produto>>> ListarProdutos()
        {
            var produtos = await _context.Produtos.ToListAsync();
            return Ok(produtos);
        }

      
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

        [HttpPost("empacotar-produto")]
        public async Task<ActionResult<Caixa>> EmpacotarProduto([FromBody] Produto produtoParaEmpacotar)
        {
            if (produtoParaEmpacotar == null || produtoParaEmpacotar.Dimensoes == null ||
                produtoParaEmpacotar.Dimensoes.Altura <= 0 || produtoParaEmpacotar.Dimensoes.Largura <= 0 || produtoParaEmpacotar.Dimensoes.Comprimento <= 0)
            {
                return BadRequest("Dados do produto para empacotar inválidos. Todas as dimensões devem ser maiores que zero.");
            }

            var caixasDisponiveis = await _context.Caixas.ToListAsync(); 

            Caixa? caixaIdeal = null;
            decimal menorVolumeAdequado = decimal.MaxValue;

            foreach (var caixa in caixasDisponiveis)
            {
                bool podeAcomodar = CanProdutoFitInCaixa(produtoParaEmpacotar.Dimensoes, caixa.Altura, caixa.Largura, caixa.Comprimento);

                if (podeAcomodar)
                {
                    
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

     
        private bool CanProdutoFitInCaixa(Dimensoes produtoDimensoes, decimal caixaAltura, decimal caixaLargura, decimal caixaComprimento)
        {
            decimal pA = produtoDimensoes.Altura;
            decimal pL = produtoDimensoes.Largura;
            decimal pC = produtoDimensoes.Comprimento;

         
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
