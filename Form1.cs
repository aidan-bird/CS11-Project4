using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AIDAN_BIRD___MAITLAND_KINGSMILL_Comsci_Project_4
{
    public partial class Form1 : Form
    {
        const int
            FORM_WIDTH = 600, //The width of the form.
            FORM_HEIGHT = 700, //The height of the form.
            REVIVE_DELAY = 120, //Time the player must wait before they are revived.
            PLAYER_STARTING_LIVES = 3, //How many lives the player starts with.
            ENEMY_SFX_DELAY = 18;  //Time before a new enemy sound effect can be created.
        static int
            score, //The player's score.
            stage = 0, //The current stage number
            currentEnemySFXDelay = 0,  //The current enemy sound effect delay.
            currentReviveDelay = 0,   //The current player revival time.
            playerLives;  //The current number of lives the player has.
        static List<Enemy> enemies;  //A list of enemy objects.
        static List<Bullet>
            playerBullets,  //A list containing the player's bullets.
            enemyBullets;  //A list containing the enemy's bullets.
        static List<ScheduledCreator> objectCreators;  //A list of active Scheduled Creators.
        static List<SFX> activeSFXs;  //A list of active sound objects.
        static Dictionary<string, Bitmap> graphicAssets;  //A dictionary that maps file names to bitmaps.
        static Dictionary<string, StringWriter> docs;  //A dictionary that maps file names to StringWriters.
        static Player player;  //The active player object.
        static Font STANDARD_FONT = new Font("arial", 11);  //A font used when drawing text to the form.
        int
            optionSelected = 0,  //The current option selected by the user.
            optionsAvailable;  //The number of options available.
        static float
            playerStartX,  //The player's starting x position.
            playerStartY;  //The player's starting y position.
        static Random rng = new Random();  //A random number generator that is used by all objects.
        static GameState gameState;  //The current gamestate.
        Rectangle selector = new Rectangle(239, 260, 122, 56); //A rectangle that indicates the currently selected option.
        static string
            execRoot = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, @"..\..\")),  //The programs's root directory.
            nextLevelPath,  //The file path to the next level's script.
            helpPath = execRoot + @"Resources\Text\help.txt",  //The path to the help document.
            pausedPath = execRoot + @"Resources\Text\paused.txt";  //The path to the pause document.
        public Form1() //Sets up the form.
        {
            components = new Container();
            tmr1 = new Timer(components);  //
            SuspendLayout();  //
            Name = "Form1";  //
            Size = new Size(FORM_WIDTH, FORM_HEIGHT);  //
            Text = "AIDAN_BIRD - MAITLAND_KINGSMILL Comsci_Project_4";  //
            SetStyle(ControlStyles.FixedHeight, true);  //
            SetStyle(ControlStyles.FixedWidth, true);  //
            DoubleBuffered = true;  //
            Location = new Point(
                Screen.FromControl(this).Bounds.Width / 2 - Width / 2,
                Screen.FromControl(this).Bounds.Height / 2 - Height / 2);  //
            FormBorderStyle = FormBorderStyle.FixedSingle;  //
            StartPosition = FormStartPosition.CenterScreen;  //
            ClientSize = new Size(FORM_WIDTH - 16, FORM_HEIGHT - 39);  //
            AutoScaleMode = AutoScaleMode.Font;  //
            AutoScaleDimensions = new SizeF(6, 13);  //
            FormClosing += new FormClosingEventHandler(FClosing);  //
            Shown += new EventHandler(FShown);  //
            KeyDown += new KeyEventHandler(FKeyDown);  //
            KeyUp += new KeyEventHandler(FKeyUp);  //
            ResumeLayout(false);  //
            tmr1.Enabled = true;  //
            tmr1.Interval = 16;  //
            tmr1.Tick += new EventHandler(GameLoop);  //
        }
        void Init()  //Initializes everything.
        {
            activeSFXs = new List<SFX>();  //Initializes the activeSFX list.
            playerBullets = new List<Bullet>();  //Initializes the playerBullet list.
            enemyBullets = new List<Bullet>();  //Initializes the enemyBullets list.
            enemies = new List<Enemy>();  //Initializes the enemies list.
            objectCreators = new List<ScheduledCreator>();  //Initializes the objectCreators list.
            docs = new Dictionary<string, StringWriter>();  //Initializes the doc dictionary. 
            docs.Add("helpText", new StringWriter());  //Adds the helpText document to the docs dictionary.
            docs.Add("pauseText", new StringWriter());  //Adds the pauseText document to the docs dictionary.
            graphicAssets = new Dictionary<string, Bitmap>();  //Initializes the graphicAssets dictionary.
            string[] graphicAssetPaths = Directory.GetFiles(execRoot.Clone().ToString() + @"Resources\Images\", "*.png");  //Gets the paths to all graphical assets in the Resources directory.
            for (int i = 0; i < graphicAssetPaths.Length; i++)
                graphicAssets.Add(Path.GetFileNameWithoutExtension(graphicAssetPaths[i]), (Bitmap)Image.FromFile(graphicAssetPaths[i]));  //Adds each graphical asset to the graphicAssets dictionary by mapping the graphical asset's file name to its bitmap.
            using (StreamReader sr = new StreamReader(helpPath))
            {
                string line;  //Declars a temporary line string.
                while ((line = sr.ReadLine()) != null) docs["helpText"].WriteLine(line);  //Add the contents of the help document to the doc dictionary.
            }
            using (StreamReader sr = new StreamReader(pausedPath))
            {
                string line;  //Declars a temporary line string.
                while ((line = sr.ReadLine()) != null) docs["pauseText"].WriteLine(line);  //Add the contents of the pause document to the doc dictionary.
            }
        }
        void GameLoopStop() { tmr1.Stop(); } //Stops the GameLoop.
        void GameLoopStart() { tmr1.Start(); } //Starts the GameLoop.
        void DoUpdate() //Called when the game needs to update all of it's objects.
        {
            if (player.GetState())  //Test if the player is alive.
                if (playerLives < 0)
                    GameOver();  //Call a game over when the player runs out of lives.
                else
                {
                    if (currentReviveDelay == 0)
                    {
                        player = new Player(playerStartX, playerStartY);  //Reset the player.
                        player.MakeInvincible();  //Make the player invincible.
                    }
                }
            else player.Update(); //Update  the player.
            if (objectCreators.Count - 1 > -1)   //Check if there are any objectCreators.
                for (int i = 0; i < objectCreators.Count; i++)
                    if (objectCreators[i].GetState()) objectCreators.Remove(objectCreators[i]);  //Remove all objectCreators that are done their task.
                    else objectCreators[i].Update();  //Update all objectCreators that are not done their tasks.
            if (enemyBullets.Count - 1 > -1)  //Check if there are any enemy bullets.
            {
                int tmp = enemyBullets.Count;  //Get the latest ammount of enemy bullets. 
                for (int i = 0; i < tmp; i++)
                {
                    if (enemyBullets[i].GetX() <= player.GetAabb(0) + player.GetAabb(2) * 0.5f &&
                                enemyBullets[i].GetX() + enemyBullets[i].GetR() >= player.GetAabb(0) - player.GetAabb(2) * 0.5f &&
                                enemyBullets[i].GetY() <= player.GetAabb(1) + player.GetAabb(2) * 0.5f &&
                                enemyBullets[i].GetY() + enemyBullets[i].GetR() >= player.GetAabb(1) - player.GetAabb(2) * 0.5f &&
                                !enemyBullets[i].HasGrazedPlayer() &&
                                !player.IsInvincible())  //Check if the player's graze bounding box has collided with an enemy bullet's bounding box.
                    {
                        enemyBullets[i].GrazedPlayer();  //Update all enemy bullets that have collided with the player's graze bounding box.
                        player.Graze();  //Tell the player that they have been grazed.
                    }
                    if (Math.Pow(player.GetX() - enemyBullets[i].GetX(), 2) + Math.Pow(player.GetY() - enemyBullets[i].GetY(), 2) <= Math.Pow(player.GetR() + enemyBullets[i].GetR(), 2))  //Check if an enemy's bullet has collided with the player.
                    {
                        enemyBullets[i].Dispose();  //Mark the enemy bullet that collided with the player for removal.
                        player.Hit();  //Tell the player that they have been hit by an enemy bullet.
                    }
                    if (enemyBullets[i].GetState()) enemyBullets.Remove(enemyBullets[i]);  //Remove any enenmy bullet that is done it's task.
                    else enemyBullets[i].Update();  //Update all enemy bullets that are not done their tasks.
                    tmp = enemyBullets.Count;  //Get the latest ammount of enemy bullets.
                }
            }
            if (enemies.Count - 1 > -1)  //Check if there are any enemies.
            {
                int tmp = enemies.Count;  //Get the latest ammount of enemies.
                for (int i = 0; i < tmp; i++) // runs the for loop for checking the game objects
                {
                    if (playerBullets.Count > 0) //Check if there are any player bullets.
                        for (int ii = 0; ii < playerBullets.Count; ii++)
                        {
                            if (enemies[i].GetX() <= playerBullets[ii].GetAabb(0) + playerBullets[ii].GetAabb(2) * 0.5f &&
                                enemies[i].GetX() + enemies[i].GetR() >= playerBullets[ii].GetAabb(0) - playerBullets[ii].GetAabb(2) * 0.5f &&
                                enemies[i].GetY() <= playerBullets[ii].GetAabb(1) + playerBullets[ii].GetAabb(3) * 0.5f &&
                                enemies[i].GetY() + enemies[i].GetR() >= playerBullets[ii].GetAabb(1) - playerBullets[ii].GetAabb(3) * 0.5f)  //Check if an enemy has collided with the player's bullets.                            {
                            {
                                enemies[i].Hit();  //Tell the enemy that they have been hit.
                                score += 100;  //Award the player 100 score for hitting an enemy.
                                playerBullets.Remove(playerBullets[ii]);  //Remove the player's bullet that collided with the enemy.
                            }
                        }
                    if (enemies[i].GetState()) enemies.Remove(enemies[i]); // checks if the object is alive and removes it if it is not.
                    else enemies[i].Update(); //Update all enemies that have not done their tasks.
                    tmp = enemies.Count;  //Get the latest ammount of enemies.
                }
            }
            if (playerBullets.Count - 1 > -1) //Check if there are any player bullets.
            {
                int tmp = playerBullets.Count;  //Get the latest ammount of player bullets.
                for (int i = 0; i < tmp; i++)
                {
                    if (playerBullets[i].GetState()) playerBullets.Remove(playerBullets[i]);  //Remove all player bullets that have done their tasks.
                    else playerBullets[i].Update();  //Update all player bullets that have not done their tasks.
                    tmp = playerBullets.Count;  //Get the latest ammount of player bullets.
                }
            }
            if (activeSFXs.Count - 1 > -1)  //Check if there are any activeSFX objects.
                for (int i = 0; i < activeSFXs.Count; i++)
                    if (activeSFXs[i].Update()) activeSFXs.Remove(activeSFXs[i]);  //Update and or remove all activeSFX objects that have done their tasks.
            if (currentReviveDelay > 0 && player.GetState()) currentReviveDelay--;  //Deincrement the revive delay when the player is dead.
            if (currentEnemySFXDelay > 0) currentEnemySFXDelay--;  //Deincrement the enemy sound delay if possible.
        }
        void GameLoop(object sender, EventArgs e) //Updatesand renders everything to the form. Called by the game's timer.
        {
            DoUpdate(); //Update the game.
            Refresh(); //Redraw the form.
        }
        void GameStateManager() //Manages the user interface.
        {
            if (optionSelected > optionsAvailable) optionSelected = 0;  //Ensure that the user has selected a valid option.
            else if (optionSelected < 0) optionSelected = optionsAvailable;  //Ensure that the user has selected a valid option.
            switch (gameState)
            {
                case GameState.MainMenu:
                    optionsAvailable = 2;  //Set the ammount of options available.
                    selector.Location = new Point(239, 260 + 112 * optionSelected);  //Set the selector location.
                    break;
            }
            Refresh();  //Redraw the form.
        }
        void PickOption(int option) //Called when the user selects a ui option.
        {
            switch (gameState)
            {
                case GameState.MainMenu:
                    switch (option)
                    {
                        case 0:
                            gameState = GameState.InGame;  //Set the game state to be in game.
                            ResetGame(execRoot.Clone().ToString() + @"Resources\Scripts\start.txt");  //Run the start script.
                            GameLoopStart();  //Start the game timer.
                            break;
                        case 1:
                            ShowHelp();  //Show the help menu.
                            break;
                        case 2:
                            Application.Exit();  //Exit the game.
                            break;
                    }
                    break;
            }
            Refresh();  //Redraw the form.
        }
        static void ResetGame(string path) //Resets the game and loads a new script.
        {
            score = 0;  //Reset the score to 0.
            playerLives = PLAYER_STARTING_LIVES;  //Reset the player's number of lives to the default value.
            enemies.Clear(); //Clear all enemies.
            enemyBullets.Clear(); //Clear all enemy bullets.
            playerBullets.Clear(); //Clear all player bullets.
            objectCreators.Clear(); //Clear all object creators.
            new Script(path); //Run a new script.         
            player = new Player(playerStartX, playerStartY); //Reset the player.
        }
        static void StopPlayerActions()  //Stops the player's actions.
        {
            player.SlowMovement(false);  //Stop slowed movement.
            player.IsGoingDown(false);  //Stop the player from going down
            player.IsGoingLeft(false);  //Stop the player from going to the left.
            player.IsGoingRight(false);  //Stop the player from going to the right.
            player.IsGoingUp(false);  //Stop the player from going up.
            player.IsShooting(false);  //Stop the player from shooting.
        }
        void GameOver()  //Called when the player looses the game.
        {
            gameState = GameState.MainMenu;  //Set the game state to the main menu.
            StopPlayerActions();  //Stop all player actions.
            GameLoopStop();  //Stop the game timer.
            MessageBox.Show("Your score: " + score, "GAME OVER");  //Show the game over menu.
            GameStateManager();  //Reset the game state.
        }
        static void GameWin()
        {
            gameState = GameState.MainMenu;
            StopPlayerActions();
            
        }
        void ShowHelp() { MessageBox.Show(docs["helpText"].ToString(), "HELP"); }  //Show the help menu.
        void ShowPause()  //Show the pause menu.
        {
            StopPlayerActions();  //Stop the player's action.
            GameLoopStop();  //Stop the game loop.
            MessageBox.Show(docs["pauseText"].ToString(), "PAUSED");  //Show the paused menu.
            GameLoopStart();  //Start the game timer.
        }
        void ExitApplication()  //Exits the game.
        {
            GameLoopStop(); // runs the GameLoopStop code
            tmr1.Dispose(); // gets rid of the timer
            Text = "APPLICATION CLOSING";  //Indicate that the form is closing.
            Application.Exit();  // exits the code
        }
        static void EnemySFX()  //Handles enemy sound effects.
        {
            if (currentEnemySFXDelay == 0 && !player.IsInvincible()) //Only play a sound effect when the player has not been hit and the SFX delay is 0.
            {
                currentEnemySFXDelay = ENEMY_SFX_DELAY; //Reset the SFX delay.
                activeSFXs.Add(new SFX(Sounds.EnemyDead)); //Play the enemy dead sound effect.
            }
        }
        [DllImport(@"winmm.dll")] //Import winmm.dll for handling sound effects.
        private static extern long mciSendString(string command, StringBuilder buffer, int bufferSize, IntPtr hwndCallback);  //Send string commands to winmm.
        protected override void OnPaint(PaintEventArgs e) //Draws everything when the form requests to be redrawn.
        {
            base.OnPaint(e);  //
            switch (gameState)
            {
                case GameState.MainMenu:  //Check if the user is in the main menu.
                    e.Graphics.DrawImage(graphicAssets["MainMenu"], 0, 0);  //Draw the main menu.
                    e.Graphics.DrawRectangle(Pens.Blue, selector);  //Draw the selector.
                    break;
                case GameState.InGame: //Check if the user is in game.
                    e.Graphics.FillRectangle(Brushes.Black, 0, 0, FORM_WIDTH, FORM_HEIGHT);  //Draw the background.
                    player.Draw(e.Graphics); //Draw the player.
                    for (int i = 0; i < enemies.Count; i++) enemies[i].Draw(e.Graphics);  //Draw all enemies.
                    for (int i = 0; i < playerBullets.Count; i++) playerBullets[i].Draw(e.Graphics);  //Draw all player bullets.
                    for (int i = 0; i < enemyBullets.Count; i++) enemyBullets[i].Draw(e.Graphics);  //Draw all enemy bullets.
                    e.Graphics.DrawString("LIVES:", STANDARD_FONT, Brushes.LimeGreen, 10, 10);  //Draw a string indicating the player's number of lives.
                    for (int i = 0; i < playerLives - 1; i++) e.Graphics.DrawImage(graphicAssets["GreenStar"], 60 + 20 * i, 10);  //Draw green stars to indicate the player's number of lives.
                    e.Graphics.DrawString("BOMBS:", STANDARD_FONT, Brushes.LimeGreen, 105, 10);  //Draw a string indicating the player's number of bombs.
                    for (int i = 0; i < player.GetBombs(); i++) e.Graphics.DrawImage(graphicAssets["RedStar"], 175 + 20 * i, 10);  //Draw red stars to indicate the player's number of bombs.
                    e.Graphics.DrawString("GRAZE: " + player.GetGrazes().ToString(), STANDARD_FONT, Brushes.LimeGreen, 235, 10);  //Draw a string indicating the player's number of grazes.
                    e.Graphics.DrawString("SCORE: " + score, STANDARD_FONT, Brushes.LimeGreen, 345, 10);  //Draw a string indicating the player's score.
                    e.Graphics.DrawString("STAGE: " + stage, STANDARD_FONT, Brushes.LimeGreen, 500, 10);  //Draw a string indicating the stage number.
                    break;
            }
        }
        void FKeyUp(object sender, KeyEventArgs e) //Called when a key is released.
        {
            if (gameState == GameState.InGame)  //Check if the user is in game.
            {
                if (e.KeyCode == Keys.ShiftKey) player.SlowMovement(false);  //Stop slowing movement when the shif key is released.
                if (e.KeyCode == Keys.W) player.IsGoingUp(false);  //Stop the player from moving up when the W key is released.
                if (e.KeyCode == Keys.S) player.IsGoingDown(false);  //Stop the player from moving downwards when the S key is released.
                if (e.KeyCode == Keys.A) player.IsGoingLeft(false);  //Stop the player from moving to the left when the A key is released.
                if (e.KeyCode == Keys.D) player.IsGoingRight(false);  //Stop the player from moving to the right when the D key is released.
                if (e.KeyCode == Keys.Up) player.IsGoingUp(false);  //Stop the player form moving to upwards when the UPARROW key is released.
                if (e.KeyCode == Keys.Down) player.IsGoingDown(false);  //Stop the player form moving downwards when the DOWNARROW key is released.
                if (e.KeyCode == Keys.Left) player.IsGoingLeft(false);  //Stop the player from moving to the left when the LEFTARROW key is released.
                if (e.KeyCode == Keys.Right) player.IsGoingRight(false);  //Stop the player from moving to the right when the RIGHTARROW key is released.
                if (e.KeyCode == Keys.Space) player.IsShooting(false);  //Stop the player from shooting when the SPACEBAR key is released.
                if (e.KeyCode == Keys.Z) player.IsShooting(false);  //Stop the player from shooting when the Z key is released.
                if (e.KeyCode == Keys.X) player.ActivateBomb(false);  //Stop the player from bombing when the x key is released.
                if (e.KeyCode == Keys.M) player.ActivateBomb(false);  //Stop the player from bombing when the x key is released.
            }
        }
        void FKeyDown(object sender, KeyEventArgs e) //Called when the key is heled down.
        {
            if (gameState == GameState.InGame)  //Check if the user is in game.
            {
                if (e.KeyCode == Keys.ShiftKey) player.SlowMovement(true);  //Slow player movement when the shift key is heled down.
                if (e.KeyCode == Keys.W || e.KeyCode == Keys.Up) player.IsGoingUp(true);  //Move the player up when the W or UPARROW key is heled down.
                if (e.KeyCode == Keys.S || e.KeyCode == Keys.Down) player.IsGoingDown(true);  //Move the player downwards when the S or DOWNARROW key is heled down.
                if (e.KeyCode == Keys.A || e.KeyCode == Keys.Left) player.IsGoingLeft(true);  //Move the player to the left when the A or LEFTARROW key is heled down.
                if (e.KeyCode == Keys.D || e.KeyCode == Keys.Right) player.IsGoingRight(true);  //Move the player to the right when the D or RIGHTARROW key is heled down.
                if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Z) player.IsShooting(true);  //Shoot when Z or SPACEBAR is heled down.
                if (e.KeyCode == Keys.X || e.KeyCode == Keys.M) player.ActivateBomb(true);  //Activate bomb when the X or M key is heled down.
                if (e.KeyCode == Keys.Escape) ShowPause();  //Pause the game when the ESC key is pressed.
                if (e.KeyCode == Keys.P)  //Show the help menu when the P key is pressed.
                {
                    StopPlayerActions();  //Stop player actions.
                    GameLoopStop();  //Pause the game's timer.
                    ShowHelp();  //Show the help window.
                    GameLoopStart();  //Start the game's timer.
                }
            }
            else
            {
                if (e.KeyCode == Keys.W || e.KeyCode == Keys.Up) optionSelected--;  //Deincrement the current selection when the W or UPARROW key is pressed.
                if (e.KeyCode == Keys.S || e.KeyCode == Keys.Down) optionSelected++;  //Increment the current selection when the S or DOWNARROW key is pressed.
                if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter) PickOption(optionSelected);  //Select the current option when the SPACEBAR or ENTER key is pressed.
                if (e.KeyCode == Keys.Escape)  //Check if the ESC key was pressed
                    if (gameState == GameState.MainMenu) ExitApplication();  //Exit the game when the ESC key is pressed.
                    else
                        ShowHelp();  //Show the help menu when the ESC key is pressed.
                if (e.KeyCode == Keys.P) ShowHelp();  //Show the help menu when the P key is pressed.
                GameStateManager();  //Refresh the game state.
            }
        }
        void FShown(object sender, EventArgs e) //Called when the form is shown.
        {
            GameLoopStop();
            Init();  //Initialize everything.
            gameState = GameState.MainMenu;  //Start the user at the main menu.
            GameStateManager(); //Refresh the game state.
        }
        void FClosing(object sender, FormClosingEventArgs e) //Called when the form is closing. Makes sure that the application closes properly.
        {
            ExitApplication();  //Exit the game.
        }
        class Script  //Scripting Engine.
        {
            public Script(string path)
            {
                string
                    line,  //Holds the current line in the string.
                    instruction;  //Holds the current instruction on each line.
                List<dynamic> parameters = new List<dynamic>();  //
                int
                    failsafe = 100,  //
                    lineNumber = 0;  //Holds the current line number.
                float lastNumber = 0;  //Holds the last numerical parameter for adding relative delay to execution time.
                try
                {
                    using (System.IO.StreamReader reader = new System.IO.StreamReader(path))  //Temporary use StreamReader to read a script.
                    {
                        while ((line = reader.ReadLine()) != null)  //Read all lines in the script.
                        {
                            lineNumber++;  //  //Increment the line number.
                            if (line == "end") break;  //Stop reading the script when the end instruction is called.
                            if (!string.IsNullOrWhiteSpace(line))  //Test and skip if the line is empty or whitespace.
                                if (line[0] != '#')  //Test and skip the line if it is a comment.
                                {
                                    int i = 0;  //
                                    instruction = string.Empty; //Empty the instruction string.
                                    parameters.Clear();  //Clear all parameters.
                                    while (line[i] != '(' && i < failsafe)
                                    {
                                        instruction += line[i];  //Build the instruction string.
                                        i++;  //
                                    }
                                    while (line[i] != ')' && i < failsafe)
                                    {
                                        string tmp = string.Empty;  //
                                        while (line[i + 1] != ',' && line[i + 1] != ')' && i < failsafe)
                                        {
                                            tmp += line[i + 1];  //
                                            i++;  //
                                        }
                                        i++;  //
                                        switch (tmp)  //Test if the parameter is a special parameter.
                                        {
                                            case "light":
                                                parameters.Add(EnemyType.Light);  //Set the enemy to have light physical properties.
                                                break;
                                            case "medium":
                                                parameters.Add(EnemyType.Medium);  //Set the enemy to have medium physical properties.
                                                break;
                                            case "heavy":
                                                parameters.Add(EnemyType.Heavy);  //Set the enemy to have heavy physical properties
                                                break;
                                            case "flee":
                                                parameters.Add(EnemyBehaviour.Flee);  //Set the enemy to flee when they run out of attacks.
                                                break;
                                            case "static":
                                                parameters.Add(EnemyBehaviour.Static);  //Set the enemy to move to the bottom of the form when they run out of attacks.
                                                break;
                                            case "unarmed":
                                                parameters.Add(EnemyAttacks.Unarmed);  //Set the enemy to not attack.
                                                break;
                                            case "single":
                                                parameters.Add(EnemyAttacks.Single);  //Set the enemy to shoot a single bullet at the player.
                                                break;
                                            case "burst":
                                                parameters.Add(EnemyAttacks.Burst);  //Set the enemy to shoot three bullets two of which are off angle towards the player.
                                                break;
                                            case "spread":
                                                parameters.Add(EnemyAttacks.Spread);  //Set the enemy to shoot a barrage of bullets, one of which is directed at the player.
                                                break;
                                            case "slowSpread":
                                                parameters.Add(EnemyAttacks.SlowSpread);  //Set the enemy to shoot a barrage of bullets over time, some of which are directed towards the player.
                                                break;
                                            case "slowTriSpread":
                                                parameters.Add(EnemyAttacks.SlowTriSpread);  //Set the enemy to shoot three barrages of bullets over time, some of which are directed towards the player.
                                                break;
                                            case "star":
                                                parameters.Add(EnemyAttacks.Star);  //Set the enemy to shoot a star pattern.
                                                break;
                                            case "Root":
                                                parameters.Add(execRoot);  //Adds the project root to the parameters.
                                                break;
                                            default:
                                                if (tmp[0] == '@')  //Test if the inputed parameter is a verbatim string.
                                                {
                                                    string tmps = tmp;  //
                                                    tmps = tmps.Remove(0, 1);  //Remove the @ at the beginning of the verbatim string.
                                                    parameters.Add(tmps);  //Add the verbatim string to the parameters.
                                                }
                                                else if (float.TryParse(tmp, out float tmpf))  //
                                                    parameters.Add(tmpf);  //
                                                else if (float.TryParse(tmp.Clone().ToString().Remove(tmp.Length - 1), out tmpf) && !float.TryParse(tmp[tmp.Length - 1].ToString(), out float notUsed))
                                                {
                                                    switch (tmp[tmp.Length - 1])
                                                    {
                                                        case 'r':
                                                            parameters.Add(lastNumber += tmpf);  //Test if the inputed number is relative to the last number.
                                                            break;
                                                    }
                                                }
                                                break;
                                        }
                                    }
                                    switch (instruction)
                                    {
                                        case "player":
                                            playerStartX = parameters[0];  //Set the player's starting x position.
                                            playerStartY = parameters[1];  //Set the player's starting y position.
                                            break;
                                        case "enemy":
                                            objectCreators.Add(new ScheduledCreator(GameObjId.Enemy, parameters, (int)parameters[parameters.Count - 1]));  //Create a new object creator that will spawn a new enemy after a specified delay.
                                            break;
                                        case "nextLevelFile":
                                            nextLevelPath = Path.Combine(parameters[0], parameters[1]);  //Get the path to the next level script.
                                            break;
                                        case "command":
                                            objectCreators.Add(new ScheduledCreator(parameters[0], (int)parameters[1]));  //Pass a command to the game that will be executed after a specified delay.
                                            break;
                                        case "stage":
                                            stage = (int)parameters[0];  //Get and set the stage number.
                                            break;
                                        default:
                                            throw new ScriptError();  //Throw an error when the instruction is invalid.
                                    }
                                }
                        }
                    }
                }
                catch
                {
                    MessageBox.Show("[ERR] script syntax error at line: " + lineNumber + "\r\n" + nextLevelPath);  //Show line number where the script stoped reading.
                    Application.Exit();  //Exit the application.
                }
            }
        }
        class ScheduledCreator  //Preforms a task after a specified delay.
        {
            int delay;  //The time to wait before executing the task.
            GameObjId gameObjId;  //Game object id to spawn.
            bool
                isDone = false,  //True when the ScheduledCreator is done it's task.
                isSpawner;  //True when the ScheduledCreator is a game object spawner.
            List<dynamic> parameters;  //A list of paramters to be passed onto the task.
            string operation;  //The operation that will be executed.
            public ScheduledCreator(GameObjId gameObjId, List<dynamic> parameters, int delay)  //Spawns a game object.
            {
                this.gameObjId = gameObjId;  //Sets the game object id to spawn.
                this.delay = delay;  //Sets the delay.
                this.parameters = new List<dynamic>(parameters);  //Sets the parameters.
                isSpawner = true;  //Make the ScheduledCreator a game object spawner.
            }
            public ScheduledCreator(string operation, int delay)  //Executes a command.
            {
                this.delay = delay;  //Sets the delay.
                this.operation = operation;  //Sets the operation to execute.
            }
            public void Update()  //Updates an instance of this class. Called by the game timer.
            {
                if (!isDone)  //Test if the ScheduledCreator is done it's task.
                    if (delay == 0)  //Test if the delay is 0.
                    {
                        isDone = true;  //Mark the ScheduledCreator for removal.
                        if (isSpawner)  //Test if the ScheduledCreator is a spawner.
                            switch (gameObjId)  //Get the game object to spawn
                            {
                                case GameObjId.Enemy:
                                    enemies.Add(new Enemy(parameters[0], parameters[1], parameters[2], parameters[3], parameters[4])); //Spawn a new enemy with the given parameters.
                                    break;
                            }
                        else
                        {
                            switch (operation)  //Get the operation.
                            {
                                case "nextLevel":
                                    ResetGame(nextLevelPath); //Reset the game when the nextLevel command is called.
                                    break;
                                case "winGame":
                                    GameWin();
                                    break;
                            }
                        }
                    }
                delay--; //Deincrement the delay.
            }
            public bool GetState() { return isDone; }  //Returns the ScheduledCreator's current state.
            public void Dispose() { isDone = true; }  //Mark the ScheduledCreator for removal.
        }
        class SFX  //Sound Effect Object.
        {
            int i = 20; //
            float id; //
            string path; //
            public SFX(Sounds sounds)
            {
                path = execRoot; //
                id = Environment.TickCount; //
                Task task = Task.Run(() =>
                {
                    PlaySound(sounds); //Load and play a sound effect.
                }); //
            }
            public bool Update()  //Updates an instance of this class. Called by the game timer.
            {
                if (i == 0)
                {
                    mciSendString(@"close " + id.ToString(), null, 0, IntPtr.Zero); //
                    return true; //Return true when the sound object is done it's task.
                }
                else
                {
                    i--; //Deincrement this objects's life time.
                    return false; //Return false when the object is not done it's task.
                }
            }
            public void PlaySound(Sounds sounds)  //Plays a sound
            {
                switch (sounds)  //Get sound name
                {
                    case Sounds.Nearmiss:
                        path += "Resources\\Audio\\nearMiss.wav"; //Set the path to the sound file.
                        break;
                    case Sounds.Pichun:
                        path += "Resources\\Audio\\pichun.wav"; //Set the path to the sound file.
                        break;
                    case Sounds.EnemyDead:
                        path += "Resources\\Audio\\enemyDead.wav"; //Set the path to the sound file.
                        break;
                }
                try
                {
                    if (!File.Exists(path))
                    {
                        throw new ScriptError(); //Test if the 
                    }
                    else
                    {
                        mciSendString("open " + path + " type waveaudio alias " + id.ToString(), null, 0, IntPtr.Zero); //
                        mciSendString("play " + id.ToString(), null, 0, IntPtr.Zero); //
                    }
                }
                catch
                {
                    MessageBox.Show("Sound file error.", "ERROR");
                    Application.Exit();
                }
            }
        }
        class Player : GameObject  //The player class.
        {
            const int
                IMMUNITY_TIME = 120,  //The player's immunity time.
                STARTING_BOMBS = 3,  //The player's starting ammount of bombs.
                GRAZE_HITBOX_SIZE = 80,  //The size of the player's graze hitbox.
                GRAZE_SFX_DELAY = 2,  //The graze sound effect delay.
                SHOOTINGDELAY = 2,  //The shooting delay.
                BASE_RANDOMNESS = 16,  //Randomness factor.
                PLAYER_SPEED = 8,    //The player's speed.
                BOMBDELAY = 16,  //The time before another bomb can be acivated.
                HITBOX_SIZE = 4,  //The player's hitbox size.
                SHOT_SPEED = 64;  
            const float
                SLOW_MOVEMENT_FACTOR = 0.33f,  //Slowed movement modifier.
                PRECISE_RANDOMNESS_FACTOR = 0.33f; //Lowers the randomness factor.
            bool
                isShooting,  //Determins if the player is shooting.
                goRight,  //Determins if the player is going to the right.
                goLeft,  //Determins if the player is going to the left.
                goUp,  //Determins if the player is going up.
                goDown,  //Determins if the player is going down.
                slowMovement,  //Determins if the player's movement is slowed.
                activateBomb,  //Determins if the player has activated a bomb.
                isHit; //Determins if the player has been hit.
            int
                currentGrazeSoundDelay = 0,  //The time left before another grze sound effect can be played.
                currentShootingDelay = 0,  //The time left before another shot can be fired.
                currentBombDelay = 0,  //The time left before another bomb can be activated.
                currentImmunityTime = 0,  //The time left before the player looses their immunity.
                posVelX,  //The current right velocity.
                posVelY,  //The current downwards velocity.
                negVelX,  //The current left velocity.
                negVelY,  //The current upwards velocity.
                bombs,  //The ammount of bombs the player has.
                graze = 0; //The ammount of times the player has been grazed by a bullet.
            float
                movementModifier = 1, //The current movement modifier
                randomnessModifier = 1; //The current randomness modifier
            float[] aabb; //The player's graze bounding box.
            public Player(float x, float y) : base(x, y, GameObjId.Player)  //Create a new player.
            {
                r = HITBOX_SIZE; //Sets the player's hitbox size.
                aabb = new float[] { x, y, GRAZE_HITBOX_SIZE }; //Sets the player's graze hitbox size.
                bombs = STARTING_BOMBS;  //Sets the player's number of bombs.
            }
            public override void Update()  //Updates an instance of this class. Called by the game timer.
            {
                if (!isDone)  //Check if the player has been marked for removal.
                {
                    velX = 0; //Reset velocity X.
                    velY = 0; //Reset velocity Y.
                    movementModifier = 1; //Reset the movement modifier.
                    randomnessModifier = 1; //Reset the randomness modifier.
                    if (slowMovement) //Check if the player's movement is slowed.
                    {
                        randomnessModifier = PRECISE_RANDOMNESS_FACTOR; //Set the randomness factor to the precise randomness factor.
                        movementModifier = SLOW_MOVEMENT_FACTOR; //Set the randomness factor to the slow movement factor.
                    }
                    if (goRight || goLeft || goUp || goDown)  //Check if the player is moving.s
                    {
                        posVelX = 0; //Reset the right velocity.
                        posVelY = 0; //Reset the downwards velocity.
                        negVelX = 0; //Reset the left velocity.
                        negVelY = 0; //Reset the upwards velocity.
                        if (goUp && y > 0) negVelY = -PLAYER_SPEED; //Set the upwards velocity.
                        if (goDown && y < FORM_HEIGHT) posVelY = PLAYER_SPEED; //Set the downwards velocity.
                        if (goRight && x < FORM_WIDTH) posVelX = PLAYER_SPEED; //Set the right velocity.
                        if (goLeft && x > 0) negVelX = -PLAYER_SPEED; //Set the left velocity.
                        velX = (posVelX + negVelX) * movementModifier; //Set the velocity x.
                        velY = (posVelY + negVelY) * movementModifier; //Set the velocity y.
                        UpdatePosition(); //Update the player's location.
                        aabb[0] = x; //Update the graze bounding box's x position.
                        aabb[1] = y; //Update the graze bounding box's y position
                    }
                    if (activateBomb && currentBombDelay == 0 && bombs > 0)
                    {
                        bombs--; //Deincrement the number of bombs.
                        enemyBullets.Clear(); //Clear all enemy bullets.
                        currentBombDelay = BOMBDELAY; //Set the 
                        currentImmunityTime = IMMUNITY_TIME; //Set the current immunity time.
                    }
                    if (isShooting && currentShootingDelay == 0)
                    {
                        currentShootingDelay = SHOOTINGDELAY; //
                        playerBullets.Add(new Bullet(x, y, 32, -1.57079637f + rng.Next(-BASE_RANDOMNESS, BASE_RANDOMNESS) * 0.01f * randomnessModifier, SHOT_SPEED + rng.Next(-BASE_RANDOMNESS, BASE_RANDOMNESS) * randomnessModifier, 0, graphicAssets["PlayerBullet"], teams, BulletBehaviour.Linear)); //
                    }
                    if (currentShootingDelay > 0) currentShootingDelay--; //
                    if (currentBombDelay > 0) currentBombDelay--; //
                    if (currentImmunityTime > 0) currentImmunityTime--; //
                    if (currentGrazeSoundDelay > 0) currentGrazeSoundDelay--; //
                }
            }
            public override void Draw(Graphics g)  //Draws the player to the screen.
            {
                if (!isDone)
                {
                    g.DrawImage(graphicAssets["Player"],x - graphicAssets["Player"].Width * 0.5f , y - graphicAssets["Player"].Height * 0.5f);
                    g.FillEllipse(Brushes.Green, x - r, y - r, 2 * r, 2 * r); //
                }
                //g.DrawRectangle(Pens.Purple, aabb[0] - aabb[2] * 0.5f, aabb[1] - aabb[2] * 0.5f, aabb[2], aabb[2]);
            }
            public void Hit()  //Called when the player collides with a bullet.
            {
                if (currentImmunityTime == 0 && !isHit)  //Test if the player is 
                {
                    currentReviveDelay = REVIVE_DELAY;
                    playerLives--; //Deincrement the player's lives.
                    isDone = true;  //Mark the player for removal
                    isHit = true;
                    activeSFXs.Clear();  //Clear all sound effects
                    activeSFXs.Add(new SFX(Sounds.Pichun));  //Play the hit sound effect
                }
            }
            public void Graze() //Called when the player is grazed by a bullet.
            {
                if (currentGrazeSoundDelay == 0)
                {
                    currentGrazeSoundDelay = GRAZE_SFX_DELAY;  //
                    activeSFXs.Add(new SFX(Sounds.Nearmiss));  //Play the graze sound.
                }
                graze++;  //Increment the graze ammount.
            }
            public void MakeInvincible() { currentImmunityTime = IMMUNITY_TIME; }
            public void ActivateBomb(bool activateBomb) { this.activateBomb = activateBomb; }
            public void IsShooting(bool isShooting) { this.isShooting = isShooting; }
            public void IsGoingRight(bool goRight) { this.goRight = goRight; }
            public void IsGoingLeft(bool goLeft) { this.goLeft = goLeft; }
            public void IsGoingUp(bool goUp) { this.goUp = goUp; }
            public void IsGoingDown(bool goDown) { this.goDown = goDown; }
            public void SlowMovement(bool slowMovement) { this.slowMovement = slowMovement; }
            public int GetBombs() { return bombs; }
            public float[] GetAabb() { return aabb; }
            public float GetAabb(int index) { return aabb[index]; }
            public int GetGrazes() { return graze; }
            public bool IsInvincible() { if (currentImmunityTime > 0) return true; else return false; }
        }
        class Bullet : GameObject
        {
            bool hasGrazedPlayer; //
            float
                angle,  //Direction to where the bullet will travel.
                speed,  //The speed of the bullet.
                drag,  //Slows the bullet down over time.
                dragScale = 0;  //Slows the bullet down over time
            float[] aabb; //
            BulletBehaviour bulletBehaviour;  //The bullet's behaviour.
            Bitmap bulletImage;  //The image to be displayed on the bullet.
            public Bullet(float x, float y, float r, float angle, float speed, float drag, Bitmap bulletImage, Teams teams, BulletBehaviour bulletBehaviour) : base(x, y, GameObjId.Bullet)
            {
                aabb = new float[4]; //
                aabb[0] = x - r * 0.5f; //
                aabb[1] = y - r * 0.5f; //
                aabb[2] = r; //
                aabb[3] = speed; //
                this.r = r; //
                this.angle = angle; //Sets the direction of the bullet.
                this.speed = speed; //Sets how fast the bullet travels.
                this.drag = drag; //Sets how much to slow the bullet down over time.
                this.teams = teams;  //Sets what team the bullet is on.
                this.bulletBehaviour = bulletBehaviour;  //Sets the bullet's behaviour.
                this.teams = teams; //
                this.bulletImage = bulletImage;  //Sets the image to be displayed on the bullet.
                velX = (float)Math.Cos(angle) * speed;  //Sets the x velocity based on the angle and speed.
                velY = (float)Math.Sin(angle) * speed;  //Sets the y velocity based on the angle and speed.
            }
            public override void Update()
            {
                if (x > FORM_WIDTH + bulletImage.Width ||
                    x < -bulletImage.Width ||
                    y < -bulletImage.Height ||
                    y > FORM_HEIGHT + bulletImage.Height) Dispose();  //Marks the bullet for removal when it is out of bounds.
                if (teams == Teams.Enemy)
                {

                }
                switch (bulletBehaviour)
                {
                    case BulletBehaviour.Linear:

                        break;
                    case BulletBehaviour.Ballistic:  //Slows the bullet down over time.
                        if (speed <= 0) Dispose();  //Mark the bullet for removal when it stops moving.
                        speed -= drag * dragScale;  //Lower the speed based on drag.
                        dragScale++;  //Increase the amount of drag.
                        velX = (float)Math.Cos(angle) * speed;  //Recalculate the x velocity.
                        velY = (float)Math.Sin(angle) * speed; //Recalculate the y velocity.
                        break;
                }
                UpdatePosition(); //Update the position of the bullet
                if (teams == Teams.Player)
                {
                    aabb[0] = x - r * 0.5f; //Update the x position of the bounding box when the bullet is a player bullet.
                    aabb[1] = y - r * 0.5f; //Update the y position of the bounding box when the bullet is a player bullet.
                }
            }
            public override void Draw(Graphics g)
            {
                g.DrawImage(bulletImage, x - bulletImage.Width * 0.5f, y - bulletImage.Height * 0.5f); //Draw the bullet to the screen.
            }
            public float[] GetAabb() { return aabb; }
            public float GetAabb(int index) { return aabb[index]; }
            public void GrazedPlayer() { hasGrazedPlayer = true; }
            public bool HasGrazedPlayer() { return hasGrazedPlayer; }
        }
        class Enemy : GameObject
        {
            bool
                stopAttacking; //
            int
                health,
                attacks,
                speed,
                shootingDelay,
                currentShootingDelay = 0,
                immunityTime,
                currentImmunityTime = 0,
                initialDelay,
                acceleration = 0,
                timeAlive = 0,
                onDestroyAward = 0; //
            float
                angleToPlayer = 0; //
            EnemyAttacks enemyAttacks; //
            EnemyBehaviour enemyBehaviour; //
            EnemyType enemyType;
            public Enemy(float x, float y, EnemyType enemyType, EnemyBehaviour enemyBehaviour, EnemyAttacks enemyAttacks) : base(x, y, GameObjId.Enemy)
            {
                this.enemyType = enemyType;
                this.enemyAttacks = enemyAttacks; //
                this.enemyBehaviour = enemyBehaviour; //
                switch (enemyType)
                {
                    case EnemyType.Light:
                        health = 1; //
                        attacks = 1; //
                        speed = 4; //
                        shootingDelay = 0; //
                        r = 8; //
                        immunityTime = 0; //
                        initialDelay = 32; //
                        onDestroyAward = 100;
                        break;
                    case EnemyType.Medium:
                        health = 2; //
                        attacks = 3; //
                        speed = 2; //
                        shootingDelay = 16; //
                        r = 12; //
                        immunityTime = 4; //
                        initialDelay = 32; //
                        onDestroyAward = 300;
                        break;
                    case EnemyType.Heavy:
                        health = 12; //
                        attacks = 6; //
                        speed = 1; //
                        shootingDelay = 12; //
                        r = 24; //
                        immunityTime = 4; //
                        initialDelay = 32; //
                        onDestroyAward = 1000;
                        break;
                }
            }
            public override void Update()
            {
                UpdatePosition(); //
                angleToPlayer = (float)Math.Atan2(player.GetY() - y, player.GetX() - x); //
                timeAlive++; //
                if (currentShootingDelay == 0 && initialDelay == 0 && !stopAttacking)
                {
                    currentShootingDelay = shootingDelay; //
                    attacks--; //
                    switch (enemyAttacks)
                    {
                        case EnemyAttacks.Unarmed:

                            break;
                        case EnemyAttacks.Single:
                            enemyBullets.Add(new Bullet(x, y, 8, angleToPlayer, 8, 0, graphicAssets["Tear"], teams, BulletBehaviour.Linear)); //
                            break;
                        case EnemyAttacks.Burst:
                            for (int i = 0; i < 3; i++) enemyBullets.Add(new Bullet(x, y, 4, angleToPlayer - 0.3f + 0.3f * i, 8, 0, graphicAssets["Fireball"], teams, BulletBehaviour.Linear)); //
                            break;
                        case EnemyAttacks.Spread:
                            for (int i = 0; i < 9; i++) enemyBullets.Add(new Bullet(x, y, 4, angleToPlayer + 0.66f * i, 16, 0, graphicAssets["Fireball"], teams, BulletBehaviour.Linear)); //
                            break;
                        case EnemyAttacks.SlowSpread:
                            shootingDelay = 1; //
                            enemyBullets.Add(new Bullet(x, y, 8, angleToPlayer + 0.1f * timeAlive, 6, 0, graphicAssets["Tear"], teams, BulletBehaviour.Linear));
                            if (timeAlive == 200) stopAttacking = true; //
                            attacks++; //
                            break;
                        case EnemyAttacks.SlowTriSpread:
                            shootingDelay = 1; //
                            enemyBullets.Add(new Bullet(x, y, 8, -2f + 0.1f * timeAlive, 8, 0, graphicAssets["Tear"], teams, BulletBehaviour.Linear)); //
                            enemyBullets.Add(new Bullet(x, y, 8, 0.1f * timeAlive, 8, 0, graphicAssets["Tear"], teams, BulletBehaviour.Linear)); //
                            enemyBullets.Add(new Bullet(x, y, 8, 2f + 0.1f * timeAlive, 8, 0, graphicAssets["Tear"], teams, BulletBehaviour.Linear)); //
                            if (timeAlive == 200) stopAttacking = true; //
                            attacks++; //
                            break; //
                        case EnemyAttacks.Star:
                            for (int i = 0; i < 5; i++) enemyBullets.Add(new Bullet(x, y, 8, angleToPlayer + 1f * i, 8, 0, graphicAssets["Fireball"], Teams.Enemy, BulletBehaviour.Linear)); //
                            break; //
                    }
                }
                if (currentShootingDelay > 0) currentShootingDelay--; //
                if (currentImmunityTime > 0) currentImmunityTime--; //
                if (attacks == 0) stopAttacking = true; //
                if (initialDelay > 0)
                {
                    initialDelay--; //
                    velY = speed; //
                }
                else
                {
                    if (stopAttacking)
                    {
                        switch (enemyBehaviour)
                        {
                            case EnemyBehaviour.Flee:
                                velX = 0.05f * speed * acceleration; //
                                if (x < FORM_WIDTH * 0.5f) velX = velX * -1; //
                                acceleration++; //
                                if (x > FORM_WIDTH + r ||
                                    x < -r ||
                                    y < -r ||
                                    y > FORM_HEIGHT + r) isDone = true; //
                                break;
                            case EnemyBehaviour.Static:
                                velY = speed; //
                                break;
                        }
                    }
                }
            }
            public override void Draw(Graphics g)
            {
                g.FillRectangle(Brushes.Yellow, x, y, 2, 2); //
                if (currentImmunityTime > 0) g.DrawEllipse(Pens.Purple, x - r, y - r, 2 * r, 2 * r); //
                else g.DrawEllipse(Pens.Red, x - r, y - r, 2 * r, 2 * r); //
            }
            public void Hit()
            {
                if (currentImmunityTime == 0)
                    if (health > 0)
                    {
                        health--; //
                        currentImmunityTime = immunityTime; //
                    }
                    else
                    {
                        score += onDestroyAward;
                        Dispose(); //
                        if(enemyType == EnemyType.Heavy) EnemySFX();
                    }
            }
        }
        abstract class GameObject //Superclass for all gameobject e.g., player, enemy, bullet.
        {
            protected float
                r,  //Radius of the game object.
                x,  //X position of the game object.
                y,  //Y position of the game object.
                velX, //X velocity of the game object.
                velY/*, *///Y velocity of the game object.
                          /*direction*/; //Direction of the game object.
            protected bool isDone;  //State of the game object.
            protected GameObjId gameObjId;  //The game object's ID.
            protected Teams teams; //
            public GameObject(float x, float y, GameObjId gameObjId) //Game object constructor.
            {
                this.x = x; //Set the x position of the game object.
                this.y = y; //Set the y position of the game object.
                this.gameObjId = gameObjId; //Set the game object id.
            }
            public abstract void Update(); //
            public abstract void Draw(Graphics g); //
            public float GetX() { return x; } //Gets the x position of a game object. - MAITLAND
            public float GetY() { return y; } //Get the y position of a game object. - MAITLAND
            public float GetVelX() { return velX; } //Gets the x velocity of a game object. - MAITLAND
            public float GetVelY() { return velY; } //Gets the y velocity of a game object. - MAITLAND
            public float GetR() { return r; } //Gets the radius of a game object. - MAITLAND
            //public float GetDirection() { return direction; } //Gets the direction in degrees. - MAITLAND
            //public GameObjId GetObjectId() { return gameObjId; } //Gets the ID of the game object. - MAITLAND
            public bool GetState() { return isDone; } //Gets the state of the game object. - MAITLAND
            public void Dispose() { isDone = true; } //Mark the game object for removal. - MAITLAND
            public void SetX(float x) { this.x = x; } //Sets the x pos of a game object. - MAITLAND
            public void SetY(float y) { this.y = y; } //Sets the y pos of a game object. - MAITLAND
            public void SetVelX(float velX) { this.velX = velX; } //Sets the x velocity of a game object. - MAITLAND
            public void SetVelY(float velY) { this.velY = velY; } //Sets the y velocity of a game object. - MAITLAND
            public void SetR(float r) { this.r = r; } //Sets the radius of a game object. - MAITLAND
            public void UpdatePosition() //Updates the location and direction of a game object.
            {
                if (velX != 0 || velY != 0)
                {
                    x += velX; //Update the x position of the game object.
                    y += velY;//Update the y position of the game object.
                    //direction = (float)Math.Atan2(velY, velX); //Update the direction of the game object
                }
            }
            public Teams GetTeam() { return teams; }
        }
        class ScriptError : Exception
        {
            public ScriptError() { }
        }
        enum GameObjId
        {
            Player,
            Bullet,
            Enemy,
            Block
        }
        enum BulletType
        {
            Tears,
            Fireball,
            Poison
        }
        enum BulletPatternType
        {
            Direct,
            TriShot,
            Burst,
            TargetedSpread
        }
        enum BulletBehaviour
        {
            Linear,
            Ballistic
        }
        enum EnemyType
        {
            Light,
            Medium,
            Heavy
        }
        enum EnemyAttacks
        {
            Unarmed,
            Single,
            Burst,
            Spread,
            SlowSpread,
            SlowTriSpread,
            Star
        }
        enum EnemyBehaviour
        {
            Static,
            Charge,
            Flee
        }
        enum Teams
        {
            Player,
            Enemy
        }
        enum Sounds
        {
            Pichun,
            Nearmiss,
            EnemyDead
        }
        enum GameState
        {
            MainMenu,
            InGame
        }
    }
}