namespace WinTopGun.UI.UserControls
{
    partial class uCtlFichaOpciones
    {
        /// <summary> 
        /// Variable del diseñador necesaria.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Limpiar los recursos que se estén usando.
        /// </summary>
        /// <param name="disposing">true si los recursos administrados se deben desechar; false en caso contrario.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Código generado por el Diseñador de componentes

        /// <summary> 
        /// Método necesario para admitir el Diseñador. No se puede modificar
        /// el contenido de este método con el editor de código.
        /// </summary>
        private void InitializeComponent()
        {
            tableLayoutPanel1 = new TableLayoutPanel();
            picBoxDirectorio = new PictureBox();
            lblTituloDirectorio = new Label();
            lblSubtituloDirectorio = new Label();
            tableLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)picBoxDirectorio).BeginInit();
            SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.ColumnCount = 1;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.Controls.Add(picBoxDirectorio, 0, 0);
            tableLayoutPanel1.Controls.Add(lblTituloDirectorio, 0, 1);
            tableLayoutPanel1.Controls.Add(lblSubtituloDirectorio, 0, 2);
            tableLayoutPanel1.Dock = DockStyle.Fill;
            tableLayoutPanel1.Location = new Point(0, 0);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 3;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 60F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));
            tableLayoutPanel1.Size = new Size(172, 150);
            tableLayoutPanel1.TabIndex = 0;
            // 
            // picBoxDirectorio
            // 
            picBoxDirectorio.Dock = DockStyle.Fill;
            picBoxDirectorio.Image = Properties.Resources.icons8_abrir_carpeta_50;
            picBoxDirectorio.Location = new Point(3, 15);
            picBoxDirectorio.Margin = new Padding(3, 15, 3, 3);
            picBoxDirectorio.Name = "picBoxDirectorio";
            picBoxDirectorio.Size = new Size(166, 72);
            picBoxDirectorio.TabIndex = 0;
            picBoxDirectorio.TabStop = false;
            picBoxDirectorio.Click += picBoxDirectorio_Click;
            // 
            // lblTituloDirectorio
            // 
            lblTituloDirectorio.AutoSize = true;
            lblTituloDirectorio.Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblTituloDirectorio.Location = new Point(3, 90);
            lblTituloDirectorio.Name = "lblTituloDirectorio";
            lblTituloDirectorio.Size = new Size(84, 21);
            lblTituloDirectorio.TabIndex = 1;
            lblTituloDirectorio.Text = "Directorio";
            // 
            // lblSubtituloDirectorio
            // 
            lblSubtituloDirectorio.AutoSize = true;
            lblSubtituloDirectorio.Location = new Point(3, 120);
            lblSubtituloDirectorio.Name = "lblSubtituloDirectorio";
            lblSubtituloDirectorio.Size = new Size(145, 15);
            lblSubtituloDirectorio.TabIndex = 2;
            lblSubtituloDirectorio.Text = "Ruta para la data obtenida";
            // 
            // uCtlFichaOpciones
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(tableLayoutPanel1);
            Name = "uCtlFichaOpciones";
            Size = new Size(172, 150);
            Load += uCtlFichaOpciones_Load;
            tableLayoutPanel1.ResumeLayout(false);
            tableLayoutPanel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)picBoxDirectorio).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private TableLayoutPanel tableLayoutPanel1;
        private PictureBox picBoxDirectorio;
        private Label lblTituloDirectorio;
        private Label lblSubtituloDirectorio;
    }
}
