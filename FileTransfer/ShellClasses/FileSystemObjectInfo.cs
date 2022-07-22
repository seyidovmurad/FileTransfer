using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace FileTransfer.ShellClasses
{
    public class FileSystemObjectInfo : BaseObject
    {
        public FileSystemObjectInfo(DriveInfo drive)
            : this(drive.RootDirectory)
        {
        }


        public FileSystemObjectInfo(FileSystemInfo info)
        {
            if (this is DummyFileSystemObjectInfo)
            {
                return;
            }

            Children = new ObservableCollection<FileSystemObjectInfo>();
            FileSystemInfo = info;

            if (info is DirectoryInfo)
            {
                ImageSource = FolderManager.GetImageSource(info.FullName, ItemState.Close);
                AddDummy();
            }
            else if (info is FileInfo)
            {
                ImageSource = FileManager.GetImageSource(info.FullName);
            }

            PropertyChanged += new PropertyChangedEventHandler(FileSystemObjectInfo_PropertyChanged);
        }

        

        public event EventHandler BeforeExpand;

        public event EventHandler AfterExpand;

        public event EventHandler BeforeExplore;

        public event EventHandler AfterExplore;

        private void RaiseBeforeExpand()
        {
            BeforeExpand?.Invoke(this, EventArgs.Empty);
        }

        private void RaiseAfterExpand()
        {
            AfterExpand?.Invoke(this, EventArgs.Empty);
        }

        private void RaiseBeforeExplore()
        {
            BeforeExplore?.Invoke(this, EventArgs.Empty);
        }

        private void RaiseAfterExplore()
        {
            AfterExplore?.Invoke(this, EventArgs.Empty);
        }


        public ObservableCollection<FileSystemObjectInfo> Children
        {
            get { return base.GetValue<ObservableCollection<FileSystemObjectInfo>>("Children"); }
            private set { base.SetValue("Children", value); }
        }

        public ImageSource ImageSource
        {
            get { return base.GetValue<ImageSource>("ImageSource"); }
            private set { base.SetValue("ImageSource", value); }
        }

        public bool IsExpanded
        {
            get { return base.GetValue<bool>("IsExpanded"); }
            set { base.SetValue("IsExpanded", value); }
        }

        public FileSystemInfo FileSystemInfo
        {
            get { return base.GetValue<FileSystemInfo>("FileSystemInfo"); }
            private set { base.SetValue("FileSystemInfo", value); }
        }

        private DriveInfo Drive
        {
            get { return base.GetValue<DriveInfo>("Drive"); }
            set { base.SetValue("Drive", value); }
        }


        private void AddDummy()
        {
            this.Children.Add(new DummyFileSystemObjectInfo());
        }

        private bool HasDummy()
        {
            return !object.ReferenceEquals(this.GetDummy(), null);
        }

        private DummyFileSystemObjectInfo GetDummy()
        {
            var list = this.Children.OfType<DummyFileSystemObjectInfo>().ToList();
            if (list.Count > 0) return list.First();
            return null;
        }

        private void RemoveDummy()
        {
            this.Children.Remove(this.GetDummy());
        }

        private void ExploreDirectories()
        {
            if (Drive?.IsReady == false)
            {
                return;
            }
            if (FileSystemInfo is DirectoryInfo)
            {
                try
                {
                    var directories = ((DirectoryInfo)FileSystemInfo).GetDirectories();
                    foreach (var directory in directories.OrderBy(d => d.Name))
                    {
                        if ((directory.Attributes & FileAttributes.System) != FileAttributes.System &&
                            (directory.Attributes & FileAttributes.Hidden) != FileAttributes.Hidden)
                        {
                            var fileSystemObject = new FileSystemObjectInfo(directory);
                            fileSystemObject.BeforeExplore += FileSystemObject_BeforeExplore;
                            fileSystemObject.AfterExplore += FileSystemObject_AfterExplore;
                            Children.Add(fileSystemObject);
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show(ex.Message);
                }
            }
        }

        private void FileSystemObject_AfterExplore(object sender, EventArgs e)
        {
            RaiseAfterExplore();
        }

        private void FileSystemObject_BeforeExplore(object sender, EventArgs e)
        {
            RaiseBeforeExplore();
        }
        private void ExploreFiles()
        {
            if (Drive?.IsReady == false)
            {
                return;
            }
            if (FileSystemInfo is DirectoryInfo)
            {
                try
                {
                    var files = ((DirectoryInfo)FileSystemInfo).GetFiles();
                    foreach (var file in files.OrderBy(d => d.Name))
                    {
                        if ((file.Attributes & FileAttributes.System) != FileAttributes.System &&
                            (file.Attributes & FileAttributes.Hidden) != FileAttributes.Hidden)
                        {
                            Children.Add(new FileSystemObjectInfo(file));
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show(ex.Message);
                }
            }
        }


        void FileSystemObjectInfo_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (FileSystemInfo is DirectoryInfo)
            {
                if (string.Equals(e.PropertyName, "IsExpanded", StringComparison.CurrentCultureIgnoreCase))
                {
                    RaiseBeforeExpand();
                    if (IsExpanded)
                    {
                        ImageSource = FolderManager.GetImageSource(FileSystemInfo.FullName, ItemState.Open);
                        if (HasDummy())
                        {
                            RaiseBeforeExplore();
                            RemoveDummy();
                            ExploreDirectories();
                            ExploreFiles();
                            RaiseAfterExplore();
                        }
                    }
                    else
                    {
                        ImageSource = FolderManager.GetImageSource(FileSystemInfo.FullName, ItemState.Close);
                    }
                    RaiseAfterExpand();
                }
            }
        }
    }
}
