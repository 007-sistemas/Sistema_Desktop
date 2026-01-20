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
        private System.Windows.Forms.Timer? clearPanelTimer; // Timer para limpar painel após registro
        public bool AllowClose { get; set; } = false; // Controla se pode fechar realmente

        public LoginForm(IConfiguration? config = null)
        {
            InitializeComponent();
            fingerprintService = new FingerprintService();
            database = new DatabaseHelper();
            
            // Inicializar timer para limpeza de painel
            clearPanelTimer = new System.Windows.Forms.Timer();
            clearPanelTimer.Tick += (sender, e) =>
            {
                try
                {
                    LogToFile($"⏰ Timer disparado - limpando painel");
                    clearPanelTimer.Stop();
                    
                    panelSimulador.BackColor = System.Drawing.Color.White;
                    lblSimulador.Text = "";
                    lblSimulador.Font = new System.Drawing.Font("Segoe UI", 12F);
                    lblSimulador.TextAlign = System.Drawing.ContentAlignment.TopLeft;
                    lblStatus.Text = "Selecione o setor para ativar o leitor";
                    
                    LogToFile($"⏰ Painel limpo com sucesso");
                }
                catch (Exception ex)
                {
                    LogToFile($"❌ Erro ao limpar painel: {ex.Message}");
                }
            };

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
        }

        private async void CarregarSetoresDoHospital()
        {
            Debug.WriteLine("═══════════════════════════════════════════");
            Debug.WriteLine("🔄 INICIANDO CARREGAMENTO DE SETORES");
            Debug.WriteLine("═══════════════════════════════════════════");

            List<NeonCooperadoHelper.SetorInfo> setores = new List<NeonCooperadoHelper.SetorInfo>();
            string cacheHospitalId = string.IsNullOrEmpty(hospitalId) ? "DEFAULT" : hospitalId;

            // Setores padrão (fallback final)
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

            try
            {
                Debug.WriteLine($"📋 hospitalId: '{hospitalId}', cacheId: '{cacheHospitalId}'");

                // Prioridade 1: Tentar Neon se tiver hospital e neonHelper
                if (!string.IsNullOrEmpty(hospitalId) && neonHelper != null)
                {
                    Debug.WriteLine("🌐 Tentativa 1: Neon");
                    try
                    {
                        setores = await neonHelper.GetSetoresDoHospitalAsync(hospitalId, database);
                        if (setores.Any())
                        {
                            Debug.WriteLine($"✅ SUCESSO: {setores.Count} setores do Neon");
                            lblStatus.Text = "✅ Setores carregados online.";
                            ExibirSetores(setores);
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"❌ Neon falhou: {ex.Message}");
                    }
                }
                else
                {
                    Debug.WriteLine("⏭️ Pulando Neon: sem hospital configurado ou neonHelper null");
                }

                // Prioridade 2: Tentar cache local
                Debug.WriteLine($"📂 Tentativa 2: Cache local ({cacheHospitalId})");
                var setoresLocais = database.BuscarSetoresLocal(cacheHospitalId);
                if (setoresLocais.Any())
                {
                    setores = setoresLocais.Select(s => new NeonCooperadoHelper.SetorInfo
                    {
                        Id = s.Id,
                        Nome = s.Nome
                    }).ToList();
                    
                    Debug.WriteLine($"✅ SUCESSO: {setores.Count} setores do cache");
                    lblStatus.Text = "📂 Setores carregados do cache (offline).";
                    ExibirSetores(setores);
                    return;
                }

                // Prioridade 3: Setores padrão (sempre funciona)
                Debug.WriteLine("📋 Tentativa 3: Setores padrão (fallback)");
                database.SalvarSetoresLocal(cacheHospitalId, setoresPadrao);
                
                setores = setoresPadrao.Select(s => new NeonCooperadoHelper.SetorInfo
                {
                    Id = s.Item1,
                    Nome = s.Item2
                }).ToList();
                
                Debug.WriteLine($"✅ SUCESSO: {setores.Count} setores padrão carregados e salvos");
                lblStatus.Text = "📂 Setores padrão carregados.";
                ExibirSetores(setores);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ ERRO GERAL: {ex.Message}");
                Debug.WriteLine($"❌ Stack: {ex.StackTrace}");
                
                // Fallback final: exibir setores padrão como string
                Debug.WriteLine("🆘 Usando fallback final: strings padrão");
                try
                {
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
                    lblStatus.Text = "📂 Setores padrão (modo emergência).";
                    Debug.WriteLine("✅ Fallback final funcionou");
                }
                catch (Exception exFinal)
                {
                    Debug.WriteLine($"❌ Até o fallback falhou: {exFinal.Message}");
                    lblStatus.Text = "⚠️ Erro crítico ao carregar setores.";
                }
            }

            Debug.WriteLine("═══════════════════════════════════════════");
        }

        private void ExibirSetores(List<NeonCooperadoHelper.SetorInfo> setores)
        {
            try
            {
                cmbSetor.Items.Clear();
                cmbSetor.Items.AddRange(setores.ToArray());
                cmbSetor.DisplayMember = "Nome";
                cmbSetor.ValueMember = "Id";
                cmbSetor.SelectedIndex = -1;
                Debug.WriteLine($"✅ Dropdown exibindo {setores.Count} setores");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Erro ao exibir setores: {ex.Message}");
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
                isCapturing = true;

                lblStatus.Text = $"⏳ Setor: {selectedSetor} - Posicione o dedo no leitor...";
                
                // Animar ícone de digital
                panelFingerprint.BackColor = System.Drawing.Color.FromArgb(230, 240, 255);
                
                // Iniciar captura automática
                await fingerprintService.StartCapture();

                // Reabilitar após captura
                cmbSetor.Enabled = true;
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
                    
                    // O banco salva como "ENTRADA" ou "SAIDA" (maiúsculas sem acento)
                    // Determinar o próximo tipo baseado no último
                    string tipoRegistro;
                    if (string.IsNullOrEmpty(ultimoTipo) || ultimoTipo.Equals("SAIDA", StringComparison.OrdinalIgnoreCase))
                    {
                        tipoRegistro = "ENTRADA";  // Se não há registro ou último foi saída, registrar entrada
                    }
                    else
                    {
                        tipoRegistro = "SAIDA";    // Se último foi entrada, registrar saída
                    }

                    LogToFile($"   Tipo de registro: {tipoRegistro} (último registro foi: {ultimoTipo ?? "nenhum"})");

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
            // Parar timer anterior se existir
            if (clearPanelTimer != null)
            {
                clearPanelTimer.Stop();
                clearPanelTimer.Dispose();
                clearPanelTimer = null;
            }
            
            // Definir cores conforme o tipo
            Color backgroundColor;
            Color textColor;
            string emoji;
            string tipoExibicao;
            
            if (tipo.Equals("ENTRADA", StringComparison.OrdinalIgnoreCase))
            {
                backgroundColor = System.Drawing.Color.FromArgb(230, 255, 240); // Verde claro
                textColor = System.Drawing.Color.FromArgb(0, 120, 60);
                emoji = "➜";
                tipoExibicao = "ENTRADA";
            }
            else
            {
                backgroundColor = System.Drawing.Color.FromArgb(255, 235, 235); // Vermelho claro
                textColor = System.Drawing.Color.FromArgb(180, 30, 30);
                emoji = "⬅";
                tipoExibicao = "SAÍDA";
            }
            
            panelSimulador.BackColor = backgroundColor;
            
            // Montar texto formatado
            string textoExibicao = $"{emoji}  {tipoExibicao} REGISTRADA\n\n";
            textoExibicao += $"{nomeCooperado}\n";
            textoExibicao += $"Cooperado\n\n";
            textoExibicao += $"📍 {selectedSetor}\n";
            textoExibicao += $"🕐 {horario:HH:mm:ss}";
            
            lblSimulador.Text = textoExibicao;
            lblSimulador.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold);
            lblSimulador.ForeColor = textColor;
            lblSimulador.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            
            lblStatus.Text = $"✅ {tipoExibicao} registrada - {nomeCooperado}";
            
            LogToFile($"⏰ Agendando limpeza do painel em 5 segundos...");
            
            // Usar thread separada para aguardar 5 segundos e depois limpar
            var cleanupThread = new Thread(() =>
            {
                try
                {
                    Thread.Sleep(5000);
                    
                    // Executar na thread UI
                    this.Invoke(new Action(() =>
                    {
                        try
                        {
                            LogToFile($"⏰ Limpando painel após 5 segundos");
                            panelSimulador.BackColor = System.Drawing.Color.White;
                            lblSimulador.Text = "";
                            lblSimulador.Font = new System.Drawing.Font("Segoe UI", 12F);
                            lblSimulador.TextAlign = System.Drawing.ContentAlignment.TopLeft;
                            lblStatus.Text = "Selecione o setor para ativar o leitor";
                            LogToFile($"⏰ Painel limpo com sucesso");
                        }
                        catch (Exception ex)
                        {
                            LogToFile($"❌ Erro ao limpar: {ex.Message}");
                        }
                    }));
                }
                catch (Exception ex)
                {
                    LogToFile($"❌ Erro na thread: {ex.Message}");
                }
            })
            {
                IsBackground = true
            };
            cleanupThread.Start();
        }
        
        
        private void LimparPainelSimulador()
        {
            try
            {
                LogToFile($"⏰ Limpando painel - início");
                
                panelSimulador.BackColor = System.Drawing.Color.White;
                lblSimulador.Text = "";
                lblSimulador.Font = new System.Drawing.Font("Segoe UI", 12F);
                lblSimulador.TextAlign = System.Drawing.ContentAlignment.TopLeft;
                lblStatus.Text = "Selecione o setor para ativar o leitor";
                
                LogToFile($"⏰ Limpando painel - concluído");
            }
            catch (Exception ex)
            {
                LogToFile($"❌ Erro em LimparPainelSimulador: {ex.Message}");
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
            // Se AllowClose for true, permite fechar sem abrir menu
            if (AllowClose)
            {
                // Limpar timer se estiver ativo
                if (clearPanelTimer != null)
                {
                    clearPanelTimer.Stop();
                    clearPanelTimer.Dispose();
                    clearPanelTimer = null;
                }
                
                syncService?.StopAutoSync();
                fingerprintService.Dispose();
                base.OnFormClosing(e);
                return;
            }
            
            // Cancelar o fechamento e mostrar tela de acesso
            e.Cancel = true;
            
            // Limpar timer se estiver ativo
            if (clearPanelTimer != null)
            {
                clearPanelTimer.Stop();
                clearPanelTimer.Dispose();
                clearPanelTimer = null;
            }
            
            syncService?.StopAutoSync();
            
            // Abrir tela de acesso
            try
            {
                var accessForm = new AccessMenuForm(this);
                accessForm.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao abrir tela de acesso: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}

