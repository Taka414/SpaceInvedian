using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SpaceInvadian
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        //
        // Fields
        // - - - - - - - - - - - - - - - - - - - -
        #region...

        //
        // 自機
        //

        /// <summary>自機</summary>
        private Image own;
        /// <summary>自機の移動量</summary>
        private double movingDistace = 3;
        /// <summary>自機の横幅</summary>
        private readonly double ownWidth = 32;
        /// <summary>自機の縦幅</summary>
        private readonly double ownHeight = 32;

        //
        // フレーム更新
        //

        /// <summary>フレーム更新用のタイマー</summary>
        private Timer frameUpdateTimer;

        //
        // キー操作
        //

        /// <summary>押している最中のキー</summary>
        private Key? leftOrRightKey = null;
        /// <summary>スペースキー押しっぱなしかどうか</summary>
        private bool isSpaceKeyDown;

        //
        // 弾管理
        //

        /// <summary>自分が撃てる弾の最大数</summary>
        private readonly int maxOwnBullet = 3;
        /// <summary>今発射している最中の弾</summary>
        private int _currentOwnBulletCnt = 0;
        /// <summary>今発射している最中の弾</summary>
        private int currentOwnBulletCnt
        {
            get { return this._currentOwnBulletCnt; }
            set
            {
                if (value < 0)
                {
                    value = 0;
                }
                this._currentOwnBulletCnt = value;
            }
        }
        /// <summary>発射中の画像の弾の管理リスト</summary>
        private List<Image> ownBulletList = new List<Image>();

        //
        // 防御ブロック
        //

        /// <summary>防御ブロックの管理リスト</summary>
        private List<Image> defenseBlockList = new List<Image>();
        /// <summary>防御ブロック横幅</summary>
        private readonly int dfBlockWidth = 16;
        /// <summary>防御ブロック高さ</summary>
        private readonly int dfBlockHeight = 16;

        //
        // UFO管理
        //
        
        /// <summary>右方向で初期化</summary>
        private MoveDirection UfoMoveDir = MoveDirection.Right;
        /// <summary>UFOに弾が当たったときの演出リスト</summary>
        private List<HitContainer> hitList = new List<HitContainer>();
        /// <summary>UFOの総被弾数</summary>
        private int UfoHitCnt;

        //
        // スコア
        //

        /// <summary>ゲームのスコアを管理する変数</summary>
        private int _score;

        #endregion

        //
        // Constructors
        // - - - - - - - - - - - - - - - - - - - -
        #region...

        /// <summary>
        /// 既定の初期値でオブジェクトを初期化します。
        /// </summary>
        public MainWindow()
        {
            this.InitializeComponent();

            // 画面更新用のタイマーを初期化
            this.frameUpdateTimer = new Timer(1.0 / 60);
            this.frameUpdateTimer.Elapsed += FrameUpdateTimer_Update;
            this.frameUpdateTimer.Start();

            // スコアを初期化
            this.TextBlockScore.Text = "0";

            // 防御ブロックの初期化
            for (int b = 0; b < 3; b++)
            {
                double offset = b * 190;
                for (int i = 0; i < 3; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        Image dfBlock = new Image()
                        {
                            Source = new BitmapImage(new Uri("/Assets/block.png", UriKind.Relative)),
                            Width = dfBlockWidth,
                            Height = dfBlockHeight,
                        };

                        Canvas.SetTop(dfBlock, 290 + (dfBlockWidth + 1) * i);
                        Canvas.SetLeft(dfBlock, 70 + offset + (dfBlockWidth + 1) * j);

                        this.Canvas.Children.Add(dfBlock);
                        this.defenseBlockList.Add(dfBlock);
                    }
                }
            }
        }

        #endregion

        //
        // EventHandlers
        // - - - - - - - - - - - - - - - - - - - -
        #region...

        /// <summary>
        /// 初期値の設定
        /// </summary>
        protected void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // 自機を配置
            var aircraft = new Image()
            {
                Source = new BitmapImage(new Uri("/Assets/own.png", UriKind.Relative)),
                Width = this.ownWidth,
                Height = this.ownHeight
            };

            this.Canvas.Children.Add(aircraft);

            Canvas.SetTop(aircraft, 350);
            Canvas.SetLeft(aircraft, this.Canvas.ActualWidth / 2 - aircraft.Width / 2);

            this.own = aircraft;
        }

        /// <summary>
        /// キー押したとき
        /// </summary>
        protected void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Left || e.Key == Key.Right)
            {
                this.leftOrRightKey = e.Key;
            }
            else if (e.Key == Key.Space)
            {
                // 1回押すたびに1発発射、押しっぱなしで連続発射しない
                if (this.isSpaceKeyDown)
                {
                    return;
                }

                // 弾の発射
                if (this.currentOwnBulletCnt < this.maxOwnBullet)
                {
                    var _bullet = new Image()
                    {
                        Source = new BitmapImage(new Uri("/Assets/bullet.png", UriKind.Relative)),
                        Width = 2,
                        Height = 4,
                    };

                    // 自機の中心に出現させる
                    Canvas.SetTop(_bullet, 350);
                    Canvas.SetLeft(_bullet, Canvas.GetLeft(this.own) + this.ownWidth / 2);

                    // 画面に追加
                    this.Canvas.Children.Add(_bullet);

                    // 弾管理リストに追加
                    this.ownBulletList.Add(_bullet);

                    this.currentOwnBulletCnt++;
                }

                this.isSpaceKeyDown = true;
            }
        }

        /// <summary>
        /// キー離したとき
        /// </summary>
        protected void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Left || e.Key == Key.Right)
            {
                this.leftOrRightKey = null;
            }
            else if (e.Key == Key.Space)
            {
                this.isSpaceKeyDown = false;
            }
        }

        /// <summary>
        /// 1フレームごとの初期
        /// </summary>
        protected void FrameUpdateTimer_Update(object sender, ElapsedEventArgs e)
        {
            try
            {
                this.Dispatcher.Invoke(() =>
                {
                    // 自機の移動
                    if (leftOrRightKey != null)
                    {

                        double currentCanvasLeft = Canvas.GetLeft(this.own);

                        if (this.leftOrRightKey == Key.Left)
                        {
                            currentCanvasLeft -= this.movingDistace;
                        }
                        else if (this.leftOrRightKey == Key.Right)
                        {
                            currentCanvasLeft += this.movingDistace;
                        }

                        if (currentCanvasLeft >= -(this.ownWidth / 2) && currentCanvasLeft + this.ownWidth / 2 <= this.Canvas.ActualWidth)
                        {
                            // はみ出していなければ移動
                            Canvas.SetLeft(this.own, currentCanvasLeft);
                        }
                    }

                    // 自機弾の処理
                    for (int i = this.ownBulletList.Count; i > 0; i--)
                    {
                        Image temp_ownBullet = this.ownBulletList[i - 1];
                        double ownBulletTop = Canvas.GetTop(temp_ownBullet);

                        // ブロックとの衝突判定
                        for (int bi = this.defenseBlockList.Count; bi > 0; bi--)
                        {
                            Image temp_dfBlock = this.defenseBlockList[bi - 1];
                            if (GameUtil.IsCollision(temp_ownBullet, temp_dfBlock))
                            {
                                this.defenseBlockList.Remove(temp_dfBlock);
                                this.Canvas.Children.Remove(temp_dfBlock);

                                this.removeBullet(temp_ownBullet);

                                this.addScore(10);

                                continue;
                            }
                        }

                        // UFOの被弾処理
                        if(GameUtil.IsCollision(this.Ufo, temp_ownBullet))
                        {
                            UfoHitCnt++;

                            this.removeBullet(temp_ownBullet);

                            // 3回当たったらUFOは死亡
                            if (UfoHitCnt >= 3)
                            {
                                this.Canvas.Children.Remove(this.Ufo);
                                this.addScore(50);

                                // UFO死んだからゲームクリア
                                this.gameClear();

                                return;
                            }

                            this.putHitMessage(Canvas.GetTop(this.Ufo) - 15, Canvas.GetLeft(this.Ufo) + 10);
                        }

                        // イカの被弾処理
                        for (int b = this.ikaList.Count; b > 0; b--)
                        {
                            var ika = this.ikaList[b - 1];
                            if (GameUtil.IsCollision(ika.IkaImage, temp_ownBullet))
                            {
                                this.removeBullet(temp_ownBullet);
                                this.Canvas.Children.Remove(ika.IkaImage);
                                this.ikaList.Remove(ika);
                                this.addScore(5);
                            }
                        }

                        if (ownBulletTop >= -4)
                        {
                            Canvas.SetTop(temp_ownBullet, ownBulletTop - 4);
                        }
                        else
                        {
                            this.removeBullet(temp_ownBullet);
                        }
                    }

                    // UFOのイカ生成
                    this.putIka(Canvas.GetTop(this.Ufo) + this.Ufo.Height + 5, Canvas.GetLeft(Ufo));

                    // イカの移動
                    this.updateIka();

                    // イカ弾の処理
                    this.updateIkaBullet();

                    // Hitメッセージの管理
                    this.removeHitMessage();

                    // UFOの移動
                    this.moveUfo();
                });
            }
            catch (TaskCanceledException)
            {
                // nop
            }
        }

        #endregion

        //
        // Private Methods
        // - - - - - - - - - - - - - - - - - - - -
        #region...

        /// <summary>
        /// ゲームのスコアを加算します。
        /// </summary>
        private void addScore(int addingPoint)
        {
            _score += addingPoint;
            this.TextBlockScore.Text = _score.ToString();
        }

        /// <summary>
        /// UFOを移動します。
        /// </summary>
        private void moveUfo()
        {
            // 現在位置の取得
            double pos = Canvas.GetLeft(this.Ufo);

            // 方向転換
            if (pos >= 530)
            {
                this.UfoMoveDir = MoveDirection.Left;
            }
            else if (pos <= 10)
            {
                this.UfoMoveDir = MoveDirection.Right;
            }

            // 移動
            if (this.UfoMoveDir == MoveDirection.Right)
            {
                pos += 2;
            }
            else if (this.UfoMoveDir == MoveDirection.Left)
            {
                pos -= 2;
            }

            Canvas.SetLeft(this.Ufo, pos);
        }

        /// <summary>
        /// イカを指定した位置に配置します。
        /// </summary>
        private void putIka(double top, double left)
        {
            // 時間を経過したらイカが生まれる
            if(this.ikaDropTime > this.ikaInterval)
            {
                this.ikaDropTime = 0;

                var ika = new Image()
                {
                    Source = new BitmapImage(new Uri("/Assets/ika.png", UriKind.Relative)),
                    Width = 32,
                    Height = 32,
                };

                this.Canvas.Children.Add(ika);
                this.ikaList.Add(new IkaContainer { IkaImage = ika });

                Canvas.SetTop(ika, top);
                Canvas.SetLeft(ika, left);
            }

            this.ikaDropTime++;
        }

        /// <summary>
        /// イカを移動します。
        /// </summary>
        private void updateIka()
        {
            for(int i = this.ikaList.Count; i > 0; i--)
            {
                var ika = this.ikaList[i - 1];

                // イカが手前まできたら除去
                if(Canvas.GetTop(ika.IkaImage) > this.Canvas.ActualHeight - 150)
                {
                    this.Canvas.Children.Remove(ika.IkaImage);
                    this.ikaList.Remove(ika);
                    continue;
                }

                // 時間が経過してら弾を発射
                if(ika.BulletCnt > 50)
                {
                    ika.BulletCnt = 0;

                    var ikaBullet = new Image()
                    {
                        Source = new BitmapImage(new Uri("/Assets/bullet.png", UriKind.Relative)),
                        Width = 2,
                        Height = 4,
                    };

                    this.Canvas.Children.Add(ikaBullet);
                    this.ikaBulletList.Add(ikaBullet);

                    Canvas.SetTop(ikaBullet, Canvas.GetTop(ika.IkaImage) + ika.IkaImage.Height + 2);
                    Canvas.SetLeft(ikaBullet, Canvas.GetLeft(ika.IkaImage) + ika.IkaImage.Width / 2);
                }

                ika.BulletCnt++;

                // イカの移動
                Canvas.SetTop(ika.IkaImage, Canvas.GetTop(ika.IkaImage) + 1);
            }
        }

        /// <summary>
        /// イカの弾を更新します。
        /// </summary>
        private void updateIkaBullet()
        {
            for(int i = this.ikaBulletList.Count; i > 0; i--)
            {
                Image ikaBullet = this.ikaBulletList[i - 1];

                bool skipflag = false;

                // 防御ブロックとのあたり判定
                for(int dfi = this.defenseBlockList.Count; dfi > 0; dfi--)
                {
                    Image dfBlock = this.defenseBlockList[dfi - 1];

                    if(GameUtil.IsCollision(dfBlock, ikaBullet))
                    {
                        this.Canvas.Children.Remove(dfBlock);
                        this.defenseBlockList.Remove(dfBlock);

                        this.Canvas.Children.Remove(ikaBullet);
                        this.ikaBulletList.Remove(ikaBullet);

                        skipflag = true;

                        break;
                    }
                }

                if (skipflag)
                {
                    skipflag = false;
                    continue;
                }

                // 自機とのあたり判定
                if(GameUtil.IsCollision(this.own, ikaBullet))
                {
                    this.gameOver();
                    break;
                }

                Canvas.SetTop(ikaBullet, Canvas.GetTop(ikaBullet) + 3);
            }
        }

        /// <summary>
        /// 自機の発射した弾を画面上から除去します。
        /// </summary>
        private void removeBullet(Image bullet)
        {
            // 弾の除去
            this.Canvas.Children.Remove(bullet);
            this.ownBulletList.Remove(bullet);
            this.currentOwnBulletCnt--;
        }

        /// <summary>UFOがイカを生成するインターバル</summary>
        private int ikaInterval = 120;
        /// <summary>イカが生まれてから経過した時間</summary>
        private int ikaDropTime;
        /// <summary>
        /// 敵のイカを管理するリスト
        /// T1 : イカの画像
        /// T2 : イカが弾を発射してからの経過フレーム数
        /// </summary>
        private List<IkaContainer> ikaList = new List<IkaContainer>();
        /// <summary>イカが発射した弾のリスト</summary>
        private List<Image> ikaBulletList = new List<Image>();

        /// <summary>
        /// UFOに弾が当たったときの演出 
        /// </summary>
        private void putHitMessage(double top, double left)
        {
            var hitMsg = new TextBlock()
            {
                Text = "HIT!",
                Foreground = new SolidColorBrush(Colors.White),
            };

            this.Canvas.Children.Add(hitMsg);

            Canvas.SetTop(hitMsg, top);
            Canvas.SetLeft(hitMsg, left);

            this.hitList.Add(new HitContainer() { Text = hitMsg });
        }

        /// <summary>
        /// UFOに弾が当たったときの演出の管理 
        /// </summary>
        private void removeHitMessage()
        {
            for(int i = this.hitList.Count; i > 0; i--)
            {
                HitContainer _hit = this.hitList[i - 1];

                if(_hit.Time > 20)
                {
                    this.Canvas.Children.Remove(_hit.Text);
                    this.hitList.Remove(_hit);
                }
                else
                {
                    _hit.Time++;
                }
            }
        }

        /// <summary>
        /// ゲームのクリア処理 
        /// </summary>
        private void gameClear()
        {
            this.frameUpdateTimer.Stop();

            var clearMessage = new TextBlock()
            {
                Text = "Clear!!",
                Foreground = new SolidColorBrush(Colors.White),
                FontSize = 42,
            };

            this.Canvas.Children.Add(clearMessage);

            Canvas.SetLeft(clearMessage, 220);
            Canvas.SetTop(clearMessage, 120);
        }

        /// <summary>
        /// ゲームオーバーの処理
        /// </summary>
        private void gameOver()
        {
            this.frameUpdateTimer.Stop();

            this.Canvas.Children.Remove(this.own);

            var clearMessage = new TextBlock()
            {
                Text = "GameOver!!",
                Foreground = new SolidColorBrush(Colors.White),
                FontSize = 42,
            };

            this.Canvas.Children.Add(clearMessage);

            Canvas.SetLeft(clearMessage, 180);
            Canvas.SetTop(clearMessage, 120);
        }

        #endregion
    }

    public class IkaContainer
    {
        public Image IkaImage { get; set; }
        public int BulletCnt { get; set; }
    }
}
