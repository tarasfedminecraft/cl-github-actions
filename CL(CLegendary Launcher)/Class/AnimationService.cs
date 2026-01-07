using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Media;
using System.Windows;

namespace CL_CLegendary_Launcher_.Class
{
    public static class AnimationService
    {
        public static void AnimatePageTransition(UIElement element, double startOffset = 40, double durationSeconds = 0.4)
        {
            if (element == null) return;

            if (element.RenderTransform == null || element.RenderTransform is not TranslateTransform)
            {
                element.RenderTransform = new TranslateTransform();
            }

            var transform = (TranslateTransform)element.RenderTransform;

            element.Visibility = Visibility.Visible;
            element.Opacity = 0;
            transform.Y = startOffset; 

            var duration = TimeSpan.FromSeconds(durationSeconds);
            var easing = new CircleEase { EasingMode = EasingMode.EaseOut };

            DoubleAnimation opacityAnim = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = duration,
                EasingFunction = easing
            };

            DoubleAnimation slideAnim = new DoubleAnimation
            {
                From = startOffset, 
                To = 0,             
                Duration = duration,
                EasingFunction = easing
            };

            element.BeginAnimation(UIElement.OpacityProperty, opacityAnim);
            transform.BeginAnimation(TranslateTransform.YProperty, slideAnim);
        }
        public static void AnimatePageTransitionExit(UIElement element, double endOffset = 40, double durationSeconds = 0.3)
        {
            if (element == null) return;

            if (element.RenderTransform == null || element.RenderTransform is not TranslateTransform)
            {
                element.RenderTransform = new TranslateTransform();
            }

            var transform = (TranslateTransform)element.RenderTransform;
            var duration = TimeSpan.FromSeconds(durationSeconds);
            var easing = new CircleEase { EasingMode = EasingMode.EaseIn }; 

            DoubleAnimation opacityAnim = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = duration,
                EasingFunction = easing
            };

            DoubleAnimation slideAnim = new DoubleAnimation
            {
                From = 0,
                To = endOffset,
                Duration = duration,
                EasingFunction = easing
            };

            opacityAnim.Completed += (s, e) =>
            {
                element.Visibility = Visibility.Hidden;
            };

            element.BeginAnimation(UIElement.OpacityProperty, opacityAnim);
            transform.BeginAnimation(TranslateTransform.YProperty, slideAnim);
        }
        public static void FadeIn(UIElement element, double duration)
        {
            if (element == null) return;

            element.BeginAnimation(UIElement.OpacityProperty, null);

            var fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromSeconds(duration),
                FillBehavior = FillBehavior.HoldEnd,
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };

            element.Visibility = Visibility.Visible;

            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                element.BeginAnimation(UIElement.OpacityProperty, fadeIn);
            });
        }
        public static void FadeOut(UIElement element, double duration)
        {
            DoubleAnimation fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(duration));
            Storyboard.SetTarget(fadeOut, element);

            fadeOut.Completed += (s, e) =>
            {
                element.Visibility = Visibility.Collapsed;
            };

            Storyboard.SetTargetProperty(fadeOut, new PropertyPath(UIElement.OpacityProperty));
            Storyboard fadeOutStoryboard = new Storyboard();
            fadeOutStoryboard.Children.Add(fadeOut);
            fadeOutStoryboard.Begin();
        }
        public static void AnimateBorder(double targetX, double targetY, UIElement border)
        {
            DoubleAnimation moveXAnimation = new DoubleAnimation
            {
                To = targetX,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new QuarticEase()
            };

            DoubleAnimation moveYAnimation = new DoubleAnimation
            {
                To = targetY,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new QuarticEase()
            };

            border.RenderTransform.BeginAnimation(TranslateTransform.XProperty, moveXAnimation);
            border.RenderTransform.BeginAnimation(TranslateTransform.YProperty, moveYAnimation);
        }

        public static void AnimateBorderObject(double targetX, double targetY, Border border, bool visibly)
        {
            if (border.RenderTransform == null || border.RenderTransform is not TranslateTransform)
                border.RenderTransform = new TranslateTransform();

            if (visibly) border.Visibility = Visibility.Visible;

            var moveXAnimation = new DoubleAnimation
            {
                To = targetX,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new QuarticEase()
            };

            var moveYAnimation = new DoubleAnimation
            {
                To = targetY,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new QuarticEase()
            };

            moveXAnimation.Completed += (s, e) =>
            {
                if (!visibly) border.Visibility = Visibility.Hidden;
            };

            var transform = (TranslateTransform)border.RenderTransform;
            transform.BeginAnimation(TranslateTransform.XProperty, moveXAnimation);
            transform.BeginAnimation(TranslateTransform.YProperty, moveYAnimation);
        }
    }
}
