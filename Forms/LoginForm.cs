using BiometricSystem.Database;
using BiometricSystem.Models;
using BiometricSystem.Services;
using System.Globalization;
using System.Drawing.Drawing2D;
using Microsoft.Extensions.Configuration;

namespace BiometricSystem.Forms
{
    public partial class LoginForm : Form
    {
        private readonly FingerprintService fingerprintService;
        private readonly DatabaseHelper database;
        private readonly SyncService? syncService;
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

        public LoginForm(IConfiguration? config = null)
        {
            InitializeComponent();
            fingerprintService = new FingerprintService();
            database = new DatabaseHelper();

            // Inicializar sincronização com Neon se configuração disponível
            if (config != null)
            {
                var neonConnectionString = config.GetConnectionString("DefaultConnection") 
                    ?? config["Neon:ConnectionString"];
                
                if (!string.IsNullOrEmpty(neonConnectionString))
                {
                    syncService = new SyncService(database, neonConnectionString);
                    syncService.StartAutoSync(intervalSeconds: 30); // Sincronizar a cada 30s
                }
            }

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
            
            // Centralizar controles ao carregar
            CentralizarControles();
            
            // Aplicar bordas arredondadas
            AplicarBordasArredondadas();
        }

        private void AplicarBordasArredondadas()
        {
            // Arredondar header
            panelHeader.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = GetRoundedRectangle(panelHeader.ClientRectangle, 20))
                {
                    panelHeader.Region = new Region(path);
                }
            };
            
            // Arredondar combobox
            cmbSetor.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            };
            
            // Arredondar painel simulador
            panelSimulador.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = GetRoundedRectangle(panelSimulador.ClientRectangle, 15))
                {
                    panelSimulador.Region = new Region(path);
                }
            };
            
            // Arredondar botão
            btnRegister.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = GetRoundedRectangle(btnRegister.ClientRectangle, 10))
                {
                    btnRegister.Region = new Region(path);
                }
            };
        }

        private GraphicsPath GetRoundedRectangle(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int diameter = radius * 2;
            
            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            
            return path;
        }

        private void CentralizarControles()
        {
            int centerX = this.ClientSize.Width / 2;
            
            // Centralizar header
            panelHeader.Left = centerX - (panelHeader.Width / 2);
            
            // Centralizar labels e combobox
            lblLocalProducao.Left = centerX - 350;
            lblSetorAla.Left = centerX - 350;
            cmbSetor.Left = centerX - 350;
            cmbSetor.Width = 700;
            
            // Centralizar instrução
            lblInstrucao.Left = centerX - 350;
            lblInstrucao.Width = 700;
            
            // Centralizar painel simulador
            panelSimulador.Left = centerX - (panelSimulador.Width / 2);
            
            // Centralizar status
            lblStatus.Left = centerX - 350;
            lblStatus.Width = 700;
            
            // Centralizar botão
            btnRegister.Left = centerX - (btnRegister.Width / 2);
        }

        private void LoginForm_Resize(object sender, EventArgs e)
        {
            CentralizarControles();
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
                    // Exibir informações no painel ao invés de MessageBox
                    ExibirRegistroPonto(matchedEmployee, recordType, timeRecord.Timestamp);
                    
                    // Resetar seleção do setor
                    cmbSetor.SelectedIndex = -1;
                }
                else
                {
                    lblStatus.Text = "❌ Erro ao registrar ponto";
                    panelSimulador.BackColor = System.Drawing.Color.FromArgb(255, 230, 230);
                    lblSimulador.Text = "Erro ao registrar ponto no banco de dados!";
                    lblSimulador.ForeColor = System.Drawing.Color.FromArgb(180, 0, 0);
                }
            }
            else
            {
                lblStatus.Text = "❌ Digital não reconhecida";
                panelSimulador.BackColor = System.Drawing.Color.FromArgb(255, 245, 230);
                lblSimulador.Text = "Digital não reconhecida!\n\nFuncionário não cadastrado no sistema.";
                lblSimulador.ForeColor = System.Drawing.Color.FromArgb(200, 100, 0);
                cmbSetor.SelectedIndex = -1;
            }
        }

        private void ExibirRegistroPonto(Employee employee, string tipo, DateTime horario)
        {
            // Definir cores conforme o tipo
            Color backgroundColor;
            Color textColor;
            string emoji;
            
            if (tipo == "Entrada")
            {
                backgroundColor = System.Drawing.Color.FromArgb(230, 255, 240); // Verde claro
                textColor = System.Drawing.Color.FromArgb(0, 120, 60);
                emoji = "➜";
            }
            else
            {
                backgroundColor = System.Drawing.Color.FromArgb(255, 235, 235); // Vermelho claro
                textColor = System.Drawing.Color.FromArgb(180, 30, 30);
                emoji = "⬅";
            }
            
            panelSimulador.BackColor = backgroundColor;
            
            // Montar texto formatado
            string textoExibicao = $"{emoji}  {tipo.ToUpper()} REGISTRADA\n\n";
            textoExibicao += $"{employee.Name}\n";
            textoExibicao += $"{employee.Position ?? "Funcionário"}\n\n";
            textoExibicao += $"📍 {selectedSetor}\n";
            textoExibicao += $"🕐 {horario:HH:mm:ss}";
            
            lblSimulador.Text = textoExibicao;
            lblSimulador.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold);
            lblSimulador.ForeColor = textColor;
            lblSimulador.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            
            lblStatus.Text = $"✅ {tipo} registrada - {employee.Name}";
            
            // Limpar painel após 5 segundos
            var timer = new System.Windows.Forms.Timer();
            timer.Interval = 5000;
            timer.Tick += (s, e) => { if (InvokeRequired) { Invoke(() => { panelSimulador.BackColor = System.Drawing.Color.White; lblSimulador.Text = ""; lblSimulador.Font = new System.Drawing.Font("Segoe UI", 12F); lblSimulador.TextAlign = System.Drawing.ContentAlignment.TopLeft; lblStatus.Text = "Selecione o setor para ativar o leitor"; }); } else { panelSimulador.BackColor = System.Drawing.Color.White; lblSimulador.Text = ""; lblSimulador.Font = new System.Drawing.Font("Segoe UI", 12F); lblSimulador.TextAlign = System.Drawing.ContentAlignment.TopLeft; lblStatus.Text = "Selecione o setor para ativar o leitor"; } timer.Stop(); timer.Dispose(); };
            timer.Start();
        }

        private void btnRegister_Click(object sender, EventArgs e)
        {
            var registerForm = new RegisterForm();
            registerForm.ShowDialog();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            syncService?.StopAutoSync();
            fingerprintService.Dispose();
            base.OnFormClosing(e);
        }
    }
}

