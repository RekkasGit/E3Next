using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.UI.Progress
{
	using System;
	using System.Drawing;
	using System.Windows.Forms;
	public enum AnimationDirection { LeftToRight, RightToLeft }
	public class LoadingDotsProgress : Control
	{
		private Timer animationTimer;
		//private int animationStep = 0;
		private int dotCount = 10;
		private int dotSize = 4;
		//private int dotSpacing = 6;
		private Color dotColor = Color.DodgerBlue;
		private AnimationDirection direction = AnimationDirection.LeftToRight;
		[System.ComponentModel.Category("Appearance")]
		[System.ComponentModel.Description("Sets the direction of the dot animation.")]
		public AnimationDirection Direction
		{
			get => direction;
			set
			{
				direction = value;
				this.Invalidate(); // Redraw immediately when changed
			}
		}
		[System.ComponentModel.Category("Behavior")]
		[System.ComponentModel.Description("Sets the speed of the animation in milliseconds. Higher is slower.")]
		public int AnimationSpeed
		{
			get => animationTimer.Interval;
			set
			{
				// Ensure the value is at least 10ms to prevent crashes
				animationTimer.Interval = Math.Max(10, value);
			}
		}
		[System.ComponentModel.Category("Appearance")]
		[System.ComponentModel.Description("Sets the number of dots to display.")]
		public int DotCount
		{
			get => dotCount;
			set
			{
				// Ensure at least 1 dot to avoid division by zero errors
				dotCount = Math.Max(1, value);
				this.Invalidate();
			}
		}
		public LoadingDotsProgress()
		{
			this.SetStyle(ControlStyles.AllPaintingInWmPaint |
						  ControlStyles.UserPaint |
						  ControlStyles.OptimizedDoubleBuffer |
						  ControlStyles.ResizeRedraw, true);

			// 2. Set the background to a dark gray/black
			this.BackColor = Color.FromArgb(30, 30, 30);

			animationTimer = new Timer();
			animationTimer.Interval = 150;
			animationTimer.Tick += AnimationTimer_Tick;
			animationTimer.Start();
		}
		private void AnimationTimer_Tick(object sender, EventArgs e)
		{
			// 0.02 is the increment speed; smaller is slower
			animationOffset += 0.05f;
			if (animationOffset > 1) animationOffset = 0;
			this.Invalidate();
		}

		private float animationOffset = 0; // Use float for smoother movement
		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);
			Graphics g = e.Graphics;
			g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

			using (SolidBrush brush = new SolidBrush(this.dotColor))
			{
				for (int i = 0; i < dotCount; i++)
				{
					// 1. Staggered time (Keep this small for tight grouping)
					float dotTime = (animationOffset - (i * 0.03f));
					if (dotTime < 0) dotTime += 1;

					// 2. The "Windows 10 Cluster" Math (Cubic Easing)
					// This causes the dots to slow down significantly in the middle (0.5)
					// and accelerate rapidly toward the edges (0 and 1).
					float xPercentage;
					if (dotTime < 0.5f)
					{
						xPercentage = 4 * dotTime * dotTime * dotTime;
					}
					else
					{
						float f = ((2 * dotTime) - 2);
						xPercentage = 0.5f * f * f * f + 1;
					}

					// 3. Draw the dots
					// Note: We subtract dotSize from width so they don't clip at the very edge
					int x = (int)(xPercentage * (this.Width - dotSize));
					int y = (this.Height - dotSize) / 2;

					if (x > -dotSize && x < this.Width)
					{
						g.FillEllipse(brush, x, y, dotSize, dotSize);
					}
				}
			}
		}

		// Safely dispose of the timer when the control is removed
		protected override void Dispose(bool disposing)
		{
			if (disposing && animationTimer != null)
			{
				animationTimer.Stop();
				animationTimer.Dispose();
			}
			base.Dispose(disposing);
		}
	}
}
