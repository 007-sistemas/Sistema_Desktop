using System;
using System.Windows.Forms;

namespace BiometricSystem.Forms
{
    public partial class AuthDialogForm : Form
    {
        public string Password => txtPassword.Text;
        public bool AuthSuccess { get; private set; } = false;
        private readonly Func<string, System.Threading.Tasks.Task<bool>> _authCallback;

        public AuthDialogForm(Func<string, System.Threading.Tasks.Task<bool>> authCallback)
        {
            InitializeComponent();
            this.TopMost = true;
            this.StartPosition = FormStartPosition.CenterScreen;
            _authCallback = authCallback;
            this.Shown += (s, e) => {
                this.BringToFront();
                this.Activate();
                this.Focus();
                this.txtPassword.Focus();
            };
            this.Activated += (s, e) => {
                this.BringToFront();
                this.Activate();
                this.Focus();
                this.txtPassword.Focus();
            };

            // Eventos do teclado virtual
            this.btnNum0.Click += (s, e) => txtPassword.Text += "0";
            this.btnNum1.Click += (s, e) => txtPassword.Text += "1";
            this.btnNum2.Click += (s, e) => txtPassword.Text += "2";
            this.btnNum3.Click += (s, e) => txtPassword.Text += "3";
            this.btnNum4.Click += (s, e) => txtPassword.Text += "4";
            this.btnNum5.Click += (s, e) => txtPassword.Text += "5";
            this.btnNum6.Click += (s, e) => txtPassword.Text += "6";
            this.btnNum7.Click += (s, e) => txtPassword.Text += "7";
            this.btnNum8.Click += (s, e) => txtPassword.Text += "8";
            this.btnNum9.Click += (s, e) => txtPassword.Text += "9";

            // btnLimpar removido
                this.btnLimparPrincipal.Click += (s, e) => txtPassword.Text = string.Empty;


            // Botão Cancelar fecha o modal
            this.btnCancel.Click += (s, e) =>
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            };

            this.btnOk.Click += async (s, e) =>
            {
                this.Enabled = false;
                try
                {
                    bool result = false;
                    try
                    {
                        result = await _authCallback(txtPassword.Text);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Erro ao autenticar: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        AuthSuccess = false;
                        txtPassword.Text = string.Empty;
                        this.DialogResult = DialogResult.Cancel;
                        this.Enabled = true;
                        return;
                    }
                    if (result)
                    {
                        AuthSuccess = true;
                        MessageBox.Show("Acesso liberado!", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        this.DialogResult = DialogResult.OK;
                        this.Close();
                    }
                    else
                    {
                        AuthSuccess = false;
                        MessageBox.Show("Acesso não autorizado!", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        txtPassword.Text = string.Empty;
                        this.DialogResult = DialogResult.Cancel;
                        this.Enabled = true;
                    }
                }
                finally
                {
                    this.Enabled = true;
                }
            };
        }
    }
}
