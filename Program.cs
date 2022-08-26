using GTAVCSMM.Config;
using GTAVCSMM.Helpers;
using GTAVCSMM.Memory;
using GTAVCSMM.Settings;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;//Suspend process Empty session
using System.Windows.Forms;

namespace GTAVCSMM
{
    static class Program
    {
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

        #region WINDOW SETUP

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
        public static Addresses addresses = new Addresses();
        public static Patterns pattern = new Patterns();
        public static TSettings settings = new TSettings();
        public static Mem Mem;
        //public static Thread _freezeGame;

        public static System.Windows.Forms.Timer ProcessTimer = new System.Windows.Forms.Timer();
        public static System.Windows.Forms.Timer MemoryTimer = new System.Windows.Forms.Timer();
        public static System.Windows.Forms.Timer fastTimer = new System.Windows.Forms.Timer();

        #endregion

        #region PROCESS INFO
        private static bool bGodMode = false;
        private static bool bgodState = false;
        private static int frameFlagCount = 0;

        [DllImport("user32.dll")]
        static extern short GetAsyncKeyState(System.Windows.Forms.Keys vKey);

        [DllImport("user32.dll")]
        public static extern void mouse_event(int a, int b, int c, int d, int damnIwonderifpeopleactuallyreadsthis);
        #endregion

        #region METHODS
        public static void pGODMODE()
        {
            if (bGodMode)
            {
                Mem.Write(settings.WorldPTR, new int[] { offsets.pCPed, offsets.oGod }, 1);
                if (!settings.pgodm)
                {
                    Activate();
                }
                settings.pgodm = true;
            }
            else
            {
                if (settings.pgodm)
                {
                    Mem.Write(settings.WorldPTR, new int[] { offsets.pCPed, offsets.oGod }, 0);
                    settings.pgodm = false;
                    Deactivate();
                }
            }
        }

        public static void getPointer()
        {
            try
            {
                Mem = new Mem(settings.gameName);

                var processes = Process.GetProcessesByName(settings.gameName);
                foreach (var p in processes)
                {
                    if (p.Id > 0)
                    {
                        settings.gameProcess = p.Id;
                    }
                }

                if (settings.gameProcess > 0)
                {
                    // GlobalPTR
                    long addr = Mem.FindPattern(pattern.GlobalPTR, pattern.GlobalPTR_Mask);
                    settings.GlobalPTR = addr + Mem.ReadInt(addr + 3, null) + 7;

                    // WorldPTR
                    long addr2 = Mem.FindPattern(pattern.WorldPTR, pattern.WorldPTR_Mask);
                    settings.WorldPTR = addr2 + Mem.ReadInt(addr2 + 3, null) + 7;

                    // BlipPTR
                    long addr3 = Mem.FindPattern(pattern.BlipPTR, pattern.BlipPTR_Mask);
                    settings.BlipPTR = addr3 + Mem.ReadInt(addr3 + 3, null) + 7;

                    // ReplayInterfacePTR
                    long addr4 = Mem.FindPattern(pattern.ReplayInterfacePTR, pattern.ReplayInterfacePTR_Mask);
                    settings.ReplayInterfacePTR = addr4 + Mem.ReadInt(addr4 + 3, null) + 7;

                    // LocalScriptsPTR
                    long addr5 = Mem.FindPattern(pattern.LocalScriptsPTR, pattern.LocalScriptsPTR_Mask);
                    settings.LocalScriptsPTR = addr5 + Mem.ReadInt(addr5 + 3, null) + 7;

                    // PlayerCountPTR
                    long addr6 = Mem.FindPattern(pattern.PlayerCountPTR, pattern.PlayerCountPTR_Mask);
                    settings.PlayerCountPTR = addr6 + Mem.ReadInt(addr6 + 3, null) + 7;

                    // PickupDataPTR
                    long addr7 = Mem.FindPattern(pattern.PickupDataPTR, pattern.PickupDataPTR_Mask);
                    settings.PickupDataPTR = addr7 + Mem.ReadInt(addr7 + 3, null) + 7;

                    // WeatherADDR
                    long addr8 = Mem.FindPattern(pattern.WeatherADDR, pattern.WeatherADDR_Mask);
                    settings.WeatherADDR = addr8 + Mem.ReadInt(addr8 + 6, null) + 10;

                    // SettingsPTR
                    long addr9 = Mem.FindPattern(pattern.SettingsPTR, pattern.SettingsPTR_Mask);
                    settings.SettingsPTR = addr9 + Mem.ReadInt(addr9 + 3, null) - Convert.ToInt64("0x89", 16);

                    // AimCPedPTR
                    long addr10 = Mem.FindPattern(pattern.AimCPedPTR, pattern.AimCPedPTR_Mask);
                    settings.AimCPedPTR = addr10 + Mem.ReadInt(addr10 + 3, null) + 7;

                    // FriendlistPTR
                    long addr11 = Mem.FindPattern(pattern.FriendlistPTR, pattern.FriendlistPTR_Mask);
                    settings.FriendlistPTR = addr11 + Mem.ReadInt(addr11 + 3, null) + 7;
                }
                else
                {
                    MessageBox.Show("Grand Theft Auto V is not Running.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Quit();
                }
            }
            catch
            {
                MessageBox.Show("Grand Theft Auto V is not Running.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Quit();
            }
        }

        /*public static void freezeGame()
        {
            Console.WriteLine("Freezing game");
            Speeder.Suspend(settings.gameProcess);
            Thread.Sleep(10000);
            Speeder.Resume(settings.gameProcess);
            _freezeGame.Abort();
        }*/
        #endregion


        #region TIMERS

        private static void ProcessTimer_Tick(object sender, EventArgs e)
        {

        }

        private static void MemoryTimer_Tick(object sender, EventArgs e)
        {
            pGODMODE();
        }

        private static void fastTimer_Tick(object sender, EventArgs e)
        {

        }

        #endregion

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

                    listboxStyle();
                    listboxFill(0, 0);
                    fastTimer.Enabled = true;
                    ProcessTimer.Enabled = true;
                    MemoryTimer.Enabled = true;
                    listBx.Enabled = true;

                    Application.Run();

                }
        }

