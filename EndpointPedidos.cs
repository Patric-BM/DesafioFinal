using System.Globalization;
using DesafioFinal.BancoDeDados;
using DesafioFinal.BancoDeDados.DTOs;
using Microsoft.EntityFrameworkCore;

namespace DesafioFinal;

public static class EndpointPedidos
{
    public static void MapPedidosEndpoint(this WebApplication app)
    {
        app.MapPost("/pedidos/resumo", async (InMemoryContext context) =>
        {


            var pedidos = context.Pedidos.ToList();
            var clientes = context.Clientes.ToList();
            var totalPedidos = pedidos
                .GroupBy(pedido => pedido.order_date.ToString("yyyy-MM"))
                .OrderByDescending(gasto => gasto.Sum(pedido => pedido.total_amount))
                .ToDictionary(pedido => pedido.Key, pedido => pedido.Sum(pedido => pedido.total_amount).ToString("C", new CultureInfo("pt-BR")));

            var topClientes = clientes
                .Join(pedidos, cliente => cliente.customer_id, pedido => pedido.customer_id, (cliente, pedido) => new { cliente, pedido })
                .GroupBy(cliente => cliente.cliente.first_name + " " + cliente.cliente.last_name)
                .OrderBy(pedido => pedido.Sum(pedido => pedido.pedido.total_amount))
                .Take(10)
                .ToDictionary(cliente => cliente.Key, pedido => pedido.Sum(pedido => pedido.pedido.total_amount).ToString("C", new CultureInfo("pt-BR")));


            var totalPedidosPorQuinzena = pedidos
                .GroupBy(p => p.order_date.ToString("yyyy-MM"))
                .ToDictionary(g => g.Key, g => new
                {
                    primeira = g.Where(p => p.order_date.Day <= 15).Sum(p => p.total_amount).ToString("C", new CultureInfo("pt-BR")),
                    segunda = g.Where(p => p.order_date.Day > 15).Sum(p => p.total_amount).ToString("C", new CultureInfo("pt-BR")),
                });
            return new
            {
                totalPedidos,
                topClientes,
                totalPedidosPorQuinzena
            };


        });


        app.MapPost("/pedidos/mais_comprados", async (InMemoryContext context) =>
        {



            var produtosMaisCompradosPorValor = await (from p in context.Pedidos
                                                       join ip in context.ItensDePedidos on p.order_id equals ip.order_id
                                                       join pr in context.Produtos on ip.product_id equals pr.product_id
                                                       join c in context.Categorias on pr.category_id equals c.category_id
                                                       group new { pr, ip, c } by new { pr.product_name, c.category_name } into g
                                                       orderby g.Sum(p => p.pr.price) descending
                                                       select new
                                                       {
                                                           nome = g.Key.product_name,
                                                           categoria = g.Key.category_name,
                                                           quantidade = g.Sum(p => p.ip.quantity),
                                                           valor = g.Sum(p => p.ip.quantity * p.pr.price).ToString("C", new CultureInfo("pt-BR"))
                                                       }).Take(30).ToListAsync();

            var produtosMaisCompradosPorQuantidade = await (from p in context.Pedidos
                                                            join ip in context.ItensDePedidos on p.order_id equals ip.order_id
                                                            join pr in context.Produtos on ip.product_id equals pr.product_id
                                                            join c in context.Categorias on pr.category_id equals c.category_id
                                                            group new { pr, ip, c } by new { pr.product_name, c.category_name } into g
                                                            orderby g.Sum(p => p.ip.quantity) descending
                                                            select new
                                                            {
                                                                nome = g.Key.product_name,
                                                                categoria = g.Key.category_name,
                                                                quantidade = g.Sum(p => p.ip.quantity),
                                                                valor = g.Sum(p => p.ip.quantity * p.pr.price).ToString("C", new CultureInfo("pt-BR"))
                                                            }).Take(30).ToListAsync();

            return new { produtosMaisCompradosPorValor, produtosMaisCompradosPorQuantidade };





        });


        app.MapPost("/pedidos/mais_comprados_por_categoria", async (InMemoryContext context) =>
        {

            var nomeDaCategoriaPorValor = await (from p in context.Pedidos
                                                 join ip in context.ItensDePedidos on p.order_id equals ip.order_id
                                                 join pr in context.Produtos on ip.product_id equals pr.product_id
                                                 join c in context.Categorias on pr.category_id equals c.category_id
                                                 group new { pr, ip, c } by new { c.category_name } into g
                                                 orderby g.Sum(p => p.ip.quantity * p.pr.price) descending
                                                 select new
                                                 {
                                                     g.Key.category_name,
                                                     produtos = g.Select(p => new
                                                     {
                                                         p.pr.product_name,
                                                         p.ip.quantity,
                                                         valor = (p.ip.quantity * p.pr.price).ToString("C", new CultureInfo("pt-BR"))
                                                     }).Take(30)
                                                 }).ToListAsync();
            Dictionary<string, List<object>> nomeDaCategoriaPorValorDict = new();
            foreach (var item in nomeDaCategoriaPorValor)
            {
                nomeDaCategoriaPorValorDict.Add(item.category_name, item.produtos.ToList<object>());
            }

            var nomeDaCategoriaPorQuantidade = await (from p in context.Pedidos
                                                      join ip in context.ItensDePedidos on p.order_id equals ip.order_id
                                                      join pr in context.Produtos on ip.product_id equals pr.product_id
                                                      join c in context.Categorias on pr.category_id equals c.category_id
                                                      group new { pr, ip, c } by new { c.category_name } into g
                                                      orderby g.Sum(p => p.ip.quantity) descending
                                                      select new
                                                      {
                                                          g.Key.category_name,
                                                          produtos = g.Select(p => new
                                                          {
                                                              p.pr.product_name,
                                                              p.ip.quantity,
                                                              valor = (p.ip.quantity * p.pr.price).ToString("C", new CultureInfo("pt-BR"))
                                                          }).Take(30)
                                                      }).ToListAsync();

            Dictionary<string, List<object>> nomeDaCategoriaPorQuantidadeDict = new();
            foreach (var item in nomeDaCategoriaPorQuantidade)
            {
                nomeDaCategoriaPorQuantidadeDict.Add(item.category_name, item.produtos.ToList<object>());
            }

            return new { nomeDaCategoriaPorValor = nomeDaCategoriaPorValorDict, nomeDaCategoriaPorQuantidade = nomeDaCategoriaPorQuantidadeDict };


        });

        app.MapPost("/pedidos/mais_comprados_por_fornecedor", async (InMemoryContext context) =>
        {


            var maisCompradosPorFornecedor = await (from p in context.Pedidos
                                                  join ip in context.ItensDePedidos on p.order_id equals ip.order_id
                                                  join pr in context.Produtos on ip.product_id equals pr.product_id
                                                  join ca in context.Categorias on pr.category_id equals ca.category_id
                                                  join s in context.Fornecedores on pr.supplier_id equals s.supplier_id
                                                  group new { pr, ip, ca, s } by new { s.supplier_name } into g
                                                  orderby  g.Key.supplier_name ascending
                                                    select new
                                                    {
                                                        g.Key.supplier_name,
                                                        produtos = g.Select(p => new
                                                        {
                                                            p.pr.product_name,
                                                            p.ca.category_name,
                                                            p.ip.quantity,
                                                            valor = (p.ip.quantity * p.pr.price).ToString("C", new CultureInfo("pt-BR"))
                                                        }).Take(30)
                                                    }).ToListAsync();

            Dictionary<string, List<object>> nomeDoFornecedorDict = new();
            foreach (var item in maisCompradosPorFornecedor)
            {
                nomeDoFornecedorDict.Add(item.supplier_name, item.produtos.ToList<object>());
            }

            return new { maisCompradosPorFornecedor = nomeDoFornecedorDict  };


        });


    }
}