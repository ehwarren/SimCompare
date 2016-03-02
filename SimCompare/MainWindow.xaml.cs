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
            //Get the data and fill the list boxes with proper filenames
            fillListBoxes();
            //set the mode selector box with the approprite modes
            fillModeSelector();
            //Set the title of the window appropriately
            setWindowTitle();
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
            fMan.modifiedFilesToParse = String.Join(",", listBox_orig.SelectedItems.Cast<String>()).Split(',');
            fMan.modifiedFilesToParse = String.Join(",", listBox_sims.SelectedItems.Cast<String>()).Split(',');
            //make sure we have at least one original and one modified file to compare
            if (fMan.originalFileToParse[0] == "" || fMan.modifiedFilesToParse[0] == "")
            {
                MessageBox.Show("You must select at least one original file and one modified file to compare...");
                return;
            }
            //Choose the correct mode
            if(modeSelector.SelectedIndex == 0)
                MessageBox.Show(fMan.parseFilesInOne());
            else if(modeSelector.SelectedIndex == 1)
                MessageBox.Show(fMan.parseFiles(false));
            else
                MessageBox.Show(fMan.parseFiles(true));
        }

        private void setWindowTitle()
        {
            this.Title = Constants.TITLE + " Version: " + Constants.VERSION + "." + Constants.REVISION;
        }
    }
}
