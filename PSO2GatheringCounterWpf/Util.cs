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
        public static DateTime getToday(int dateLineHour)
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
                    if (userItemColumns.Length != 2) continue;
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

    }
}
