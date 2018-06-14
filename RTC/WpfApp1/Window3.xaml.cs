using System;
using System.Collections.Generic;
using System.IO;
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
using System.Windows.Shapes;
using System.Windows.Markup;
using System.Xml;

namespace WpfApp1
{
    /// <summary>
    /// Window3.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class Window3 : Window
    {
        public static class WPFObjectCopier
        {
            public static T Clone<T>(T source)
            {
                string objXaml = XamlWriter.Save(source);
                StringReader stringReader = new StringReader(objXaml);
                XmlReader xmlReader = XmlReader.Create(stringReader);
                T t = (T)XamlReader.Load(xmlReader);
                return t;
            }
        }

        bool isOn;
        bool isWritable;

        public Window3()
        {
            InitializeComponent();
            this.isOn = true;
        }

        private void toggle_mic(object sender, RoutedEventArgs e)
        {
            Button mic = (sender as Button);
            ImageBrush current = (ImageBrush)mic.OpacityMask;

            if (this.isOn)
            {
                ImageSource micOff = new BitmapImage(new Uri("pack://siteoforigin:,,,/Resources/mic_off.png"));
                current.ImageSource = micOff;
                this.isOn = false;
            }
            else
            {
                ImageSource micOn = new BitmapImage(new Uri("pack://siteoforigin:,,,/Resources/mic_on.png"));
                current.ImageSource = micOn;
                this.isOn = true;
            }
        }

        private void toggle_pencil(object sender, RoutedEventArgs e)
        {
            Button pencil = (sender as Button);
            ImageBrush current = (ImageBrush)pencil.OpacityMask;

            if (this.isWritable)
            {
                ImageSource writable = new BitmapImage(new Uri("pack://siteoforigin:,,,/Resources/pencil.png"));
                current.ImageSource = writable;
                this.isWritable = false;
            }
            else
            {
                ImageSource nonWritable = new BitmapImage(new Uri("pack://siteoforigin:,,,/Resources/pencil2.png"));
                current.ImageSource = nonWritable;
                this.isWritable = true;
            }
        }

        private void add_user(object sender, RoutedEventArgs e)
        {
            StackPanel userList = (StackPanel)this.FindName("userList");
            Canvas userInfo = (Canvas)this.FindName("userInfo");

            Canvas addInfo = WPFObjectCopier.Clone<Canvas>(userInfo);
            (addInfo.Children[0] as Label).Content = userList.Children.Count.ToString();
            if(userList.Children.Count * userInfo.Height > userList.Height)
            {
                userList.Height += userInfo.Height;
            }
            userList.Children.Add(addInfo);
        }
    }
}
