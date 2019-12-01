//
// The code below was created and published by Mark Szabo and has been
// modified in minor ways. I would like to share all my gratitude for
// the presentation he and his colleagues at Microsoft has held for us
// and made it possible for me to create this application today!
//
// https://github.com/mark-szabo/digit-recognizer-bot
//

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.Primitives;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DigitRecognizerBot.Graphics
{
    public class ImagePreprocessor
    {
        private readonly Rgba32 _backgroundColor = Rgba32.White;
        private readonly Rgba32 _foregroundColor = Rgba32.Black;

        public ImagePreprocessor() { }

        public ImagePreprocessor(Rgba32 backgroundColor, Rgba32 foregroundColor)
        {
            _backgroundColor = backgroundColor;
            _foregroundColor = foregroundColor;
        }

        /// <summary>
        /// Preprocess camera images for MNIST-based neural networks.
        /// </summary>
        /// <param name="image">Source image in a byte array.</param>
        /// <returns>Preprocessed image in a byte array.</returns>
        public byte[] Preprocess(byte[] input)
        {
            Image<Rgba32> image = Image.Load(input);

            image = Preprocess(image);

            var stream = new MemoryStream();
            image.SaveAsPng(stream);

            return stream.ToArray();
        }

        /// <summary>
        /// Preprocess camera images for MNIST-based neural networks.
        /// </summary>
        /// <param name="image">Source image in a file format agnostic structure in memory as a series of Rgba32 pixels.</param>
        /// <returns>Preprocessed image in a file format agnostic structure in memory as a series of Rgba32 pixels.</returns>
        public Image<Rgba32> Preprocess(Image<Rgba32> image)
        {
            // Step 1: Apply a grayscale filter 
            image.Mutate(i => i.Grayscale());

            // Step 2: Apply a white vignette on the corners to remove shadow marks
            image.Mutate(i => i.Vignette(Rgba32.White));

            // Step 3: Separate foreground and background with a threshold and set the correct colors
            image.Mutate(i => i.BinaryThreshold(0.6f, _backgroundColor, _foregroundColor));

            // Step 4: Crop to bounding box
            var boundingBox = FindBoundingBox(image);
            image.Mutate(i => i.Crop(boundingBox));

            // Step 5: Make the image a square
            var maxWidthHeight = Math.Max(image.Width, image.Height);
            image.Mutate(i => i.Pad(maxWidthHeight, maxWidthHeight).BackgroundColor(_backgroundColor));

            // Step 6: Downscale to 20x20
            image.Mutate(i => i.Resize(20, 20));

            // Step 7: Add 4 pixel margin
            image.Mutate(i => i.Pad(28, 28).BackgroundColor(_backgroundColor));

            return image;
        }

        private Rectangle FindBoundingBox(Image<Rgba32> image)
        {
            // ➡
            var topLeftX = F(0, 0, x => x < image.Width, y => y < image.Height, true, 1);

            // ⬇
            var topLeftY = F(0, 0, y => y < image.Height, x => x < image.Width, false, 1);

            // ⬅
            var bottomRightX = F(image.Width - 1, image.Height - 1, x => x >= 0, y => y >= 0, true, -1);

            // ⬆
            var bottomRightY = F(image.Height - 1, image.Width - 1, y => y >= 0, x => x >= 0, false, -1);

            return new Rectangle(topLeftX, topLeftY, bottomRightX - topLeftX, bottomRightY - topLeftY);

            int F(int coordinateI, int coordinateJ, Func<int, bool> comparerI, Func<int, bool> comparerJ, bool horizontal, int increment)
            {
                var limit = 0;
                for (int i = coordinateI; comparerI(i); i += increment)
                {
                    bool foundForegroundPixel = false;
                    for (int j = coordinateJ; comparerJ(j); j += increment)
                    {
                        var pixel = horizontal ? image[i, j] : image[j, i];
                        if (pixel != _backgroundColor)
                        {
                            foundForegroundPixel = true;
                            break;
                        }
                    }

                    if (foundForegroundPixel) break;
                    limit = i;
                }

                return limit;
            }
        }

        public static int[] ConvertImageToArray(Image<Rgba32> image)
        {
            var pixels = new int[28 * 28];
            var i = 0;
            for (int j = 0; j < image.Height; j++)
            {
                for (int k = 0; k < image.Width; k++)
                {
                    pixels[i] = 255 - ((image[k, j].R + image[k, j].G + image[k, j].B) / 3);
                    i++;
                }
            }

            return pixels;
        }

        public static double[,] ConvertImageToTwoDimensionalArray(Image<Rgba32> image)
        {
            var pixels = new double[28, 28];
            for (int j = 0; j < image.Height; j++)
            {
                for (int k = 0; k < image.Width; k++)
                {
                    pixels[j, k] = (255 - ((image[k, j].R + image[k, j].G + image[k, j].B) / 3)) / 255;
                }
            }

            return pixels;
        }

        private static void PrintImageToConsole(Image<Rgba32> image)
        {
            var pixels = ConvertImageToArray(image);
            for (int i = 0; i < 784; i++)
            {
                Console.Write(pixels[i].ToString("D3"));
                if ((i + 1) % 28 == 0) Console.WriteLine();
            }
        }
    }
}
