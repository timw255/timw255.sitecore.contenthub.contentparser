using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using Newtonsoft.Json;

namespace timw255.sitecore.contenthub.contentparser
{
    public static class SmartCrop
    {
        // this was a quick port from a library I wrote a while back. the original version resized images (prior to crop) based on the focal area.
        // resizing prior to cropping doesn't seem possible in content hub so, instead of resizing the image, the crop area should probably
        // be scaled to the same aspect ratio as the final output. i didn't feel like figuring that out though so...
        // just don't set "preserveFocalArea" to "true", unless you wanna do some coding first :D
        //
        // TL;DR - this is just a quick and dirty example. don't judge me.
        public static JToken GetConversionConfiguration(int focalPointX, int focalPointY, int focalPointWidth, int focalPointHeight, int focalPointAnchor, int inputWidth, int inputHeight, int outputWidth, int outputHeight, bool preserveFocalArea, bool canScaleUp = false)
        {
            float s = 1;
            bool scaleDown = (focalPointWidth > outputWidth) || (focalPointHeight > outputHeight);
            bool scaleUp = ((focalPointWidth < outputWidth) || (focalPointHeight < outputHeight)) && canScaleUp;
            bool shouldScale = scaleDown || scaleUp;
            Image source = new Bitmap(inputWidth, inputHeight);
            Image scaledSource = source;

            if (preserveFocalArea && shouldScale)
            {
                if (((float)focalPointWidth / (float)outputWidth > (float)focalPointHeight / (float)outputHeight))
                {
                    s = (float)outputWidth / (float)focalPointWidth;
                }
                else
                {
                    s = (float)outputHeight / (float)focalPointHeight;
                }
                //ImagesHelper.TryResizeImage(source, (int)Math.Round(source.Width * s, 0), (int)Math.Round(source.Height * s, 0), out scaledSource, args.Quality);
                scaledSource = new Bitmap((int)Math.Round(source.Width * s, 0), (int)Math.Round(source.Height * s, 0));
            }

            int cropX = Math.Min(Math.Max((int)Math.Round(focalPointX * s + focalPointWidth * s / 2 - outputWidth / 2, 0), 0), scaledSource.Width - outputWidth);

            if (outputWidth > scaledSource.Width)
            {
                cropX = (scaledSource.Width - outputWidth) / 2;
            }

            int cropY = Math.Min(Math.Max((int)Math.Round(focalPointY * s + focalPointHeight * s / 2 - outputHeight / 2, 0), 0), scaledSource.Height - outputHeight);

            if (outputHeight > scaledSource.Height)
            {
                cropY = (scaledSource.Height - outputHeight) / 2;
            }

            if (!preserveFocalArea)
            {
                switch (focalPointAnchor)
                {
                    case 0:
                        break;
                    case 1:
                        cropY = focalPointY;
                        break;
                    case 2:
                        cropX = Math.Max((focalPointX + focalPointWidth) - outputWidth, 0);
                        break;
                    case 3:
                        cropY = Math.Max((focalPointY + focalPointHeight) - outputHeight, 0);
                        break;
                    case 4:
                        cropX = focalPointX;
                        break;
                }
            }

            Rectangle crop = new Rectangle(cropX, cropY, outputWidth, outputHeight);

            //var bmp = new Bitmap(crop.Width, crop.Height);

            //using (var gr = Graphics.FromImage(bmp))
            //{
            //    gr.DrawImage(scaledSource, new Rectangle(0, 0, bmp.Width, bmp.Height), crop, GraphicsUnit.Pixel);
            //}

            //return bmp;

            // build conversion config
            JObject conversionConfig = new JObject();
            conversionConfig["width"] = null;
            conversionConfig["height"] = null;

            if (preserveFocalArea && shouldScale)
            {
                conversionConfig["width"] = outputWidth;
                conversionConfig["height"] = outputHeight;
            }

            conversionConfig["cropping_configuration"] = new JObject();
            conversionConfig["cropping_configuration"]["cropping_type"] = "Custom";
            conversionConfig["cropping_configuration"]["top_left"] = new JObject();
            conversionConfig["cropping_configuration"]["top_left"]["x"] = cropX;
            conversionConfig["cropping_configuration"]["top_left"]["y"] = cropY;
            conversionConfig["cropping_configuration"]["width"] = outputWidth;
            conversionConfig["cropping_configuration"]["height"] = outputHeight;

            conversionConfig["original_width"] = inputWidth;
            conversionConfig["original_height"] = inputHeight;

            conversionConfig["ratio"] = new JObject();
            conversionConfig["ratio"]["name"] = "free";

            //Console.WriteLine(JsonConvert.SerializeObject(conversionConfig, Formatting.Indented));

            return conversionConfig;
        }
    }
}
