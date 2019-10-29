using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SanitisedFish
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region constructor

        public MainWindow()
        {
            InitializeComponent();
        }

        #endregion

        #region event handlers
        private void BtnOpenEPCISFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _showError("");
                _currentEPCISFile = _openAndShowFile(false, listviewEPCISFile);
                listviewProcessedFile.Items.Clear();
                tbEPCISFile.Text = _currentEPCISFile;
                tbProcessedFile.Text = "";
            }
            catch (Exception ex)
            {
                _showError(ex.Message);
            }
        }

        private void BtnOpenProcessedFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _showError("");
                string openedFile = _openAndShowFile(true, listviewProcessedFile);
                tbProcessedFile.Text = openedFile;
                if (!string.IsNullOrEmpty(openedFile))
                {
                    listviewEPCISFile.Items.Clear();
                    tbEPCISFile.Text = "";
                }
            }
            catch (Exception ex)
            {
                _showError(ex.Message);
            }
        }

        private void BtnProcessEPCISFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _showError("");
                if (!File.Exists(_currentEPCISFile))
                    return;

                EPCISLib.Sanitiser.hashFile(_currentEPCISFile, _outputFile);

                _showFile(_outputFile, listviewProcessedFile);
                tbProcessedFile.Text = _outputFile;
            }
            catch (Exception ex)
            {
                _showError(ex.Message);
            }

        }

        #endregion

        #region private methods
        /// <summary>
        /// open a file and visualize it
        /// </summary>
        /// <param name="isProcessed">true when the file to be opened is already processed</param>
        /// <param name="listView"></param>
        /// <returns>name of the file opened if any, return empty string otherwise</returns>
        private string _openAndShowFile(bool isProcessed, ListView listView)
        {
            string selectedFile = _selectFile(isProcessed);

            if (string.IsNullOrEmpty(selectedFile))
                return "";

            _showFile(selectedFile, listView);
            return selectedFile;
        }

        void _showFile(string file, ListView listView)
        {
            listView.Items.Clear();

            if (File.Exists(file))
                using (StreamReader reader = new StreamReader(file))
                {
                    while (!reader.EndOfStream)
                        listView.Items.Add(reader.ReadLine());
                }
        }

        string _selectFile(bool hashed)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            // Set filter for file extension and default file extension 
            if (hashed)
            {
                dlg.DefaultExt = ".json";
                dlg.Filter = "json Files (*.json)|*.json";
            }
            else
            {
                dlg.DefaultExt = ".xml";
                dlg.Filter = "xml Files (*.xml)|*.xml|txt Files (*.txt)|*.txt";
            }

            // Display OpenFileDialog by calling ShowDialog method 
            Nullable<bool> result = dlg.ShowDialog();


            // Get the selected file name and display in a TextBox 
            if (result == true)
                // Open document 
                return dlg.FileName;
            else
                return "";
        }


        void _showError(string error)
        {
            tbError.Text = error;
        }

        #endregion

        #region private data

        string _currentEPCISFile;
        readonly string _outputFile = System.IO.Path.Combine(Environment.CurrentDirectory, "output.json");

        #endregion
    }
}
