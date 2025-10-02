using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace PerfilWeb.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClientesController : ControllerBase
    {
        private static List<Cliente> clientes = new()
        {
            new Cliente { CNPJCPF = "12345678901234", Descricao = "Cliente Mock 1", DataValidade = DateTime.Now.AddMonths(1), Bloqueado = false, Mensagem = "Plano ativo" },
            new Cliente { CNPJCPF = "98765432100011", Descricao = "Cliente Mock 2", DataValidade = DateTime.Now.AddDays(-10), Bloqueado = true, Mensagem = "Assinatura vencida" }
        };

        [Authorize]
        [HttpGet]
        public IActionResult GetClientes()
        {
            return Ok(clientes);
        }

        [Authorize]
        [HttpPost("{cnpj}/bloquear")]
        public IActionResult Bloquear(string cnpj)
        {
            var cliente = clientes.FirstOrDefault(c => c.CNPJCPF == cnpj);
            if (cliente == null) return NotFound();

            cliente.Bloqueado = true;
            cliente.Mensagem = "Assinatura bloqueada manualmente";
            return Ok(cliente);
        }

        [Authorize]
        [HttpPost("{cnpj}/renovar")]
        public IActionResult Renovar(string cnpj)
        {
            var cliente = clientes.FirstOrDefault(c => c.CNPJCPF == cnpj);
            if (cliente == null) return NotFound();

            cliente.Renovar = true;
            cliente.Bloqueado = false;
            cliente.Mensagem = "Assinatura renovada com sucesso";
            return Ok(cliente);
        }
    }

    public class Cliente
    {
        public required string CNPJCPF { get; set; }
        public required string Descricao { get; set; }
        public DateTime DataValidade { get; set; }

        public bool Bloqueado { get; set; }

        public string? Mensagem { get; set; }
        public bool Renovar { get; internal set; }
    }
}