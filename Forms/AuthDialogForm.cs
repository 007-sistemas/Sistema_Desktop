using System;
using System.Windows.Forms;

namespace BiometricSystem.Forms
{
    public partial class AuthDialogForm : Form
    {
        public string Username => txtUsername.Text;
        public string Password => txtPassword.Text;
        public bool AuthSuccess { get; private set; } = false;
        private readonly Func<string, string, Task<bool>> _authCallback;

        public AuthDialogForm(Func<string, string, Task<bool>> authCallback)
        {
            InitializeComponent();
            this.TopMost = true;
            this.StartPosition = FormStartPosition.CenterScreen;
            _authCallback = authCallback;
            this.Shown += (s, e) => {
                this.BringToFront();
                this.Activate();
                this.Focus();
                this.txtUsername.Focus();
            };
            this.Activated += (s, e) => {
                this.BringToFront();
                this.Activate();
                this.Focus();
                this.txtUsername.Focus();
            };
            this.btnOk.Click += async (s, e) =>
            {
                this.Enabled = false;
                try
                {
                    bool result = false;
                    try
                    {
                        result = await _authCallback(txtUsername.Text, txtPassword.Text);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Erro ao autenticar: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        AuthSuccess = false;
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
