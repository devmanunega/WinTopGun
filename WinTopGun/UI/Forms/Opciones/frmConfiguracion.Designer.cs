namespace WinTopGun.UI.Forms.Opciones
{
    partial class frmConfiguracion
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            floLaPaContainer = new FlowLayoutPanel();
            SuspendLayout();
            // 
            // floLaPaContainer
            // 
            floLaPaContainer.Dock = DockStyle.Fill;
            floLaPaContainer.Location = new Point(0, 0);
            floLaPaContainer.Name = "floLaPaContainer";
            floLaPaContainer.Padding = new Padding(50, 0, 0, 0);
            floLaPaContainer.Size = new Size(800, 450);
            floLaPaContainer.TabIndex = 0;
            // 
            // frmConfiguracion
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(floLaPaContainer);
            Name = "frmConfiguracion";
            Text = "Configuración";
            Load += frmConfiguracion_Load;
            ResumeLayout(false);
        }

        #endregion

        private FlowLayoutPanel floLaPaContainer;
    }
}