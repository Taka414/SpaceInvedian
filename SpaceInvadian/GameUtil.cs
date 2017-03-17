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
            double x_abs = Math.Abs((Canvas.GetTop(a) + a.Width / 2) - (Canvas.GetTop(b) + b.Width / 2));
            double y_abs = Math.Abs((Canvas.GetLeft(a) + a.Height / 2) - (Canvas.GetLeft(b) + b.Height / 2));

            double aw = a.Width / 2 + b.Width / 2;
            double ah = a.Height / 2 + b.Height / 2;

            return x_abs < aw && y_abs < ah;
        }

        /// <summary>
        /// 指定した画像の中央座標を取得します。
        /// Tuple
        ///   - Item1 : X-Center
        ///   - Item2 : Y-Center
        /// </summary>
        public static Tuple<double, double> GetCenter(FrameworkElement src)
        {
            double left = Canvas.GetLeft(src) + src.Width / 2;
            double top = Canvas.GetTop(src) + src.Height / 2;

            return new Tuple<double, double>(left, top);
        }
    }
}
