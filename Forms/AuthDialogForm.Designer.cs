namespace BiometricSystem.Forms
{
    partial class AuthDialogForm
    {
        private System.ComponentModel.IContainer components = null;
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }
        private void InitializeComponent()
        {
            this.txtUsername = new System.Windows.Forms.TextBox();
            this.txtPassword = new System.Windows.Forms.TextBox();
            this.lblUsername = new System.Windows.Forms.Label();
            this.lblPassword = new System.Windows.Forms.Label();
            this.btnOk = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // lblUsername
            // 
            this.lblUsername.Text = "Usuário:";
            this.lblUsername.Location = new System.Drawing.Point(20, 20);
            this.lblUsername.Size = new System.Drawing.Size(80, 20);
            // 
            // txtUsername
            // 
            this.txtUsername.Location = new System.Drawing.Point(110, 20);
            this.txtUsername.Size = new System.Drawing.Size(180, 23);
            // 
            // lblPassword
            // 
            this.lblPassword.Text = "Senha:";
            this.lblPassword.Location = new System.Drawing.Point(20, 60);
            this.lblPassword.Size = new System.Drawing.Size(80, 20);
            // 
            // txtPassword
            // 
            this.txtPassword.Location = new System.Drawing.Point(110, 60);
            this.txtPassword.Size = new System.Drawing.Size(180, 23);
            this.txtPassword.PasswordChar = '*';
            // 
            // btnOk
            // 
            this.btnOk.Text = "OK";
            this.btnOk.Location = new System.Drawing.Point(110, 100);
            this.btnOk.Size = new System.Drawing.Size(80, 30);
            this.btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
            // 
            // btnCancel
            // 
            this.btnCancel.Text = "Cancelar";
            this.btnCancel.Location = new System.Drawing.Point(210, 100);
            this.btnCancel.Size = new System.Drawing.Size(80, 30);
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            // 
            // AuthDialogForm
            // 
            this.AcceptButton = this.btnOk;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(320, 150);
            this.Controls.Add(this.lblUsername);
            this.Controls.Add(this.txtUsername);
            this.Controls.Add(this.lblPassword);
            this.Controls.Add(this.txtPassword);
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.btnCancel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Autenticação Administrativa";
            this.ResumeLayout(false);
            this.PerformLayout();
        }
        private System.Windows.Forms.TextBox txtUsername;
        private System.Windows.Forms.TextBox txtPassword;
        private System.Windows.Forms.Label lblUsername;
        private System.Windows.Forms.Label lblPassword;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.Button btnCancel;
    }
}
