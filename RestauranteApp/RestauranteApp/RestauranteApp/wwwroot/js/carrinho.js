// carrinho.js — gerenciamento do carrinho local

let carrinho = JSON.parse(localStorage.getItem('carrinho') || '[]');
let periodoCarrinho = localStorage.getItem('periodoCarrinho') || null;

function adicionarAoCarrinho(item, ehSugestao) {
  // Valida período único por carrinho
  if (periodoCarrinho && periodoCarrinho !== item.periodo.toString()) {
    mostrarToast('Seu carrinho só aceita itens do mesmo período (almoço ou jantar).', 'erro');
    return;
  }

  const existente = carrinho.find(c => c.id === item.id);
  if (existente) {
    existente.quantidade++;
  } else {
    const precoFinal = ehSugestao ? item.precoBase * 0.8 : item.precoBase;
    carrinho.push({
      id: item.id,
      nome: item.nome,
      precoBase: item.precoBase,
      precoFinal,
      ehSugestao,
      quantidade: 1,
      periodo: item.periodo
    });
    periodoCarrinho = item.periodo.toString();
    localStorage.setItem('periodoCarrinho', periodoCarrinho);
  }

  salvarCarrinho();
  atualizarBadgeCarrinho();
  mostrarToast(`${item.nome} adicionado ao carrinho!`);
}

function removerDoCarrinho(id) {
  carrinho = carrinho.filter(c => c.id !== id);
  if (carrinho.length === 0) {
    periodoCarrinho = null;
    localStorage.removeItem('periodoCarrinho');
  }
  salvarCarrinho();
  atualizarBadgeCarrinho();
  renderizarCarrinho();
}

function salvarCarrinho() {
  localStorage.setItem('carrinho', JSON.stringify(carrinho));
}

function limparCarrinho() {
  carrinho = [];
  periodoCarrinho = null;
  localStorage.removeItem('carrinho');
  localStorage.removeItem('periodoCarrinho');
  atualizarBadgeCarrinho();
}

function totalCarrinho() {
  return carrinho.reduce((acc, c) => acc + c.precoFinal * c.quantidade, 0);
}

function atualizarBadgeCarrinho() {
  const badge = document.getElementById('carrinho-badge');
  const total = document.getElementById('carrinho-total-nav');
  if (badge) badge.textContent = carrinho.reduce((a, c) => a + c.quantidade, 0);
  if (total) total.textContent = formatarMoeda(totalCarrinho());
}

function renderizarCarrinho() {
  const lista = document.getElementById('carrinho-lista');
  if (!lista) return;

  if (carrinho.length === 0) {
    lista.innerHTML = '<p style="color:#888;text-align:center;padding:1rem">Carrinho vazio</p>';
    const tot = document.getElementById('carrinho-total');
    if (tot) tot.textContent = '';
    return;
  }

  lista.innerHTML = carrinho.map(c => `
    <div class="carrinho-item">
      <div>
        <strong>${c.nome}</strong> x${c.quantidade}
        ${c.ehSugestao ? ' <span style="color:#d4a017;font-size:0.75rem">⭐ Chef</span>' : ''}
        <br><small>${formatarMoeda(c.precoFinal)} cada</small>
      </div>
      <div style="display:flex;align-items:center;gap:0.5rem">
        <strong>${formatarMoeda(c.precoFinal * c.quantidade)}</strong>
        <button onclick="removerDoCarrinho(${c.id})">✕</button>
      </div>
    </div>
  `).join('');

  const tot = document.getElementById('carrinho-total');
  if (tot) tot.innerHTML = `<div class="carrinho-total">Total: ${formatarMoeda(totalCarrinho())}</div>`;
}
