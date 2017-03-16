using System;
using System.Windows;
using System.Windows.Controls;

namespace SpaceInvadian
{
    /// <summary>
    /// ゲームで使用する汎用機能を提供します。
    /// </summary>
    public class GameUtil
    {
        /// <summary>
        /// 指定した2つのオブジェクトが衝突しているか確認します。
        /// </summary>
        public static bool IsCollision(FrameworkElement a, FrameworkElement b)
        {
            double x_abs = Math.Abs((Canvas.GetTop(a) + a.Width / 2) - (Canvas.GetTop(b) + a.Width / 2));
            double y_abs = Math.Abs((Canvas.GetLeft(a) + a.Height / 2) - (Canvas.GetLeft(b) + a.Height / 2));

            double aw = a.Width / 2 + b.Width / 2;
            double ah = a.Height / 2 + b.Height / 2;

            return x_abs < aw && y_abs < ah;
        }
    }
}
