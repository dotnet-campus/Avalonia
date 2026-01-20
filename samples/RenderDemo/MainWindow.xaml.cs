using System;
using System.Linq.Expressions;
using System.Numerics;
using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Rendering;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Animations;
using RenderDemo.ViewModels;
using MiniMvvm;

namespace RenderDemo
{
    public class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.AttachDevTools();

            var vm = new MainWindowViewModel();

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
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private Vector3DKeyFrameAnimation? _vector3DKeyFrameAnimation;
        private CompositionVisual? _scanBorderCompositionVisual;

        private void ControlButton_OnClick(object? sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var grid = (Grid) button.Parent;
            var ScanBorder = grid.Children[0];

            _scanBorderCompositionVisual = ElementComposition.GetElementVisual(ScanBorder)!;
            var compositor = _scanBorderCompositionVisual.Compositor;

            _vector3DKeyFrameAnimation = compositor.CreateVector3DKeyFrameAnimation();
            _vector3DKeyFrameAnimation.InsertKeyFrame(0f, _scanBorderCompositionVisual.Offset with { Y = 0 });
            _vector3DKeyFrameAnimation.InsertKeyFrame(1f, _scanBorderCompositionVisual.Offset with { Y = this.Bounds.Height - ScanBorder.Height });
            _vector3DKeyFrameAnimation.Duration = TimeSpan.FromSeconds(2);
            _vector3DKeyFrameAnimation.IterationBehavior = AnimationIterationBehavior.Count;
            _vector3DKeyFrameAnimation.IterationCount = 3000;

            _scanBorderCompositionVisual.StartAnimation("Offset", _vector3DKeyFrameAnimation);
        }

        private void BeginAnimation(bool flag, Control rect)
        {
            var easing = new SplineEasing(0.1, 0.9, 0.2);

            var from = flag ? new Vector3(-500, 225, 0) : new Vector3(150, 225, 0);
            var to = flag ? new Vector3(150, 225, 0) : new Vector3(-500, 225, 0);

            var visual = ElementComposition.GetElementVisual(rect)!;
            var compositor = visual.Compositor;
            var ani = compositor.CreateVector3KeyFrameAnimation();

            ani.InsertKeyFrame(0f, from, easing);
            ani.InsertKeyFrame(1f, to, easing);

            ani.Duration = TimeSpan.FromMilliseconds(445);

            visual.StartAnimation("Offset", ani);
        }

        private void Button_OnClick(object? sender, RoutedEventArgs e)
        {
            _flag = !_flag;

            var button = (Button)sender;
            var grid = (Grid)button.Parent;
            var rect = grid.Children[0];

            BeginAnimation(_flag,rect);
        }

        private bool _flag;
    }
}
