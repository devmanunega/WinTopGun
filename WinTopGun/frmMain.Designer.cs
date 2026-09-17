namespace WinTopGun
{
    partial class frmMain
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            menuStrip1 = new MenuStrip();
            archivoToolStripMenuItem = new ToolStripMenuItem();
            opcionesToolStripMenuItem = new ToolStripMenuItem();
            directorioDestinoToolStripMenuItem = new ToolStripMenuItem();
            flowLayoutPanel1 = new FlowLayoutPanel();
            btnExtract = new Button();
            btnCancelar = new Button();
            panelEstado = new Panel();
            progressBarExtraccion = new ProgressBar();
            lblEstado = new Label();
            panelEstado.SuspendLayout();
            menuStrip1.SuspendLayout();
            flowLayoutPanel1.SuspendLayout();
            SuspendLayout();
            // 
            // menuStrip1
            // 
            menuStrip1.Items.AddRange(new ToolStripItem[] { archivoToolStripMenuItem, opcionesToolStripMenuItem });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(658, 24);
            menuStrip1.TabIndex = 0;
            menuStrip1.Text = "menuStrip1";
            // 
            // archivoToolStripMenuItem
            // 
            archivoToolStripMenuItem.Name = "archivoToolStripMenuItem";
            archivoToolStripMenuItem.Size = new Size(60, 20);
            archivoToolStripMenuItem.Text = "Archivo";
            // 
            // opcionesToolStripMenuItem
            // 
            opcionesToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { directorioDestinoToolStripMenuItem });
            opcionesToolStripMenuItem.Name = "opcionesToolStripMenuItem";
            opcionesToolStripMenuItem.Size = new Size(69, 20);
            opcionesToolStripMenuItem.Text = "Opciones";
            // 
            // directorioDestinoToolStripMenuItem
            // 
            directorioDestinoToolStripMenuItem.Name = "directorioDestinoToolStripMenuItem";
            directorioDestinoToolStripMenuItem.Size = new Size(168, 22);
            directorioDestinoToolStripMenuItem.Text = "Directorio destino";
            directorioDestinoToolStripMenuItem.Click += directorioDestinoToolStripMenuItem_Click;
            // 
            // btnCancelar
            // 
            btnCancelar.Enabled = false;
            btnCancelar.Location = new Point(77, 3);
            btnCancelar.Name = "btnCancelar";
            btnCancelar.Size = new Size(90, 57);
            btnCancelar.TabIndex = 1;
            btnCancelar.Text = "Cancelar";
            btnCancelar.UseVisualStyleBackColor = true;
            btnCancelar.Click += btnCancelar_Click;
            // 
            // flowLayoutPanel1
            // 
            flowLayoutPanel1.Controls.Add(btnExtract);
            flowLayoutPanel1.Controls.Add(btnCancelar);
            flowLayoutPanel1.Dock = DockStyle.Fill;
            flowLayoutPanel1.Location = new Point(0, 24);
            flowLayoutPanel1.Name = "flowLayoutPanel1";
            flowLayoutPanel1.Size = new Size(658, 426);
            flowLayoutPanel1.TabIndex = 1;
            // 
            // btnExtract
            // 
            btnExtract.Image = Properties.Resources.icons8_prismaticos_50;
            btnExtract.Location = new Point(3, 3);
            btnExtract.Name = "btnExtract";
            btnExtract.Size = new Size(68, 57);
            btnExtract.TabIndex = 0;
            btnExtract.UseVisualStyleBackColor = true;
            btnExtract.Click += btnExtract_Click;
            // 
            // panelEstado
            // 
            panelEstado.Controls.Add(lblEstado);
            panelEstado.Controls.Add(progressBarExtraccion);
            panelEstado.Dock = DockStyle.Bottom;
            panelEstado.Location = new Point(0, 416);
            panelEstado.Name = "panelEstado";
            panelEstado.Size = new Size(658, 34);
            panelEstado.TabIndex = 2;
            // 
            // progressBarExtraccion
            // 
            progressBarExtraccion.Dock = DockStyle.Bottom;
            progressBarExtraccion.Location = new Point(0, 28);
            progressBarExtraccion.Name = "progressBarExtraccion";
            progressBarExtraccion.Size = new Size(658, 6);
            progressBarExtraccion.Style = ProgressBarStyle.Continuous;
            progressBarExtraccion.TabIndex = 0;
            // 
            // lblEstado
            // 
            lblEstado.AutoEllipsis = true;
            lblEstado.Dock = DockStyle.Fill;
            lblEstado.Location = new Point(0, 0);
            lblEstado.Name = "lblEstado";
            lblEstado.Padding = new Padding(3, 0, 3, 0);
            lblEstado.Size = new Size(658, 28);
            lblEstado.TabIndex = 1;
            lblEstado.Text = "Listo.";
            lblEstado.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // frmMain
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(658, 450);
            Controls.Add(flowLayoutPanel1);
            Controls.Add(panelEstado);
            Controls.Add(menuStrip1);
            MainMenuStrip = menuStrip1;
            Name = "frmMain";
            Text = "Win Top Gun";
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            flowLayoutPanel1.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private MenuStrip menuStrip1;
        private ToolStripMenuItem archivoToolStripMenuItem;
        private ToolStripMenuItem opcionesToolStripMenuItem;
        private ToolStripMenuItem directorioDestinoToolStripMenuItem;
        private FlowLayoutPanel flowLayoutPanel1;
        private Button btnExtract;
        private Button btnCancelar;
        private Panel panelEstado;
        private ProgressBar progressBarExtraccion;
        private Label lblEstado;
    }
}
