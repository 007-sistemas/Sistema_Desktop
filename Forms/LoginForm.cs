using BiometricSystem.Database;
using BiometricSystem.Models;
using BiometricSystem.Services;
using System.Globalization;
using System.Drawing.Drawing2D;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;

namespace BiometricSystem.Forms
{
    public partial class LoginForm : Form
    {
        private readonly FingerprintService fingerprintService;
        private readonly DatabaseHelper database;
        private readonly SyncService? syncService;
        private NeonCooperadoHelper? neonHelper;
        private string? neonConnectionString;
        private string? selectedSetor;
        private int? selectedSetorId;
        private bool isCapturing = false;
        private string? hospitalId;
        private string? hospitalNome;
        private string? hospitalCodigo;

        public LoginForm(IConfiguration? config = null)
        {
            InitializeComponent();
            fingerprintService = new FingerprintService();
            database = new DatabaseHelper();

            // Inicializar sincronização com Neon se configuração disponível
            if (config != null)
            {
                neonConnectionString = config.GetConnectionString("DefaultConnection") 
                    ?? config["Neon:ConnectionString"];
                
                // Carregar configuração do hospital
                hospitalId = config["Hospital:Id"];
                hospitalNome = config["Hospital:Nome"];
                hospitalCodigo = config["Hospital:Codigo"];
                hospitalCodigo = config["Hospital:Codigo"];
                
                if (!string.IsNullOrEmpty(neonConnectionString))
                {
                    neonHelper = new NeonCooperadoHelper(neonConnectionString);
                    syncService = new SyncService(database, neonConnectionString);
                    syncService.StartAutoSync(intervalSeconds: 30); // Sincronizar a cada 30s
                }
            }
            
            // Fallback: usar string hardcoded se não tiver config
            if (string.IsNullOrEmpty(neonConnectionString))
            {
                neonConnectionString = "Host=ep-dry-dawn-ahl0dlm6-pooler.c-3.us-east-1.aws.neon.tech;Database=neondb;Username=neondb_owner;Password=npg_lOhyE4z1QBtc;SSL Mode=Require;Trust Server Certificate=true";
                neonHelper = new NeonCooperadoHelper(neonConnectionString);
            }

            // Atualizar label com nome do hospital
            if (!string.IsNullOrEmpty(hospitalNome))
            {
                lblLocalProducao.Text = $"🏥 {hospitalNome}";
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
            
            // Carregar setores do hospital
            CarregarSetoresDoHospital();
            
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

        private async void CarregarSetoresDoHospital()
        {
            try
            {
                if (string.IsNullOrEmpty(hospitalId) || neonHelper == null)
                {
                    // Fallback: usar lista padrão se não tiver hospital configurado
                    cmbSetor.Items.Clear();
                    cmbSetor.Items.AddRange(new string[] 
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
                    });
                    cmbSetor.SelectedIndex = -1;
                    return;
                }

                lblStatus.Text = "⏳ Carregando setores...";
                
                List<NeonCooperadoHelper.SetorInfo> setores = new List<NeonCooperadoHelper.SetorInfo>();

                // Tentar carregar do Neon (se houver conexão)
                try
                {
                    setores = await neonHelper.GetSetoresDoHospitalAsync(hospitalId, database);
                    lblStatus.Text = "✅ Setores carregados online.";
                    Debug.WriteLine($"✅ {setores.Count} setores carregados do Neon");
                }
                catch (Exception exNeon)
                {
                    // Sem conexão ou erro - tentar usar cache local
                    Debug.WriteLine($"⚠️ Erro ao carregar do Neon: {exNeon.Message}");
                    
                    var setoresLocais = database.BuscarSetoresLocal(hospitalId);
                    if (setoresLocais.Any())
                    {
                        // Converter para SetorInfo
                        setores = setoresLocais.Select(s => new NeonCooperadoHelper.SetorInfo
                        {
                            Id = s.Id,
                            Nome = s.Nome
                        }).ToList();
                        
                        lblStatus.Text = "📂 Setores carregados do cache local (offline).";
                        Debug.WriteLine($"✅ {setores.Count} setores carregados do cache local");
                    }
                    else
                    {
                        // Usar setores padrão como último recurso
                        var setoresPadrao = new List<(int, string)>
                        {
                            (1, "CENTRO CIRÚRGICO"),
                            (2, "EMERGÊNCIA"),
                            (3, "UTI"),
                            (4, "ENFERMARIA"),
                            (5, "LABORATÓRIO"),
                            (6, "RADIOLOGIA"),
                            (7, "FARMÁCIA"),
                            (8, "RECEPÇÃO"),
                            (9, "ADMINISTRATIVO")
                        };
                        
                        database.SalvarSetoresLocal(hospitalId, setoresPadrao);
                        
                        setores = setoresPadrao.Select(s => new NeonCooperadoHelper.SetorInfo
                        {
                            Id = s.Item1,
                            Nome = s.Item2
                        }).ToList();
                        
                        lblStatus.Text = "📂 Usando setores padrão (offline).";
                        Debug.WriteLine($"📂 {setores.Count} setores padrão carregados e salvos no cache");
                    }
                }
                
                if (setores.Any())
                {
                    cmbSetor.Items.Clear();
                    cmbSetor.Items.AddRange(setores.ToArray());
                    cmbSetor.DisplayMember = "Nome";
                    cmbSetor.ValueMember = "Id";
                    cmbSetor.SelectedIndex = -1;
                    Debug.WriteLine($"✅ {setores.Count} setores exibidos no combo");
                }
                else
                {
                    lblStatus.Text = "⚠️ Nenhum setor encontrado para este hospital.";
                    Debug.WriteLine("⚠️ Nenhum setor disponível");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Erro geral ao carregar setores: {ex.Message}\n{ex.StackTrace}");
                lblStatus.Text = "❌ Erro ao carregar setores.";
                // Não mostrar MessageBox aqui pois já tratamos acima
            }
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

            // Capturar setor e ID do setor selecionado
            if (cmbSetor.SelectedItem is NeonCooperadoHelper.SetorInfo setorInfo)
            {
                selectedSetor = setorInfo.Nome;
                selectedSetorId = setorInfo.Id;
            }
            else
            {
                // Fallback para string simples (lista padrão)
                selectedSetor = cmbSetor.SelectedItem?.ToString();
                selectedSetorId = null;
            }
            
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

        private async void OnFingerprintCaptured(object? sender, byte[] template)
        {
            lblStatus.Text = "⏳ Verificando digital localmente...";
            Refresh();

            try
            {
                LogToFile("🔍 OnFingerprintCaptured - Iniciando verificação LOCAL");

                // Buscar biometrias do banco LOCAL (muito mais rápido)
                LogToFile("📡 Buscando biometrias do SQLite local...");
                var biometriasLocais = database.BuscarBiometriasLocais();
                
                LogToFile($"✅ Biometrias retornadas: {biometriasLocais.Count}");
                
                if (biometriasLocais.Count == 0)
                {
                    LogToFile("⚠️ Lista de biometrias está vazia");
                    lblStatus.Text = "⚠️ Nenhuma biometria cadastrada no sistema";
                    panelSimulador.BackColor = System.Drawing.Color.FromArgb(255, 245, 230);
                    lblSimulador.Text = "Nenhuma biometria cadastrada!\n\nCadastre biometrias primeiro.";
                    lblSimulador.ForeColor = System.Drawing.Color.FromArgb(200, 100, 0);
                    cmbSetor.SelectedIndex = -1;
                    return;
                }

                string? matchedCooperadoId = null;
                string? matchedCooperadoNome = null;

                LogToFile($"🔍 Verificando template capturado contra {biometriasLocais.Count} biometrias...");
                // Verificar contra cada biometria usando o verificador nativo do SDK
                int idx = 0;
                foreach (var biometria in biometriasLocais)
                {
                    idx++;
                    if (biometria.Template != null && biometria.Template.Length > 0)
                    {
                        LogToFile($"   Testando biometria {idx}: {biometria.CooperadoNome} ({biometria.Template.Length} bytes)");
                        if (fingerprintService.VerifyAgainstTemplate(biometria.Template))
                        {
                            LogToFile($"   ✅ MATCH! Cooperado: {biometria.CooperadoNome}");
                            matchedCooperadoId = biometria.CooperadoId;
                            matchedCooperadoNome = biometria.CooperadoNome;
                            break;
                        }
                    }
                    else
                    {
                        LogToFile($"   ⚠️ Biometria {idx} tem template nulo ou vazio");
                    }
                }

                // Limpar features capturadas após verificação completa
                fingerprintService.ClearCapturedFeatures();

                if (matchedCooperadoId != null)
                {
                    LogToFile($"✅ Digital identificada: {matchedCooperadoNome}");
                    
                    // Buscar o ÚLTIMO tipo de ponto registrado localmente
                    var ultimoTipo = database.GetUltimoPontoTipo(matchedCooperadoId);
                    string tipoRegistro = (ultimoTipo == null || ultimoTipo == "Saída") ? "Entrada" : "Saída";

                    LogToFile($"   Tipo de registro: {tipoRegistro}");

                    // Formatar local como no sistema web: "CODIGO_HOSPITAL - SETOR"
                    string localFormatado = string.IsNullOrEmpty(hospitalCodigo) 
                        ? (selectedSetor ?? "N/A")
                        : $"{hospitalCodigo} - {selectedSetor ?? "N/A"}";

                    // Registrar ponto LOCAL (instantâneo)
                    bool sucessoLocal = database.SalvarPontoLocal(
                        matchedCooperadoId,
                        matchedCooperadoNome,
                        tipoRegistro,
                        localFormatado,
                        hospitalId,
                        selectedSetorId
                    );

                    if (sucessoLocal)
                    {
                        LogToFile("   ✅ Ponto registrado localmente com sucesso!");
                        // Exibir informações no painel
                        ExibirRegistroPontoLocal(
                            matchedCooperadoNome,
                            tipoRegistro,
                            DateTime.Now
                        );
                        
                        // Resetar seleção do setor
                        cmbSetor.SelectedIndex = -1;

                        // Sincronizar com NEON em background (não bloqueia UI)
                        LogToFile("   ℹ️ Disparando sincronização em background...");
                        #pragma warning disable CS4014
                        Task.Run(async () => await SincronizarComNeonAsync());
                        #pragma warning restore CS4014
                        LogToFile("   ℹ️ Sincronização disparada (método async)");
                    }
                    else
                    {
                        LogToFile("   ❌ Erro ao registrar ponto localmente");
                        lblStatus.Text = "❌ Erro ao registrar ponto";
                        panelSimulador.BackColor = System.Drawing.Color.FromArgb(255, 230, 230);
                        lblSimulador.Text = "Erro ao registrar ponto no banco de dados!";
                        lblSimulador.ForeColor = System.Drawing.Color.FromArgb(180, 0, 0);
                    }
                }
                else
                {
                    LogToFile("❌ Nenhuma biometria correspondente encontrada");
                    lblStatus.Text = "❌ Digital não reconhecida";
                    panelSimulador.BackColor = System.Drawing.Color.FromArgb(255, 245, 230);
                    lblSimulador.Text = "Digital não reconhecida!\n\nCooperado não cadastrado no sistema.";
                    lblSimulador.ForeColor = System.Drawing.Color.FromArgb(200, 100, 0);
                    cmbSetor.SelectedIndex = -1;
                }
            }
            catch (Exception ex)
            {
                LogToFile($"❌ ERRO em OnFingerprintCaptured: {ex.Message}");
                LogToFile($"   Stack: {ex.StackTrace}");
                lblStatus.Text = $"❌ Erro: {ex.Message}";
            }
        }

        private void LogToFile(string message)
        {
            try
            {
                string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "biometric_log.txt");
                string logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}";
                File.AppendAllText(logPath, logMessage + Environment.NewLine);
                System.Diagnostics.Debug.WriteLine(logMessage);
            }
            catch { }
        }

        private void ExibirRegistroPontoLocal(string nomeCooperado, string tipo, DateTime horario)
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
            textoExibicao += $"{nomeCooperado}\n";
            textoExibicao += $"Cooperado\n\n";
            textoExibicao += $"📍 {selectedSetor}\n";
            textoExibicao += $"🕐 {horario:HH:mm:ss}";
            
            lblSimulador.Text = textoExibicao;
            lblSimulador.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold);
            lblSimulador.ForeColor = textColor;
            lblSimulador.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            
            lblStatus.Text = $"✅ {tipo} registrada - {nomeCooperado}";
            
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

