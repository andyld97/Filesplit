using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Units;
using Forms = System.Windows.Forms;

namespace Filesplit
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly bool isInitalized = false;

        public MainWindow()
        {
            InitializeComponent();
            ApplyTheming();

            isInitalized = true;
        }

        private void ButtonSplitFile_Click(object sender, RoutedEventArgs e)
        {
            string destinationPath = string.Empty;
            string sourceFile = TextPath.Text;
            int chunkSize = int.Parse(NumericTextBox.Text);            

            using (Forms.FolderBrowserDialog ofd = new Forms.FolderBrowserDialog())
            {
                if (ofd.ShowDialog() == Forms.DialogResult.OK)
                {
                    destinationPath = ofd.SelectedPath;
                }
            }

            if (string.IsNullOrEmpty(destinationPath) || !System.IO.Directory.Exists(destinationPath))
                return;

            string splittedFilePath = System.IO.Path.Combine(destinationPath, FileOperations.SPLITTED_FILENAME);
            if (System.IO.File.Exists(splittedFilePath))
            {
                MessageBox.Show("It seems that there is already a file splitted in the directory, please choose another (empty) one!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);    
                return;
            }

            long chunkSizeBytes = (long)ByteUnit.FromMB(chunkSize).To(Unit.B).Length;
            try
            {
                var fileLength = new FileInfo(sourceFile).Length;
                if (fileLength / chunkSizeBytes >= 10)
                {
                    MessageBoxResult result = MessageBox.Show("The file will be splitted into more than 10 files. Do you want to continue?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (result == MessageBoxResult.No)
                        return;
                }
            }
            catch
            {
                // ignore
            }

            ProgressDialogWindow progressDialogWindow = new ProgressDialogWindow(ActionType.Split, sourceFile, destinationPath, chunkSize);            
            progressDialogWindow.ShowDialog();
        }

        private void ButtonMergeFiles_Click(object sender, RoutedEventArgs e)
        {
            string destinationPath = string.Empty;

            using (Forms.FolderBrowserDialog ofd = new Forms.FolderBrowserDialog())
            {
                if (ofd.ShowDialog() == Forms.DialogResult.OK)
                {
                    destinationPath = ofd.SelectedPath;
                }
            }

            if (string.IsNullOrEmpty(destinationPath) || !System.IO.Directory.Exists(destinationPath))
                return;

            ProgressDialogWindow progressDialogWindow = new ProgressDialogWindow(ActionType.Merge, null, destinationPath, 0);
            progressDialogWindow.ShowDialog();
        }

        private void GeneratePreview()
        {
            try
            {
                if (string.IsNullOrEmpty(TextPath.Text) || !System.IO.File.Exists(TextPath.Text))
                {
                    TextPreview.Text = "-";
                    return;
                }

                var fi = new FileInfo(TextPath.Text);

                string firstPart = $"Input file: {ByteUnit.FindUnit(fi.Length)}";
                string previewText = $"{firstPart}{Environment.NewLine}{string.Join(string.Empty, Enumerable.Repeat("=", firstPart.Length))}{Environment.NewLine}";

                var chunkSize = ByteUnit.FromMB(int.Parse(NumericTextBox.Text)).To(Unit.B).Length;

                if (chunkSize > fi.Length)
                    previewText += "Chunk size is larger than file size. No split needed.";
                else
                {
                    int mainChunks = (int)Math.Floor(fi.Length / chunkSize);
                    int lastChunk = (int)(fi.Length % chunkSize);
                    if (lastChunk > 0)
                        mainChunks++;

                    for (int c = 0; c < mainChunks; c++)
                    {
                        if (c == mainChunks - 1 && lastChunk > 0)
                            chunkSize = lastChunk;

                        previewText += $"{System.IO.Path.GetFileName(TextPath.Text)}.{c}: {ByteUnit.FindUnit(chunkSize)}{Environment.NewLine}";
                    }
                }

                TextPreview.Text = previewText;
            }
            catch
            {
                TextPreview.Text = "-";
            }
        }

        private void ButtonChoseSplitFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            var result = ofd.ShowDialog();

            if (result.HasValue && result.Value)
            {
                TextPath.Text = ofd.FileName;
                GeneratePreview();
            }
        }

        #region Numeric Up Down

        private const int ScrollIncrement = 50;
        private const int Min = 1;
        private const int Max = 999999;

        private void ButtonIncrement_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(NumericTextBox.Text, out int value))
            {
                NumericTextBox.Text = CoerceValue((value + 1)).ToString();
            }
        }

        private void ButtonDecrement_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(NumericTextBox.Text, out int value) && value > 0)
            {
                NumericTextBox.Text = CoerceValue((value - 1)).ToString();
            }
        }

        private void NumericTextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = !int.TryParse(e.Text, out _);
        }

        private void NumericTextBox_PreviewMouseWheel_1(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            if (int.TryParse(NumericTextBox.Text, out int value))
            {
                if (e.Delta > 0)
                {
                    value += ScrollIncrement;
                }
                else if (e.Delta < 0) 
                {
                    value = Math.Max(0, value - ScrollIncrement);
                }

                NumericTextBox.Text = CoerceValue( value).ToString();
            }

            e.Handled = true; 
        }

        private int CoerceValue(int value)
        {
            if (value > Max)
                return Max;
            if (value < Min)
                return Min;

            return value;
        }

        private void NumericTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (!isInitalized) return;

            GeneratePreview();
        }
        #endregion

        #region Theming

        public static void ApplyTheming()
        {
            Color tabControlBackgroundColor, tabItemBackground, tabItemSelectedBackground;

            tabControlBackgroundColor = (Color)ColorConverter.ConvertFromString($"#DADADA");
            tabItemBackground = (Color)ColorConverter.ConvertFromString("#D7D7D7");
            tabItemSelectedBackground = (Color)ColorConverter.ConvertFromString("#F9F9F9");

            // Apply own theming colors
            App.Current.Resources["TabControl.Background"] = new SolidColorBrush(tabControlBackgroundColor);
            App.Current.Resources["TabItemBackground"] = new SolidColorBrush(tabItemBackground);
            App.Current.Resources["TabItemSelectedBackground"] = new SolidColorBrush(tabItemSelectedBackground);
        }


        #endregion
    }
}