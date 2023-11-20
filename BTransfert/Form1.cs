using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Windows.Input;
using System.Windows.Interop;
using System.Runtime.CompilerServices;

namespace BTransfert
{
    public partial class Form1 : Form
    {
        private int Port = VarGlobalAPP.Port;
        string tempDirectory;
        bool reponse = false;

        public Form1()
        {
            InitializeComponent();
            this.MaximizeBox = false;
            this.KeyPreview = true;
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            CenterToScreen();
            BringToFront();
            textBox1.Text = "Adresse IP";
            textBox1.ForeColor = SystemColors.GrayText;
            textBox2.Text = "Chemin Fichier";
            textBox2.ForeColor = SystemColors.GrayText;
            label2.Text = "Port : " + Port;
            label3.Text = "Utilisateur : "+GetUserName();
            label1.Text = "Adresse IP : " + GetLocalIPAddress();
            tempDirectory = Path.Combine(Path.GetTempPath(), "btransfert");
            button1.Enabled = false;

            await Task.Run(() => EcouteReseau());
            await Task.Delay(1000);
            

        }
        private async Task EcouteReseau()
        {
            try
            {
                TcpListener listener = new TcpListener(IPAddress.Any, VarGlobalAPP.Port);
                listener.Start();


                while (true)
                {
                    TcpClient client = await listener.AcceptTcpClientAsync();

                    // Gérer la réception du fichier dans une tâche asynchrone
                    await Task.Run(() => FichierRecu(client));

                    // Vous pouvez mettre en place une gestion de file d'attente ici si nécessaire
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur : " + ex.Message);
            }
        }
        private async void FichierRecu(TcpClient client)
        {
            string ipAddress = GetLocalIPAddress();
            try
            {
                using (NetworkStream stream = client.GetStream())
                {
                    byte[] fileNameSizeBytes = new byte[4];
                    stream.Read(fileNameSizeBytes, 0, fileNameSizeBytes.Length);
                    int fileNameSize = BitConverter.ToInt32(fileNameSizeBytes, 0);
                    byte[] fileNameBytes = new byte[fileNameSize];
                    int bytesRead = stream.Read(fileNameBytes, 0, fileNameBytes.Length);
                    string filnmip = System.Text.Encoding.UTF8.GetString(fileNameBytes, 0, bytesRead);
                    string[] tabfilmip = filnmip.Split(';');
                    string fileName = tabfilmip[0];
                    string ipsrc = tabfilmip[1];
                    string usr = tabfilmip[2];               
                    
                    ListViewItem listViewItem1 = new ListViewItem(new string[] { ipsrc, usr });
                    AddItemToList(listViewItem1);
                    if (fileName == "Requette_Btransfert")
                    {

                        await Task.Run(() => Message(ipsrc, "Reponse_Btransfert", null));

                    }
                    else if (fileName == "Reponse_Btransfert")
                    {
                        
                    }
                    else if (fileName == "Reponse_Reception")
                    {
                        reponse = true;
                    }
                    else if (fileName == "Application_Fermee")
                    {
                        RemoveItemFromList(listViewItem1);
                    }
                    else if (fileName == "Message")
                    {
                        string msg = tabfilmip[3];
                        richTextBox1.Text += $"{usr} : {msg}{Environment.NewLine}{Environment.NewLine}";

                    }
                    else
                    {
                        string tempFilePath = Path.Combine(tempDirectory, fileName);
                        if (!File.Exists(tempFilePath))
                        {
                            using (FileStream fileStream = File.Create(tempFilePath))
                            {
                                byte[] buffer = new byte[1024];
                                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                                {
                                    fileStream.Write(buffer, 0, bytesRead);
                                }
                            }
                            ListViewItem listViewItem = new ListViewItem(new string[] { fileName, (Convert.ToDouble(new FileInfo(tempFilePath).Length) / 1000).ToString("F1") + " Ko", DateTime.Now.ToString(), usr, ipsrc });
                            listView1.Items.Add(listViewItem);
                            FlashWindow();
                            if (tabControl1.SelectedTab == tabPage2)
                            {
                                tabControl1.TabPages[0].ForeColor = Color.Red;
                                tabControl1.TabPages[0].BackColor = Color.LightBlue;
                            }
                            try
                            {
                                await Task.Run(() => Message(ipsrc,"Reponse_Reception", null));
                            }
                            catch (Exception)
                            {

                         
                            }
                            
                        }
                    }

                }
                client.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur : " + ex.Message, "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private async Task Message(string ip,string type,string message)
        {
            try
            {
                using (TcpClient client = new TcpClient())
                {
                    await client.ConnectAsync(ip, Port);

                    using (NetworkStream stream = client.GetStream())
                    {
                        byte[] fileNameBytes = System.Text.Encoding.UTF8.GetBytes(type + ";" + GetLocalIPAddress() + ";" + GetUserName()+";"+ message);
                        int fileNameSize = fileNameBytes.Length;
                        byte[] fileNameSizeBytes = BitConverter.GetBytes(fileNameSize);
                        await stream.WriteAsync(fileNameSizeBytes, 0, fileNameSizeBytes.Length);
                        await stream.WriteAsync(fileNameBytes, 0, fileNameSize);
                    }
                }
            }
            catch (Exception ex)
            {
                // Gérer les erreurs ici
                if (type!= "Message")
                {
                   MessageBox.Show("Erreur lors de l'envoi du mesage à " + ip + " : " + ex.Message, "Erreur Envoi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }                
            }
        }

        private async void button1_Click(object sender, EventArgs e)  // envoi fichier
        {
            IPAddress ipAddress;
            if (IPAddress.TryParse(textBox1.Text, out ipAddress)&&(Path.IsPathRooted(textBox2.Text)))
            {
                await Envoi_Fichier(textBox1.Text, textBox2.Text);
            }
            
        }
        private async Task Envoi_Fichier(string ip,string path)
        {
            button8.Enabled = false;

            try
            {
                using (TcpClient client = new TcpClient())
                {
                    await client.ConnectAsync(ip, Port);

                    using (NetworkStream stream = client.GetStream())
                    {
                        string fileName = Path.GetFileName(path);
                        byte[] fileNameBytes = System.Text.Encoding.UTF8.GetBytes(fileName.Replace(";", "_") + ";" + GetLocalIPAddress() + ";" + GetUserName());
                        int fileNameSize = fileNameBytes.Length;
                        byte[] fileNameSizeBytes = BitConverter.GetBytes(fileNameSize);
                        await stream.WriteAsync(fileNameSizeBytes, 0, fileNameSizeBytes.Length);
                        await stream.WriteAsync(fileNameBytes, 0, fileNameSize);

                        using (FileStream fileStream = File.OpenRead(path))
                        {
                            byte[] buffer = new byte[1024];
                            int bytesRead;
                            while ((bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                            {
                                await stream.WriteAsync(buffer, 0, bytesRead);
                            }
                        }
                    }
                    if (!tous)
                    {
                        int compt = 0;
                        while (reponse == false)
                        {
                            await Task.Delay(1000);
                            compt++;
                            if (compt > Convert.ToInt32(new FileInfo(path).Length / 1000) + 5)
                            {
                                reponse = true;
                                DialogResult resulta = MessageBox.Show("Le recepteur n'a peut etre pas reçu votre fichier", "Time Out", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                 button8.Enabled = true;
                                reponse = false;
                                return;
                            }
                        }
                         button8.Enabled = true;
                        reponse = false;
                        DialogResult result = MessageBox.Show("Votre message a bien été reçu", "Envoi Reussi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                     button8.Enabled = true;

                }
            }
            catch (Exception ex)
            {
                // Gérer les erreurs ici
                MessageBox.Show("Erreur lors de l'envoi du fichier : " + ex.Message);
                 button8.Enabled = true;
            }
        }



        private void button2_Click(object sender, EventArgs e)
        {
            var filePath = string.Empty;
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.InitialDirectory = "c:\\";
                openFileDialog.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
                openFileDialog.FilterIndex = 2;
                openFileDialog.RestoreDirectory = true;
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    //Get the path of specified file
                    filePath = openFileDialog.FileName;
                }
            }
            textBox2.Text = filePath;
        }
        public static string GetUserName()
        {
            // Obtient l'identité de l'utilisateur actuellement connecté à la session
            WindowsIdentity currentIdentity = WindowsIdentity.GetCurrent();

            if (currentIdentity != null)
            {
                // Récupère le nom d'utilisateur à partir de l'identité
                string userName = currentIdentity.Name;

                // Vous pouvez extraire uniquement le nom d'utilisateur (et non le domaine) si nécessaire
                int index = userName.IndexOf("\\");
                if (index != -1)
                {
                    userName = userName.Substring(index + 1);
                }

                return userName;
            }
            else
            {
                // L'identité actuelle n'a pas pu être récupérée
                return "Utilisateur inconnu";
            }
        }
        public static string GetLocalIPAddress()
        {
            string hostName = Dns.GetHostName();
            IPHostEntry hostEntry = Dns.GetHostEntry(hostName);
            foreach (IPAddress address in hostEntry.AddressList)
            {
                if (address.AddressFamily == AddressFamily.InterNetwork)
                {
                    return address.ToString();
                }
            }
            return null;
        }

        private async void button3_Click(object sender, EventArgs e)
        {
            await Scan();
        }
        private async Task Scan()
        {
            button3.Enabled = false;
            
            //listView2.Items.Clear();

            string ipAddress = GetLocalIPAddress();
            string[] ipParts = ipAddress.Split('.');
            string baseIp = ipParts[0] + "." + ipParts[1] + "." + ipParts[2] + ".";
            //List<Task<bool>> tasks = new List<Task<bool>>();
            var taskss = new Task<bool>[254];
            for (int i = 1; i <= 254; i++)
            {
                string targetIp = baseIp + i;
                taskss[i - 1] = Taches(targetIp, Port);

            }
            bool[] results = await Task.WhenAll(taskss);
            button3.Enabled = true;
            
        }
        public async Task<bool> Taches(string ipAddress, int port)
        {
            var task = TestConnectivityAsync(ipAddress, port);
            var timeoutTask = Task.Delay(1000); // Délai maximum de 1 seconde

            // Utilisez Task.WhenAny pour attendre la tâche ou le délai maximum
            var completedTask = await Task.WhenAny(task, timeoutTask);

            if (completedTask == task)
            {
                // La tâche a réussi
                bool result = await task;
                return result;
            }
            else
            {
                // La tâche a pris trop de temps
                return false;
            }
        }

        public async Task<bool> TestConnectivityAsync(string ipAddress, int port)
        {

            using (TcpClient client = new TcpClient())
            {
                if (ipAddress != GetLocalIPAddress())
                {
                    try
                    {
                        await client.ConnectAsync(ipAddress, Port);

                        using (NetworkStream stream = client.GetStream())
                        {
                            byte[] fileNameBytes = System.Text.Encoding.UTF8.GetBytes("Requette_Btransfert" + ";" + GetLocalIPAddress() + ";" + GetUserName());
                            int fileNameSize = fileNameBytes.Length;
                            byte[] fileNameSizeBytes = BitConverter.GetBytes(fileNameSize);
                            await stream.WriteAsync(fileNameSizeBytes, 0, fileNameSizeBytes.Length);
                            await stream.WriteAsync(fileNameBytes, 0, fileNameSize);
                        }

                        return true;
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                }
                return false;
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            
            coller();
        }
        private void coller()
        {
            string nom = $"Screenshot_{DateTime.Now:yyyyMMddHHmmss}.png";
            string Directory = Path.Combine(tempDirectory, nom);
            try
            {
                if (Clipboard.ContainsImage())
                {
                    Image clipboardImage = Clipboard.GetImage();

                    if (clipboardImage != null)
                    {
                        clipboardImage.Save(Directory);
                        textBox2.Text = Directory;
                    }
                }
                if (Clipboard.ContainsFileDropList())
                {
                    var fileDropList = Clipboard.GetFileDropList();
                    if (fileDropList.Count > 0)
                    {
                        string firstFilePath = fileDropList[0];
                        string targetPath = Path.Combine(tempDirectory, Path.GetFileName(firstFilePath));
                        File.Copy(firstFilePath, targetPath);
                        textBox2.Text = targetPath;
                    }
                    else
                    {

                    }
                }
                else
                {

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur : " + ex.Message);
            }
        }
        private void listView1_ItemActivate(object sender, EventArgs e)
        {
        }

        private void listView2_ItemActivate(object sender, EventArgs e)                         //METTRE IP SELECTIONE DANS TEXTBOX
        {
            textBox1.Text = listView2.SelectedItems[0].SubItems[0].Text;
        }
        string fichselect;
        private void listView1_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)                         //DETECTION CHANGEMENT SELECTION LISTVIEW
        {
            if (e.IsSelected)
            {
                fichselect = listView1.SelectedItems[0].SubItems[0].Text;
                button5.Enabled = true; button6.Enabled = true; button7.Enabled = true;
                e.Item.ForeColor = SystemColors.GrayText;
            }
            else
            {
                fichselect = null;
                button5.Enabled = false; button6.Enabled = false; ; button7.Enabled = false;
            }

        }
        private void button5_Click(object sender, EventArgs e)                                  //COPIER LE FICHIER
        {
            if (fichselect != null)
            {
                string filePath = Path.Combine(tempDirectory, fichselect);
                StringCollection fileCollection = new StringCollection();
                fileCollection.Add(filePath);

                // Copiez la liste de fichiers dans le presse-papiers
                Clipboard.SetFileDropList(fileCollection);
            }
        }

        private void button6_Click(object sender, EventArgs e)                                      //ENREGISTRER SOUS LE FICHIER
        {
            if (fichselect != null)
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.FileName = System.IO.Path.GetFileName(Path.Combine(tempDirectory, fichselect));
                saveFileDialog.Title = "Enregistrer sous...";

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string destinationFilePath = saveFileDialog.FileName;

                    try
                    {
                        System.IO.File.Copy(Path.Combine(tempDirectory, fichselect), destinationFilePath);
                        
                    }
                    catch (Exception ex)
                    {
                        
                    }
                }
            }
        }

        private void button7_Click(object sender, EventArgs e)
        {
            if (fichselect != null)
            {
                string filePath = Path.Combine(tempDirectory, fichselect);
                Process.Start(filePath);
            }

        }
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool FlashWindowEx(ref FLASHWINFO pwfi);

        [StructLayout(LayoutKind.Sequential)]
        public struct FLASHWINFO
        {
            public uint cbSize;
            public IntPtr hwnd;
            public uint dwFlags;
            public uint uCount;
            public uint dwTimeout;
        }
        private void FlashWindow()
        {
            FLASHWINFO fw = new FLASHWINFO();
            fw.cbSize = Convert.ToUInt32(Marshal.SizeOf(typeof(FLASHWINFO)));
            fw.hwnd = this.Handle;
            fw.dwFlags = 0x00000003 | 0x0000000C; // Flash both the window and the taskbar button
            fw.uCount = uint.MaxValue; // Keep flashing until the window comes to the foreground
            fw.dwTimeout = 0;
            FlashWindowEx(ref fw);
        }
        private void Form1_Activated(object sender, EventArgs e)
        { 
        }

        private void Form1_Enter(object sender, EventArgs e)
        {

        }

        private void textBox1_Enter(object sender, EventArgs e)
        {
            if (textBox1.Text == "Adresse IP")
            {
                textBox1.Text = "";
                textBox1.ForeColor = SystemColors.WindowText;
            }
        }

        private void textBox1_Leave(object sender, EventArgs e)
        {
            if (textBox1.Text.Length == 0)
            {
                textBox1.Text = "Adresse IP";
                textBox1.ForeColor = SystemColors.GrayText;
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            textBox1.ForeColor = SystemColors.WindowText;
            boutonsenvoi();
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            textBox2.ForeColor = SystemColors.WindowText;
            boutonsenvoi();
        }
        private void boutonsenvoi()
        {
            IPAddress ipAddress;
            if (IPAddress.TryParse(textBox1.Text, out ipAddress) && (Path.IsPathRooted(textBox2.Text)))
            {
                button1.Enabled = true;
                if (listView2.Items.Count != 0)
                {
                    button8.Enabled = true;
                }
                else
                {
                    button8.Enabled = false;
                }
            }
            else
            {
                button8.Enabled = false;
                button1.Enabled = false;
            }
        }

        private void textBox2_Enter(object sender, EventArgs e)
        {
            if (textBox2.Text == "Chemin Fichier")
            {
                textBox2.Text = "";
                textBox2.ForeColor = SystemColors.WindowText;
            }
        }

        private void textBox2_Leave(object sender, EventArgs e)
        {
            if (textBox2.Text.Length == 0)
            {                
                textBox2.Text = "Chemin Fichier";
                textBox2.ForeColor = SystemColors.GrayText;
            }
        }

        private void RemoveItemFromList(ListViewItem item)
        {
            
            for (int i = listView2.Items.Count - 1; i >= 0; i--)
            {
                ListViewItem items = listView2.Items[i];
                if (items.SubItems[0].Text == item.SubItems[0].Text && items.SubItems[1].Text == item.SubItems[1].Text)
                {
                    listView2.Items.RemoveAt(i);
                    label4.Text = "Recepteurs : " + listView2.Items.Count;
                }
            }
            boutonsenvoi();
        }
        private void AddItemToList(ListViewItem item)
        {
            
            if (!ItemExist(item))
            {
                listView2.Items.Add(item) ;
                label4.Text = "Recepteurs : " + listView2.Items.Count;
            }
            else
            {
                
            }
            boutonsenvoi();
        }
        private bool ItemExist(ListViewItem item)
        {
            bool itemExists = false;
            foreach (ListViewItem items in listView2.Items)
            {
                if (items.SubItems[0].Text == item.SubItems[0].Text && items.SubItems[1].Text == item.SubItems[1].Text)
                {
                    
                    return true; 
                }
            }
            return false;
        }

        private async void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            
        }

        private async void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            foreach (ListViewItem items in listView2.Items)
            {
                await Task.Run(() => Message(items.SubItems[0].Text, "Application_Fermee", null));
            }
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            Scan();
        }
        bool tous = false;
        private async void button8_Click(object sender, EventArgs e)
        {
            tous = true;
            foreach (ListViewItem items in listView2.Items)
            {
                await Envoi_Fichier(items.SubItems[0].Text, textBox2.Text);                
            }
            tous = false;
        }
        public async void MessageE(string msg)
        {
            foreach (ListViewItem items in listView2.Items)
            {
                await Message(items.SubItems[0].Text, "Message", msg);
            }
        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void button9_Click(object sender, EventArgs e)
        {
            envoichat();
                 
        }
        private void envoichat()
        {
            if (textBox3.Text!="pastèque")
            {
                richTextBox1.Text += $"{GetUserName()} : {textBox3.Text}{Environment.NewLine}{Environment.NewLine}";
                MessageE(textBox3.Text);
                textBox3.Text = "";
            }
            else
            {
                new Form3().ShowDialog();
            }
            
        }


        private void textBox3_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                
                if (textBox3.Text != null)
                {
                    envoichat();
                }
            }
        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControl1.SelectedTab == tabPage1)
            {
                tabControl1.TabPages[0].BackColor = SystemColors.Control;
            }
            else
            {
                tabControl1.TabPages[1].BackColor = SystemColors.Control;
            }
        }

        private void tabControl1_DrawItem(object sender, DrawItemEventArgs e)
        {

        }

        private void Form1_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {

        }

        private void Form1_KeyPress(object sender, KeyPressEventArgs e)
        {

        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {

        }
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // Vérifiez si la combinaison de touches Ctrl+V est pressée
            if (keyData == (Keys.Control | Keys.V))
            {
                // Exécutez votre fonction ici
                coller();
                return true; // Indique que la commande a été traitée
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}
