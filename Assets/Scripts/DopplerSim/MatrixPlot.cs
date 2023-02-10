using System;
using System.Linq;
using DopplerSim.Tools;
using MathNet.Numerics.LinearAlgebra;
using UnityEngine;
using Random = UnityEngine.Random;

namespace DopplerSim
{
    public class MatrixPlot
    {
        public Texture2D texture { get; }
        public int width => texture.width;
        public int height => texture.height;
        public double[][] data { get; private set; }

        public MatrixPlot(int width, int height)
        {
            texture = new Texture2D(width, height, TextureFormat.RGB24, false)
            {
                // Render pixelated graph! We've got high enough resolution that I'd prefer that :))))))
                filterMode = FilterMode.Point
            };
        }

        public void setOneDataRow(int col)
        {
            //Color[] cols = data[col].Select(x => new Color((float) x, (float) x, (float) x, 1.0f)).ToArray();
            // int mipCount = Mathf.Min(3, texture.mipmapCount);
            //
            // // tint each mip level
            // for (int mip = 0; mip < mipCount; ++mip)
            // {
            //     texture.SetPixels(cols, mip);
            // }

            for (int x = 0; x < width; x++)
            {
                texture.SetPixel(x, col, new Color((float) data[col][x], (float) data[col][x], (float) data[col][x]));
            }
            
            texture.Apply(false);
        }

        // TODO use this method to draw one slice per millisecond?
        public void SetDataSlice(Matrix<double> slice, int start)
        {
            foreach (var (y, x, value) in slice.EnumerateIndexed(Zeros.Include))
            {
                texture.SetPixel((start + x) % width, y, new Color((float)value, (float)value, (float)value));
            };
            texture.Apply(false);
        }
        
        public void setData(double[][] data)
        {
            if ((data.Length != height) || (data[0].Length != width)) {
                throw new IndexOutOfRangeException($"Data size ({data[0].Length}x{data.Length}) does not equal Plot size ({width}x{height})!");
            }
            this.data = data;

            Color[] cols = data.Flatten<double>().Select(x => new Color((float) x, (float) x, (float) x, 1.0f)).ToArray();
            int mipCount = Mathf.Min(3, texture.mipmapCount);

            // tint each mip level
            for (int mip = 0; mip < mipCount; ++mip)
            {
                texture.SetPixels(cols, mip);
            }
            texture.Apply(false);
        }

        public void ApplyRandomTexture()
        {
            Debug.Assert(texture.isReadable, "Texture is not readable");
            // colors used to tint the first 3 mip levels
            Color[] colors = new Color[3];
            colors[0] = Color.red;
            colors[1] = Color.green;
            colors[2] = Color.blue;
            int mipCount = Mathf.Min(3, texture.mipmapCount);

            // tint each mip level
            for (int mip = 0; mip < mipCount; ++mip)
            {
                Color[] cols = texture.GetPixels(mip);
                for (int i = 0; i < cols.Length; ++i)
                {
                    cols[i] = new Color(Random.value, Random.value, Random.value); //Color.Lerp(cols[i], colors[mip], 0.33f);
                }
                texture.SetPixels(cols, mip);
            }
            // actually apply all SetPixels, don't recalculate mip levels
            texture.Apply(false);
        }
    
    }
}
