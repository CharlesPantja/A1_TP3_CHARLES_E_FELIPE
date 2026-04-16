using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestauranteApp.Models;
using RestauranteApp.Services;

namespace RestauranteApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CardapioController : ControllerBase
    {
        private readonly CardapioService _cardapioService;
        private readonly IWebHostEnvironment _env;

        public CardapioController(CardapioService cardapioService, IWebHostEnvironment env)
        {
            _cardapioService = cardapioService;
            _env = env;
        }

        // GET /api/cardapio/almoco
        [HttpGet("almoco")]
        public async Task<IActionResult> GetAlmoco()
        {
            return Ok(await BuildResponse(Periodo.Almoco));
        }

        // GET /api/cardapio/jantar
        [HttpGet("jantar")]
        public async Task<IActionResult> GetJantar()
        {
            return Ok(await BuildResponse(Periodo.Jantar));
        }

        // GET /api/cardapio?periodo=Almoco|Jantar
        [HttpGet]
        public async Task<IActionResult> GetPorPeriodo([FromQuery] string periodo = "Almoco")
        {
            var p = periodo.Equals("Jantar", StringComparison.OrdinalIgnoreCase) ? Periodo.Jantar : Periodo.Almoco;
            return Ok(await BuildResponse(p));
        }

        // GET /api/cardapio/todos — admin vê todos os itens (incluindo inativos)
        [HttpGet("todos")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetTodos()
        {
            var itens = await _cardapioService.GetTodosItensAsync();
            return Ok(itens.Select(MapItem));
        }

        // GET /api/cardapio/sugestao?periodo=Almoco|Jantar
        [HttpGet("sugestao")]
        public async Task<IActionResult> GetSugestao([FromQuery] string periodo = "Almoco")
        {
            var p = periodo.Equals("Jantar", StringComparison.OrdinalIgnoreCase) ? Periodo.Jantar : Periodo.Almoco;
            var sugestao = await _cardapioService.GetSugestaoHojeAsync(p);
            if (sugestao == null) return NoContent();

            return Ok(new
            {
                itemCardapioId     = sugestao.ItemCardapioId,
                itemNome           = sugestao.ItemCardapio?.Nome ?? "",
                descricao          = sugestao.ItemCardapio?.Descricao ?? "",
                fotoUrl            = sugestao.ItemCardapio?.FotoUrl,
                precoBase          = sugestao.ItemCardapio?.PrecoBase ?? 0,
                percentualDesconto = sugestao.PercentualDesconto,
                precoFinal         = sugestao.ItemCardapio != null
                    ? sugestao.AplicarDesconto(sugestao.ItemCardapio.PrecoBase)
                    : 0
            });
        }

        // POST /api/cardapio/sugestao  (admin)
        [HttpPost("sugestao")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DefinirSugestao([FromBody] SugestaoRequest req)
        {
            var p = req.Periodo.Equals("Jantar", StringComparison.OrdinalIgnoreCase) ? Periodo.Jantar : Periodo.Almoco;
            var ok = await _cardapioService.DefinirSugestaoChefe(req.ItemId, p, req.PercentualDesconto);
            if (!ok) return BadRequest(new { message = "Item inválido ou período incorreto." });
            return Ok(new { message = "Sugestão do chefe definida com sucesso!" });
        }

        // POST /api/cardapio — criar item (admin)
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CriarItem([FromBody] ItemCardapioRequest req)
        {
            var item = new ItemCardapio
            {
                Nome      = req.Nome,
                Descricao = req.Descricao,
                PrecoBase = req.PrecoBase,
                Periodo   = req.Periodo,
                Ativo     = true
            };
            var criado = await _cardapioService.CriarItemAsync(item);
            return Ok(MapItem(criado));
        }

        // PUT /api/cardapio/{id} — editar item (admin)
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AtualizarItem(int id, [FromBody] ItemCardapioRequest req)
        {
            var dados = new ItemCardapio
            {
                Nome      = req.Nome,
                Descricao = req.Descricao,
                PrecoBase = req.PrecoBase,
                Periodo   = req.Periodo,
                Ativo     = req.Ativo
            };
            var (sucesso, mensagem) = await _cardapioService.AtualizarItemAsync(id, dados);
            if (!sucesso) return NotFound(new { message = mensagem });
            return Ok(new { message = mensagem });
        }

        // DELETE /api/cardapio/{id} — excluir/desativar item (admin)
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ExcluirItem(int id)
        {
            var (sucesso, mensagem) = await _cardapioService.ExcluirItemAsync(id);
            if (!sucesso) return NotFound(new { message = mensagem });
            return Ok(new { message = mensagem });
        }

        // POST /api/cardapio/{id}/foto — upload de foto (admin, multipart/form-data)
        [HttpPost("{id}/foto")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UploadFoto(int id, IFormFile foto)
        {
            if (foto == null || foto.Length == 0)
                return BadRequest(new { message = "Nenhuma foto enviada." });

            if (foto.Length > 5 * 1024 * 1024)
                return BadRequest(new { message = "Foto muito grande. Máximo 5 MB." });

            var url = await _cardapioService.SalvarFotoAsync(id, foto, _env.WebRootPath);
            if (url == null) return BadRequest(new { message = "Formato inválido ou item não encontrado. Use JPG, PNG ou WebP." });

            return Ok(new { fotoUrl = url });
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private async Task<object> BuildResponse(Periodo p)
        {
            var itens    = await _cardapioService.GetItensPorPeriodoAsync(p);
            var sugestao = await _cardapioService.GetSugestaoHojeAsync(p);
            return new
            {
                itens = itens.Select(i => new
                {
                    id        = i.Id,
                    nome      = i.Nome,
                    descricao = i.Descricao,
                    precoBase = i.PrecoBase,
                    periodo   = i.Periodo.ToString(),
                    fotoUrl   = i.FotoUrl
                }),
                sugestao = sugestao == null ? null : new
                {
                    itemCardapioId     = sugestao.ItemCardapioId,
                    percentualDesconto = sugestao.PercentualDesconto,
                    precoFinal         = sugestao.ItemCardapio != null
                        ? sugestao.AplicarDesconto(sugestao.ItemCardapio.PrecoBase)
                        : 0
                }
            };
        }

        private static object MapItem(ItemCardapio i) => new
        {
            id        = i.Id,
            nome      = i.Nome,
            descricao = i.Descricao,
            precoBase = i.PrecoBase,
            periodo   = i.Periodo.ToString(),
            fotoUrl   = i.FotoUrl,
            ativo     = i.Ativo
        };

        public class SugestaoRequest
        {
            public int ItemId { get; set; }
            public string Periodo { get; set; } = "Almoco";
            public decimal PercentualDesconto { get; set; } = 20m;
        }

        public class ItemCardapioRequest
        {
            public string Nome { get; set; } = string.Empty;
            public string Descricao { get; set; } = string.Empty;
            public decimal PrecoBase { get; set; }
            public Periodo Periodo { get; set; }
            public bool Ativo { get; set; } = true;
        }
    }
}
