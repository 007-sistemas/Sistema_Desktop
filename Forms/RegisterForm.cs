using BiometricSystem.Database;
using BiometricSystem.Models;
using BiometricSystem.Services;

namespace BiometricSystem.Forms
{
    public partial class RegisterForm : Form
    {
        private readonly FingerprintService fingerprintService;
        private readonly DatabaseHelper database;
        private byte[]? capturedFingerprint;

        public RegisterForm()
        {
            InitializeComponent();
            fingerprintService = new FingerprintService();
            database = new DatabaseHelper();

            fingerprintService.OnStatusChanged += (sender, status) =>
            {
                if (InvokeRequired)
                {
                    Invoke(() => lblFingerprintStatus.Text = status);
                }
                else
                {
                    lblFingerprintStatus.Text = status;
                }
            };

            fingerprintService.OnFingerprintCaptured += OnFingerprintCaptured;

            if (!fingerprintService.InitializeReader())
            {
                lblFingerprintStatus.Text = "⚠️ Leitor não encontrado";
                btnCaptureFingerprint.Enabled = false;
            }
        }

        private async void btnCaptureFingerprint_Click(object sender, EventArgs e)
        {
            btnCaptureFingerprint.Enabled = false;
            btnSave.Enabled = false;
            lblFingerprintStatus.Text = "🔄 Iniciando captura da digital...";
            lblFingerprintStatus.ForeColor = Color.Blue;

            // Iniciar modo de enrollment
            fingerprintService.StartEnrollment();

            // Capturar múltiplas vezes até que o enrollment esteja completo
            bool enrollmentComplete = false;
            int attempts = 0;
            const int maxAttempts = 10; // Máximo de tentativas

            while (!enrollmentComplete && attempts < maxAttempts)
            {
                lblFingerprintStatus.Text = $"⏳ Posicione o dedo no leitor...";
                await fingerprintService.StartCapture();
                
                attempts++;

                // Aguardar um pouco antes da próxima captura
                await Task.Delay(800);

                // Verificar se o enrollment já está completo
                // Isso será detectado quando OnFingerprintCaptured for chamado
                if (capturedFingerprint != null)
                {
                    enrollmentComplete = true;
                    lblFingerprintStatus.ForeColor = Color.Green;
                }
            }

            if (!enrollmentComplete && capturedFingerprint == null)
            {
                lblFingerprintStatus.Text = "⚠️ Falha na captura. Tente novamente.";
                lblFingerprintStatus.ForeColor = Color.Red;
            }

            fingerprintService.StopEnrollment();
            btnCaptureFingerprint.Enabled = true;
        }

        private void OnFingerprintCaptured(object? sender, byte[] template)
        {
            capturedFingerprint = template;

            if (InvokeRequired)
            {
                Invoke(() =>
                {
                    lblFingerprintStatus.Text = "✅ Digital capturada com sucesso!";
                    lblFingerprintStatus.ForeColor = Color.Green;
                    btnSave.Enabled = true;
                    panelFingerprint.Visible = true;
                    panelFingerprint.BackColor = Color.FromArgb(34, 197, 94);
                });
            }
            else
            {
                lblFingerprintStatus.Text = "✅ Digital capturada com sucesso!";
                lblFingerprintStatus.ForeColor = Color.Green;
                btnSave.Enabled = true;
                panelFingerprint.Visible = true;
                panelFingerprint.BackColor = Color.FromArgb(34, 197, 94);
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            // Validações
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("Por favor, informe o nome do funcionário.", "Atenção", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtName.Focus();
                return;
            }

            // Limpar CPF (remover pontos, hífens e vírgulas)
            string cleanedCPF = txtCPF.Text.Replace(".", "").Replace("-", "").Replace(",", "").Trim();
            
            if (string.IsNullOrWhiteSpace(cleanedCPF) || cleanedCPF.Length != 11 || !cleanedCPF.All(char.IsDigit))
            {
                MessageBox.Show("Por favor, informe um CPF válido (11 dígitos).", "Atenção", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtCPF.Focus();
                return;
            }

            if (capturedFingerprint == null)
            {
                MessageBox.Show("Por favor, capture a digital do funcionário.", "Atenção", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Criar novo funcionário
            var employee = new Employee
            {
                Name = txtName.Text.Trim(),
                CPF = cleanedCPF,
                Email = txtEmail.Text.Trim(),
                Position = txtPosition.Text.Trim(),
                FingerprintTemplate = capturedFingerprint,
                CreatedAt = DateTime.Now,
                IsActive = true
            };

            // Salvar no banco
            if (database.AddEmployee(employee))
            {
                MessageBox.Show(
                    $"Funcionário cadastrado com sucesso!\n\n" +
                    $"Nome: {employee.Name}\n" +
                    $"CPF: {cleanedCPF}\n" +
                    $"Cargo: {employee.Position}",
                    "Sucesso",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                MessageBox.Show(
                    "Erro ao cadastrar funcionário!\n\n" +
                    "Verifique se o CPF já não está cadastrado.",
                    "Erro",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            fingerprintService.Dispose();
            base.OnFormClosing(e);
        }
    }
}
