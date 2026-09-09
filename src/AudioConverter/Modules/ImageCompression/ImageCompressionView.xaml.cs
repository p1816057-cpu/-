using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Media;
using AudioConverter.Common;

namespace AudioConverter.Modules.ImageCompression
{
    public partial class ImageCompressionView : UserControl
    {
        public ImageCompressionView()
        {
            InitializeComponent();
            DataContextChanged += (s, e) =>
            {
                if (DataContext is ImageCompressionViewModel viewModel)
                {
                    viewModel.PropertyChanged -= OnViewModelPropertyChanged;
                    viewModel.PropertyChanged += OnViewModelPropertyChanged;
                }
            };
            Loaded += (s, e) =>
            {
                if (DataContext is ImageCompressionViewModel viewModel)
                {
                    AnimateUploadLayout(viewModel.IsEmpty);
                }
            };
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ImageCompressionViewModel.IsEmpty) && IsLoaded)
            {
                Dispatcher.BeginInvoke(new System.Action(() =>
                {
                    if (sender is ImageCompressionViewModel vm)
                    {
                        AnimateUploadLayout(vm.IsEmpty);
                    }
                }));
            }
        }

        private void AnimateUploadLayout(bool isEmpty)
        {
            var topRow = ImageLeftGrid.RowDefinitions[0];
            if (isEmpty)
            {
                topRow.BeginAnimation(RowDefinition.HeightProperty, null);
                topRow.Height = new GridLength(1, GridUnitType.Star);
                return;
            }

            if (topRow.Height.IsStar)
            {
                double from = topRow.ActualHeight > 0
                    ? topRow.ActualHeight
                    : Math.Max(160, ImageLeftGrid.ActualHeight / 3);
                var animation = new GridLengthAnimation
                {
                    From = new GridLength(from, GridUnitType.Pixel),
                    To = new GridLength(118, GridUnitType.Pixel),
                    Duration = TimeSpan.FromMilliseconds(280)
                };
                topRow.BeginAnimation(RowDefinition.HeightProperty, animation);
            }
        }

        private void OnDragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
                e.Handled = true;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }

        private void OnDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop) &&
                DataContext is ImageCompressionViewModel vm)
            {
                vm.AddPaths(e.Data.GetData(DataFormats.FileDrop) as string[]);
            }

            e.Handled = true;
        }

        private void OnPreviousClick(object sender, RoutedEventArgs e)
        {
            if (!(DataContext is ImageCompressionViewModel viewModel))
            {
                return;
            }

            FadeInPreview();
            viewModel.MovePrevious();
        }

        private void OnNextClick(object sender, RoutedEventArgs e)
        {
            if (!(DataContext is ImageCompressionViewModel viewModel))
            {
                return;
            }

            FadeInPreview();
            viewModel.MoveNext();
        }

        private void FadeInPreview()
        {
            PreviewHost.Opacity = 0;
            var animation = new DoubleAnimation(0, 1, System.TimeSpan.FromMilliseconds(120));
            animation.EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut };
            PreviewHost.BeginAnimation(System.Windows.UIElement.OpacityProperty, animation);
        }
    }
}
