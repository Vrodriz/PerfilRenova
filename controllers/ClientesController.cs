using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using PerfilWeb.Api.Models;
using PerfilRenovaWeb.api.Dtos;

namespace PerfilWeb.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClientesController : ControllerBase
    {
        private static readonly List<Client> _clients = CreateMockClients();

        private static List<Client> CreateMockClients()
        {
            var list = new List<Client>
            {
                Client.Create(1, "12.345.678/0001-90", "Empresa Tech Solutions Ltda", DateTime.Parse("2026-12-31")),
                Client.Create(3, "11.222.333/0001-44", "Indústria Metal Forte S.A", DateTime.Parse("2024-03-20"), true),
                Client.Create(6, "33.444.555/0001-66", "Construtora Alicerce", DateTime.Parse("2026-01-05")),
                Client.Create(8, "66.777.888/0001-99", "Clínica Médica Saúde Total", DateTime.Parse("2023-12-15"), true),
                Client.Create(9, "77.888.999/0001-22", "Supermercado Economia Ltda", DateTime.Parse("2026-06-30")),
                Client.Create(12, "10.111.222/0001-55", "Advocacia Justiça & Direito", DateTime.Parse("2024-01-10"), true),
                Client.Create(13, "20.222.333/0001-66", "Padaria Pão Quente", DateTime.Parse("2026-08-15"))
            };

            var client2 = Client.Create(2, "98.765.432/0001-10", "Comércio Digital Brasil", DateTime.Parse("2025-11-15"));
            client2.Block(Models.ClientMessage.ManuallyBlocked);
            list.Add(client2);

            return list;
        }
    }
}