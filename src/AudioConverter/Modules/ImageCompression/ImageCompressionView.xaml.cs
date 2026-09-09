using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace AudioConverter.Modules.ImageCompression
{
    public partial class ImageCompressionView : UserControl
    {
        public ImageCompressionView()
        {
            InitializeComponent();
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
    }
}
