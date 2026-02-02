using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using BiometricSystem.Database;
using BiometricSystem.Services;
using static BiometricSystem.Database.NeonCooperadoHelper;

namespace BiometricSystem.Forms
{
    /// <summary>
    /// Tela de Cadastro de Biometria com Sincronização de Cooperados do NEON
    /// </summary>
    public partial class CadastrarBiometriaForm : Form
    {
        private NeonCooperadoHelper _neonCooperadoHelper;
        private FingerprintService _fingerprintService;
        private List<Cooperado> _cooperados = new List<Cooperado>();
        private Cooperado _cooperadoSelecionado;
        private string _connectionString;

        // Controles
        private ComboBox comboBoxCooperados;
        private Label labelCooperadoSelecionado;
        private TextBox textBoxCPF;
        private TextBox textBoxEmail;
        private TextBox textBoxTelefone;
        private Button buttonCapturarDigital;
        private Button buttonSalvar;
        private Button buttonCancelar;
        private Button buttonRecarregar;
        private Button buttonLimparBiometria;
        private Label labelStatus;
        private Label labelLeitorDetectado;
        private Label labelBiometriaStatus;
        private PictureBox pictureBoxFingerprintIcon;
        private byte[] _biometriaCapturada;

        public CadastrarBiometriaForm(string connectionString)
        {
            _connectionString = connectionString;
            _fingerprintService = new FingerprintService();

            // Inicializar neonHelper no construtor
            _neonCooperadoHelper = new NeonCooperadoHelper(connectionString);

            // Configurar eventos do serviço biométrico
            _fingerprintService.OnFingerprintCaptured += OnFingerprintCaptured;
            _fingerprintService.OnStatusChanged += (sender, status) =>
            {
                if (InvokeRequired)
                {
                    Invoke(() => labelStatus.Text = status);
                }
                else
                {
                    labelStatus.Text = status;
                }
            };

            InitializeComponent();
            InitializeControls();

            // Sempre em primeiro plano
            this.TopMost = true;

            // Inicializar leitor em segundo plano
            Task.Run(() =>
            {
                if (_fingerprintService.InitializeReader())
                {
                    Invoke(() => labelLeitorDetectado.Text = "✅ Leitor detectado");
                }
                else
                {
                    Invoke(() => labelLeitorDetectado.Text = "⚠️ Leitor não encontrado");
                }
            });
        }

