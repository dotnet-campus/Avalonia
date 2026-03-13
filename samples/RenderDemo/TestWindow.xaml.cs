using System;
using System.Linq.Expressions;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Rendering;
using RenderDemo.ViewModels;
using MiniMvvm;

namespace RenderDemo
{
    public class TestWindow : Window
    {
        public TestWindow()
        {
            InitializeComponent();
            TransparencyLevelHint = [WindowTransparencyLevel.AcrylicBlur];
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
