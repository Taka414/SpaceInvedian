using System.Windows.Controls;

namespace SpaceInvadian
{
    /// <summary>
    /// ヒット
    /// </summary>
    public class HitContainer
    {
        /// <summary>
        /// 経過時間を設定または取得します。
        /// </summary>
        public int Time { get; set; }

        /// <summary>
        /// 管理対象のテキストを設定または取得します。
        /// </summary>
        public TextBlock Text { get; set; }
    }
}
