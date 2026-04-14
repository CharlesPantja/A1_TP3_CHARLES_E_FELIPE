using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RestauranteApp.Models;

namespace RestauranteApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<Usuario> _userManager;
        private readonly SignInManager<Usuario> _signInManager;

        public AuthController(UserManager<Usuario> userManager, SignInManager<Usuario> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [HttpPost("cadastro")]
        public async Task<IActionResult> Cadastro([FromBody] CadastroRequest req)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var usuario = new Usuario
            {
                UserName = req.Email,
                Email = req.Email,
                NomeCompleto = req.NomeCompleto
            };

            var result = await _userManager.CreateAsync(usuario, req.Senha);
            if (!result.Succeeded)
                return BadRequest(result.Errors.Select(e => e.Description));

            await _signInManager.SignInAsync(usuario, isPersistent: false);
            return Ok(new { mensagem = "Cadastro realizado com sucesso!", nome = usuario.NomeCompleto });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest req)
        {
            var result = await _signInManager.PasswordSignInAsync(
                req.Email, req.Senha, req.LembrarMe, lockoutOnFailure: false);

            if (!result.Succeeded)
                return Unauthorized("Email ou senha inválidos.");

            var usuario = await _userManager.FindByEmailAsync(req.Email);
            return Ok(new { mensagem = "Login realizado!", nome = usuario!.NomeCompleto, email = usuario.Email });
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return Ok("Logout realizado.");
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> Me()
        {
            var usuario = await _userManager.GetUserAsync(User);
            if (usuario == null) return Unauthorized();
            return Ok(new { nome = usuario.NomeCompleto, email = usuario.Email });
        }

        public class CadastroRequest
        {
            public string NomeCompleto { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Senha { get; set; } = string.Empty;
        }

        public class LoginRequest
        {
            public string Email { get; set; } = string.Empty;
            public string Senha { get; set; } = string.Empty;
            public bool LembrarMe { get; set; } = false;
        }
    }
}
