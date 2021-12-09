using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.File;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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

namespace AzureBlobSearcher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        static string _storageAcc;
        static string _container;
        static string _subFolder;
        static CancellationTokenSource _cancellationTokenSource;
        static CancellationToken _cancellationToken;
        static int _tasksCount;
        static string _searchText;
        static int _filesCount;
        static int _folderCount;

        public MainWindow()
        {
            InitializeComponent();

            StorageAccountText.Text = "DefaultEndpointsProtocol=https;AccountName=X;AccountKey=Y;EndpointSuffix=core.windows.net";
            BlobContainerText.Text = "BlobNameHere";
            FolderText.Text = @"\folder";
            KeyText.Password = "PasswordHere";
            SearchText.Text = "PartialFileName";
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            StartSearch();
        }

        private async void StartSearch()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _cancellationToken = _cancellationTokenSource.Token;

            _tasksCount = 0;
            _filesCount = 0;
            _folderCount = 0;

            ResultsText.Text = "";
            ProgressList.Items.Clear();

            _storageAcc = StorageAccountText.Text;
            _container = BlobContainerText.Text;
            _subFolder = FolderText.Text;
            _searchText = SearchText.Text;

            ShowUIBusy();
            
            ExecuteQuery();
            
        }

        private void ExecuteQuery()
        {
            WriteStatus("Connecting to Azure...");

            try
            {
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(_storageAcc);
                // Create the File service client.
                CloudFileClient fileClient = storageAccount.CreateCloudFileClient();

                CloudFileShare share = fileClient.GetShareReference(_container);

                if (share.Exists())
                {
                    IEnumerable<IListFileItem> fileList = share.GetRootDirectoryReference().ListFilesAndDirectories();

                    // hard code sub folder for version 1.0
                    // todo - make this dynamic
                    var f1 = GetFolder(fileList, "FolderName");
                    var f2 = GetFolder(f1.ListFilesAndDirectories(), "ChildFolder1");
                    var f3 = GetFolder(f2.ListFilesAndDirectories(), "GrandChildFolder1");

                    // start search at given folder
                    foreach (IListFileItem listItem in f3.ListFilesAndDirectories())
                    {
                        if (listItem.GetType() == typeof(CloudFileDirectory))
                        {
                            Task.Run(() => ListSubDir(listItem), _cancellationToken);
                        }
                        else
                        {
                            WriteResultRow(listItem.Uri.Segments.Last());
                            WriteStatus("Processed " + _filesCount + " files");
                        }

                        // handle cancellation
                        if (_cancellationToken.IsCancellationRequested)
                        {
                            _cancellationToken.ThrowIfCancellationRequested();
                        }
                    }
                }

                WriteStatus("Complete OK");
            }
            catch(Exception ex)
            {
                WriteStatus(ex.Message);
            }

        }

        private CloudFileDirectory GetFolder(IEnumerable<IListFileItem> fileList, string dirName)
        {
            foreach (IListFileItem listItem in fileList)
            {
                if (listItem.GetType() == typeof(CloudFileDirectory))
                {
                    if (listItem.Uri.Segments.Last() == dirName)
                    {
                        return (CloudFileDirectory)listItem;
                    }
                }
            }
            return null;
        }

        public void ListSubDir(IListFileItem list)
        {
            _tasksCount++;
            _folderCount++;

            try
            {
                WriteProgress("Tasks: " + _tasksCount + " folder: " + list.Uri);
                CloudFileDirectory fileDirectory = (CloudFileDirectory)list;
                
                IEnumerable<IListFileItem> fileList = fileDirectory.ListFilesAndDirectories();

                // Print all files/directories in the folder.
                foreach (IListFileItem listItem in fileList)
                {
                    // handle cancellation
                    if (_cancellationToken.IsCancellationRequested)
                    {
                        _cancellationToken.ThrowIfCancellationRequested();
                    }
                    // listItem type will be CloudFile or CloudFileDirectory.
                    if (listItem.GetType() == typeof(CloudFileDirectory))
                    {
                        Task.Run(() => ListSubDir(listItem));
                    }
                    else
                    {
                        _filesCount++;
                        if (listItem.Uri.ToString().ToUpper().Contains(_searchText.ToUpper()))
                        {
                            WriteResultRow(String.Join(@"", listItem.Uri.Segments));
                        }
                    }
                    WriteStatus("Processed " + _filesCount + " files/" + _folderCount + " folders");
                }
            }catch(Exception ex)
            {
                WriteStatus(ex.Message);
            }
            _tasksCount--;

            WriteProgress("Folder Complete, Tasks: " + _tasksCount);
            if (_tasksCount == 0)
            {
                //Dispatcher.BeginInvoke((Action)(() => ShowUIReady()));
            }
        }
        
        private void WriteProgress(string message)
        {
            _ = Dispatcher.BeginInvoke((Action)(() =>
            {
                ProgressList.Items.Add(message);
                if(ProgressList.Items.Count > 20)
                {
                    ProgressList.Items.RemoveAt(0);
                }
            }));
        }
        private void WriteStatus(string message)
        {
            _ = Dispatcher.BeginInvoke((Action)(() => StatusText.Text = message));
        }

        private void WriteResultRow(string message)
        {
            _ = Dispatcher.BeginInvoke((Action)(() => ResultsText.Text += message + "\n"));
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            _cancellationTokenSource.Cancel();
        }

        private void ShowUIBusy()
        {
            CancelButton.Visibility = Visibility.Visible;
            SearchButton.Visibility = Visibility.Hidden;
        }

        private void ShowUIReady()
        {
            CancelButton.Visibility = Visibility.Hidden;
            SearchButton.Visibility = Visibility.Visible;
            BusyIndicator1.IsBusy = false;
        }
    }
}
