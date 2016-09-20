using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;


//Libraries Required 
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Threading;
using System.IO;
using System.Media;

namespace Ghetto_Pacman
{
    public partial class frmMain : Form
    {
        //Sound Variables 
        SoundPlayer sp; //used to play WAKA 

        //Enemp Variables 
        List<Ghost> Ghosts; //Holds all ghosts 
        int SepGhosts = 0; //Used to seperate ghosts start time 

        //Map Variables 
        string MapPath = Application.StartupPath + "\\Map.map"; //Location of the map file 
        int mapSize = 20; //The size of the map 
        int[, ,] Map; //The map itself in a 3D Array format (x, y, z) 
        int TileSize = 32; //The size of the tiles 

        //PLayer Variables 
        int playerScore = 0; //players score 
        int playerX = 0; //players x coord 
        int playerY = 0; //players y coord 
        int playerDirection = 0; //players movement direction 
        int MouthType = 0; //the type of animation to use for pacman open or closed
        int PlayerLifes = 3; //how many lifes the player has 

        //Graphic Variables 
        Graphics BackBuffer; //double buffer (draws the frame)
        Graphics Buffer; //Draw the frame on the screen 
        Bitmap Screen; //The frame 
        Rectangle rctDestination; //used sometimes when drawing images or tiles 
        Bitmap food; //Holds the food image in the memory to save code / time 

        //FPS Variable 
        int intFPS; //The Average FPS 
        int intTick; //count how many frames were draw in the second
        int tSec = DateTime.Now.Second; //For timing the counter for FPS 

        /// <summary>
        /// Intilizes everything, this code is ran as soon as the frmMain is created in program.cs 
        /// </summary>
        public frmMain()
        {
            InitializeComponent(); //Prepare the form and all variables 

            //Setup our sound player to play WAKA WAKA 
            sp = new SoundPlayer(Properties.Resources.pacman_chomp);

            //Setup the map (just make it blank)
            Map = new int[mapSize, mapSize, 10];

            //Load the map settings 
            LoadGame(MapPath); 

            /*
             * Prepare to add out ghosts 
             * 1. Create new ghosts 
             * 2. Set ghost color (For old version)
             * 3. Set the ghosts image 
             * 4. Add the ghost to the enemy list 
             */

            //Intializes our list of ghosts 
            Ghosts = new List<Ghost>();

            //Red Ghost 
            Ghost g1 = new Ghost();
            g1.Color = Brushes.Red;
            g1.img = Properties.Resources.red;
            Ghosts.Add(g1);

            //Blue Ghost
            Ghost g2 = new Ghost();
            g2.Color = Brushes.Blue;
            g2.img = Properties.Resources.blue;
            Ghosts.Add(g2);

            //Pink Ghost
            Ghost g3 = new Ghost();
            g3.Color = Brushes.Pink;
            g3.img = Properties.Resources.pink;
            Ghosts.Add(g3);
            /* End of Ghost Creation */

            /* Setup Default Locations */ 
            for (int i = 0; i < Ghosts.Count; i++) //Loop through all tiles 
            {
                Ghosts[i].ghostX = (mapSize / 2) - 1; //Set the ghost X position in the middle of the screen (middle tile)
                Ghosts[i].ghostY = (mapSize / 2) - 1; //Set the ghost Y position in the middle of the screen (middle Tile)
                Ghosts[i].CanMove = true; //Old method used before to stop ghosts from overlapping, disreguard this line 
                Ghosts[i].img.MakeTransparent(Color.Black); //Remove the background (black color) from all ghosts 
            }
            /* End of ghost default locations */

            //Set our food image, and make the black transparent (for overlapping)
            food = Properties.Resources.food;
            food.MakeTransparent(Color.Black);

            //Set the player to his position 
            playerX = 9; //9 Tiles -> 
            playerY = mapSize - 5;// (mapSize -> 20) - 5 = 15 v

            //Center the Form before it shows 
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
        }

