using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace AudioConverter.Modules.Conversion
{
    public partial class ConversionView : UserControl
    {
        public ConversionView()
        {
            InitializeComponent();
        }

        private void OnDragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
                if (DropArea != null)
                {
                    DropArea.Background = FindBrush("Brush.AccentSoft");
                    DropArea.BorderBrush = FindBrush("Brush.Accent");
                }
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }

            e.Handled = true;
        }

        private void OnDrop(object sender, DragEventArgs e)
        {
            if (DropArea != null)
            {
                DropArea.Background = FindBrush("Brush.SurfaceAlt");
                DropArea.BorderBrush = new SolidColorBrush(Color.FromRgb(0xD4, 0xDD, 0xE5));
            }

            if (e.Data.GetDataPresent(DataFormats.FileDrop) &&
                DataContext is ConversionViewModel viewModel)
            {
                var paths = e.Data.GetData(DataFormats.FileDrop) as string[];
                viewModel.AddPaths(paths);
            }

            e.Handled = true;
        }

        private static Brush FindBrush(string key)
        {
            return Application.Current?.TryFindResource(key) as Brush ?? Brushes.Transparent;
        }
    }
}
