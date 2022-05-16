using System;
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

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private readonly AutoResetEvent autoResetEvent;

        private readonly Random random;
        public MainWindow()
        {
            this.autoResetEvent = new AutoResetEvent(false);

            this.random = new Random();



            InitializeComponent();
        }
        private void start_button_click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(ip_textbox.Text) || string.IsNullOrEmpty(port_textbox.Text)
                || string.IsNullOrEmpty(transaction_period_textbox.Text)
                || string.IsNullOrEmpty(transaction_time_textbox.Text)) ;
            else
            {
                if ((string)start_button.Content == "Stop")
                {
                    start_button.Content = "Start";
                }
                else
                {
                    autoResetEvent.Set();
                    transaction_queue_textbox.Text = "";
                    start_button.Content = "Stop";
                }
            }
        }

        private void transaction_queue_textbox_TextChanged(object sender, TextChangedEventArgs e)
        {

            transaction_queue_textbox.ScrollToEnd();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Application.Current.Shutdown(); // 어플리케이션을 종료
            Environment.Exit(0); // 어플리케이션의 모든 쓰레드를 멈추어 종료시킴
        }
    }
}