        /// <summary>
        /// This method will reset all ghost and player positions 
        /// </summary>
        public void ResetPositions()
        {
            //loop through all ghosts 
            for (int i = 0; i < Ghosts.Count; i++)
            {
                Ghosts[i].ghostX = (mapSize / 2) - 1; //set default X 
                Ghosts[i].ghostY = (mapSize / 2) - 1; //Set Default Y 
            }

            //Set player potitions 
            playerX = 9;
            playerY = mapSize - 5;
        }

        /// <summary>
        /// This method will load right before the game window is shown 
        /// </summary>
        private void Form1_Load(object sender, EventArgs e)
        {
            /* The two lines below are required because of our game loop */
            this.Show(); //Show the form 
            this.Focus(); //Focus on the form 

            /* Add Event Handlers Manually  */
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(Form1_KeyDown); //For movement 
            this.MouseClick += new MouseEventHandler(Form1_MouseClick); //For Tile Editing 
            this.MouseMove += new MouseEventHandler(Form1_MouseMove); //For Tile Editing
            this.FormClosing += new FormClosingEventHandler(Form1_FormClosing); //To kill our inifite loops 

            //Setup our form size at run time 
            this.Height = 32 * 22;
            this.Width = 32 * 21;

            //Prompt the user to start the game
            MessageBox.Show("Get Ready!\nUse the [W][A][S][D] Keys To Control Pacman!");

            Cursor.Hide(); //Hide the cursor so it doesnt get in the way of the player 

            //Start the game 
            GameStart();
        }


        /// <summary>
        /// When the form closes, this event will run.
        /// </summary>
        void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            System.Diagnostics.Process.GetCurrentProcess().Kill(); //Kill the process of the application running because of our infinite loops, if we do not do this, our process will stay open with OUR LOOPS STILL RUNNING
        }

