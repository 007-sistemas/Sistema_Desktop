# 📦 Guia de Instalação - Sistema Biométrico

## ✅ Como criar um instalador profissional (.MSI)

### **Opção 1: Instalador Automático (Recomendado)**

#### Pré-requisitos:
- Visual Studio 2022 com "Microsoft Visual Studio Installer Projects" instalado

#### Passos:

1. **Instale a extensão de Installer Projects:**
   - Abra Visual Studio 2022
   - Vá em: `Extensions` → `Manage Extensions`
   - Procure por: `Microsoft Visual Studio Installer Projects`
   - Clique em `Download` e instale

2. **Crie um novo projeto Setup:**
   - Clique em `File` → `New` → `Project`
   - Procure por: `Setup Project`
   - Dê o nome: `BiometricSystemSetup`

3. **Configure o projeto:**
   - Clique com botão direito em `BiometricSystemSetup`
   - Selecione `Project Output`
   - Selecione `BiometricSystem` e clique em `Primary output`

4. **Adicione a pasta SDK:**
   - Clique com botão direito em `BiometricSystemSetup`
   - Selecione `File System`
   - Clique com botão direito em `Application Folder`
   - Selecione `Add` → `Folder`
   - Nomeie como `SDK`
   - Arraste os arquivos DLL da pasta `SDK\` do projeto principal

5. **Build do Instalador:**
   - Clique com botão direito no projeto `BiometricSystemSetup`
   - Selecione `Build`
   - O arquivo `.msi` será gerado em: `BiometricSystemSetup\Release\`

---

### **Opção 2: Usando WiX Toolset (Profissional)**

1. **Instale o WiX Toolset:**
   - Acesse: https://github.com/wixtoolset/wix3/releases/
   - Baixe a versão mais recente
   - Execute o instalador

2. **Execute o script:**
   ```bash
   .\criar-instalador.bat
   ```

---

### **Opção 3: Distribuição Portável (Mais Simples)**

Se não quiser criar um instalador, simplesmente:

1. Copie a pasta: `bin\publish\`
2. Renomeie para: `BiometricSystem`
3. Comprima em `.ZIP` ou `.RAR`
4. Distribua para os usuários
5. Eles descompactam e executam `BiometricSystem.exe`

---

## 🗄️ Como o banco de dados funciona no outro PC

**Importante:** O banco de dados é criado **automaticamente** na primeira execução!

### Localização do banco:
- **Local exato:** Mesma pasta onde `BiometricSystem.exe` está localizado
- **Nome:** `biometric.db`
- **Tamanho inicial:** ~60KB

### Exemplo:
```
Se instalado em: C:\Program Files\BiometricSystem\
O banco estará em: C:\Program Files\BiometricSystem\biometric.db
```

### Fluxo automático:
1. Usuário executa `BiometricSystem.exe`
2. App detecta que não há `biometric.db`
3. Cria automaticamente as tabelas:
   - `Employees` (funcionários cadastrados)
   - `TimeRecords` (pontos batidos)
4. Sistema pronto para usar!

---

## 📋 Pré-requisitos no outro PC

Para o executável funcionar corretamente, o outro PC precisa ter:

| Requisito | Status |
|-----------|--------|
| Windows 10/11 64-bit | ✅ Necessário |
| .NET 8 Runtime | ✅ Incluído no instalador |
| Driver DigitalPersona | ✅ Necessário (deve instalar antes) |
| Leitor biométrico DP4500 | ✅ Hardware |

### Instalando o Driver DigitalPersona:
1. Visite: https://www.crossmatch.com/
2. Baixe o driver para "DigitalPersona U.are.U 4500"
3. Execute o instalador
4. Reinicie o PC
5. Conecte o leitor biométrico USB
6. Execute o `BiometricSystem.exe`

---

## 🚀 Checklist de Distribuição

- [ ] Criar o arquivo `.msi` (Opção 1 ou 2)
- [ ] Testar o instalador em outro PC
- [ ] Verificar se o banco de dados foi criado
- [ ] Testar cadastro de funcionário
- [ ] Testar registro de ponto
- [ ] Distribuir para usuários

---

## ❓ Dúvidas Frequentes

**P: O banco de dados será perdido se desinstalar?**
R: Sim, a desinstalação remove a pasta. Para preservar, faça backup de `biometric.db` antes.

**P: Posso usar o mesmo banco em vários PCs?**
R: Não recomendado. Cada PC deve ter seu próprio banco. Para sincronizar, use a API de sync (funcionalidade futura).

**P: Qual é o tamanho total do instalador?**
R: Aproximadamente 200MB (inclui .NET 8 Runtime)

---

## 📞 Suporte

Para dúvidas sobre a instalação, consulte este guia ou reinstale o sistema.
