using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using Units;
using Filesplit.Model;
using System.Runtime.InteropServices;

namespace Filesplit
{
    /// <summary>
    /// Interaktionslogik für ProgressDialogWindow.xaml
    /// </summary>
    public partial class ProgressDialogWindow : Window
    {
        private readonly ActionType action;
        private readonly string sourceFile;
        private readonly string destinationPath;
        private readonly int chunkSize;

        public ProgressDialogWindow(ActionType action, string sourceFile, string destinationPath, int chunkSize)
        {
            InitializeComponent();

            this.action = action;
            this.sourceFile = sourceFile;
            this.destinationPath = destinationPath;
            this.chunkSize = chunkSize;

            Loaded += ProgressDialogWindow_Loaded;
            FileOperations.ProgressChanged += FileOperations_ProgressChanged;
        }

        private void FileOperations_ProgressChanged(Progress progress)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                // Update progress bars
                try
                {
                    ProgressTotal.Value = progress.TotalProgress;
                    TextTotalProgress.Text = $"{ByteUnit.FindUnit(progress.TotalBytesProcessed)} / {ByteUnit.FindUnit(progress.TotalBytes)}";
                }
                catch
                {
                    // double.NaN
                }

                try
                {
                    ProgressCurrent.Value = progress.CurrentProgress;
                    TextCurrentProgress.Text = $"{ByteUnit.FindUnit(progress.CurrentBytesProcessed)} / {ByteUnit.FindUnit(progress.CurrentFileBytes)}";
                }
                catch
                {
                    // double.NaN
                }

            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        private async void ProgressDialogWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (action == ActionType.Split)
            {
                await Task.Factory.StartNew(new Action(async () =>
                {
                    try
                    {
                        await FileOperations.SplitFileAsync(sourceFile, destinationPath, chunkSize);

                        Dispatcher.Invoke(new Action(() =>
                        {
                            MessageBox.Show("Done! Your selected file was splitted successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                            Close();
                        }));
                    }
                    catch (Exception ex)
                    {
                        Dispatcher.Invoke(new Action(() =>
                        {
                            MessageBox.Show($"Failed to split the selected file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            Close();
                        }));
                    }
                }));
            }
            else if (action == ActionType.Merge)
            {
                await Task.Factory.StartNew(new Action(async () =>
                {
                    try
                    {
                        await FileOperations.MergeFileAsync(destinationPath);

                        Dispatcher.Invoke(new Action(() =>
                        {
                            MessageBox.Show("Done! Your file was restored successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                            Close();
                        }));
                    }
                    catch (Exception ex)
                    {
                        Dispatcher.Invoke(new Action(() =>
                        {
                            MessageBox.Show($"An error ocurred while recovering your file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            Close();
                        }));
                    }
                }));
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            FileOperations.ProgressChanged -= FileOperations_ProgressChanged;
        }

        #region Disable Closing Button

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            // Hole das Fenster-Handle
            var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;

            // Hole die aktuellen Fensterstile
            int currentStyle = GetWindowLong(hwnd, GWL_STYLE);

            // Entferne den Schließen-Button
            SetWindowLong(hwnd, GWL_STYLE, currentStyle & ~WS_SYSMENU);
        }

        private const int GWL_STYLE = -16;
        private const int WS_SYSMENU = 0x80000;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        #endregion
    }

    public enum ActionType
    {
        Split,
        Merge
    }
}