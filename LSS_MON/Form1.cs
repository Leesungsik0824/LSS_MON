using System;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Linq;

namespace LSS_MON
{
    public partial class LSS_MON : Form
    {
        public LSS_MON()
        {
            InitializeComponent();
            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;
            this.Visible = false;
            this.notifyIcon1.Visible = true;
            this.Opacity = 0;
        }
        const int HOTKEY_ID_MAX  = 5932; //내맘대로 키(내사번)
        const int HOTKEY_ID_NEXT = 5933; //내맘대로 키(내사번)

        [DllImport("user32.dll")]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, KeyModifiers fsModifiers, Keys vk);

        [DllImport("user32.dll")]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
           
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetWindowRect(IntPtr hWnd, ref RECT Rect);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int Width, int Height, bool Repaint);

        public enum KeyModifiers
        {
            None = 0,
            Alt = 1,
            Control = 2,
            Shift = 4,
            Windows = 8
        }

        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        const int WM_HOTKEY = 0x0312;

        protected override void WndProc(ref Message m)
        {
            Keys key = (Keys)(((int)m.LParam >> 16) & 0xFFFF);
            KeyModifiers modifier = (KeyModifiers)((int)m.LParam & 0xFFFF);

            switch (m.Msg)
            {
                case WM_HOTKEY:
                    if (Properties.Settings.Default.Next == key && KeyModifiers.Control == modifier)
                    {
                        MoveMon();
                    }
                    else if (Properties.Settings.Default.Max == key && KeyModifiers.Control == modifier)
                    {
                        MaxMon();
                    }
                    break;
            }
            base.WndProc(ref m);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            레지스터등록();
        }

        private void 레지스터등록()
        {
            RegisterHotKey(this.Handle, HOTKEY_ID_MAX, KeyModifiers.Control, Properties.Settings.Default.Max);
            RegisterHotKey(this.Handle, HOTKEY_ID_NEXT, KeyModifiers.Control, Properties.Settings.Default.Next);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            레지스터해제();
        }
        private void 레지스터해제()
        {
            UnregisterHotKey(this.Handle, HOTKEY_ID_MAX);
            UnregisterHotKey(this.Handle, HOTKEY_ID_NEXT);
        }

        private void 종료ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("종료하시겠습니까?", "종료", MessageBoxButtons.YesNo) == DialogResult.No)
            {
                return;
            }
            else
            {
                Application.Exit();                
            }
        }

        private void MoveMon()
        {
            IntPtr ptop = GetForegroundWindow();

            //모니터 정렬을 지원 안해준다. 정렬방법을 찾아야 함.
            //Screen[] sc = Screen.AllScreens;
            var sc = Screen.AllScreens.ToArray();
            //좌측으로 가로 순서 정렬 한번 , 세로 순서 정렬 한번
            sc = sc.OrderBy(s => s.WorkingArea.Left).OrderByDescending(s => s.WorkingArea.Top).ToArray();            

            int NowScIndex = 0;
            int NextScIndex = 0;
            int OldLeft = 0;
            int OldTop = 0;
            for (int i = 0; i < sc.Length; i++)
            {
                if (sc[i].DeviceName == Screen.FromHandle(ptop).DeviceName)
                {
                    NowScIndex = i;
                    OldLeft = sc[i].WorkingArea.Left;
                    OldTop = sc[i].WorkingArea.Top;
                }
            }

            if ((NowScIndex + 1) == sc.Length)
            {
                NextScIndex = 0;
            }
            else
            {
                NextScIndex = NowScIndex + 1;
            }

            RECT ProRect = new RECT();
            Rectangle currentDesktopRect = sc[NextScIndex].WorkingArea;

            if (GetWindowRect(ptop, ref ProRect))
            {
                //ProRect.right - ProRect.left ==> 실행프로그램의 width
                //ProRect.bottom - ProRect.top ==> 실행프로그램의 height
                //실행프로그램이 다음 모니터로 갔을때 left 위치를 모니터의 left 에 실행창의 left 를 더해준다.( 똑같은 자리로 옮기기 위함 )
                //NowLeft 는 옮겨질 모니터의 시작점 + ( 이전모니터의 프로그램 left - 이전 모니터의 left 를 해줘서 모니터에서 얼마나 떨어져 있는지를 계산
                //NowTop 도 left 와 마찬가지로 계산
                int iwidth = ProRect.right - ProRect.left;
                int iheight = ProRect.bottom - ProRect.top;
                int NowLeft = currentDesktopRect.Left + (ProRect.left - OldLeft);
                int NowTop = currentDesktopRect.Top + (ProRect.top - OldTop);
                MoveWindow(ptop, NowLeft, NowTop, iwidth, iheight, true);
            }
        }

        private void MaxMon()
        {
            //현재 실행중인 프로세스
            IntPtr ptop = GetForegroundWindow();
            RECT ProRect = new RECT();
            
            Rectangle currentDesktopRect = Screen.FromHandle(ptop).WorkingArea;
            //현재 실행중인 프로세스를 Rect 에 담아준다.
            if (GetWindowRect(ptop, ref ProRect))
            {
                //현재 모니터의 넓이와 프로그램 창의 넓이를 비교(최대창인지를 찾기 위해)
                if(Math.Abs((Math.Abs(currentDesktopRect.Right) - Math.Abs(currentDesktopRect.Left))) > Math.Abs((Math.Abs(ProRect.right) - Math.Abs(ProRect.left))))
                {
                    //최대화(3 번이 최대화 , 2번은 최소화 , 1번은 보통)
                    ShowWindow(ptop, 3);
                }
                else if (Math.Abs((Math.Abs(currentDesktopRect.Bottom) - Math.Abs(currentDesktopRect.Top))) > Math.Abs((Math.Abs(ProRect.bottom) - Math.Abs(ProRect.top))))
                {
                    //최대화(3 번이 최대화 , 2번은 최소화 , 1번은 보통)
                    ShowWindow(ptop, 3);
                }
                else
                {
                    //보통
                    ShowWindow(ptop, 1);
                }
            }           
        }

        private void 다음모니터로이동ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenForm("Next");
        }

        private void 창최대화ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenForm("Max");
        }

        private void OpenForm(string Mode)
        {
            Form form = new Form
            {
                Size = new Size(300, 100),
                MaximizeBox = false
            };

            TextBox textBox = new TextBox
            {
                ReadOnly = true,
                Width = 200,
                Dock = DockStyle.Fill,
                Name = "ShortCut"
            };

            if (Mode.Equals("Next"))
            {
                form.Text = "단축키 설정(Next)";
                textBox.Text = "Control + " + Properties.Settings.Default.Next.ToString();
                textBox.Tag = Properties.Settings.Default.Next;
            }
            else if (Mode.Equals("Max"))
            {
                form.Text = "단축키 설정(Max)";
                textBox.Text = "Control + " + Properties.Settings.Default.Max.ToString();
                textBox.Tag = Properties.Settings.Default.Max;
            }

            textBox.KeyUp += TextBox_KeyUp;

            Button button = new Button
            {
                Text = "저장",
                Width = 50,
                Dock = DockStyle.Fill
            };

            //버튼 클릭시 파라미터를 넘겨주기 위함.
            button.Click += (sender , e) => Button_Click(form , Mode , textBox);

            TableLayoutPanel table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Location = new Point(0, 0),
                ColumnCount = 1,
                RowCount = 2
            };

            table.Controls.Add(textBox);
            table.Controls.Add(button);
            table.SetRow(textBox, 0);
            table.SetRow(button, 1);

            form.Controls.Add(table);
            form.ShowDialog();
        }

        //private void Button_Click(object sender, EventArgs e)
        private void Button_Click(Form form , string Mode , TextBox text)
        {
            try
            {
                if (Mode.Equals("Next"))
                {
                    //이미 등록되어 있는 단축키로 덮어쓰기
                    if (Properties.Settings.Default.Max.Equals((Keys)text.Tag))
                    {
                        Properties.Settings.Default.Max = Keys.None;
                    }
                    Properties.Settings.Default.Next = (Keys)text.Tag;
                }
                else if (Mode.Equals("Max"))
                {
                    //이미 등록되어 있는 단축키로 덮어쓰기
                    if (Properties.Settings.Default.Next.Equals((Keys)text.Tag))
                    {
                        Properties.Settings.Default.Next = Keys.None;
                    }
                    Properties.Settings.Default.Max = (Keys)text.Tag;
                }
                Properties.Settings.Default.Save();
                
                //변경된 단축키로 셋팅
                레지스터해제();
                레지스터등록();
                MessageBox.Show("저장되었습니다.", "알림");
                form.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                MessageBox.Show(ex.StackTrace);
            }
        }

        private void TextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (ModifierKeys ==(Keys.Control))
            {
                string final = e.Modifiers + " + " + e.KeyCode;
                ((TextBox)sender).Tag = e.KeyCode;
                ((TextBox)sender).Text = final;
            }
        }
    }
}
