using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using PerfilWeb.Api.Models;

namespace PerfilWeb.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClientesController : ControllerBase
    {
        private static readonly List<Cliente> clientes = CriarClientesMock();

        private static List<Cliente> CriarClientesMock()
        {
            var lista = new List<Cliente>
            {
                Cliente.Criar("12345678901234", "Cliente Mock 1", DateTime.Now.AddMonths(1))
            };

            var cliente2 = Cliente.Criar("98765432100011", "Cliente Mock 2", DateTime.Now.AddDays(-10), true);
            cliente2.Bloquear("Assinatura vencida");
            lista.Add(cliente2);

            return lista;
        }

        [Authorize]
        [HttpGet]
        public IActionResult GetClientes()
        {
            return Ok(clientes);
        }

        [Authorize]
        [HttpPost("{cnpj}/bloquear")]
        public IActionResult Bloquear(string cnpj, [FromQuery] string? motivo = null)
        {
            var cliente = clientes.FirstOrDefault(c => c.CNPJCPF == cnpj);

            if (cliente == null)
                return NotFound(new { mensagem = "Cliente não encontrado" });

            try
            {
                cliente.Bloquear(motivo ?? "Assinatura bloqueada manualmente");
                return Ok(cliente);
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }

        /// <summary>
        /// Renova definindo uma data específica de validade
        /// </summary>
        [Authorize]
        [HttpPost("{cnpj}/renovar")]
        public IActionResult Renovar(string cnpj, [FromBody] RenovacaoRequest request)
        {
            var cliente = clientes.FirstOrDefault(c => c.CNPJCPF == cnpj);

            if (cliente == null)
                return NotFound(new { mensagem = "Cliente não encontrado" });

            try
            {
                cliente.Renovar(request.DataValidade);
                return Ok(cliente);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }

        [Authorize]
        [HttpPost("{cnpj}/desbloquear")]
        public IActionResult Desbloquear(string cnpj)
        {
            var cliente = clientes.FirstOrDefault(c => c.CNPJCPF == cnpj);

            if (cliente == null)
                return NotFound(new { mensagem = "Cliente não encontrado" });

            try
            {
                cliente.Desbloquear();
                return Ok(cliente);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
        }
    }

    public class RenovacaoRequest
    {
        public DateTime DataValidade { get; set; }
    }
}