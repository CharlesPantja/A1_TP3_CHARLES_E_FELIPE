using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestauranteApp.Models;

namespace RestauranteApp.Controllers
{
    [ApiController]
    [Route("api/admin")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly UserManager<Usuario> _userManager;

        public AdminController(UserManager<Usuario> userManager)
        {
            _userManager = userManager;
        }

        // GET /api/admin/usuarios
        [HttpGet("usuarios")]
        public async Task<IActionResult> ListarUsuarios()
        {
            var usuarios = await _userManager.Users
                .OrderBy(u => u.Email)
                .ToListAsync();

            var resultado = new List<object>();
            foreach (var u in usuarios)
            {
                var roles = await _userManager.GetRolesAsync(u);
                resultado.Add(new
                {
                    id           = u.Id,
                    nome         = u.NomeCompleto,
                    email        = u.Email,
                    isAdmin      = roles.Contains("Admin"),
                    emailConfirmed = u.EmailConfirmed,
                    lockoutEnabled = u.LockoutEnabled,
                    lockoutEnd   = u.LockoutEnd
                });
            }
            return Ok(resultado);
        }

        // PATCH /api/admin/usuarios/{id}/papel — promove/rebaixa admin
        [HttpPatch("usuarios/{id}/papel")]
        public async Task<IActionResult> AlterarPapel(string id, [FromBody] AlterarPapelRequest req)
        {
            var meId = _userManager.GetUserId(User);
            if (id == meId)
                return BadRequest(new { message = "Você não pode alterar seu próprio papel." });

            var usuario = await _userManager.FindByIdAsync(id);
            if (usuario == null) return NotFound(new { message = "Usuário não encontrado." });

            if (req.IsAdmin)
            {
                if (!await _userManager.IsInRoleAsync(usuario, "Admin"))
                    await _userManager.AddToRoleAsync(usuario, "Admin");
            }
            else
            {
                if (await _userManager.IsInRoleAsync(usuario, "Admin"))
                    await _userManager.RemoveFromRoleAsync(usuario, "Admin");
            }

            return Ok(new { message = req.IsAdmin ? "Usuário promovido a Admin." : "Privilégio Admin removido." });
        }

        // DELETE /api/admin/usuarios/{id}
        [HttpDelete("usuarios/{id}")]
        public async Task<IActionResult> ExcluirUsuario(string id)
        {
            var meId = _userManager.GetUserId(User);
            if (id == meId)
                return BadRequest(new { message = "Você não pode excluir sua própria conta." });

            var usuario = await _userManager.FindByIdAsync(id);
            if (usuario == null) return NotFound(new { message = "Usuário não encontrado." });

            if (await _userManager.IsInRoleAsync(usuario, "Admin"))
                return BadRequest(new { message = "Não é possível excluir outro administrador." });

            var result = await _userManager.DeleteAsync(usuario);
            if (!result.Succeeded)
                return BadRequest(result.Errors.Select(e => e.Description));

            return Ok(new { message = "Usuário excluído." });
        }

        public class AlterarPapelRequest
        {
            public bool IsAdmin { get; set; }
        }
    }
}
