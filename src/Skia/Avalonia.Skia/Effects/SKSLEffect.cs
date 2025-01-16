using System.Collections.Generic;
using Avalonia.Logging;
using Avalonia.Media;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Drawing;
using Avalonia.Rendering.Composition.Server;
using Avalonia.Rendering.Composition.Transport;
using SkiaSharp;

namespace Avalonia.Skia.Effects
{
    public class SkSLEffect : Effect, IShaderEffect, ICompositionRenderResource<IImmutableEffect>, ICompositorSerializable
    {
        public SKRuntimeShaderBuilder ShaderBuilder { get; set; }

        public string[] ChildShaderNames { get; set; } = [];

        public SKImageFilter?[] Inputs { get; set; } = [];

        private readonly Dictionary<AvaloniaProperty, string> _uniformProperties = new Dictionary<AvaloniaProperty, string>();

        public SkSLEffect(SKRuntimeShaderBuilder builder)
        {
            ShaderBuilder = builder;
        }

        public void RegisterUniform(string name, AvaloniaProperty<int> property) => _uniformProperties.Add(property, name);

        public void RegisterUniform(string name, AvaloniaProperty<float> property) => _uniformProperties.Add(property, name);

        public void RegisterUniform(string name, AvaloniaProperty<Size> property) => _uniformProperties.Add(property, name);

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            if (_uniformProperties.ContainsKey(change.Property))
            {
                _resource.RegisterForInvalidationOnAllCompositors(this);
            }

            base.OnPropertyChanged(change);
        }

        private CompositorResourceHolder<ServerCompositionSimpleShaderEffect> _resource;

        IImmutableEffect ICompositionRenderResource<IImmutableEffect>.GetForCompositor(Compositor c) => _resource.GetForCompositor(c);

        void ICompositionRenderResource.AddRefOnCompositor(Compositor c)
        {
            if (_resource.CreateOrAddRef(c, this, out _, static (cc) => new ServerCompositionSimpleShaderEffect(cc.Server)))
            {
                OnRefOnCompositor();
            }
        }

        void ICompositionRenderResource.ReleaseOnCompositor(Compositor c)
        {
            if (_resource.Release(c))
            {
                OnReleaseOnCompositor();
            }
        }

        protected virtual void OnRefOnCompositor()
        {

        }

        protected virtual void OnReleaseOnCompositor()
        {

        }

        SimpleServerObject? ICompositorSerializable.TryGetServer(Compositor c) => _resource.TryGetForCompositor(c);

        void ICompositorSerializable.SerializeChanges(Compositor c, BatchStreamWriter writer)
        {
            SKRuntimeShaderBuilder builder = new SKRuntimeShaderBuilder(ShaderBuilder);
            foreach (var property in _uniformProperties)
            {
                var value = GetValue(property.Key);
                if (value is int intVal)
                {
                    builder.Uniforms[property.Value] = intVal;
                }
                else if (value is float floatVal)
                {
                    builder.Uniforms[property.Value] = floatVal;
                }
                else if (value is Size sizeVal)
                {
                    float[] val = [(float)sizeVal.Width, (float)sizeVal.Height];
                    builder.Uniforms[property.Value] = val;
                }
                else
                {
                    Logger.TryGet(LogEventLevel.Error, "Effect")?.Log(this, $"Unsupported uniform type: {value?.GetType() ?? null}");
                }
            }

            writer.WriteObject(builder);
            writer.WriteObject(ChildShaderNames);
            writer.WriteObject(Inputs);
        }
    }
}
