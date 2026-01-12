using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using BiometricSystem.Services;

namespace BiometricSystem
{
    /// <summary>
    /// INTEGRAÇÃO COMPLETA COM SISTEMA WEB
    /// 
    /// Este arquivo contém a configuração e os métodos para integrar
    /// o Sistema Desktop com https://bypass-lime.vercel.app
    /// 
    /// Adicione este código ao seu DashboardForm.cs
    /// </summary>
    public partial class DashboardFormBiometricIntegration
    {
        private FingerprintService _fingerprintService;
        private List<UserDto> _loadedUsers = new List<UserDto>();
        private const string API_BASE_URL = "https://bypass-lime.vercel.app";

        /// <summary>
        /// Chamar no Form_Load ou no construtor
        /// </summary>
        private async void InitializeBiometricSystem()
        {
            try
            {
                // Criar fingerprint service
                _fingerprintService = new FingerprintService();

                // Inscrever em eventos
                _fingerprintService.OnStatusChanged += (sender, message) =>
                {
                    UpdateStatusLabel(message);
                };

                // Inicializar API
                _fingerprintService.InitializeApiService(API_BASE_URL);

                // Verificar conexão
                bool isConnected = await CheckServerConnection();

                if (isConnected)
                {
                    SetServerStatus("🟢 ONLINE", System.Drawing.Color.Green);
                    await LoadUsersAsync();
                }
                else
                {
                    SetServerStatus("🔴 OFFLINE", System.Drawing.Color.Red);
                    MessageBox.Show(
                        "⚠ Servidor web não está disponível.\n\n" +
                        "Verifique:\n" +
                        "• Conexão com internet\n" +
                        "• Se bypass-lime.vercel.app está online\n" +
                        "• Firewall/Proxy settings",
                        "Erro de Conexão",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao inicializar: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Verifica conexão com servidor
        /// </summary>
        private async Task<bool> CheckServerConnection()
        {
            try
            {
                return await _fingerprintService.CheckWebServerAvailabilityAsync();
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Carregar lista de usuários do servidor
        /// </summary>
        private async Task LoadUsersAsync()
        {
            try
            {
                UpdateStatusLabel("📥 Carregando usuários do servidor...");

                var users = await _fingerprintService.GetUsersFromWebAsync();

                if (users != null && users.Count > 0)
                {
                    _loadedUsers = users;

                    // Atualizar ComboBox
                    comboBoxUsers.Items.Clear();

                    foreach (var user in users)
                    {
                        comboBoxUsers.Items.Add(new
                        {
                            id = user.Id,
                            displayText = $"{user.Name} (Mat: {user.Matricula}) - {user.Categoria}"
                        });
                    }

                    comboBoxUsers.DisplayMember = "displayText";
                    comboBoxUsers.ValueMember = "id";

                    UpdateStatusLabel($"✓ {users.Count} usuários carregados com sucesso");
                }
                else
                {
                    UpdateStatusLabel("⚠ Nenhum usuário encontrado");
                }
            }
            catch (Exception ex)
            {
                UpdateStatusLabel($"❌ Erro ao carregar usuários: {ex.Message}");
            }
        }

        /// <summary>
        /// Registrar biometria do usuário selecionado
        /// </summary>
        private async void ButtonRegisterBiometric_Click(object sender, EventArgs e)
        {
            try
            {
                if (comboBoxUsers.SelectedIndex == -1)
                {
                    MessageBox.Show("❌ Selecione um usuário primeiro!", "Aviso");
                    return;
                }

                int userId = (int)((dynamic)comboBoxUsers.SelectedItem).id;
                string userName = ((dynamic)comboBoxUsers.SelectedItem).displayText;

                UpdateStatusLabel("📤 Registrando biometria no servidor...");

                bool success = await _fingerprintService.RegisterBiometricOnWebAsync(userId);

                if (success)
                {
                    MessageBox.Show(
                        $"✓ BIOMETRIA REGISTRADA\n\n" +
                        $"Usuário: {userName}",
                        "Sucesso",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );

                    // Recarregar lista
                    await LoadUsersAsync();
                }
                else
                {
                    MessageBox.Show("❌ Falha ao registrar biometria no servidor", "Erro");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Erro: {ex.Message}", "Erro");
            }
        }

        /// <summary>
        /// Registrar entrada do usuário
        /// </summary>
        private async void ButtonCheckIn_Click(object sender, EventArgs e)
        {
            try
            {
                if (comboBoxUsers.SelectedIndex == -1)
                {
                    MessageBox.Show("❌ Selecione um usuário primeiro!", "Aviso");
                    return;
                }

                int userId = (int)((dynamic)comboBoxUsers.SelectedItem).id;
                string userName = ((dynamic)comboBoxUsers.SelectedItem).displayText;

                UpdateStatusLabel("🔍 Verificando biometria e registrando entrada...");

                var result = await _fingerprintService.RegisterTimeRecordOnWebAsync(userId, "entrada");

                if (result != null && result.VerificationSuccess)
                {
                    MessageBox.Show(
                        $"✓ ENTRADA REGISTRADA COM SUCESSO\n\n" +
                        $"Usuário: {userName}\n" +
                        $"Horário: {result.RecordedAt:HH:mm:ss}\n" +
                        $"Data: {result.RecordedAt:dd/MM/yyyy}",
                        "Entrada Registrada",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                }
                else if (result != null)
                {
                    MessageBox.Show(
                        $"⚠ {result.Message}\n\n" +
                        $"Usuário: {userName}",
                        "Aviso",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Erro: {ex.Message}", "Erro");
            }
        }

        /// <summary>
        /// Registrar saída do usuário
        /// </summary>
        private async void ButtonCheckOut_Click(object sender, EventArgs e)
        {
            try
            {
                if (comboBoxUsers.SelectedIndex == -1)
                {
                    MessageBox.Show("❌ Selecione um usuário primeiro!", "Aviso");
                    return;
                }

                int userId = (int)((dynamic)comboBoxUsers.SelectedItem).id;
                string userName = ((dynamic)comboBoxUsers.SelectedItem).displayText;

                UpdateStatusLabel("🔍 Verificando biometria e registrando saída...");

                var result = await _fingerprintService.RegisterTimeRecordOnWebAsync(userId, "saida");

                if (result != null && result.VerificationSuccess)
                {
                    MessageBox.Show(
                        $"✓ SAÍDA REGISTRADA COM SUCESSO\n\n" +
                        $"Usuário: {userName}\n" +
                        $"Horário: {result.RecordedAt:HH:mm:ss}\n" +
                        $"Data: {result.RecordedAt:dd/MM/yyyy}",
                        "Saída Registrada",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                }
                else if (result != null)
                {
                    MessageBox.Show(
                        $"⚠ {result.Message}\n\n" +
                        $"Usuário: {userName}",
                        "Aviso",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Erro: {ex.Message}", "Erro");
            }
        }

        /// <summary>
        /// Sincronizar usuários com servidor
        /// </summary>
        private async void ButtonSyncUsers_Click(object sender, EventArgs e)
        {
            await LoadUsersAsync();
        }

        /// <summary>
        /// Auxiliar: Atualizar label de status
        /// </summary>
        private void UpdateStatusLabel(string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => UpdateStatusLabel(message)));
                return;
            }

            labelStatus.Text = message;
            Application.DoEvents();
        }

        /// <summary>
        /// Auxiliar: Definir status do servidor
        /// </summary>
        private void SetServerStatus(string text, System.Drawing.Color color)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => SetServerStatus(text, color)));
                return;
            }

            labelServerStatus.Text = text;
            labelServerStatus.ForeColor = color;
        }
    }

    /*
    ================================================
    CONTROLES A ADICIONAR NO DESIGNER DO FORM
    ================================================
    
    ComboBox comboBoxUsers
    - Name: comboBoxUsers
    - DisplayMember: displayText
    - ValueMember: id
    
    Label labelStatus
    - Name: labelStatus
    - Font: 10pt
    - AutoSize: True
    - Text: "Inicializando..."
    
    Label labelServerStatus
    - Name: labelServerStatus
    - Font: 12pt, Bold
    - AutoSize: True
    - ForeColor: Green
    - Text: "🟢 ONLINE"
    
    Button buttonRegisterBiometric
    - Name: buttonRegisterBiometric
    - Text: "Registrar Biometria"
    - Click: ButtonRegisterBiometric_Click
    
    Button buttonCheckIn
    - Name: buttonCheckIn
    - Text: "Registrar Entrada"
    - Click: ButtonCheckIn_Click
    
    Button buttonCheckOut
    - Name: buttonCheckOut
    - Text: "Registrar Saída"
    - Click: ButtonCheckOut_Click
    
    Button buttonSyncUsers
    - Name: buttonSyncUsers
    - Text: "Sincronizar Usuários"
    - Click: ButtonSyncUsers_Click
    
    ================================================
    */
}