        /// <summary>
        /// This event will execute when the mouse is moved over the form 
        /// </summary>
        void Form1_MouseMove(object sender, MouseEventArgs e)
        {
            //If the left mouse button is clicked
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                //Find the mouse tile 
                int mouseX = 0;
                int mouseY = 0;

                mouseX = e.X / TileSize; //Get mouse X Tile 
                mouseY = e.Y / TileSize; //Get mouse Y Tile 

                Map[mouseX, mouseY, 1] = 2; //Set the mouse title to blue boarder 
            }
            else if(e.Button == System.Windows.Forms.MouseButtons.Right) //if the right mouse button is being clicked... 
            {
                //find mouse tile 
                int mouseX = 0;
                int mouseY = 0;

                mouseX = e.X / TileSize; //Get mouse X Tile 
                mouseY = e.Y / TileSize; //Get mouse Y Tile 

                Map[mouseX, mouseY, 1] = 0; //remove the boarder from the map 
            }
        }

        /// <summary>
        /// This event will run only when the mouse is clicked on the form 
        /// </summary>
        void Form1_MouseClick(object sender, MouseEventArgs e)
        {
            //If the left mouse button is clicked
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                //Find the mouse tile 
                int mouseX = 0;
                int mouseY = 0;

                mouseX = e.X / TileSize; //Get mouse X Tile 
                mouseY = e.Y / TileSize; //Get mouse Y Tile 

                Map[mouseX, mouseY, 1] = 2; //Set the mouse title to blue boarder 
            }
            else if (e.Button == System.Windows.Forms.MouseButtons.Right) //if the right mouse button is being clicked... 
            {
                //find mouse tile 
                int mouseX = 0;
                int mouseY = 0;

                mouseX = e.X / TileSize; //Get mouse X Tile 
                mouseY = e.Y / TileSize; //Get mouse Y Tile 

                Map[mouseX, mouseY, 1] = 0; //remove the boarder from the map 
            }
        }

        /// <summary>
        /// This event will run when a keystroke is pressed 
        /// </summary>
        void Form1_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            //Figure out which key was pressed 
            switch (e.KeyCode)
            {
                case Keys.C: //show the cursor (used when editing the map) 
                    Cursor.Show();
                    break;
                case Keys.V: //Hide cursor 
                    Cursor.Hide();
                    break;
                case System.Windows.Forms.Keys.A: //Move left 
                    if (Map[playerX - 1, playerY, 1] != 2) //Make sure we arent going into a boundary 
                    {
                        playerDirection = 3; //Set the new player direction to move in 
                    }
                    break;
                case System.Windows.Forms.Keys.S: //Move Down 
                    if (Map[playerX, playerY + 1, 1] != 2) //Make sure we arent going into a boundary
                    {
                        playerDirection = 2; //Set the new player direction to move in 
                    }
                    break;
                case System.Windows.Forms.Keys.D: //Move Right 
                    if (Map[playerX + 1, playerY, 1] != 2) //Make sure we arent going into a boundary
                    {
                        playerDirection = 4; //Set the new player direction to move in 
                    }
                    break;
                case System.Windows.Forms.Keys.W: //Move up 
                    if (Map[playerX, playerY - 1, 1] != 2) //Make sure we arent going into a boundary
                    {
                        playerDirection = 1; //Set the new player direction to move in 
                    }
                    break;
                case Keys.F1: //Save the map file 
                    SaveGame(MapPath);
                    break;
                case Keys.F2: //Load the map file 
                    LoadGame(MapPath);
                    break;
            }
        }

        /// <summary>
        /// This method will setup all of the graphic variables and start our main game loop 
        /// </summary>
        public void GameStart()
        {
            Buffer = this.CreateGraphics(); //This graphic will draw our frame to the screen 
            Screen = new System.Drawing.Bitmap(this.Height, this.Width); //This is our Frame 
            BackBuffer = Graphics.FromImage(Screen); //This graphic will make our frame 
            GameLoop(); //This will run our game 
        }

        /// <summary>
        /// This method will run in a loop to play our game 
        /// </summary>
        public void GameLoop()
        {
            sp.PlayLooping(); //start the waka waka (sound)

            SepGhosts = Ghosts.Count; //Setup our sep ghosts to seperate when our ghosts are released 

            /*
             * Create a new thread to seperate our ghosts on,
             * if a new thread is not created the game will freeze
             * on the main thread every time we get to the sleep method 
            */
            new Thread(() =>
                {
                    while (SepGhosts != 0) //Continue releasing more ghosts till all ghosts are released 
                    {
                        SepGhosts--; //Release one more ghost
                        Thread.Sleep(10000); //Wait 10 seconds before releasing another 
                    }
                }).Start(); //Start the new thread 

            /* 
             * This thread will move all of our ghosts all at the same time,
             * this code is in a new thread as it is timed to happen every 175ms (25ms slower then the player),
             * if this code was ran in the main game draw loop it would freeze up the code as well 
             */ 
            new Thread(() =>
                {
                    while (true) //continue moving the ghosts forever 
                    {
                        for (int i = 0; i < Ghosts.Count - SepGhosts; i++) //Loop through all ghosts (At first because of sepghosts, it does one ghost, then two, then all three 
                        {
                            if (Ghosts[i].CanMove) //if a ghost is not overlapping, then move to the next position
                            {
                                //Movement based off of the player 
                                if (Ghosts[i].ghostY > playerY && Map[Ghosts[i].ghostX, Ghosts[i].ghostY - 1, 1] != 2) //If the player is higher and you can move up 1 tile 
                                {
                                    Ghosts[i].ghostY--; //Move the ghost up one 
                                }
                                else if (Ghosts[i].ghostY < playerY && Map[Ghosts[i].ghostX, Ghosts[i].ghostY + 1, 1] != 2 && Ghosts[i].ghostX != (mapSize / 2) - 1) //If the player is under the ghost (also the last part stops the ghost from going back to the original start position)
                                {
                                    Ghosts[i].ghostY++; //Move the ghost down one 
                                }
                                else if (Ghosts[i].ghostX > playerX && Map[Ghosts[i].ghostX - 1, Ghosts[i].ghostY, 1] != 2) //if the player is more left then the ghost and it can move left 
                                {
                                    Ghosts[i].ghostX--; //Move the ghost left one 
                                }
                                else if (Ghosts[i].ghostX < playerX && Map[Ghosts[i].ghostX + 1, Ghosts[i].ghostY, 1] != 2) //If the player is more right then the ghost, and it can move right 
                                {
                                    Ghosts[i].ghostX++; //Move the ghost right one 
                                }
                                else //If the ghost can not move towards the player, lets move somewhere else anyways.. 
                                {
                                    if (Ghosts[i].ghostY - 1 != -1) //If the ghost wont go out of bounds
                                    {
                                        if (Map[Ghosts[i].ghostX, Ghosts[i].ghostY - 1, 1] != 2) //Check if the ghost can go up 
                                        {
                                            Ghosts[i].ghostY--; //Move Up 
                                        }
                                    }
                                    else if (Ghosts[i].ghostX + 1 != mapSize) //If the ghost wont go out of bounds
                                    {
                                        if (Map[Ghosts[i].ghostX + 1, Ghosts[i].ghostY + 1, 1] != 2) //Check of the ghost can go right 
                                        {
                                            Ghosts[i].ghostX++; //Move Right 
                                        }
                                    }
                                    else if (Ghosts[i].ghostX - 1 != -1)//If the ghost wont go out of bounds
                                    {
                                        if (Map[Ghosts[i].ghostX - 1, Ghosts[i].ghostY, 1] != 2) //Check if the ghost can go left 
                                        {
                                            Ghosts[i].ghostX--; //Move Left 
                                        }
                                    }
                                    else if (Ghosts[i].ghostY + 1 != mapSize && Ghosts[i].ghostX != (mapSize / 2) - 1) //If the ghost wont go out of bounds (Also make sure we wont go back to the start position)
                                    {
                                        if (Map[Ghosts[i].ghostX, Ghosts[i].ghostY + 1, 1] != 2) //Check if the ghost can go down 
                                        {
                                            Ghosts[i].ghostY++; //Move Down 
                                        }
                                    }
                                }
                            }
                            else
                            {
                                Ghosts[i].CanMove = true; //Stop the ghosts from over lapping (old way)
                            }
                        }
                        Thread.Sleep(175); //Wait 175ms before moving again 
                        CheckGhostOverLap(); //New  way to stop ghosts from over lapping 
                    }
                }).Start(); //Start the thread 

            /*
             * Player movement + Animation Settings,
             * Same as ghost, this must run on a new thread to insure smoother game play
             */
            new Thread(() =>
                {
                    while (true) //Loop forever 
                    {
                        if (MouthType == 1) //If the mouth is open 
                            MouthType = 0; //Close it 
                        else //if not... 
                            MouthType = 1; //Open the mouth 

                        //Determine the direction to move the player in
                        switch (playerDirection)
                        {
                            case 1: //Up
                                if (playerY - 1 > -1) //Check if we are going out of bounds 
                                {
                                    if (Map[playerX, playerY - 1, 1] != 2) //Check if the next tile is free to move into or if it's a block ( == 2 means it's a block, != 2 means it has food or is empty)
                                    {
                                        playerY--; //move player vertically
                                    }
                                }
                                else
                                {
                                    playerDirection = 0; //Can't move this way so stop pacman
                                }
                                break;
                            case 2: //Down 
                                if (playerY + 1 < 20) //Check if we are going out of bounds 
                                {
                                    if (Map[playerX, playerY + 1, 1] != 2) //Check if the next tile is free to move into or if it's a block ( == 2 means it's a block, != 2 means it has food or is empty)
                                    {
                                        playerY++; //move player vertically 
                                    }
                                }
                                else
                                {
                                    playerDirection = 0; //Can't move this way so stop pacman
                                }
                                break;
                            case 3: //Left 
                                if (playerX - 1 > -1) //Check if we are going out of bounds 
                                {
                                    if (Map[playerX - 1, playerY, 1] != 2) //Check if the next tile is free to move into or if it's a block ( == 2 means it's a block, != 2 means it has food or is empty)
                                    {
                                        playerX--; //move player horizontally
                                    }
                                }
                                else
                                {
                                    playerDirection = 0; //Can't move this way so stop pacman
                                }
                                break;
                            case 4: //Right 
                                if (playerX + 1 < 20) //Check if we are going out of bounds 
                                {
                                    if (Map[playerX + 1, playerY, 1] != 2) //Check if the next tile is free to move into or if it's a block ( == 2 means it's a block, != 2 means it has food or is empty)
                                    {
                                        playerX++; //move player horizontally 
                                    }
                                }
                                else
                                {
                                    playerDirection = 0; //Can't move this way so stop pacman
                                }
                                break;
                            default: //no movement, so stop (Direction = 0, or for some reason playerdirection is more then 4)
                                break;
                        }

                        //WAKA WAKA, AKA eat food 
                        if (Map[playerX, playerY, 1] == 1) //if the players current tile = 1, then there is food to eat 
                        {
                            playerScore += 10; //increase score from the food 
                            Map[playerX, playerY, 1] = 0; //Remove the food from the map 
                        }

                        //Check if the player Died, this code is ran here instead of the main game loop in order to give the player a short 25ms advantage (may be required on older machines as well in order to draw the frame to show the player how  they lost)
                        for (int i = 0; i < Ghosts.Count; i++) //Loop through all ghosts 
                        {
                            if (Ghosts[i].ghostX == playerX && Ghosts[i].ghostY == playerY) //If the ghost x and y are the same as the players 
                            {
                                PlayerLifes--; //remove a life 
                                SoundPlayer dead = new SoundPlayer(Properties.Resources.pacman_death); //create a new death sound player 
                                sp.Stop(); //stop our WAKA 
                                sp.Tag = ""; //for older version, when I was playing with the sound on the main game loop 
                                Thread.Sleep(500); //Wait 5 seconds for a WAKA WAKA to end 
                                dead.Play(); //Play the death sound 
                                Thread.Sleep(2000); //Wait for the death sound to end 

                                if (PlayerLifes == -1) //If the player lost all lifes
                                {
                                    Cursor.Show();
                                    MessageBox.Show("YOU LOSE!"); //Tell him 
                                    System.Diagnostics.Process.GetCurrentProcess().Kill(); //Close the program and all infinite loops 
                                }

                                //Player lost a life, so lefts reset all positions 
                                ResetPositions();

                                //Start WAKA WAKA again 
                                sp.PlayLooping();
                            }
                        }

                        //Do this loop every 150 ms (25ms faster then ghosts to give the player an advantage) 
                        Thread.Sleep(150);
                    }
                }).Start(); //Start the thread 

            //Actual Game Loop
            while (true)
            {
                //Stop the program from freezing
                Application.DoEvents();

                //If the player collected all the food / balls then trigger a win event 
                if (playerScore == 1810)
                {
                    //Tell the player he won 
                    Cursor.Show();
                    MessageBox.Show("YOU WIN!");

                    //Kill thep process (I was lazy and used infinite loops)
                    System.Diagnostics.Process.GetCurrentProcess().Kill();
                }

                //Limit Frames?
                //if (intTick < 30)
                //{
                //    Thread.Sleep(10);
                //}

                //Draw the next frame 
                DrawGame();
                
                //Update the Frames Per Second
                CheckFPS();
            }
        }

        /// <summary>
        /// Update The FPS and FPS Ticks 
        /// </summary>
        private void CheckFPS()
        {
            if (tSec != DateTime.Now.Second) //Check if a second has passed 
            {
                intFPS = intTick; //set the new FPS 
                intTick = 0; //reset ticks 
                tSec = DateTime.Now.Second; //set the next second 
            }
            else //if a second has not passed 
            {
                intTick++; //increased ticks (Ticks are which frame is currently being drawn) 
            }
        }

        /// <summary>
        /// The new method for stoping ghosts from overlapping.
        /// </summary>
        public void CheckGhostOverLap()
        {
            bool Detected = false; //Determine if a ghost overlap was detected 

            for (int i = 0; i < Ghosts.Count; i++) //Foreach ghost in the game
            {
                for (int j = 0; j < Ghosts.Count; j++) //Twice
                {
                    if (j != i) //if we arent looking at ourself... 
                    {
                        if (Ghosts[j].ghostX == Ghosts[i].ghostX && Ghosts[j].ghostY == Ghosts[i].ghostY && Ghosts[i].ghostX != (mapSize / 2) - 1 && Detected == false) //Check if the ghosts are overlapping 
                        {
                            Ghosts[j].CanMove = false; //If they are block them, for their next movement
                            Detected = true; //Tell the method that we already found the overlap 
                        }
                    }
                }
            }
        }

        /// <summary>
        /// The code that draws the frame and performs all the magic 
        /// </summary>
        public void DrawGame()
        {
            //Setup a new frame by erasing the last one 
            BackBuffer.Clear(Color.Black);

            //Loop through all map tiles 
            for (int x = 0; x < mapSize; x++) //X Coords
            {
                for (int y = 0; y < mapSize; y++) //Y Coords 
                {
                    rctDestination = new System.Drawing.Rectangle(x * TileSize, y * TileSize, TileSize, TileSize); //Setup the new destination to draw the tile to 

                    //BackBuffer.DrawRectangle(Pens.Red, rctDestination);

                    if (Map[x, y, 1] == 1) //If the current tile is food... 
                    {
                        //BackBuffer.FillEllipse(Brushes.Aqua, rctDestination);
                        BackBuffer.DrawImage(food, rctDestination); //draw food to the tile 
                    }

                    if (Map[x, y, 1] == 2) //If the current tile is a block 
                    {
                        BackBuffer.FillRectangle(Brushes.Blue, rctDestination); //Just fill the tile in with blue boarder or block 
                    }
                }
            }

            //Pacman Drawing 
            rctDestination = new Rectangle(playerX * TileSize, playerY * TileSize, TileSize, TileSize);

            //Draw pacmans body 
            BackBuffer.FillEllipse(Brushes.Yellow, rctDestination);

            //Pacmans mouth 
            if (MouthType == 1)
            {
                switch (playerDirection) //Determine where to draw the mouth based off his direction 
                {
                    case 1: //UP
                        //Draw a Black Triangle to create the mouth of pack man in the appropriate direction 
                        BackBuffer.FillPolygon(Brushes.Black, new Point[] {
                        new Point((playerX * TileSize) + (TileSize / 2), (playerY * TileSize) + (TileSize / 2)),
                        new Point((playerX * TileSize), (playerY* TileSize)),
                        new Point((playerX * TileSize) + TileSize, (playerY * TileSize))
                    });
                        break;
                    case 2: //Down 
                        //Draw a Black Triangle to create the mouth of pack man in the appropriate direction 
                        BackBuffer.FillPolygon(Brushes.Black, new Point[] {
                        new Point((playerX * TileSize) + (TileSize / 2), (playerY * TileSize) + (TileSize / 2)),
                        new Point((playerX * TileSize), (playerY* TileSize) + TileSize),
                        new Point((playerX * TileSize) + TileSize, (playerY * TileSize) + TileSize)
                    });
                        break;
                    case 3: //LEFT
                        //Draw a Black Triangle to create the mouth of pack man in the appropriate direction 
                        BackBuffer.FillPolygon(Brushes.Black, new Point[] {
                        new Point((playerX * TileSize) + (TileSize / 2), (playerY * TileSize) + (TileSize / 2)),
                        new Point((playerX * TileSize), (playerY * TileSize)),
                        new Point((playerX * TileSize), (playerY * TileSize) + TileSize)
                    });
                        break;
                    case 4: //RIGH
                        //Draw a Black Triangle to create the mouth of pack man in the appropriate direction 
                        BackBuffer.FillPolygon(Brushes.Black, new Point[] {
                        new Point((playerX * TileSize) + (TileSize / 2), (playerY * TileSize) + (TileSize / 2)),
                        new Point((playerX * TileSize) + TileSize, (playerY * TileSize)),
                        new Point((playerX * TileSize) + TileSize, (playerY * TileSize) + TileSize)
                    });
                        break;
                    default: //no direction, he is not moving 
                        //do nothing :D 
                        break;
                }
            }

            //Draw npcs
            foreach (Ghost g in Ghosts)
            {
                BackBuffer.DrawImage(g.img, g.ghostX * TileSize, g.ghostY * TileSize, TileSize, TileSize); //Draw each ghost
            }

            //Draw Life Icons 
            for (int i = 0; i < PlayerLifes; i++)
            {
                rctDestination = new Rectangle(10 + (TileSize * i), ((mapSize - 1) * TileSize) + 10, TileSize, TileSize); //Setup the point in which to draw the life
                BackBuffer.FillRectangle(Brushes.Black, rctDestination); //Make it black for the BG 
                BackBuffer.FillEllipse(Brushes.Yellow, rctDestination); //Make The Yellow Body of packman 

                //Draw his mouth on the right side of his body 
                BackBuffer.FillPolygon(Brushes.Black, new Point[] {
                    //center point 
                    new Point(((10 + (TileSize * i))) + (TileSize / 2), ((mapSize - 1) * TileSize) + (TileSize / 2) + 10),
                    //Outter points 
                    new Point(((10 + (TileSize * i))) + TileSize, ((mapSize - 1) * TileSize) + 10),
                    new Point(((10 + (TileSize * i))) + TileSize, ((mapSize - 1) * TileSize) + TileSize + 10)
                });
            }

            //Draw the debug information at the top of the screen on the left side 
            BackBuffer.DrawString("Frames Per Second: " + intFPS + Environment.NewLine + "Current Frame: " + intTick + Environment.NewLine + "Score: " + playerScore + Environment.NewLine + "Current Direction: " + playerDirection, DefaultFont, Brushes.Lime, new Point(10, 10));

            //Draw the Frame to the players screen 
            Buffer.DrawImage(Screen, 0, 0);

        }

        /// <summary>
        /// Save the map file to a location.
        /// </summary>
        /// <param name="path">The location to save the map to</param>
        public void SaveGame(string path)
        {
            MemoryStream ms = new MemoryStream(); //Memory stream to store information in 
            BinaryWriter bw = new BinaryWriter(ms); //Binary Writer to write information with 

            for (int x = 0; x < Map.GetLength(0); x++) //Loop through all X Coords
            {
                for (int y = 0; y < Map.GetLength(1); y++) //Loop through all Y Coords 
                {
                    for (int z = 0; z < Map.GetLength(2); z++) //Loop Through All Z Values 
                    {
                        bw.Write(Map[x, y, z]); //Write Map Information 
                    }
                }
            }

            //Write the information to a file 
            File.WriteAllBytes(path, ms.ToArray());

            //Clean up the writes and storage area
            bw.Dispose();
            ms.Dispose();

        }

        /// <summary>
        /// Load the map from a file.
        /// </summary>
        /// <param name="path">The path of the map file.</param>
        public void LoadGame(string path)
        {
            //Create a new binary reader -> Reads from a new memory stream -> Loaded the map file bytes
            BinaryReader br = new BinaryReader(new MemoryStream(File.ReadAllBytes(path)));

            for (int x = 0; x < Map.GetLength(0); x++) //Load through all map X Values 
            {
                for (int y = 0; y < Map.GetLength(1); y++) //Load all the Y Values 
                {
                    for (int z = 0; z < Map.GetLength(2); z++) //Load all Z Values 
                    {
                        Map[x, y, z] = br.ReadInt32(); //Set the map value 
                    }
                }
            }


            //Clean up 
            br.Close();
            br.Dispose();
            GC.Collect();
        }
    }

    /// <summary>
    /// This class will be used to hold ghosts information
    /// </summary>
    class Ghost
    {
        public int ghostX; //Ghost X Coord 
        public int ghostY; //Ghost Y Coord
        public Brush Color; //Ghost Color (no longer used)
        public bool CanMove; //If the ghost is allowed to move or not 
        public Bitmap img; //The ghosts Image 
    }
}
