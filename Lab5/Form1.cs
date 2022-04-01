using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace Lab5
{
    public partial class Form1 : Form
    {
        private Marker Human { get; set; }
        private bool ComputerPlaysFirst { get; set; }

        private GameEngine GE;

        private static int delta = 5;  // inside border of X/O

        public Form1()
        {
            InitializeComponent(); // default render comp

            GE = new GameEngine();
            GE.SetGridSize(3); // make 3x3
            Human = Marker.X; // make human X's
            ComputerPlaysFirst = false; // default, human plays first

            GE.PlayerChanged += PlayerChanged;
            GE.GameStarted += GameStarted;
            GE.GameOver += GameOver;
            GE.GridChanged += GridChanged; // connecting board coordinates to gameengine
        }
        private void tsbRestart_Click(object sender, EventArgs e)
        {
            StartGame();
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            StartGame();
            toolStripContainer1_ContentPanel_Resize(this, e);
        }
        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            Rectangle rect = Game.ClientRectangle;
            int col = e.X / (rect.Width / GE.GridSize); // prevents overflow
            int row = e.Y / (rect.Height / GE.GridSize);
            if (GE.CanMove(row, col)) // make move by computer/user
            {
                GE.Move(row, col);  // Makes the user move
                GE.AutoMove();      // Make the computer move
            }
        }
        private void toolStripContainer1_ContentPanel_Resize(object sender, EventArgs e)
        {
            Rectangle rect = Game.Parent.ClientRectangle; // size board
            int size = Math.Max(100, Math.Min(rect.Width, rect.Height) - 18);
            Game.SetBounds((rect.Width - size) / 2, (rect.Height - size) / 2, size, size); // keep objects in container
            Game.Invalidate();
        }

        private void Game_Paint(object sender, PaintEventArgs e)
        {
            DrawGrid(e.Graphics); // uses make grid function
        }

        private void ComputerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            opt check = new opt();
            check.Options.ComputerPlaysFirst = ComputerPlaysFirst;
            check.Options.GridSize = GE.GridSize; // makes sure grid is 3x3
            check.Options.GameInProgress = !GE.GameIsOver; // ensures whether or not to make computer change after

            if (check.ShowDialog() == DialogResult.OK)
            {
                ComputerPlaysFirst = check.Options.ComputerPlaysFirst; // checks if opt buttton is enabled
                if (check.Options.GridSize != GE.GridSize)
                {
                    GE.SetGridSize(check.Options.GridSize); // checks 3x3 again
                    StartGame();
                }
                else if (!GE.GameIsOver)
                    GE.AutoMove(); // if new game, enable computer going first
            }
        }

        private void StartGame()
        {
            GE.Reset(ComputerPlaysFirst ? Human.GetOtherPlayer() : Human); // start the new game
            if (ComputerPlaysFirst)
                GE.AutoMove(); // let computer go first
        }

        void GameStarted(object sender, EventArgs e)
        {
            statusStrip.Text = String.Empty; // reset the winning stat bar
        }

        void PlayerChanged(object sender, EventArgs e)
        {
            string status = String.Empty;
            if (GE.CurrentPlayer != Marker.N)
            {
                if (GE.CurrentPlayer == Human)
                    status = "Your turn!"; // update with computer going first or user
                else
                    status = "Computing!"; // computer calculating next move(s)
            }
            statusStrip.Text = status;
        }
        void GridChanged(object sender, GridChangedArgs e)
        {
            Game.Invalidate(); // render grid changes with graphics object
        }

        void GameOver(object sender, EventArgs e)
        {
            statusStrip.Text = (GE.WinningPlayer == Marker.N) ? // check if tie/endgame conditions
                "Tied Game" :
                String.Format("{0} Wins!", GE.WinningPlayer); // print winner if not tie
        }

        private void DrawGrid(Graphics g)
        {
            var size = GE.GridSize;
            Rectangle rect = Game.ClientRectangle;
            rect.Inflate(-delta, -delta); // initialize boundary conditions
            if (rect.Width <= 0 || rect.Height <= 0) return;
            int xcell = rect.Width / size;
            int ycell = rect.Height / size;
            Pen line = new Pen(Color.Black, 4); // make pen for 3x3
            line.SetLineCap(LineCap.Round, LineCap.Round, DashCap.Round);
            g.SmoothingMode = SmoothingMode.HighQuality;

            for (int i = 1; i < size; i++)
            {
                g.DrawLine(line, rect.Left + (i * xcell), rect.Top, rect.Left + (i * xcell), rect.Bottom); // draw vert coords
                g.DrawLine(line, rect.Left, rect.Top + (i * ycell), rect.Right, rect.Top + (i * ycell)); // draw horizontal coords
            }

            for (int row = 0; row < GE.GridSize; row++)
            {
                for (int col = 0; col < GE.GridSize; col++)
                {
                    var value = GE.GetLoc(row, col); // allows gameengine to read/write to coordinates OR user
                    if (value == Marker.X)
                        DrawX(g, new Rectangle(rect.Left + col * xcell, rect.Top + row * ycell, xcell, ycell)); //User write to coords
                    else if (value == Marker.O)
                        DrawO(g, new Rectangle(rect.Left + col * xcell, rect.Top + row * ycell, xcell, ycell)); // gameengine write to coords
                }
            };
        }

        private void DrawX(Graphics g, Rectangle rect)
        {
            rect.Inflate(-delta, -delta); // set game grid boundaries
            if (rect.Width <= 0 || rect.Height <= 0) return;
            Pen line = new Pen(Color.Black, 8);
            line.SetLineCap(LineCap.Round, LineCap.Round, DashCap.Round); // make the pen for user marker lines
            g.DrawLine(line, rect.Left, rect.Top, rect.Right, rect.Bottom); // diagonal 1
            g.DrawLine(line, rect.Right, rect.Top, rect.Left, rect.Bottom); // diagonal 2
        }

        private void DrawO(Graphics g, Rectangle rect)
        {
            rect.Inflate(-delta, -delta);
            if (rect.Width <= 0 || rect.Height <= 0) return;
            Pen line = new Pen(Color.Black, 8); // make pen for computer marker lines
            g.DrawEllipse(line, rect); // make circle for computer marker
        }
    }
}
