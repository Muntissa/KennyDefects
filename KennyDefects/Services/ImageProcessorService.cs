// KennyDefects.Services/ImageProcessorService.cs

using OpenCvSharp;
using System.Xml.Linq;

namespace KennyDefects.Services
{
    public class ImageProcessorService
    {
        private Mat _original;
        private Mat _gray;
        private Mat _blur;
        private Mat _dilate;
        private Mat _roi;
        private Mat _canny;
        private string? _imgPath;
        public static ImageProcessorService Instance { get; private set; }

        public ImageProcessorService()
        {
            Instance = this;
        }

        public byte[] ToGray(string path)
        {
            _imgPath = path;
            _original = Cv2.ImRead(path, ImreadModes.Color);
            _gray = new Mat();
            Cv2.CvtColor(_original, _gray, ColorConversionCodes.BGR2GRAY);
            return _gray.ToBytes(".png");
        }

        public byte[] GaussianBlur(int kernel = 5, int sigma = 0)
        {
            // Ядро должно быть нечётным и >= 1
            if (kernel % 2 == 0) kernel += 1;
            if (kernel < 1) kernel = 1;

            _blur = new Mat();
            Cv2.GaussianBlur(_gray, _blur, new Size(kernel, kernel), sigma);
            return _blur.ToBytes(".png");
        }

        public byte[] Dilate(int kernel = 3)
        {
            // Ядро должно быть нечётным и >= 1
            if (kernel % 2 == 0) kernel += 1;
            if (kernel < 1) kernel = 1;

            _dilate = new Mat();
            var element = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(kernel, kernel));
            Cv2.Dilate(_blur, _dilate, element);
            return _dilate.ToBytes(".png");
        }

        public byte[] ApplyRoi(RoiRect roi)
        {
            var rect = new Rect(roi.x, roi.y, roi.width, roi.height);
            _roi = new Mat(_dilate, rect);
            return _roi.ToBytes(".png");
        }

        public byte[] Canny(int threshold1 = 100, int threshold2 = 200)
        {
            _canny = new Mat();
            Cv2.Canny(_dilate, _canny, threshold1, threshold2);
            return _canny.ToBytes(".png");
        }

        public byte[] DrawDefectBoxes()
        {
            if (_original == null) throw new Exception("No original image loaded!");

            var result = _original.Clone();

            var boxes = GetAllRoiFromXml(_imgPath);
            foreach (var roi in boxes)
            {
                var pt1 = new Point(roi.x, roi.y);
                var pt2 = new Point(roi.x + roi.width, roi.y + roi.height);
                Cv2.Rectangle(result, pt1, pt2, Scalar.Red, 2);
            }

            return result.ToBytes(".png");
        }

        public List<RoiRect> GetAllRoiFromXml(string imgPath)
        {
            var fileName = Path.GetFileNameWithoutExtension(imgPath);
            var xmlPath = Path.Combine("C:/Images/NEU-DET/ANNOTATIONS", fileName + ".xml");
            var rois = new List<RoiRect>();
            if (!File.Exists(xmlPath)) return rois;
            var xdoc = XDocument.Load(xmlPath);
            foreach (var obj in xdoc.Descendants("bndbox"))
            {
                rois.Add(new RoiRect
                {
                    x = int.Parse(obj.Element("xmin")!.Value),
                    y = int.Parse(obj.Element("ymin")!.Value),
                    width = int.Parse(obj.Element("xmax")!.Value) - int.Parse(obj.Element("xmin")!.Value),
                    height = int.Parse(obj.Element("ymax")!.Value) - int.Parse(obj.Element("ymin")!.Value)
                });
            }
            return rois;
        }
    }

    public class RoiRect
    {
        public int x { get; set; }
        public int y { get; set; }
        public int width { get; set; }
        public int height { get; set; }
    }
}
