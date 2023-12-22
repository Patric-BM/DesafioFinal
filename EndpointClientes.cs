using DesafioFinal.BancoDeDados;
using DesafioFinal.BancoDeDados.DTOs;
using Microsoft.EntityFrameworkCore;

namespace DesafioFinal
{
    public static class EndpointClientes
    {
        public static void MapClientesEndpoint(this WebApplication app)
        {
            app.MapPost("/clientes", async (InMemoryContext context) =>
            {

                IQueryable<Clientes> clientesDTO = (from c in context.Clientes
                                                 orderby c.first_name
                                                 select c).Take(100);

                var clientes = await clientesDTO.Select(c => new
                {
                    NomeCompleto = c.first_name,
                    Email = c.email,

                }).ToListAsync();

                
                return new { clientes };
                
            });

            app.MapPost("/clientes/resumo", async (InMemoryContext context) =>
            {

                var clientes = context.Clientes.ToList();
                var paisesComMaisClientes = clientes
                    .GroupBy(c => string.IsNullOrEmpty(c.country) || c.country == "-" ? "desconhecido" : c.country)
                    .OrderByDescending(g => g.Count())
                    .Take(5)
                    .ToDictionary(g => g.Key, g => g.Count());

                var dominios = clientes
                    .Where(c => !string.IsNullOrEmpty(c.email))
                    .GroupBy(c => c.email.Split('@').Last())
                    .OrderByDescending(g => g.Count())
                    .Take(5)
                    .ToDictionary(g => g.Key, g => g.Count());

                return new
                {
                    paisesComMaisClientes,
                    dominios
                };


            });

        }

    }
}
