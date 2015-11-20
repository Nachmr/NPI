//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.SkeletonBasics
{
    using System.IO;
    using System.Windows;
    using System.Threading;
    using System.Windows.Media;
    using Microsoft.Kinect;
    using System.IO.Packaging;
    using System.Windows.Media.Imaging;
    using System;
    using System.Timers;

    public struct Coordenadas
    {
        public float x;
        public float y;
        public float z;
    }

    public enum Estado_mano_derecha
    {
        Inicial,
        Fin,
        Indefinido
    }

    public enum Situacion
    {
        Midiendo,
        Tocando
    }

    public enum Nota{
        Aire,
        Do,
        Re,
        Mi,
        Fa,
        Sol,
        La,
        Si
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Situacion actual;
        //Variable para guardar en que estado se está
        Estado_mano_derecha mano_derecha = Estado_mano_derecha.Indefinido;

        float tam_mastil = 0;
        float tam_traste = 0;

        System.Media.SoundPlayer player;

        /// <summary>
        /// Width of output drawing
        /// </summary>
        private const float RenderWidth = 640.0f;

        /// <summary>
        /// Height of our output drawing
        /// </summary>
        private const float RenderHeight = 480.0f;

        /// <summary>
        /// Thickness of drawn joint lines
        /// </summary>
        private const double JointThickness = 3;

        /// <summary>
        /// Thickness of body center ellipse
        /// </summary>
        private const double BodyCenterThickness = 10;

        /// <summary>
        /// Thickness of clip edge rectangles
        /// </summary>
        private const double ClipBoundsThickness = 10;

        /// <summary>
        /// Brush used to draw skeleton center point
        /// </summary>
        private readonly Brush centerPointBrush = Brushes.Blue;

        /// <summary>
        /// Brush used for drawing joints that are currently tracked
        /// </summary>
        private readonly Brush trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68));

        /// <summary>
        /// Brush used for drawing joints that are currently inferred
        /// </summary>        
        private readonly Brush inferredJointBrush = Brushes.Yellow;

        /// <summary>
        /// Pen used for drawing bones that are currently tracked
        /// </summary>
        private readonly Pen trackedBonePen = new Pen(Brushes.Green, 6);

        /// <summary>
        /// Pen used for drawing bones that are currently inferred
        /// </summary>        
        private readonly Pen inferredBonePen = new Pen(Brushes.Gray, 1);

        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        private KinectSensor sensor;

        /// <summary>
        /// Drawing group for skeleton rendering output
        /// </summary>
        private DrawingGroup drawingGroup;

        /// <summary>
        /// Drawing image that we will display
        /// </summary>
        private DrawingImage imageSource;

        /// <summary>
        /// Bitmap that will hold color information
        /// </summary>
        private WriteableBitmap colorBitmap;

        /// <summary>
        /// Intermediate storage for the color data received from the camera
        /// </summary>
        private byte[] colorPixels;

        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Draws indicators to show which edges are clipping skeleton data
        /// </summary>
        /// <param name="skeleton">skeleton to draw clipping information for</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private static void RenderClippedEdges(Skeleton skeleton, DrawingContext drawingContext)
        {
            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Bottom))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, RenderHeight - ClipBoundsThickness, RenderWidth, ClipBoundsThickness));
            }

            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Top))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, RenderWidth, ClipBoundsThickness));
            }

            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Left))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, ClipBoundsThickness, RenderHeight));
            }

            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Right))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(RenderWidth - ClipBoundsThickness, 0, ClipBoundsThickness, RenderHeight));
            }
        }

        /// <summary>
        /// Execute startup tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            player = new System.Media.SoundPlayer();
            actual = Situacion.Midiendo;

            // Create the drawing group we'll use for drawing
            this.drawingGroup = new DrawingGroup();

            // Create an image source that we can use in our image control
            this.imageSource = new DrawingImage(this.drawingGroup);

            // Look through all sensors and start the first connected one.
            // This requires that a Kinect is connected at the time of app startup.
            // To make your app robust against plug/unplug, 
            // it is recommended to use KinectSensorChooser provided in Microsoft.Kinect.Toolkit (See components in Toolkit Browser).
            foreach (var potentialSensor in KinectSensor.KinectSensors)
            {
                if (potentialSensor.Status == KinectStatus.Connected)
                {
                    this.sensor = potentialSensor;
                    break;
                }
            }

            if (null != this.sensor)
            {
                // Turn on the color stream to receive color frames
                this.sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);

                // Allocate space to put the pixels we'll receive
                this.colorPixels = new byte[this.sensor.ColorStream.FramePixelDataLength];

                // This is the bitmap we'll display on-screen
                this.colorBitmap = new WriteableBitmap(this.sensor.ColorStream.FrameWidth, this.sensor.ColorStream.FrameHeight, 96.0, 96.0, PixelFormats.Bgr32, null);

                // Set the image we display to point to the bitmap where we'll put the image data
                this.Image.Source = this.colorBitmap;

                // Add an event handler to be called whenever there is new color frame data
                this.sensor.ColorFrameReady += this.SensorColorFrameReady;

                // Turn on the skeleton stream to receive skeleton frames
                this.sensor.SkeletonStream.Enable();

                // Add an event handler to be called whenever there is new color frame data
                this.sensor.SkeletonFrameReady += this.SensorSkeletonFrameReady;

                this.Skeleto.Source = this.imageSource;

                // Start the sensor!
                try
                {
                    this.sensor.Start();
                }
                catch (IOException)
                {
                    this.sensor = null;
                }
            }

            if (null == this.sensor)
            {
                this.statusBarText.Text = Properties.Resources.NoKinectReady;
            }
        }

        /// <summary>
        /// Execute shutdown tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (null != this.sensor)
            {
                this.sensor.Stop();
            }
        }

        /// <summary>
        /// Event handler for Kinect sensor's SkeletonFrameReady event
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void SensorSkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            Skeleton[] skeletons = new Skeleton[0];

            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame != null)
                {
                    skeletons = new Skeleton[skeletonFrame.SkeletonArrayLength];
                    skeletonFrame.CopySkeletonDataTo(skeletons);
                }
            }

            //Llamamos a la función que controla los estados por los que hay que pasar
            detectar_estado(skeletons);
        }

        /// <summary>
        /// Maps a SkeletonPoint to lie within our render space and converts to Point
        /// </summary>
        /// <param name="skelpoint">point to map</param>
        /// <returns>mapped point</returns>
        private Point SkeletonPointToScreen(SkeletonPoint skelpoint)
        {
            // Convert point to depth space.  
            // We are not using depth directly, but we do want the points in our 640x480 output resolution.
            DepthImagePoint depthPoint = this.sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(skelpoint, DepthImageFormat.Resolution640x480Fps30);
            return new Point(depthPoint.X, depthPoint.Y);
        }

        /// <summary>
        /// Handles the checking or unchecking of the seated mode combo box
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void CheckBoxSeatedModeChanged(object sender, RoutedEventArgs e)
        {
            if (null != this.sensor)
            {
                if (this.checkBoxSeatedMode.IsChecked.GetValueOrDefault())
                {
                    this.sensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Seated;
                }
                else
                {
                    this.sensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Default;
                }
            }
        }

        //Función para controlar las acciones a realizar según el estado en que nos encontramos.
        public void detectar_estado(Skeleton[] skeletons)
        {
            if (actual == Situacion.Midiendo)
            {
                solucionP2.Content = "Midiendo";
                foreach (Skeleton bones in skeletons)
                {
                   
                    if(bones.Joints[JointType.WristLeft].Position.Y < bones.Joints[JointType.ElbowLeft].Position.Y + 0.01
                        && bones.Joints[JointType.WristLeft].Position.Y > bones.Joints[JointType.ElbowLeft].Position.Y - 0.01
                        && bones.Joints[JointType.ElbowLeft].Position.Y < bones.Joints[JointType.ShoulderLeft].Position.Y + 0.01
                        && bones.Joints[JointType.ElbowLeft].Position.Y > bones.Joints[JointType.ShoulderLeft].Position.Y - 0.01
                        && bones.Joints[JointType.ElbowLeft].Position.Y != 0)
                    {
                        tam_mastil = bones.Joints[JointType.WristLeft].Position.X - bones.Joints[JointType.ShoulderLeft].Position.X;
                        tam_traste = tam_mastil / 8;
                        actual = Situacion.Tocando;
                    }
                }
            }
            if (actual == Situacion.Tocando)
            {
                solucionP2.Content = "Tocando!!";

                foreach (Skeleton bones in skeletons)
                {                   
                    if (mano_derecha == Estado_mano_derecha.Indefinido)
                    {
                        //solucionP3.Content = "Indefinido";
                        if (bones.Joints[JointType.WristRight].Position.X < bones.Joints[JointType.ShoulderRight].Position.X
                            && bones.Joints[JointType.WristRight].Position.X > bones.Joints[JointType.ShoulderLeft].Position.X
                            && bones.Joints[JointType.WristRight].Position.Y > bones.Joints[JointType.HipCenter].Position.Y)
                        {   //Mano derecha más alta que la cintura y entre los dos hombros
                            mano_derecha = Estado_mano_derecha.Inicial;
                            solucionP3.Content = "Inicial";
                        }
                    }
                    if (mano_derecha == Estado_mano_derecha.Inicial)
                    {
                        //solucionP3.Content = "Inicial";
                        if (bones.Joints[JointType.WristRight].Position.X < bones.Joints[JointType.ShoulderLeft].Position.X
                            || bones.Joints[JointType.WristRight].Position.X > bones.Joints[JointType.ShoulderRight].Position.X)
                        {   //Mano derecha sale de la zona de estado inicial por cualquier lado menos por abajo (Por arriba no se puede salir)
                            mano_derecha = Estado_mano_derecha.Indefinido;
                            solucionP3.Content = "Indefinido";
                        }
                        else if (bones.Joints[JointType.WristRight].Position.Y < bones.Joints[JointType.HipCenter].Position.Y)
                        {   //Mano derecha sale de la zona de estado inicial por abajo
                            Sonar(bones);
                            solucionP3.Content = "Sonar";
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Event handler for Kinect sensor's ColorFrameReady event
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void SensorColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            using (ColorImageFrame colorFrame = e.OpenColorImageFrame())
            {
                if (colorFrame != null)
                {
                    // Copy the pixel data from the image to a temporary array
                    colorFrame.CopyPixelDataTo(this.colorPixels);

                    // Write the pixel data into our bitmap
                    this.colorBitmap.WritePixels(
                        new Int32Rect(0, 0, this.colorBitmap.PixelWidth, this.colorBitmap.PixelHeight),
                        this.colorPixels,
                        this.colorBitmap.PixelWidth * sizeof(int),
                        0);
                }
            }
        }

        private void Sonar(Skeleton bones)
        {
            Nota nota = pos_mano_izquierda(bones);
            toca_nota(nota);
            mano_derecha = Estado_mano_derecha.Indefinido;
            
        }

        private Nota pos_mano_izquierda(Skeleton bones){
            Nota nota = Nota.Aire;

            if (bones.Joints[JointType.WristLeft].Position.Y < bones.Joints[JointType.HipCenter].Position.Y
                || bones.Joints[JointType.WristLeft].Position.Y > bones.Joints[JointType.Head].Position.Y
                || bones.Joints[JointType.WristLeft].Position.X > bones.Joints[JointType.ShoulderLeft].Position.X)
            {
                nota = Nota.Aire;
            }
            else
            {
                if (bones.Joints[JointType.WristLeft].Position.X < bones.Joints[JointType.ShoulderLeft].Position.X-tam_traste
                    && bones.Joints[JointType.WristLeft].Position.X > bones.Joints[JointType.ShoulderLeft].Position.X + tam_traste
                    )
                {
                    nota = Nota.Si;
                }

                if (bones.Joints[JointType.WristLeft].Position.X < bones.Joints[JointType.ShoulderLeft].Position.X + tam_traste
                    && bones.Joints[JointType.WristLeft].Position.X > bones.Joints[JointType.ShoulderLeft].Position.X + tam_traste * 2
                    )
                {
                    nota = Nota.La;
                }

                if (bones.Joints[JointType.WristLeft].Position.X < bones.Joints[JointType.ShoulderLeft].Position.X + tam_traste * 2
                    && bones.Joints[JointType.WristLeft].Position.X > bones.Joints[JointType.ShoulderLeft].Position.X + tam_traste * 3
                    )
                {
                    nota = Nota.Sol;
                }

                if (bones.Joints[JointType.WristLeft].Position.X < bones.Joints[JointType.ShoulderLeft].Position.X + tam_traste * 3
                    && bones.Joints[JointType.WristLeft].Position.X > bones.Joints[JointType.ShoulderLeft].Position.X + tam_traste * 4
                    )
                {
                    nota = Nota.Fa;
                }

                if (bones.Joints[JointType.WristLeft].Position.X < bones.Joints[JointType.ShoulderLeft].Position.X + tam_traste * 4
                    && bones.Joints[JointType.WristLeft].Position.X > bones.Joints[JointType.ShoulderLeft].Position.X + tam_traste * 5
                    )
                {
                    nota = Nota.Mi;
                }

                if (bones.Joints[JointType.WristLeft].Position.X < bones.Joints[JointType.ShoulderLeft].Position.X + tam_traste * 5
                    && bones.Joints[JointType.WristLeft].Position.X > bones.Joints[JointType.ShoulderLeft].Position.X + tam_traste * 6
                    )
                {
                    nota = Nota.Re;
                }

                if (bones.Joints[JointType.WristLeft].Position.X < bones.Joints[JointType.ShoulderLeft].Position.X + tam_traste * 6
                    && bones.Joints[JointType.WristLeft].Position.X > bones.Joints[JointType.ShoulderLeft].Position.X + tam_traste * 7
                    )
                {
                    nota = Nota.Do;
                }

            }

            return nota;
        }

        private void toca_nota(Nota nota)
        {
            switch (nota)
            {
                case Nota.Aire:
                    player.SoundLocation = Path.GetFullPath("..\\..\\Sounds/Aire.wav");
                    solucionP.Content = "Nota al aire";
                break;

                case Nota.Do:
                player.SoundLocation = Path.GetFullPath("..\\..\\Sounds/C.wav");
            
                    solucionP.Content = "Nota Do";
                break;

                case Nota.Re:
                solucionP.Content = "Nota Re";
                player.SoundLocation = Path.GetFullPath("..\\..\\Sounds/D.wav");
                break;

                case Nota.Mi:
                player.SoundLocation = Path.GetFullPath("..\\..\\Sounds/E.wav");
                solucionP.Content = "Nota Mi";
                break;

                case Nota.Fa:
                player.SoundLocation = Path.GetFullPath("..\\..\\Sounds/F.wav");
                solucionP.Content = "Nota Fa";
                break;

                case Nota.Sol:
                player.SoundLocation = Path.GetFullPath("..\\..\\Sounds/G.wav");
                solucionP.Content = "Nota Sol";
                break;

                case Nota.La:
                player.SoundLocation = Path.GetFullPath("..\\..\\Sounds/A.wav");
                solucionP.Content = "Nota La";
                break;

                case Nota.Si:
                player.SoundLocation = Path.GetFullPath("..\\..\\Sounds/B.wav");
                solucionP.Content = "Nota Si";
                break;

                default:
                solucionP.Content = "Fallo";
                break;
            }
            player.Play();
        }
    }
}