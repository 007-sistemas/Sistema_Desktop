# 📝 Tela de Cadastro de Funcionário - Guia de Uso

## 🎯 O que faz:

1. **Pesquisa em Tempo Real** - Busca funcionários pelo nome ou matrícula
2. **Carregamento Automático** - Traz todos os usuários do sistema web
3. **Preenchimento Automático** - Dados já vêm preenchidos do servidor
4. **Captura de Biometria** - Registra a digital do funcionário
5. **Sincronização** - Envia para o servidor web

---

## 📋 Como usar:

### **Passo 1: Abrir a Tela**
```csharp
// No seu FormPrincipal ou menu:
CadastroFuncionarioForm form = new CadastroFuncionarioForm();
form.ShowDialog();
```

### **Passo 2: Pesquisar Funcionário**
- Digite o nome ou matrícula no campo "Pesquisar Funcionário:"
- A lista filtra automaticamente
- Veja os resultados em tempo real

### **Passo 3: Selecionar Funcionário**
- Clique na pessoa na lista
- Os campos serão preenchidos automaticamente:
  - Nome Completo
  - Matrícula
  - E-mail
  - Cargo

### **Passo 4: Capturar Digital**
- Clique no botão **"👆 Capturar Digital"**
- Posicione o dedo no leitor
- Aguarde a confirmação

### **Passo 5: Salvar Cadastro**
- Clique em **"💾 Salvar Cadastro"**
- A biometria será enviada para o servidor
- Sucesso! ✓

---

## 🔧 Integração no Seu Código

### **1. Adicionar o FormControl**
Copie `CadastroFuncionarioForm.cs` para sua pasta `Forms/`

### **2. Adicionar ao Seu Menu Principal**
```csharp
private void MenuCadastroFuncionario_Click(object sender, EventArgs e)
{
    CadastroFuncionarioForm form = new CadastroFuncionarioForm();
    form.ShowDialog();
}
```

### **3. Dados que Vêm do Web**
- ✅ Nome completo
- ✅ Matrícula
- ✅ E-mail
- ✅ Cargo/Categoria

### **4. Dados Enviados para o Web**
- ✅ Biometria (digital capturada)
- ✅ Data/hora do registro
- ✅ IP da máquina

---

## 🔍 Recursos

| Recurso | Descrição |
|---------|-----------|
| **Pesquisa em Tempo Real** | Filtra conforme digita |
| **AutoComplete** | Sugestões de nomes |
| **Campos Protegidos** | Não pode editar dados do web |
| **Validação** | Verifica antes de enviar |
| **Feedback Visual** | Status em tempo real |

---

## ⚙️ Customizações Possíveis

### **Mudar URL do Servidor**
```csharp
private const string API_BASE_URL = "https://bypass-lime.vercel.app";
// Ou
private const string API_BASE_URL = "https://seu-dominio.com";
```

### **Mudar Cores dos Botões**
```csharp
buttonCapturarDigital.BackColor = System.Drawing.Color.Blue;
```

### **Mudar Tamanho da Janela**
```csharp
this.Size = new System.Drawing.Size(800, 900);
```

---

## 📊 Fluxo de Dados

```
┌─────────────────────────────┐
│   Tela Cadastro Funcionário │
└────────────┬────────────────┘
             │
             ├─ GET /api/users → Carrega lista
             │
             ├─ Pesquisa em Tempo Real
             │
             └─ POST /biometrics → Envia digital
                │
                ↓
        ┌──────────────────┐
        │  Servidor Vercel │
        │  (bypass-lime)   │
        └────────┬─────────┘
                 │
                 ↓
        ┌──────────────────┐
        │  Neon PostgreSQL │
        │  (Banco de Dados)│
        └──────────────────┘
```

---

## ✅ Verificação

Após implementar, verifique:
- ✓ A lista de funcionários carrega ao abrir
- ✓ A pesquisa filtra corretamente
- ✓ Os campos preenchem automaticamente
- ✓ A biometria é capturada
- ✓ O servidor recebe os dados

---

**Pronto para usar! 🚀**
