

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
            // Constantes para bloquear movimentação
            private const int WM_NCLBUTTONDOWN = 0xA1;
            private const int HTCAPTION = 0x2;
        // Guardar tamanhos originais para restaurar
        private float fonteOriginalHeader;
        private float fonteOriginalData;
        private float fonteOriginalTitulo;
        private float fonteOriginalInstrucao;
        private float fonteOriginalStatus;
        private Size tamanhoOriginalPanelHeader;
        private Size tamanhoOriginalPanelSimulador;
        private Size tamanhoOriginalPanelStatusBar;

        private readonly FingerprintService fingerprintService;
        private readonly DatabaseHelper database;
        private readonly SyncService? syncService;
        private NeonCooperadoHelper? neonHelper;
        private string? neonConnectionString;
        private string? selectedSetor;
        private int? selectedSetorId;
        private bool isCapturing = false;
        public bool VoltarDaProducao { get; set; } = false;
        private string? hospitalId;
        private string? hospitalNome;
        private string? hospitalCodigo;
        private System.Windows.Forms.Timer? clearPanelTimer; // Timer para limpar painel após registro
        public bool AllowClose { get; set; } = false; // Controla se pode fechar realmente

        public LoginForm(IConfiguration? config = null)
        {
            database = new DatabaseHelper();
            // Solicitar cadastro de senha local se ainda não existir
            if (!database.ExisteSenhaLocal())
            {
                using (var senhaForm = new CadastroSenhaLocalForm())
                {
                    while (true)
                    {
                        var result = senhaForm.ShowDialog();
                        if (result == DialogResult.OK)
                        {
                            if (string.IsNullOrWhiteSpace(senhaForm.Senha) || senhaForm.Senha.Length < 4)
                            {
                                MessageBox.Show("A senha deve ter pelo menos 4 caracteres.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                continue;
                            }
                            if (senhaForm.Senha != senhaForm.Confirmacao)
                            {
                                MessageBox.Show("As senhas não coincidem.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                continue;
                            }
                            if (database.SalvarSenhaLocal(senhaForm.Senha))
                            {
                                MessageBox.Show("Senha local cadastrada com sucesso!", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                break;
                            }
                            else
                            {
                                MessageBox.Show("Erro ao salvar a senha local.", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                continue;
                            }
                        }
                        else
                        {
                            MessageBox.Show("O cadastro da senha local é obrigatório para uso do sistema.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                }
            }
            InitializeComponent();
            // NÃO forçar FormBorderStyle=None aqui, para permitir o X
            this.StartPosition = FormStartPosition.Manual;
            this.Bounds = Screen.FromHandle(this.Handle).Bounds;
            this.TopMost = true;
            // Impede redimensionamento e mantém o X
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            // Salvar tamanhos e fontes originais após InitializeComponent
            fonteOriginalHeader = lblTime.Font.Size;
            fonteOriginalData = lblDate.Font.Size;
            fonteOriginalTitulo = lblLocalProducao.Font.Size;
            fonteOriginalInstrucao = lblInstrucao.Font.Size;
            fonteOriginalStatus = lblStatus.Font.Size;
            tamanhoOriginalPanelHeader = panelHeader.Size;
            tamanhoOriginalPanelSimulador = panelSimulador.Size;
            tamanhoOriginalPanelStatusBar = panelStatusBar.Size;

            // Adaptação dinâmica para telas pequenas
            this.Resize += (s, e) => AdaptarParaTelaPequena();
            AdaptarParaTelaPequena();

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

        // Método para adaptar dinamicamente para telas pequenas
        private void AdaptarParaTelaPequena()
        {
            // Sempre adapta para a área útil da tela
            // Defina o limite de altura considerado "pequeno"
            int limiteAltura = 700;
            bool telaPequena = this.Height < limiteAltura;

            if (telaPequena)
            {
                // Reduzir fontes
                panelHeader.Font = new Font("Segoe UI", fonteOriginalHeader * 0.7f, FontStyle.Bold);
                lblTime.Font = new Font("Segoe UI", fonteOriginalHeader * 0.7f, FontStyle.Bold);
                lblDate.Font = new Font("Segoe UI", fonteOriginalData * 0.8f);
                lblLocalProducao.Font = new Font("Segoe UI", fonteOriginalTitulo * 0.9f, FontStyle.Bold);
                lblInstrucao.Font = new Font("Segoe UI", fonteOriginalInstrucao * 0.9f, FontStyle.Bold);
                lblStatus.Font = new Font("Segoe UI", fonteOriginalStatus * 0.9f);

                // Reduzir painéis
                panelHeader.Size = new Size(tamanhoOriginalPanelHeader.Width, (int)(tamanhoOriginalPanelHeader.Height * 0.7));
                panelSimulador.Size = new Size(tamanhoOriginalPanelSimulador.Width, (int)(tamanhoOriginalPanelSimulador.Height * 0.7));
                panelStatusBar.Size = new Size(tamanhoOriginalPanelStatusBar.Width, (int)(tamanhoOriginalPanelStatusBar.Height * 0.7));
            }
            else
            {
                // Restaurar fontes
                panelHeader.Font = new Font("Segoe UI", fonteOriginalHeader, FontStyle.Bold);
                lblTime.Font = new Font("Segoe UI", fonteOriginalHeader, FontStyle.Bold);
                lblDate.Font = new Font("Segoe UI", fonteOriginalData);
                lblLocalProducao.Font = new Font("Segoe UI", fonteOriginalTitulo, FontStyle.Bold);
                lblInstrucao.Font = new Font("Segoe UI", fonteOriginalInstrucao, FontStyle.Bold);
                lblStatus.Font = new Font("Segoe UI", fonteOriginalStatus);

                // Restaurar painéis
                panelHeader.Size = tamanhoOriginalPanelHeader;
                panelSimulador.Size = tamanhoOriginalPanelSimulador;
                panelStatusBar.Size = tamanhoOriginalPanelStatusBar;
            }
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
            
            // Não centralizar manualmente a barra de status, pois ela está dockada
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

                // SINCRONIZAR BIOMETRIAS NA PRIMEIRA INSTALAÇÃO (logo após setor selecionado)
                LogToFile("[SETOR-SELECIONADO] 🔍 Verificando se é primeira instalação para sincronização inicial...");
                if (database.EhPrimeiraInstalacao() && neonHelper != null)
                {
                    LogToFile("[SETOR-SELECIONADO] 📥 Primeira instalação detectada! Iniciando sincronização...");
                    await ExecutarSincronizacaoInicial();
                }

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

        private async Task ExecutarSincronizacaoInicial()
        {
            // Criar e exibir formulário de progresso
            BiometriaSyncProgressForm syncProgressForm = null;
            this.Invoke(() =>
            {
                syncProgressForm = new BiometriaSyncProgressForm();
                syncProgressForm.Show(this);
            });

            try
            {
                LogToFile("[SINC-INICIAL] 📡 Chamando BaixarTodasBiometriasParaSincAsync()...");
                var biometriasDoNeon = await neonHelper!.BaixarTodasBiometriasParaSincAsync();
                LogToFile($"[SINC-INICIAL] ✅ Download concluído: {biometriasDoNeon?.Count ?? 0} biometrias recebidas");

                if (biometriasDoNeon != null && biometriasDoNeon.Count > 0)
                {
                    LogToFile($"[SINC-INICIAL] 💾 Salvando {biometriasDoNeon.Count} biometrias no banco local...");
                    int totalInseridas = await database.SalvarBiometriasEmLoteAsync(biometriasDoNeon);
                    LogToFile($"[SINC-INICIAL] ✅ {totalInseridas} biometrias salvas no banco local");

                    if (syncProgressForm != null)
                    {
                        this.Invoke(() => { syncProgressForm.SetSuccess(totalInseridas); });
                    }
                    await Task.Delay(2500);
                }
                else
                {
                    LogToFile("[SINC-INICIAL] ⚠️ Nenhuma biometria encontrada no servidor para sincronizar");
                    if (syncProgressForm != null)
                    {
                        this.Invoke(() =>
                        {
                            syncProgressForm.SetWarning(
                                "Nenhuma biometria foi encontrada no servidor para sincronizar.\n" +
                                "O sistema está pronto para uso (banco local vazio)."
                            );
                        });
                    }
                    await Task.Delay(2500);
                }
            }
            catch (Exception syncEx)
            {
                LogToFile($"[SINC-INICIAL] ❌ ERRO ao sincronizar: {syncEx.GetType().Name}: {syncEx.Message}");
                if (syncProgressForm != null)
                {
                    this.Invoke(() =>
                    {
                        syncProgressForm.SetError(
                            $"{syncEx.GetType().Name}: {syncEx.Message}\n\n" +
                            "O sistema continuará funcionando com o banco local."
                        );
                    });
                }
                await Task.Delay(3000);
            }
            finally
            {
                this.Invoke(() =>
                {
                    syncProgressForm?.Close();
                    syncProgressForm?.Dispose();
                });
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
                    AgendarLimpezaPainel();
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

                    // Decidir o tipo do próximo ponto com base na tolerância e plantão noturno
                    string tipoRegistro = database.DecidirTipoProximoPonto(matchedCooperadoId, 14, 16);
                    LogToFile($"   Tipo de registro: {tipoRegistro} (lógica tolerância/plantão)");

                    // Bloqueio: se última ENTRADA foi há menos de 1 hora, não permite SAÍDA
                    if (tipoRegistro == "SAIDA")
                    {
                        var ultimaEntradaDt = database.ObterTimestampUltimaEntrada(matchedCooperadoId);
                        if (ultimaEntradaDt != null)
                        {
                            var agora = DateTimeOffset.Now;
                            var diff = (agora - ultimaEntradaDt.Value).TotalMinutes;
                            if (diff <= 60)
                            {
                                // Exibir alerta amarelo
                                panelSimulador.BackColor = System.Drawing.Color.FromArgb(255, 255, 200); // Amarelo claro
                                lblSimulador.Text = $"⚠️ {matchedCooperadoNome}, você já possui um registro de ENTRADA às {ultimaEntradaDt:HH:mm}.";
                                lblSimulador.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold);
                                lblSimulador.ForeColor = System.Drawing.Color.FromArgb(180, 120, 0); // Amarelo escuro
                                lblSimulador.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
                                lblStatus.Text = $"⚠️ ENTRADA recente - {matchedCooperadoNome}";
                                
                                // Agendar limpeza automática
                                AgendarLimpezaPainel();
                                return;
                            }
                        }
                    }

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
                        AgendarLimpezaPainel();
                    }
                }
                else
                {
                    LogToFile("❌ Nenhuma biometria correspondente encontrada");
                    lblStatus.Text = "❌ Digital não reconhecida";
                    panelSimulador.BackColor = System.Drawing.Color.FromArgb(255, 200, 200); // Vermelho claro
                    lblSimulador.Text = "❌ Digital não reconhecida!\n\nCooperado não cadastrado no sistema.";
                    lblSimulador.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold);
                    lblSimulador.ForeColor = System.Drawing.Color.FromArgb(200, 0, 0); // Vermelho escuro
                    lblSimulador.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
                    cmbSetor.SelectedIndex = -1;
                    AgendarLimpezaPainel();
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
                string logRoot = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) ?? "C:\\Temp";
                if (string.IsNullOrEmpty(logRoot)) logRoot = "C:\\Temp";
                string logDir = System.IO.Path.Combine(logRoot, "BiometricSystem");
                System.IO.Directory.CreateDirectory(logDir);
                string logPath = System.IO.Path.Combine(logDir, "biometric_log.txt");
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
            
            // Agendar limpeza automática do painel
            AgendarLimpezaPainel();
        }
        
        
        /// <summary>
        /// Agenda a limpeza automática do painel após 5 segundos
        /// </summary>
        private void AgendarLimpezaPainel()
        {
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
                            LimparPainelSimulador();
                        }
                        catch (Exception ex)
                        {
                            LogToFile($"❌ Erro ao limpar painel: {ex.Message}");
                        }
                    }));
                }
                catch (Exception ex)
                {
                    LogToFile($"❌ Erro na thread de limpeza: {ex.Message}");
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
                            biometria.FingerIndex,
                            biometria.CooperadoNome
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

        protected override async void OnFormClosing(FormClosingEventArgs e)
        {

            // Se for retorno do menu de produção, só trava/maximiza e não pede autenticação
            if (VoltarDaProducao)
            {
                VoltarDaProducao = false;
                this.WindowState = FormWindowState.Maximized;
                this.TopMost = true;
                e.Cancel = true;
                // Aqui pode travar a tela se necessário
                return;
            }

            // Se AllowClose for true, permite fechar sem autenticação
            if (AllowClose)
            {
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

            // Prompt de autenticação administrativa
            e.Cancel = true;
            var authDialog = new AuthDialogForm(async (pass) =>
            {
                // Permite autenticar com senha local OU senha Neon
                if (database.ValidarSenhaLocal(pass))
                    return true;
                return neonHelper != null && await neonHelper.ValidarManagerByPasswordAsync(pass);
            });
            authDialog.TopMost = true;
            authDialog.BringToFront();
            this.TopMost = false; // Garante que o dialog fique acima
            authDialog.FormClosed += async (s, args) =>
            {
                this.TopMost = true; // Restaura prioridade
                if (authDialog.AuthSuccess)
                {
                    LogToFile("[SINC-INICIAL] ✅ Abrindo AccessMenuForm...");
                    var menu = new AccessMenuForm(this);
                    menu.TopMost = true;
                    menu.Show();
                    this.Hide();
                    menu.FormClosed += (ms, ma) => {
                        Application.Exit();
                    };
                }
                // Se não autenticou, volta para tela de produção (LoginForm permanece visível)
            };
            authDialog.Show();
        }

        // Impede movimentação da janela
        protected override void WndProc(ref Message m)
        {
		if (m.Msg == WM_NCLBUTTONDOWN && m.WParam.ToInt32() == HTCAPTION)
		{
			// Bloqueia o arrastar da janela
			return;
		}
		base.WndProc(ref m);
	}
}
}

