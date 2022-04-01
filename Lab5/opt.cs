using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Lab5
{
    public partial class opt : Form
    {
        public Options Options = new Options();

        public opt()
        {
            InitializeComponent();
        }

        private void opt_Load(object sender, EventArgs e)
        {

            chkComputerPlaysFirst.Checked = Options.ComputerPlaysFirst;

        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            // Alert user if change we reset current game
            int gridSize = 3;
            if (Options.GameInProgress &&
                MessageBox.Show(this, "Reset the current game first with Game->New for ticked option to hold its effect.", "Reset Game",
                    MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) != DialogResult.OK)
            {
                DialogResult = DialogResult.None;
                return;
            }
            // Update options
            Options.GridSize = gridSize;
            Options.ComputerPlaysFirst = chkComputerPlaysFirst.Checked;
        }

    }

    public class Options
    {
        public int GridSize = 3;
        public bool ComputerPlaysFirst { get; set; }
        public bool GameInProgress { get; set; }
    }

}
