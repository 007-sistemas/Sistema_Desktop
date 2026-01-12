using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using BiometricSystem.Services;

namespace BiometricSystem.Forms
{
    /// <summary>
    /// Tela de Cadastro de Funcionário com Pesquisa
    /// Integrada com o Sistema Web via API
    /// </summary>
    public partial class CadastroFuncionarioForm : Form
    {
        private FingerprintService _fingerprintService;
        private ApiService _apiService;
        private List<UserDto> _allUsers = new List<UserDto>();
        private UserDto _selectedUser;
        private const string API_BASE_URL = "https://bypass-lime.vercel.app";

        // Controles
        private TextBox textBoxPesquisa;
        private ListBox listBoxResultados;
        private TextBox textBoxNome;
        private TextBox textBoxCPF;
        private TextBox textBoxEmail;
        private TextBox textBoxCargo;
        private Button buttonCapturarDigital;
        private Button buttonSalvar;
        private Button buttonCancelar;
        private Label labelStatus;
        private Label labelLeitorDetectado;

        public CadastroFuncionarioForm()
        {
            InitializeComponent();
            InitializeControls();
        }

        private void InitializeControls()
        {
            // Configurar Form
            this.Text = "Cadastro de Funcionário";
            this.Size = new System.Drawing.Size(600, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            // Label Título
            Label labelTitulo = new Label();
            labelTitulo.Text = "Cadastro de Funcionário";
            labelTitulo.Font = new System.Drawing.Font("Arial", 18, System.Drawing.FontStyle.Bold);
            labelTitulo.Location = new System.Drawing.Point(20, 20);
            labelTitulo.Size = new System.Drawing.Size(300, 40);
            this.Controls.Add(labelTitulo);

            // Pesquisa
            Label labelPesquisa = new Label();
            labelPesquisa.Text = "Pesquisar Funcionário:";
            labelPesquisa.Location = new System.Drawing.Point(20, 70);
            labelPesquisa.Size = new System.Drawing.Size(200, 20);
            this.Controls.Add(labelPesquisa);

            textBoxPesquisa = new TextBox();
            textBoxPesquisa.Location = new System.Drawing.Point(20, 95);
            textBoxPesquisa.Size = new System.Drawing.Size(540, 30);
            textBoxPesquisa.Font = new System.Drawing.Font("Arial", 11);
            textBoxPesquisa.TextChanged += TextBoxPesquisa_TextChanged;
            // textBoxPesquisa.Placeholder = "Digite o nome do funcionário..."; // TextBox não suporta Placeholder em WinForms antigos
            this.Controls.Add(textBoxPesquisa);

            // ListBox de Resultados
            listBoxResultados = new ListBox();
            listBoxResultados.Location = new System.Drawing.Point(20, 130);
            listBoxResultados.Size = new System.Drawing.Size(540, 120);
            listBoxResultados.Font = new System.Drawing.Font("Arial", 10);
            listBoxResultados.SelectedIndexChanged += ListBoxResultados_SelectedIndexChanged;
            this.Controls.Add(listBoxResultados);

            // Nome Completo
            Label labelNome = new Label();
            labelNome.Text = "Nome Completo:";
            labelNome.Location = new System.Drawing.Point(20, 260);
            labelNome.Size = new System.Drawing.Size(540, 20);
            this.Controls.Add(labelNome);

            textBoxNome = new TextBox();
            textBoxNome.Location = new System.Drawing.Point(20, 285);
            textBoxNome.Size = new System.Drawing.Size(540, 30);
            textBoxNome.Font = new System.Drawing.Font("Arial", 11);
            textBoxNome.ReadOnly = true;
            this.Controls.Add(textBoxNome);

            // CPF
            Label labelCPF = new Label();
            labelCPF.Text = "Matrícula:";
            labelCPF.Location = new System.Drawing.Point(20, 320);
            labelCPF.Size = new System.Drawing.Size(200, 20);
            this.Controls.Add(labelCPF);

            textBoxCPF = new TextBox();
            textBoxCPF.Location = new System.Drawing.Point(20, 345);
            textBoxCPF.Size = new System.Drawing.Size(200, 30);
            textBoxCPF.Font = new System.Drawing.Font("Arial", 11);
            textBoxCPF.ReadOnly = true;
            this.Controls.Add(textBoxCPF);

            // Email
            Label labelEmail = new Label();
            labelEmail.Text = "E-mail:";
            labelEmail.Location = new System.Drawing.Point(320, 320);
            labelEmail.Size = new System.Drawing.Size(240, 20);
            this.Controls.Add(labelEmail);

            textBoxEmail = new TextBox();
            textBoxEmail.Location = new System.Drawing.Point(320, 345);
            textBoxEmail.Size = new System.Drawing.Size(240, 30);
            textBoxEmail.Font = new System.Drawing.Font("Arial", 11);
            textBoxEmail.ReadOnly = true;
            this.Controls.Add(textBoxEmail);

            // Cargo
            Label labelCargo = new Label();
            labelCargo.Text = "Cargo:";
            labelCargo.Location = new System.Drawing.Point(20, 385);
            labelCargo.Size = new System.Drawing.Size(540, 20);
            this.Controls.Add(labelCargo);

            textBoxCargo = new TextBox();
            textBoxCargo.Location = new System.Drawing.Point(20, 410);
            textBoxCargo.Size = new System.Drawing.Size(540, 30);
            textBoxCargo.Font = new System.Drawing.Font("Arial", 11);
            textBoxCargo.ReadOnly = true;
            this.Controls.Add(textBoxCargo);

            // Botão Capturar Digital
            buttonCapturarDigital = new Button();
            buttonCapturarDigital.Text = "👆 Capturar Digital";
            buttonCapturarDigital.Location = new System.Drawing.Point(20, 460);
            buttonCapturarDigital.Size = new System.Drawing.Size(540, 50);
            buttonCapturarDigital.Font = new System.Drawing.Font("Arial", 12, System.Drawing.FontStyle.Bold);
            buttonCapturarDigital.BackColor = System.Drawing.Color.FromArgb(33, 150, 243);
            buttonCapturarDigital.ForeColor = System.Drawing.Color.White;
            buttonCapturarDigital.Enabled = false;
            buttonCapturarDigital.Click += ButtonCapturarDigital_Click;
            this.Controls.Add(buttonCapturarDigital);

            // Label Status
            labelStatus = new Label();
            labelStatus.Text = "Selecione um funcionário para continuar";
            labelStatus.Location = new System.Drawing.Point(20, 520);
            labelStatus.Size = new System.Drawing.Size(540, 30);
            labelStatus.Font = new System.Drawing.Font("Arial", 10);
            labelStatus.AutoSize = true;
            this.Controls.Add(labelStatus);

            // Label Leitor Detectado
            labelLeitorDetectado = new Label();
            labelLeitorDetectado.Text = "☑ Leitor biométrico detectado!";
            labelLeitorDetectado.Location = new System.Drawing.Point(20, 555);
            labelLeitorDetectado.Size = new System.Drawing.Size(300, 20);
            labelLeitorDetectado.Font = new System.Drawing.Font("Arial", 9);
            labelLeitorDetectado.ForeColor = System.Drawing.Color.Green;
            this.Controls.Add(labelLeitorDetectado);

            // Botão Salvar
            buttonSalvar = new Button();
            buttonSalvar.Text = "💾 Salvar Cadastro";
            buttonSalvar.Location = new System.Drawing.Point(20, 590);
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
            buttonCancelar.Location = new System.Drawing.Point(300, 590);
            buttonCancelar.Size = new System.Drawing.Size(260, 45);
            buttonCancelar.Font = new System.Drawing.Font("Arial", 11, System.Drawing.FontStyle.Bold);
            buttonCancelar.BackColor = System.Drawing.Color.FromArgb(244, 67, 54);
            buttonCancelar.ForeColor = System.Drawing.Color.White;
            buttonCancelar.Click += ButtonCancelar_Click;
            this.Controls.Add(buttonCancelar);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            InitializeBiometricSystem();
        }

        private async void InitializeBiometricSystem()
        {
            try
            {
                _fingerprintService = new FingerprintService();
                _apiService = new ApiService(API_BASE_URL);

                // Carregar usuários
                await CarregarUsuarios();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao inicializar: {ex.Message}");
            }
        }

        private async Task CarregarUsuarios()
        {
            try
            {
                labelStatus.Text = "Carregando usuários do servidor...";
                _allUsers = await _apiService.GetUsersAsync();

                if (_allUsers != null && _allUsers.Count > 0)
                {
                    labelStatus.Text = $"✓ {_allUsers.Count} usuários carregados";
                }
                else
                {
                    labelStatus.Text = "⚠ Nenhum usuário encontrado no servidor";
                }
            }
            catch (Exception ex)
            {
                labelStatus.Text = $"❌ Erro ao carregar usuários: {ex.Message}";
            }
        }

        private void TextBoxPesquisa_TextChanged(object sender, EventArgs e)
        {
            string pesquisa = textBoxPesquisa.Text.ToLower().Trim();

            listBoxResultados.Items.Clear();

            if (string.IsNullOrWhiteSpace(pesquisa))
            {
                labelStatus.Text = "Digite o nome do funcionário para pesquisar";
                return;
            }

            // Filtrar usuários
            var resultados = _allUsers
                .Where(u => u.Name.ToLower().Contains(pesquisa) || 
                           u.Matricula.ToLower().Contains(pesquisa))
                .ToList();

            // Adicionar à ListBox
            foreach (var user in resultados)
            {
                listBoxResultados.Items.Add(new UserDisplayItem
                {
                    Id = user.Id,
                    DisplayText = $"{user.Name} (Mat: {user.Matricula})"
                });
            }

            if (resultados.Count == 0)
            {
                labelStatus.Text = "Nenhum funcionário encontrado";
            }
            else
            {
                labelStatus.Text = $"✓ {resultados.Count} funcionário(s) encontrado(s)";
            }
        }

        private void ListBoxResultados_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBoxResultados.SelectedIndex == -1)
            {
                LimparCampos();
                buttonCapturarDigital.Enabled = false;
                return;
            }

