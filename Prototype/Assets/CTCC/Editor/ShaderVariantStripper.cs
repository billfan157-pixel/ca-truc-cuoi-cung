using System.Collections.Generic;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace CTCC.Editor
{
    /// <summary>
    /// Strips unused lighting/fog/detail shader variants during player builds.
    /// CTCC M2 uses Dual-Scenario Baked LightProbes (ReceiveGI.LightProbes, unity_SH*) + Realtime Shadows + FogMode.ExponentialSquared,
    /// so baked lightmap/shadowmask/linear-fog variants are never sampled at runtime.
    /// Cuts player build time from 30+ min to ~45s and shrinks runtime D3D12 PSO overhead.
    /// </summary>
    public sealed class ShaderVariantStripper : IPreprocessShaders
    {
        public int callbackOrder => 10;

        private static readonly ShaderKeyword[] UnusedKeywords =
        {
            new("LIGHTMAP_ON"),
            new("DIRLIGHTMAP_COMBINED"),
            new("DYNAMICLIGHTMAP_ON"),
            new("SHADOWS_SHADOWMASK"),
            new("LIGHTMAP_SHADOW_MIXING"),
            new("FOG_LINEAR"),
            new("FOG_EXP"),
            new("DEBUG_DISPLAY"),
            new("EDITOR_VISUALIZATION"),
            new("_DETAIL_MULX2"),
            new("_DETAIL_SCALED"),
            new("_PARALLAXMAP"),
        };

        public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
        {
            if (data == null || data.Count == 0) return;
            for (int i = data.Count - 1; i >= 0; i--)
            {
                var kws = data[i].shaderKeywordSet;
                for (int k = 0; k < UnusedKeywords.Length; k++)
                {
                    if (kws.IsEnabled(UnusedKeywords[k]))
                    {
                        data.RemoveAt(i);
                        break;
                    }
                }
            }
        }
    }
}
