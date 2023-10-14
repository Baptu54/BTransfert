using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BTransfert
{
    internal static class Program
    {
        /// <summary>
        /// Point d'entrée principal de l'application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            do
            {
                VarGlobalAPP.ChangePort = false;                
                Application.Run(new Form2());
                Application.Run(new Form1());
                string tempDirectory = Path.Combine(Path.GetTempPath(), "btransfert");
                Directory.Delete(tempDirectory, true);
            } while (VarGlobalAPP.ChangePort);            
            Environment.Exit(1);
        }
    }
}
