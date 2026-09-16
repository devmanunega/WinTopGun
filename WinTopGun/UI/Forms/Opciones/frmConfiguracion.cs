using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using WinTopGun.UI.UserControls;

namespace WinTopGun.UI.Forms.Opciones
{
    public partial class frmConfiguracion : Form
    {
        public frmConfiguracion()
        {
            InitializeComponent();
        }

        private void frmConfiguracion_Load(object sender, EventArgs e)
        {
            uCtlFichaOpciones confDirectorioData = new uCtlFichaOpciones();
            
            floLaPaContainer.Controls.Add(confDirectorioData);

        }
    }
}
