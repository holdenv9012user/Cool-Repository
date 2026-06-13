using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SimpleFlappyBird
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new GameForm());
        }
    }

    internal sealed class GameForm : Form
    {
        private readonly Timer timer = new Timer();
        private readonly Random random = new Random();
        private readonly List<PipePair> pipes = new List<PipePair>();
        private readonly Font titleFont = new Font("Segoe UI", 28f, FontStyle.Bold);
        private readonly Font hudFont = new Font("Segoe UI", 15f, FontStyle.Bold);
        private readonly Font smallFont = new Font("Segoe UI", 10f, FontStyle.Regular);
        private float birdY;
        private float velocity;
        private int score;
        private int bestScore;
        private bool started;
        private bool gameOver;
        private const int WidthPx = 480;
        private const int HeightPx = 640;
        private const int GroundHeight = 86;
        private const int BirdX = 116;
        private const int BirdSize = 32;
        private const float Gravity = 0.42f;
        private const float FlapVelocity = -7.6f;
        private const float PipeSpeed = 2.75f;
        private const int PipeWidth = 70;
        private const int PipeGap = 168;
        private const int PipeSpacing = 245;

        public GameForm()
        {
            Text = "Simple Flappy Bird Clone";
            ClientSize = new Size(WidthPx, HeightPx);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            DoubleBuffered = true;
            BackColor = Color.FromArgb(115, 205, 236);
            KeyPreview = true;

            timer.Interval = 16;
            timer.Tick += delegate { TickGame(); };
            KeyDown += OnKeyDown;
            MouseDown += delegate { FlapOrRestart(); };

            ResetGame();
            timer.Start();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                timer.Dispose();
                titleFont.Dispose();
                hudFont.Dispose();
                smallFont.Dispose();
            }
            base.Dispose(disposing);
        }

        private void ResetGame()
        {
            birdY = 250;
            velocity = 0;
            score = 0;
            started = false;
            gameOver = false;
            pipes.Clear();
            for (int i = 0; i < 4; i++)
            {
                pipes.Add(NewPipe(WidthPx + 80 + i * PipeSpacing));
            }
            Invalidate();
        }

        private PipePair NewPipe(float x)
        {
            int topHeight = random.Next(78, HeightPx - GroundHeight - PipeGap - 88);
            return new PipePair(x, topHeight, false);
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Up || e.KeyCode == Keys.W)
            {
                FlapOrRestart();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.R)
            {
                ResetGame();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Escape)
            {
                Close();
            }
        }

        private void FlapOrRestart()
        {
            if (gameOver)
            {
                ResetGame();
                started = true;
            }
            else
            {
                started = true;
                velocity = FlapVelocity;
            }
        }

        private void TickGame()
        {
            if (!started || gameOver)
            {
                Invalidate();
                return;
            }

            velocity += Gravity;
            birdY += velocity;

            float farthestX = 0;
            for (int i = 0; i < pipes.Count; i++)
            {
                PipePair pipe = pipes[i];
                pipe.X -= PipeSpeed;
                if (pipe.X > farthestX) farthestX = pipe.X;

                if (!pipe.Scored && pipe.X + PipeWidth < BirdX)
                {
                    pipe.Scored = true;
                    score++;
                    if (score > bestScore) bestScore = score;
                }

                pipes[i] = pipe;
            }

            for (int i = 0; i < pipes.Count; i++)
            {
                if (pipes[i].X + PipeWidth < -10)
                {
                    pipes[i] = NewPipe(farthestX + PipeSpacing);
                    farthestX += PipeSpacing;
                }
            }

            if (Collides())
            {
                gameOver = true;
                started = false;
            }

            Invalidate();
        }

        private bool Collides()
        {
            RectangleF bird = BirdBounds();
            if (bird.Top < 0 || bird.Bottom > HeightPx - GroundHeight + 2) return true;

            foreach (PipePair pipe in pipes)
            {
                RectangleF topPipe = new RectangleF(pipe.X, 0, PipeWidth, pipe.TopHeight);
                RectangleF bottomPipe = new RectangleF(pipe.X, pipe.TopHeight + PipeGap, PipeWidth, HeightPx - GroundHeight - pipe.TopHeight - PipeGap);
                if (bird.IntersectsWith(topPipe) || bird.IntersectsWith(bottomPipe)) return true;
            }
            return false;
        }

        private RectangleF BirdBounds()
        {
            return new RectangleF(BirdX - BirdSize / 2f, birdY - BirdSize / 2f, BirdSize, BirdSize);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            DrawSky(g);
            DrawPipes(g);
            DrawGround(g);
            DrawBird(g);
            DrawHud(g);
        }

        private void DrawSky(Graphics g)
        {
            using (LinearGradientBrush brush = new LinearGradientBrush(ClientRectangle, Color.FromArgb(105, 199, 235), Color.FromArgb(185, 232, 244), LinearGradientMode.Vertical))
            {
                g.FillRectangle(brush, ClientRectangle);
            }

            DrawCloud(g, 62, 90, 1.0f);
            DrawCloud(g, 310, 140, 0.8f);
            DrawCloud(g, 210, 58, 0.65f);
        }

        private void DrawCloud(Graphics g, float x, float y, float scale)
        {
            using (SolidBrush brush = new SolidBrush(Color.FromArgb(205, 255, 255, 255)))
            {
                g.FillEllipse(brush, x, y + 14 * scale, 64 * scale, 30 * scale);
                g.FillEllipse(brush, x + 22 * scale, y, 46 * scale, 42 * scale);
                g.FillEllipse(brush, x + 52 * scale, y + 12 * scale, 58 * scale, 34 * scale);
            }
        }

        private void DrawPipes(Graphics g)
        {
            foreach (PipePair pipe in pipes)
            {
                DrawPipe(g, pipe.X, 0, pipe.TopHeight, true);
                DrawPipe(g, pipe.X, pipe.TopHeight + PipeGap, HeightPx - GroundHeight - pipe.TopHeight - PipeGap, false);
            }
        }

        private void DrawPipe(Graphics g, float x, float y, float height, bool top)
        {
            RectangleF body = new RectangleF(x, y, PipeWidth, height);
            RectangleF lip = top
                ? new RectangleF(x - 8, y + height - 22, PipeWidth + 16, 22)
                : new RectangleF(x - 8, y, PipeWidth + 16, 22);

            using (LinearGradientBrush brush = new LinearGradientBrush(body, Color.FromArgb(55, 181, 80), Color.FromArgb(25, 124, 52), LinearGradientMode.Horizontal))
            using (Pen outline = new Pen(Color.FromArgb(17, 92, 36), 3f))
            using (SolidBrush lipBrush = new SolidBrush(Color.FromArgb(75, 205, 95)))
            {
                g.FillRectangle(brush, body);
                g.DrawRectangle(outline, body.X, body.Y, body.Width, body.Height);
                g.FillRectangle(lipBrush, lip);
                g.DrawRectangle(outline, lip.X, lip.Y, lip.Width, lip.Height);
            }
        }

        private void DrawGround(Graphics g)
        {
            int y = HeightPx - GroundHeight;
            using (SolidBrush grass = new SolidBrush(Color.FromArgb(96, 196, 82)))
            using (SolidBrush dirt = new SolidBrush(Color.FromArgb(214, 177, 112)))
            using (Pen grassLine = new Pen(Color.FromArgb(52, 134, 58), 3f))
            {
                g.FillRectangle(grass, 0, y, WidthPx, 18);
                g.DrawLine(grassLine, 0, y, WidthPx, y);
                g.FillRectangle(dirt, 0, y + 18, WidthPx, GroundHeight - 18);
            }

            using (Pen stripe = new Pen(Color.FromArgb(188, 145, 84), 2f))
            {
                for (int x = -20; x < WidthPx + 40; x += 34)
                {
                    g.DrawLine(stripe, x, y + 42, x + 18, y + 24);
                }
            }
        }

        private void DrawBird(Graphics g)
        {
            RectangleF bird = BirdBounds();
            g.TranslateTransform(BirdX, birdY);
            g.RotateTransform(Math.Max(-22, Math.Min(30, velocity * 3f)));
            g.TranslateTransform(-BirdX, -birdY);

            using (SolidBrush body = new SolidBrush(Color.FromArgb(255, 214, 58)))
            using (SolidBrush wing = new SolidBrush(Color.FromArgb(245, 165, 42)))
            using (SolidBrush beak = new SolidBrush(Color.FromArgb(244, 112, 42)))
            using (SolidBrush eye = new SolidBrush(Color.White))
            using (SolidBrush pupil = new SolidBrush(Color.FromArgb(35, 35, 35)))
            using (Pen outline = new Pen(Color.FromArgb(107, 79, 24), 2.5f))
            {
                g.FillEllipse(body, bird);
                g.DrawEllipse(outline, bird);
                g.FillEllipse(wing, BirdX - 12, birdY + 2, 18, 12);
                PointF[] beakPoints = { new PointF(BirdX + 12, birdY - 3), new PointF(BirdX + 31, birdY + 4), new PointF(BirdX + 12, birdY + 11) };
                g.FillPolygon(beak, beakPoints);
                g.DrawPolygon(outline, beakPoints);
                g.FillEllipse(eye, BirdX + 4, birdY - 12, 10, 10);
                g.FillEllipse(pupil, BirdX + 8, birdY - 8, 4, 4);
            }

            g.ResetTransform();
        }

        private void DrawHud(Graphics g)
        {
            string scoreText = score.ToString();
            SizeF scoreSize = g.MeasureString(scoreText, titleFont);
            DrawShadowText(g, scoreText, titleFont, WidthPx / 2f - scoreSize.Width / 2f, 22, Color.White);
            DrawShadowText(g, "Best " + bestScore, smallFont, 14, 14, Color.White);

            if (!started && !gameOver)
            {
                DrawCenteredPanel(g, "FLAPPY BIRD", "Space, Up, W, or click to flap");
            }
            else if (gameOver)
            {
                DrawCenteredPanel(g, "GAME OVER", "Space or click to restart");
            }
        }

        private void DrawCenteredPanel(Graphics g, string title, string subtitle)
        {
            float panelW = 360;
            float panelH = 116;
            float panelX = (WidthPx - panelW) / 2f;
            float panelY = 220;
            using (SolidBrush panel = new SolidBrush(Color.FromArgb(168, 18, 38, 50)))
            using (Pen border = new Pen(Color.FromArgb(210, 255, 255, 255), 2f))
            {
                using (GraphicsPath path = RoundRect(panelX, panelY, panelW, panelH, 8))
                {
                    g.FillPath(panel, path);
                    g.DrawPath(border, path);
                }
            }

            SizeF titleSize = g.MeasureString(title, titleFont);
            SizeF subtitleSize = g.MeasureString(subtitle, hudFont);
            DrawShadowText(g, title, titleFont, WidthPx / 2f - titleSize.Width / 2f, panelY + 16, Color.White);
            DrawShadowText(g, subtitle, hudFont, WidthPx / 2f - subtitleSize.Width / 2f, panelY + 72, Color.FromArgb(232, 248, 255));
        }

        private void DrawShadowText(Graphics g, string text, Font font, float x, float y, Color color)
        {
            using (SolidBrush shadow = new SolidBrush(Color.FromArgb(120, 0, 0, 0)))
            using (SolidBrush brush = new SolidBrush(color))
            {
                g.DrawString(text, font, shadow, x + 2, y + 2);
                g.DrawString(text, font, brush, x, y);
            }
        }

        private GraphicsPath RoundRect(float x, float y, float width, float height, float radius)
        {
            float d = radius * 2f;
            GraphicsPath path = new GraphicsPath();
            path.AddArc(x, y, d, d, 180, 90);
            path.AddArc(x + width - d, y, d, d, 270, 90);
            path.AddArc(x + width - d, y + height - d, d, d, 0, 90);
            path.AddArc(x, y + height - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private struct PipePair
        {
            public float X;
            public int TopHeight;
            public bool Scored;

            public PipePair(float x, int topHeight, bool scored)
            {
                X = x;
                TopHeight = topHeight;
                Scored = scored;
            }
        }
    }
}
