using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Forms;

namespace BTransfert
{
    public partial class Form3 : Form
    {
        public Form3()
        {
            InitializeComponent();
            webBrowser1.ScriptErrorsSuppressed = true;
            webBrowser1.Url = new Uri("https://suika-game.app/");
        }

        private void Form3_Load(object sender, EventArgs e)
        {

        }
    }
}
