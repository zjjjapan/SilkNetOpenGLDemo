using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using System;
using System.IO;
using System.Numerics;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Interop;
using System.Windows.Media;
using Silk.NET.Maths;

namespace SilkNetOpenGLDemo
{
    public partial class MainWindow : System.Windows.Window
    {
        

        public MainWindow()
        {
            InitializeComponent();
        }

        private void MoveLineButton_Click(object sender, RoutedEventArgs e)
        {
            // 偏移值：每次点击按钮移动 10 像素
            LineControl.MoveLine(10f, 10f);
        }
    }

    
}
