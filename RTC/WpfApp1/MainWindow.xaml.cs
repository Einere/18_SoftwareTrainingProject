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

namespace WpfApp1
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        
        private void bt_server_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("server");
            
        }

        private void bt_client_Click(object sender, RoutedEventArgs e)
        {
            Window1 connectionForm = new Window1();
            connectionForm.Owner = this;
            connectionForm.Show();
        }
    }
}