        public static void createMainForm()
        {
            // 
            // listBx
            // 
            listBx.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(128)))));
            listBx.BorderStyle = System.Windows.Forms.BorderStyle.None;
            listBx.Font = new System.Drawing.Font("Tahoma", 13.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
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
            label1.Font = new System.Drawing.Font("Tahoma", 18F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            label1.Location = new System.Drawing.Point(1, 9);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(168, 33);
            label1.TabIndex = 1;
            label1.Text = "GTAVCSMM";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new System.Drawing.Font("Arial", 15.75F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            label2.Location = new System.Drawing.Point(162, 16);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(65, 24);
            label2.TabIndex = 2;
            label2.Text = "o1.58";
            // 
            // fastTimer
            // 
            fastTimer.Interval = 1;
            fastTimer.Tick += new System.EventHandler(fastTimer_Tick);
            // 
            // ProcessTimer
            // 
            ProcessTimer.Interval = 500;
            ProcessTimer.Tick += new System.EventHandler(ProcessTimer_Tick);
            // 
            // MemoryTimer
            // 
            MemoryTimer.Interval = 500;
            MemoryTimer.Tick += new System.EventHandler(MemoryTimer_Tick);
            // 
            // Form1
            // 
            mainForm.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            mainForm.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            mainForm.AutoSize = true;
            mainForm.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            mainForm.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(128)))));
            mainForm.ClientSize = new System.Drawing.Size(207, 116);
            mainForm.Controls.Add(label2);
            mainForm.Controls.Add(label1);
            mainForm.Controls.Add(listBx);
            mainForm.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            mainForm.KeyPreview = true;
            mainForm.Name = "Form1";
            mainForm.Opacity = 0.8D;
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

        public static void listboxStyle()
        {
        }

        public static void listboxFill(int mainMenulevel, int menulevel)
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
                    listBx.Items.Add("Weapon \t\t\t ►");     // 0,4
                    listBx.Items.Add("Teleport \t\t\t ►");   // 0,5
                    listBx.Items.Add("Tunables \t\t\t ►");   // 0,6
                    listBx.Items.Add("Online Services \t\t ►");   // 0,7

                    menuMainLvl = 0;
                    menuLvl = 0;

                    LastMenuMainLvl = 0;
                    LastMenuLvl = 0;
                    LastMenuItm = 0;
                    break;

                case 1:
                    switch (menulevel)
                    {
                        case 0:
                            listBx.Items.Add("Refresh");
                            listBx.Items.Add("Exit");

                            menuMainLvl = 1;
                            menuLvl = 0;

                            LastMenuMainLvl = 0;
                            LastMenuLvl = 1;
                            LastMenuItm = 0;
                            break;

                        case 1:
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

                        case 2:
                            listBx.Items.Add("God Mode (F6)");

                            menuMainLvl = 1;
                            menuLvl = 2;

                            LastMenuMainLvl = 0;
                            LastMenuLvl = 1;
                            LastMenuItm = 2;
                            break;

                        case 3:
                            listBx.Items.Add("God Mode");

                            menuMainLvl = 1;
                            menuLvl = 3;

                            LastMenuMainLvl = 0;
                            LastMenuLvl = 1;
                            LastMenuItm = 3;
                            break;

                        case 4:
                            listBx.Items.Add("To Waypoint");
                            listBx.Items.Add("To Objective");
                            listBx.Items.Add("Locations \t\t ►");

                            menuMainLvl = 1;
                            menuLvl = 4;

                            LastMenuMainLvl = 0;
                            LastMenuLvl = 1;
                            LastMenuItm = 4;
                            break;

                        case 5:
                            listBx.Items.Add("Quick Car Spawn \t\t ►");
                            listBx.Items.Add("Manual Car Spawn \t\t ►");

                            menuMainLvl = 1;
                            menuLvl = 5;

                            LastMenuMainLvl = 0;
                            LastMenuLvl = 1;
                            LastMenuItm = 5;
                            break;
                    }
                    break;

                case 2:
                    switch (menulevel)
                    {
                        case 7:
                            listBx.Items.Add("Wanted Level = 0");
                            listBx.Items.Add("Wanted Level = 1");
                            listBx.Items.Add("Wanted Level = 2");
                            listBx.Items.Add("Wanted Level = 3");
                            listBx.Items.Add("Wanted Level = 4");
                            listBx.Items.Add("Wanted Level = 5");

                            menuMainLvl = 2;
                            menuLvl = 11;

                            LastMenuMainLvl = 1;
                            LastMenuLvl = 2;
                            LastMenuItm = 11;
                            break;
                    }
                    break;

                case 5:
                    switch (menulevel)
                    {
                        case 2:
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

                            menuMainLvl = 5;
                            menuLvl = 1;

                            LastMenuMainLvl = 1;
                            LastMenuLvl = 5;
                            LastMenuItm = 2;
                            break;
                    }
                    break;

                case 6:
                    switch (menulevel)
                    {
                        case 0:
                            listBx.Items.Add("ZR380");
                            listBx.Items.Add("Deluxo");
                            listBx.Items.Add("Opressor2");
                            listBx.Items.Add("Vigilante");
                            listBx.Items.Add("Toreador");
                            listBx.Items.Add("Future Brutus");
                            listBx.Items.Add("Future Dominator");
                            listBx.Items.Add("Future Imperator");

                            menuMainLvl = 7;
                            menuLvl = 2;

                            LastMenuMainLvl = 1;
                            LastMenuLvl = 7;
                            LastMenuItm = 2;
                            break;
                    }
                    break;
            }
            listBx.SelectedIndex = 0;
            mainForm.TopMost = true;
        }

        public static void runitem(int mainMenulevel, int menulevel, int menuItem)
        {
            int[] tpIdArray;// Add tpIdArray
            int[] tpColArray;// Add tpColArray
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
                                    listboxFill(1, 0);
                                    break;
                                case 1:
                                    listboxFill(1, 1);
                                    break;
                                case 2:
                                    listboxFill(1, 2);
                                    break;
                                case 3:
                                    listboxFill(1, 3);
                                    break;
                                case 4:
                                    listboxFill(1, 4);
                                    break;
                                case 5:
                                    listboxFill(1, 5);
                                    break;
                                case 6:
                                    listboxFill(1, 6);
                                    break;
                                case 7:
                                    listboxFill(1, 7);
                                    break;
                            }
                            break;
                    }
                    break;

                case 1:
                    switch (menulevel)
                    {
                        case 0:
                            switch (menuItem)
                            {
                                case 0:
                                    // Re-Init
                                    Console.WriteLine("Nothing to do");
                                    break;
                                case 1:
                                    Quit();
                                    break;
                            }
                            break;
                        case 1:
                            switch (menuItem)
                            {
                                case 0:
                                    Activate();
                                    LoadSession(0);
                                    break;
                                case 1:
                                    Activate();
                                    LoadSession(1);
                                    break;
                                case 2:
                                    Activate();
                                    LoadSession(10);
                                    break;
                                case 3:
                                    Activate();
                                    LoadSession(-1);
                                    break;
                                case 4:
                                    Activate();
                                    empty_session();
                                    break;
                                case 5:
                                    Activate();
                                    LoadSession(11);
                                    break;
                                case 6:
                                    Activate();
                                    LoadSession(9);
                                    break;
                                case 7:
                                    Activate();
                                    LoadSession(6);
                                    break;
                                case 8:
                                    Activate();
                                    LoadSession(3);
                                    break;
                                case 9:
                                    Activate();
                                    LoadSession(12);
                                    break;
                                case 10:
                                    Activate();
                                    LoadSession(2);
                                    break;
                        case 2:
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
				break;
			}
		}

        public static void runSingleItem()
        {
            Console.WriteLine("Command to run backward: " + LastMenuMainLvl + " " + LastMenuLvl + " " + LastMenuItm);
            int oldMenuItm = LastMenuItm;
            listboxFill(LastMenuMainLvl, LastMenuLvl);
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
                if ((Keys)vkCode == Keys.F5)
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
                else if ((Keys)vkCode == Keys.NumPad5)
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
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        public static void LoadSession(int id)
        {
            Task.Run(() =>
            {
                _SG_Int(1575015, id);
                _SG_Int(1574589 + 2, id == -1 ? -1 : 0);
                _SG_Int(1574589, 1);
            });
        }


        #Region Teleport part
        private static void Teleport(Location l)
        {
            if (Mem.ReadInt(settings.WorldPTR, new int[] { offsets.pCPed, offsets.oInVehicle }) == 0)
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
                long blip = settings.BlipPTR + (i * 8);
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
                tempLocation.z = -255.0f;
            } else
            {
                tempLocation.z = tempLocation.z + zOffset;
            }

            Console.WriteLine("New location: " + tempLocation.x + ", " + tempLocation.y + ", " + tempLocation.z);
            return new Location { x = tempLocation.x, y = tempLocation.y, z = tempLocation.z };
        }

        public static float PlayerX
        {
            get { return Mem.ReadFloat(settings.WorldPTR, new int[] { offsets.pCPed, offsets.pCNavigation, offsets.oPositionX }); }
            set
            {
                Mem.Write(settings.WorldPTR, new int[] { offsets.pCPed, offsets.pCNavigation, offsets.oPositionX }, value);
                Mem.Write(settings.WorldPTR, new int[] { offsets.pCPed, offsets.oVisualX }, value);
            }
        }
        public static float PlayerY
        {
            get { return Mem.ReadFloat(settings.WorldPTR, new int[] { offsets.pCPed, offsets.pCNavigation, offsets.oPositionY }); }
            set
            {
                Mem.Write(settings.WorldPTR, new int[] { offsets.pCPed, offsets.pCNavigation, offsets.oPositionY }, value);
                Mem.Write(settings.WorldPTR, new int[] { offsets.pCPed, offsets.oVisualY }, value);
            }
        }
        public static float PlayerZ
        {
            get { return Mem.ReadFloat(settings.WorldPTR, new int[] { offsets.pCPed, offsets.pCNavigation, offsets.oPositionZ }); }
            set
            {
                Mem.Write(settings.WorldPTR, new int[] { offsets.pCPed, offsets.pCNavigation, offsets.oPositionZ }, value);
                Mem.Write(settings.WorldPTR, new int[] { offsets.pCPed, offsets.oVisualZ }, value);
            }
        }

        public static float CarX
        {
            get { return Mem.ReadFloat(settings.WorldPTR, new int[] { offsets.pCPed, offsets.pCVehicle, offsets.pCNavigation, offsets.oPositionX }); }
            set
            {
                Mem.Write(settings.WorldPTR, new int[] { offsets.pCPed, offsets.pCVehicle, offsets.pCNavigation, offsets.oPositionX }, value);
                Mem.Write(settings.WorldPTR, new int[] { offsets.pCPed, offsets.pCVehicle, offsets.oVisualX }, value);
            }
        }
        public static float CarY
        {
            get { return Mem.ReadFloat(settings.WorldPTR, new int[] { offsets.pCPed, offsets.pCVehicle, offsets.pCNavigation, offsets.oPositionY }); }
            set
            {
                Mem.Write(settings.WorldPTR, new int[] { offsets.pCPed, offsets.pCVehicle, offsets.pCNavigation, offsets.oPositionY }, value);
                Mem.Write(settings.WorldPTR, new int[] { offsets.pCPed, offsets.pCVehicle, offsets.oVisualY }, value);
            }
        }
        public static float CarZ
        {
            get { return Mem.ReadFloat(settings.WorldPTR, new int[] { offsets.pCPed, offsets.pCVehicle, offsets.pCNavigation, offsets.oPositionZ }); }
            set
            {
                Mem.Write(settings.WorldPTR, new int[] { offsets.pCPed, offsets.pCVehicle, offsets.pCNavigation, offsets.oPositionZ }, value);
                Mem.Write(settings.WorldPTR, new int[] { offsets.pCPed, offsets.pCVehicle, offsets.oVisualZ }, value);
            }
        }
        #EndRegion

        #region Global Addresses function
        public static T GG<T>(int index) where T : struct { return Mem.Read<T>(GA(index)); }

        public static void SG<T>(int index, T vaule) where T : struct { Mem.Write<T>(GA(index), vaule); }
        
        public static long GA(int Index)
        {
            long p = settings.GlobalPTR + (8 * (Index >> 0x12 & 0x3F));
            long p_ga = Mem.ReadPointer(p, null);
            long p_ga_final = p_ga + (8 * (Index & 0x3FFFF));
            return p_ga_final;
        }

        public static int _GG_Int(int Index)
        {
            return Mem.ReadInt(GA(Index), null);
        }
        public static float _GG_Float(int Index)
        {
            return Mem.ReadFloat(GA(Index), null);
        }
        public static string _GG_String(int Index, int size)
        {
            return Mem.ReadString(GA(Index), null, size);
        }
        public static void _SG_Int(int Index, int value)
        {
            Mem.writeInt(GA(Index), null, value);
        }
        public static void _SG_UInt(int Index, uint value)
        {
            Mem.writeUInt(GA(Index), null, value);
        }
        public static void _SG_Float(int Index, float value)
        {
            Mem.writeFloat(GA(Index), null, value);
        }
        public static void _SG_String(int Index, string value)
        {
            Mem.Write(GA(Index), null, value);
        }

        #endregion
        public static long GetLocalScript(string name)
        {
            int size = name.Length;
            for (int i = 0; i <= 52; i++)
            {
                long lc_p = Mem.ReadPointer(settings.LocalScriptsPTR, new int[] { (i * 8), 0xB0 });
                string lc_n = Mem.ReadString(settings.LocalScriptsPTR, new int[] { (i * 8), 0xD0 }, size);
                if (lc_n == name)
                {
                    i = 53;
                    Console.WriteLine(lc_p);
                    return (lc_p);
                }
            }
            return 0;
        }
		// Create Vehicle Function
        public static void carSpawn(string Hash, int pegasus = 0)
        {
            string model = Hash.ToLower();
            float ped_heading = Mem.ReadFloat(settings.WorldPTR, new int[] { offsets.pCPed, offsets.pCNavigation, offsets.oHeading });
            float ped_heading2 = Mem.ReadFloat(settings.WorldPTR, new int[] { offsets.pCPed, offsets.pCNavigation, offsets.oHeading2 });
            Console.WriteLine(ped_heading + " " + ped_heading2);
            float spawner_x = PlayerX;
            float spawner_y = PlayerY;
            float spawner_z = PlayerZ;
            spawner_x = spawner_x - (ped_heading2 * 5f);
            spawner_y = spawner_y + (ped_heading * 5f);
            spawner_z = spawner_z + 0.5f;
			
            _SG_Float(offsets.oVMCreate + 7 + 0, spawner_x);
            _SG_Float(offsets.oVMCreate + 7 + 1, spawner_y);
            _SG_Float(offsets.oVMCreate + 7 + 2, spawner_z);
            _SG_Int(offsets.oVMCreate + 27 + 66, (int)JOAAT.GetHashKey(Hash));
            _SG_Int(offsets.oVMCreate + 27 + 28, 1); // Weaponised ownerflag
            _SG_Int(offsets.oVMCreate + 27 + 60, 1);
            _SG_Int(offsets.oVMCreate + 27 + 95, 14); // Ownerflag
            _SG_Int(offsets.oVMCreate + 27 + 94, 2); // Personal car ownerflag
            _SG_Int(offsets.oVMCreate + 5, 1); // SET('i', CarSpawn + 0x1168, 1)--can spawn flag must be odd
            _SG_Int(offsets.oVMCreate + 2, 1); // SET('i', CarSpawn + 0x1180, 1)--spawn toggle gets reset to 0 on car spawn
            _SG_Int(offsets.oVMCreate + 3, pegasus);
            _SG_Int(offsets.oVMCreate + 27 + 74, 1); // Red Neon Amount 1-255 100%-0%
            _SG_Int(offsets.oVMCreate + 27 + 75, 1); // Green Neon Amount 1-255 100%-0%
            _SG_Int(offsets.oVMCreate + 27 + 76, 0); // Blue Neon Amount 1-255 100%-0%
            _SG_UInt(offsets.oVMCreate + 27 + 60, 1); // landinggear 
			_SG_Int(offsets.oVMCreate + 27 + 77, 4030726305)// vehstate
            _SG_Int(offsets.oVMCreate + 27 + 5, -1); // default paintjob primary -1 auto 120
            _SG_Int(offsets.oVMCreate + 27 + 6, -1); // default paintjob secondary -1 auto 120
            _SG_Int(offsets.oVMCreate + 27 + 7, -1);
            _SG_Int(offsets.oVMCreate + 27 + 8, -1);
            _SG_Int(offsets.oVMCreate + 27 + 19, 4);
            _SG_Int(offsets.oVMCreate + 27 + 21, 4); // Engine(0 - 3)
            _SG_Int(offsets.oVMCreate + 27 + 22, 3);
            _SG_Int(offsets.oVMCreate + 27 + 23, 3); // Transmission(0 - 9)
            _SG_Int(offsets.oVMCreate + 27 + 24, 58);
            _SG_Int(offsets.oVMCreate + 27 + 26, 5); // Armor(0 - 18)
            _SG_Int(offsets.oVMCreate + 27 + 27, 1); // Turbo(0 - 1)
            _SG_Int(offsets.oVMCreate + 27 + 65, 2); // Window tint 0 - 6
            _SG_Int(offsets.oVMCreate + 27 + 69, -1); // Wheel type
            _SG_Int(offsets.oVMCreate + 27 + 33, -1); // Wheel Selection
            _SG_Int(offsets.oVMCreate + 27 + 25, 8); // Suspension(0 - 13)
            _SG_Int(offsets.oVMCreate + 27 + 19, -1);
            Mem.writeInt(GA(offsets.oVMCreate + 27 + 77) + 1, null, 2); // 2:bulletproof 0:false
			
            int whichWpn = 2;
            int whichWpn2 = 2;


            if (model == "vigilante" || model == "oppressor")
            {
				whichWpn = 1;
                whichWpn2 = 1;
            }
            if (model == "apc" || model == "deluxo")
            {
				whichWpn = 1;
                whichWpn2 = 1;
            }
            if (model == "bombushka")
            {
				whichWpn = 1;
                whichWpn2 = 1;
            }
            if (model == "tampa3" || model == "insurgent3" || model == "halftrack" )
            {
				whichWpn = 3;
                whichWpn2 = 3;
            }
            if (model == "barrage")
            {
				whichWpn = 30;
                whichWpn2 = 30;
            }
            _SG_Int(offsets.oVMCreate + 27 + 15, whichWpn); // primary weapon
            _SG_Int(offsets.oVMCreate + 27 + 20, whichWpn2); // primary weapon
            // _SG_Int(offsets.oVMCreate + 27 + 1, "FCK4FD"); // License plate
            _SG_Int(offsets.oVMCreate + 27 + 19, -1);
            _SG_Int(offsets.oVMCreate + 27 + 21, 3);  //-- Engine (0-3)
            _SG_Int(offsets.oVMCreate + 27 + 22, 3);
            _SG_Int(offsets.oVMCreate + 27 + 23, 9);
            _SG_Int(offsets.oVMCreate + 27 + 25, 8); //-- suspension (0-13)
            _SG_Int(offsets.oVMCreate + 27 + 24, 58);
            _SG_Int(offsets.oVMCreate + 27 + 69, -1); //-- Wheel type
            _SG_Int(offsets.oVMCreate + 27 + 33, -1); //-- Wheel Selection
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

        public static void empty_session()
        {
            Task.Run(() =>
            {
                ProcessMgr.SuspendProcess(settings.gameProcess);
                Task.Delay(10000).Wait();
                ProcessMgr.ResumeProcess(settings.gameProcess);
            });
        }
    }

    struct Location { public float x, y, z; }
}
