# 🔧 SOLUÇÃO: Leitor Biométrico Desconectado

## ❌ Problema Identificado

O leitor **U.are.U 4500** está conectado fisicamente, mas o sistema não o reconhece porque:

1. ✅ **Driver WBF (Windows Biometric Framework)** está ativo e tomou controle do leitor
2. ❌ **Driver DigitalPersona SDK** não consegue acessar o dispositivo
3. ⚠️ **Conflito de drivers**: Dois drivers tentando usar o mesmo hardware

### Status Atual:
```
- Leitor conectado: ✅ SIM
- DigitalPersona instalado: ✅ SIM (versão 1.6.1.965)
- Serviço WBF rodando: ⚠️ SIM (bloqueando o SDK)
- Driver WBF ativo: ⚠️ SIM
- Driver DigitalPersona: ❌ BLOQUEADO
```

---

## 🛠️ SOLUÇÃO RÁPIDA (Recomendada)

### Passo 1: Executar o Script de Correção

1. Localize o arquivo: **`fix_leitor_biometrico.bat`**
2. **Clique com botão direito** no arquivo
3. Selecione **"Executar como Administrador"**
4. Confirme clicando em qualquer tecla quando solicitado

### Passo 2: Reconectar o Leitor

1. **Desconecte o cabo USB** do leitor
2. Aguarde **5 segundos**
3. **Reconecte o cabo USB**
4. Aguarde o Windows instalar o driver

### Passo 3: Verificar

1. Execute seu sistema biométrico
2. Verifique se o status mudou de "Leitor desconectado" para "Leitor conectado"

---

## 📋 O Que o Script Faz?

O script **`fix_leitor_biometrico.bat`** realiza as seguintes ações:

1. **Para** o serviço Windows Biometric (WbioSrvc)
2. **Desabilita** o serviço para não iniciar automaticamente
3. **Desabilita** o driver WBF do leitor U.are.U
4. Libera o leitor para o SDK do DigitalPersona usar

---

## 🔄 Como Reverter as Alterações?

Se você precisar usar o Windows Hello ou outro sistema que dependa do WBF:

1. Execute o arquivo: **`reverter_para_wbf.bat`** (como Administrador)
2. Isso reabilitará o Windows Biometric Framework
3. Seu sistema biométrico **parará de funcionar** novamente

---

## ⚙️ SOLUÇÃO ALTERNATIVA (Manual)

Se preferir fazer manualmente, siga estes passos:

### 1. Abrir PowerShell como Administrador

Clique com botão direito no menu Iniciar → **Windows PowerShell (Admin)**

### 2. Parar e Desabilitar o Serviço WBF

```powershell
Stop-Service WbioSrvc
Set-Service WbioSrvc -StartupType Disabled
```

### 3. Desabilitar o Driver WBF

```powershell
Get-PnpDevice | Where-Object { $_.FriendlyName -like '*U.are.U*WBF*' } | Disable-PnpDevice -Confirm:$false
```

### 4. Reconectar o Leitor

1. Desconecte o cabo USB
2. Aguarde 5 segundos
3. Reconecte o cabo USB

---

## 🧪 VERIFICAÇÃO FINAL

Depois de aplicar a solução, execute no PowerShell:

```powershell
Get-PnpDevice | Where-Object { $_.FriendlyName -like '*U.are.U*' } | Select-Object Status, FriendlyName
```

**Resultado esperado:**
```
Status  FriendlyName
------  ------------
OK      Leitora de Impressão Digital, U.are.U® 4500
```

---

## ❓ FAQ - Perguntas Frequentes

### Por que isso acontece?

O Windows 10/11 inclui o **Windows Biometric Framework (WBF)** para Windows Hello. Quando instalado, ele assume controle exclusivo dos leitores biométricos, impedindo que aplicações antigas (como o SDK DigitalPersona) acessem o hardware.

### Isso vai afetar meu Windows Hello?

Sim. Se você usa **Windows Hello** (login com impressão digital no Windows), ele parará de funcionar. Para usar Windows Hello novamente, execute o script `reverter_para_wbf.bat`.

### Preciso fazer isso toda vez?

Não. Depois de desabilitar o WBF, ele permanecerá desabilitado até que você reverta as alterações.

### E se eu precisar usar ambos?

Infelizmente, não é possível usar o **Windows Hello (WBF)** e o **SDK DigitalPersona** ao mesmo tempo. Você precisa escolher um:

- **WBF**: Para Windows Hello e aplicações modernas
- **SDK DigitalPersona**: Para seu sistema de ponto biométrico

---

## 🆘 Suporte

Se o problema persistir após seguir todos os passos:

1. Verifique se executou o script como **Administrador**
2. Reinicie o computador
3. Verifique se o cabo USB está em uma porta USB funcionando
4. Tente outra porta USB (preferencialmente USB 2.0)
5. Verifique no Gerenciador de Dispositivos se há erros no leitor

---

## ✅ Checklist de Verificação

Antes de executar o script, confirme:

- [ ] Você tem permissões de **Administrador**
- [ ] O leitor está **conectado via USB**
- [ ] O DigitalPersona está **instalado** (versão 1.6.1.965 ou superior)
- [ ] Você **não precisa** usar Windows Hello temporariamente
- [ ] Você fez backup dos dados importantes (precaução)

---

**Data de criação:** 12 de janeiro de 2026  
**Sistema:** Windows 10/11  
**Leitor:** U.are.U 4500 Fingerprint Reader  
**SDK:** DigitalPersona One Touch SDK 1.6.1.965
