﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VRMShaders;
using ColorSpace = VRMShaders.ColorSpace;

namespace UniGLTF
{
    /// <summary>
    /// Gltf から MaterialImportParam に変換する
    ///
    /// StandardShader variables
    ///
    /// _Color
    /// _MainTex
    /// _Cutoff
    /// _Glossiness
    /// _Metallic
    /// _MetallicGlossMap
    /// _BumpScale
    /// _BumpMap
    /// _Parallax
    /// _ParallaxMap
    /// _OcclusionStrength
    /// _OcclusionMap
    /// _EmissionColor
    /// _EmissionMap
    /// _DetailMask
    /// _DetailAlbedoMap
    /// _DetailNormalMapScale
    /// _DetailNormalMap
    /// _UVSec
    /// _EmissionScaleUI
    /// _EmissionColorUI
    /// _Mode
    /// _SrcBlend
    /// _DstBlend
    /// _ZWrite
    ///
    /// </summary>
    public static class GltfPbrMaterialImporter
    {
        private static readonly int SrcBlend = Shader.PropertyToID("_SrcBlend");
        private static readonly int DstBlend = Shader.PropertyToID("_DstBlend");
        private static readonly int ZWrite = Shader.PropertyToID("_ZWrite");
        private static readonly int Cutoff = Shader.PropertyToID("_Cutoff");
        public const string ShaderName = "Standard";

        private enum BlendMode
        {
            Opaque,
            Cutout,
            Fade,
            Transparent
        }

