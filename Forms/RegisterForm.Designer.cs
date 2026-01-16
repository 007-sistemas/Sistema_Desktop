namespace BiometricSystem.Forms
{
    partial class RegisterForm
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
            this.lblTitle = new System.Windows.Forms.Label();
            this.lblName = new System.Windows.Forms.Label();
            this.txtName = new System.Windows.Forms.TextBox();
            this.lblCPF = new System.Windows.Forms.Label();
            this.txtCPF = new System.Windows.Forms.MaskedTextBox();
            this.lblEmail = new System.Windows.Forms.Label();
            this.txtEmail = new System.Windows.Forms.TextBox();
            this.lblPosition = new System.Windows.Forms.Label();
            this.txtPosition = new System.Windows.Forms.TextBox();
            this.btnCaptureFingerprint = new System.Windows.Forms.Button();
            this.lblFingerprintStatus = new System.Windows.Forms.Label();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.panelFingerprint = new System.Windows.Forms.Panel();
            this.SuspendLayout();
            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 18F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(99)))), ((int)(((byte)(235)))));
            this.lblTitle.Location = new System.Drawing.Point(30, 30);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(320, 32);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "Cadastro de Funcionário";
            // 
            // lblName
            // 
            this.lblName.AutoSize = true;
            this.lblName.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.lblName.Location = new System.Drawing.Point(30, 90);
            this.lblName.Name = "lblName";
            this.lblName.Size = new System.Drawing.Size(110, 19);
            this.lblName.TabIndex = 1;
            this.lblName.Text = "Nome Completo";
            // 
            // txtName
            // 
            this.txtName.Font = new System.Drawing.Font("Segoe UI", 11F);
            this.txtName.Location = new System.Drawing.Point(30, 115);
            this.txtName.Name = "txtName";
            this.txtName.Size = new System.Drawing.Size(500, 27);
            this.txtName.TabIndex = 2;
            // 
            // lblCPF
            // 
            this.lblCPF.AutoSize = true;
            this.lblCPF.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.lblCPF.Location = new System.Drawing.Point(30, 160);
            this.lblCPF.Name = "lblCPF";
            this.lblCPF.Size = new System.Drawing.Size(33, 19);
            this.lblCPF.TabIndex = 3;
            this.lblCPF.Text = "CPF";
            // 
            // txtCPF
            // 
            this.txtCPF.Font = new System.Drawing.Font("Segoe UI", 11F);
            this.txtCPF.Location = new System.Drawing.Point(30, 185);
            this.txtCPF.Mask = "000.000.000-00";
            this.txtCPF.Name = "txtCPF";
            this.txtCPF.Size = new System.Drawing.Size(240, 27);
            this.txtCPF.TabIndex = 4;
            // 
            // lblEmail
            // 
            this.lblEmail.AutoSize = true;
            this.lblEmail.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.lblEmail.Location = new System.Drawing.Point(290, 160);
            this.lblEmail.Name = "lblEmail";
            this.lblEmail.Size = new System.Drawing.Size(46, 19);
            this.lblEmail.TabIndex = 5;
            this.lblEmail.Text = "E-mail";
            // 
            // txtEmail
            // 
            this.txtEmail.Font = new System.Drawing.Font("Segoe UI", 11F);
            this.txtEmail.Location = new System.Drawing.Point(290, 185);
            this.txtEmail.Name = "txtEmail";
            this.txtEmail.Size = new System.Drawing.Size(240, 27);
            this.txtEmail.TabIndex = 6;
            // 
            // lblPosition
            // 
            this.lblPosition.AutoSize = true;
            this.lblPosition.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.lblPosition.Location = new System.Drawing.Point(30, 230);
            this.lblPosition.Name = "lblPosition";
            this.lblPosition.Size = new System.Drawing.Size(42, 19);
            this.lblPosition.TabIndex = 7;
            this.lblPosition.Text = "Cargo";
            // 
            // txtPosition
            // 
            this.txtPosition.Font = new System.Drawing.Font("Segoe UI", 11F);
            this.txtPosition.Location = new System.Drawing.Point(30, 255);
            this.txtPosition.Name = "txtPosition";
            this.txtPosition.Size = new System.Drawing.Size(500, 27);
            this.txtPosition.TabIndex = 8;
            // 
            // btnCaptureFingerprint
            // 
            this.btnCaptureFingerprint.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(99)))), ((int)(((byte)(235)))));
            this.btnCaptureFingerprint.FlatAppearance.BorderSize = 0;
            this.btnCaptureFingerprint.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCaptureFingerprint.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.btnCaptureFingerprint.ForeColor = System.Drawing.Color.White;
            this.btnCaptureFingerprint.Location = new System.Drawing.Point(30, 320);
            this.btnCaptureFingerprint.Name = "btnCaptureFingerprint";
            this.btnCaptureFingerprint.Size = new System.Drawing.Size(500, 45);
            this.btnCaptureFingerprint.TabIndex = 9;
            this.btnCaptureFingerprint.Text = "👆 Capturar Digital";
            this.btnCaptureFingerprint.UseVisualStyleBackColor = false;
            this.btnCaptureFingerprint.Click += new System.EventHandler(this.btnCaptureFingerprint_Click);
            // 
            // lblFingerprintStatus
            // 
            this.lblFingerprintStatus.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.lblFingerprintStatus.ForeColor = System.Drawing.Color.Gray;
            this.lblFingerprintStatus.Location = new System.Drawing.Point(30, 380);
            this.lblFingerprintStatus.Name = "lblFingerprintStatus";
            this.lblFingerprintStatus.Size = new System.Drawing.Size(500, 50);
            this.lblFingerprintStatus.TabIndex = 10;
            this.lblFingerprintStatus.Text = "Digital não capturada";
            this.lblFingerprintStatus.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // btnSave
            // 
            this.btnSave.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(34)))), ((int)(((byte)(197)))), ((int)(((byte)(94)))));
            this.btnSave.Enabled = false;
            this.btnSave.FlatAppearance.BorderSize = 0;
            this.btnSave.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSave.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.btnSave.ForeColor = System.Drawing.Color.White;
            this.btnSave.Location = new System.Drawing.Point(30, 460);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(240, 45);
            this.btnSave.TabIndex = 11;
            this.btnSave.Text = "💾 Salvar Cadastro";
            this.btnSave.UseVisualStyleBackColor = false;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(239)))), ((int)(((byte)(68)))), ((int)(((byte)(68)))));
            this.btnCancel.FlatAppearance.BorderSize = 0;
            this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCancel.Font = new System.Drawing.Font("Segoe UI", 11F);
            this.btnCancel.ForeColor = System.Drawing.Color.White;
            this.btnCancel.Location = new System.Drawing.Point(290, 460);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(240, 45);
            this.btnCancel.TabIndex = 12;
            this.btnCancel.Text = "❌ Cancelar";
            this.btnCancel.UseVisualStyleBackColor = false;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // panelFingerprint
            // 
            this.panelFingerprint.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.panelFingerprint.Location = new System.Drawing.Point(190, 380);
            this.panelFingerprint.Name = "panelFingerprint";
            this.panelFingerprint.Size = new System.Drawing.Size(180, 50);
            this.panelFingerprint.TabIndex = 13;
            this.panelFingerprint.Visible = false;
            // 
            // RegisterForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(560, 540);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.lblFingerprintStatus);
            this.Controls.Add(this.btnCaptureFingerprint);
            this.Controls.Add(this.txtPosition);
            this.Controls.Add(this.lblPosition);
            this.Controls.Add(this.txtEmail);
            this.Controls.Add(this.lblEmail);
            this.Controls.Add(this.txtCPF);
            this.Controls.Add(this.lblCPF);
            this.Controls.Add(this.txtName);
            this.Controls.Add(this.lblName);
            this.Controls.Add(this.lblTitle);
            this.Controls.Add(this.panelFingerprint);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "RegisterForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Cadastro de Funcionário";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblName;
        private System.Windows.Forms.TextBox txtName;
        private System.Windows.Forms.Label lblCPF;
        private System.Windows.Forms.MaskedTextBox txtCPF;
        private System.Windows.Forms.Label lblEmail;
        private System.Windows.Forms.TextBox txtEmail;
        private System.Windows.Forms.Label lblPosition;
        private System.Windows.Forms.TextBox txtPosition;
        private System.Windows.Forms.Button btnCaptureFingerprint;
        private System.Windows.Forms.Label lblFingerprintStatus;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Panel panelFingerprint;
    }
}