        private void InitializeControls()
        {
            // Configurar Form
            this.Text = "Cadastrar Biometria";
            this.Size = new System.Drawing.Size(600, 750);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = System.Drawing.Color.FromArgb(245, 246, 248);

            // Label Título
            Label labelTitulo = new Label();
            labelTitulo.Text = "Cadastrar Biometria";
            labelTitulo.Font = new System.Drawing.Font("Arial", 18, System.Drawing.FontStyle.Bold);
            labelTitulo.Location = new System.Drawing.Point(20, 20);
            labelTitulo.Size = new System.Drawing.Size(300, 40);
            labelTitulo.ForeColor = System.Drawing.Color.FromArgb(33, 33, 33);
            this.Controls.Add(labelTitulo);

            // Label Selecione o Cooperado
            Label labelCooperado = new Label();
            labelCooperado.Text = "Selecione o Cooperado:";
            labelCooperado.Location = new System.Drawing.Point(20, 70);
            labelCooperado.Size = new System.Drawing.Size(400, 20);
            labelCooperado.Font = new System.Drawing.Font("Arial", 11, System.Drawing.FontStyle.Bold);
            this.Controls.Add(labelCooperado);

            // ComboBox de Cooperados
            comboBoxCooperados = new ComboBox();
            comboBoxCooperados.Location = new System.Drawing.Point(20, 95);
            comboBoxCooperados.Size = new System.Drawing.Size(520, 30);
            comboBoxCooperados.Font = new System.Drawing.Font("Arial", 11);
            comboBoxCooperados.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxCooperados.SelectedIndexChanged += ComboBoxCooperados_SelectedIndexChanged;
            this.Controls.Add(comboBoxCooperados);

            // Botão Recarregar
            buttonRecarregar = new Button();
            buttonRecarregar.Text = "🔄 Recarregar";
            buttonRecarregar.Location = new System.Drawing.Point(20, 135);
            buttonRecarregar.Size = new System.Drawing.Size(520, 35);
            buttonRecarregar.Font = new System.Drawing.Font("Arial", 10);
            buttonRecarregar.BackColor = System.Drawing.Color.FromArgb(76, 175, 80);
            buttonRecarregar.ForeColor = System.Drawing.Color.White;
            buttonRecarregar.Click += ButtonRecarregar_Click;
            this.Controls.Add(buttonRecarregar);

            // Label Dados do Cooperado
            labelCooperadoSelecionado = new Label();
            labelCooperadoSelecionado.Text = "Selecione um cooperado para visualizar os dados";
            labelCooperadoSelecionado.Location = new System.Drawing.Point(20, 185);
            labelCooperadoSelecionado.Size = new System.Drawing.Size(540, 25);
            labelCooperadoSelecionado.Font = new System.Drawing.Font("Arial", 10, System.Drawing.FontStyle.Bold);
            labelCooperadoSelecionado.ForeColor = System.Drawing.Color.FromArgb(33, 33, 33);
            this.Controls.Add(labelCooperadoSelecionado);

            // Status Biometria
            labelBiometriaStatus = new Label();
            labelBiometriaStatus.Text = "Biometria: --";
            labelBiometriaStatus.Location = new System.Drawing.Point(20, 210);
            labelBiometriaStatus.Size = new System.Drawing.Size(540, 25);
            labelBiometriaStatus.Font = new System.Drawing.Font("Arial", 9, System.Drawing.FontStyle.Bold);
            labelBiometriaStatus.ForeColor = System.Drawing.Color.FromArgb(120, 120, 120);
            labelBiometriaStatus.BackColor = System.Drawing.Color.FromArgb(235, 235, 235);
            labelBiometriaStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.Controls.Add(labelBiometriaStatus);

            // CPF
            Label labelCPF = new Label();
            labelCPF.Text = "CPF:";
            labelCPF.Location = new System.Drawing.Point(20, 245);
            labelCPF.Size = new System.Drawing.Size(260, 20);
            this.Controls.Add(labelCPF);

            textBoxCPF = new TextBox();
            textBoxCPF.Location = new System.Drawing.Point(20, 270);
            textBoxCPF.Size = new System.Drawing.Size(260, 30);
            textBoxCPF.Font = new System.Drawing.Font("Arial", 11);
            textBoxCPF.ReadOnly = true;
            textBoxCPF.BackColor = System.Drawing.Color.FromArgb(230, 230, 230);
            this.Controls.Add(textBoxCPF);

            // Email
            Label labelEmail = new Label();
            labelEmail.Text = "E-mail:";
            labelEmail.Location = new System.Drawing.Point(300, 245);
            labelEmail.Size = new System.Drawing.Size(260, 20);
            this.Controls.Add(labelEmail);

            textBoxEmail = new TextBox();
            textBoxEmail.Location = new System.Drawing.Point(300, 270);
            textBoxEmail.Size = new System.Drawing.Size(260, 30);
            textBoxEmail.Font = new System.Drawing.Font("Arial", 11);
            textBoxEmail.ReadOnly = true;
            textBoxEmail.BackColor = System.Drawing.Color.FromArgb(230, 230, 230);
            this.Controls.Add(textBoxEmail);

            // Telefone
            Label labelTelefone = new Label();
            labelTelefone.Text = "Telefone:";
            labelTelefone.Location = new System.Drawing.Point(20, 315);
            labelTelefone.Size = new System.Drawing.Size(540, 20);
            this.Controls.Add(labelTelefone);

            textBoxTelefone = new TextBox();
            textBoxTelefone.Location = new System.Drawing.Point(20, 340);
            textBoxTelefone.Size = new System.Drawing.Size(540, 30);
            textBoxTelefone.Font = new System.Drawing.Font("Arial", 11);
            textBoxTelefone.ReadOnly = true;
            textBoxTelefone.BackColor = System.Drawing.Color.FromArgb(230, 230, 230);
            this.Controls.Add(textBoxTelefone);

            // Ícone de Impressão Digital
            pictureBoxFingerprintIcon = new PictureBox();
            pictureBoxFingerprintIcon.Location = new System.Drawing.Point(20, 390);
            pictureBoxFingerprintIcon.Size = new System.Drawing.Size(60, 60);
            pictureBoxFingerprintIcon.SizeMode = PictureBoxSizeMode.StretchImage;
            pictureBoxFingerprintIcon.BackColor = System.Drawing.Color.Transparent;
            // Você pode adicionar uma imagem aqui se tiver
            this.Controls.Add(pictureBoxFingerprintIcon);

            // Botão Capturar Digital
            buttonCapturarDigital = new Button();
            buttonCapturarDigital.Text = "☝️ Capturar Digital";
            buttonCapturarDigital.Location = new System.Drawing.Point(90, 390);
            buttonCapturarDigital.Size = new System.Drawing.Size(470, 60);
            buttonCapturarDigital.Font = new System.Drawing.Font("Arial", 12, System.Drawing.FontStyle.Bold);
            buttonCapturarDigital.BackColor = System.Drawing.Color.FromArgb(33, 150, 243);
            buttonCapturarDigital.ForeColor = System.Drawing.Color.White;
            buttonCapturarDigital.Enabled = false;
            buttonCapturarDigital.Click += ButtonCapturarDigital_Click;
            this.Controls.Add(buttonCapturarDigital);

            // Label Status
            labelStatus = new Label();
            labelStatus.Text = "⏳ Carregando cooperados...";
            labelStatus.Location = new System.Drawing.Point(20, 465);
            labelStatus.Size = new System.Drawing.Size(540, 30);
            labelStatus.Font = new System.Drawing.Font("Arial", 10);
            labelStatus.AutoSize = true;
            labelStatus.ForeColor = System.Drawing.Color.FromArgb(100, 100, 100);
            this.Controls.Add(labelStatus);

            // Label Leitor Detectado
            labelLeitorDetectado = new Label();
            labelLeitorDetectado.Text = "☑ Leitor biométrico detectado";
            labelLeitorDetectado.Location = new System.Drawing.Point(20, 495);
            labelLeitorDetectado.Size = new System.Drawing.Size(300, 20);
            labelLeitorDetectado.Font = new System.Drawing.Font("Arial", 9);
            labelLeitorDetectado.ForeColor = System.Drawing.Color.Green;
            this.Controls.Add(labelLeitorDetectado);

            // Botão Salvar
            buttonSalvar = new Button();
            buttonSalvar.Text = "💾 Salvar Biometria";
            buttonSalvar.Location = new System.Drawing.Point(20, 530);
            buttonSalvar.Size = new System.Drawing.Size(260, 45);
            buttonSalvar.Font = new System.Drawing.Font("Arial", 11, System.Drawing.FontStyle.Bold);
            buttonSalvar.BackColor = System.Drawing.Color.FromArgb(76, 175, 80);
            buttonSalvar.ForeColor = System.Drawing.Color.White;
            buttonSalvar.Enabled = false;
            buttonSalvar.Click += ButtonSalvar_Click;
            this.Controls.Add(buttonSalvar);

            // Botão Cancelar
            buttonCancelar = new Button();
            buttonCancelar.Text = "❌ Cancelar";
            buttonCancelar.Location = new System.Drawing.Point(300, 530);
            buttonCancelar.Size = new System.Drawing.Size(260, 45);
            buttonCancelar.Font = new System.Drawing.Font("Arial", 11, System.Drawing.FontStyle.Bold);
            buttonCancelar.BackColor = System.Drawing.Color.FromArgb(244, 67, 54);
            buttonCancelar.ForeColor = System.Drawing.Color.White;
            buttonCancelar.Click += ButtonCancelar_Click;
            this.Controls.Add(buttonCancelar);

            // Botão Limpar Biometria
            buttonLimparBiometria = new Button();
            buttonLimparBiometria.Text = "🧹 Limpar Biometria";
            buttonLimparBiometria.Location = new System.Drawing.Point(20, 585);
            buttonLimparBiometria.Size = new System.Drawing.Size(520, 45);
            buttonLimparBiometria.Font = new System.Drawing.Font("Arial", 11, System.Drawing.FontStyle.Bold);
            buttonLimparBiometria.BackColor = System.Drawing.Color.FromArgb(255, 152, 0);
            buttonLimparBiometria.ForeColor = System.Drawing.Color.White;
            buttonLimparBiometria.Enabled = false;
            buttonLimparBiometria.Click += ButtonLimparBiometria_Click;
            this.Controls.Add(buttonLimparBiometria);

            AplicarEstiloModerno();
        }