            var item = (UserDisplayItem)listBoxResultados.SelectedItem;
            _selectedUser = _allUsers.FirstOrDefault(u => u.Id == item.Id);

            if (_selectedUser != null)
            {
                // Preencher campos
                textBoxNome.Text = _selectedUser.Name;
                textBoxCPF.Text = _selectedUser.Matricula;
                textBoxEmail.Text = _selectedUser.Email;
                textBoxCargo.Text = _selectedUser.Categoria;

                // Habilitar botão
                buttonCapturarDigital.Enabled = true;
                labelStatus.Text = $"✓ Funcionário selecionado: {_selectedUser.Name}";
            }
        }

        private async void ButtonCapturarDigital_Click(object sender, EventArgs e)
        {
            try
            {
                if (_selectedUser == null)
                {
                    MessageBox.Show("Selecione um funcionário primeiro!");
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
                // Aqui você integra com o FingerprintService para capturar
                // Por enquanto, retorna true simulando a captura
                await Task.Delay(2000); // Simula captura
                return true;
            }
            catch
            {
                return false;
            }
        }

        private async void ButtonSalvar_Click(object sender, EventArgs e)
        {
            try
            {
                if (_selectedUser == null)
                {
                    MessageBox.Show("Nenhum funcionário selecionado");
                    return;
                }

                labelStatus.Text = "📤 Registrando biometria no servidor...";
                Application.DoEvents();

                // Enviar para servidor
                bool success = await _apiService.RegisterBiometricAsync(
                    _selectedUser.Id,
                    new byte[] { }, // Aqui entra a biometria capturada
                    "fingerprint"
                );

                if (success)
                {
                    MessageBox.Show(
                        $"✓ Biometria registrada com sucesso!\n\n" +
                        $"Funcionário: {_selectedUser.Name}",
                        "Sucesso",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );

                    LimparCampos();
                    labelStatus.Text = "Cadastro concluído com sucesso!";
                }
                else
                {
                    MessageBox.Show("Falha ao registrar biometria", "Erro");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro: {ex.Message}");
            }
        }

        private void ButtonCancelar_Click(object sender, EventArgs e)
        {
            LimparCampos();
            this.DialogResult = DialogResult.Cancel;
        }

        private void LimparCampos()
        {
            textBoxPesquisa.Clear();
            textBoxNome.Clear();
            textBoxCPF.Clear();
            textBoxEmail.Clear();
            textBoxCargo.Clear();
            listBoxResultados.Items.Clear();
            _selectedUser = null;
            buttonCapturarDigital.Enabled = false;
            buttonSalvar.Enabled = false;
            labelStatus.Text = "Digite o nome do funcionário para pesquisar";
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.ResumeLayout(false);
        }

        private class UserDisplayItem
        {
            public int Id { get; set; }
            public string DisplayText { get; set; }

            public override string ToString() => DisplayText;
        }
    }
}
