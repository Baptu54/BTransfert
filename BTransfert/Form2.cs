using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using AutoUpdaterDotNET;

namespace BTransfert
{
    public partial class Form2 : Form
    {
        public Form2()
        {
            InitializeComponent();
        }

        private void Form2_Load(object sender, EventArgs e)
        {            
            Assembly assembly = Assembly.GetExecutingAssembly(); // Obtenir l'assembly en cours d'exécution
            Version version = assembly.GetName().Version;
            label1.Text = "Version : " + version;
            try
            {
                AutoUpdater.Mandatory = true;
                AutoUpdater.Start("https://github.com/Baptu54/BTransfert/raw/master/aaaa.xml");
            }
            catch
            {
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            VarGlobalAPP.Port = Convert.ToInt32(numericUpDown1.Value);
            this.Close();
        }
    }
}