        private void AplicarEstiloModerno()
        {
            Color primary = Color.FromArgb(33, 150, 243);
            Color success = Color.FromArgb(76, 175, 80);
            Color danger = Color.FromArgb(244, 67, 54);
            Color warning = Color.FromArgb(255, 152, 0);
            Color neutral = Color.FromArgb(96, 125, 139);

            buttonRecarregar.BackColor = neutral;
            buttonCapturarDigital.BackColor = primary;
            buttonSalvar.BackColor = success;
            buttonCancelar.BackColor = danger;
            buttonLimparBiometria.BackColor = warning;

            foreach (var btn in new[] { buttonRecarregar, buttonCapturarDigital, buttonSalvar, buttonCancelar, buttonLimparBiometria })
            {
                btn.FlatStyle = FlatStyle.Flat;
                btn.FlatAppearance.BorderSize = 0;
                btn.ForeColor = Color.White;
                btn.Cursor = Cursors.Hand;
                RoundControl(btn, 8);
            }

            comboBoxCooperados.BackColor = Color.White;
            textBoxCPF.BackColor = Color.FromArgb(245, 245, 245);
            textBoxEmail.BackColor = Color.FromArgb(245, 245, 245);
            textBoxTelefone.BackColor = Color.FromArgb(245, 245, 245);

            labelBiometriaStatus.Padding = new Padding(8, 0, 0, 0);
            RoundControl(labelBiometriaStatus, 6);
        }

