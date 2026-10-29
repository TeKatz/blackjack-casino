using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using BlackjackCasino.Core;

namespace BlackjackCasino.Desktop;

internal partial class NineLivesWindow
{
    private readonly TranslateTransform[] reelSpinTranslate = new TranslateTransform[NineLivesRules.Cells];
    private readonly BlurEffect[] reelSpinBlur = new BlurEffect[NineLivesRules.Cells];
    private readonly Border[] reelSpinGlow = new Border[NineLivesRules.Reels];
    private readonly Rectangle[] reelLightSweep = new Rectangle[NineLivesRules.Reels];
    private readonly TranslateTransform[] reelLightSweepTranslate = new TranslateTransform[NineLivesRules.Reels];
    private readonly bool[] reelMotionStopped = new bool[NineLivesRules.Reels];
    private readonly int[] reelSpinFrame = new int[NineLivesRules.Reels];

    private bool reelMotionInitialized;
    private bool reelMotionWasSpinning;
    private DateTime reelMotionStartedAt;

    protected override void OnClosed(EventArgs e)
    {
        CompositionTarget.Rendering -= ReelMotion_Rendering;
        base.OnClosed(e);
    }

    private void InitializeReelMotionPresentation()
    {
        if (reelMotionInitialized)
        {
            return;
        }

        reelMotionInitialized = true;

        for (int index = 0; index < NineLivesRules.Cells; index++)
        {
            ScaleTransform scale = symbolScale[index];
            symbolImages[index].RenderTransform = Transform.Identity;

            var translate = new TranslateTransform();
            reelSpinTranslate[index] = translate;

            var group = new TransformGroup();
            group.Children.Add(scale);
            group.Children.Add(translate);
            symbolImages[index].RenderTransform = group;

            reelSpinBlur[index] = new BlurEffect
            {
                Radius = 2.8,
                KernelType = KernelType.Gaussian,
                RenderingBias = RenderingBias.Performance,
            };
        }

        Brush sweepBrush = CreateReelSweepBrush();
        for (int reel = 0; reel < NineLivesRules.Reels; reel++)
        {
            var glow = new Border
            {
                Width = 269,
                Height = 633,
                CornerRadius = new CornerRadius(18),
                BorderThickness = new Thickness(2),
                BorderBrush = reel == 1 ? Violet : Gold,
                Background = Brushes.Transparent,
                IsHitTestVisible = false,
                Opacity = 0,
                Effect = new DropShadowEffect
                {
                    Color = reel == 1 ? Color.FromRgb(190, 77, 255) : Color.FromRgb(255, 213, 106),
                    BlurRadius = 30,
                    ShadowDepth = 0,
                    Opacity = 0.72,
                    RenderingBias = RenderingBias.Performance,
                },
            };
            reelSpinGlow[reel] = glow;
            Canvas.SetLeft(glow, ReelX[reel] - 6);
            Canvas.SetTop(glow, ReelY[0] - 6);
            Panel.SetZIndex(glow, -1);
            ReelCanvas.Children.Add(glow);

            var sweepTranslate = new TranslateTransform();
            reelLightSweepTranslate[reel] = sweepTranslate;
            var sweep = new Rectangle
            {
                Width = 243,
                Height = 92,
                RadiusX = 18,
                RadiusY = 18,
                Fill = sweepBrush,
                Opacity = 0,
                IsHitTestVisible = false,
                RenderTransform = sweepTranslate,
                Effect = new BlurEffect
                {
                    Radius = 9,
                    KernelType = KernelType.Gaussian,
                    RenderingBias = RenderingBias.Performance,
                },
            };
            reelLightSweep[reel] = sweep;
            Canvas.SetLeft(sweep, ReelX[reel] + 7);
            Canvas.SetTop(sweep, ReelY[0] - 34);
            Panel.SetZIndex(sweep, 10);
            ReelCanvas.Children.Add(sweep);
        }

        CompositionTarget.Rendering += ReelMotion_Rendering;
    }

    private void ReelMotion_Rendering(object? sender, EventArgs e)
    {
        if (!reelMotionInitialized)
        {
            return;
        }

        if (!AnimationsEnabled || !spinning)
        {
            if (reelMotionWasSpinning)
            {
                ResetReelMotionPresentation();
            }

            reelMotionWasSpinning = false;
            return;
        }

        if (!reelMotionWasSpinning)
        {
            BeginReelMotionPresentation();
        }

        reelMotionWasSpinning = true;
        double elapsed = (DateTime.UtcNow - reelMotionStartedAt).TotalSeconds;

        for (int reel = 0; reel < NineLivesRules.Reels; reel++)
        {
            if (!reelMotionStopped[reel] && ReelHasStartedSettling(reel))
            {
                StopReelMotion(reel);
            }

            if (reelMotionStopped[reel])
            {
                continue;
            }

            RenderSpinningReel(reel, elapsed);
        }
    }

