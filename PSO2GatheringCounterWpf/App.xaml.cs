using PSO2GatheringCounter;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;

namespace PSO2GatheringCounterWpf
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// ShowWindowAsync関数のパラメータに渡す定義値
        /// 画面を元の大きさに戻す
        /// </summary>
        private const int SW_RESTORE = 9;

        /// <summary>二重起動防止用Mutexの名前</summary>
        private const string MutexName = "PSO2GatheringCounter";

        /// <summary>二重起動防止用Mutex</summary>
        private Mutex? _mutex;
        private bool _ownerShip = false;

        /// <summary>
        /// 開始時処理
        /// Mutexを作成し、新規作成の場合のみ起動処理を行う。
        /// </summary>
        /// <param name="e"></param>
        protected override void OnStartup(StartupEventArgs e)
        {
            _mutex = new Mutex(_ownerShip, MutexName);
            try
            {
                _ownerShip = _mutex.WaitOne(0);
            }
            catch (AbandonedMutexException)
            {
                // 正しく開放されずに破棄されていたという通知なので無視
                _ownerShip = true;
            }
            catch
            {
                // NOP
            }
            // 多重起動抑止判定
            if (_ownerShip)
            {
                base.OnStartup(e);
            }
            else
            {
                // 二重起動時、最小化されていれば起こす
                WakeupPrevious();
                this.Shutdown();
            }
        }

        /// <summary>
        /// 同じアプリケーションが最小化されていれば起こす。
        /// </summary>
        private void WakeupPrevious()
        {
            Process previousProcess = GetPreviousProcess();
            if (previousProcess != null)
            {
                WakeupWindow(previousProcess.MainWindowHandle);
            }
        }

        /// <summary>
        /// 実行中の同じアプリケーションのプロセスを取得する。
        /// </summary>
        /// <returns>実行中の同じアプリケーションのプロセス</returns>
        private Process GetPreviousProcess()
        {
            Process curProcess = Process.GetCurrentProcess();
            Process[] allProcesses = Process.GetProcessesByName(curProcess.ProcessName);

            foreach (Process checkProcess in allProcesses)
            {
                // 自分自身のプロセスIDは無視する
                if (checkProcess.Id != curProcess.Id)
                {
                    // プロセスのフルパス名を比較して同じアプリケーションか検証
                    if (String.Compare(checkProcess.MainModule.FileName, curProcess.MainModule.FileName, true) == 0)
                    {
                        // 同じフルパス名のプロセスを取得
                        return checkProcess;
                    }
                }
            }
            // 同じアプリケーションのプロセスが見つからない！
            return null;
        }

        /// <summary>
        /// 外部プロセスのウィンドウを起動する。
        /// </summary>
        /// <param name="hWnd">ウィンドウハンドル</param>
        private void WakeupWindow(IntPtr hWnd)
        {
            // メインウィンドウが最小化されていれば元に戻す
            if (IsIconic(hWnd))
            {
                ShowWindowAsync(hWnd, SW_RESTORE);
            }

            // メインウィンドウを最前面に表示する
            SetForegroundWindow(hWnd);
        }

        // 外部プロセスのメイン・ウィンドウを起動するためのWin32 API
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")]
        private static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);
        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        /// <summary>
        /// 終了時処理
        /// Mutexを解放する。
        /// </summary>
        /// <param name="e"></param>
        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            if (_ownerShip)
            {
                _mutex?.ReleaseMutex();
            }
            _mutex?.Close();
        }
    }
}
