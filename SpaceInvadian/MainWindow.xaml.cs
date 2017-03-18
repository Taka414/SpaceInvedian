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
        /// <summary>敵の死亡モーションを管理するリスト</summary>
        private List<TimerdImage> deadEnemyList = new List<TimerdImage>();


        /// <summary>敵の移動速度が速くなる時間の感覚</summary>
        private int speedupPeriod = 240;
        /// <summary>移動速度のカウンタ</summary>
        private int currentTime;
        
        /// <summary>敵機が左右の端に達しているかのフラグ</summary>
        private bool isArraivalEnd;
        /// <summary></summary>
        private MoveDirection enemyMovingDirection;

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
                    this.moveOwn();

                    this.updateOwnBullet();

                    this.updateOwnBulletExp();

                    this.updateEnemyExp();

                    this.updateEnemy();

                    this.putEnemyBullet();

                    this.updateEnemyBullet();

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

            this.moveInterval = 60;

            this.isArraivalEnd = false;
            this.enemyMovingDirection = MoveDirection.Right;

            this.speedupPeriod = 240;
            this.currentTime = 0;
        }

        /// <summary>
        /// 自機の表示を初期化します。
        /// </summary>
        private void initOwn()
        {
            if (this.own != null)
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
            for (int i = 0; i < 11; i++)
            {
                var enemy = new AnimationImage(this.Canvas)
                {
                    OptionValue = 30,
                };
                enemy.AddImage("/Assets/inv_1_1.png", 24, 24);
                enemy.AddImage("/Assets/inv_1_2.png", 24, 24);
                enemy.Init();

                Canvas.SetTop(enemy.CurrentImage, 60);
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

                    Canvas.SetTop(enemy.CurrentImage, 104 + (enemy.CurrentImage.Height + 24) * i);
                    Canvas.SetLeft(enemy.CurrentImage, 15 + j * (24 + 22) + ((24 - 33) / 2));

                    this.enemyList.Add(enemy);
                }
            }

            // 4, 5列目
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 11; j++)
                {
                    var enemy = new AnimationImage(this.Canvas)
                    {
                        OptionValue = 10
                    };
                    enemy.AddImage("/Assets/inv_3_1.png", 27, 24);
                    enemy.AddImage("/Assets/inv_3_2.png", 27, 24);
                    enemy.Init();

                    Canvas.SetTop(enemy.CurrentImage, 204 + (enemy.CurrentImage.Height + 24) * i);
                    Canvas.SetLeft(enemy.CurrentImage, 15 + j * (24 + 22) + ((24 - 27) / 2));

                    this.enemyList.Add(enemy);
                }
            }
        }

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

            if (pos <= 0 || pos + this.own.Width >= this.Canvas.ActualWidth)
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
            if (this.ownBulletList.Count >= 1)
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
                Canvas.SetTop(ownBullet, Canvas.GetTop(ownBullet) - 8);
            }
        }

        /// <summary>
        /// 自機の弾を除去します。
        /// </summary>
        private void removeOwnBullet(Image ownBullet)
        {
            this.Canvas.Children.Remove(ownBullet);
            this.ownBulletList.Remove(ownBullet);
        }

        /// <summary>敵が弾を発射する間隔</summary>
        private int enemyBulletFireInterval = 40;
        /// <summary>敵の弾を発射するタイマー変数</summary>
        private int enemyBulletFireTimer = 0;
        /// <summary>敵の弾のリスト</summary>
        private List<AnimationImage> enemyBulletList = new List<AnimationImage>();

        /// <summary>
        /// 指定した座標上に敵の弾を画面上に配置します。
        /// </summary>
        private void putEnemyBullet()
        {
            // 弾の発射タイミングかどうかを判定
            //  - 一定秒数経過
            //  - 弾が画面上に3発以下なら発射
            if (this.enemyBulletFireTimer <= this.enemyBulletFireInterval || 
                this.enemyBulletList.Count >= 3)
            {
                this.enemyBulletFireTimer++;
                return;
            }

            // 弾を発射する敵の選定
            AnimationImage enemy_target = null;

            double own_left = Canvas.GetLeft(this.own);
            double own_right = own_left + this.own.Width;

            // 自分の直上の一番手前の選定
            foreach (var enemy in this.enemyList)
            {
                double enemy_left = Canvas.GetLeft(enemy.CurrentImage);
                double enemy_right = enemy_left + enemy.CurrentImage.Width;

                if(own_left >= enemy_left && own_left <= enemy_right ||
                   own_right >= enemy_left && own_right <= enemy_right)
                {
                    enemy_target = enemy;
                }
            }

            if(enemy_target == null)
            {
                return;
            }

            var enemyBullet = new AnimationImage(this.Canvas)
            {
                Delay = 10,
            };
            //enemyBullet.AddImage("/Assets/enm_b_1.png", 6, 14);
            enemyBullet.AddImage("/Assets/inv_1_1.png", 6, 14);
            enemyBullet.AddImage("/Assets/enm_b_2.png", 6, 14);
            enemyBullet.Init();

            Canvas.SetTop(enemyBullet.CurrentImage, Canvas.GetTop(enemy_target.CurrentImage) + enemy_target.CurrentImage.Height + 5);
            GameUtil.SetCenter(enemy_target.CurrentImage, enemyBullet.CurrentImage);

            this.enemyBulletList.Add(enemyBullet);

            this.enemyBulletFireTimer = 0;
        }

        /// <summary>
        /// 敵の弾を更新します。
        /// </summary>
        private void updateEnemyBullet()
        {
            for(int i = this.enemyBulletList.Count; i > 0; i--)
            {
                var enemyBullet = this.enemyBulletList[i - 1];

                // 自機に当たっているかを判定
                // あとで

                // 一番手前まで来たかどうか判定
                if(Canvas.GetTop(enemyBullet.CurrentImage) + enemyBullet.CurrentImage.Height >= this.Canvas.ActualHeight)
                {
                    // 爆発表現を追加
                    //this.putOwnBulletExp(enemyBullet.CurrentImage);

                    // 管理から取り除く
                    this.Canvas.Children.Remove(enemyBullet.CurrentImage);
                    this.enemyBulletList.Remove(enemyBullet);

                    continue;
                }

                // 移動処理
                Canvas.SetTop(enemyBullet.CurrentImage, Canvas.GetTop(enemyBullet.CurrentImage) + 3);
                enemyBullet.UpdateNext();
            }
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
            for (int i = this.ownBulletExpList.Count; i > 0; i--)
            {
                var bulletExp = this.ownBulletExpList[i - 1];

                if (bulletExp.Elapsed > 30)
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
            if (this.currentDistance > this.moveInterval)
            {
                // 描画の更新
                foreach (var enemy in this.enemyList)
                {
                    enemy.UpdateNext();
                }

                // 敵の移動
                if (this.isArraivalEnd)
                {
                    // 前進する
                    foreach (var enemy in this.enemyList)
                    {
                        Canvas.SetTop(enemy.CurrentImage, Canvas.GetTop(enemy.CurrentImage) + 5);
                    }

                    this.isArraivalEnd = false;

                    // 移動方向を入れ替え
                    if (this.enemyMovingDirection == MoveDirection.Left)
                    {
                        this.enemyMovingDirection = MoveDirection.Right;
                    }
                    else if (this.enemyMovingDirection == MoveDirection.Right)
                    {
                        this.enemyMovingDirection = MoveDirection.Left;
                    }
                }
                else
                {
                    // 左右に移動する
                    foreach (var enemy in this.enemyList)
                    {
                        double moveDistance = 5;
                        if (this.enemyMovingDirection == MoveDirection.Left)
                        {
                            moveDistance = moveDistance * -1; // 左へ移動を設定
                        }

                        Canvas.SetLeft(enemy.CurrentImage, Canvas.GetLeft(enemy.CurrentImage) + moveDistance);
                    }

                    // 左右にいる敵の
                    foreach (var enemy in this.enemyList)
                    {
                        // 全員のうち一番右に居るやるが画面右側に到着したらフラグ立てる
                        if (Canvas.GetLeft(enemy.CurrentImage) <= 0 || Canvas.GetLeft(enemy.CurrentImage) + enemy.CurrentImage.Width > this.Canvas.ActualWidth)
                        {
                            this.isArraivalEnd = true;
                            break;
                        }
                    }
                }

                this.currentDistance = 0;
            }
            else
            {
                this.currentDistance++;
            }

            // 敵の移動速度の高速化
            if (this.currentTime >= this.speedupPeriod)
            {
                // 一定速度以上は高速化しない
                if (this.moveInterval > 20)
                {

                    this.moveInterval -= 2;
                    this.currentTime = 0;

                    Console.WriteLine("Speed up!");
                }
            }
            else
            {
                this.currentTime++;
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

        /// <summary>
        /// Updateを何回呼べば次の画像へ移行するかを遅延する回数
        /// </summary>
        public int Delay { get; set; } = 1;

        /// <summary>
        /// 現在のスキップ回数
        /// </summary>
        public int DelayCurrent { get; private set; }

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
            if (this.imageList.Count == 0)
            {
                return;
            }

            // もし何か表示していたら全部削除
            foreach (Image img in this.imageList)
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
            if (!(this.DelayCurrent >= this.Delay))
            {
                this.DelayCurrent++;
                return;
            }

            this.DelayCurrent = 0;

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
