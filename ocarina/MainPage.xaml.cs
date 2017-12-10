using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace ocarina
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        NoteDetector NoteDetector { get; set; }
        public MainPage()
        {
            this.InitializeComponent();
            NoteDetector = NoteDetector.Create(this);

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            int ret = NoteDetector.HitButton();
            if(ret==1)
            {
                (sender as Button).Content = "Stop";
            }
            else
            {
                (sender as Button).Content = "Start";
            }
        }

        public void ShowLogs(string log)
        {
            if (FindName("logOutput") is TextBlock logOutput)
            {
                logOutput.Text = String.Concat(logOutput.Text, log + "\n");
            }
        }

        public void ShowLogs(List<string> log)
        {
            if (FindName("logOutput") is TextBlock logOutput)
            {
                logOutput.Text = String.Concat(log);
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            NoteDetector.CleanUp();
        }

    }
}