    private void BeginReelMotionPresentation()
    {
        Array.Fill(reelMotionStopped, false);
        Array.Fill(reelSpinFrame, -1);
        reelMotionStartedAt = DateTime.UtcNow;

        for (int reel = 0; reel < NineLivesRules.Reels; reel++)
        {
            reelSpinGlow[reel].Opacity = 0.24;
            reelLightSweep[reel].Opacity = 0.26;

            for (int row = 0; row < NineLivesRules.Rows; row++)
            {
                int index = NineLivesRules.Index(row, reel);
                reelSpinTranslate[index].Y = 0;
                symbolImages[index].Effect = reelSpinBlur[index];
                RenderOptions.SetBitmapScalingMode(symbolImages[index], BitmapScalingMode.LowQuality);
            }
        }
    }

    private void RenderSpinningReel(int reel, double elapsed)
    {
        // Each reel owns its visual cadence. This keeps the still-spinning reels alive while
        // another reel is awaiting its settle/anticipation animation in AnimateReelsAsync.
        double frameDuration = 0.060 + (reel * 0.004);
        int frame = (int)(elapsed / frameDuration);
        double phase = (elapsed % frameDuration) / frameDuration;

        if (frame != reelSpinFrame[reel])
        {
            reelSpinFrame[reel] = frame;
            for (int row = 0; row < NineLivesRules.Rows; row++)
            {
                int index = NineLivesRules.Index(row, reel);
                SetSymbol(index, PreviewSymbols[visualRandom.Next(PreviewSymbols.Length)]);
            }
        }

        // Move continuously down through the cell, then swap the symbol as the next visual
        // frame enters. The clipped cell plus blur hides the wrap and reads like a real reel.
        double offset = -18.0 + (phase * 36.0);
        for (int row = 0; row < NineLivesRules.Rows; row++)
        {
            int index = NineLivesRules.Index(row, reel);
            reelSpinTranslate[index].Y = offset;
        }

        double pulse = 0.5 + (0.5 * Math.Sin((elapsed * 5.8) + (reel * 1.2)));
        reelSpinGlow[reel].Opacity = 0.21 + (pulse * 0.22);

        double sweepProgress = ((elapsed * 0.82) + (reel * 0.21)) % 1.0;
        reelLightSweepTranslate[reel].Y = sweepProgress * 584.0;
        reelLightSweep[reel].Opacity = 0.18 + ((1.0 - Math.Abs((sweepProgress * 2.0) - 1.0)) * 0.22);
    }

    private bool ReelHasStartedSettling(int reel)
    {
        for (int row = 0; row < NineLivesRules.Rows; row++)
        {
            Border cell = cells[NineLivesRules.Index(row, reel)];
            if (cell.BorderThickness.Left >= 3.0 && Equals(cell.BorderBrush, Gold))
            {
                return true;
            }
        }

        return false;
    }

    private void StopReelMotion(int reel)
    {
        reelMotionStopped[reel] = true;
        reelSpinGlow[reel].Opacity = 0;
        reelLightSweep[reel].Opacity = 0;
        reelLightSweepTranslate[reel].Y = 0;

        for (int row = 0; row < NineLivesRules.Rows; row++)
        {
            int index = NineLivesRules.Index(row, reel);
            reelSpinTranslate[index].Y = 0;
            symbolImages[index].Effect = null;
            RenderOptions.SetBitmapScalingMode(symbolImages[index], BitmapScalingMode.HighQuality);
        }
    }

    private void ResetReelMotionPresentation()
    {
        for (int reel = 0; reel < NineLivesRules.Reels; reel++)
        {
            reelMotionStopped[reel] = false;
            reelSpinFrame[reel] = -1;
            reelSpinGlow[reel].Opacity = 0;
            reelLightSweep[reel].Opacity = 0;
            reelLightSweepTranslate[reel].Y = 0;

            for (int row = 0; row < NineLivesRules.Rows; row++)
            {
                int index = NineLivesRules.Index(row, reel);
                reelSpinTranslate[index].Y = 0;
                symbolImages[index].Effect = null;
                RenderOptions.SetBitmapScalingMode(symbolImages[index], BitmapScalingMode.HighQuality);
            }
        }
    }

    private static Brush CreateReelSweepBrush()
    {
        var brush = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(0, 1),
        };
        brush.GradientStops.Add(new GradientStop(Color.FromArgb(0, 179, 77, 255), 0.0));
        brush.GradientStops.Add(new GradientStop(Color.FromArgb(62, 179, 77, 255), 0.30));
        brush.GradientStops.Add(new GradientStop(Color.FromArgb(92, 255, 213, 106), 0.50));
        brush.GradientStops.Add(new GradientStop(Color.FromArgb(62, 179, 77, 255), 0.70));
        brush.GradientStops.Add(new GradientStop(Color.FromArgb(0, 179, 77, 255), 1.0));
        brush.Freeze();
        return brush;
    }
}
