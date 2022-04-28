using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class BeatDetection : MonoBehaviour
{
    public int blockSize = 1024;

    private float[][] _samples;

    public float[] max;
    public float[] min;

    public int channelsCount = 2;

    public Transform[] channels;
    public Material[] materials;

    public int sampleRate;

    public float mult;

    private int _sampleBlocks;
    public float[] frameEnergy;
    private int _frameIndex;
    public float[] frameAvgE;
    private int _avgEIndex;
    private float _varE;
    private float _c;

    public Transform averageE;
    public Transform c;

    private AudioSource _audioSource;

    private Texture2D _rawSample;
    private Image _imgRawSample;
    private Color[] _pixelsRawSample;

    private static Color _white = Color.white;
    private static Color _black = Color.black;

    private static float WrapSample(float sample)
    {
        return (sample + 1f) / 2f;
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

        // var errorSum = 0f;
        // for (int i = 0; i < _sampleBlocks; i++)
        // {
        //     var error = Mathf.Pow(sum - _frameEnergy[(_frameIndex + i) % _sampleBlocks], 2f);
        //     errorSum += error;
        // }

        // errorSum /= _sampleBlocks;
        // _varE = errorSum;
        // _c = mult * _varE + 1.5142857f;

        var height = frameAvgE[currAvgEIndex];
        averageE.localScale = new Vector3(10f, height, 1f);
        var cpos = averageE.localPosition;
        averageE.localPosition = new Vector3(cpos.x, height / 2f, cpos.z);

        // height = _c * 3f;
        // c.localScale = new Vector3(10f, height, 1f);
        // cpos = c.localPosition;
        // c.localPosition = new Vector3(cpos.x, height / 2f, cpos.z);
    }

    // Start is called before the first frame update
    private void Start()
    {
        _sampleBlocks = Mathf.FloorToInt(sampleRate / blockSize);
        frameEnergy = new float[_sampleBlocks];
        frameAvgE = new float[_sampleBlocks];
        _avgEIndex = 0;

        _samples = new float[channelsCount][];
        max = new float[channelsCount];
        min = new float[channelsCount];
        _audioSource = GetComponent<AudioSource>();
        for (var chn = 0; chn < channelsCount; chn++)
        {
            _samples[chn] = new float[blockSize];
            max[chn] = -99999999f;
            min[chn] = 99999999f;
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
    }

    private void Update()
    {
        for (var chn = 0; chn < channelsCount; chn++)
        {
            _audioSource.GetOutputData(_samples[chn], chn);
            max[chn] = Mathf.Max(_samples[chn].Max(), max[chn]);
            min[chn] = Mathf.Min(_samples[chn].Min(), min[chn]);
            for (var i = 0; i < blockSize; i++)
            {
                var rawS = _samples[chn][i];
                // var s = WrapSample(rawS);
                // var x = (float) i; // i % 32;
                // var y = 0f; //i / 32;
                // var obj = channels[chn].GetChild(i);
                // var h = s * 500f;
                // obj.localScale = new Vector3(1, h, 1);
                // obj.localPosition = new Vector3(x, h / 2, y);

                // norm 2 1024
                if (chn == 1)
                {
                    var t1024_0 = Mathf.FloorToInt((rawS + 1f) / 2f * 1024);
                    var t1024_1 = Mathf.FloorToInt((_samples[0][i] + 1f) / 2f * 1024);
                    var t1024 = (t1024_0 + t1024_1) / 2;
                    for (int j = 0; j < t1024; j++)
                    {
                        _pixelsRawSample[i + j * 1024] = _black;
                    }

                    for (int j = t1024; j < 1024; j++)
                    {
                        _pixelsRawSample[i + j * 1024] = _white;
                    }
                }
            }

            if (chn == 1)
            {
                _rawSample.SetPixels(0, 0, 1024, 1024, _pixelsRawSample);
                _rawSample.Apply();
            }
        }

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
