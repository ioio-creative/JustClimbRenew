using JustClimbTrial.Helpers;
using JustClimbTrial.Kinect;
using Microsoft.Kinect;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace JustClimbTrial.Kinect
{
    public class KinectWall
    {
        private string wallID;

        public string WallID
        {
            get { return wallID; }
            set { wallID = value; }
        }


        private Canvas wCanvas;
        private CoordinateMapper wallMapper;
        private DepthSpacePoint[] dCoordinatesInColorFrame { get; set; }
        private int width = 0;
        private int height = 0;

        private ushort[] wallDepthData;
        private byte[] wallBitmap;

        private bool IsSnapshotTaken = false;


        public KinectWall(Canvas canvas, CoordinateMapper coMapper)
        {
            wCanvas = canvas;
            wallMapper = coMapper;
        }

        public CameraSpacePoint GetCamSpacePointFromMousePoint(Point mousePt, SpaceMode spMode)
        {
            if (!IsSnapshotTaken)
            {
                return default(CameraSpacePoint);
            }

            Tuple<float, float> dimensions = KinectExtensions.FrameDimensions[spMode];
            float x_temp = (float)(mousePt.X * dimensions.Item1 / wCanvas.ActualWidth);
            float y_temp = (float)(mousePt.Y * dimensions.Item2 / wCanvas.ActualHeight);

            DepthSpacePoint depPtFromMousePt = dCoordinatesInColorFrame[(int)(x_temp + 0.5f) + (int)(y_temp + 0.5f) * (int)dimensions.Item1];

            if (depPtFromMousePt.X == float.NegativeInfinity || depPtFromMousePt.Y == float.NegativeInfinity)
            {
                return default(CameraSpacePoint);
            }

            ushort depth = wallDepthData[(int)depPtFromMousePt.X + (int)(depPtFromMousePt.Y) * (int)KinectExtensions.FrameDimensions[SpaceMode.Depth].Item1];
            CameraSpacePoint camPtFromMousePt = wallMapper.MapDepthPointToCameraSpace(depPtFromMousePt, depth);

            return camPtFromMousePt;
        }

        //public bool AddBoulder(double mouseX, double mouseY, 
        //    double sliderRadius,
        //    double canvasWidth, double canvasHeight, 
        //    SpaceMode spMode, CoordinateMapper coMapper, Canvas canvas)
        //{
        //    bool isAddBoulderSuccess = false;

        //    if (!IsSnapshotTaken)
        //    {
        //        return isAddBoulderSuccess;
        //    }

        //    DepthSpacePoint _boulderDSP = new DepthSpacePoint { X = float.NegativeInfinity, Y = float.NegativeInfinity };
        //    CameraSpacePoint _boulderCAMSP = new CameraSpacePoint();

        //    Tuple<float, float> dimensions = KinectExtensions.FrameDimensions[spMode];
        //    float x_temp = (float)(mouseX * dimensions.Item1 / canvasWidth);
        //    float y_temp = (float)(mouseY * dimensions.Item2 / canvasHeight);

        //    _boulderDSP = dCoordinatesInColorFrame[(int)(x_temp + 0.5f) + (int)(y_temp + 0.5f) * (int)dimensions.Item1];

        //    ushort depth = 0;
        //    if (_boulderDSP.X != float.NegativeInfinity && _boulderDSP.Y != float.NegativeInfinity)
        //    {
        //        depth = wallDepthData[(int)_boulderDSP.X + (int)(_boulderDSP.Y) * (int)KinectExtensions.FrameDimensions[SpaceMode.Depth].Item1];
        //        _boulderCAMSP = coMapper.MapDepthPointToCameraSpace(_boulderDSP, depth);
        //    }
        //    Console.WriteLine($"Position: Color[{(int)(x_temp + 0.5f)}][{(int)(y_temp + 0.5f)}] ==> Depth[{_boulderDSP.X}][{_boulderDSP.Y}]");
        //    Console.WriteLine($"New Boulder: x = {_boulderCAMSP.X}; y = {_boulderCAMSP.Y}; z = {_boulderCAMSP.Z}");

        //    if (boulderList == null)
        //    {
        //        boulderList = new List<Boulder>();
        //    }            

        //    boulderList.Add(new Boulder(_boulderCAMSP, new Point(mouseX, mouseY), sliderRadius, sliderRadius, canvas));

        //    isAddBoulderSuccess = true;
        //    return isAddBoulderSuccess;
        //}

        public bool SnapShotWallData(DepthSpacePoint[] colorSpaceMap, ushort[] dFrameData, byte[] colFrameData)
        {
            // Deep copy
            dCoordinatesInColorFrame = colorSpaceMap.Clone() as DepthSpacePoint[];
            wallDepthData = dFrameData.Clone() as ushort[];
            wallBitmap = colFrameData.Clone() as byte[];
            IsSnapshotTaken = true;

            return IsSnapshotTaken;
        }

        public void SaveWallData()
        {
            if (IsSnapshotTaken)
            {
                // Set a variable to the My Documents path.
                string mydocpath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string wallLogPath = Path.Combine(FileHelper.WallLogFolderPath(), wallID);
                if (!Directory.Exists(wallLogPath))
                {
                    DirectoryInfo di = Directory.CreateDirectory(wallLogPath);
                    Console.WriteLine("Log directory created successfully at {0}.", Directory.GetCreationTime(wallLogPath));
                }

                width = (int)KinectExtensions.FrameDimensions[SpaceMode.Color].Item1;
                height = (int)KinectExtensions.FrameDimensions[SpaceMode.Color].Item2;
                ExportDCoordinatesFile(wallLogPath);
                ExportWallPNG(wallLogPath, wallBitmap, width, height, PixelFormats.Bgr32);
            }
        }

        private bool ExportDCoordinatesFile(string folderPath)
        {
            bool exportMapperSuccess = false;
            string filePath = Path.Combine(folderPath, "Coordinate Map.txt");

            // Write the text to a new file named "WriteFile.txt".
            File.WriteAllLines(filePath, new string[] { $"KinectWall Coordinates [{width}][{height}]" });
            // Append text to an existing file named "WriteLines.txt".
            try
            {
                using (StreamWriter outputFile = new StreamWriter(filePath, true))
                {
                    int colorWidth = (int)KinectExtensions.FrameDimensions[SpaceMode.Color].Item1;
                    int CPIndex = 0;
                    foreach (DepthSpacePoint dPoint in dCoordinatesInColorFrame)
                    {
                        // Append new lines of text to the file
                        outputFile.WriteLine($"Color[{CPIndex % colorWidth}][{CPIndex / colorWidth}] = Depth[{dPoint.X}][{dPoint.Y}]");
                        CPIndex++;
                    }
                    exportMapperSuccess = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR----Wall Coordinate Export Exception: " + ex.ToString() );
                throw;
            }
            return exportMapperSuccess;
        }

        private bool ExportWallPNG(string folderPath, byte[] bitmapData, int width, int height, PixelFormat format)
        {
            bool isImgSaved = false;

            // create a bitmapsource object using byte[]
            BitmapSource bitmapSrc = KinectExtensions.ToBitmapSrc(bitmapData, width, height, PixelFormats.Bgr32);

            // create a png bitmap encoder which knows how to save a .png file
            BitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmapSrc));

            string filePath = System.IO.Path.Combine(folderPath, wallID + ".png");
            // write the new file to disk
            try
            { 
                // FileStream is IDisposable
                using (FileStream fs = new FileStream(filePath, FileMode.Create))
                {
                    encoder.Save(fs);
                    isImgSaved = true;
                }               
            }
            catch (IOException ex)
            {
                Console.WriteLine("ERROR----Wall Image Export Exception: " + ex.ToString());
            }

            return isImgSaved;
        }

    }//class KinectWall
}//namespace
