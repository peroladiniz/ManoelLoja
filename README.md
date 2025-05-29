# Loja do Seu Manoel – API de Empacotamento de Pedidos

Este projeto é um microserviço desenvolvido em .NET 7 que automatiza o processo de empacotamento de pedidos para a loja de jogos online do Seu Manoel. A aplicação processa pedidos com produtos de diferentes tamanhos e determina a melhor forma de alocá-los em caixas pré-definidas.

## Descrição do Problema

A loja do Seu Manoel deseja otimizar a forma como os pedidos são embalados. Dado um conjunto de produtos com suas respectivas dimensões (altura, largura e comprimento), a API deve decidir automaticamente:

- Quais caixas usar por pedido
- Quais produtos vão em cada caixa
- Como minimizar o número de caixas usadas

## Tamanhos das Caixas Disponíveis

| Caixa | Altura (cm) | Largura (cm) | Comprimento (cm) |
|-------|-------------|--------------|------------------|
| 1     | 30          | 40           | 80               |
| 2     | 80          | 50           | 40               |
| 3     | 50          | 80           | 60               |

## Requisitos Atendidos

- Microserviço criado em .NET 7
- Banco de dados SQL Server
- API e banco rodando via Docker + Docker Compose
- Swagger disponível para testes
- Estrutura com repositório GitHub organizada
- Documentação completa neste README.md

## Pré-requisitos

- Docker instalado
- Docker Compose (incluso no Docker Desktop)
- (Opcional) Visual Studio ou VS Code para editar/explorar o projeto

## Como Executar o Projeto

1. Clone o repositório:

`` bash
git clone https://github.com/seuusuario/seu-repositorio.git
cd seu-repositorio


2. Suba os containers com Docker Compose:

bash
Copiar
Editar
docker-compose up --build


3. Acesse a aplicação:

API Swagger: http://localhost:8080/swagger
A porta 8081 também está exposta como reserva para acesso HTTPS ou futura configuração com TLS. Foi incluída para facilitar a expansão futura ou testes em ambientes com HTTPS configurado.

Endpoints Principais
POST /api/empacotador/empacotar
Descrição: Recebe um ou mais pedidos com seus produtos e retorna quais caixas foram usadas e os produtos em cada caixa.

## ESTRUTURA DO PROJETO 

/LojaSeuManoel
│
├── Dockerfile
├── docker-compose.yml
├── README.md
├── /LojaSeuManoel.API
│   ├── Controllers
│   ├── Services
│   ├── Models
│   ├── Program.cs
│   └── appsettings.json


Banco de Dados
SQL Server é executado no container sqlserver_manoelloja

Nome do banco: ManoelLojaDb

Usuário: sa

Senha: Senhaforte321

A conexão é configurada no appsettings.json ("ConnectionStrings": {
  "DefaultConnection": "Server=sqlserver_manoelloja,1433;Database=ManoelLojaDb;User Id=sa;Password=Senhaforte321;"
}



## Testes

(Opcional) Testes unitários podem ser adicionados na pasta Tests/.

Sobre as portas do Docker
As portas 8080 e 8081 foram expostas com os seguintes propósitos:

8080: Utilizada para acessar a aplicação via HTTP e Swagger.

8081: Mantida para possíveis testes futuros com HTTPS, uso de TLS ou configuração de ambientes seguros sem alterar a estrutura atual.

Considerações Finais
Este projeto foi desenvolvido como parte de um desafio técnico, com foco em boas práticas de organização, uso de Docker, separação de camadas e documentação clara.
