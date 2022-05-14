using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class BeatDetection : MonoBehaviour
{
    public int blockSize = 1024;

    [Range(1, 64)]
    public int prevFrames = 10;

    private float[][] _samples;
    private float[][][] _prevSamples;

    // public float[] max;
    // public float[] min;

    public int channelsCount = 2;

    public Transform[] channels;
    public Material[] materials;
    private Material _imageMat;

    public int sampleRate;

    public float mult;

    private int _sampleBlocks;
    public float[] frameEnergy;
    private int _frameIndex;
    public float[] frameAvgE;
    private int _avgEIndex;
    private float _varE;
    private float _c;
    private float _maxC;

    public Transform averageE;
    public Transform c;

    private AudioSource _audioSource;

    private Texture2D _rawSample;
    private Image _imgRawSample;
    private Color[] _pixelsRawSample;

    private static Color _white = new Color(1, 1, 1, 1);
    private static Color _black = new Color(0, 0, 0, 0);
    private static readonly int BeatStrength = Shader.PropertyToID("_BeatStrength");

    private static float WrapSample(float sample)
    {
        return (sample + 1f) / 2f;
    }

    private static float Divide(float dividend, float divisor)
    {
        return Mathf.Approximately(divisor, 0.000001f) ? 0f : dividend / divisor;
    }

    public void ComputeSoundEnergy()
    {
        var e = 0f;
        for (int i = 0; i < blockSize; i++)
        {
            var sum = 0f;
            for (int j = 0; j < channelsCount; j++)
            {
                // var sample = WrapSample(_samples[j][i]);
                var sample = _samples[j][i];
                sum += Mathf.Pow(sample, 2);
            }

            e += sum;
        }

        frameEnergy[_frameIndex++ % _sampleBlocks] = e;
    }

    public void UpdateAverageE()
    {
        var sum = 0f;
        for (var i = 1; i <= _sampleBlocks; i++)
        {
            sum += frameEnergy[(_frameIndex + i) % _sampleBlocks];
        }

        sum /= _sampleBlocks;
        var currAvgEIndex = _avgEIndex++ % _sampleBlocks;
        frameAvgE[currAvgEIndex] = sum;

        var errorSum = 0f;
        for (int i = 0; i < _sampleBlocks; i++)
        {
            var error = Mathf.Pow(sum - frameEnergy[(_frameIndex + i) % _sampleBlocks], 2f);
            errorSum += error;
        }

        errorSum /= _sampleBlocks;
        _varE = errorSum;
        _c = Mathf.Abs(mult * _varE + 1.5142857f);
        _maxC = Mathf.Max(_c, _maxC);

        var height = frameAvgE[currAvgEIndex];
        averageE.localScale = new Vector3(10f, height, 1f);
        var cpos = averageE.localPosition;
        averageE.localPosition = new Vector3(cpos.x, height / 2f, cpos.z);
        var normalizedC = Divide(_c, _maxC);

        height = Divide(_c, _maxC) * 100;
        c.localScale = new Vector3(10f, height, 1f);
        cpos = c.localPosition;
        c.localPosition = new Vector3(cpos.x, height / 2f, cpos.z);
        
        _imageMat.SetFloat(BeatStrength, normalizedC);
    }

    // Start is called before the first frame update
    private void Start()
    {
        prevFrames = Mathf.FloorToInt(Mathf.Pow(2, Mathf.FloorToInt(Mathf.Log(prevFrames) / Mathf.Log(2))));
        _sampleBlocks = Mathf.FloorToInt((float) sampleRate / blockSize);
        frameEnergy = new float[_sampleBlocks];
        frameAvgE = new float[_sampleBlocks];
        _avgEIndex = 0;

        _samples = new float[channelsCount][];
        _prevSamples = new float[prevFrames][][];
        for (int i = 0; i < prevFrames; i++)
        {
            _prevSamples[i] = new float[channelsCount][];
        }

        // max = new float[channelsCount];
        // min = new float[channelsCount];
        _audioSource = GetComponent<AudioSource>();
        for (var chn = 0; chn < channelsCount; chn++)
        {
            _samples[chn] = new float[blockSize];
            for (int i = 0; i < prevFrames; i++)
            {
                _prevSamples[i][chn] = new float[blockSize];
            }

            // max[chn] = -99999999f;
            // min[chn] = 99999999f;
            // for (var i = 0; i < blockSize; i++)
            // {
            //     var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            //     obj.GetComponent<Renderer>().material = materials[chn];
            //     obj.transform.SetParent(channels[chn]);
            // }
        }

        _imgRawSample = transform.Find("Canvas/sp_raw_sample").GetComponent<Image>();
        _rawSample = _imgRawSample.sprite.texture;
        _pixelsRawSample = new Color[1024 * 1024];
        _imageMat = _imgRawSample.material;
    }

    private static int Normalize(float channel0, float channel1)
    {
        var t1024_0 = Mathf.FloorToInt((channel0 + 1f) / 2f * 1024);
        var t1024_1 = Mathf.FloorToInt((channel1 + 1f) / 2f * 1024);
        return (t1024_0 + t1024_1) / 2;
    }

    private static int ModIndex(int i, int offset, int mod)
    {
        return (i + offset + mod) % mod;
    }

    private void Update()
    {
        var currentFrameOfPrevFrames = Time.frameCount % prevFrames;
        // for (int i = 0; i < channelsCount; i++)
        // {
        //     Array.Copy(_samples[i], _prevSamples[currentFrameOfPrevFrames][i], _samples[i].Length);
        // }

        for (var chn = 0; chn < channelsCount; chn++)
        {
            _audioSource.GetOutputData(_samples[chn], chn);
            Array.Copy(_samples[chn], _prevSamples[currentFrameOfPrevFrames][chn], _samples[chn].Length);
            // max[chn] = Mathf.Max(_samples[chn].Max(), max[chn]);
            // min[chn] = Mathf.Min(_samples[chn].Min(), min[chn]);
        }

        var i = 0;
        for (var y = 0; y < prevFrames; y++)
        {
            var x = (y + currentFrameOfPrevFrames) % prevFrames;
            for (var n = 0; n < blockSize; n += prevFrames)
            {
                var curr = Normalize(_prevSamples[x][0][n], _prevSamples[x][1][n]);

                // norm 2 1024
                for (var j = 0; j < curr; j++)
                {
                    _pixelsRawSample[i + j * 1024] = _black;
                }

                for (var j = curr; j < 1024; j++)
                {
                    _pixelsRawSample[i + j * 1024] = _white;
                }

                i++;
            }
        }

        _rawSample.SetPixels(0, 0, 1024, 1024, _pixelsRawSample);
        _rawSample.Apply();

        ComputeSoundEnergy();
        UpdateAverageE();
    }

    public static int Factorial(int number)
    {
        int i;
        var fact = number;
        for (i = number - 1; i > 1; i--)
        {
            fact *= i;
        }

        return fact;
    }

    private static float Normalize(IReadOnlyList<float> input)
    {
        Debug.Log($"Input list is {string.Join(",", input)}");
        var dividend = 0f;
        var divisor = 0f;
        for (var i = 0; i < input.Count; i++)
        {
            dividend += 1f / Factorial(i + 1) * input[i];
            divisor += 1f / Factorial(i + 1);
        }

        Debug.Log($"{dividend} / {divisor} = {dividend / divisor}");
        return dividend / divisor;
    }

    [MenuItem("Math/TestNormalize")]
    public static void TestNormalize()
    {
        Debug.Log(Normalize(new[] {1f, 2f, 4f, 8f, 16f, 32f}));
        Debug.Log(Normalize(new[] {1f, 2f, 4f, 8f, 16f}));
        Debug.Log(Normalize(new[] {1f, 2f, 4f, 8f}));
        Debug.Log(Normalize(new[] {1f, 2f, 4f}));
        Debug.Log(Normalize(new[] {1f, 2f}));
    }
}