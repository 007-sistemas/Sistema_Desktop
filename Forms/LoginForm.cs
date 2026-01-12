using BiometricSystem.Database;
using BiometricSystem.Models;
using BiometricSystem.Services;
using System.Globalization;

namespace BiometricSystem.Forms
{
    public partial class LoginForm : Form
    {
        private readonly FingerprintService fingerprintService;
        private readonly DatabaseHelper database;
        private string? selectedSetor;
        private bool isCapturing = false;

        // Lista de setores disponíveis
        private readonly List<string> setores = new List<string>
        {
            "CENTRO CIRÚRGICO",
            "EMERGÊNCIA",
            "UTI",
            "ENFERMARIA",
            "LABORATÓRIO",
            "RADIOLOGIA",
            "FARMÁCIA",
            "RECEPÇÃO",
            "ADMINISTRATIVO"
        };

        public LoginForm()
        {
            InitializeComponent();
            fingerprintService = new FingerprintService();
            database = new DatabaseHelper();

            // Configurar eventos do serviço biométrico
            fingerprintService.OnStatusChanged += (sender, status) =>
            {
                if (InvokeRequired)
                {
                    Invoke(() => lblStatus.Text = status);
                }
                else
                {
                    lblStatus.Text = status;
                }
            };

            fingerprintService.OnFingerprintCaptured += OnFingerprintCaptured;
            
            // Carregar setores no ComboBox
            cmbSetor.Items.AddRange(setores.ToArray());
            cmbSetor.SelectedIndex = -1;
            
            // Inicializar leitor em segundo plano
            Task.Run(() =>
            {
                if (!fingerprintService.InitializeReader())
                {
                    Invoke(() => lblStatus.Text = "⚠️ Leitor não encontrado. Verifique a conexão.");
                }
                else
                {
                    Invoke(() => lblStatus.Text = "✅ Leitor pronto. Selecione o setor.");
                }
            });

            // Atualizar relógio
            UpdateClock();
        }

        private void UpdateClock()
        {
            lblTime.Text = DateTime.Now.ToString("HH:mm:ss");
            
            // Formatar data em português
            var culture = new CultureInfo("pt-BR");
            lblDate.Text = DateTime.Now.ToString("dddd, dd 'de' MMMM 'de' yyyy", culture);
        }

        private void timerClock_Tick(object sender, EventArgs e)
        {
            UpdateClock();
        }

        private async void cmbSetor_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbSetor.SelectedIndex == -1 || isCapturing)
                return;

            selectedSetor = cmbSetor.SelectedItem?.ToString();
            
            if (!string.IsNullOrEmpty(selectedSetor))
            {
                // Desabilitar combo durante captura
                cmbSetor.Enabled = false;
                btnRegister.Enabled = false;
                isCapturing = true;

                lblStatus.Text = $"⏳ Setor: {selectedSetor} - Posicione o dedo no leitor...";
                
                // Animar ícone de digital
                panelFingerprint.BackColor = System.Drawing.Color.FromArgb(230, 240, 255);
                
                // Iniciar captura automática
                await fingerprintService.StartCapture();

                // Reabilitar após captura
                cmbSetor.Enabled = true;
                btnRegister.Enabled = true;
                isCapturing = false;
                panelFingerprint.BackColor = System.Drawing.Color.White;
            }
        }

        private void OnFingerprintCaptured(object? sender, byte[] template)
        {
            lblStatus.Text = "⏳ Verificando digital no banco de dados...";
            Refresh(); // Atualizar a interface imediatamente
            
            // Buscar todos os funcionários no banco de dados
            var employees = database.GetAllEmployees();
            Employee? matchedEmployee = null;

            // Verificar contra cada funcionário usando o verificador nativo do SDK
            foreach (var employee in employees)
            {
                if (employee.FingerprintTemplate != null && employee.FingerprintTemplate.Length > 0)
                {
                    if (fingerprintService.VerifyAgainstTemplate(employee.FingerprintTemplate))
                    {
                        matchedEmployee = employee;
                        break;
                    }
                }
            }

            // Limpar features capturadas após verificação completa
            fingerprintService.ClearCapturedFeatures();

            if (matchedEmployee != null)
            {
                // Buscar o ÚLTIMO registro do funcionário
                var lastRecord = database.GetEmployeeTimeRecords(matchedEmployee.Id).FirstOrDefault();
                string recordType = (lastRecord == null || lastRecord.Type == "Saída") ? "Entrada" : "Saída";

                var timeRecord = new TimeRecord
                {
                    EmployeeId = matchedEmployee.Id,
                    Type = recordType,
                    Timestamp = DateTime.Now,
                    Notes = $"Setor: {selectedSetor ?? "N/A"}"
                };

                if (database.RegisterTimeRecord(timeRecord))
                {
                    MessageBox.Show(
                        $"✅ Ponto registrado com sucesso!\n\n" +
                        $"Funcionário: {matchedEmployee.Name}\n" +
                        $"Setor: {selectedSetor}\n" +
                        $"Tipo: {recordType}\n" +
                        $"Horário: {timeRecord.Timestamp:HH:mm:ss}",
                        "Sucesso",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);

                    lblStatus.Text = $"✅ {recordType} registrada - {matchedEmployee.Name}";
                    
                    // Resetar seleção do setor
                    cmbSetor.SelectedIndex = -1;
                }
                else
                {
                    MessageBox.Show(
                        "❌ Erro ao registrar ponto no banco de dados!",
                        "Erro",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    lblStatus.Text = "❌ Erro ao registrar ponto";
                }
            }
            else
            {
                MessageBox.Show(
                    "Digital não reconhecida!\n\nFuncionário não cadastrado no sistema.",
                    "Atenção",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                lblStatus.Text = "❌ Digital não reconhecida";
                cmbSetor.SelectedIndex = -1;
            }
        }

        private void btnRegister_Click(object sender, EventArgs e)
        {
            var registerForm = new RegisterForm();
            registerForm.ShowDialog();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            fingerprintService.Dispose();
            base.OnFormClosing(e);
        }
    }
}
