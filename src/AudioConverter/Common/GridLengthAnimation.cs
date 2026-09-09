using System;
using System.Windows;
using System.Windows.Media.Animation;

namespace AudioConverter.Common
{
    public sealed class GridLengthAnimation : AnimationTimeline
    {
        public static readonly DependencyProperty FromProperty =
            DependencyProperty.Register("From", typeof(GridLength), typeof(GridLengthAnimation));

        public static readonly DependencyProperty ToProperty =
            DependencyProperty.Register("To", typeof(GridLength), typeof(GridLengthAnimation));

        public GridLength From
        {
            get { return (GridLength)GetValue(FromProperty); }
            set { SetValue(FromProperty, value); }
        }

        public GridLength To
        {
            get { return (GridLength)GetValue(ToProperty); }
            set { SetValue(ToProperty, value); }
        }

        public override Type TargetPropertyType
        {
            get { return typeof(GridLength); }
        }

        public override object GetCurrentValue(object defaultOriginValue, object defaultDestinationValue, AnimationClock animationClock)
        {
            if (animationClock == null || animationClock.CurrentProgress == null)
            {
                return To;
            }

            double progress = animationClock.CurrentProgress.Value;
            progress = Math.Max(0, Math.Min(1, progress));
            progress = 1 - (1 - progress) * (1 - progress);

            double from = From.IsStar ? 320 : From.Value;
            double to = To.IsStar ? 320 : To.Value;
            double value = from + (to - from) * progress;

            if (To.IsStar)
            {
                return new GridLength(1, GridUnitType.Star);
            }

            return new GridLength(value, GridUnitType.Pixel);
        }

        protected override Freezable CreateInstanceCore()
        {
            return new GridLengthAnimation();
        }
    }
}
