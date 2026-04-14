// api.js — wrapper central para chamadas à API

const API = 'http://localhost:5000/api';

async function apiFetch(path, options = {}) {
  const defaults = {
    credentials: 'include',
    headers: { 'Content-Type': 'application/json', ...options.headers }
  };
  const res = await fetch(API + path, { ...defaults, ...options });
  if (res.status === 401) {
    window.location.href = '/login.html';
    return null;
  }
  const text = await res.text();
  try {
    return { ok: res.ok, status: res.status, data: JSON.parse(text) };
  } catch {
    return { ok: res.ok, status: res.status, data: text };
  }
}

const api = {
  get:    (path)         => apiFetch(path, { method: 'GET' }),
  post:   (path, body)   => apiFetch(path, { method: 'POST',   body: JSON.stringify(body) }),
  delete: (path)         => apiFetch(path, { method: 'DELETE' }),
};

// Toast global
function mostrarToast(msg, tipo = 'sucesso') {
  const t = document.getElementById('toast');
  if (!t) return;
  t.textContent = msg;
  t.style.background = tipo === 'erro' ? '#c0392b' : '#1b4332';
  t.classList.add('visivel');
  setTimeout(() => t.classList.remove('visivel'), 3000);
}

// Formata data br
function formatarData(iso) {
  if (!iso) return '';
  return new Date(iso).toLocaleDateString('pt-BR');
}

function formatarDataHora(iso) {
  if (!iso) return '';
  return new Date(iso).toLocaleString('pt-BR');
}

function formatarMoeda(v) {
  return Number(v).toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
}
