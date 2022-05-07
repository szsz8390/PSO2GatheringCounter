using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace PSO2GatheringCounter
{
    /// <summary>
    /// メイン画面
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>数値の正規表現</summary>
        private readonly System.Text.RegularExpressions.Regex _numberRegex = new("[0-9]+");
        /// <summary>ログファイルを定期的に読むためのタイマー</summary>
        private readonly DispatcherTimer _timer = new ();
        /// <summary>アイテムリスト</summary>
        private readonly ObservableCollection<GridModel> _items = new();

        /// <summary>右クリックされた行のインデックス</summary>
        private int _rightClickedRowIndex = -1;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            this.Closed += MainWindow_Closed;

            // ウィンドウ設定
            LoadWindowBounds();
            bool aot = Properties.Settings.Default.AlwaysOnTop;
            Topmost = aot;
            MenuViewAot.IsChecked = aot;

            // 画面設定
            SetGrid();
            SetToday();
            // 初回データ読み込み
            GetCounts();
            // タイマー設定
            SetupTimer();
        }

        /// <summary>
        /// アプリケーション設定からウィンドウの大きさと位置を取得して反映させる。
        /// </summary>
        private void LoadWindowBounds()
        {
            var settings = Properties.Settings.Default;
            if (settings.WindowLeft >= 0)
            {
                this.Left = settings.WindowLeft;
            }
            if (settings.WindowTop >= 0)
            {
                this.Top = settings.WindowTop;
            }
            if (settings.WindowWidth > 0)
            {
                this.Width = settings.WindowWidth;
            }
            if (settings.WindowHeight > 0)
            {
                this.Height = settings.WindowHeight;
            }
        }

        /// <summary>
        /// 収集アイテムリストの設定
        /// </summary>
        private void SetGrid()
        {
            // 固定のアイテム
            _items.Add(new GridModel("アルファリアクター", 14, true));
            _items.Add(new GridModel("ステラーシード", 10, true));
            _items.Add(new GridModel("フォトンスケイル", 5, true));

            // ユーザ定義のアイテムを取得して追加
            var userItems = Util.GetUserItems();
            if (userItems.Count > 0)
            {
                foreach (var userItem in userItems)
                {
                    _items.Add(userItem);
                }
            }

            // 新規追加行
            _items.Add(new GridModel("", 0));

            dataGrid.ItemsSource = _items;
        }

        /// <summary>
        /// 日付表示ラベルの設定
        /// </summary>
        private void SetToday()
        {
            // 日付+時
            var realToday = Util.getToday(0);
            textRealToday.Content = $"{realToday.ToString("yyyy/MM/dd HH")}時";
            // 4時切り替えの日付も表示
            var today = Util.getToday(4);
            textToday.Content = today.ToString("yyyy/MM/dd");
        }

        /// <summary>
        /// ログファイルを読み、該当アイテムの取得数を数える。
        /// </summary>
        /// <remarks>
        /// ファイルの更新差分を取得するのが面倒なため、毎回ゼロから数える。
        /// </remarks>
        private void GetCounts()
        {
            // 起動中に日をまたぐ場合もあるので更新しておく
            SetToday();
            var documentDir = System.Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            var logDir = System.IO.Path.Combine(documentDir, "SEGA", "PHANTASYSTARONLINE2", "log_ngs");
            // ログファイルの日付
            // ログファイル ActionLog[yyyyMMdd]_*.txt
            // *: "00"
            // ログ形式: TSV
            // 2022-05-05T16:14:47	3	[Pickup]	*player id*	*chara name*	アルファリアクター	Num(1)
            var today = Util.getToday(4);
            // TODO: 要検証 01以降がある（ローリングする）のか？条件は？
            //   ファイルサイズ？日付？1MBを超えてもローリングされていはいない
            // 翌日になっても同じファイルに書き込まれる場合がある
            // いつ切り替わるのか？
            // 翌日AM7時でも同じファイルに書き込まれたことがある。ログインし続けたわけではない。
            // ファイル: ActionLog20220503_00.txt
            // 2022-05-04T07:04:48	22	[Pickup]	*player id*	*chara name*	RestaSign	CurrentNum(9)
            // 4時でないとしたら11時？なお、2022/5/4は水曜日（メンテ日、ただしメンテ自体はお休みの日）
            var fileName = $"ActionLog{today.ToString("yyyyMMdd")}_*.txt";
            var files = Directory.GetFiles(logDir, fileName);
            var itemsCount = _items.Count;
            // 個数
            var counts = new int[itemsCount];

            // ログファイルに記録された取得ログを数える
            foreach (var file in files)
            {
                var lines = Util.ReadFileAllLines(file);
                for (int i = 0; i < itemsCount; i++)
                {
                    var line = lines[i];
                    var item = _items[i];
                    if (string.IsNullOrWhiteSpace(item.ItemName)) continue;
                    lines.Where(line => line.Contains(item.ItemName)).ToList().ForEach(line =>
                    {
                        // 獲得数を取得
                        var num = GetCountFromLine(line, item.ItemName);
                        counts[i] += num;
                    });
                }
            }

            // 数えた数を設定
            for (int i = 0; i < itemsCount; i++)
            {
                var item = _items[i];
                item.GetCount = counts[i];
                // ノルマ数以上になったら完了にする
                if (!string.IsNullOrWhiteSpace(item.ItemName))
                {
                    if (item.GetCount >= item.NormaCount)
                    {
                        item.Completed = true;
                    }
                }
            }
        }

        /// <summary>
        /// 該当アイテムの獲得数を取得する。
        /// </summary>
        /// <param name="line">ログの1行</param>
        /// <param name="itemName">アイテム名</param>
        /// <returns>アイテムの獲得数（0～）</returns>
        private int GetCountFromLine(string line, string itemName)
        {
            int count = 0;
            // ログファイルはTSV
            var columns = line.Split("\t");
            // アイテム名が合っていて、取得ログの場合のみ加算
            if (columns[2] == "[Pickup]" && columns[5] == itemName)
            {
                // 獲得数 Num\([0-9]+\)
                var num = columns[6];
                var match = _numberRegex.Match(num);
                // 数値の正規表現にマッチした部分なので直Parseで大丈夫
                count = int.Parse(match?.Value ?? "0");
            }
            return count;
        }

        /// <summary>
        /// タイマーの設定
        /// </summary>
        private void SetupTimer()
        {
            _timer.Interval = new TimeSpan(0, 0, 1);
            _timer.Tick += new EventHandler(OnTimer);
            _timer.Start();
            this.Closing += new CancelEventHandler(StopTimer);
        }

        /// <summary>
        /// タイマー処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnTimer(object? sender, EventArgs e)
        {
            GetCounts();
        }

        /// <summary>
        /// タイマーの停止
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StopTimer(object? sender, CancelEventArgs e)
        {
            _timer.Stop();
        }

        /// <summary>
        /// ウィンドウの大きさと位置をアプリケーション設定に保存する。
        /// </summary>
        private void SaveWindowBounds()
        {
            var settings = Properties.Settings.Default;
            settings.WindowLeft = this.Left;
            settings.WindowTop = this.Top;
            settings.WindowWidth = this.Width;
            settings.WindowHeight = this.Height;
            settings.Save();
        }

        /// <summary>
        /// メイン画面終了時、設定を保存する。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainWindow_Closed(object? sender, EventArgs e)
        {
            // ウィンドウ
            SaveWindowBounds();
            // ユーザー定義のアイテム
            Util.WriteUserItems(_items);
        }

        /// <summary>
        /// メニューの「終了(_Q)」クリック時、終了する。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItemQuit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        /// <summary>
        /// メニューの「常に手前に表示」クリック時、常に手前に表示するかどうかを切り替える。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItemAot_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            if (menuItem == null) return;
            var aot = menuItem.IsChecked;
            var settings = Properties.Settings.Default;
            settings.AlwaysOnTop = aot;
            settings.Save();
            Topmost = aot;
        }

        /// <summary>
        /// グリッドのデータソース更新時、新規追加行のアイテム名を埋めたなら
        /// 新しい新規追加行を追加する。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dataGrid_SourceUpdated(object sender, DataTransferEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(_items.Last().ItemName))
            {
                // 新規追加行
                _items.Add(new GridModel("", 0));
            }
        }

        /// <summary>
        /// グリッドの行ロード時、右クリックイベントを追加する。
        /// </summary>
        /// <remarks>
        /// 同じ行に対して複数回呼ばれることがあるので、
        /// イベントは一度解除してから追加する。
        /// </remarks>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.MouseRightButtonDown -= new MouseButtonEventHandler(Row_MouseRightButtonDown);
            e.Row.MouseRightButtonDown += new MouseButtonEventHandler(Row_MouseRightButtonDown);

            e.Row.MouseRightButtonUp -= new MouseButtonEventHandler(Row_MouseRightButtonUp);
            e.Row.MouseRightButtonUp += new MouseButtonEventHandler(Row_MouseRightButtonUp);
        }

        /// <summary>
        /// グリッド行の右クリック（Down）時処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Row_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        /// <summary>
        /// グリッド行の右クリック（Up）時処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Row_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            // グリッド行に割り当てたイベントなのでsenderはグリッド行
            DataGridRow row = sender as DataGridRow;
            // クリックされた行を選択状態にする
            dataGrid.SelectedIndex = row.GetIndex();
        }

        /// <summary>
        /// グリッドの右クリックメニューから「削除」をクリックした時、
        /// 行を削除する。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void contextMenuDelete_Click(object sender, RoutedEventArgs e)
        {
            if (_rightClickedRowIndex < 0) return;
            _items.RemoveAt(_rightClickedRowIndex);
            _rightClickedRowIndex = -1;
        }

        /// <summary>
        /// グリッドの右クリック（Up）時、右クリックメニューを表示する。
        /// </summary>
        /// <remarks>
        /// グリッド行に割り当てたイベントが先に発火するので、
        /// 選択はされているはず
        /// TODO: 検証
        /// </remarks>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dataGrid_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            // 選択なしの場合、なにもしない
            if (dataGrid.SelectedCells.Count <= 0) return;

            // 行のモデルを取得
            GridModel d = (GridModel)dataGrid.SelectedCells[0].Item;
            int rowIndex = _items.IndexOf(d);
            if (d.ReadOnly)
            {
                // 固定のアイテム行（ReadOnly）なら処理を打ち切る
                return;
            }
            else
            {
                // アイテム名が空白の場合、最終行なら新規追加行なので
                // 削除させる必要はないため、処理を打ち切る
                if (string.IsNullOrWhiteSpace(d.ItemName))
                {
                    if (rowIndex == _items.Count - 1)
                    {
                        return;
                    }
                }
            }
            // 行インデックスを保存
            _rightClickedRowIndex = rowIndex;

            // 右クリックメニューアイテム「削除」作成
            MenuItem menuitem = new MenuItem();
            menuitem.Header = "削除";
            menuitem.Click += contextMenuDelete_Click;
            // 右クリックメニュー作成
            ContextMenu contextmenu = new ContextMenu();
            contextmenu.Items.Add(menuitem);
            // グリッドの右クリックメニューとする
            dataGrid.ContextMenu = contextmenu;
        }

        /// <summary>
        /// グリッド編集開始時、行が固定のアイテム行（ReadOnly）なら編集をキャンセルする。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dataGrid_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            var rowIndex = dataGrid.ItemContainerGenerator.IndexFromContainer(e.Row);
            var currentItem = _items[rowIndex];
            if (currentItem.ReadOnly)
            {
                e.Cancel = true;
            }
        }

        /// <summary>
        /// グリッド編集終了時、アイテム名が空ならメッセージを表示し、
        /// 編集をキャンセルするか行を削除するか選択させる。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            var editingElement = e.EditingElement;
            if (editingElement is TextBox)
            {
                var editingTextBox = editingElement as TextBox;
                var newValue = editingTextBox.Text;
                // アイテム名が空の場合、確認
                if (string.IsNullOrWhiteSpace(newValue))
                {
                    var rowIndex = e.Row.GetIndex();
                    var result = MessageBox.Show("アイテム名を空にすることはできません。\nこの行を削除しますか？", "確認", MessageBoxButton.OKCancel);
                    if (result == MessageBoxResult.OK)
                    {
                        // 行削除
                        _items.RemoveAt(rowIndex);
                    }
                    else
                    {
                        // アイテム名をもとにもどす
                        editingTextBox.Text = _items[rowIndex].ItemName;
                    }
                }

            }
        }

    }
}
