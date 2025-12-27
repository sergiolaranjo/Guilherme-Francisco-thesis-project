// ============================================================================
// CardiacVR - VR Platform for Cardiac Surgery Planning
// Copyright (c) 2024 Sérgio Laranjo
// Computational Cardiology, AI and Data Science for Health Lab
// Nova Medical School, Universidade Nova de Lisboa
// All rights reserved.
// ============================================================================

using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Suppress Unity 6 URP deprecation warnings for legacy render pass methods
#pragma warning disable CS0672
#pragma warning disable CS0618

public class KawaseBlur : ScriptableRendererFeature
{
    [System.Serializable]
    public class KawaseBlurSettings
    {
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
        public Material blurMaterial = null;

        [Range(2, 15)]
        public int blurPasses = 1;

        [Range(1, 4)]
        public int downsample = 1;
        public bool copyToFramebuffer;
        public string targetName = "_blurTexture";
    }

    public KawaseBlurSettings settings = new KawaseBlurSettings();

    class CustomRenderPass : ScriptableRenderPass
    {
        public Material blurMaterial;
        public int passes;
        public int downsample;
        public bool copyToFramebuffer;
        public string targetName;
        string profilerTag;

        private RTHandle sourceHandle;
        private RTHandle tmpRT1Handle;
        private RTHandle tmpRT2Handle;

        public CustomRenderPass(string profilerTag)
        {
            this.profilerTag = profilerTag;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            var descriptor = renderingData.cameraData.cameraTargetDescriptor;
            descriptor.width /= downsample;
            descriptor.height /= downsample;
            descriptor.depthBufferBits = 0;
            descriptor.colorFormat = RenderTextureFormat.ARGB32;

            // Allocate temporary render textures using RTHandle
            RenderingUtils.ReAllocateHandleIfNeeded(ref tmpRT1Handle, descriptor, FilterMode.Bilinear, name: "tmpBlurRT1");
            RenderingUtils.ReAllocateHandleIfNeeded(ref tmpRT2Handle, descriptor, FilterMode.Bilinear, name: "tmpBlurRT2");

            // Get source from camera color target handle
            sourceHandle = renderingData.cameraData.renderer.cameraColorTargetHandle;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (blurMaterial == null)
                return;

            CommandBuffer cmd = CommandBufferPool.Get(profilerTag);

            // First pass
            cmd.SetGlobalFloat("_offset", 1.5f);
            Blit(cmd, sourceHandle, tmpRT1Handle, blurMaterial);

            RTHandle currentSource = tmpRT1Handle;
            RTHandle currentDest = tmpRT2Handle;

            for (var i = 1; i < passes - 1; i++)
            {
                cmd.SetGlobalFloat("_offset", 0.5f + i);
                Blit(cmd, currentSource, currentDest, blurMaterial);

                // Pingpong
                var tmp = currentSource;
                currentSource = currentDest;
                currentDest = tmp;
            }

            // Final pass
            cmd.SetGlobalFloat("_offset", 0.5f + passes - 1f);
            if (copyToFramebuffer)
            {
                Blit(cmd, currentSource, sourceHandle, blurMaterial);
            }
            else
            {
                Blit(cmd, currentSource, currentDest, blurMaterial);
                cmd.SetGlobalTexture(targetName, currentDest);
            }

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            // RTHandles are managed by RenderingUtils.ReAllocateHandleIfNeeded
        }

        public void Dispose()
        {
            tmpRT1Handle?.Release();
            tmpRT2Handle?.Release();
        }
    }

    CustomRenderPass scriptablePass;

    public override void Create()
    {
        scriptablePass = new CustomRenderPass("KawaseBlur");
        scriptablePass.blurMaterial = settings.blurMaterial;
        scriptablePass.passes = settings.blurPasses;
        scriptablePass.downsample = settings.downsample;
        scriptablePass.copyToFramebuffer = settings.copyToFramebuffer;
        scriptablePass.targetName = settings.targetName;
        scriptablePass.renderPassEvent = settings.renderPassEvent;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (settings.blurMaterial != null)
        {
            renderer.EnqueuePass(scriptablePass);
        }
    }

    protected override void Dispose(bool disposing)
    {
        scriptablePass?.Dispose();
    }
}

#pragma warning restore CS0672
#pragma warning restore CS0618
