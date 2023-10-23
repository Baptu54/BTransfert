using System;
using System.IO;
using System.Windows.Forms;
using System.Resources;
using System.Reflection;
using System.Resources;
using System.Net;
using System.Diagnostics;


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
            Process[] processes = Process.GetProcessesByName("BTransfert");
            if (Process.GetProcessesByName("BTransfert").Length > 1)
            {               
               
                processes[0].Kill(); // Si la fermeture échoue, tuez le processus
                               
            }
            
            string tempDirectory = Path.Combine(Path.GetTempPath(), "btransfert");
            bool reussite = false;
            if (!Directory.Exists(tempDirectory))
            {
                Directory.CreateDirectory(tempDirectory);
            }
            do
            {
                try
                {
                    if (!File.Exists("AutoUpdater.NET.dll") || !File.Exists("Microsoft.Web.WebView2.Core.dll") || !File.Exists("Microsoft.Web.WebView2.WinForms.dll"))
                    {
                        using (WebClient webClient = new WebClient())
                        {
                            if (!File.Exists("AutoUpdater.NET.dll"))
                            {
                                webClient.DownloadFile("https://github.com/Baptu54/BTransfert/raw/master/AutoUpdater.NET.dll", "AutoUpdater.NET.dll");
                            }
                            if (!File.Exists("Microsoft.Web.WebView2.Core.dll"))
                            {
                                webClient.DownloadFile("https://github.com/Baptu54/BTransfert/raw/master/Microsoft.Web.WebView2.Core.dll", "Microsoft.Web.WebView2.Core.dll");
                            }
                            if (!File.Exists("Microsoft.Web.WebView2.WinForms.dll"))
                            {
                                webClient.DownloadFile("https://github.com/Baptu54/BTransfert/raw/master/Microsoft.Web.WebView2.WinForms.dll", "Microsoft.Web.WebView2.WinForms.dll");
                            }
                        }
                    }
                    reussite = true;

                }
                catch (WebException ex)
                {
                    string message = "Erreur de réseau : Veuillez réessayer.";
                    string title = "Erreur Réseau";
                    MessageBoxButtons buttons = MessageBoxButtons.RetryCancel;
                    DialogResult result = MessageBox.Show(message, title, buttons, MessageBoxIcon.Error);
                    if (result == DialogResult.Cancel)
                    {
                        Environment.Exit(1);
                    }
                    else if (result == DialogResult.Retry)
                    {
                        reussite = false;
                    }
                    else
                    {
                        Environment.Exit(1);
                    }
                }
            } while (reussite==false);
            
            

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form2());
            Application.Run(new Form1());                
            Directory.Delete(tempDirectory, true);
            Environment.Exit(1);
        }
    }
}
