using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestauranteApp.Data;
using RestauranteApp.Models;
using System.Security.Claims;

namespace RestauranteApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class EnderecoController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public EnderecoController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetEnderecos()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var enderecos = await _context.Enderecos
                .Where(e => e.UsuarioId == userId)
                .ToListAsync();
            return Ok(enderecos);
        }

        [HttpPost]
        public async Task<IActionResult> AdicionarEndereco([FromBody] Endereco endereco)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            endereco.UsuarioId = userId;
            _context.Enderecos.Add(endereco);
            await _context.SaveChangesAsync();
            return Ok(endereco);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> RemoverEndereco(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var endereco = await _context.Enderecos
                .FirstOrDefaultAsync(e => e.Id == id && e.UsuarioId == userId);
            if (endereco == null) return NotFound();
            _context.Enderecos.Remove(endereco);
            await _context.SaveChangesAsync();
            return Ok("Endereço removido.");
        }
    }
}
