# Cadastro de Biometria de Cooperados

## 📋 Como Usar

### Passo 1: Abrir a Tela de Cadastro
1. Na tela principal do sistema, clique em **"Cadastrar Novo Funcionário"**
2. A tela de cadastro abrirá e automaticamente carregará a lista de todos os cooperados do servidor web

### Passo 2: Lista de Cooperados
- Ao abrir a tela, você verá **TODOS os cooperados cadastrados no Neon (web)**
- A lista mostrará:
  - **Nome completo**
  - **Matrícula**
  - **Categoria/Cargo**

### Passo 3: Pesquisar Cooperado (Opcional)
- Use o campo "Pesquisar Funcionário" para filtrar por:
  - Nome completo
  - Matrícula
- Se deixar em branco, mostrará todos os cooperados

### Passo 4: Selecionar Cooperado
1. Clique no nome do cooperado na lista
2. Os campos serão preenchidos automaticamente:
   - Nome Completo
   - Matrícula
   - E-mail
   - Cargo

### Passo 5: Capturar Digital
1. Clique em **"👆 Capturar Digital"**
2. Coloque o dedo no leitor biométrico
3. Aguarde a captura ser processada

### Passo 6: Salvar Cadastro
1. Após capturar a digital, clique em **"💾 Salvar Cadastro"**
2. A biometria será registrada no servidor web (Neon)
3. Uma confirmação será exibida

## 🔄 Fluxo Completo

```
┌─────────────────────────────────┐
│  Abre Tela de Cadastro          │
└────────────┬────────────────────┘
             │
             ▼
┌─────────────────────────────────┐
│  Carrega Lista do Web (Neon)    │
│  - Mostra todos os cooperados   │
└────────────┬────────────────────┘
             │
             ▼
┌─────────────────────────────────┐
│  Seleciona Cooperado da Lista   │
│  - Campos preenchidos auto      │
└────────────┬────────────────────┘
             │
             ▼
┌─────────────────────────────────┐
│  Captura Digital no Leitor      │
│  - Posiciona dedo               │
└────────────┬────────────────────┘
             │
             ▼
┌─────────────────────────────────┐
│  Salva no Servidor Web (Neon)   │
│  - Envia biometria              │
└─────────────────────────────────┘
```

## ⚙️ Integração Técnica

### Dados Carregados do Web
- **Origem:** API do web em `https://bypass-lime.vercel.app`
- **Endpoint:** `GET /api/users`
- **Dados retornados:**
  - ID
  - Name (Nome)
  - Email
  - Matricula
  - Categoria

### Dados Salvos
- **Destino:** API do web
- **Endpoint:** `POST /api/users/{id}/biometrics`
- **Dados enviados:**
  - Biometric (bytes da digital)
  - BiometricType (fingerprint)

## 📱 Status e Feedback

A tela mostra mensagens de status em tempo real:
- ⏳ Carregando usuários do servidor...
- ✓ N usuários carregados com sucesso!
- Mostrando todos os N funcionário(s)
- ✓ Funcionário selecionado: Nome
- 📥 Posicione o dedo no leitor...
- ✓ Biometria capturada com sucesso!
- 📤 Registrando biometria no servidor...

## 🔗 Sincronização

✅ **A lista é sincronizada em tempo real com o Neon**
- Novos cooperados cadastrados no web aparecerão na próxima abertura da tela
- Dados do cooperado vêm diretamente do banco Neon
- Biometria é registrada no Neon via API

## ❓ Dúvidas Frequentes

**P: Preciso cadastrar os dados do cooperado?**
R: Não! Os dados vêm prontos do web. Você só captura a digital.

**P: Posso editar os dados do cooperado?**
R: Não, os campos são de apenas leitura. Os dados vêm do servidor.

**P: Onde a biometria é armazenada?**
R: No banco Neon, na tabela `biometrics`, sincronizado via API.

**P: E se a lista não aparecer?**
R: Verifique:
- Conexão com internet
- API ativa em `https://bypass-lime.vercel.app`
- Cooperados cadastrados no web
