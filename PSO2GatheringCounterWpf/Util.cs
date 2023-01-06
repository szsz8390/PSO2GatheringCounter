using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PSO2GatheringCounter
{
    /// <summary>
    /// ユーティリティクラス
    /// </summary>
    internal static class Util
    {
        /// <summary>数値の正規表現</summary>
        private static readonly System.Text.RegularExpressions.Regex _numberRegex = new("[0-9]+");
        /// <summary>ログの日付をパースする時、カルチャに依存せずにパースするためのカルチャ指定</summary>
        private static readonly System.Globalization.CultureInfo _defaultCultureProvider = System.Globalization.CultureInfo.InvariantCulture;

        /// <summary>
        /// 今日の日時を取得する。
        /// 日付の切り替え時刻に達していない場合、前日の日時を取得する。
        /// </summary>
        /// <example>
        /// 4/2 0時 getToday(0) → 4/2 0時
        /// 4/2 0時 getToday(1) → 4/1 0時
        /// </example>
        /// <param name="dateLineHour">日付の切り替え時刻</param>
        /// <returns>今日の日時</returns>
        public static DateTime GetToday(int dateLineHour)
        {
            var now = DateTime.Now;
            // 日付の切り替え時刻に達していない場合、前日とする
            if (now.Hour < dateLineHour)
            {
                return now.AddDays(-1);
            }
            return now;
        }

        /// <summary>
        /// 指定ファイルの全行を読み込む。
        /// </summary>
        /// <remarks>
        /// File.ReadFileAllLinesではロックのかかっている
        /// ＝PSO2が書き込んでいるログファイルが開けないので
        /// その代替メソッド。
        /// </remarks>
        /// <param name="path">ファイルのパス</param>
        /// <returns>指定ファイルの内容</returns>
        public static string[] ReadFileAllLines(string path)
        {
            var lines = new List<string>();
            try
            {
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using (var sr = new StreamReader(fs))
                    {
                        // 改行で分割
                        // TODO: 要確認 CRLFでよい？
                        lines.AddRange(sr.ReadToEnd().Split("\r\n").Where(line => !string.IsNullOrWhiteSpace(line)));
                    }
                }
            }
            catch
            {
                // 取得失敗時
            }
            return lines.ToArray();
        }

        /// <summary>
        /// ユーザ定義のアイテム情報を取得する。
        /// </summary>
        /// <returns>ユーザ定義アイテムリスト</returns>
        public static IList<GridModel> GetUserItems()
        {
            // ユーザ定義のアイテム
            // カレントディレクトリのitems.csvファイル
            var userItemFilePath = Path.Combine(Directory.GetCurrentDirectory(), "items.csv");
            var items = new List<GridModel>();
            if (File.Exists(userItemFilePath))
            {
                var lines = ReadFileAllLines(userItemFilePath);
                var userItems = lines.Where(line => !string.IsNullOrWhiteSpace(line)).ToArray();
                foreach (var userItem in userItems)
                {
                    // CSV行[アイテム名,ノルマ数]
                    var userItemColumns = userItem.Split(",");
                    if (userItemColumns.Length < 2) continue;
                    var itemName = userItemColumns[0];
                    int normaCount;
                    if (!string.IsNullOrWhiteSpace(itemName) && int.TryParse(userItemColumns[1], out normaCount))
                    {
                        items.Add(new GridModel(userItemColumns[0], normaCount, false));
                    }
                }
            }
            return items;
        }

        /// <summary>
        /// ユーザ定義のアイテム情報をファイルに保存する。
        /// </summary>
        /// <param name="list">アイテムリスト</param>
        public static void WriteUserItems(IList<GridModel> list)
        {
            // 固定のアイテム（ReadOnly）以外、かつアイテム名が空でないもののみ保存対象
            var userItems = list.Where(item => !item.ReadOnly && !string.IsNullOrWhiteSpace(item.ItemName));
            if (userItems.Count() == 0)
            {
                return;
            }
            // CSV行[アイテム名,ノルマ数]
            var fileContents = userItems.Select(item => $"{item.ItemName},{item.NormaCount}").ToArray();
            // カレントディレクトリのitems.csvに保存
            var userItemFilePath = Path.Combine(Directory.GetCurrentDirectory(), "items.csv");
            File.WriteAllLinesAsync(userItemFilePath, fileContents);
        }

        public static IList<string> GetTargetLogFiles()
        {
            var targetLogFiles = new List<string>();
            // ログフォルダ
            // (ドキュメントフォルダ)\SEGA\PHANTASYSTARONLINE2\log_ngs
            var documentDir = System.Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            var logDir = System.IO.Path.Combine(documentDir, "SEGA", "PHANTASYSTARONLINE2", "log_ngs");
            // ログファイルの日付
            // ログファイル ActionLog[yyyyMMdd]_*.txt
            // *: "00"
            // ログ形式: TSV
            // 2022-05-05T16:14:47	3	[Pickup]	*player id*	*chara name*	アルファリアクター	Num(1)
            // 翌日になっても同じファイルに書き込まれる場合があるので
            // 前日のファイルも読み込み、カウント時に書き込み日付を判断する
            // TODO: 要検証 01以降がある（ローリングする）のか？条件は？
            //   ファイルサイズ？日付？1MBを超えてもローリングされていはいない
            var today = DateTime.Now;
            var fileName = $"ActionLog{today.ToString("yyyyMMdd")}_*.txt";
            var files = Directory.GetFiles(logDir, fileName);
            if (files != null && files.Length > 0)
            {
                targetLogFiles.AddRange(files);
            }
            var yesterday = today.AddDays(-1);
            var yesterdayFileName = $"ActionLog{yesterday.ToString("yyyyMMdd")}_*.txt";
            var yesterdayFiles = Directory.GetFiles(logDir, yesterdayFileName);
            if (yesterdayFiles != null && yesterdayFiles.Length > 0)
            {
                targetLogFiles.AddRange(yesterdayFiles);
            }
            return targetLogFiles;
        }

        /// <summary>
        /// 該当アイテムの獲得数を取得する。
        /// </summary>
        /// <param name="line">ログの1行</param>
        /// <param name="itemName">アイテム名</param>
        /// <returns>アイテムの獲得数（0～）</returns>
        public static int GetCountFromLine(string line, string itemName)
        {
            int count = 0;
            // ログファイルはTSV
            var columns = line.Split("\t");
            // 2022-04-30T17:15:23
            var logDatetime = columns[0];
            var logDt = DateTime.ParseExact(logDatetime, "yyyy-MM-ddTHH:mm:ss", _defaultCultureProvider);
            logDt = logDt.Hour < 4 ? logDt.AddDays(-1) : logDt;
            var today = Util.GetToday(4);
            if (logDt.Date.CompareTo(today.Date) != 0)
            {
                return count;
            }
            // アイテム名が合っていて、取得ログの場合のみ加算
            if (columns[2] == "[Pickup]" && columns[5] == itemName)
            {
                if (itemName == "アルファリアクター")
                {
                    System.Diagnostics.Debug.WriteLine("logDate: " + logDt + ", today: " + today);
                }
                // 獲得数 Num\([0-9]+\)
                var num = columns[6];
                var match = _numberRegex.Match(num);
                // 数値の正規表現にマッチした部分なので直Parseで大丈夫
                count = int.Parse(match?.Value ?? "0");
            }
            return count;
        }

        /// <summary>
        /// エラーログを書き込む。
        /// </summary>
        /// <param name="log">ログ内容</param>
        public static void WriteErrorLog(string log)
        {
            try
            {
                var errorLogFilePath = Path.Combine(Directory.GetCurrentDirectory(), "error.log");
                using (var fs = new FileStream(errorLogFilePath, FileMode.Append, FileAccess.ReadWrite, FileShare.ReadWrite))
                {
                    using (var sw = new StreamWriter(fs))
                    {
                        sw.WriteLine(log);
                    }
                }
            }
            catch
            {
            }
        }

    }
}
