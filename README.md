# 🍽 Restaurante Bella Vista — Sistema de Gestão

Projeto desenvolvido em **C# + ASP.NET Core 8 + Entity Framework + SQL Server Express**.

---

## 📋 Pré-requisitos

- [Visual Studio 2022](https://visualstudio.microsoft.com/) (com workload *ASP.NET and web development*)
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)
- SQL Server Express (já vem com o Visual Studio, ou baixe separado)

---

## 🚀 Como rodar

### 1. Abrir o projeto
Dê dois cliques em `RestauranteApp.sln` para abrir no Visual Studio.

### 2. Restaurar pacotes NuGet
O Visual Studio fará isso automaticamente. Caso não faça:
- Menu **Tools → NuGet Package Manager → Manage NuGet Packages for Solution**
- Clique em **Restore**

### 3. Configurar connection string (se necessário)
Abra `RestauranteApp/appsettings.json`.  
A connection string padrão usa **LocalDB** (já instalado com o Visual Studio):
```json
"Server=(localdb)\\mssqllocaldb;Database=RestauranteAppDb;Trusted_Connection=True"
```
Se quiser usar o SQL Server Express instalado separadamente, mude para:
```json
"Server=.\\SQLEXPRESS;Database=RestauranteAppDb;Trusted_Connection=True"
```

### 4. Aplicar as Migrations
No Visual Studio, abra o **Package Manager Console** (Tools → NuGet → Package Manager Console) e rode:
```
Add-Migration InitialCreate
Update-Database
```
> O banco será criado automaticamente com todas as tabelas e dados de seed (40 itens de cardápio, 5 mesas, ingredientes).

### 5. Rodar o projeto
Pressione **F5** ou clique no botão ▶️ verde.

O navegador abrirá em `http://localhost:5000` com a landing page.

---

## 🌐 Páginas disponíveis

| Página | URL |
|---|---|
| Landing Page | `http://localhost:5000/` |
| Login / Cadastro | `http://localhost:5000/login.html` |
| Relatórios | `http://localhost:5000/relatorio.html` |
| Swagger (API) | `http://localhost:5000/swagger` |

---

## 📁 Estrutura do Projeto

```
RestauranteApp/
├── Controllers/
│   ├── AuthController.cs        ← login, cadastro, logout
│   ├── CardapioController.cs    ← cardápio + sugestão do chefe
│   ├── PedidoController.cs      ← criar pedido, listar pedidos
│   ├── ReservaController.cs     ← reservar mesa, consultar disponibilidade
│   ├── RelatorioController.cs   ← relatórios de faturamento e vendas
│   └── EnderecoController.cs    ← endereços do usuário
├── Models/
│   ├── Usuario.cs               ← extends IdentityUser
│   ├── Endereco.cs
│   ├── Cardapio.cs              ← ItemCardapio, Ingrediente, N-N
│   ├── SugestaoChefe.cs
│   ├── Atendimento.cs           ← classe base + 3 subclasses (herança TPH)
│   ├── Pedido.cs                ← Pedido + PedidoItem
│   └── Reserva.cs               ← Mesa + Reserva
├── Services/
│   ├── CardapioService.cs
│   ├── PedidoService.cs
│   ├── ReservaService.cs
│   └── RelatorioService.cs
├── Data/
│   └── ApplicationDbContext.cs  ← DbContext + configurações + seed
├── wwwroot/
│   ├── index.html               ← landing page principal
│   ├── login.html               ← login e cadastro
│   ├── relatorio.html           ← relatórios
│   ├── css/style.css
│   └── js/
│       ├── api.js               ← helper de fetch
│       └── carrinho.js          ← carrinho local (localStorage)
├── Program.cs                   ← configuração do app
└── appsettings.json             ← connection string
```

---

## 🏗 Conceitos de POO aplicados

| Conceito | Onde |
|---|---|
| **Herança** | `Atendimento` (base) → `AtendimentoPresencial`, `AtendimentoDeliveryProprio`, `AtendimentoDeliveryAplicativo` |
| **Polimorfismo** | Método `CalcularTaxa()` sobrescrito em cada subclasse |
| **Abstração** | Classe `Atendimento` é abstrata; `CalcularTaxa()` é abstrato |
| **Encapsulamento** | Services isolam regras de negócio dos Controllers |
| **Relacionamentos N-N** | `ItemCardapio ↔ Ingrediente` e `Pedido ↔ ItemCardapio` |
| **Relacionamentos 1-N** | `Usuario → Pedidos`, `Usuario → Enderecos`, `Mesa → Reservas` |

---

## 📊 Regras de negócio implementadas

- ✅ Só 1 sugestão do chefe por período por dia (index único no banco)
- ✅ Desconto de 20% aplicado no momento do pedido
- ✅ Pedido de almoço só aceita itens de almoço (validação no Service)
- ✅ Reservas só entre 19h e 22h
- ✅ Taxa delivery próprio: fixa; delivery app: 4% dia / 6% noite
- ✅ Usuário pode ter múltiplos endereços
- ✅ Código de confirmação gerado automaticamente para reservas

