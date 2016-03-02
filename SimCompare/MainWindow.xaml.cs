using System;
using System.Collections.Generic;
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

namespace SimCompare
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        FileMan fMan = new FileMan();
        String[] modes = new String[3] { "Sim Compare - 1 CSV", "CSV For Each", "CSV For Each - DIFF ONLY" };
        public MainWindow()
        {
            InitializeComponent();
            fillListBoxes();
            fillModeSelector();
        }
        public void fillListBoxes()
        {
            listBox_orig.ItemsSource = fMan.getOriginalNames();
            listBox_orig.SelectedIndex = 0;
            listBox_sims.ItemsSource = fMan.getSimulationNames();
            listBox_sims.SelectedIndex = 0;
        }
        public void fillModeSelector()
        {
            modeSelector.ItemsSource = modes;
            modeSelector.SelectedIndex = 0;
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            fMan.refreshFiles();
            fillListBoxes();
        }

        private void btnCompare_Click(object sender, RoutedEventArgs e)
        {
            //update the list with our selected files
            fMan.originalFileToParse = String.Join(",", listBox_orig.SelectedItems.Cast<String>()).Split(',');
            fMan.modifiedFilesToParse = String.Join(",", listBox_sims.SelectedItems.Cast<String>()).Split(',');
            if(modeSelector.SelectedIndex == 0)
            {
                fMan.parseFilesInOne();
            }else if(modeSelector.SelectedIndex == 1)
            {
                fMan.parseFiles(false);
            }
            else
            {
                fMan.parseFiles(true);
            }
            MessageBox.Show("Files Compared Succesfully");
        }
    }
}
