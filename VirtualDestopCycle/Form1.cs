using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Input;
using WindowsDesktop;
using GlobalHotKey;

namespace VirtualDesktopManager
{
    public partial class Form1 : Form
    {
        [DllImport("user32.dll", ExactSpelling = true)]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        private IList<VirtualDesktop> desktops;
        private IntPtr[] activePrograms;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        extern static bool DestroyIcon(IntPtr handle);

        private readonly HotKeyManager _rightHotkey;
        private readonly HotKeyManager _leftHotkey;
        private readonly HotKeyManager _numberHotkey;

        private bool closeToTray;
        private readonly IConfiguration configuration;
        
        public Form1(IConfiguration configuration)
        {
            this.configuration = configuration;
            InitializeComponent();

            handleChangedNumber();

            closeToTray = true;

            _rightHotkey = new HotKeyManager();
            _rightHotkey.KeyPressed += RightKeyManagerPressed;

            _leftHotkey = new HotKeyManager();
            _leftHotkey.KeyPressed += LeftKeyManagerPressed;

            _numberHotkey = new HotKeyManager();
            _numberHotkey.KeyPressed += NumberHotkeyPressed;

            VirtualDesktop.CurrentChanged += VirtualDesktop_CurrentChanged;
            VirtualDesktop.Created += VirtualDesktop_Added;
            VirtualDesktop.Destroyed += VirtualDesktop_Destroyed;

            this.FormClosing += Form1_FormClosing;

            checkBoxAlternateCombination.Checked = Properties.Settings.Default.AltHotKey;

            listViewFiles.Items.Clear();
            listViewFiles.Columns.Add("File").Width = 400;
            foreach (var file in Properties.Settings.Default.DesktopBackgroundFiles)
            {
                listViewFiles.Items.Add(NewListViewItem(file));
            }
        }

