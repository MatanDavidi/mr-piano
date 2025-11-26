//using PassthroughCameraSamples;
using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Linq;
using TMPro;
using Unity.InferenceEngine;
using UnityEngine;
using UnityEngine.Networking;

public class ModelManager : MonoBehaviour
{

    [SerializeField] private ModelAsset modelAsset;
    //[SerializeField] private TMP_Text speedText;

    private Model runtimeModel;
    private Worker worker;

    private Model normModel;
    private Worker normWorker;


    [Header("[Editor Only] Convert to Sentis")]
    public ModelAsset OnnxModel;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        InitializeModel();
    }

    // Update is called once per frame
    void Update()
    {
    }

    void InitializeModel()
    {
        AddNormalizationHead();
        runtimeModel = ModelLoader.Load(modelAsset);
        UnityEngine.Debug.Log("model loaded correctly!");
        worker = new Worker(runtimeModel, BackendType.GPUCompute);
        UnityEngine.Debug.Log("Sentis Model Initialized!");
        WarmUpSentis();
    }

    /// <summary>
    /// Runs inference in a blocking and synchronous way.
    /// </summary>
    /// <param name="targetTexture">Input texture</param>
    /// <returns>Array of keypoints (Vector2)</returns>
    public Vector2[] RunInference(Texture targetTexture)
    {
        TensorShape inputShape = new TensorShape(1, 3, targetTexture.height, targetTexture.width);
        Tensor<float> input = new Tensor<float>(inputShape);
        TextureConverter.ToTensor(targetTexture, input);
        Tensor<float> inputCpu = input.ReadbackAndClone();

        SaveTensorForPyTorch(inputCpu, "input.tensor");

        normWorker.Schedule(input);
        Tensor<float> normOutput = normWorker.PeekOutput() as Tensor<float>;
        var cpuNormOutput = normOutput.ReadbackAndClone();

        SaveTensorForPyTorch(cpuNormOutput, "normalized.tensor");

        // Normalization Network :))))))))
        Stopwatch watch = Stopwatch.StartNew();
        worker.Schedule(cpuNormOutput);
        Tensor<float> output = worker.PeekOutput("kpts") as Tensor<float>;
        var cpuTensor = output.ReadbackAndClone();
        watch.Stop();

        SaveTensorForPyTorch(cpuTensor, "kpts.tensor");
        Vector2[] kpts = parseKeyPoints(cpuTensor);

        foreach (Vector2 kpt in kpts)
        {
            UnityEngine.Debug.Log($"[{kpt.x}, {kpt.y}]");
        }

        UnityEngine.Debug.Log($"Inference took {watch.ElapsedMilliseconds}, output shape: {cpuTensor.shape}");

        
        //speedText.text = watch.ElapsedMilliseconds.ToString() + " ms";

        input.Dispose();
        cpuTensor.Dispose();

        return kpts;
    }

    //public void RunInferenceAsync(Texture targetTexture)
    //{
    //    TensorShape inputShape = new TensorShape(1, 3, targetTexture.height, targetTexture.width);
    //    Tensor<float> input = new Tensor<float>(inputShape);
    //    TextureConverter.ToTensor(targetTexture, input);

    //    var inputCpu = input.ReadbackAndClone();
    //    SaveTensorForPyTorch(inputCpu, "input.tensor");

    //    // 1️⃣ Schedule normalization worker
    //    normWorker.Schedule(input);

    //    // 2️⃣ Read normalization output asynchronously
    //    var normOutput = normWorker.PeekOutput() as Tensor<float>;
    //    var awaiter1 = normOutput.ReadbackAndCloneAsync().GetAwaiter();
    //    awaiter1.OnCompleted(() =>
    //    {
    //        var cpuNormOutput = awaiter1.GetResult();
    //        SaveTensorForPyTorch(cpuNormOutput, "normalized.tensor");

    //        UnityEngine.Debug.Log("Normalization finished.");
    //        // 3️⃣ Schedule main worker AFTER normalization is ready
    //        worker.Schedule(normOutput);

    //        // 4️⃣ Read main output asynchronously
    //        //var output = worker.PeekOutput("kpts") as Tensor<float>;
    //        //var awaiter2 = output.ReadbackAndCloneAsync().GetAwaiter();
    //        //awaiter2.OnCompleted(() =>
    //        //{
    //        //    var cpuOutput = awaiter2.GetResult();
    //        //    SaveTensorForPyTorch(cpuOutput, "kpts.tensor");

    //        //    Vector2[] kpts = parseKeyPoints(cpuOutput);
    //        //    foreach (Vector2 kpt in kpts)
    //        //        UnityEngine.Debug.Log($"[{kpt.x}, {kpt.y}]");

    //        //    UnityEngine.Debug.Log($"Inference complete, output shape: {cpuOutput.shape}");

    //        //    // Clean up
    //        //    input.Dispose();
    //        //    cpuOutput.Dispose();
    //        //    normOutput.Dispose();
    //        //});

    //        var output = worker.PeekOutput("heatmaps") as Tensor<float>;
    //        var awaiter2 = output.ReadbackAndCloneAsync().GetAwaiter();
    //        awaiter2.OnCompleted(() =>
    //        {
    //            var cpuOutput = awaiter2.GetResult();
    //            SaveTensorForPyTorch(cpuOutput, "heatmaps.tensor");
    //            UnityEngine.Debug.Log($"Inference complete, output shape: {cpuOutput.shape}");

    //            // Clean up
    //            input.Dispose();
    //            cpuOutput.Dispose();
    //            normOutput.Dispose();
    //        });
    //    });
    //}

    public Vector2[] parseKeyPoints(Tensor<float> tensor)
    {
        Vector2[] kpts = new Vector2[4];
        kpts[0] = new Vector2(tensor[0, 0, 0], tensor[0, 0, 1]);
        kpts[1] = new Vector2(tensor[0, 1, 0], tensor[0, 1, 1]);
        kpts[2] = new Vector2(tensor[0, 2, 0], tensor[0, 2, 1]);
        kpts[3] = new Vector2(tensor[0, 3, 0], tensor[0, 3, 1]);

        return kpts;
    }

    public void AddNormalizationHead()
    {
        // @TODO: Generalize for arbitrary input sizes.
        TensorShape inputShape = new TensorShape(1, 3, 240, 320);
        FunctionalGraph graph = new FunctionalGraph();
        FunctionalTensor inputNode = graph.AddInput<float>(inputShape);
        FunctionalTensor meanNode = Functional.Constant(new TensorShape(1, 3, 1, 1), new float[] { 0.485f, 0.456f, 0.406f });
        FunctionalTensor stdNode = Functional.Constant(new TensorShape(1, 3, 1, 1), new float[] { 0.229f, 0.224f, 0.225f });
        FunctionalTensor subNode = Functional.Sub(inputNode, meanNode);
        FunctionalTensor normNode = Functional.Div(subNode, stdNode);

        normModel = graph.Compile(normNode);
        normWorker = new Worker(normModel, BackendType.GPUCompute);
    }

    void WarmUpSentis()
    {
        // @TODO: Generalize for arbitrary input sizes.
        TensorShape shape = new TensorShape(1, 3, 240, 320);
        using var tensor = new Tensor<float>(shape, clearOnInit: false);
        worker.Schedule(tensor);
        Tensor<float> output = worker.PeekOutput() as Tensor<float>;
        UnityEngine.Debug.Log("Sentis warmed up!");
    }

    public static void SaveTensorForPyTorch(Tensor<float> tensor, string filename)
    {
        // Can later be read as: .\adb pull /sdcard/Android/data/com.DefaultCompany.ARMusicians/files/normalized.tensor ./normalized.tensor
        string path = Path.Combine(Application.persistentDataPath, filename);

        using (BinaryWriter writer = new BinaryWriter(File.Open(path, FileMode.Create)))
        {
            // Write float32 values in row-major order
            for (int i = 0; i < tensor.count; i++)
                writer.Write(tensor[i]);
        }

        UnityEngine.Debug.Log($"Tensor saved to {path}. You can pull it via ADB and load in Python.");
    }
}