        private void btnCadastrarBiometria_Click(object sender, EventArgs e)
        {
            try
            {
                // Obter a string de conexão do NEON
                string? neonConnectionString = null;
                
                if (syncService != null)
                {
                    // Tentar obter a string de conexão através da sincronização
                    // Se não estiver disponível, precisamos passar através de outro meio
                    neonConnectionString = "Host=ep-dry-dawn-ahl0dlm6-pooler.c-3.us-east-1.aws.neon.tech;Database=neondb;Username=neondb_owner;Password=npg_lOhyE4z1QBtc;SSL Mode=Require;Trust Server Certificate=true";
                }
                else
                {
                    // Fallback: usar a string fornecida
                    neonConnectionString = "Host=ep-dry-dawn-ahl0dlm6-pooler.c-3.us-east-1.aws.neon.tech;Database=neondb;Username=neondb_owner;Password=npg_lOhyE4z1QBtc;SSL Mode=Require;Trust Server Certificate=true";
                }

                var biometriaForm = new CadastrarBiometriaForm(neonConnectionString);
                biometriaForm.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erro ao abrir tela de cadastro de biometria:\n{ex.Message}",
                    "Erro",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        /// <summary>
        /// Sincroniza registros locais (não sincronizados) com NEON em background
        /// Executa de forma assíncrona e não bloqueia a UI
        /// </summary>
        private async Task SincronizarComNeonAsync()
        {
            try
            {
                // Pequeno delay para deixar a UI processar a exibição primeiro
                await Task.Delay(100);

                LogToFile("🔄 [SYNC] Iniciando sincronização com NEON em background...");

                // Verificar se neonHelper está inicializado
                if (neonHelper == null)
                {
                    LogToFile("⚠️ [SYNC] neonHelper não inicializado, pulando sincronização");
                    return;
                }

                // Sincronizar pontos não sincronizados (prioridade alta)
                var pontosNaoSincronizados = database.BuscarPontosNaoSincronizados();
                LogToFile($"   📊 Pontos não sincronizados: {pontosNaoSincronizados.Count}");

                foreach (var ponto in pontosNaoSincronizados)
                {
                    try
                    {
                        LogToFile($"   📤 Sincronizando ponto: {ponto.CooperadoNome} - {ponto.Tipo}");
                        
                        var registro = new RegistroPonto
                        {
                            Id = ponto.Id,
                            Codigo = ponto.Codigo,
                            CooperadoId = ponto.CooperadoId,
                            CooperadoNome = ponto.CooperadoNome,
                            Timestamp = ponto.Timestamp,
                            Tipo = ponto.Tipo,
                            Local = ponto.Local,
                            HospitalId = ponto.HospitalId,
                            SetorId = ponto.SetorId?.ToString(),
                            Status = ponto.Status ?? "Aberto",
                            IsManual = ponto.IsManual,
                            RelatedId = ponto.RelatedId,
                            Date = ponto.Date,
                            Entrada = ponto.Entrada,
                            Saida = ponto.Saida,
                            Observacao = ponto.Observacao,
                            BiometriaEntradaHash = ponto.BiometriaEntradaHash,
                            BiometriaSaidaHash = ponto.BiometriaSaidaHash
                        };

                        bool sucesso = await neonHelper.RegistrarPontoAsync(registro);

                        if (sucesso)
                        {
                            // Marcar como sincronizado no banco local
                            database.MarcaPontoComoSincronizado(ponto.Id);
                            LogToFile($"   ✅ Ponto {ponto.Id} sincronizado com NEON");
                        }
                        else
                        {
                            LogToFile($"   ⚠️ Falha ao sincronizar ponto {ponto.Id}");
                        }
                    }
                    catch (Exception ex)
                    {
                        LogToFile($"   ❌ Erro ao sincronizar ponto: {ex.Message}");
                        LogToFile($"      Stack: {ex.StackTrace}");
                    }
                }

                // Sincronizar biometrias não sincronizadas
                var biometriasNaoSincronizadas = database.BuscarBiometriasNaoSincronizadas();
                LogToFile($"   📊 Biometrias não sincronizadas: {biometriasNaoSincronizadas.Count}");

                foreach (var biometria in biometriasNaoSincronizadas)
                {
                    try
                    {
                        LogToFile($"   📤 Sincronizando biometria: {biometria.CooperadoId}");
                        
                        bool sucesso = await neonHelper.SalvarBiometriaAsync(
                            biometria.CooperadoId,
                            biometria.Template,
                            biometria.FingerIndex
                        );

                        if (sucesso)
                        {
                            // Marcar como sincronizado no banco local
                            database.MarcabiometriaComoSincronizada(biometria.Id);
                            LogToFile($"   ✅ Biometria {biometria.Id} sincronizada com NEON");
                        }
                        else
                        {
                            LogToFile($"   ⚠️ Falha ao sincronizar biometria {biometria.Id}");
                        }
                    }
                    catch (Exception ex)
                    {
                        LogToFile($"   ❌ Erro ao sincronizar biometria: {ex.Message}");
                        LogToFile($"      Stack: {ex.StackTrace}");
                    }
                }

                LogToFile("✅ [SYNC] Sincronização com NEON concluída");
            }
            catch (Exception ex)
            {
                LogToFile($"❌ [SYNC] Erro geral na sincronização: {ex.Message}");
                LogToFile($"   Stack: {ex.StackTrace}");
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            syncService?.StopAutoSync();
            fingerprintService.Dispose();
            base.OnFormClosing(e);
        }
    }
}