        private void RoundControl(Control control, int radius)
        {
            if (control.Width == 0 || control.Height == 0)
            {
                return;
            }

            var rect = new Rectangle(0, 0, control.Width, control.Height);
            int d = radius * 2;

            using (var path = new GraphicsPath())
            {
                path.AddArc(rect.X, rect.Y, d, d, 180, 90);
                path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
                path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
                path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
                path.CloseFigure();

                control.Region = new Region(path);
            }
        }

        private async Task AtualizarStatusBiometriaAsync()
        {
            if (_cooperadoSelecionado == null)
            {
                labelBiometriaStatus.Text = "Biometria: --";
                labelBiometriaStatus.ForeColor = Color.FromArgb(120, 120, 120);
                labelBiometriaStatus.BackColor = Color.FromArgb(235, 235, 235);
                buttonLimparBiometria.Enabled = false;
                return;
            }

            labelBiometriaStatus.Text = "Biometria: verificando...";
            labelBiometriaStatus.ForeColor = Color.FromArgb(80, 80, 80);
            labelBiometriaStatus.BackColor = Color.FromArgb(235, 235, 235);

            bool existeLocal = false;
            bool existeNeon = false;

            try
            {
                DatabaseHelper db = new DatabaseHelper();
                existeLocal = db.TemBiometriaLocal(_cooperadoSelecionado.Id);
            }
            catch { }

            try
            {
                existeNeon = await _neonCooperadoHelper.TemBiometriaAsync(_cooperadoSelecionado.Id);
            }
            catch { }

            if (existeLocal || existeNeon)
            {
                labelBiometriaStatus.Text = $"Biometria: CADASTRADA (Local: {(existeLocal ? "Sim" : "Não")} | NEON: {(existeNeon ? "Sim" : "Não")})";
                labelBiometriaStatus.ForeColor = Color.FromArgb(27, 94, 32);
                labelBiometriaStatus.BackColor = Color.FromArgb(200, 230, 201);
                buttonLimparBiometria.Enabled = true;
            }
            else
            {
                labelBiometriaStatus.Text = "Biometria: NÃO CADASTRADA";
                labelBiometriaStatus.ForeColor = Color.FromArgb(183, 28, 28);
                labelBiometriaStatus.BackColor = Color.FromArgb(255, 205, 210);
                buttonLimparBiometria.Enabled = false;
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            CarregarCooperados();
        }

        private async void CarregarCooperados()
        {
            try
            {
                labelStatus.Text = "⏳ Conectando ao NEON...";
                Application.DoEvents();

                // Testar conexão (neonHelper já inicializado no construtor)
                bool conexaoOk = await _neonCooperadoHelper.TestConnectionAsync();
                if (!conexaoOk)
                {
                    labelStatus.Text = "❌ Erro: Não foi possível conectar ao NEON";
                    MessageBox.Show(
                        "Não foi possível conectar ao banco de dados NEON.\n\nVerifique a string de conexão.",
                        "Erro de Conexão",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                    return;
                }

                labelStatus.Text = "⏳ Carregando cooperados...";
                Application.DoEvents();

                // Carregar cooperados
                _cooperados = await _neonCooperadoHelper.GetCooperadosAsync();

                if (_cooperados.Count > 0)
                {
                    PreencherComboBox();
                    labelStatus.Text = $"✅ {_cooperados.Count} cooperado(s) carregado(s) com sucesso!";
                }
                else
                {
                    labelStatus.Text = "⚠ Nenhum cooperado encontrado no NEON";
                }
            }
            catch (Exception ex)
            {
                labelStatus.Text = $"❌ Erro ao carregar cooperados: {ex.Message}";
                Debug.WriteLine($"Erro: {ex}");
            }
        }

        private void PreencherComboBox()
        {
            try
            {
                comboBoxCooperados.Items.Clear();
                comboBoxCooperados.Items.Add("-- Selecione um Cooperado --");

                foreach (var cooperado in _cooperados)
                {
                    comboBoxCooperados.Items.Add(new CooperadoDisplayItem
                    {
                        Cooperado = cooperado,
                        DisplayText = cooperado.Nome
                    });
                }

                // Somente setar SelectedIndex se houver itens
                if (comboBoxCooperados.Items.Count > 0)
                {
                    comboBoxCooperados.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                labelStatus.Text = $"❌ Erro ao preencher lista: {ex.Message}";
                Debug.WriteLine($"Erro em PreencherComboBox: {ex}");
            }
        }

        private void ComboBoxCooperados_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBoxCooperados.SelectedIndex <= 0)
            {
                LimparCampos();
                buttonCapturarDigital.Enabled = false;
                _cooperadoSelecionado = null;
                labelCooperadoSelecionado.Text = "Selecione um cooperado para visualizar os dados";
                _ = AtualizarStatusBiometriaAsync();
                return;
            }

            var item = (CooperadoDisplayItem)comboBoxCooperados.SelectedItem;
            _cooperadoSelecionado = item.Cooperado;

            // Preencher campos
            labelCooperadoSelecionado.Text = $"Dados de: {_cooperadoSelecionado.Nome}";
            textBoxCPF.Text = _cooperadoSelecionado.Cpf ?? "Não informado";
            textBoxEmail.Text = _cooperadoSelecionado.Email ?? "Não informado";
            textBoxTelefone.Text = _cooperadoSelecionado.Telefone ?? "Não informado";

            // Habilitar botão de captura
            buttonCapturarDigital.Enabled = true;
            labelStatus.Text = $"✓ Cooperado selecionado: {_cooperadoSelecionado.Nome}";
            _ = AtualizarStatusBiometriaAsync();
        }

        private async void ButtonCapturarDigital_Click(object sender, EventArgs e)
        {
            try
            {
                if (_cooperadoSelecionado == null)
                {
                    MessageBox.Show("Selecione um cooperado primeiro!");
                    return;
                }

                labelStatus.Text = "📥 Posicione o dedo no leitor...";
                Application.DoEvents();

                // Aguardar captura de biometria
                bool biometriaCaptured = await CapturarBiometria();

                if (biometriaCaptured)
                {
                    labelStatus.Text = "✓ Biometria capturada com sucesso!";
                    buttonSalvar.Enabled = true;
                }
                else
                {
                    labelStatus.Text = "❌ Falha ao capturar biometria";
                    buttonSalvar.Enabled = false;
                }
            }
            catch (Exception ex)
            {
                labelStatus.Text = $"❌ Erro: {ex.Message}";
            }
        }

        private async Task<bool> CapturarBiometria()
        {
            try
            {
                // Iniciar modo de registro (enrollment)
                if (!_fingerprintService.StartEnrollment())
                {
                    labelStatus.Text = "❌ Erro ao iniciar captura";
                    return false;
                }

                // Aguardar até que 3 amostras sejam capturadas
                var tcs = new TaskCompletionSource<bool>();
                bool capturaCompleta = false;
                byte[]? templateCapturado = null;

                EventHandler<byte[]>? handler = null;
                handler = (sender, template) =>
                {
                    templateCapturado = template;
                    capturaCompleta = true;
                    tcs.TrySetResult(true);
                };

                _fingerprintService.OnFingerprintCaptured += handler;

                // Aguardar captura com timeout de 30 segundos
                var timeoutTask = Task.Delay(30000);
                var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);

                _fingerprintService.OnFingerprintCaptured -= handler;
                _fingerprintService.StopEnrollment();

                if (completedTask == timeoutTask)
                {
                    labelStatus.Text = "⏰ Tempo esgotado. Tente novamente.";
                    return false;
                }

                if (capturaCompleta && templateCapturado != null)
                {
                    _biometriaCapturada = templateCapturado;
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                labelStatus.Text = $"❌ Erro: {ex.Message}";
                return false;
            }
        }

        private void OnFingerprintCaptured(object? sender, byte[] template)
        {
            if (InvokeRequired)
            {
                Invoke(() => OnFingerprintCaptured(sender, template));
                return;
            }

            _biometriaCapturada = template;
            labelStatus.Text = "✓ Biometria capturada com sucesso!";
        }

        private async void ButtonSalvar_Click(object sender, EventArgs e)
        {
            try
            {
                if (_cooperadoSelecionado == null)
                {
                    MessageBox.Show("Selecione um cooperado primeiro!", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (_biometriaCapturada == null || _biometriaCapturada.Length == 0)
                {
                    MessageBox.Show("Nenhuma biometria capturada. Capture a digital primeiro!", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (string.IsNullOrEmpty(_cooperadoSelecionado.Id))
                {
                    MessageBox.Show("Cooperado inválido. Recarregue a lista e tente novamente.", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                labelStatus.Text = "📤 Salvando biometria localmente...";
                buttonSalvar.Enabled = false;
                Application.DoEvents();

                // Salvar biometria LOCAL primeiro (instantâneo)
                DatabaseHelper db = new DatabaseHelper();
                bool sucessoLocal = db.SalvarBiometriaLocal(
                    _cooperadoSelecionado.Id,
                    _cooperadoSelecionado.Nome,
                    _biometriaCapturada,
                    out string errorMessage,
                    hash: "" // Hash será gerado no NEON
                );

                if (sucessoLocal)
                {
                    string cooperadoId = _cooperadoSelecionado.Id;
                    MessageBox.Show(
                        $"✓ Biometria registrada com sucesso!\n\n" +
                        $"Cooperado: {_cooperadoSelecionado.Nome}\n" +
                        $"Template: {_biometriaCapturada.Length} bytes\n\n" +
                        $"(Sincronizando com NEON em background...)",
                        "Sucesso",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );

                    LimparCampos();
                    labelStatus.Text = "✅ Cadastro concluído com sucesso!";

                    _ = AtualizarStatusBiometriaAsync();

                    // Sincronizar com NEON em background (não bloqueia)
                    _ = SincronizarBiometriaComNeonAsync(cooperadoId);
                }
                else
                {
                    string msgErro = string.IsNullOrEmpty(errorMessage) 
                        ? "Erro ao salvar biometria no banco de dados local.\n\nVerifique e tente novamente."
                        : $"Erro ao salvar biometria no banco de dados local:\n\n{errorMessage}\n\nVerifique e tente novamente.";
                    
                    MessageBox.Show(
                        msgErro,
                        "Erro",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                    labelStatus.Text = "❌ Erro ao salvar biometria";
                    buttonSalvar.Enabled = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                labelStatus.Text = $"❌ Erro: {ex.Message}";
                buttonSalvar.Enabled = true;
            }
        }

        /// <summary>
        /// Sincroniza biometria local com NEON em background
        /// </summary>
        private async Task SincronizarBiometriaComNeonAsync(string cooperadoId)
        {
            try
            {
                LogToFile($"[SYNC] Iniciando sincronização de biometria. cooperadoId={cooperadoId}");
                // Verificar se neonHelper está inicializado
                if (_neonCooperadoHelper == null)
                {
                    Debug.WriteLine("⚠️ NeonCooperadoHelper não inicializado, pulando sincronização");
                    LogToFile("[SYNC] NeonCooperadoHelper não inicializado, pulando sincronização");
                    return;
                }

                if (InvokeRequired)
                {
                    Invoke(() => labelStatus.Text = "🔄 Sincronizando com NEON...");
                }
                else
                {
                    labelStatus.Text = "🔄 Sincronizando com NEON...";
                }
                Application.DoEvents();

                DatabaseHelper db = new DatabaseHelper();
                var biometriasNaoSincronizadas = db.BuscarBiometriasNaoSincronizadas()
                    .Where(b => b.CooperadoId == cooperadoId)
                    .ToList();

                LogToFile($"[SYNC] Biometrias não sincronizadas encontradas: {biometriasNaoSincronizadas.Count}");

                foreach (var biometria in biometriasNaoSincronizadas)
                {
                    try
                    {
                        // Usar o nome do cooperado diretamente do registro local
                        string cooperadoNome = biometria.CooperadoNome ?? "";

                        LogToFile($"[SYNC] Enviando biometria {biometria.Id}. cooperadoId={cooperadoId}, nome={cooperadoNome}, fingerIndex={biometria.FingerIndex}, bytes={biometria.Template?.Length ?? 0}");

                        bool sucesso = await _neonCooperadoHelper.SalvarBiometriaAsync(
                            cooperadoId,
                            biometria.Template,
                            biometria.FingerIndex,
                            cooperadoNome
                        );

                        if (sucesso)
                        {
                            db.MarcabiometriaComoSincronizada(biometria.Id);
                            Debug.WriteLine($"✅ Biometria {biometria.Id} sincronizada com NEON");
                            LogToFile($"[SYNC] Biometria {biometria.Id} sincronizada com sucesso");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Erro ao sincronizar biometria com NEON: {ex.Message}");
                        LogToFile($"[SYNC] Erro ao sincronizar biometria {biometria.Id}: {ex.Message}");
                        LogToFile($"[SYNC] Stack: {ex.StackTrace}");
                    }
                }

                if (InvokeRequired)
                {
                    Invoke(() => labelStatus.Text = "✅ Sincronização com NEON concluída!");
                }
                else
                {
                    labelStatus.Text = "✅ Sincronização com NEON concluída!";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro na sincronização de biometria: {ex.Message}");
            }
        }

        private async void ButtonLimparBiometria_Click(object sender, EventArgs e)
        {
            if (_cooperadoSelecionado == null)
            {
                MessageBox.Show("Selecione um cooperado primeiro!", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var confirm = MessageBox.Show(
                "Deseja remover a biometria deste cooperado?\n\nEssa ação apaga a biometria local e no NEON.",
                "Confirmar remoção",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning
            );

            if (confirm != DialogResult.Yes)
            {
                return;
            }

            buttonLimparBiometria.Enabled = false;
            labelStatus.Text = "🧹 Removendo biometria...";
            Application.DoEvents();

            int removidasLocal = 0;
            int removidasNeon = 0;
            string erroLocal = string.Empty;

            try
            {
                DatabaseHelper db = new DatabaseHelper();
                removidasLocal = db.RemoverBiometriasLocal(_cooperadoSelecionado.Id, out erroLocal);
            }
            catch (Exception ex)
            {
                erroLocal = ex.Message;
            }

            try
            {
                removidasNeon = await _neonCooperadoHelper.RemoverBiometriasAsync(_cooperadoSelecionado.Id);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro ao remover biometria no NEON: {ex.Message}");
            }

            if (!string.IsNullOrEmpty(erroLocal))
            {
                MessageBox.Show(
                    $"Erro ao remover biometria local:\n{erroLocal}",
                    "Erro",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }

            labelStatus.Text = $"✅ Biometria removida (Local: {removidasLocal} | NEON: {removidasNeon})";
            await AtualizarStatusBiometriaAsync();
        }

        private void ButtonCancelar_Click(object sender, EventArgs e)
        {
            LimparCampos();
            this.DialogResult = DialogResult.Cancel;
        }

        private async void ButtonRecarregar_Click(object sender, EventArgs e)
        {
            CarregarCooperados();
        }

        private void LimparCampos()
        {
            textBoxCPF.Clear();
            textBoxEmail.Clear();
            textBoxTelefone.Clear();
            comboBoxCooperados.SelectedIndex = 0;
            _cooperadoSelecionado = null;
            _biometriaCapturada = null;
            buttonCapturarDigital.Enabled = false;
            buttonSalvar.Enabled = false;
            labelCooperadoSelecionado.Text = "Selecione um cooperado para visualizar os dados";
            labelBiometriaStatus.Text = "Biometria: --";
            labelBiometriaStatus.ForeColor = Color.FromArgb(120, 120, 120);
            labelBiometriaStatus.BackColor = Color.FromArgb(235, 235, 235);
            buttonLimparBiometria.Enabled = false;
        }

        private void LogToFile(string message)
        {
            try
            {
                string logRoot = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) ?? "C:\\Temp";
                if (string.IsNullOrEmpty(logRoot)) logRoot = "C:\\Temp";
                string logDir = Path.Combine(logRoot, "BiometricSystem");
                Directory.CreateDirectory(logDir);
                string logPath = Path.Combine(logDir, "biometric_log.txt");
                string logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}";
                File.AppendAllText(logPath, logMessage + Environment.NewLine);
                Debug.WriteLine(logMessage);
            }
            catch { }
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.ResumeLayout(false);
        }

        private class CooperadoDisplayItem
        {
            public Cooperado Cooperado { get; set; }
            public string DisplayText { get; set; }

            public override string ToString() => DisplayText;
        }
    }
}
