using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
    private float[] _frameEnergy;
    private int _frameIndex;
    private float[] _frameAvgE;
    private int _avgEIndex;
    private float _varE;
    private float _c;

    public Transform averageE;
    public Transform c;

    private AudioSource _audioSource;

    public void ComputeSoundEnergy()
    {
        var e = 0f;
        for (int i = 0; i < blockSize; i++)
        {
            var sum = 0f;
            for (int j = 0; j < channelsCount; j++)
            {
                var sample = _samples[j][i];
                sum += Mathf.Pow(sample, 2);
            }

            e += sum;
        }

        _frameEnergy[_frameIndex++ % _sampleBlocks] = e;
    }

    public void UpdateAverageE()
    {
        var sum = 0f;
        for (int i = 1; i <= _sampleBlocks; i++)
        {
            sum += _frameEnergy[(_frameIndex + i) % _sampleBlocks];
        }

        sum /= _sampleBlocks;
        _frameAvgE[_avgEIndex++ % _sampleBlocks] = sum;

        var errorSum = 0f;
        for (int i = 0; i < _sampleBlocks; i++)
        {
            var error = Mathf.Pow(sum - _frameEnergy[(_frameIndex + i) % _sampleBlocks], 2f);
            errorSum += error;
        }

        errorSum /= _sampleBlocks;
        _varE = errorSum;
        _c = mult * _varE + 1.5142857f;

        var height = sum * 3f;
        averageE.localScale = new Vector3(10f, height, 1f);
        var cpos = averageE.localPosition;
        averageE.localPosition = new Vector3(cpos.x, height / 2f, cpos.z);

        height = _c * 3f;
        c.localScale = new Vector3(10f, height, 1f);
        cpos = c.localPosition;
        c.localPosition = new Vector3(cpos.x, height / 2f, cpos.z);
    }

    // Start is called before the first frame update
    private void Start()
    {
        _sampleBlocks = Mathf.FloorToInt(sampleRate / blockSize);
        _frameEnergy = new float[_sampleBlocks];
        _frameAvgE = new float[_sampleBlocks];
        _avgEIndex = 0;

        _samples = new float[channelsCount][];
        max = new float[channelsCount];
        min = new float[channelsCount];
        _audioSource = GetComponent<AudioSource>();
        for (var c = 0; c < channelsCount; c++)
        {
            _samples[c] = new float[blockSize];
            max[c] = -99999999f;
            min[c] = 99999999f;
            for (var i = 0; i < blockSize; i++)
            {
                var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                obj.GetComponent<Renderer>().material = materials[c];
                obj.transform.SetParent(channels[c]);
            }
        }
    }

    // Update is called once per frame
    private void Update()
    {
        for (var c = 0; c < channelsCount; c++)
        {
            _audioSource.GetOutputData(_samples[c], c);
            max[c] = Mathf.Max(_samples[c].Max(), max[c]);
            min[c] = Mathf.Min(_samples[c].Min(), min[c]);
            for (var i = 0; i < blockSize; i++)
            {
                var s = _samples[c][i];
                var x = (float) i; // i % 32;
                var y = 0f; //i / 32;
                var obj = channels[c].GetChild(i);
                var h = (1f - (s + 1) / 2f) * 500f;
                obj.localScale = new Vector3(1, h, 1);
                obj.localPosition = new Vector3(x, h / 2, y);
            }
        }

        ComputeSoundEnergy();
        UpdateAverageE();
    }
}