        private void NumberHotkeyPressed(object sender, KeyPressedEventArgs e)
        {
            var index = (int)e.HotKey.Key - (int)Key.D0 - 1;
            if (index != CurrentDesktopIndex && index < desktops.Count)
            {
                desktops.ElementAt(index)?.Switch();
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (closeToTray)
            {
                e.Cancel = true;
                this.Visible = false;
                this.ShowInTaskbar = false;
                notifyIcon1.BalloonTipTitle = "Still Running...";
                notifyIcon1.BalloonTipText = "Right-click on the tray icon to exit.";
                notifyIcon1.ShowBalloonTip(2000);
            }
        }

        private void handleChangedNumber()
        {
            desktops = VirtualDesktop.GetDesktops();
            activePrograms = new IntPtr[desktops.Count];
        }

        private void VirtualDesktop_Added(object sender, VirtualDesktop e)
        {
            handleChangedNumber();
        }

        private void VirtualDesktop_Destroyed(object sender, VirtualDesktopDestroyEventArgs e)
        {
            handleChangedNumber();
        }

        private void VirtualDesktop_CurrentChanged(object sender, VirtualDesktopChangedEventArgs e)
        {
            RefreshCurrentDesktop();
        }
        
        private void RefreshCurrentDesktop()
        {
            // 0 == first
            int currentDesktopIndex = CurrentDesktopIndex;

            string pictureFile = PickNthFile(currentDesktopIndex);
            if (pictureFile != null)
            {
                Native.SetBackground(pictureFile);
            }

            restoreApplicationFocus(currentDesktopIndex);
            changeTrayIcon(currentDesktopIndex);
        }

        private string PickNthFile(int currentDesktopIndex)
        {
            int n = Properties.Settings.Default.DesktopBackgroundFiles.Count;
            if (n == 0)
                return null;
            int index = currentDesktopIndex % n;
            var filename = Properties.Settings.Default.DesktopBackgroundFiles[index];
            RefreshCurrentDesktop();
            return filename;
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _rightHotkey.Dispose();
            _leftHotkey.Dispose();
            _numberHotkey.Dispose();

            closeToTray = false;

            this.Close();
        }

        private void normalHotkeys()
        {
            try
            {
                _rightHotkey.Register(Key.Right, System.Windows.Input.ModifierKeys.Control | System.Windows.Input.ModifierKeys.Alt);
                _leftHotkey.Register(Key.Left, System.Windows.Input.ModifierKeys.Control | System.Windows.Input.ModifierKeys.Alt);
                RegisterNumberHotkeys(System.Windows.Input.ModifierKeys.Control | System.Windows.Input.ModifierKeys.Alt);
            }
            catch (Exception err)
            {
                notifyIcon1.BalloonTipTitle = "Error setting hotkeys";
                notifyIcon1.BalloonTipText = "Could not set hotkeys. Please open settings and try the alternate combination.";
                notifyIcon1.ShowBalloonTip(2000);
            }
        }

        private void alternateHotkeys()
        {
            try
            {
                _rightHotkey.Register(Key.Right, System.Windows.Input.ModifierKeys.Shift | System.Windows.Input.ModifierKeys.Alt);
                _leftHotkey.Register(Key.Left, System.Windows.Input.ModifierKeys.Shift | System.Windows.Input.ModifierKeys.Alt);
                RegisterNumberHotkeys(System.Windows.Input.ModifierKeys.Shift | System.Windows.Input.ModifierKeys.Alt);
            }
            catch (Exception err)
            {
                notifyIcon1.BalloonTipTitle = "Error setting hotkeys";
                notifyIcon1.BalloonTipText = "Could not set hotkeys. Please open settings and try the default combination.";
                notifyIcon1.ShowBalloonTip(2000);
            }
        }

        private void RegisterNumberHotkeys(ModifierKeys modifiers)
        {
            _numberHotkey.Register(Key.D1, modifiers);
            _numberHotkey.Register(Key.D2, modifiers);
            _numberHotkey.Register(Key.D3, modifiers);
            _numberHotkey.Register(Key.D4, modifiers);
            _numberHotkey.Register(Key.D5, modifiers);
            _numberHotkey.Register(Key.D6, modifiers);
            _numberHotkey.Register(Key.D7, modifiers);
            _numberHotkey.Register(Key.D8, modifiers);
            _numberHotkey.Register(Key.D9, modifiers);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            labelStatus.Text = "";

            RegisterHotKeys(checkBoxAlternateCombination.Checked);
            var desktop = initialDesktopState();
            changeTrayIcon();

            this.Visible = false;
        }

        private void RegisterHotKeys(bool condition)
        {
            if (condition)
            {
                normalHotkeys();
            }
            else
            {
                alternateHotkeys();
            }
        }

        private int CurrentDesktopIndex => desktops.IndexOf(VirtualDesktop.Current);

        private void saveApplicationFocus(int currentDesktopIndex = -1)
        {
            IntPtr activeAppWindow = GetForegroundWindow();

            if (currentDesktopIndex == -1)
                currentDesktopIndex = CurrentDesktopIndex;

            activePrograms[currentDesktopIndex] = activeAppWindow;
        }

        private void restoreApplicationFocus(int currentDesktopIndex = -1)
        {
            if (currentDesktopIndex == -1)
                currentDesktopIndex = CurrentDesktopIndex;

            if (activePrograms[currentDesktopIndex] != null && activePrograms[currentDesktopIndex] != IntPtr.Zero)
            {
                SetForegroundWindow(activePrograms[currentDesktopIndex]);
            }
        }

        private void changeTrayIcon(int currentDesktopIndex = -1)
        {
            if (currentDesktopIndex == -1)
            {
                currentDesktopIndex = CurrentDesktopIndex;
            }
            Bitmap newIcon = new Bitmap(256, 256);
            var trayIcon = configuration.TrayIcon(currentDesktopIndex);
            Font desktopNumberFont = new Font(
                trayIcon.FontName,
                trayIcon.FontSize,
                trayIcon.FontStyle,
                trayIcon.FontGraphicsUnit
            );
            // -- https://stackoverflow.com/questions/2991490/bad-text-rendering-using-drawstring-on-top-of-transparent-pixels
            var gr = Graphics.FromImage(newIcon);
            gr.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            gr.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;
            gr.DrawString(
                trayIcon.Format(currentDesktopIndex + 1),
                desktopNumberFont,
                // FindFont(
                //     gr,
                //     trayIcon.Format(currentDesktopIndex + 1),
                //     newIcon.Size,
                //     desktopNumberFont,
                //     trayIcon.FontGraphicsUnit
                // ),
                trayIcon.FontColor,
                trayIcon.OffsetX,
                trayIcon.OffsetY
            );
            Icon numberedIcon = Icon.FromHandle(newIcon.GetHicon());
            notifyIcon1.Icon = numberedIcon;

            DestroyIcon(numberedIcon.Handle);
            desktopNumberFont.Dispose();
            newIcon.Dispose();
            gr.Dispose();
        }

        /// <summary>
        /// calculates fontScale based on size (~it works, but it looks weird...)
        /// </summary>
        private Font FindFont(System.Drawing.Graphics g, string longString, Size size, Font font, GraphicsUnit unit)
        {
            SizeF realSize = g.MeasureString(longString, font);
            float heightScaleRatio = size.Height / realSize.Height;
            float widthScaleRatio = size.Width / realSize.Width;
            float scaleRatio = (heightScaleRatio < widthScaleRatio
                ? heightScaleRatio
                : widthScaleRatio
            );
            float scaleFontSize = font.Size * scaleRatio;
            return new Font(font.FontFamily, scaleFontSize + 0, font.Style, unit);
        }
        
        VirtualDesktop initialDesktopState()
        {
            var desktop = VirtualDesktop.Current;
            int desktopIndex = CurrentDesktopIndex;

            saveApplicationFocus(desktopIndex);

            return desktop;
        }

        void RightKeyManagerPressed(object sender, KeyPressedEventArgs e)
        {
            desktops[(CurrentDesktopIndex + 1) % desktops.Count].Switch();
        }

        void LeftKeyManagerPressed(object sender, KeyPressedEventArgs e)
        {
            desktops[(CurrentDesktopIndex - 1 + desktops.Count) % desktops.Count].Switch();
        }

        private void openSettings(object sender = null, EventArgs e = null)
        {
            this.Visible = true;
            this.WindowState = FormWindowState.Normal;
            this.ShowInTaskbar = true;
        }

        private void upButton_Click(object sender, EventArgs e)
        {
            try
            {
                int total = listViewFiles.Items.Count;
                foreach (ListViewItem selected in listViewFiles.SelectedItems)
                {
                    int index = selected.Index;
                    listViewFiles.Items.RemoveAt(index);
                    listViewFiles.Items.Insert((index - 1 + total) % total, selected);
                }
            }
            catch (Exception ex)
            {

            }
        }

        private void downButton_Click(object sender, EventArgs e)
        {
            try
            {
                int total = listViewFiles.Items.Count;
                ListViewItem[] items = new ListViewItem[listViewFiles.SelectedIndices.Count];
                listViewFiles.SelectedItems.CopyTo(items, 0);
                foreach (ListViewItem selected in items.Reverse())
                {
                    int index = selected.Index;
                    listViewFiles.Items.RemoveAt(index);
                    listViewFiles.Items.Insert((index + 1) % total, selected);
                }
            }
            catch (Exception ex)
            {
            }
        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            _rightHotkey.Unregister(Key.Right, System.Windows.Input.ModifierKeys.Control | System.Windows.Input.ModifierKeys.Alt);
            _leftHotkey.Unregister(Key.Left, System.Windows.Input.ModifierKeys.Control | System.Windows.Input.ModifierKeys.Alt);
            _rightHotkey.Unregister(Key.Right, System.Windows.Input.ModifierKeys.Shift | System.Windows.Input.ModifierKeys.Alt);
            _leftHotkey.Unregister(Key.Left, System.Windows.Input.ModifierKeys.Shift | System.Windows.Input.ModifierKeys.Alt);

            Properties.Settings.Default.AltHotKey = checkBoxAlternateCombination.Checked;
            RegisterHotKeys(checkBoxAlternateCombination.Checked);

            Properties.Settings.Default.DesktopBackgroundFiles.Clear();
            foreach (ListViewItem item in listViewFiles.Items)
            {
                Properties.Settings.Default.DesktopBackgroundFiles.Add(item.Tag.ToString());
            }

            Properties.Settings.Default.Save();
            labelStatus.Text = "Changes were successful.";
        }

        private void addFileButton_Click(object sender, EventArgs e)
        {
            openFileDialog1.CheckFileExists = true;
            openFileDialog1.CheckPathExists = true;
            openFileDialog1.Filter = "Image Files(*.BMP;*.JPG;*.GIF)|*.BMP;*.JPG;*.GIF|All files (*.*)|*.*";
            openFileDialog1.FilterIndex = 0;
            openFileDialog1.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            openFileDialog1.Multiselect = true;
            openFileDialog1.Title = "Select desktop background image";
            if (openFileDialog1.ShowDialog(this) == DialogResult.OK)
            {
                foreach (string file in openFileDialog1.FileNames)
                {
                    listViewFiles.Items.Add(NewListViewItem(file));
                }
            }
        }

        private static ListViewItem NewListViewItem(string file)
        {
            return new ListViewItem()
            {
                Text = Path.GetFileName(file),
                ToolTipText = file,
                Name = Path.GetFileName(file),
                Tag = file
            };
        }

        private void removeButton_Click(object sender, EventArgs e)
        {
            try
            {
                foreach (int index in listViewFiles.SelectedIndices)
                {
                    listViewFiles.Items.RemoveAt(index);
                }
            }
            catch (Exception ex)
            {
            }
        }
    }
}
