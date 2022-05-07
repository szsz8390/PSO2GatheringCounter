using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace PSO2GatheringCounter
{
    /// <summary>
    /// グリッドのアイテム行モデルクラス
    /// </summary>
    internal class GridModel : INotifyPropertyChanged
    {
        private string _ItemName;
        /// <summary>アイテム名</summary>
        public string ItemName
        { 
            get
            {
                return _ItemName;
            }
            set
            {
                _ItemName = value;
                OnPropertyChanged(nameof(ItemName));
            }
        }
        private int _GetCount;
        /// <summary>取得数</summary>
        public int GetCount
        {
            get
            {
                return _GetCount;
            }
            set
            {
                _GetCount = value;
                OnPropertyChanged(nameof(GetCount));
            }
        }
        private int _NormaCount;
        /// <summary>ノルマ数</summary>
        public int NormaCount
        {
            get
            {
                return _NormaCount;
            }
            set
            {
                _NormaCount = value;
                OnPropertyChanged(nameof(NormaCount));
            }
        }
        private bool _Completed;
        /// <summary>完了</summary>
        public bool Completed
        {
            get
            {
                return _Completed;
            }
            set
            {
                _Completed = value;
                OnPropertyChanged(nameof(Completed));
            }
        }
        /// <summary>読み取り専用かどうか</summary>
        public bool ReadOnly
        { 
            get; 
            set; 
        }

        /// <summary>
        /// ユーザ定義アイテム用コンストラクタ
        /// </summary>
        /// <param name="itemName">アイテム名</param>
        /// <param name="normaCount">ノルマ数</param>
        public GridModel(string itemName, int normaCount) : this(itemName, 0, normaCount, false, false)
        {
        }

        /// <summary>
        /// 固定アイテム用コンストラクタ
        /// </summary>
        /// <param name="itemName">アイテム名</param>
        /// <param name="normaCount">ノルマ数</param>
        /// <param name="readOnly">読み取り専用かどうか</param>
        public GridModel(string itemName, int normaCount, bool readOnly) : this(itemName, 0, normaCount, false, readOnly)
        {
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="itemName">アイテム名</param>
        /// <param name="getCount">取得数</param>
        /// <param name="normaCount">ノルマ数</param>
        /// <param name="completed">完了</param>
        /// <param name="readOnly">読み取り専用かどうか</param>
        public GridModel(string itemName, int getCount, int normaCount, bool completed, bool readOnly)
        {
            ItemName = itemName;
            GetCount = getCount;
            NormaCount = normaCount;
            Completed = completed;
            ReadOnly = readOnly;
        }


        #region "INotifyPropertyChanged Implementation"

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

    }
}
