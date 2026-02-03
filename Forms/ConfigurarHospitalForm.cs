using BiometricSystem.Database;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Text.Json;

namespace BiometricSystem.Forms
{
    public class ConfigurarHospitalForm : Form
    {
        private ComboBox cmbHospitais;
        private Button btnConfirmar;
        private Button btnCancelar;
        private Label lblTitulo;
        private Label lblDescricao;
        private NeonCooperadoHelper neonHelper;
        private List<NeonCooperadoHelper.Hospital> hospitais;

        public ConfigurarHospitalForm(string connectionString)
        {
            neonHelper = new NeonCooperadoHelper(connectionString);
            InitializeComponents();
            CarregarHospitais();
        }

        private void InitializeComponents()
        {
            // Configuração do Form
            this.Text = "Configuração Inicial - Hospital";
            this.Size = new Size(500, 300);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.White;

            // Título
            lblTitulo = new Label
            {
                Text = "🏥 Configuração do Hospital",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 120, 215),
                Location = new Point(20, 20),
                Size = new Size(450, 40),
                TextAlign = ContentAlignment.MiddleCenter
            };

            // Descrição
            lblDescricao = new Label
            {
                Text = "Selecione o hospital que este sistema representará.\nTodos os registros de ponto serão vinculados a ele.",
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(60, 60, 60),
                Location = new Point(20, 70),
                Size = new Size(450, 50),
                TextAlign = ContentAlignment.TopCenter
            };

            // ComboBox Hospitais
            cmbHospitais = new ComboBox
            {
                Location = new Point(50, 140),
                Size = new Size(400, 30),
                Font = new Font("Segoe UI", 11F),
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            // Botão Confirmar
            btnConfirmar = new Button
            {
                Text = "✓ Confirmar",
                Location = new Point(160, 200),
                Size = new Size(120, 40),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Enabled = false
            };
            btnConfirmar.FlatAppearance.BorderSize = 0;
            btnConfirmar.Click += BtnConfirmar_Click;

            // Botão Cancelar
            btnCancelar = new Button
            {
                Text = "✕ Cancelar",
                Location = new Point(290, 200),
                Size = new Size(120, 40),
                Font = new Font("Segoe UI", 10F),
                BackColor = Color.FromArgb(220, 220, 220),
                ForeColor = Color.FromArgb(60, 60, 60),
                FlatStyle = FlatStyle.Flat
            };
            btnCancelar.FlatAppearance.BorderSize = 0;
            btnCancelar.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            // Evento para habilitar botão quando selecionar
            cmbHospitais.SelectedIndexChanged += (s, e) =>
            {
                btnConfirmar.Enabled = cmbHospitais.SelectedIndex >= 0;
            };

            // Adicionar controles ao form
            this.Controls.Add(lblTitulo);
            this.Controls.Add(lblDescricao);
            this.Controls.Add(cmbHospitais);
            this.Controls.Add(btnConfirmar);
            this.Controls.Add(btnCancelar);
        }

