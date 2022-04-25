using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace LSS_MON
{
    internal static class Program
    {
        [DllImportAttribute("user32.dll", EntryPoint = "FindWindow")]
        public static extern int FindWindow(string clsName, string wndName);
        
        /// <summary>
        /// 해당 애플리케이션의 주 진입점입니다.
        /// </summary>
        [STAThread]
        static void Main()
        {
            if (!IsDuplicated())
            {
                RunApplication();
            }
        }
        private static bool IsDuplicated()
        {
            try
            {
                int processCount = 0;

                // 프로세스 이름으로 체크 하면 , exe 파일명을 바꾸면 소용이 없음.
                // 프로그램이 먼저 실행중이 아니면 0을 리턴
                processCount = FindWindow(null, "LSS_MON㈜");

                // 중복 프로세스 탐지함
                if (processCount > 0)
                {
                    MessageBox.Show("프로그램이 이미 실행중입니다.", "알림");
                    return true;
                }

                // 중복 프로세스 없음
                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Exception");
                return true;
            }
        }

        private static void RunApplication()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new LSS_MON());
        }
    }
}
