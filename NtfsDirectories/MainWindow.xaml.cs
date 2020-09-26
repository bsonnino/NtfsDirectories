using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Filesystem.Ntfs;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace NtfsDirectories
{
    public class DirectorySize
    {
        public DirectorySize(string name, long size)
        {
            Name = name;
            Size = size;
        }

        public string Name { get; }
        public Int64 Size { get; }
    }

    
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            var ntfsDrives = DriveInfo.GetDrives()
                .Where(d => d.IsReady && d.DriveFormat == "NTFS").ToList();
            DrvCombo.ItemsSource = ntfsDrives;
            DrvCombo.SelectionChanged += DrvCombo_SelectionChanged;
        }

        private IEnumerable<INode> nodes = null;
        private async void DrvCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DrvCombo.SelectedItem == null) return;
            var driveToAnalyze = (DriveInfo)DrvCombo.SelectedItem;
            DrvCombo.IsEnabled = false;
            LevelCombo.IsEnabled = false;
            StatusTxt.Text = "Analyzing drive";
            
            await Task.Factory.StartNew(() => { nodes = GetFileNodes(driveToAnalyze); });

            LevelCombo.SelectedIndex = -1;
            LevelCombo.SelectedIndex = 3;

            DrvCombo.IsEnabled = true;
            LevelCombo.IsEnabled = true;
            StatusTxt.Text = "";
        }

        private static IEnumerable<INode> GetFileNodes(DriveInfo driveToAnalyze)
        {
            var ntfsReader =
                new NtfsReader(driveToAnalyze, RetrieveMode.All);
            return
                ntfsReader.GetNodes(driveToAnalyze.Name)
                    .Where(n => (n.Attributes &
                                 (Attributes.Hidden | Attributes.System |
                                  Attributes.Temporary | Attributes.Device |
                                  Attributes.Directory | Attributes.Offline |
                                  Attributes.ReparsePoint | Attributes.SparseFile)) == 0);
        }

        private IList<DirectorySize> GetDirectorySizes(IEnumerable<INode> nodes, int level)
        {
             var directories = nodes
                .Select(n => new
                {
                    Path = string.Join(Path.DirectorySeparatorChar.ToString(),
                        Path.GetDirectoryName(n.FullName)
                            .Split(Path.DirectorySeparatorChar)
                            .Take(level)),
                    Node = n
                })
                .GroupBy(g => g.Path)
                .Select(g => new DirectorySize(g.Key, g.Sum(d => (Int64)d.Node.Size)))
                .OrderByDescending(d => d.Size)
                .ToList();
              return directories;
        }

        private void LevelCombo_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DirectoriesList == null)
                return;
            int level = Int32.MaxValue-1;
            if (LevelCombo.SelectedIndex < 0 || nodes == null)
                DirectoriesList.ItemsSource = null;
            else if (LevelCombo.SelectedIndex < 3)
                level = LevelCombo.SelectedIndex;
            DirectoriesList.ItemsSource = GetDirectorySizes(nodes, level+1);
        }
    }
}
