using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using WixToolset.Dtf.WindowsInstaller;

namespace CustomActions
{
    public class PromptCloseApplication : IDisposable
    {
        private readonly string _productName;
        private readonly string _processName;
        private readonly string _displayName;
        private System.Threading.Timer _timer;
        private Form _form;
        private IntPtr _mainWindowHandle;

        [DllImport("user32.dll")]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        public PromptCloseApplication(Session session, string productName, string processName, string displayName)
        {
            session.Log("PromptCloseApplication: {0} {1} {2}", productName, processName, displayName);
            _productName = productName;
            _processName = processName;
            _displayName = displayName;
        }

        public bool Prompt()
        {
            if (IsRunning(_processName))
            {
                System.Threading.Thread.Sleep(5000);
            }
            if (IsRunning(_processName))
            {
                _form = new ClosePromptForm(String.Format("Please close running instances of {0} before running this update.  " +
                "This dialog will close automatically after {0} has been closed.", _displayName));
                _mainWindowHandle = FindWindow(null, _productName + " Setup");
                if (_mainWindowHandle == IntPtr.Zero)
                    _mainWindowHandle = FindWindow("#32770", _productName);

                _timer = new System.Threading.Timer(TimerElapsed, _form, 200, 200);

                return ShowDialog();
            }
            return true;
        }

        bool ShowDialog()
        {
            if (_form.ShowDialog(new WindowWrapper(_mainWindowHandle)) == DialogResult.OK)
                return !IsRunning(_processName) || ShowDialog();
            return false;
        }

        private void TimerElapsed(object sender)
        {
            if (_form == null || IsRunning(_processName) || !_form.Visible)
            {
                if (_form != null)
                {
                    _form.TopMost = true;
                    _form.Activate();
                    _form.BringToFront();
                    _form.Focus();
                }
                return;
            }
            _form.DialogResult = DialogResult.OK;
            _form.Close();
        }

        static bool IsRunning(string processName)
        {
            return Process.GetProcessesByName(processName).Length > 0;
        }

        public void Dispose()
        {
            if (_timer != null)
                _timer.Dispose();
            if (_form != null && _form.Visible)
                _form.Close();
        }
    }
}