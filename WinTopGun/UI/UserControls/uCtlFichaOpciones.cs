using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace WinTopGun.UI.UserControls
{
    public partial class uCtlFichaOpciones : UserControl
    {
        public uCtlFichaOpciones()
        {
            InitializeComponent();
        }

        private void picBoxDirectorio_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();

            folderBrowserDialog.ShowNewFolderButton = true;
            folderBrowserDialog.ShowPinnedPlaces = true;

            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                Properties.Settings.Default.RutaDatosExtraidos = folderBrowserDialog.SelectedPath;

                MessageBox.Show($"{Properties.Settings.Default.RutaDatosExtraidos} - Directorio de destino actualizado correctamente.", "Información", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

        }
    }
}