        private async void CarregarHospitais()
        {
            try
            {
                lblDescricao.Text = "⏳ Carregando hospitais...";
                cmbHospitais.Enabled = false;
                Application.DoEvents();

                hospitais = await neonHelper.GetHospitaisAsync();

                if (hospitais == null || hospitais.Count == 0)
                {
                    MessageBox.Show(
                        "Nenhum hospital encontrado no banco de dados.\n\n" +
                        "Possíveis causas:\n" +
                        "• Tabela 'hospitais' está vazia no NEON\n" +
                        "• Problema na conexão com o banco de dados\n" +
                        "• Credenciais inválidas\n\n" +
                        "Verifique o arquivo de log (biometric_log.txt) para mais detalhes.",
                        "Erro",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                    this.DialogResult = DialogResult.Cancel;
                    this.Close();
                    return;
                }

                cmbHospitais.DataSource = hospitais;
                cmbHospitais.DisplayMember = "Nome";
                cmbHospitais.ValueMember = "Id";
                cmbHospitais.SelectedIndex = -1;
                cmbHospitais.Enabled = true;

                lblDescricao.Text = "Selecione o hospital que este sistema representará.\nTodos os registros de ponto serão vinculados a ele.";
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erro ao carregar hospitais:\n\n{ex.Message}\n\n" +
                    $"Stack trace:\n{ex.StackTrace?.Substring(0, Math.Min(200, ex.StackTrace?.Length ?? 0))}\n\n" +
                    "Verifique o arquivo de log (biometric_log.txt) para mais detalhes.",
                    "Erro",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            }
        }

        private void BtnConfirmar_Click(object sender, EventArgs e)
        {
            try
            {
                if (cmbHospitais.SelectedItem == null)
                {
                    MessageBox.Show("Selecione um hospital.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var hospitalSelecionado = (NeonCooperadoHelper.Hospital)cmbHospitais.SelectedItem;

                // Salvar no appsettings.json
                SalvarConfiguracao(hospitalSelecionado);

                MessageBox.Show(
                    $"Hospital configurado com sucesso!\n\nHospital: {hospitalSelecionado.Nome}",
                    "Sucesso",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erro ao salvar configuração:\n{ex.Message}",
                    "Erro",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        private void SalvarConfiguracao(NeonCooperadoHelper.Hospital hospital)
        {
            // Caminho seguro para configuração do usuário
            string? appDataRoot = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            if (string.IsNullOrEmpty(appDataRoot))
            {
                MessageBox.Show(
                    "Não foi possível localizar a pasta de dados do usuário (APPDATA). Entre em contato com o suporte.",
                    "Erro de Inicialização",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                throw new InvalidOperationException("APPDATA não encontrado");
            }
            string appDataDir = Path.Combine(appDataRoot, "BiometricSystem");
            Directory.CreateDirectory(appDataDir);
            string appSettingsPath = Path.Combine(appDataDir, "appsettings.json");
            // Proteção extra: nunca gravar fora do AppData
            if (!appSettingsPath.ToLower().Contains("appdata"))
            {
                MessageBox.Show($"Tentativa de gravação fora do AppData bloqueada!\nCaminho: {appSettingsPath}", "Proteção de Segurança", MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw new UnauthorizedAccessException($"Tentativa de gravação fora do AppData: {appSettingsPath}");
            }

            // Se não existir, criar um novo arquivo padrão completo
            if (!File.Exists(appSettingsPath))
            {
                var defaultConfig = new Dictionary<string, object?>
                {
                    { "Neon", new Dictionary<string, string> {
                        { "ConnectionString", "Host=ep-dry-dawn-ahl0dlm6-pooler.c-3.us-east-1.aws.neon.tech;Database=neondb;Username=neondb_owner;Password=npg_lOhyE4z1QBtc;SSL Mode=Require;Trust Server Certificate=true" }
                    } },
                    { "Hospital", new Dictionary<string, string>() },
                    { "Logging", new Dictionary<string, object> {
                        { "LogLevel", new Dictionary<string, string> { { "Default", "Information" } } }
                    } }
                };
                var optionsDefault = new JsonSerializerOptions { WriteIndented = true };
                string defaultJson = JsonSerializer.Serialize(defaultConfig, optionsDefault);
                File.WriteAllText(appSettingsPath, defaultJson);
            }

            // Ler o arquivo atual
            string json = File.ReadAllText(appSettingsPath);
            var config = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
            if (config == null) config = new Dictionary<string, JsonElement>();

            // Atualizar seção Hospital
            var hospitalConfig = new Dictionary<string, string>
            {
                { "Id", hospital.Id },
                { "Nome", hospital.Nome },
                { "Codigo", hospital.Codigo }
            };

            config["Hospital"] = JsonSerializer.SerializeToElement(hospitalConfig);

            // Salvar de volta
            var options = new JsonSerializerOptions { WriteIndented = true };
            string newJson = JsonSerializer.Serialize(config, options);
            File.WriteAllText(appSettingsPath, newJson);
        }
    }
}
