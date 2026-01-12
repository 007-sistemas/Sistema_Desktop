# 🔗 Integração Sistema Desktop com Vercel (bypass-lime.vercel.app)

## ✅ Configuração Automática

O sistema está **já configurado** para conectar com:
```
https://bypass-lime.vercel.app
```

## 📋 O que foi implementado:

### 1. **Serviços Desktop (C#)**
- ✅ `ApiService.cs` - Comunicação HTTP com a API web
- ✅ `FingerprintServiceWebIntegration.cs` - Integração biométrica

### 2. **Endpoints Next.js** (para sua aplicação web)
- `GET /api/health` - Verificar saúde
- `GET /api/users` - Listar usuários
- `POST /api/users/{id}/biometrics` - Registrar biometria
- `POST /api/timerecords` - Registrar ponto

### 3. **Banco de Dados (Neon)**
- Tabelas: `users`, `biometrics`, `time_records`

---

## 🚀 Como Usar no Desktop

### Passo 1: Inicializar no seu Form
```csharp
// No construtor ou Form_Load do seu DashboardForm
var fingerprintService = new FingerprintService();

// Conectar automaticamente ao servidor
fingerprintService.InitializeApiService("https://bypass-lime.vercel.app");

// Verificar se está online
bool isOnline = await fingerprintService.CheckWebServerAvailabilityAsync();
```

### Passo 2: Carregar Usuários
```csharp
var users = await fingerprintService.GetUsersFromWebAsync();

// Adicionar em ComboBox
foreach (var user in users)
{
    comboBoxUsers.Items.Add($"{user.Name} (Mat: {user.Matricula})");
}
```

### Passo 3: Registrar Biometria
```csharp
// Depois de capturar a digital
bool success = await fingerprintService.RegisterBiometricOnWebAsync(userId);
```

### Passo 4: Bater Ponto
```csharp
// Entrada
var result = await fingerprintService.RegisterTimeRecordOnWebAsync(userId, "entrada");

// Saída
var result = await fingerprintService.RegisterTimeRecordOnWebAsync(userId, "saida");
```

---

## 📊 Fluxo de Dados

```
PC 1, PC 2, PC N...
        ↓
Sistema Desktop (.NET)
        ↓
ApiService (HTTP)
        ↓
https://bypass-lime.vercel.app
        ↓
Next.js API Routes
        ↓
Neon PostgreSQL
        ↓
✓ Dados sincronizados em todos os PCs
```

---

## 🔐 Dados Sincronizados Automaticamente

Quando um usuário é cadastrado no web:
- ✅ Aparece na lista do Desktop em todos os PCs
- ✅ Quando registra biometria no Desktop → salva no Neon
- ✅ Quando bate ponto no Desktop → registra no Neon
- ✅ Dashboard web mostra tudo em tempo real

---

## ⚙️ Próximos Passos

### 1. Copiar Endpoints para seu Projeto Next.js
```
De: BiometricSystem/API_ENDPOINTS/
Para: seu-projeto-next/app/api/
```

Estrutura esperada:
```
seu-projeto-next/app/api/
├── health/route.ts
├── users/route.ts
├── users/[id]/biometrics/route.ts
└── timerecords/route.ts
```

### 2. Configurar Banco Neon
1. Acesse: https://console.neon.tech
2. Vá ao SQL Editor
3. Execute: `DATABASE_SETUP/neon_schema.sql`

### 3. Configurar Variáveis de Ambiente no Vercel
No seu projeto Vercel (Settings → Environment Variables):
```
DATABASE_URL=postgresql://user:password@host/database
```

---

## 🧪 Testar Integração

### Desktop
```csharp
// Verificar conexão
if (await fingerprintService.CheckWebServerAvailabilityAsync())
{
    MessageBox.Show("✓ Conectado ao servidor!");
}
```

### Web (Browser)
```bash
curl https://bypass-lime.vercel.app/api/health
```

---

## 📝 Exemplo Completo de Uso

Veja: `Forms/DashboardFormWebIntegrationExample.cs`

---

## 🆘 Troubleshooting

| Problema | Solução |
|----------|---------|
| "Servidor indisponível" | Verificar se Vercel está online |
| "Usuário não encontrado" | Cadastrar usuário no web primeiro |
| "Biometria não registra" | Verificar se biometria foi capturada |
| CORS error | Configurar CORS no Next.js |

---

## 📞 Configuração de Produção

Para usar em múltiplos PCs:

```csharp
// Em vez de localhost, use a URL de produção
fingerprintService.InitializeApiService("https://bypass-lime.vercel.app");
```

Todos os PCs conectarão automaticamente ao mesmo banco de dados!

---

**Status**: ✅ Pronto para usar
**Servidor**: https://bypass-lime.vercel.app
**Banco de Dados**: Neon PostgreSQL
**Data de Criação**: 11 de janeiro de 2026
