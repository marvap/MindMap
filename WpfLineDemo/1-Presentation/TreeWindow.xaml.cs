using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MindMap._2_Logical;
using MindMap.Data;
using MindMap.Logical;

namespace WpfLineDemo
{
    /// <summary>
    /// Interaction logic for TreeWindow.xaml.
    /// Modal window (F2) showing the whole hierarchy as a text tree. Each node = a level;
    /// the current level is bold. Double-click a level to navigate straight to it.
    /// </summary>
    public partial class TreeWindow : Window
    {
        public TreeWindow()
        {
            InitializeComponent();
            BuildTree();
        }

        private void BuildTree()
        {
            // owners of the currently open level, for bold highlighting (reference identity)
            List<ElementBaseData> currentOwners = Context.Controller.CurrentOwnerPath();

            TreeViewItem rootItem = MakeItem("root", new List<ElementBaseData>(), currentOwners);
            AddChildLevels(rootItem, Context.RootProject, new List<ElementBaseData>(), currentOwners);
            rootItem.IsExpanded = true;
            Tree.Items.Add(rootItem);
        }

        private void AddChildLevels(TreeViewItem parentItem, MindMapData level, List<ElementBaseData> pathSoFar, List<ElementBaseData> currentOwners)
        {
            foreach (ElementBaseData e in level.Elements.Where(el => el.ChildLevel != null))
            {
                List<ElementBaseData> path = new List<ElementBaseData>(pathSoFar) { e };
                TreeViewItem item = MakeItem(Controller.TruncateLabel(e.Text, 20), path, currentOwners);
                AddChildLevels(item, e.ChildLevel!, path, currentOwners);
                item.IsExpanded = true;
                parentItem.Items.Add(item);
            }
        }

        private TreeViewItem MakeItem(string label, List<ElementBaseData> path, List<ElementBaseData> currentOwners)
        {
            bool isCurrent = path.Count == currentOwners.Count && path.SequenceEqual(currentOwners);
            return new TreeViewItem
            {
                Header = label,
                Tag = path,
                FontWeight = isCurrent ? FontWeights.Bold : FontWeights.Normal
            };
        }

        private void Tree_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (Tree.SelectedItem is TreeViewItem item && item.Tag is List<ElementBaseData> path)
            {
                Context.Controller.NavigateToOwnerPath(path);
                Close();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void TreeWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape || e.Key == Key.F2)
            {
                Close();
                e.Handled = true;
            }
        }
    }
}
