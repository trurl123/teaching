using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace TennisGame
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private double racketWidth; //ширина ракетки
		private double racketHeight; //высота ракетки
		private double ballSize; //ширина и высота мяча
		private int leftDirection; //направление левой ракетки (0,-1,1)
		private int rightDirection; //направление правой ракетки (0,-1,1)
		private double ballDirectionX; //направление мяча по оси X
		private double ballDirectionY; //направление мяча по оси Y
		private DispatcherTimer movingTimer; //таймер движения
		private readonly KeyboardDevice keyboardDevice = Keyboard.PrimaryDevice;
		private const double initialSpeed = 0.03; //начальная скорость
		private double speed = initialSpeed; 
		private const double speedMultiplier = 1.1; //коэфф. увеличения скорости
		private const int tick = 50; //интервал таймера в миллисекундах
		private readonly Random random = new Random(); //генератор случаных чисел
		private Side? lastFailedSide; //последняя сторона, которой забили гол
		private int leftScore; //счет левого игрока
		private int rightScore; //счет правого игрока
		private const int maxScore = 10; //максимальный счет

		private double MoveCoef => speed*tick*racketWidth;

		private enum Side
		{
			Left,
			Right
		}

		private enum GameState
		{
			Stoped,
			Playing,
			Goal,
			Finished,
		}

		private GameState state = GameState.Stoped;

		public MainWindow()
		{
			InitializeComponent();
		}

		private void MovingObjectsEvent(object sender, EventArgs eventArgs)
		{
			MoveRacket(LeftRacket, leftDirection);
			MoveRacket(RightRacket, rightDirection);
			MoveBall();
		}


		private void MoveBall()
		{
			if (state != GameState.Playing) //если не идет игра, то мяч не двигаем
				return;
			var top = Canvas.GetTop(Ball); //текущая координата Y мяча
			var newTop = top + ballDirectionY * MoveCoef; //предполгагаемая координата Y мяча
			if (newTop <= 0) //если мяч улетает вверх, то он должен начать двигаться вниз
			{
				newTop = 0;
				ballDirectionY = Math.Abs(ballDirectionY);
			}
			if (newTop >= Field.ActualHeight - ballSize) //если мяч улетает вниз, то он должен начать двигаться вверх
			{
				newTop = Field.ActualHeight - ballSize;
				ballDirectionY = -Math.Abs(ballDirectionY);
			}
			var diff = Math.Abs(newTop - top);
			if (diff > 0.5) 
			{
				Canvas.SetTop(Ball, newTop);
			}
				
			var left = Canvas.GetLeft(Ball);
			var newLeft = left + ballDirectionX * MoveCoef;
			if (newLeft < racketWidth*2)
			{
				if (!IsBallInsideRacket(newTop, LeftRacket))
				{
					ShowGoal(ref leftScore, LeftScore, Side.Left);
					return;
				}
				newLeft = racketWidth * 2;
				ballDirectionX = Math.Abs(ballDirectionX);
				speed *= speedMultiplier;
			}
			if (newLeft > Field.ActualWidth - ballSize - racketWidth * 2)
			{
				if (!IsBallInsideRacket(newTop, RightRacket))
				{
					ShowGoal(ref rightScore, RightScore, Side.Right);
					return;
				}
				newLeft = Field.ActualWidth - ballSize - racketWidth * 2;
				ballDirectionX = -Math.Abs(ballDirectionX);
				speed *= speedMultiplier;
			}
			diff = Math.Abs(newLeft - left);
			if (diff > 0.5)
			{
				Canvas.SetLeft(Ball, newLeft);
			}
		}

		private void ShowGoal(ref int score, TextBlock textBlock, Side side)
		{
			lastFailedSide = side;
			score++;
			textBlock.Text = score.ToString();
			if (score < maxScore)
				state = GameState.Goal;
			else
			{
				state = GameState.Finished;
				MessageBox.Show(this, "Игра закончена, победил игрок " + (side == Side.Left ? "справа" : "слева") );
				state = GameState.Stoped;
				leftScore = 0;
				rightScore = 0;
				LeftScore.Text = "0";
				RightScore.Text = "0";
			}
			speed = initialSpeed;
			InitObjects();
		}

		private bool IsBallInsideRacket(double top, Rectangle racket)
		{
			var racketTop = Canvas.GetTop(racket);
			return top > racketTop - ballSize && top < racketTop + racketHeight;
		}

		private void MoveRacket(Rectangle racket, int direction)
		{
			if (direction == 0)
				return;
			var top = Canvas.GetTop(racket);
			//var newTop = direction > 0 ? Field.ActualHeight - racketHeight : 0;
			var newTop = top + direction * MoveCoef;
			if (newTop < 0)
				newTop = 0;
			if (newTop > Field.ActualHeight - racketHeight)
				newTop = Field.ActualHeight - racketHeight;
			var diff = Math.Abs(newTop - top);
			if (diff > 0.5)
			{
				Canvas.SetTop(racket, newTop);
			}
		}

		private void ChangeDirection(out int direction, Key keyUp, Key keyDown)
		{
			var isUp = (keyboardDevice.GetKeyStates(keyUp) & KeyStates.Down) == KeyStates.Down;
			var isDown = (keyboardDevice.GetKeyStates(keyDown) & KeyStates.Down) == KeyStates.Down;
			if (isUp == isDown)
				direction = 0;
			else if (isUp)
				direction = -1;
			else
				direction = 1;
		}

		private void ChangeDirections()
		{
			ChangeDirection(out leftDirection, Key.W, Key.S);
			ChangeDirection(out rightDirection, Key.Up, Key.Down);
			if (state != GameState.Playing && (leftDirection != 0 || rightDirection != 0))
			{
				state = GameState.Playing;
				ballDirectionX = Canvas.GetLeft(Ball) > Field.ActualWidth / 2 ? -1 : 1;
				ballDirectionY = leftDirection != 0 ? leftDirection : rightDirection;
			}
		}

		private void Window_KeyDown(object sender, KeyEventArgs e)
		{
			ChangeDirections();
		}

		private void Window_Initialized(object sender, EventArgs e)
		{
			InitObjects();
			movingTimer = new DispatcherTimer();
			movingTimer.Tick += MovingObjectsEvent;
			movingTimer.Interval = TimeSpan.FromMilliseconds(tick);
			movingTimer.Start();
		}

		private void InitObjects()
		{
			racketWidth = Field.ActualWidth / 100;
			ballSize = racketWidth;
			racketHeight = racketWidth * 10;
			LeftRacket.Width = racketWidth;
			RightRacket.Width = racketWidth;
			LeftRacket.Height = racketHeight;
			RightRacket.Height = racketHeight;
			Ball.Width = racketWidth;
			Ball.Height = racketWidth;
			Canvas.SetLeft(LeftRacket, racketWidth);
			Canvas.SetTop(LeftRacket, Field.ActualHeight / 2 - racketHeight / 2);
			Canvas.SetLeft(RightRacket, Field.ActualWidth - racketWidth * 2);
			Canvas.SetTop(RightRacket, Field.ActualHeight / 2 - racketHeight / 2);
			if (lastFailedSide == null)
			{
				lastFailedSide = random.Next(0, 2) == 0 ? Side.Left : Side.Right;
			}
			if (lastFailedSide == Side.Left)
				Canvas.SetLeft(Ball, ballSize * 2);
			else
				Canvas.SetLeft(Ball, Field.ActualWidth - ballSize * 3);
			Canvas.SetTop(Ball, Field.ActualHeight / 2 - ballSize / 2);
		}

		private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			InitObjects();
		}

		private void Window_KeyUp(object sender, KeyEventArgs e)
		{
			ChangeDirections();
		}
	}
}
