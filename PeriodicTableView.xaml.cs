using System;
using System.Windows;
using System.Windows.Controls;

namespace YMM4ChemicalStructurePlugin.Shape
{
    public class ElementSelectedEventArgs : EventArgs
    {
        public ElementInfo SelectedElement { get; }

        public ElementSelectedEventArgs(ElementInfo selectedElement)
        {
            SelectedElement = selectedElement;
        }
    }

    public partial class PeriodicTableView : UserControl
    {
        public event EventHandler<ElementSelectedEventArgs>? ElementSelected;

        public PeriodicTableView()
        {
            InitializeComponent();
        }

        private void ElementButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is ElementInfo element)
            {
                ElementSelected?.Invoke(this, new ElementSelectedEventArgs(element));
            }
        }
    }
}