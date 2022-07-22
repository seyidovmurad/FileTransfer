using FileTransfer.ShellClasses;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

namespace FileTransfer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    [AddINotifyPropertyChangedInterface]
    public partial class MainWindow : Window
    {
        public string FirstPath { get; set; } = "";

        public string SecondtPath { get; set; } = "";

        private bool isDirectionLeft = false;
        public string Direction { get; set; } = "->";

        public MainWindow()
        {
            InitializeComponent();
            InitializeFileSystemObjects();
            DataContext = this;
        }

        private void InitializeFileSystemObjects()
        {
            
            treeView.Items.Clear();
            treeView2.Items.Clear();
            DriveInfo.GetDrives().ToList().ForEach(drive =>
            {
                var fileSystemObject = new FileSystemObjectInfo(drive);
                var fileSystemObject2 = new FileSystemObjectInfo(drive);
                fileSystemObject.BeforeExplore += FileSystemObject_BeforeExplore;
                fileSystemObject2.BeforeExplore += FileSystemObject_BeforeExplore;
                fileSystemObject.AfterExplore += FileSystemObject_AfterExplore;
                fileSystemObject2.AfterExplore += FileSystemObject_AfterExplore;
                treeView.Items.Add(fileSystemObject);
                treeView2.Items.Add(fileSystemObject2);
            });
        }

        private void FileSystemObject_AfterExplore(object sender, System.EventArgs e)
        {
            Cursor = Cursors.Arrow;
        }

        private void FileSystemObject_BeforeExplore(object sender, System.EventArgs e)
        {
            Cursor = Cursors.Wait;
        }

        private void treeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            try
            {
                if (treeView.SelectedItem != null)
                    FirstPath = (treeView.SelectedItem as FileSystemObjectInfo).FileSystemInfo.FullName;
            }
            catch { }
        }

        private void treeView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(Environment.GetEnvironmentVariable("WINDIR") + @"\explorer.exe", FirstPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }

        private void treeView2_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            try
            {
                if (treeView2.SelectedItem != null)
                    SecondtPath = (treeView2.SelectedItem as FileSystemObjectInfo).FileSystemInfo.FullName;
            }
            catch { }
        }

        private void treeView2_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(Environment.GetEnvironmentVariable("WINDIR") + @"\explorer.exe", SecondtPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private async void MoveBtn_Click(object sender, RoutedEventArgs e)
        {
            await MoveFileAsync();
            InitializeFileSystemObjects();
        }

        private async void CopyBtn_Click(object sender, RoutedEventArgs e)
        {
            await CopyFileAsync();
            InitializeFileSystemObjects();
        }

        private void DeleteBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var attr = File.GetAttributes(FirstPath);

                if (attr == FileAttributes.Directory)
                {
                    var dir = new DirectoryInfo(FirstPath);
                    dir.Delete(true);
                }
                else 
                    File.Delete(FirstPath);

                FirstPath = "";
                SecondtPath = "";
                InitializeFileSystemObjects();
            }
            catch (Exception ex)
            {
                
                MessageBox.Show(ex.Message);
            }
        }

        private void OpenBtn_Click(object sender, RoutedEventArgs e)
        {
            treeView_MouseDoubleClick(null, null);
        }

        private async void EncryptBtn_Click(object sender, RoutedEventArgs e)
        {
            await EncryptAsync();
            MessageBox.Show("Files De\\Encrypted");
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            isDirectionLeft = !isDirectionLeft;
            if (isDirectionLeft)
                Direction = "<-";
            else 
                Direction = "->";
        }


        private async Task EncryptAsync()
        {
            var attr = File.GetAttributes(FirstPath);

            if(attr == FileAttributes.Directory)
            {
                foreach (string newPath in Directory.GetFiles(FirstPath, "*.*", SearchOption.AllDirectories))
                    await EncryptFileAsync(newPath);
            }
            else
            {
                await EncryptFileAsync(FirstPath);
            }
        }

        private async Task EncryptFileAsync(string path)
        {
            byte[] buffer = new byte[50];

            if (!path.EndsWith(".txt"))
            {
                System.Windows.MessageBox.Show("You can only encrypt .txt file");
                return;
            }

            await Task.Run(() =>
            {
                string salt = "2705";
                var text = File.ReadAllText(path);
                var result = EncryptDecrypt(text, salt);

                File.WriteAllText(path, result, Encoding.UTF8);

            });
        }

        private string EncryptDecrypt(string text, string key)
        {
            var result = new StringBuilder();

            for (int c = 0; c < text.Length; c++)
                result.Append((char)((uint)text[c] ^ (uint)key[c % key.Length]));

            return result.ToString();
        }

        private async Task CopyFileAsync()
        {
            if (!string.IsNullOrEmpty(FirstPath) && !string.IsNullOrEmpty(SecondtPath))
            {
                string from = isDirectionLeft ? SecondtPath : FirstPath;
                string to = isDirectionLeft ? FirstPath : SecondtPath;


                await Task.Run(() =>
                {
                    var fromAttr = File.GetAttributes(from);
                    var toAttr = File.GetAttributes(to);

                    if (toAttr != FileAttributes.Directory)
                        to = RemoveFile(to);
                    else
                        to += "\\";


                    if (fromAttr == FileAttributes.Directory)
                    {
                        foreach (string dirPath in Directory.GetDirectories(from, "*", SearchOption.AllDirectories))
                        {
                            Directory.CreateDirectory(dirPath.Replace(from, to));
                            if (Directory.Exists(from))
                            {
                                //Copy all the files
                                foreach (string newPath in Directory.GetFiles(from, "*.*", SearchOption.AllDirectories))
                                    File.Copy(newPath, newPath.Replace(from, to), true);
                            }
                        }
                    }
                    else
                    {
                        var fileName = from.Split('\\').Last();
                        File.Copy(from, to + fileName,true);
                    }
                });

            }
        }

        private string RemoveFile(string path)
        {
            string str = "";
            var temp = path.Split('\\').ToList();
            temp.Remove(temp.Last());

            foreach (var item in temp)
            {
                str += item + '\\';
            }

            return str;
        }

        private async Task MoveFileAsync()
        {
            if (!string.IsNullOrEmpty(FirstPath) && !string.IsNullOrEmpty(SecondtPath))
            {
                string from = isDirectionLeft ? SecondtPath : FirstPath;
                string to = isDirectionLeft ? FirstPath : SecondtPath;


                await Task.Run(() =>
                {
                    var fromAttr = File.GetAttributes(from);
                    var toAttr = File.GetAttributes(to);

                    if (toAttr != FileAttributes.Directory)
                        to = RemoveFile(to);
                    else
                        to += "\\";


                    if (fromAttr == FileAttributes.Directory)
                    {
                        foreach (string dirPath in Directory.GetDirectories(from, "*", SearchOption.AllDirectories))
                        {
                            Directory.CreateDirectory(dirPath.Replace(from, to));
                            if (Directory.Exists(from))
                            {
                                //Copy all the files
                                foreach (string newPath in Directory.GetFiles(from, "*.*", SearchOption.AllDirectories))
                                    File.Move(newPath, newPath.Replace(from, to), true);
                            }
                        }
                    }
                    else
                    {
                        var fileName = from.Split('\\').Last();
                        File.Move(from, to + fileName, true);
                    }
                });

            }
        }

    }
}
