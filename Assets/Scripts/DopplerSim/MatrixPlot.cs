using System;
using MathNet.Numerics.LinearAlgebra;
using UnityEngine;

namespace DopplerSim
{
    public class MatrixPlot
    {
        public Texture2D texture { get; }
        public int width => texture.width;
        public int height => texture.height;

        public MatrixPlot(int width, int height)
        {
            texture = new Texture2D(width, height, TextureFormat.RGB24, false)
            {
                // Render pixelated graph! We've got high enough resolution that I'd prefer that :))))))
                filterMode = FilterMode.Point
            };
        }

        public void SetDataSlice(Matrix<double> slice, int start)
        {
            int mipmapCount = Math.Min(3, texture.mipmapCount);
            foreach (var (y, x, value) in slice.EnumerateIndexed(Zeros.Include))
            {
                var color = new Color((float)value, (float)value, (float)value);
                // TODO does this one even mipmap?
                for (int mipmapLevel = 0; mipmapLevel < mipmapCount; mipmapLevel++)
                {
                    texture.SetPixel((start + x) % width, y, color, mipmapLevel);
                }
            }

            texture.Apply(false);
        }
    }
}