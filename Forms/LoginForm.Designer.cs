namespace BiometricSystem.Forms
{
    partial class LoginForm
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
            this.components = new System.ComponentModel.Container();
            
            this.panelHeader = new System.Windows.Forms.Panel();
            this.lblTime = new System.Windows.Forms.Label();
            this.lblDate = new System.Windows.Forms.Label();
            
            this.lblLocalProducao = new System.Windows.Forms.Label();
            this.lblSetorAla = new System.Windows.Forms.Label();
            this.cmbSetor = new System.Windows.Forms.ComboBox();
            this.lblInstrucao = new System.Windows.Forms.Label();
            
            this.panelSimulador = new System.Windows.Forms.Panel();
            this.lblSimulador = new System.Windows.Forms.Label();
            this.btnSimular = new System.Windows.Forms.Button();
            
            this.panelFingerprint = new System.Windows.Forms.Panel();
            this.lblFingerprint = new System.Windows.Forms.Label();
            
            this.lblModoSimulacao = new System.Windows.Forms.Label();
            this.lblStatus = new System.Windows.Forms.Label();
            this.btnRegister = new System.Windows.Forms.Button();
            
            this.timerClock = new System.Windows.Forms.Timer(this.components);
            
            this.panelHeader.SuspendLayout();
            this.panelSimulador.SuspendLayout();
            this.panelFingerprint.SuspendLayout();
            this.SuspendLayout();
            
            // panelHeader
            this.panelHeader.BackColor = System.Drawing.Color.FromArgb(34, 139, 87);
            this.panelHeader.Controls.Add(this.lblTime);
            this.panelHeader.Controls.Add(this.lblDate);
            this.panelHeader.Location = new System.Drawing.Point(60, 20);
            this.panelHeader.Name = "panelHeader";
            this.panelHeader.Padding = new System.Windows.Forms.Padding(30, 20, 30, 20);
            this.panelHeader.Size = new System.Drawing.Size(780, 140);
            this.panelHeader.TabIndex = 0;
            this.panelHeader.BackColor = System.Drawing.Color.FromArgb(16, 118, 128);
            
            // lblTime
            this.lblTime.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblTime.Font = new System.Drawing.Font("Segoe UI", 48F, System.Drawing.FontStyle.Bold);
            this.lblTime.ForeColor = System.Drawing.Color.White;
            this.lblTime.Location = new System.Drawing.Point(30, 20);
            this.lblTime.Name = "lblTime";
            this.lblTime.Size = new System.Drawing.Size(720, 70);
            this.lblTime.TabIndex = 0;
            this.lblTime.Text = "00:00:00";
            this.lblTime.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            
            // lblDate
            this.lblDate.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.lblDate.Font = new System.Drawing.Font("Segoe UI", 12F);
            this.lblDate.ForeColor = System.Drawing.Color.White;
            this.lblDate.Location = new System.Drawing.Point(30, 95);
            this.lblDate.Name = "lblDate";
            this.lblDate.Size = new System.Drawing.Size(720, 25);
            this.lblDate.TabIndex = 1;
            this.lblDate.Text = "segunda-feira, 12 de janeiro de 2026";
            this.lblDate.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            
            // lblLocalProducao
            this.lblLocalProducao.AutoSize = true;
            this.lblLocalProducao.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold);
            this.lblLocalProducao.ForeColor = System.Drawing.Color.FromArgb(100, 50, 150);
            this.lblLocalProducao.Location = new System.Drawing.Point(100, 180);
            this.lblLocalProducao.Name = "lblLocalProducao";
            this.lblLocalProducao.Size = new System.Drawing.Size(180, 25);
            this.lblLocalProducao.TabIndex = 2;
            this.lblLocalProducao.Text = "📍 Local de Produção";
            
            // lblSetorAla
            this.lblSetorAla.AutoSize = true;
            this.lblSetorAla.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.lblSetorAla.ForeColor = System.Drawing.Color.FromArgb(100, 100, 100);
            this.lblSetorAla.Location = new System.Drawing.Point(100, 220);
            this.lblSetorAla.Name = "lblSetorAla";
            this.lblSetorAla.Size = new System.Drawing.Size(90, 19);
            this.lblSetorAla.TabIndex = 3;
            this.lblSetorAla.Text = "🏢 SETOR / ALA";
            
            // cmbSetor
            this.cmbSetor.BackColor = System.Drawing.Color.White;
            this.cmbSetor.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbSetor.Font = new System.Drawing.Font("Segoe UI", 12F);
            this.cmbSetor.ForeColor = System.Drawing.Color.FromArgb(50, 50, 50);
            this.cmbSetor.FormattingEnabled = true;
            this.cmbSetor.Location = new System.Drawing.Point(100, 245);
            this.cmbSetor.Name = "cmbSetor";
            this.cmbSetor.Size = new System.Drawing.Size(700, 29);
            this.cmbSetor.TabIndex = 4;
            this.cmbSetor.SelectedIndexChanged += new System.EventHandler(this.cmbSetor_SelectedIndexChanged);
            
            // lblInstrucao
            this.lblInstrucao.AutoSize = false;
            this.lblInstrucao.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Italic);
            this.lblInstrucao.ForeColor = System.Drawing.Color.FromArgb(120, 120, 120);
            this.lblInstrucao.Location = new System.Drawing.Point(100, 282);
            this.lblInstrucao.Name = "lblInstrucao";
            this.lblInstrucao.Size = new System.Drawing.Size(700, 20);
            this.lblInstrucao.TabIndex = 8;
            this.lblInstrucao.Text = "Para registrar o plantão, selecione o setor e em seguida pressione a digital";
            this.lblInstrucao.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            
            // panelSimulador
            this.panelSimulador.BackColor = System.Drawing.Color.White;
            this.panelSimulador.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.panelSimulador.Controls.Add(this.lblSimulador);
            this.panelSimulador.Location = new System.Drawing.Point(190, 310);
            this.panelSimulador.Name = "panelSimulador";
            this.panelSimulador.Size = new System.Drawing.Size(520, 220);
            this.panelSimulador.TabIndex = 5;
            this.panelSimulador.Padding = new System.Windows.Forms.Padding(20);
            
            // lblSimulador
            this.lblSimulador.BackColor = System.Drawing.Color.Transparent;
            this.lblSimulador.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblSimulador.Font = new System.Drawing.Font("Segoe UI", 12F);
            this.lblSimulador.ForeColor = System.Drawing.Color.FromArgb(100, 100, 100);
            this.lblSimulador.Location = new System.Drawing.Point(0, 0);
            this.lblSimulador.Name = "lblSimulador";
            this.lblSimulador.Padding = new System.Windows.Forms.Padding(20);
            this.lblSimulador.Size = new System.Drawing.Size(518, 280);
            this.lblSimulador.TabIndex = 0;
            this.lblSimulador.Text = "";
            this.lblSimulador.TextAlign = System.Drawing.ContentAlignment.TopLeft;
            
            // btnSimular
            this.btnSimular.BackColor = System.Drawing.Color.White;
            this.btnSimular.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSimular.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(37, 99, 235);
            this.btnSimular.FlatAppearance.BorderSize = 2;
            this.btnSimular.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSimular.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnSimular.ForeColor = System.Drawing.Color.FromArgb(37, 99, 235);
            this.btnSimular.Location = new System.Drawing.Point(60, 50);
            this.btnSimular.Name = "btnSimular";
            this.btnSimular.Size = new System.Drawing.Size(400, 35);
            this.btnSimular.TabIndex = 1;
            this.btnSimular.Text = "";
            this.btnSimular.UseVisualStyleBackColor = false;
            this.btnSimular.Visible = false;
            
            // panelFingerprint
            this.panelFingerprint.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.panelFingerprint.Cursor = System.Windows.Forms.Cursors.Default;
            this.panelFingerprint.Location = new System.Drawing.Point(185, 95);
            this.panelFingerprint.Name = "panelFingerprint";
            this.panelFingerprint.Size = new System.Drawing.Size(150, 120);
            this.panelFingerprint.TabIndex = 2;
            this.panelFingerprint.Visible = false;
            
            // lblFingerprint
            this.lblFingerprint.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblFingerprint.Font = new System.Drawing.Font("Segoe UI", 48F);
            this.lblFingerprint.ForeColor = System.Drawing.Color.FromArgb(37, 99, 235);
            this.lblFingerprint.Location = new System.Drawing.Point(0, 0);
            this.lblFingerprint.Name = "lblFingerprint";
            this.lblFingerprint.Size = new System.Drawing.Size(148, 118);
            this.lblFingerprint.TabIndex = 0;
            this.lblFingerprint.Text = "";
            this.lblFingerprint.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblFingerprint.Visible = false;
            
            // lblModoSimulacao
            this.lblModoSimulacao.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblModoSimulacao.ForeColor = System.Drawing.Color.Gray;
            this.lblModoSimulacao.Location = new System.Drawing.Point(20, 225);
            this.lblModoSimulacao.Name = "lblModoSimulacao";
            this.lblModoSimulacao.Size = new System.Drawing.Size(480, 40);
            this.lblModoSimulacao.TabIndex = 3;
            this.lblModoSimulacao.Text = "Modo Simulação Ativo";
            this.lblModoSimulacao.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblModoSimulacao.Visible = false;
            
            // lblStatus
            this.lblStatus.BackColor = System.Drawing.Color.White;
            this.lblStatus.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.lblStatus.ForeColor = System.Drawing.Color.Gray;
            this.lblStatus.Location = new System.Drawing.Point(100, 540);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(700, 30);
            this.lblStatus.TabIndex = 6;
            this.lblStatus.Text = "Selecione o setor para ativar o leitor";
            this.lblStatus.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            
            // btnRegister
            this.btnRegister.BackColor = System.Drawing.Color.FromArgb(34, 139, 87);
            this.btnRegister.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnRegister.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(34, 139, 87);
            this.btnRegister.FlatAppearance.BorderSize = 0;
            this.btnRegister.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRegister.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.btnRegister.ForeColor = System.Drawing.Color.White;
            this.btnRegister.Location = new System.Drawing.Point(320, 585);
            this.btnRegister.Name = "btnRegister";
            this.btnRegister.Size = new System.Drawing.Size(260, 50);
            this.btnRegister.TabIndex = 7;
            this.btnRegister.Text = "📝 Cadastrar Novo Funcionário";
            this.btnRegister.UseVisualStyleBackColor = false;
            this.btnRegister.Click += new System.EventHandler(this.btnRegister_Click);
            
            // timerClock
            this.timerClock.Enabled = true;
            this.timerClock.Interval = 1000;
            this.timerClock.Tick += new System.EventHandler(this.timerClock_Tick);
            
            // LoginForm
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(240, 242, 245);
            this.ClientSize = new System.Drawing.Size(900, 680);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.btnRegister);
            this.Controls.Add(this.panelSimulador);
            this.Controls.Add(this.lblInstrucao);
            this.Controls.Add(this.cmbSetor);
            this.Controls.Add(this.lblSetorAla);
            this.Controls.Add(this.lblLocalProducao);
            this.Controls.Add(this.panelHeader);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
            this.MaximizeBox = true;
            this.Name = "LoginForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.Text = "Sistema Biométrico - Registro de Ponto";
            this.Resize += new System.EventHandler(this.LoginForm_Resize);
            
            this.panelHeader.ResumeLayout(false);
            this.panelSimulador.ResumeLayout(false);
            this.panelFingerprint.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        private System.Windows.Forms.Panel panelHeader;
        private System.Windows.Forms.Label lblTime;
        private System.Windows.Forms.Label lblDate;
        private System.Windows.Forms.Label lblLocalProducao;
        private System.Windows.Forms.Label lblSetorAla;
        private System.Windows.Forms.ComboBox cmbSetor;
        private System.Windows.Forms.Label lblInstrucao;
        private System.Windows.Forms.Panel panelSimulador;
        private System.Windows.Forms.Label lblSimulador;
        private System.Windows.Forms.Button btnSimular;
        private System.Windows.Forms.Panel panelFingerprint;
        private System.Windows.Forms.Label lblFingerprint;
        private System.Windows.Forms.Label lblModoSimulacao;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Button btnRegister;
        private System.Windows.Forms.Timer timerClock;
    }
}
