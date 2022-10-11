using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Windows;
using System.Windows.Input;

namespace BonelabModLocator
{
    public partial class MainWindow : Window
    {
        private const string AppDataBoneLabModsPath = @"C:\Users\<USER>\AppData\LocalLow\Stress Level Zero\BONELAB\Mods";
        private string targetBonelabModsPath;
        private bool IsExistBackup = false;
        private bool IsCanProcessSymlinkLocation = false;

        #region injectedServices
        [DllImport("kernel32.dll")]
        static extern bool CreateSymbolicLink(
        string lpSymlinkFileName, string lpTargetFileName, SymbolicLink dwFlags);

        enum SymbolicLink
        {
            File = 0,
            Directory = 1
        }
        #endregion

        #region statics
        static void CopyDirectory(string sourceDir, string destinationDir, bool recursive = true)
        {
            // Get information about the source directory
            var dir = new DirectoryInfo(sourceDir);

            // Check if the source directory exists
            if (!dir.Exists)
                throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

            // Cache directories before we start copying
            DirectoryInfo[] dirs = dir.GetDirectories();

            // Create the destination directory
            Directory.CreateDirectory(destinationDir);

            // Get the files in the source directory and copy to the destination directory
            foreach (FileInfo file in dir.GetFiles())
            {
                string targetFilePath = Path.Combine(destinationDir, file.Name);
                file.CopyTo(targetFilePath, true);
            }

            // If recursive and copying subdirectories, recursively call this method
            if (recursive)
            {
                foreach (DirectoryInfo subDir in dirs)
                {
                    string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                    CopyDirectory(subDir.FullName, newDestinationDir, true);
                }
            }
        }
        #endregion

        public MainWindow()
        {
            InitializeComponent();

            if (!IsAdministrator())
            {
                System.Windows.Forms.MessageBox.Show("Программа требует запуска от имени администратора!" +
                    "\n Перезапустите программу и повторите попытку.");
                Close();
            }
        }

        #region Buttons
        private void Button_ChooseFolder(object sender, MouseButtonEventArgs e)
        {
            targetBonelabModsPath = ChooseTargetModsFolder();

            if (!string.IsNullOrEmpty(targetBonelabModsPath))
            {
                IsCanProcessSymlinkLocation = true;
                DirectoryPathBox.Text = targetBonelabModsPath;
                System.Windows.Forms.MessageBox.Show("Каталог выбран успешно!");
            }
            else
                System.Windows.Forms.MessageBox.Show("Ошибка выбора каталога. Каталога не существует или он пустой!");
        }

        private void bthSaveLocationEvent(object sender, RoutedEventArgs e)
        {
            bool isSuccess = false;

            if (IsCanProcessSymlinkLocation)
            {
                var tempPath = CreateAndGetTempModsBackup();

                isSuccess = CreateSymlink(tempPath, targetBonelabModsPath);
            }

            if (isSuccess)
                System.Windows.Forms.MessageBox.Show($"Перенаправление папки модов на папку {targetBonelabModsPath} создано успешно!");
            else
                System.Windows.Forms.MessageBox.Show($"Перенаправление папки модов не завершено. \n Попробуйте ещё раз!");
        }

        private void bthExitEvent(object sender, RoutedEventArgs e)
        {
            Close();
        }
        #endregion

        #region Internal

        private bool CreateSymlink(string backupModsPath, string targetModsPath)
        {
            if (!IsExistBackup || !Directory.Exists(targetModsPath) || !IsDirectoryEmpty(targetModsPath))
                return false;

            Directory.Move(GetInitialModsPath(), GetInitialModsPath() + "back");
            CopyDirectory(backupModsPath, targetModsPath);

            var isSuccess = CreateSymbolicLink(GetInitialModsPath(), targetModsPath, SymbolicLink.Directory);

            if (isSuccess)
            {
                Directory.Delete(GetInitialModsPath() + "back", true);
                Directory.Delete(backupModsPath, true);
            }
            else
            {
                Directory.Move(GetInitialModsPath() + "back", GetInitialModsPath());
            }

            return isSuccess;
        }


        private string CreateAndGetTempModsBackup(string initialmodsPath = null)
        {
            var tempPath = Path.GetTempPath() + "BonelabModsBackup";

            if (Directory.Exists(tempPath))
                Directory.Delete(tempPath, true);

            var info = Directory.CreateDirectory(tempPath);

            CopyDirectory(string.IsNullOrEmpty(initialmodsPath) ? GetInitialModsPath() : initialmodsPath, info.FullName);

            IsExistBackup = true;

            return info.FullName;
        }

        private string GetInitialModsPath()
            => AppDataBoneLabModsPath.Replace("<USER>", Environment.UserName);

        private string ChooseTargetModsFolder()
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    return dialog.SelectedPath;
            }

            return null;
        }

        public bool IsDirectoryEmpty(string path)
        {
            return !Directory.EnumerateFileSystemEntries(path).Any();
        }

        public static bool IsAdministrator()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        #endregion

        #region UI
        private void Grid_MouseDragMove(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }
        #endregion
    }
}
