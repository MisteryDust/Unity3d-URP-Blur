using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class TutorialBlurRenderFeature : ScriptableRendererFeature
{
    public enum BlurType
    {
        KawaseBlur,
        DualBlur,
        RadialBlur
    }

    [System.Serializable]
    public class Settings
    {
        public RenderPassEvent passEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        public Shader shader;
        public BlurType blurType;
    }
    private TutorialBlurRenderPass pass;
    [SerializeField]
    public Settings settings = new Settings();
    public override void Create()
    {
        pass = new TutorialBlurRenderPass(settings);
    }
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        pass.renderTarget = renderer.cameraColorTarget;
        renderer.EnqueuePass(pass);
    }
}
public class TutorialBlurRenderPass : ScriptableRenderPass
{
    private Material passMaterial;

    public RenderTargetIdentifier renderTarget;
    private RenderTextureDescriptor renderTextureDescriptor;

    private CustomBlurProcess blurProcess;

    private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
    private static readonly string tag = "TutorialBlur";

    private RenderTargetHandle buffer01, buffer02;
    private TutorialBlurRenderFeature.BlurType blurType;


    public TutorialBlurRenderPass(TutorialBlurRenderFeature.Settings settings)
    {
        this.renderPassEvent = settings.passEvent;

        if (passMaterial == null && settings.shader != null)
        {
            passMaterial = CoreUtils.CreateEngineMaterial(settings.shader);
        }
        blurType = settings.blurType;
    }
    /// <summary>
    /// 重写Configure，主要是拿一下【cameraTextureDescriptor】纹理参数
    /// </summary>
    /// <param name="cmd"></param>
    /// <param name="cameraTextureDescriptor"></param>
    public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
    {
        base.Configure(cmd, cameraTextureDescriptor);
        this.renderTextureDescriptor = cameraTextureDescriptor;
        this.renderTextureDescriptor.depthBufferBits = 0;

        buffer01.Init("buffer01");
        buffer02.Init("buffer02");
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (passMaterial == null) return;
        if (renderingData.cameraData.isSceneViewCamera) return; //Scene视图不予处理
        if (!renderingData.cameraData.postProcessEnabled) return; //摄像机未开启后处理 

        var stack = VolumeManager.instance.stack; //获取全局后处理实例栈
        blurProcess = stack.GetComponent<CustomBlurProcess>(); //获取我们的扩展组件
        if (blurProcess == null) return;

        //cmd执行
        var cmd = CommandBufferPool.Get(tag);

        if (blurType == TutorialBlurRenderFeature.BlurType.DualBlur)
        {
            DualBlur(cmd);
        }
        else if (blurType == TutorialBlurRenderFeature.BlurType.KawaseBlur)
        {
            KawaseBlur(cmd);
        }
        else if (blurType == TutorialBlurRenderFeature.BlurType.RadialBlur)
        {
            RadialBlur(cmd);
        }
        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }


    private void KawaseBlur(CommandBuffer cmd)
    {
        var source = renderTarget; //摄像机图源

        cmd.SetGlobalTexture(MainTexId, source); //设置摄像机纹理到_MainTex

        var dsp = renderTextureDescriptor; //获取纹理参数描述符
        var width = dsp.width / blurProcess.donwSample.value; //降采样宽度
        var height = dsp.height / blurProcess.donwSample.value; //降采样高度
        var blurRange = blurProcess.blurRange.value; //模糊采样距离

        cmd.GetTemporaryRT(buffer01.id, width, height, 0, FilterMode.Bilinear, RenderTextureFormat.ARGB32); //获取临时RT
        cmd.GetTemporaryRT(buffer02.id, width, height, 0, FilterMode.Bilinear, RenderTextureFormat.ARGB32);

        passMaterial.SetFloat("_BlurRange", 0); //初始化BlurRange

        cmd.Blit(source, buffer01.Identifier(), passMaterial, 0); //使用shader的第一个pass进行渲染

        for (int i = 0; i < blurProcess.blurTimes.value; i++) //模糊循环
        {
            passMaterial.SetFloat("_BlurRange", (i + 1) * blurRange); //随着迭代次数，BlurRange逐渐扩大
            cmd.Blit(buffer01.Identifier(), buffer02.Identifier(), passMaterial, 0); //使用shader的第一个pass进行渲染

            var temRT = buffer01; //交换RT
            buffer01 = buffer02;
            buffer02 = temRT;
        }
        cmd.Blit(buffer01.Identifier(), source, passMaterial, 0); //把最后的结果写入摄像机

        cmd.ReleaseTemporaryRT(buffer01.id); //释放临时RT
        cmd.ReleaseTemporaryRT(buffer02.id);
    }

    private void DualBlur(CommandBuffer cmd)
    {
        int width = this.renderTextureDescriptor.width, height = this.renderTextureDescriptor.height;
        var loopCount = blurProcess.blurTimes.value;
        var downSampleRT = new int[loopCount];
        var upSampleRT = new int[loopCount];

        RenderTargetIdentifier tmpRT = renderTarget;

        passMaterial.SetFloat("_BlurRange", blurProcess.blurRange.value);

        //initial
        for (int i = 0; i < loopCount; i++)
        {
            downSampleRT[i] = Shader.PropertyToID("DownSample" + i);//临时图像
            upSampleRT[i] = Shader.PropertyToID("UpSample" + i);//临时图像
        }
        //downSample
        for (int i = 0; i < loopCount; i++)
        {
            cmd.GetTemporaryRT(downSampleRT[i], width, height, 0, FilterMode.Bilinear, RenderTextureFormat.Default);
            cmd.GetTemporaryRT(upSampleRT[i], width, height, 0, FilterMode.Bilinear, RenderTextureFormat.Default);//在down时，顺便把up也申请了
            width = Mathf.Max(width / 2, 1);
            height = Mathf.Max(height / 2, 1);

            cmd.Blit(tmpRT, downSampleRT[i], passMaterial, 1);
            tmpRT = downSampleRT[i];
        }
        //UpSample
        for (int j = loopCount - 1; j >= 0; j--)
        {
            cmd.Blit(tmpRT, upSampleRT[j], passMaterial, 2);
            tmpRT = upSampleRT[j];
        }
        //release
        cmd.Blit(tmpRT, renderTarget);

        for (int i = 0; i < loopCount; i++)
        {
            cmd.ReleaseTemporaryRT(downSampleRT[i]);
            cmd.ReleaseTemporaryRT(upSampleRT[i]);
        }
    }


    private void RadialBlur(CommandBuffer cmd)
    {
        passMaterial.SetFloat("_BlurRange", blurProcess.blurRange.value);
        passMaterial.SetInt("_LoopCount", blurProcess.blurTimes.value);
        passMaterial.SetFloat("_X", blurProcess.centerX.value);
        passMaterial.SetFloat("_Y", blurProcess.centerY.value);

        var dsp = this.renderTextureDescriptor;
        var downSample = this.blurProcess.donwSample.value;
        var height = dsp.height / downSample;
        var width = dsp.width / downSample;

        cmd.GetTemporaryRT(buffer01.id, width, height, 0, FilterMode.Bilinear, RenderTextureFormat.ARGB32);//用来存降采样的
        cmd.GetTemporaryRT(buffer02.id, dsp);//模糊图

        var source = this.renderTarget;

        cmd.Blit(source, buffer01.Identifier());
        cmd.Blit(buffer01.Identifier(), buffer02.Identifier(), passMaterial, 3);
        cmd.Blit(buffer02.Identifier(), source);

        cmd.ReleaseTemporaryRT(buffer01.id);
        cmd.ReleaseTemporaryRT(buffer02.id);
    }

}

