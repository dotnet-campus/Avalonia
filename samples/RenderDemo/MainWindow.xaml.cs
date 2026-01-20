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
    public class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            var vm = new MainWindowViewModel();
            ViewModel = vm;

            void BindOverlay(Expression<Func<MainWindowViewModel, bool>> expr, RendererDebugOverlays overlay)
                => vm.WhenAnyValue(expr).Subscribe(x =>
                {
                    var diagnostics = RendererDiagnostics;
                    diagnostics.DebugOverlays = x ?
                        diagnostics.DebugOverlays | overlay :
                        diagnostics.DebugOverlays & ~overlay;
                });

            BindOverlay(x => x.DrawDirtyRects, RendererDebugOverlays.DirtyRects);
            BindOverlay(x => x.DrawFps, RendererDebugOverlays.Fps);
            BindOverlay(x => x.DrawLayoutTimeGraph, RendererDebugOverlays.LayoutTimeGraph);
            BindOverlay(x => x.DrawRenderTimeGraph, RendererDebugOverlays.RenderTimeGraph);

            DataContext = vm;

            SizeChanged += MainWindow_SizeChanged;
        }

        private MainWindowViewModel ViewModel { get; }

        private void MainWindow_SizeChanged(object? sender, SizeChangedEventArgs e)
        {
            ViewModel.WindowSizeText = $"Width: {e.NewSize.Width:0.00}, Height: {e.NewSize.Height:0.00}";
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void AddWindowSizeButton_OnClick(object? sender, RoutedEventArgs e)
        {
            ViewModel.Width += 100;
            ViewModel.Height += 100;
        }
    }
}
