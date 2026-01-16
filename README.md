# 🎯 Sistema de Ponto Biométrico

## ✅ Executar

```
C:\Users\seu usuario\Downloads\BiometricSystem\bin\publish\BiometricSystem.exe
```

## 📋 Funcionalidades

- ✅ Cadastro de cooperados com biometria (DigitalPersona 4500 U.are)
- ✅ Registro de ponto (Entrada/Saída)
- ✅ Banco SQLite local (`biometric.db`)
- ✅ Sincronização com NEON (PostgreSQL)
- ✅ **Novo:** Cadastro de Biometria com sincronização de cooperados do NEON

## 📊 Banco de Dados

### SQLite Local
Localizado em: `bin/publish/biometric.db`

**Tabelas:**
- `Employees` - Cooperados cadastrados
- `TimeRecords` - Registros de ponto

### NEON (PostgreSQL Cloud)
Conexão configurada para sincronizar com NEON

**Tabelas principais:**
- `cooperados` - Lista de profissionais cadastrados
- `biometrias` - Armazenamento de digitais capturadas
- `pontos` - Registros de ponto sincronizados

## 🆕 Nova Funcionalidade: Cadastrar Biometria

### Como usar:
1. Clique no botão **"👆 Cadastrar Biometria"** na tela principal
2. A lista de cooperados será carregada automaticamente do NEON
3. Selecione o cooperado na lista suspensa
4. Posicione o dedo no leitor biométrico
5. Clique em **"☝️ Capturar Digital"**
6. Salve a biometria clicando em **"💾 Salvar Biometria"**

### Arquivos implementados:
- `Database/NeonCooperadoHelper.cs` - Consulta cooperados do NEON
- `Forms/CadastrarBiometriaForm.cs` - Interface de cadastro
- `Forms/LoginForm.Designer.cs` - Botão integrado na tela principal

### Documentação completa:
Veja [GUIA_CADASTRAR_BIOMETRIA.md](GUIA_CADASTRAR_BIOMETRIA.md)

## 🔧 Desenvolvido em

- C# .NET 8.0
- Windows Forms
- SQLite (local)
- PostgreSQL com NEON (nuvem)
- Npgsql (driver PostgreSQL)
- DigitalPersona SDK