        public static bool TryCreateParam(GltfData data, int i, out MaterialDescriptor matDesc)
        {
            if (i < 0 || i >= data.GLTF.materials.Count)
            {
                matDesc = default;
                return false;
            }

            var src = data.GLTF.materials[i];

            var textureSlots = new Dictionary<string, TextureDescriptor>();
            var floatValues = new Dictionary<string, float>();
            var colors = new Dictionary<string, Color>();
            var vectors = new Dictionary<string, Vector4>();
            var actions = new List<Action<Material>>();

            TextureDescriptor? standardTexDesc = default;
            if (src.pbrMetallicRoughness != null || src.occlusionTexture != null)
            {
                if (src.pbrMetallicRoughness.metallicRoughnessTexture != null || src.occlusionTexture != null)
                {
                    if (GltfPbrTextureImporter.TryStandardTexture(data, src, out var key, out var desc))
                    {
                        standardTexDesc = desc;
                    }
                }

                if (src.pbrMetallicRoughness.baseColorFactor != null &&
                    src.pbrMetallicRoughness.baseColorFactor.Length == 4)
                {
                    colors.Add("_Color",
                        src.pbrMetallicRoughness.baseColorFactor.ToColor4(ColorSpace.Linear, ColorSpace.sRGB)
                    );
                }

                if (src.pbrMetallicRoughness.baseColorTexture != null &&
                    src.pbrMetallicRoughness.baseColorTexture.index != -1)
                {
                    if (GltfPbrTextureImporter.TryBaseColorTexture(data, src, out var key, out var desc))
                    {
                        textureSlots.Add("_MainTex", desc);
                    }
                }

                if (src.pbrMetallicRoughness.metallicRoughnessTexture != null &&
                    src.pbrMetallicRoughness.metallicRoughnessTexture.index != -1 &&
                    standardTexDesc.HasValue)
                {
                    actions.Add(material => material.EnableKeyword("_METALLICGLOSSMAP"));
                    textureSlots.Add("_MetallicGlossMap", standardTexDesc.Value);
                    // Set 1.0f as hard-coded. See: https://github.com/dwango/UniVRM/issues/212.
                    floatValues.Add("_Metallic", 1.0f);
                    floatValues.Add("_GlossMapScale", 1.0f);
                }
                else
                {
                    floatValues.Add("_Metallic", src.pbrMetallicRoughness.metallicFactor);
                    floatValues.Add("_Glossiness", 1.0f - src.pbrMetallicRoughness.roughnessFactor);
                }
            }

            if (src.normalTexture != null && src.normalTexture.index != -1)
            {
                actions.Add(material => material.EnableKeyword("_NORMALMAP"));
                if (GltfPbrTextureImporter.TryNormalTexture(data, src, out var key, out var desc))
                {
                    textureSlots.Add("_BumpMap", desc);
                    floatValues.Add("_BumpScale", src.normalTexture.scale);
                }
            }

            if (src.occlusionTexture != null && src.occlusionTexture.index != -1 && standardTexDesc.HasValue)
            {
                textureSlots.Add("_OcclusionMap", standardTexDesc.Value);
                floatValues.Add("_OcclusionStrength", src.occlusionTexture.strength);
            }

            if (src.emissiveFactor != null
                || (src.emissiveTexture != null && src.emissiveTexture.index != -1))
            {
                actions.Add(material =>
                {
                    material.EnableKeyword("_EMISSION");
                    material.globalIlluminationFlags &= ~MaterialGlobalIlluminationFlags.EmissiveIsBlack;
                });

                if (src.emissiveFactor != null && src.emissiveFactor.Length == 3)
                {
                    var emissiveFactor = src.emissiveFactor.ToColor3(ColorSpace.Linear, ColorSpace.Linear);
                    if (UniGLTF.glTF_KHR_materials_emissive_strength.TryGet(src.extensions, out UniGLTF.glTF_KHR_materials_emissive_strength emissiveStrength))
                    {
                        emissiveFactor *= emissiveStrength.emissiveStrength;
                    }
                    else if (UniGLTF.Extensions.VRMC_materials_hdr_emissiveMultiplier.GltfDeserializer.TryGet(src.extensions,
                        out UniGLTF.Extensions.VRMC_materials_hdr_emissiveMultiplier.
                            VRMC_materials_hdr_emissiveMultiplier ex))
                    {
                        emissiveFactor *= ex.EmissiveMultiplier.Value;
                    }

                    colors.Add("_EmissionColor", emissiveFactor);
                }

                if (src.emissiveTexture != null && src.emissiveTexture.index != -1)
                {
                    if (GltfPbrTextureImporter.TryEmissiveTexture(data, src, out var key, out var desc))
                    {
                        textureSlots.Add("_EmissionMap", desc);
                    }
                }
            }

            actions.Add(material =>
            {
                BlendMode blendMode;
                // https://forum.unity.com/threads/standard-material-shader-ignoring-setfloat-property-_mode.344557/#post-2229980
                switch (src.alphaMode)
                {
                    case "BLEND":
                        blendMode = BlendMode.Fade;
                        material.SetOverrideTag("RenderType", "Transparent");
                        material.SetInt(SrcBlend, (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        material.SetInt(DstBlend, (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        material.SetInt(ZWrite, 0);
                        material.DisableKeyword("_ALPHATEST_ON");
                        material.EnableKeyword("_ALPHABLEND_ON");
                        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        material.renderQueue = 3000;
                        break;

                    case "MASK":
                        blendMode = BlendMode.Cutout;
                        material.SetOverrideTag("RenderType", "TransparentCutout");
                        material.SetInt(SrcBlend, (int)UnityEngine.Rendering.BlendMode.One);
                        material.SetInt(DstBlend, (int)UnityEngine.Rendering.BlendMode.Zero);
                        material.SetInt(ZWrite, 1);
                        material.SetFloat(Cutoff, src.alphaCutoff);
                        material.EnableKeyword("_ALPHATEST_ON");
                        material.DisableKeyword("_ALPHABLEND_ON");
                        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        material.renderQueue = 2450;

                        break;

                    default: // OPAQUE
                        blendMode = BlendMode.Opaque;
                        material.SetOverrideTag("RenderType", "");
                        material.SetInt(SrcBlend, (int)UnityEngine.Rendering.BlendMode.One);
                        material.SetInt(DstBlend, (int)UnityEngine.Rendering.BlendMode.Zero);
                        material.SetInt(ZWrite, 1);
                        material.DisableKeyword("_ALPHATEST_ON");
                        material.DisableKeyword("_ALPHABLEND_ON");
                        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        material.renderQueue = -1;
                        break;
                }

                material.SetFloat("_Mode", (float)blendMode);
            });

            matDesc = new MaterialDescriptor(
                GltfMaterialDescriptorGenerator.GetMaterialName(i, src),
                ShaderName,
                null,
                textureSlots,
                floatValues,
                colors,
                vectors,
                actions);

            return true;
        }
    }
}