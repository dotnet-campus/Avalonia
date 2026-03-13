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
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void SetTransparencyButton_OnClick(object? sender, RoutedEventArgs e)
        {
            TransparencyLevelHint = [WindowTransparencyLevel.AcrylicBlur];
        }

        private void SetNotTransparencyButton_OnClick(object? sender, RoutedEventArgs e)
        {
            TransparencyLevelHint = [WindowTransparencyLevel.None];
        }
    }
}
