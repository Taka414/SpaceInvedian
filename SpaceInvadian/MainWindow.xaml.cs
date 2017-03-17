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
        private bool isSpaceKeyDowned;

        //
        // 管理系
        //

        /// <summary>スコア管理</summary>
        private ScoreManager scoreMgr;

        //
        // 自機系
        //

        /// <summary>自機の画像</summary>
        private Image own;
        /// <summary>自機が発射した弾のリスト</summary>
        private List<Image> ownBulletList = new List<Image>();
        /// <summary>自機の弾の爆発モーションの管理リスト</summary>
        private List<TimerdImage> ownBulletExpList = new List<TimerdImage>();

        //
        // 敵機系
        //

        /// <summary>移動間隔</summary>
        private int moveInterval = 60;
        /// <summary>移動間隔</summary>
        private int currentDistance;
        /// <summary>敵のリスト</summary>
        private List<AnimationImage> enemyList = new List<AnimationImage>();
        /// <summary>敵の爆発表示のリスト</summary>
        private List<TimerdImage> enemyExpList = new List<TimerdImage>();

        //
        // Constructors
        // - - - - - - - - - - - - - - - - - - - -

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
        }

        //
        // EventHandlers
        // - - - - - - - - - - - - - - - - - - - -

        /// <summary>
        /// 初期値の設定
        /// </summary>
        protected void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.initGame();
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
                if (this.isSpaceKeyDowned)
                {
                    return;
                }

                // 自機の弾を発射
                this.putOwnBullet();

                this.isSpaceKeyDowned = true;
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
                this.isSpaceKeyDowned = false;
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
                    //testImg.UpdateNext();

                    this.moveOwn();

                    this.updateOwnBullet();

                    this.updateOwnBulletExp();

                    this.updateEnemyExp();

                    this.updateEnemy();

                });

            }
            catch (TaskCanceledException)
            {
                // nop
            }
        }

        //
        // 初期化系
        // - - - - - - - - - - - - - - - - - - - -

        /// <summary>
        /// ゲームを初期化します。
        /// </summary>
        private void initGame()
        {
            // スコア表示系の初期化
            this.scoreMgr = new ScoreManager(this.TextBlockScore);
            this.scoreMgr.InitScore();

            //this.testImg = new AnimationImage(this.Canvas);
            //this.testImg.AddImage("/Assets/inv_1_1.png", 24, 24);
            //this.testImg.AddImage("/Assets/inv_1_2.png", 24, 24);

            //this.testImg.Init();

            this.initOwn();
            this.initEnemy();
        }

        /// <summary>
        /// 自機の表示を初期化します。
        /// </summary>
        private void initOwn()
        {
            if(this.own != null)
            {
                this.Canvas.Children.Remove(this.own);
                this.own = null;
            }

            var _own = new Image()
            {
                Source = new BitmapImage(new Uri("/Assets/own_air.png", UriKind.Relative)),
                Width = 39,
                Height = 21,
            };

            this.own = _own;
            this.Canvas.Children.Add(_own);

            Canvas.SetTop(this.own, 490);
            Canvas.SetLeft(this.own, this.Canvas.ActualWidth / 2 - _own.Width / 2);
        }

        /// <summary>
        /// 敵のリストを初期化します。
        /// </summary>
        private void initEnemy()
        {
            // 1列目
            for(int i = 0; i < 11; i++)
            {
                var enemy = new AnimationImage(this.Canvas)
                {
                    OptionValue = 30,
                };
                enemy.AddImage("/Assets/inv_1_1.png", 24, 24);
                enemy.AddImage("/Assets/inv_1_2.png", 24, 24);
                enemy.Init();

                Canvas.SetTop(enemy.CurrentImage, 100);
                Canvas.SetLeft(enemy.CurrentImage, 15 + i * (24 + 22));

                this.enemyList.Add(enemy);
            }

            // 2, 3列目
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 11; j++)
                {
                    var enemy = new AnimationImage(this.Canvas)
                    {
                        OptionValue = 20
                    };
                    enemy.AddImage("/Assets/inv_2_1.png", 33, 24);
                    enemy.AddImage("/Assets/inv_2_2.png", 33, 24);
                    enemy.Init();

                    Canvas.SetTop(enemy.CurrentImage, 144 + (enemy.CurrentImage.Height + 24) * i);
                    Canvas.SetLeft(enemy.CurrentImage, 15 + j * (24 + 22) + ((24 - 33) / 2));

                    this.enemyList.Add(enemy);
                }
            }

            // 1列目
            for (int i = 0; i < 11; i++)
            {
                var enemy = new AnimationImage(this.Canvas)
                {
                    OptionValue = 10
                };
                enemy.AddImage("/Assets/inv_3_1.png", 27, 24);
                enemy.AddImage("/Assets/inv_3_2.png", 27, 24);
                enemy.Init();

                Canvas.SetTop(enemy.CurrentImage, 242);
                Canvas.SetLeft(enemy.CurrentImage, 15 + i * (24 + 22) + ((24 - 27) / 2));

                this.enemyList.Add(enemy);
            }
        }

        //private AnimationImage testImg;

        //
        // 自機の操作
        // - - - - - - - - - - - - - - - - - - - -

        /// <summary>
        /// 自機の移動
        /// </summary>
        private void moveOwn()
        {
            double pos = Canvas.GetLeft(this.own);

            if (this.leftOrRightKey == Key.Left)
            {
                pos -= 4;
            }
            else if (this.leftOrRightKey == Key.Right)
            {
                pos += 4;
            }

            if(pos <= 0 || pos + this.own.Width >= this.Canvas.ActualWidth)
            {
                return;
            }

            Canvas.SetLeft(this.own, pos);
        }

        //
        // 自機の弾関係
        // - - - - - - - - - - - - - - - - - - - -

        /// <summary>
        /// 自機の弾を発射します。
        /// </summary>
        private void putOwnBullet()
        {
            if(this.ownBulletList.Count >= 1)
            {
                return;
            }

            var bullet = new Image()
            {
                Source = new BitmapImage(new Uri("/Assets/own_bullet.png", UriKind.Relative)),
                Width = 2,
                Height = 6,
            };

            this.Canvas.Children.Add(bullet);
            this.ownBulletList.Add(bullet);

            Canvas.SetTop(bullet, Canvas.GetTop(this.own) - bullet.Height - 4);
            Canvas.SetLeft(bullet, Canvas.GetLeft(this.own) + this.own.Width / 2 - bullet.Width / 2);
        }

        /// <summary>
        /// 自機の弾を更新します。
        /// </summary>
        private void updateOwnBullet()
        {
            for (int i = this.ownBulletList.Count; i > 0; i--)
            {
                Image ownBullet = this.ownBulletList[i - 1];

                // 自分の弾が上端に達したら爆発モーションへ
                if (Canvas.GetTop(ownBullet) <= 0)
                {
                    this.putOwnBulletExp(ownBullet);
                    this.removeOwnBullet(ownBullet);
                    continue;
                }

                // 敵とのあたり判定に使用
                bool isBreak = false;

                for (int ienemy = this.enemyList.Count; ienemy > 0; ienemy--)
                {
                    var enemy = this.enemyList[ienemy - 1];

                    // 敵と衝突したら除去
                    if (GameUtil.IsCollision(ownBullet, enemy.CurrentImage))
                    {
                        // 敵を除去
                        this.Canvas.Children.Remove(enemy.CurrentImage);
                        this.enemyList.Remove(enemy);

                        // 爆発発生

                        var enemyExp = new Image()
                        {
                            Source = new BitmapImage(new Uri("/Assets/exp.png", UriKind.Relative)),
                            Width = 39,
                            Height = 21,
                        };

                        this.Canvas.Children.Add(enemyExp);
                        this.enemyExpList.Add(new TimerdImage() { Image = enemyExp });

                        Canvas.SetTop(enemyExp, Canvas.GetTop(enemy.CurrentImage) + (enemy.CurrentImage.Height - enemyExp.Height) / 2);
                        Canvas.SetLeft(enemyExp, Canvas.GetLeft(enemy.CurrentImage) + (enemy.CurrentImage.Width - enemyExp.Width) / 2);

                        this.removeOwnBullet(ownBullet);
                        isBreak = true;

                        this.scoreMgr.AddScore(enemy.OptionValue);

                        break;
                    }
                }

                if (isBreak)
                {
                    continue;
                }

                // 自機の弾を移動
                Canvas.SetTop(ownBullet, Canvas.GetTop(ownBullet) - 7);
            }
        }

        /// <summary>敵の死亡モーションを管理するリスト</summary>
        private List<TimerdImage> deadEnemyList = new List<TimerdImage>();

        /// <summary>
        /// 自機の弾を除去します。
        /// </summary>
        private void removeOwnBullet(Image ownBullet)
        {
            this.Canvas.Children.Remove(ownBullet);
            this.ownBulletList.Remove(ownBullet);
        }

        //
        // 自機の弾の爆発
        // - - - - - - - - - - - - - - - - - - - -

        /// <summary>
        /// 自機の弾の爆発表示を行います。
        /// </summary>
        private void putOwnBulletExp(Image ownBullet)
        {
            var ownExp = new Image()
            {
                Source = new BitmapImage(new Uri("/Assets/own_bullet_b.png", UriKind.Relative)),
                Width = 24,
                Height = 24,
            };

            this.Canvas.Children.Add(ownExp);
            this.ownBulletExpList.Add(new TimerdImage() { Image = ownExp });

            //Canvas.SetTop(ownExp, 0);
            Canvas.SetLeft(ownExp, Canvas.GetLeft(ownBullet) - ownExp.Width / 2);
        }

        /// <summary>
        /// 自機の弾の爆発表示の更新を行います。
        /// </summary>
        private void updateOwnBulletExp()
        {
            for(int i = this.ownBulletExpList.Count; i > 0; i--)
            {
                var bulletExp = this.ownBulletExpList[i - 1];

                if(bulletExp.Elapsed > 30)
                {
                    // 取り除く
                    this.Canvas.Children.Remove(bulletExp.Image);
                    this.ownBulletExpList.Remove(bulletExp);
                    continue;
                }

                // 時間を進める
                bulletExp.Elapsed++;
            }
        }

        //
        // 敵機関係
        // - - - - - - - - - - - - - - - - - - - -

        /// <summary>
        /// 敵を更新します。
        /// </summary>
        private void updateEnemy()
        {
            if(this.currentDistance > this.moveInterval)
            {
                foreach(var enemy in this.enemyList)
                {
                    enemy.UpdateNext();
                }

                this.currentDistance = 0;
            }
            else
            {
                this.currentDistance++;
            }
        }

        /// <summary>
        /// 敵の爆発を更新します。
        /// </summary>
        private void updateEnemyExp()
        {
            for (int i = this.enemyExpList.Count; i > 0; i--)
            {
                var enemy = this.enemyExpList[i - 1];

                if (enemy.Elapsed > 25)
                {
                    this.Canvas.Children.Remove(enemy.Image);
                    this.enemyExpList.Remove(enemy);
                }
                else
                {
                    enemy.Elapsed++;
                }
            }
        }
    }

    /// <summary>
    /// スコアを管理するクラス
    /// </summary>
    public class ScoreManager
    {
        /// <summary>実際にスコアを表示する場所</summary>
        private TextBlock target;
        /// <summary>現在の得点</summary>
        private int score;

        /// <summary>
        /// 表示先の TextBlock を指定してオブジェクトを初期化します。
        /// </summary>
        public ScoreManager(TextBlock target)
        {
            this.target = target;
        }

        /// <summary>
        /// スコア表示を初期化します。
        /// </summary>
        public void InitScore()
        {
            this.score = 0;
            this.AddScore(0);
        }

        /// <summary>
        /// 指定した値をスコアに加算します。
        /// </summary>
        public void AddScore(int val)
        {
            this.score += val;
            this.target.Text = this.score.ToString("0000");
        }
    }

    /// <summary>
    /// アニメーションする画像を表します。
    /// </summary>
    public class AnimationImage
    {
        //
        // Fielsd
        // - - - - - - - - - - - - - - - - - - - -

        /// <summary>描画先のCanvas</summary>
        private Canvas canvas;
        /// <summary>画像リスト</summary>
        private List<Image> imageList = new List<Image>();
        /// <summary>現在のフレーム数</summary>
        private int currentFrame;

        //
        // Props
        // - - - - - - - - - - - - - - - - - - - -

        /// <summary>
        /// 現在表示中の画像を取得します。
        /// </summary>
        public Image CurrentImage { get; private set; }

        /// <summary>
        /// 任意の整数を設定できるフィールド
        /// </summary>
        public int OptionValue { get; set; }

        //
        // Constructors
        // - - - - - - - - - - - - - - - - - - - -

        /// <summary>
        /// 画像描画先の Canvas を指定してオブジェクトを初期化します。
        /// </summary>
        public AnimationImage(Canvas canvas)
        {
            this.canvas = canvas;
        }

        //
        // Public Methods
        // - - - - - - - - - - - - - - - - - - - -

        /// <summary>
        /// 画像を追加します。
        /// </summary>
        public void AddImage(string path, int width, int height)
        {
            var img = new Image()
            {
                Source = new BitmapImage(new Uri(path, UriKind.Relative)),
                Width = width,
                Height = height,
            };

            this.imageList.Add(img);
        }

        /// <summary>
        /// 表示を初期化します。
        /// </summary>
        public void Init()
        {
            if(this.imageList.Count == 0)
            {
                return;
            }

            // もし何か表示していたら全部削除
            foreach(Image img in this.imageList)
            {
                if (this.canvas.Children.Contains(img))
                {
                    this.canvas.Children.Remove(img);
                }
            }

            // フレーム数を初期化
            this.currentFrame = 0;

            // 自分の管理値を初期化
            this.CurrentImage = this.imageList[0];
            this.canvas.Children.Add(this.CurrentImage);
        }

        /// <summary>
        /// 次の画像へ切り替えます
        /// </summary>
        public void UpdateNext()
        {
            this.canvas.Children.Remove(this.CurrentImage);

            double x = Canvas.GetLeft(this.CurrentImage);
            double y = Canvas.GetTop(this.CurrentImage);

            if (this.currentFrame + 1 >= this.imageList.Count)
            {
                this.currentFrame = 0;
            }
            else
            {
                this.currentFrame++;
            }

            this.CurrentImage = this.imageList[this.currentFrame];
            this.canvas.Children.Add(this.CurrentImage);

            Canvas.SetTop(this.CurrentImage, y);
            Canvas.SetLeft(this.CurrentImage, x);
        }
    }

    /// <summary>
    /// 整数と画像の組み合わせを表します。
    /// </summary>
    public class TimerdImage
    {
        /// <summary>
        /// 経過時間を設定または取得します。
        /// </summary>
        public int Elapsed { get; set; }

        /// <summary>
        /// 画像を設定または取得します。
        /// </summary>
        public Image Image { get; set; }
    }
}
