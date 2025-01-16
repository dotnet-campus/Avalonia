using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Rendering.Composition.Transport;

namespace Avalonia.Rendering.Composition.Server
{
    internal sealed class ServerCompositionSimpleShaderEffect : SimpleServerRenderResource, IShaderEffect, IImmutableEffect
    {
        public ServerCompositionSimpleShaderEffect(ServerCompositor compositor) : base(compositor)
        {
        }

        public object? ShaderObject { get; set; }

        public string[] ChildShaderNames { get; set; } = [];

        public object?[] Inputs = [];

        protected override void DeserializeChangesCore(BatchStreamReader reader, TimeSpan committedAt)
        {
            base.DeserializeChangesCore(reader, committedAt);

            (ShaderObject as IDisposable)?.Dispose();
            ShaderObject = reader.ReadObject<object>();
            ChildShaderNames = reader.ReadObject<string[]>();
            Inputs = reader.ReadObject<object?[]>();
        }

        public bool Equals(IEffect? other)
        {
            return false;
        }

        public override void Dispose()
        {
            base.Dispose();
            (ShaderObject as IDisposable)?.Dispose();
        }
    }
}
