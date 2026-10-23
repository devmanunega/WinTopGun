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
            reiniciarRegistroPartidosToolStripMenuItem = new ToolStripMenuItem();
            flowLayoutPanel1 = new FlowLayoutPanel();
            grpExtraccion = new GroupBox();
            flowProcesos = new FlowLayoutPanel();
            grpControl = new GroupBox();
            grpClasificacion = new GroupBox();
            flowClasificacion = new FlowLayoutPanel();
            btnClasificacionDirectorio = new Button();
            btnClasificacionOrganizar = new Button();
            btnClasificacionDepurar = new Button();
            btnExtract = new Button();
            btnCancelar = new Button();
            panelEstado = new Panel();
            lblEstado = new Label();
            progressBarExtraccion = new ProgressBar();
            btnExtractPlayers = new Button();
            btnExtractPartidos = new Button();
            btnPartidosPorLiga = new Button();
            menuStrip1.SuspendLayout();
            flowLayoutPanel1.SuspendLayout();
            grpExtraccion.SuspendLayout();
            flowProcesos.SuspendLayout();
            grpControl.SuspendLayout();
            grpClasificacion.SuspendLayout();
            flowClasificacion.SuspendLayout();
            panelEstado.SuspendLayout();
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
            opcionesToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { directorioDestinoToolStripMenuItem, reiniciarRegistroPartidosToolStripMenuItem });
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
            // reiniciarRegistroPartidosToolStripMenuItem
            // 
            reiniciarRegistroPartidosToolStripMenuItem.Name = "reiniciarRegistroPartidosToolStripMenuItem";
            reiniciarRegistroPartidosToolStripMenuItem.Size = new Size(280, 22);
            reiniciarRegistroPartidosToolStripMenuItem.Text = "Reiniciar registro de partidos";
            reiniciarRegistroPartidosToolStripMenuItem.Click += reiniciarRegistroPartidosToolStripMenuItem_Click;
            // 
            // flowLayoutPanel1
            // 
            flowLayoutPanel1.Controls.Add(grpExtraccion);
            flowLayoutPanel1.Controls.Add(grpControl);
            flowLayoutPanel1.Controls.Add(grpClasificacion);
            flowLayoutPanel1.Dock = DockStyle.Fill;
            flowLayoutPanel1.Location = new Point(0, 24);
            flowLayoutPanel1.Name = "flowLayoutPanel1";
            flowLayoutPanel1.Size = new Size(658, 392);
            flowLayoutPanel1.TabIndex = 1;
            // 
            // grpExtraccion
            // 
            grpExtraccion.AutoSize = true;
            grpExtraccion.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            grpExtraccion.Controls.Add(flowProcesos);
            grpExtraccion.Location = new Point(3, 3);
            grpExtraccion.Name = "grpExtraccion";
            grpExtraccion.Padding = new Padding(3);
            grpExtraccion.Size = new Size(506, 95);
            grpExtraccion.TabIndex = 0;
            grpExtraccion.TabStop = false;
            grpExtraccion.Text = "Procesos de extracción";
            // 
            // flowProcesos
            // 
            flowProcesos.AutoSize = true;
            flowProcesos.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            flowProcesos.Controls.Add(btnExtract);
            flowProcesos.Controls.Add(btnExtractPlayers);
            flowProcesos.Controls.Add(btnExtractPartidos);
            flowProcesos.Controls.Add(btnPartidosPorLiga);
            flowProcesos.Dock = DockStyle.Fill;
            flowProcesos.Location = new Point(6, 21);
            flowProcesos.Name = "flowProcesos";
            flowProcesos.Size = new Size(494, 68);
            flowProcesos.TabIndex = 0;
            // 
            // grpControl
            // 
            grpControl.AutoSize = true;
            grpControl.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            grpControl.Controls.Add(btnCancelar);
            grpControl.Location = new Point(515, 3);
            grpControl.Name = "grpControl";
            grpControl.Padding = new Padding(3);
            grpControl.Size = new Size(102, 95);
            grpControl.TabIndex = 1;
            grpControl.TabStop = false;
            grpControl.Text = "Control";
            // 
            // grpClasificacion
            // 
            grpClasificacion.AutoSize = true;
            grpClasificacion.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            grpClasificacion.Controls.Add(flowClasificacion);
            grpClasificacion.Location = new Point(3, 104);
            grpClasificacion.Name = "grpClasificacion";
            grpClasificacion.Padding = new Padding(3);
            grpClasificacion.Size = new Size(386, 95);
            grpClasificacion.TabIndex = 2;
            grpClasificacion.TabStop = false;
            grpClasificacion.Text = "Clasificación";
            // 
            // flowClasificacion
            // 
            flowClasificacion.AutoSize = true;
            flowClasificacion.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            flowClasificacion.Controls.Add(btnClasificacionDirectorio);
            flowClasificacion.Controls.Add(btnClasificacionOrganizar);
            flowClasificacion.Controls.Add(btnClasificacionDepurar);
            flowClasificacion.Dock = DockStyle.Fill;
            flowClasificacion.Location = new Point(6, 21);
            flowClasificacion.Name = "flowClasificacion";
            flowClasificacion.Size = new Size(374, 68);
            flowClasificacion.TabIndex = 0;
            // 
            // btnClasificacionDirectorio
            // 
            btnClasificacionDirectorio.Image = Properties.Resources.icons8_prismaticos_50;
            btnClasificacionDirectorio.Location = new Point(3, 3);
            btnClasificacionDirectorio.Name = "btnClasificacionDirectorio";
            btnClasificacionDirectorio.Size = new Size(118, 57);
            btnClasificacionDirectorio.TabIndex = 0;
            btnClasificacionDirectorio.Text = "Directorio";
            btnClasificacionDirectorio.TextAlign = ContentAlignment.MiddleRight;
            btnClasificacionDirectorio.TextImageRelation = TextImageRelation.TextBeforeImage;
            btnClasificacionDirectorio.UseVisualStyleBackColor = true;
            btnClasificacionDirectorio.Click += btnClasificacionDirectorio_Click;
            // 
            // btnClasificacionOrganizar
            // 
            btnClasificacionOrganizar.Image = Properties.Resources.icons8_prismaticos_50;
            btnClasificacionOrganizar.Location = new Point(127, 3);
            btnClasificacionOrganizar.Name = "btnClasificacionOrganizar";
            btnClasificacionOrganizar.Size = new Size(118, 57);
            btnClasificacionOrganizar.TabIndex = 1;
            btnClasificacionOrganizar.Text = "Organizar";
            btnClasificacionOrganizar.TextAlign = ContentAlignment.MiddleRight;
            btnClasificacionOrganizar.TextImageRelation = TextImageRelation.TextBeforeImage;
            btnClasificacionOrganizar.UseVisualStyleBackColor = true;
            btnClasificacionOrganizar.Click += btnClasificacionOrganizar_Click;
            // 
            // btnClasificacionDepurar
            // 
            btnClasificacionDepurar.Image = Properties.Resources.icons8_prismaticos_50;
            btnClasificacionDepurar.Location = new Point(251, 3);
            btnClasificacionDepurar.Name = "btnClasificacionDepurar";
            btnClasificacionDepurar.Size = new Size(118, 57);
            btnClasificacionDepurar.TabIndex = 2;
            btnClasificacionDepurar.Text = "Depurar";
            btnClasificacionDepurar.TextAlign = ContentAlignment.MiddleRight;
            btnClasificacionDepurar.TextImageRelation = TextImageRelation.TextBeforeImage;
            btnClasificacionDepurar.UseVisualStyleBackColor = true;
            btnClasificacionDepurar.Click += btnClasificacionDepurar_Click;
            // 
            // btnExtract
            // 
            btnExtract.Image = Properties.Resources.icons8_prismaticos_50;
            btnExtract.Location = new Point(3, 3);
            btnExtract.Name = "btnExtract";
            btnExtract.Size = new Size(118, 57);
            btnExtract.TabIndex = 0;
            btnExtract.Text = "Equipos";
            btnExtract.TextAlign = ContentAlignment.MiddleRight;
            btnExtract.TextImageRelation = TextImageRelation.TextBeforeImage;
            btnExtract.UseVisualStyleBackColor = true;
            btnExtract.Click += btnExtract_Click;
            // 
            // btnCancelar
            btnCancelar.Enabled = false;
            btnCancelar.Location = new Point(6, 21);
            btnCancelar.Name = "btnCancelar";
            btnCancelar.Size = new Size(90, 57);
            btnCancelar.TabIndex = 1;
            btnCancelar.Text = "Cancelar";
            btnCancelar.UseVisualStyleBackColor = true;
            btnCancelar.Click += btnCancelar_Click;
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
            // progressBarExtraccion
            // 
            progressBarExtraccion.Dock = DockStyle.Bottom;
            progressBarExtraccion.Location = new Point(0, 28);
            progressBarExtraccion.Name = "progressBarExtraccion";
            progressBarExtraccion.Size = new Size(658, 6);
            progressBarExtraccion.Style = ProgressBarStyle.Continuous;
            progressBarExtraccion.TabIndex = 0;
            // 
            // btnExtractPlayers
            // 
            btnExtractPlayers.Image = Properties.Resources.icons8_prismaticos_50;
            btnExtractPlayers.Location = new Point(127, 3);
            btnExtractPlayers.Name = "btnExtractPlayers";
            btnExtractPlayers.Size = new Size(118, 57);
            btnExtractPlayers.TabIndex = 2;
            btnExtractPlayers.Text = "Jugadores";
            btnExtractPlayers.TextAlign = ContentAlignment.MiddleRight;
            btnExtractPlayers.TextImageRelation = TextImageRelation.TextBeforeImage;
            btnExtractPlayers.UseVisualStyleBackColor = true;
            btnExtractPlayers.Click += btnExtractPlayers_Click;
            // 
            // btnExtractPartidos
            // 
            btnExtractPartidos.Image = Properties.Resources.icons8_prismaticos_50;
            btnExtractPartidos.Location = new Point(251, 3);
            btnExtractPartidos.Name = "btnExtractPartidos";
            btnExtractPartidos.Size = new Size(118, 57);
            btnExtractPartidos.TabIndex = 3;
            btnExtractPartidos.Text = "Partidos";
            btnExtractPartidos.TextAlign = ContentAlignment.MiddleRight;
            btnExtractPartidos.TextImageRelation = TextImageRelation.TextBeforeImage;
            btnExtractPartidos.UseVisualStyleBackColor = true;
            btnExtractPartidos.Click += btnExtractPartidos_Click;
            // 
            // btnPartidosPorLiga
            // 
            btnPartidosPorLiga.Image = Properties.Resources.icons8_prismaticos_50;
            btnPartidosPorLiga.Location = new Point(375, 3);
            btnPartidosPorLiga.Name = "btnPartidosPorLiga";
            btnPartidosPorLiga.Size = new Size(118, 57);
            btnPartidosPorLiga.TabIndex = 4;
            btnPartidosPorLiga.Text = "Partidos por Liga";
            btnPartidosPorLiga.TextAlign = ContentAlignment.MiddleCenter;
            btnPartidosPorLiga.UseVisualStyleBackColor = true;
            btnPartidosPorLiga.Click += btnPartidosPorLiga_Click;
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
            grpExtraccion.ResumeLayout(false);
            flowProcesos.ResumeLayout(false);
            grpControl.ResumeLayout(false);
            grpClasificacion.ResumeLayout(false);
            flowClasificacion.ResumeLayout(false);
            panelEstado.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private MenuStrip menuStrip1;
        private ToolStripMenuItem archivoToolStripMenuItem;
        private ToolStripMenuItem opcionesToolStripMenuItem;
        private ToolStripMenuItem directorioDestinoToolStripMenuItem;
        private ToolStripMenuItem reiniciarRegistroPartidosToolStripMenuItem;
        private FlowLayoutPanel flowLayoutPanel1;
        private GroupBox grpExtraccion;
        private FlowLayoutPanel flowProcesos;
        private GroupBox grpControl;
        private GroupBox grpClasificacion;
        private FlowLayoutPanel flowClasificacion;
        private Button btnClasificacionDirectorio;
        private Button btnClasificacionOrganizar;
        private Button btnClasificacionDepurar;
        private Button btnExtract;
        private Button btnCancelar;
        private Panel panelEstado;
        private ProgressBar progressBarExtraccion;
        private Label lblEstado;
        private Button btnExtractPlayers;
        private Button btnExtractPartidos;
        private Button btnPartidosPorLiga;
    }
}
