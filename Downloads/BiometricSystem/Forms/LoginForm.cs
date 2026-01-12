using BiometricSystem.Database;
using BiometricSystem.Models;
using BiometricSystem.Services;

namespace BiometricSystem.Forms
{
    public partial class LoginForm : Form
    {
        private readonly FingerprintService fingerprintService;
        private readonly DatabaseHelper database;

        public LoginForm()
        {
            InitializeComponent();
            fingerprintService = new FingerprintService();
            database = new DatabaseHelper();

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
            
            // Inicializar leitor
            if (!fingerprintService.InitializeReader())
            {
                lblStatus.Text = "⚠️ Leitor não encontrado. Verifique a conexão.";
            }
            else
            {
                lblStatus.Text = "✅ Leitor conectado. Pronto para uso.";
            }
        }

        private async void btnBiometricLogin_Click(object sender, EventArgs e)
        {
            btnBiometricLogin.Enabled = false;
            btnRegister.Enabled = false;
            lblStatus.Text = "⏳ Posicione o dedo no leitor...";

            // Não usar enrollment mode para login - apenas capturar uma vez
            await fingerprintService.StartCapture();

            btnBiometricLogin.Enabled = true;
            btnRegister.Enabled = true;
        }

        private void OnFingerprintCaptured(object? sender, byte[] template)
        {
            // Buscar todos os funcionários no banco de dados
            var employees = database.GetAllEmployees();
            Employee? matchedEmployee = null;

            // Verificar contra cada funcionário usando o verificador nativo do SDK
            // IMPORTANTE: As features são reutilizadas em cada loop
            foreach (var employee in employees)
            {
                if (employee.FingerprintTemplate != null && employee.FingerprintTemplate.Length > 0)
                {
                    // Usar o verificador nativo do SDK
                    if (fingerprintService.VerifyAgainstTemplate(employee.FingerprintTemplate))
                    {
                        matchedEmployee = employee;
                        break; // Parar na primeira correspondência
                    }
                }
            }

            // Limpar features capturadas após verificação completa
            fingerprintService.ClearCapturedFeatures();

            if (matchedEmployee != null)
            {
                // Funcionário encontrado - registrar ponto
                // Buscar o ÚLTIMO registro do funcionário (em qualquer data)
                var lastRecord = database.GetEmployeeTimeRecords(matchedEmployee.Id)
                    .FirstOrDefault();

                string recordType = (lastRecord == null || lastRecord.Type == "Saída") ? "Entrada" : "Saída";

                var timeRecord = new TimeRecord
                {
                    EmployeeId = matchedEmployee.Id,
                    Type = recordType,
                    Timestamp = DateTime.Now
                };

                if (database.RegisterTimeRecord(timeRecord))
                {
                    MessageBox.Show(
                        $"✅ Ponto registrado com sucesso!\n\n" +
                        $"Funcionário: {matchedEmployee.Name}\n" +
                        $"Tipo: {recordType}\n" +
                        $"Horário: {timeRecord.Timestamp:HH:mm:ss}",
                        "Sucesso",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);

                    lblStatus.Text = $"✅ {recordType} registrada - {matchedEmployee.Name}";
                }
                else
                {
                    MessageBox.Show(
                        "❌ Erro ao registrar ponto no banco de dados!\n\nTente novamente.",
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
            }
        }

        private void btnRegister_Click(object sender, EventArgs e)
        {
            var registerForm = new RegisterForm();
            registerForm.ShowDialog();
        }

        private void Button_MouseEnter(object? sender, EventArgs e)
        {
            if (sender is Button btn)
            {
                btn.BackColor = Color.FromArgb(29, 78, 188);
            }
        }

        private void Button_MouseLeave(object? sender, EventArgs e)
        {
            if (sender is Button btn)
            {
                btn.BackColor = Color.FromArgb(37, 99, 235);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            fingerprintService.Dispose();
            base.OnFormClosing(e);
        }
    }
}
