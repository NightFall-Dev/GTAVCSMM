using GTAVCSMM.Config;
using GTAVCSMM.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GTAVCSMM
{
    static class Program
    {
        #region Program Init keys
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private static LowLevelKeyboardProc _proc = HookCallback;
        private static IntPtr _hookID = IntPtr.Zero;
        const int SW_HIDE = 0;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook,
        LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
            IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private static bool isHidden = false;
        private static IntPtr formHandle;

        private static int menuMainLvl = 0;
        private static int LastMenuMainLvl = 0;
        private static int menuLvl = 0;
        private static int LastMenuLvl = 0;
        private static int menuItm = 0;
        private static int LastMenuItm = 0;
        private static Form mainForm = new Form();
        private static ListBox listBx = new ListBox();
        private static Label label1 = new Label();
        private static Label label2 = new Label();
        private static string lastNavigation = string.Empty;

        private static List<long> pedList = new List<long>();
        private static List<long> vehList = new List<long>();

        public const string WINDOW_NAME = "Grand Theft Auto V";
        public static IntPtr handle = FindWindow(null, WINDOW_NAME);

        public static RECT rect;

        public struct RECT
        {
            public int left, top, right, bottom;
        }

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string IpClassName, string IpWindowName);

        [DllImport("user32.dll")]
        static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hwnd, out RECT IpRect);

        public static Offsets offsets = new Offsets();
        public static Patterns pattern = new Patterns();
        public static Mem Mem;

        private static bool bGodMode = false;
        private static bool bgodState = false;
        private static bool bVehicleGodMode = false;
        private static int frameFlagCount = 0;
        private static bool bCopKiller = false;

        [DllImport("user32.dll")]
        static extern short GetAsyncKeyState(System.Windows.Forms.Keys vKey);

        [DllImport("user32.dll")]
        public static extern void mouse_event(int a, int b, int c, int d, int damnIwonderifpeopleactuallyreadsthis);
        #endregion

        #region Timers

        private static void MemoryTimer_Tick(object sender, EventArgs e)
        {
            vCOPKILLER();
        }

        private static void fastTimer_Tick(object sender, EventArgs e)
        {
        }

        #endregion

        #region System/Main
        public static void getPointer()
        {
            try
            {
                Mem = new Mem(Settings.gameName);

                var processes = Process.GetProcessesByName(Settings.gameName);
                foreach (var p in processes)
                {
                    if (p.Id > 0)
                    {
                        Settings.gameProcess = p.Id;
                    }
                }

                if (Settings.gameProcess > 0)
                {
                    // GlobalPTR
                    long addr = Mem.FindPattern(pattern.GlobalPTR, pattern.GlobalPTR_Mask);
                    Settings.GlobalPTR = addr + Mem.ReadInt(addr + 3, null) + 7;

                    // WorldPTR
                    long addr2 = Mem.FindPattern(pattern.WorldPTR, pattern.WorldPTR_Mask);
                    Settings.WorldPTR = addr2 + Mem.ReadInt(addr2 + 3, null) + 7;

                    // BlipPTR
                    long addr3 = Mem.FindPattern(pattern.BlipPTR, pattern.BlipPTR_Mask);
                    Settings.BlipPTR = addr3 + Mem.ReadInt(addr3 + 3, null) + 7;

                    // ReplayInterfacePTR
                    long addr4 = Mem.FindPattern(pattern.ReplayInterfacePTR, pattern.ReplayInterfacePTR_Mask);
                    Settings.ReplayInterfacePTR = addr4 + Mem.ReadInt(addr4 + 3, null) + 7;

                    // LocalScriptsPTR
                    long addr5 = Mem.FindPattern(pattern.LocalScriptsPTR, pattern.LocalScriptsPTR_Mask);
                    Settings.LocalScriptsPTR = addr5 + Mem.ReadInt(addr5 + 3, null) + 7;

                    // PlayerCountPTR
                    long addr6 = Mem.FindPattern(pattern.PlayerCountPTR, pattern.PlayerCountPTR_Mask);
                    Settings.PlayerCountPTR = addr6 + Mem.ReadInt(addr6 + 3, null) + 7;

                    // PickupDataPTR
                    long addr7 = Mem.FindPattern(pattern.PickupDataPTR, pattern.PickupDataPTR_Mask);
                    Settings.PickupDataPTR = addr7 + Mem.ReadInt(addr7 + 3, null) + 7;

                    // WeatherADDR
                    long addr8 = Mem.FindPattern(pattern.WeatherADDR, pattern.WeatherADDR_Mask);
                    Settings.WeatherADDR = addr8 + Mem.ReadInt(addr8 + 6, null) + 10;

                    // SettingsPTR
                    long addr9 = Mem.FindPattern(pattern.SettingsPTR, pattern.SettingsPTR_Mask);
                    Settings.SettingsPTR = addr9 + Mem.ReadInt(addr9 + 3, null) - Convert.ToInt64("0x89", 16);

                    // AimCPedPTR
                    long addr10 = Mem.FindPattern(pattern.AimCPedPTR, pattern.AimCPedPTR_Mask);
                    Settings.AimCPedPTR = addr10 + Mem.ReadInt(addr10 + 3, null) + 7;

                    // FriendlistPTR
                    long addr11 = Mem.FindPattern(pattern.FriendlistPTR, pattern.FriendlistPTR_Mask);
                    Settings.FriendlistPTR = addr11 + Mem.ReadInt(addr11 + 3, null) + 7;
                }
                else
                {
                    MessageBox.Show("GTA is not Running!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Quit();
                }
            }
            catch
            {
                MessageBox.Show("GTA is not Running!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Quit();
            }
        }
        public static string ShowDialog(string text, string caption)
        {
            Form prompt = new Form()
            {
                Width = 500,
                Height = 150,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = caption,
                StartPosition = FormStartPosition.Manual,
                Location = new Point(100, 100)
            };
            Label textLabel = new Label() { Left = 50, Top = 20, Width = 400, Text = text };
            TextBox textBox = new TextBox() { Left = 50, Top = 50, Width = 400 };
            Button confirmation = new Button() { Text = "Ok", Left = 350, Width = 100, Top = 70, DialogResult = DialogResult.OK };
            confirmation.Click += (sender, e) => { prompt.Close(); };
            prompt.Controls.Add(textBox);
            prompt.Controls.Add(confirmation);
            prompt.Controls.Add(textLabel);
            prompt.AcceptButton = confirmation;
            prompt.MaximizeBox = false;
            prompt.MinimizeBox = false;
            prompt.TopMost = true;

            return prompt.ShowDialog() == DialogResult.OK ? textBox.Text : "";
        }
        private static void Quit()
        {
            Environment.Exit(0);
        }

        //audio response to actions
        public static void Activate()
        {
            Console.Beep(523, 75);
            Console.Beep(587, 75);
            Console.Beep(700, 75);
        }

        public static void Deactivate()
        {
            Console.Beep(700, 75);
            Console.Beep(587, 75);
            Console.Beep(523, 75);
        }

        public static void Toggle()
        {
            Console.Beep(523, 75);
            Console.Beep(523, 75);
            Console.Beep(523, 75);
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            bool createdNew = true;
            using (Mutex mutex = new Mutex(true, "GTA5TrainerCS", out createdNew))

                if (createdNew)
                {
                    createMainForm();
                    listBx.Items.Add("Getting game pointers.");
                    listBx.Enabled = false;

                    getPointer();

                    ListboxStyle();
                    ListboxFill(0, 0);
                    listBx.Enabled = true;

                    Task.Run(() =>
                    {
                        HiPrecisionTimer();
                    });

                    Task.Run(() =>
                    {
                        StdPrecisionTimer();
                    });

                    Task.Run(() =>
                    {
                        LoPrecisionTimer();
                    });

                    Application.Run();
                }
        }
        public static void HiPrecisionTimer()
        {
            while (true)
            {
                Thread.Sleep(500);
            }
        }
        public static void StdPrecisionTimer()
        {
            while (true)
            {
                pGODMODE();
                vGODMODE();
                Thread.Sleep(1000);
            }
        }
        public static void LoPrecisionTimer()
        {
            while (true)
            {
                vCOPKILLER();
                Thread.Sleep(2000);
            }
        }

        public static void createMainForm()
        {
            // 
            // listBx
            // 
            listBx.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(128)))));
            listBx.BorderStyle = System.Windows.Forms.BorderStyle.None;
            listBx.Font = new System.Drawing.Font("Calibri", 12.00f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            listBx.FormattingEnabled = true;
            listBx.ItemHeight = 24;
            listBx.Location = new System.Drawing.Point(6, 50);
            listBx.Margin = new System.Windows.Forms.Padding(10);
            listBx.MaximumSize = new System.Drawing.Size(290, 500);
            listBx.Name = "listBox1";
            listBx.Size = new System.Drawing.Size(290, 480);
            listBx.TabIndex = 0;

            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(128)))));
            label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 18F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            label1.Location = new System.Drawing.Point(1, 9);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(168, 33);
            label1.TabIndex = 1;
            label1.Text = "GTAVCSMM";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new System.Drawing.Font("Microsoft San Serif", 12.00f, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            label2.Location = new System.Drawing.Point(162, 16);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(65, 24);
            label2.TabIndex = 2;
            label2.Text = "o1.61";
            // 
            // Form1
            // 
            mainForm.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            mainForm.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            mainForm.AutoSize = true;
            mainForm.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            mainForm.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));// Menu Color
            mainForm.ClientSize = new System.Drawing.Size(207, 116);
            mainForm.Controls.Add(label2);
            mainForm.Controls.Add(label1);
            mainForm.Controls.Add(listBx);
            mainForm.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            mainForm.KeyPreview = true;
            mainForm.Name = "Form1";
            mainForm.Opacity = 0.6D;
            mainForm.ShowIcon = false;
            mainForm.ShowInTaskbar = false;
            mainForm.Text = "GTAVCSMM";
            mainForm.TopMost = true;

            formHandle = mainForm.Handle;
            _hookID = SetHook(_proc);

            mainForm.FormBorderStyle = FormBorderStyle.None;
            int InitialStyle = GetWindowLong(mainForm.Handle, -10);
            SetWindowLong(mainForm.Handle, -10, InitialStyle | 0x800000 | 0x20);
            GetWindowRect(handle, out rect);
            mainForm.Top = rect.top - 20;
            mainForm.Left = rect.left + 30;

            mainForm.Show();
        }
        #endregion

        #region List strings
        public static void ListboxStyle()
        {
        }

        public static void ListboxFill(int mainMenulevel, int menulevel)
        {
            listBx.Items.Clear();
            switch (mainMenulevel)
            {
                case 0:
                    /*
                     * Mainlevel 0
                     */
                    listBx.Items.Add("Main \t\t\t ►");       // 0,0
                    listBx.Items.Add("Session \t\t\t ►");    // 0,1
                    listBx.Items.Add("Player \t\t\t ►");     // 0,2
                    listBx.Items.Add("Vehicle \t\t\t ►");    // 0,3
                    listBx.Items.Add("Teleport \t\t\t ►");   // 0,4
                    listBx.Items.Add("Online Services \t\t ►");   // 0,5
                    listBx.Items.Add("World \t\t\t ►");   // 0,6

                    menuMainLvl = 0;
                    menuLvl = 0;

                    LastMenuMainLvl = 0;
                    LastMenuLvl = 0;
                    LastMenuItm = 0;
                    break;

                case 1:
                    switch (menulevel)
                    {
                        case 0:// Main
                            listBx.Items.Add("Refresh");
                            listBx.Items.Add("Exit");

                            menuMainLvl = 1;
                            menuLvl = 0;

                            LastMenuMainLvl = 0;
                            LastMenuLvl = 1;
                            LastMenuItm = 0;
                            break;

                        case 1:// Session
                            listBx.Items.Add("Join Public Session");
                            listBx.Items.Add("New Public Session");
                            listBx.Items.Add("Solo Session");
                            listBx.Items.Add("Leave Online");
                            listBx.Items.Add("Empty Session (10 Sec. Freeze)");
                            listBx.Items.Add("Invite Only Session");
                            listBx.Items.Add("Find Friend Session");
                            listBx.Items.Add("Closed Friend Session");
                            listBx.Items.Add("Crew Session");
                            listBx.Items.Add("Join Crew Session");
                            listBx.Items.Add("Closed Crew Session");
                            /*
                            listBx.Items.Add("Disconnect");
                            */

                            menuMainLvl = 1;
                            menuLvl = 1;

                            LastMenuMainLvl = 0;
                            LastMenuLvl = 1;
                            LastMenuItm = 1;
                            break;

                        case 2:// Player
                            listBx.Items.Add("God Mode (F6)");
                            listBx.Items.Add("Wanted Level \t\t ►");

                            menuMainLvl = 1;
                            menuLvl = 2;

                            LastMenuMainLvl = 0;
                            LastMenuLvl = 1;
                            LastMenuItm = 2;
                            break;

                        case 3:// Vehicle
                            listBx.Items.Add("God Mode");

                            menuMainLvl = 1;
                            menuLvl = 3;

                            LastMenuMainLvl = 0;
                            LastMenuLvl = 1;
                            LastMenuItm = 3;
                            break;

                        case 4:// Teleport
                            listBx.Items.Add("Waypoint (F8)");
                            listBx.Items.Add("Objective");
                            listBx.Items.Add("Locations \t\t ►");

                            menuMainLvl = 1;
                            menuLvl = 5;

                            LastMenuMainLvl = 0;
                            LastMenuLvl = 1;
                            LastMenuItm = 5;
                            break;

                        case 5:// Online Services
                            listBx.Items.Add("Quick Car Spawn \t\t ►");
                            listBx.Items.Add("Manual Car Spawn \t\t ►");

                            menuMainLvl = 1;
                            menuLvl = 7;

                            LastMenuMainLvl = 0;
                            LastMenuLvl = 1;
                            LastMenuItm = 7;
                            break;

                        case 6:// World
                            listBx.Items.Add("Kill NPCs");
                            listBx.Items.Add("Kill Enemies");
                            listBx.Items.Add("Kill Cops");
                            listBx.Items.Add("Blind Cops");
                            listBx.Items.Add("Bribe Cops");
                            listBx.Items.Add("Destroy Vehicles (NPCs)");
                            listBx.Items.Add("Destroy Vehicles (Enemies)");
                            listBx.Items.Add("Destroy Vehicles (Cops)");
                            listBx.Items.Add("Destroy Vehicles (All)");
                            listBx.Items.Add("Revive Vehicles");
                            listBx.Items.Add("Cop Killer");

                            menuMainLvl = 1;
                            menuLvl = 8;

                            LastMenuMainLvl = 0;
                            LastMenuLvl = 1;
                            LastMenuItm = 8;
                            break;
                    }
                    break;

                case 2:// Teleport
                    switch (menulevel)
                    {
                        case 2:// Location
                            listBx.Items.Add("Nightclub");              // 0
                            listBx.Items.Add("Arcade");                 // 1
                            listBx.Items.Add("Office");                 // 2
                            listBx.Items.Add("Bunker");                 // 3
                            listBx.Items.Add("Facility");               // 4
                            listBx.Items.Add("Hangar");                 // 5
                            listBx.Items.Add("Yacht");                  // 6
                            listBx.Items.Add("Kosatka");                // 4
                            listBx.Items.Add("Sell Vehicles & Cargo");  // 8
                            listBx.Items.Add("Goods Warehouse");        // 9
                            listBx.Items.Add("Auto Warehouse");         // 10
                            listBx.Items.Add("MC Clubhouse");           // 11
                            listBx.Items.Add("Meth Lab");               // 12
                            listBx.Items.Add("Cocaine Lockup");         // 13
                            listBx.Items.Add("Weed Farm");              // 14
                            listBx.Items.Add("Counterfeit Cash");       // 15
                            listBx.Items.Add("Document Forgery");       // 16
                            listBx.Items.Add("Casino");                 // 17
                            listBx.Items.Add("LS Car Meet");            // 18
                            listBx.Items.Add("Auto Shop Property");     // 19
                            listBx.Items.Add("Agency (F. Clinton)");    // 20
                            listBx.Items.Add("Music Locker");           // 21
                            listBx.Items.Add("Arena Workshop");         // 22
                            listBx.Items.Add("Cayo Perico");            // 23
                            listBx.Items.Add("Flight School");          // 24
                            listBx.Items.Add("Masks (Vespucci Beach)"); // 25
                            listBx.Items.Add("Record A Studios");       // 26

                            menuMainLvl = 5;
                            menuLvl = 1;

                            LastMenuMainLvl = 1;
                            LastMenuLvl = 5;
                            LastMenuItm = 2;
                            break;
                    }
                    break;

                case 3:// Online Services
                    switch (menulevel)
                    {
                        case 0:// Quick car spawn
                            listBx.Items.Add("ZR380");
                            listBx.Items.Add("Deluxo");
                            listBx.Items.Add("Opressor2");
                            listBx.Items.Add("Vigilante");
                            listBx.Items.Add("Toreador");
                            listBx.Items.Add("Future Brutus");
                            listBx.Items.Add("Future Dominator");
                            listBx.Items.Add("Future Imperator");

                            menuMainLvl = 3;
                            menuLvl = 0;

                            LastMenuMainLvl = 1;
                            LastMenuLvl = 3;
                            LastMenuItm = 0;
                            break;
                    }
                    break;
            }
            listBx.SelectedIndex = 0;
            mainForm.TopMost = true;
        }
        #endregion

        #region List functions
        public static void runitem(int mainMenulevel, int menulevel, int menuItem)
        {
            int[] tpIdArray;
            int[] tpColArray;
            Console.WriteLine("Command to run: " + mainMenulevel + " " + menulevel + " " + menuItem);
            switch (mainMenulevel)
            {
                case 0:
                    switch (menulevel)
                    {
                        case 0:
                            switch (menuItem)
                            {
                                case 0:
                                    listboxFill(1, 0);// Main
                                    break;
                                case 1:
                                    listboxFill(1, 1);// Session
                                    break;
                                case 2:
                                    listboxFill(1, 2);// Player
                                    break;
                                case 3:
                                    listboxFill(1, 3);// Vehicle
                                    break;
                                case 4:
                                    listboxFill(1, 4);// Online Services
                                    break;
                                case 5:
                                    listboxFill(1, 5);// World
                                    break;
                            }
                            break;
                    }
                    break;

                case 1:
                    switch (menulevel)
                    {
                        case 0:// Main Menu
                            switch (menuItem)
                            {
                                case 0:// Refresh Program
                                    Console.WriteLine("Nothing to do");
                                    break;
                                case 1:// Exit
                                    Quit();
                                    break;
                            }
                            break;
                        case 1:// Session
                            switch (menuItem)
                            {
                                case 0:// Public Session
                                    Activate();
                                    LoadSession(0);
                                    break;
                                case 1:// New Public Session
                                    Activate();
                                    LoadSession(1);
                                    break;
                                case 2:// Solo Session
                                    Activate();
                                    LoadSession(10);
                                    break;
                                case 3:// Leave Session
                                    Activate();
                                    LoadSession(-1);
                                    break;
                                case 4:// Empty Session
                                    Activate();
                                    empty_session();
                                    break;
                                case 5:// Invite Only Session
                                    Activate();
                                    LoadSession(11);
                                    break;
                                case 6:// Find Friend Session
                                    Activate();
                                    LoadSession(9);
                                    break;
                                case 7:// Closed Friend Session
                                    Activate();
                                    LoadSession(6);
                                    break;
                                case 8:// Crew Session
                                    Activate();
                                    LoadSession(3);
                                    break;
                                case 9:// Join Crew Session
                                    Activate();
                                    LoadSession(12);
                                    break;
                                case 10:// Closed Crew Session
                                    Activate();
                                    LoadSession(2);
                                    break;
                                    /*
                                case 11:
                                    Activate();
                                    LoadSession(-2);
                                    break;
                                    */
                            }
                            break;
                        case 2:// Player Tab
                            switch (menuItem)
                            {
                                case 0:// God mode
                                    bGodMode = !bGodMode;
                                    break;
                                case 1:
                                    listboxFill(2, 1);// Wanted Level
                                    break;
                            }
                            break;
                        case 3:// Vehicle
                            switch (menuItem)
                            {
                                case 0:// Vehicle God
                                    bVehicleGodMode = !bVehicleGodMode;
                                    break;
                            }
                            break;
                        case 4:// Teleport
                            switch (menuItem)
                            {
                                case 0:// Waypoint
                                    if (bGodMode)
                                    {
                                        bgodState = true;
                                    }
                                    else
                                    {
                                        bGodMode = true;
                                        bgodState = false;
                                    }
                                    Activate();
                                    tpIdArray = new int[] { 8 };
                                    tpColArray = new int[] { 84 };
                                    teleportBlip(tpIdArray, tpColArray, 20);
                                    if (!bgodState)
                                    {
                                        bGodMode = false;
                                    }
                                    break;
                                case 1:// Objective
                                    Activate();

                                    if (bGodMode)
                                    {
                                        bgodState = true;
                                    }
                                    else
                                    {
                                        bGodMode = true;
                                        bgodState = false;
                                    }
                                    tpIdArray = new int[] { 1 };
                                    tpColArray = new int[] { 5, 60, 66 };
                                    teleportBlip(tpIdArray, tpColArray);
                                    if (!bgodState)
                                    {
                                        bGodMode = false;
                                    }
                                    break;
                                case 2:// Location
                                    listboxFill(5, 2);
                                    break;
                            }
                            break;

                        case 5:// Online Services
                            switch (menuItem)
                            {
                                case 0:// Quick car spawn
                                    listboxFill(5, 0);
                                    break;
                                case 1:// Manual car spawn
                                    new Thread(() =>
                                    {
                                        Thread.CurrentThread.IsBackground = true;
                                        string promptValue = ShowDialog("Enter the name like \"opressor2\" without the quotes.", "Enter car name!");
                                        if (promptValue != "")
                                        {
                                            Activate();
                                            carSpawn(promptValue, 0);
                                        }
                                    }).Start();
                                    break;
                            }
                            break;
                        case 6:// World
                            switch (menuItem)
                            {
                                case 0:
                                    Activate();
                                    kill_npcs();
                                    break;
                                case 1:
                                    Activate();
                                    kill_enemies();
                                    break;
                                case 2:
                                    Activate();
                                    kill_cops();
                                    break;
                                case 3:
                                    Activate();
                                    blind_cops(true);
                                    break;
                                case 4:
                                    Activate();
                                    bribe_cops(true);
                                    break;
                                case 5:
                                    Activate();
                                    destroy_vehs_of_npcs();
                                    break;
                                case 6:
                                    Activate();
                                    destroy_vehs_of_enemies();
                                    break;
                                case 7:
                                    Activate();
                                    destroy_vehs_of_cops();
                                    break;
                                case 8:
                                    Activate();
                                    destroy_all_vehicles();
                                    break;
                                case 9:
                                    Activate();
                                    revive_all_vehicles();
                                    break;
                                case 10:
                                    bCopKiller = !bCopKiller;
                                    break;
                            }
                            break;
                    }
                    break;
                 case 2:
						switch (menuItem)
						{
							case 0:
                            Activate();
                            Mem.Write(Settings.WorldPTR, new int[] { offsets.pCPed, offsets.pCPlayerInfo, offsets.oWanted }, 0);
                            break;
							case 1:
                            Activate();
                            Mem.Write(Settings.WorldPTR, new int[] { offsets.pCPed, offsets.pCPlayerInfo, offsets.oWanted }, 1);
                                    break;
                            case 2:
                            Activate();
                            Mem.Write(Settings.WorldPTR, new int[] { offsets.pCPed, offsets.pCPlayerInfo, offsets.oWanted }, 2);
                            break;
                            case 3:
                            Activate();
                            Mem.Write(Settings.WorldPTR, new int[] { offsets.pCPed, offsets.pCPlayerInfo, offsets.oWanted }, 3);
                            break;
                            case 4:
                            Activate();
                            Mem.Write(Settings.WorldPTR, new int[] { offsets.pCPed, offsets.pCPlayerInfo, offsets.oWanted }, 4);
                            break;
                            case 5:
                            Activate();
                            Mem.Write(Settings.WorldPTR, new int[] { offsets.pCPed, offsets.pCPlayerInfo, offsets.oWanted }, 5);
                            break;
                        }
                        break;
                    }
                    break;
		}
                case 3:// Teleport
                    switch (menulevel)
                    {
                        case 1:
                            switch (menuItem)
                            {
                                case 0:
                                    Activate();

                                    if (bGodMode)
                                    {
                                        bgodState = true;
                                    }
                                    else
                                    {
                                        bGodMode = true;
                                        bgodState = false;
                                    }
                                    tpIdArray = new int[] { 614 };
                                    tpColArray = new int[] { };
                                    teleportBlip(tpIdArray, tpColArray);
                                    if (!bgodState)
                                    {
                                        bGodMode = false;
                                    }
                                    break;
                                case 1:
                                    Activate();

                                    if (bGodMode)
                                    {
                                        bgodState = true;
                                    }
                                    else
                                    {
                                        bGodMode = true;
                                        bgodState = false;
                                    }
                                    tpIdArray = new int[] { 740 };
                                    tpColArray = new int[] { };
                                    teleportBlip(tpIdArray, tpColArray);
                                    if (!bgodState)
                                    {
                                        bGodMode = false;
                                    }
                                    break;
                                case 2:
                                    Activate();

                                    if (bGodMode)
                                    {
                                        bgodState = true;
                                    }
                                    else
                                    {
                                        bGodMode = true;
                                        bgodState = false;
                                    }
                                    tpIdArray = new int[] { 475 };
                                    tpColArray = new int[] { };
                                    teleportBlip(tpIdArray, tpColArray);
                                    if (!bgodState)
                                    {
                                        bGodMode = false;
                                    }
                                    break;
                                case 3:
                                    Activate();

                                    if (bGodMode)
                                    {
                                        bgodState = true;
                                    }
                                    else
                                    {
                                        bGodMode = true;
                                        bgodState = false;
                                    }
                                    tpIdArray = new int[] { 557 };
                                    tpColArray = new int[] { };
                                    teleportBlip(tpIdArray, tpColArray);
                                    if (!bgodState)
                                    {
                                        bGodMode = false;
                                    }
                                    break;
                                case 4:
                                    Activate();

                                    if (bGodMode)
                                    {
                                        bgodState = true;
                                    }
                                    else
                                    {
                                        bGodMode = true;
                                        bgodState = false;
                                    }
                                    tpIdArray = new int[] { 590 };
                                    tpColArray = new int[] { };
                                    teleportBlip(tpIdArray, tpColArray);
                                    if (!bgodState)
                                    {
                                        bGodMode = false;
                                    }
                                    break;
                                case 5:
                                    Activate();

                                    if (bGodMode)
                                    {
                                        bgodState = true;
                                    }
                                    else
                                    {
                                        bGodMode = true;
                                        bgodState = false;
                                    }
                                    tpIdArray = new int[] { 569 };
                                    tpColArray = new int[] { };
                                    teleportBlip(tpIdArray, tpColArray);
                                    if (!bgodState)
                                    {
                                        bGodMode = false;
                                    }
                                    break;
                                case 6:
                                    Activate();

                                    if (bGodMode)
                                    {
                                        bgodState = true;
                                    }
                                    else
                                    {
                                        bGodMode = true;
                                        bgodState = false;
                                    }
                                    tpIdArray = new int[] { 455 };
                                    tpColArray = new int[] { };
                                    teleportBlip(tpIdArray, tpColArray);
                                    if (!bgodState)
                                    {
                                        bGodMode = false;
                                    }
                                    break;
                                case 7:
                                    Activate();

                                    if (bGodMode)
                                    {
                                        bgodState = true;
                                    }
                                    else
                                    {
                                        bGodMode = true;
                                        bgodState = false;
                                    }
                                    tpIdArray = new int[] { 760 };
                                    tpColArray = new int[] { };
                                    teleportBlip(tpIdArray, tpColArray);
                                    if (!bgodState)
                                    {
                                        bGodMode = false;
                                    }
                                    break;
                                case 8:
                                    Activate();

                                    if (bGodMode)
                                    {
                                        bgodState = true;
                                    }
                                    else
                                    {
                                        bGodMode = true;
                                        bgodState = false;
                                    }
                                    tpIdArray = new int[] { 64, 427, 478, 423, 501, 556 };
                                    tpColArray = new int[] { 2, 3 };
                                    teleportBlip(tpIdArray, tpColArray, 2);
                                    if (!bgodState)
                                    {
                                        bGodMode = false;
                                    }
                                    break;
                                case 9:
                                    Activate();

                                    if (bGodMode)
                                    {
                                        bgodState = true;
                                    }
                                    else
                                    {
                                        bGodMode = true;
                                        bgodState = false;
                                    }
                                    tpIdArray = new int[] { 473 };
                                    tpColArray = new int[] { };
                                    teleportBlip(tpIdArray, tpColArray);
                                    if (!bgodState)
                                    {
                                        bGodMode = false;
                                    }
                                    break;
                                case 10:
                                    Activate();

                                    if (bGodMode)
                                    {
                                        bgodState = true;
                                    }
                                    else
                                    {
                                        bGodMode = true;
                                        bgodState = false;
                                    }
                                    tpIdArray = new int[] { 524 };
                                    tpColArray = new int[] { };
                                    teleportBlip(tpIdArray, tpColArray);
                                    if (!bgodState)
                                    {
                                        bGodMode = false;
                                    }
                                    break;
                                case 11:
                                    Activate();

                                    if (bGodMode)
                                    {
                                        bgodState = true;
                                    }
                                    else
                                    {
                                        bGodMode = true;
                                        bgodState = false;
                                    }
                                    tpIdArray = new int[] { 492 };
                                    tpColArray = new int[] { };
                                    teleportBlip(tpIdArray, tpColArray);
                                    if (!bgodState)
                                    {
                                        bGodMode = false;
                                    }
                                    break;
                                case 12:
                                    Activate();

                                    if (bGodMode)
                                    {
                                        bgodState = true;
                                    }
                                    else
                                    {
                                        bGodMode = true;
                                        bgodState = false;
                                    }
                                    tpIdArray = new int[] { 499 };
                                    tpColArray = new int[] { };
                                    teleportBlip(tpIdArray, tpColArray);
                                    if (!bgodState)
                                    {
                                        bGodMode = false;
                                    }
                                    break;
                                case 13:
                                    Activate();

                                    if (bGodMode)
                                    {
                                        bgodState = true;
                                    }
                                    else
                                    {
                                        bGodMode = true;
                                        bgodState = false;
                                    }
                                    tpIdArray = new int[] { 497 };
                                    tpColArray = new int[] { };
                                    teleportBlip(tpIdArray, tpColArray);
                                    if (!bgodState)
                                    {
                                        bGodMode = false;
                                    }
                                    break;
                                case 14:
                                    Activate();

                                    if (bGodMode)
                                    {
                                        bgodState = true;
                                    }
                                    else
                                    {
                                        bGodMode = true;
                                        bgodState = false;
                                    }
                                    tpIdArray = new int[] { 496 };
                                    tpColArray = new int[] { };
                                    teleportBlip(tpIdArray, tpColArray);
                                    if (!bgodState)
                                    {
                                        bGodMode = false;
                                    }
                                    break;
                                case 15:
                                    Activate();

                                    if (bGodMode)
                                    {
                                        bgodState = true;
                                    }
                                    else
                                    {
                                        bGodMode = true;
                                        bgodState = false;
                                    }
                                    tpIdArray = new int[] { 500 };
                                    tpColArray = new int[] { };
                                    teleportBlip(tpIdArray, tpColArray);
                                    if (!bgodState)
                                    {
                                        bGodMode = false;
                                    }
                                    break;
                                case 16:
                                    Activate();

                                    if (bGodMode)
                                    {
                                        bgodState = true;
                                    }
                                    else
                                    {
                                        bGodMode = true;
                                        bgodState = false;
                                    }
                                    tpIdArray = new int[] { 498 };
                                    tpColArray = new int[] { };
                                    teleportBlip(tpIdArray, tpColArray);
                                    if (!bgodState)
                                    {
                                        bGodMode = false;
                                    }
                                    break;
                                case 17:
                                    Activate();

                                    if (bGodMode)
                                    {
                                        bgodState = true;
                                    }
                                    else
                                    {
                                        bGodMode = true;
                                        bgodState = false;
                                    }
                                    tpIdArray = new int[] { 679 };
                                    tpColArray = new int[] { };
                                    teleportBlip(tpIdArray, tpColArray);
                                    if (!bgodState)
                                    {
                                        bGodMode = false;
                                    }
                                    break;
                                case 18:
                                    Activate();

                                    if (bGodMode)
                                    {
                                        bgodState = true;
                                    }
                                    else
                                    {
                                        bGodMode = true;
                                        bgodState = false;
                                    }
                                    tpIdArray = new int[] { 777 };
                                    tpColArray = new int[] { };
                                    teleportBlip(tpIdArray, tpColArray);
                                    if (!bgodState)
                                    {
                                        bGodMode = false;
                                    }
                                    break;
                                case 19:
                                    Activate();

                                    if (bGodMode)
                                    {
                                        bgodState = true;
                                    }
                                    else
                                    {
                                        bGodMode = true;
                                        bgodState = false;
                                    }
                                    tpIdArray = new int[] { 779 };
                                    tpColArray = new int[] { };
                                    teleportBlip(tpIdArray, tpColArray);
                                    if (!bgodState)
                                    {
                                        bGodMode = false;
                                    }
                                    break;
                                case 20:
                                    Activate();

                                    if (bGodMode)
                                    {
                                        bgodState = true;
                                    }
                                    else
                                    {
                                        bGodMode = true;
                                        bgodState = false;
                                    }
                                    tpIdArray = new int[] { 826 };
                                    tpColArray = new int[] { };
                                    teleportBlip(tpIdArray, tpColArray);
                                    if (!bgodState)
                                    {
                                        bGodMode = false;
                                    }
                                    break;
                                case 21:
                                    Activate();

                                    if (bGodMode)
                                    {
                                        bgodState = true;
                                    }
                                    else
                                    {
                                        bGodMode = true;
                                        bgodState = false;
                                    }
                                    tpIdArray = new int[] { 136 };
                                    tpColArray = new int[] { };
                                    teleportBlip(tpIdArray, tpColArray);
                                    if (!bgodState)
                                    {
                                        bGodMode = false;
                                    }
                                    break;
                                case 22:
                                    Activate();

                                    if (bGodMode)
                                    {
                                        bgodState = true;
                                    }
                                    else
                                    {
                                        bGodMode = true;
                                        bgodState = false;
                                    }
                                    tpIdArray = new int[] { 643 };
                                    tpColArray = new int[] { };
                                    teleportBlip(tpIdArray, tpColArray);
                                    if (!bgodState)
                                    {
                                        bGodMode = false;
                                    }
                                    break;
                                case 23:
                                    Activate();

                                    if (bGodMode)
                                    {
                                        bgodState = true;
                                    }
                                    else
                                    {
                                        bGodMode = true;
                                        bgodState = false;
                                    }
                                    tpIdArray = new int[] { 766 };
                                    tpColArray = new int[] { };
                                    teleportBlip(tpIdArray, tpColArray);
                                    if (!bgodState)
                                    {
                                        bGodMode = false;
                                    }
                                    break;
                                case 24:
                                    Activate();

                                    if (bGodMode)
                                    {
                                        bgodState = true;
                                    }
                                    else
                                    {
                                        bGodMode = true;
                                        bgodState = false;
                                    }
                                    tpIdArray = new int[] { 90 };
                                    tpColArray = new int[] { };
                                    teleportBlip(tpIdArray, tpColArray);
                                    if (!bgodState)
                                    {
                                        bGodMode = false;
                                    }
                                    break;
                                case 25:
                                    Activate();

                                    if (bGodMode)
                                    {
                                        bgodState = true;
                                    }
                                    else
                                    {
                                        bGodMode = true;
                                        bgodState = false;
                                    }
                                    tpIdArray = new int[] { 362 };
                                    tpColArray = new int[] { };
                                    teleportBlip(tpIdArray, tpColArray);
                                    if (!bgodState)
                                    {
                                        bGodMode = false;
                                    }
                                    break;
                                case 26:// Record A Studios
                                    Activate();

                                    if (bGodMode)
                                    {
                                        bgodState = true;
                                    }
                                    else
                                    {
                                        bGodMode = true;
                                        bgodState = false;
                                    }
                                    tpIdArray = new int[] { 819 };
                                    tpColArray = new int[] { };
                                    teleportBlip(tpIdArray, tpColArray);
                                    if (!bgodState)
                                    {
                                        bGodMode = false;
                                    }
                                    break;
                            }
                            break;
                    }
                    break;
		}
                case 4:// Casino Prize Wheel
                    switch (menulevel)
                    {
                        case 0:// Quick car spawn
                            switch (menuItem)
                            {
                                case 0:
                                    Activate();
                                    carSpawn("zr380", 0);
                                    break;
                                case 1:
                                    Activate();
                                    carSpawn("Deluxo", 0);
                                    break;
                                case 2:
                                    Activate();
                                    carSpawn("oppressor2", 0);
                                    break;
                                case 3:
                                    Activate();
                                    carSpawn("vigilante", 0);
                                    break;
                                case 4:
                                    Activate();
                                    carSpawn("Toreador", 0);
                                    break;
                                case 5:
                                    Activate();
                                    carSpawn("brutus2", 0);
                                    break;
                                case 6:
                                    Activate();
                                    carSpawn("dominator5", 0);
                                    break;
                                case 7:
                                    Activate();
                                    carSpawn("imperator2", 0);
                                    break;
                            }
                            break;
                    }
                    break;
            }

        }
        #endregion

        #region Keyboard hooks
        public static void runSingleItem()
        {
            Console.WriteLine("Command to run backward: " + LastMenuMainLvl + " " + LastMenuLvl + " " + LastMenuItm);
            int oldMenuItm = LastMenuItm;
            ListboxFill(LastMenuMainLvl, LastMenuLvl);
            listBx.SelectedIndex = oldMenuItm;
        }

        private static void mainlistup()
        {
            if (listBx.SelectedIndex > 0)
            {
                listBx.SelectedIndex = listBx.SelectedIndex - 1;
                menuItm = listBx.SelectedIndex;
            }
        }

        private static void mainlistdown()
        {
            if (listBx.SelectedIndex < listBx.Items.Count - 1)
            {
                listBx.SelectedIndex = listBx.SelectedIndex + 1;
                menuItm = listBx.SelectedIndex;
            }
        }

        public static void showHideOverlay()
        {
            if (!isHidden)
            {
                isHidden = true;
                ShowWindow(formHandle, SW_HIDE);
            }
            else
            {
                isHidden = false;
                ShowWindow(formHandle, 1);
            }
        }

        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
                    GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private delegate IntPtr LowLevelKeyboardProc(
            int nCode, IntPtr wParam, IntPtr lParam);

        private static IntPtr HookCallback(
            int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                if ((Keys)vkCode == Keys.Insert)
                {
                    showHideOverlay();
                }
                else if ((Keys)vkCode == Keys.NumPad0)
                {
                    if (!isHidden)
                    {
                        if (menuMainLvl <= 0)
                        {
                            isHidden = true;
                            ShowWindow(formHandle, SW_HIDE);
                        }
                        else
                        {
                            runSingleItem();
                        }
                    }
                }
                else if ((Keys)vkCode == Keys.NumPad5)// To activate/toggle
                {
                    runitem(menuMainLvl, menuLvl, listBx.SelectedIndex);
                }
                else if ((Keys)vkCode == Keys.Up || (Keys)vkCode == Keys.Down || (Keys)vkCode == Keys.Left || (Keys)vkCode == Keys.Right)
                {
                    if (!isHidden)
                    {
                        return (IntPtr)1;
                    }
                }
                else if ((Keys)vkCode == Keys.NumPad2)
                {
                    if (!isHidden)
                    {
                        mainlistdown();
                    }
                }
                else if ((Keys)vkCode == Keys.NumPad8)
                {
                    if (!isHidden)
                    {
                        mainlistup();
                    }
                }
                else if ((Keys)vkCode == Keys.Delete)
                {
                    if (!isHidden)
                    {
                        Quit();
                    }
                }
                /*else if ((Keys)vkCode == Keys.NumPad1)
                {
                    if (!isHidden)
                    {
                        
                          Development
                        
                    }
                }*/
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }
        #endregion

        #region Methods
        public static void pGODMODE()
        {
            if (bGodMode)
            {
                Mem.Write(Settings.WorldPTR, new int[] { offsets.pCPed, offsets.oGod }, 1);
                if (!Settings.pgodm)
                {
                    Activate();
                }
                Settings.pgodm = true;
            }
            else
            {
                if (Settings.pgodm)
                {
                    Mem.Write(Settings.WorldPTR, new int[] { offsets.pCPed, offsets.oGod }, 0);
                    Settings.pgodm = false;
                    Deactivate();
                }
            }
        }

        public static void vGODMODE()
        {
            long paddr = Mem.ReadPointer(Settings.WorldPTR, new int[] { offsets.pCPed, offsets.pCVehicle });
            if (paddr > 0)
            {
                long paddr2 = Mem.GetPtrAddr(paddr + offsets.oGod, null);
                if (bVehicleGodMode)
                {
                    Mem.writeInt(paddr2, null, 1);
                    if (!Settings.vgodm)
                    {
                        Activate();
                    }
                    Settings.vgodm = true;
                }
                else
                {
                    if (Settings.vgodm)
                    {
                        Mem.writeInt(paddr2, null, 0);
                        Settings.vgodm = false;
                        Deactivate();
                    }
                }
            }
        }
		
        public static void vCOPKILLER()
        {
            if (bCopKiller)
            {
                if (!Settings.cKiller)
                {
                    Settings.cKiller = true;
                    Activate();
                }
                kill_cops();
            }
            else
            {
                if (Settings.cKiller)
                {
                    Settings.cKiller = false;
                    Deactivate();
                }
            }
        }
		
        public static void carSpawn(string Hash, int pegasus = 0)
        {
            string model = Hash.ToLower();
            float ped_heading = Mem.ReadFloat(Settings.WorldPTR, new int[] { offsets.pCPed, offsets.pCNavigation, offsets.oHeading });
            float ped_heading2 = Mem.ReadFloat(Settings.WorldPTR, new int[] { offsets.pCPed, offsets.pCNavigation, offsets.oHeading2 });
            Console.WriteLine(ped_heading + " " + ped_heading2);
            float spawner_x = PlayerX;
            float spawner_y = PlayerY;
            float spawner_z = PlayerZ;
            spawner_x = spawner_x - (ped_heading2 * 5f);
            spawner_y = spawner_y + (ped_heading * 5f);
            spawner_z = spawner_z + 0.5f;
            SG<float>(offsets.oVMCreate + 7 + 0, spawner_x);
            SG<float>(offsets.oVMCreate + 7 + 1, spawner_y);
            SG<float>(offsets.oVMCreate + 7 + 2, spawner_z);
            SG<uint>(offsets.oVMCreate + 27 + 66, Joaat(Hash));
            SG<int>(offsets.oVMCreate + 27 + 28, 1); // Weaponised ownerflag
            SG<int>(offsets.oVMCreate + 27 + 60, 1);
            SG<int>(offsets.oVMCreate + 27 + 95, 14); // Ownerflag
            SG<int>(offsets.oVMCreate + 27 + 94, 2); // Personal car ownerflag
            SG<int>(offsets.oVMCreate + 5, 1); // SET('i', CarSpawn + 0x1168, 1)--can spawn flag must be odd
            SG<int>(offsets.oVMCreate + 2, 1); // SET('i', CarSpawn + 0x1180, 1)--spawn toggle gets reset to 0 on car spawn
            SG<int>(offsets.oVMCreate + 3, pegasus);
            SG<int>(offsets.oVMCreate + 27 + 74, 1); // Red Neon Amount 1-255 100%-0%
            SG<int>(offsets.oVMCreate + 27 + 75, 1); // Green Neon Amount 1-255 100%-0%
            SG<int>(offsets.oVMCreate + 27 + 76, 0); // Blue Neon Amount 1-255 100%-0%
            SG<uint>(offsets.oVMCreate + 27 + 60, 4030726305); // landinggear / vehstate
            SG<int>(offsets.oVMCreate + 27 + 5, -1); // default paintjob primary -1 auto 120
            SG<int>(offsets.oVMCreate + 27 + 6, -1); // default paintjob secondary -1 auto 120
            SG<int>(offsets.oVMCreate + 27 + 7, -1);
            SG<int>(offsets.oVMCreate + 27 + 8, -1);
            SG<int>(offsets.oVMCreate + 27 + 19, 4);
            SG<int>(offsets.oVMCreate + 27 + 21, 4); // Engine(0 - 3)
            SG<int>(offsets.oVMCreate + 27 + 22, 3);
            SG<int>(offsets.oVMCreate + 27 + 23, 3); // Transmission(0 - 9)
            SG<int>(offsets.oVMCreate + 27 + 24, 58);
            SG<int>(offsets.oVMCreate + 27 + 26, 5); // Armor(0 - 18)
            SG<int>(offsets.oVMCreate + 27 + 27, 1); // Turbo(0 - 1)
            SG<int>(offsets.oVMCreate + 27 + 65, 2); // Window tint 0 - 6
            SG<int>(offsets.oVMCreate + 27 + 69, -1); // Wheel type
            SG<int>(offsets.oVMCreate + 27 + 33, -1); // Wheel Selection
            SG<int>(offsets.oVMCreate + 27 + 25, 8); // Suspension(0 - 13)
            SG<int>(offsets.oVMCreate + 27 + 19, -1);
            Mem.Write(GA(offsets.oVMCreate + 27 + 77) + 1, null, 2); // 2:bulletproof 0:false

            int weapon1 = 2;
            int weapon2 = 1;

            if (model == "oppressor2")
            {
                weapon1 = 2;
            }
            else if (model == "apc")
            {
                weapon1 = -1;
            }
            else if (model == "deluxo")
            {
                weapon1 = -1;
            }
            else if (model == "bombushka")
            {
                weapon1 = 1;
            }
            else if (model == "tampa3")
            {
                weapon1 = 3;
            }
            else if (model == "insurgent3")
            {
                weapon1 = 3;
            }
            else if (model == "halftrack")
            {
                weapon1 = 3;
            }
            else if (model == "barrage")
            {
                weapon1 = 30;
            }
            SG<int>(offsets.oVMCreate + 27 + 15, weapon1); // primary weapon
            SG<int>(offsets.oVMCreate + 27 + 20, weapon2); // primary weapon
            // _SG_Int(offsets.oVMCreate + 27 + 1, "FCK4FD"); // License plate
        }

        public static void LoadSession(int id)
        {
            Task.Run(() =>
            {
                SG<int>(1575015, id);//1575012
                SG<int>(1574589 + 2, id == -1 ? -1 : 0);
                SG<int>(1574589, 1);
            });
        }

        public static void empty_session()
        {
            Task.Run(() =>
            {
                ProcessMgr.SuspendProcess(Settings.gameProcess);
                Task.Delay(10000).Wait();
                ProcessMgr.ResumeProcess(Settings.gameProcess);
            });
        }

        public static void getPeds()
        {
            int pedListOffset = 0x10;
            int count = Mem.ReadInt(Settings.ReplayInterfacePTR, new int[] { offsets.pCPedInterface, offsets.oPedNum });
            for (int i = 0; i <= count; i++)
            {
                long Ped = Mem.ReadPointer(Settings.ReplayInterfacePTR, new int[] { offsets.pCPedInterface, offsets.pPedList, (i * pedListOffset) });
                int pedType = Mem.ReadByte(Settings.ReplayInterfacePTR, new int[] { offsets.pCPedInterface, offsets.pPedList, (i * pedListOffset), offsets.oEntityType });
                if (pedType != 156 && Mem.IsValid(Ped))
                {
                    pedList.Add(Ped);
                }
            }
        }

        public static void getVehs()
        {
            int count = Mem.ReadInt(Settings.ReplayInterfacePTR, new int[] { offsets.pCVehicleInterface, offsets.oVehNum });
            for (int i = 0; i <= count; i++)
            {
                long Veh = Mem.ReadPointer(Settings.ReplayInterfacePTR, new int[] { offsets.pCVehicleInterface, offsets.pVehList, (i * 0x10) });
                if (Mem.IsValid(Veh))
                {
                    vehList.Add(Veh);
                }
            }
        }

        public static void kill_npcs()
        {
            Task.Run(() =>
            {
                getPeds();
                for (int i = 0; i < pedList.Count; i++)
                {
                    long ped = pedList[i];
                    uint pedtype = get_pedtype(ped);
                    set_health(ped, 0.0f);
                }
            });
        }

        public static void kill_enemies()
        {
            Task.Run(() =>
            {
                getPeds();
                for (int i = 0; i < pedList.Count; i++)
                {
                    long ped = pedList[i];
                    if (is_enemy(ped))
                    {
                        set_health(ped, 0.0f);
                    }
                }
            });
        }

        public static void kill_cops()
        {
            Task.Run(() =>
            {
                getPeds();
                for (int i = 0; i < pedList.Count; i++)
                {
                    long ped = pedList[i];
                    uint pedtype = get_pedtype(ped);
                    if (pedtype == (uint)EnumData.PedTypes.COP ||
                        pedtype == (uint)EnumData.PedTypes.SWAT ||
                        pedtype == (uint)EnumData.PedTypes.ARMY)
                    {
                        set_health(ped, 0.0f);
                    }
                }
            });
        }

        public static void blind_cops(bool toggle = true)
        {
            Task.Run(() =>
            {
                SG<int>(offsets.oVMYCar + 4625, toggle ? 1 : 0);
                if (toggle) SG<int>(offsets.oVMYCar + 4627, get_network_time() + 3600000);
                SG<int>(offsets.oVMYCar + 4624, toggle ? 5 : 0);
            });
        }

        public static void bribe_cops(bool toggle = true)
        {
            Task.Run(() =>
            {
                SG<int>(offsets.oVMYCar + 4625, toggle ? 1 : 0);
                if (toggle) SG<int>(offsets.oVMYCar + 4627, get_network_time() + 3600000);
                SG<int>(offsets.oVMYCar + 4624, toggle ? 21 : 0);
            });
        }

        public static void destroy_vehicle(long vehicle)
        {
            Task.Run(() =>
            {
                revive_vehicle(vehicle);
                set_health3(vehicle, -999.9f);
            });
        }

        public static void destroy_vehs_of_npcs()
        {
            Task.Run(() =>
            {
                getPeds();
                for (int i = 0; i < pedList.Count; i++)
                {
                    long ped = pedList[i];
                    if (is_player(ped)) continue;
                    destroy_vehicle(get_current_vehicle(ped));
                }
            });
        }

        public static void destroy_vehs_of_enemies()
        {
            Task.Run(() =>
            {
                getPeds();
                for (int i = 0; i < pedList.Count; i++)
                {
                    long ped = pedList[i];
                    if (ped == get_local_ped()) continue;
                    if (is_enemy(ped)) destroy_vehicle(get_current_vehicle(ped));
                }
            });
        }

        public static void destroy_vehs_of_cops()
        {
            Task.Run(() =>
            {
                getPeds();
                for (int i = 0; i < pedList.Count; i++)
                {
                    long ped = pedList[i];
                    if (ped == get_local_ped()) continue;
                    uint pedtype = get_pedtype(ped);
                    if (pedtype == (uint)EnumData.PedTypes.COP ||
                        pedtype == (uint)EnumData.PedTypes.SWAT ||
                        pedtype == (uint)EnumData.PedTypes.ARMY) destroy_vehicle(get_current_vehicle(ped));
                }
            });
        }

        public static void destroy_all_vehicles()
        {
            Task.Run(() =>
            {
                getVehs();
                for (int i = 0; i < vehList.Count; i++)
                {
                    long vehicle = vehList[i];
                    destroy_vehicle(vehicle);
                }
            });
        }

        public static void revive_all_vehicles()
        {
            Task.Run(() =>
            {
                getVehs();
                for (int i = 0; i < vehList.Count; i++)
                {
                    long vehicle = vehList[i];
                    revive_vehicle(vehicle);
                }
            });
        }

        public static void fill_all_ammo()
        {
            long p = get_ped_inventory(get_local_ped());
            p = Mem.Read<long>(p + 0x48);
            int count = 0;
            while (Mem.Read<int>(p + count * 0x08) != 0 && Mem.Read<int>(p + count * 0x08, new int[] { 0x08 }) != 0)
            {
                Func<int, int, int> Max = (int a, int b) => { return a > b ? a : b; };
                int max_ammo = Max(Mem.Read<int>(p + count * 0x08, new int[] { 0x08, 0x28 }), Mem.Read<int>(p + count * 0x08, new int[] { 0x08, 0x34 }));
                if (max_ammo > 0)
                {
                    Mem.Write<int>(p + count * 0x08, new int[] { 0x20 }, max_ammo);
                }
                count++;
            }
            Activate();
        }

        public static long get_local_ped() { return Mem.ReadPointer(Settings.WorldPTR, new int[] { 0x8 }); }
        public static long get_playerinfo(long ped) { return Mem.Read<long>(ped + offsets.pCPlayerInfo); }
        public static int get_network_time() { return GG<int>(1574755 + 11); }
        public static int player_id() { return GG<int>(offsets.oPlayerGA); }
        public static byte get_type(long entity) { return Mem.Read<byte>(entity + 0x2B); }
        public static bool is_player(long entity) { return ((get_type(entity) == 156) ? true : false); }
        public static byte get_hostility(long ped) { return Mem.Read<byte>(ped + 0x18C); }
        public static bool is_enemy(long ped) { return ((get_hostility(ped) > 1) ? true : false); }
        public static uint get_pedtype(long ped) { return Mem.Read<uint>(ped + 0x10B8) << 11 >> 25; }
        public static void set_health(long ped, float value) { Mem.Write<float>(ped + 0x280, value); }
        public static long get_ped_inventory(long ped) { return Mem.Read<long>(ped + 0x10D0); }
        public static bool is_in_vehicle(long ped) { return ((Mem.Read<byte>(ped + 0xE52) == 1) ? true : false); }

        public static void set_health3(long vehicle, float value) { Mem.Write<float>(vehicle + 0x844, value); }
        public static void set_health2(long vehicle, float value) { Mem.Write<float>(vehicle + 0x840, value); }
        public static void set_engine_health(long vehicle, float value) { Mem.Write<float>(vehicle + 0x908, value); }
        public static byte get_state(long vehicle) { return Mem.Read<byte>(vehicle + 0xD8); }
        public static void set_state(long vehicle, byte value) { Mem.Write<byte>(vehicle + 0xD8, value); }
        public static long get_current_vehicle(long ped) { return Mem.Read<long>(ped + 0xD30); }
        public static long get_navigation(long entity) { return Mem.Read<long>(entity + 0x30); }
        public static void set_state_is_destroyed(long vehicle, bool toggle)
        {
            byte temp = get_state(vehicle);
            if (toggle) temp = (byte)(temp | (1 << 1) | (1 << 0));
            else temp = (byte)(temp & ~(1 << 0));
            set_state(vehicle, temp);
        }

        public static void revive_vehicle(long vehicle)
        {
            set_state_is_destroyed(vehicle, false);
            set_health(vehicle, 1000.0f);
            set_health2(vehicle, 1000.0f);
            set_health3(vehicle, 1000.0f);
            set_engine_health(vehicle, 1000.0f);
        }
        #endregion

        #region Teleport part
        private static void Teleport(Location l)
        {
            if (Mem.ReadInt(Settings.WorldPTR, new int[] { offsets.pCPed, offsets.oInVehicle }) == 0)
            {
                CarX = l.x;
                CarY = l.y;
                CarZ = l.z;
            }
            else
            {
                PlayerX = l.x;
                PlayerY = l.y;
                PlayerZ = l.z;
            }
        }

        private static void teleportBlip(int[] ID, int[] color, int height = 0)
        {
            Location tmpLoc = getBlipCoords(ID, color, height);
            if (tmpLoc.x != 0 && tmpLoc.y != 0)
            {
                Teleport(tmpLoc);
            }
            else
            {
                Console.WriteLine("No TP, wrong coords (x, y).");
            }
        }

        private static Location getBlipCoords(int[] id, int[] color = null, int height = 0)
        {
            float zOffset = 0;
            Location tempLocation = new Location() { };
            for (int i = 2000; i > 1; i--)
            {
                long blip = Settings.BlipPTR + (i * 8);
                int blipId = Mem.ReadInt(blip, new int[] { 0x40 });
                int blipColor = Mem.ReadInt(blip, new int[] { 0x48 });
                if (id != null && id.Contains(blipId))
                {
                    zOffset = (float)(Math.Round(Math.Pow(i, -0.2), 1) * height);
                    tempLocation = new Location
                    {
                        x = Mem.ReadFloat(blip, new int[] { 0x10 }),
                        y = Mem.ReadFloat(blip, new int[] { 0x14 }),
                        z = Mem.ReadFloat(blip, new int[] { 0x18 })
                    };

                    if (color != null && color.Contains(blipColor))
                    {
                        tempLocation = new Location
                        {
                            x = Mem.ReadFloat(blip, new int[] { 0x10 }),
                            y = Mem.ReadFloat(blip, new int[] { 0x14 }),
                            z = Mem.ReadFloat(blip, new int[] { 0x18 })
                        };
                    }
                }
            }
            if (tempLocation.z == 20)
            {
                tempLocation.z = -255f;
            } else
            {
                tempLocation.z = tempLocation.z + zOffset;
            }

            Console.WriteLine("New location: " + tempLocation.x + ", " + tempLocation.y + ", " + tempLocation.z);
            return new Location { x = tempLocation.x, y = tempLocation.y, z = tempLocation.z };
        }

        public static float PlayerX
        {
            get { return Mem.ReadFloat(Settings.WorldPTR, new int[] { offsets.pCPed, offsets.pCNavigation, offsets.oPositionX }); }
            set
            {
                Mem.Write(Settings.WorldPTR, new int[] { offsets.pCPed, offsets.pCNavigation, offsets.oPositionX }, value);
                Mem.Write(Settings.WorldPTR, new int[] { offsets.pCPed, offsets.oVisualX }, value);
            }
        }
        public static float PlayerY
        {
            get { return Mem.ReadFloat(Settings.WorldPTR, new int[] { offsets.pCPed, offsets.pCNavigation, offsets.oPositionY }); }
            set
            {
                Mem.Write(Settings.WorldPTR, new int[] { offsets.pCPed, offsets.pCNavigation, offsets.oPositionY }, value);
                Mem.Write(Settings.WorldPTR, new int[] { offsets.pCPed, offsets.oVisualY }, value);
            }
        }
        public static float PlayerZ
        {
            get { return Mem.ReadFloat(Settings.WorldPTR, new int[] { offsets.pCPed, offsets.pCNavigation, offsets.oPositionZ }); }
            set
            {
                Mem.Write(Settings.WorldPTR, new int[] { offsets.pCPed, offsets.pCNavigation, offsets.oPositionZ }, value);
                Mem.Write(Settings.WorldPTR, new int[] { offsets.pCPed, offsets.oVisualZ }, value);
            }
        }

        public static float CarX
        {
            get { return Mem.ReadFloat(Settings.WorldPTR, new int[] { offsets.pCPed, offsets.pCVehicle, offsets.pCNavigation, offsets.oPositionX }); }
            set
            {
                Mem.Write(Settings.WorldPTR, new int[] { offsets.pCPed, offsets.pCVehicle, offsets.pCNavigation, offsets.oPositionX }, value);
                Mem.Write(Settings.WorldPTR, new int[] { offsets.pCPed, offsets.pCVehicle, offsets.oVisualX }, value);
            }
        }
        public static float CarY
        {
            get { return Mem.ReadFloat(Settings.WorldPTR, new int[] { offsets.pCPed, offsets.pCVehicle, offsets.pCNavigation, offsets.oPositionY }); }
            set
            {
                Mem.Write(Settings.WorldPTR, new int[] { offsets.pCPed, offsets.pCVehicle, offsets.pCNavigation, offsets.oPositionY }, value);
                Mem.Write(Settings.WorldPTR, new int[] { offsets.pCPed, offsets.pCVehicle, offsets.oVisualY }, value);
            }
        }
        public static float CarZ
        {
            get { return Mem.ReadFloat(Settings.WorldPTR, new int[] { offsets.pCPed, offsets.pCVehicle, offsets.pCNavigation, offsets.oPositionZ }); }
            set
            {
                Mem.Write(Settings.WorldPTR, new int[] { offsets.pCPed, offsets.pCVehicle, offsets.pCNavigation, offsets.oPositionZ }, value);
                Mem.Write(Settings.WorldPTR, new int[] { offsets.pCPed, offsets.pCVehicle, offsets.oVisualZ }, value);
            }
        }
        #endregion

        #region Global Addresses function
        public static T GG<T>(int index) where T : struct { return Mem.Read<T>(GA(index)); }

        public static void SG<T>(int index, T vaule) where T : struct { Mem.Write<T>(GA(index), vaule); }
        
        public static long GA(int Index)
        {
            long p = Settings.GlobalPTR + (8 * (Index >> 0x12 & 0x3F));
            long p_ga = Mem.ReadPointer(p, null);
            long p_ga_final = p_ga + (8 * (Index & 0x3FFFF));
            return p_ga_final;
        }
        #endregion

        #region JOAAT function
        public static uint Joaat(string input)
        {
            uint num1 = 0U;
            input = input.ToLower();
            foreach (char c in input)
            {
                uint num2 = num1 + c;
                uint num3 = num2 + (num2 << 10);
                num1 = num3 ^ num3 >> 6;
            }
            uint num4 = num1 + (num1 << 3);
            uint num5 = num4 ^ num4 >> 11;

            return num5 + (num5 << 15);
        }
        #endregion

        #region Local Script function
        public static long GetLocalScript(string name)
        {
            int size = name.Length;
            for (int i = 0; i <= 52; i++)
            {
                long lc_p = Mem.ReadPointer(Settings.LocalScriptsPTR, new int[] { (i * 8), 0xB0 });
                string lc_n = Mem.ReadString(Settings.LocalScriptsPTR, new int[] { (i * 8), 0xD0 }, size);
                if (lc_n == name)
                {
                    i = 53;
                    return (lc_p);
                }
            }
            return 0;
        }
        #endregion
    }
    struct Location { public float x, y, z; }
}
