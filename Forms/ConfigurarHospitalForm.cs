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

                if (hospitais.Count == 0)
                {
                    MessageBox.Show(
                        "Nenhum hospital encontrado no banco de dados.\n\nVerifique a conexão com o NEON.",
                        "Erro",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
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
                    $"Erro ao carregar hospitais:\n{ex.Message}",
                    "Erro",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
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
            string appSettingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");

            // Ler o arquivo atual
            string json = File.ReadAllText(appSettingsPath);
            var config = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

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
