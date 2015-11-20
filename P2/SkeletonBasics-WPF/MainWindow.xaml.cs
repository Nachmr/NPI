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
        Si,
        Aire_up,
        Do_up,
        Re_up,
        Mi_up,
        Fa_up,
        Sol_up,
        La_up,
        Si_up,
        Dom,
        Rem,
        Mim,
        Fam,
        Solm,
        Lam,
        Sim,
        Dom_up,
        Rem_up,
        Mim_up,
        Fam_up,
        Solm_up,
        Lam_up,
        Sim_up
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
                        if (bones.Joints[JointType.WristRight].Position.X < bones.Joints[JointType.ShoulderRight].Position.X
                           && bones.Joints[JointType.WristRight].Position.X > bones.Joints[JointType.ShoulderLeft].Position.X
                           && bones.Joints[JointType.WristRight].Position.Y < bones.Joints[JointType.HipCenter].Position.Y)
                        {   //Mano derecha más alta que la cintura y entre los dos hombros
                            mano_derecha = Estado_mano_derecha.Fin;
                            solucionP3.Content = "Fin";
                        }
                    }
                    if (mano_derecha == Estado_mano_derecha.Inicial || mano_derecha == Estado_mano_derecha.Fin)
                    {
                        //solucionP3.Content = "Inicial";
                        if (bones.Joints[JointType.WristRight].Position.X < bones.Joints[JointType.ShoulderLeft].Position.X
                            || bones.Joints[JointType.WristRight].Position.X > bones.Joints[JointType.ShoulderRight].Position.X)
                        {   //Mano derecha sale de la zona de estado inicial por cualquier lado menos por abajo (Por arriba no se puede salir)
                            mano_derecha = Estado_mano_derecha.Indefinido;
                            solucionP3.Content = "Indefinido";
                        }
                        else
                        {
                            if (mano_derecha == Estado_mano_derecha.Inicial)
                            {
                                if (bones.Joints[JointType.WristRight].Position.Y < bones.Joints[JointType.HipCenter].Position.Y)
                                {   //Mano derecha sale de la zona de estado inicial por abajo
                                    Sonar(bones, true);
                                    mano_derecha = Estado_mano_derecha.Fin;
                                    solucionP3.Content = "Fin";
                                }
                            }

                            if (mano_derecha == Estado_mano_derecha.Fin)
                            {
                                if (bones.Joints[JointType.WristRight].Position.Y > bones.Joints[JointType.HipCenter].Position.Y)
                                {   //Mano derecha sale de la zona de estado inicial por abajo
                                    Sonar(bones, false);
                                    mano_derecha = Estado_mano_derecha.Inicial;
                                    solucionP3.Content = "Inicial";
                                }
                            }
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

        private void Sonar(Skeleton bones, bool direccion)
        {
            Nota nota = pos_mano_izquierda(bones, direccion);
            toca_nota(nota);
            
            
        }

        private Nota pos_mano_izquierda(Skeleton bones, bool direccion){
            Nota nota = Nota.Aire;

            if (direccion) //Si la mano derecha va de arriba a abajo
            {
                //Si la mano izquierda fuera de la region
                if (bones.Joints[JointType.WristLeft].Position.Y < bones.Joints[JointType.HipCenter].Position.Y
                    || bones.Joints[JointType.WristLeft].Position.Y > bones.Joints[JointType.Head].Position.Y
                    || bones.Joints[JointType.WristLeft].Position.X > bones.Joints[JointType.ShoulderLeft].Position.X + bones.Joints[JointType.ShoulderLeft].Position.X- tam_traste)
                {
                    nota = Nota.Aire;
                }
                else //Si la mano izquierda está dentro de la región
                {
                    //Si la mano izquierda está en el rango de los acordes mayores, vemos en qué nota (Acorde mayor, de arriba a abajo)
                    if (bones.Joints[JointType.WristLeft].Position.Y > ((bones.Joints[JointType.ShoulderCenter].Position.Y + bones.Joints[JointType.HipCenter].Position.Y) / 2))
                    {
                        if (bones.Joints[JointType.WristLeft].Position.X < bones.Joints[JointType.ShoulderLeft].Position.X - tam_traste
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
                    else    //Si la mano izquierda está en el rango de los acordes menores, vemos en qué nota (Acorde menor, de arriba a abajo)
                    {
                        if (bones.Joints[JointType.WristLeft].Position.X < bones.Joints[JointType.ShoulderLeft].Position.X - tam_traste
                            && bones.Joints[JointType.WristLeft].Position.X > bones.Joints[JointType.ShoulderLeft].Position.X + tam_traste)
                        {
                            nota = Nota.Sim;
                        }

                        if (bones.Joints[JointType.WristLeft].Position.X < bones.Joints[JointType.ShoulderLeft].Position.X + tam_traste
                            && bones.Joints[JointType.WristLeft].Position.X > bones.Joints[JointType.ShoulderLeft].Position.X + tam_traste * 2
                            )
                        {
                            nota = Nota.Lam;
                        }

                        if (bones.Joints[JointType.WristLeft].Position.X < bones.Joints[JointType.ShoulderLeft].Position.X + tam_traste * 2
                            && bones.Joints[JointType.WristLeft].Position.X > bones.Joints[JointType.ShoulderLeft].Position.X + tam_traste * 3
                            )
                        {
                            nota = Nota.Solm;
                        }

                        if (bones.Joints[JointType.WristLeft].Position.X < bones.Joints[JointType.ShoulderLeft].Position.X + tam_traste * 3
                            && bones.Joints[JointType.WristLeft].Position.X > bones.Joints[JointType.ShoulderLeft].Position.X + tam_traste * 4
                            )
                        {
                            nota = Nota.Fam;
                        }

                        if (bones.Joints[JointType.WristLeft].Position.X < bones.Joints[JointType.ShoulderLeft].Position.X + tam_traste * 4
                            && bones.Joints[JointType.WristLeft].Position.X > bones.Joints[JointType.ShoulderLeft].Position.X + tam_traste * 5
                            )
                        {
                            nota = Nota.Mim;
                        }

                        if (bones.Joints[JointType.WristLeft].Position.X < bones.Joints[JointType.ShoulderLeft].Position.X + tam_traste * 5
                            && bones.Joints[JointType.WristLeft].Position.X > bones.Joints[JointType.ShoulderLeft].Position.X + tam_traste * 6
                            )
                        {
                            nota = Nota.Rem;
                        }

                        if (bones.Joints[JointType.WristLeft].Position.X < bones.Joints[JointType.ShoulderLeft].Position.X + tam_traste * 6
                            && bones.Joints[JointType.WristLeft].Position.X > bones.Joints[JointType.ShoulderLeft].Position.X + tam_traste * 7
                            )
                        {
                            nota = Nota.Dom;
                        }
                    }
                }
            }
            else     //Si la mano derecha va de abajo a arriba
            {
                //Si la mano izquierda fuera de la region
                if (bones.Joints[JointType.WristLeft].Position.Y < bones.Joints[JointType.HipCenter].Position.Y
                    || bones.Joints[JointType.WristLeft].Position.Y > bones.Joints[JointType.Head].Position.Y
                    || bones.Joints[JointType.WristLeft].Position.X > bones.Joints[JointType.ShoulderLeft].Position.X-tam_traste)
                {
                    nota = Nota.Aire_up;
                }
                else     //Si la mano izquierda dentro de la region
                {
                    //Si la mano izquierda está en el rango de los acordes mayores vemos en qué nota (Acorde mayor, de abajo a arriba)
                    if (bones.Joints[JointType.WristLeft].Position.Y > ((bones.Joints[JointType.ShoulderCenter].Position.Y + bones.Joints[JointType.HipCenter].Position.Y) / 2))
                    {
                        if (bones.Joints[JointType.WristLeft].Position.X < bones.Joints[JointType.ShoulderLeft].Position.X - tam_traste
                            && bones.Joints[JointType.WristLeft].Position.X > bones.Joints[JointType.ShoulderLeft].Position.X + tam_traste
                            )
                        {
                            nota = Nota.Si_up;
                        }

                        if (bones.Joints[JointType.WristLeft].Position.X < bones.Joints[JointType.ShoulderLeft].Position.X + tam_traste
                            && bones.Joints[JointType.WristLeft].Position.X > bones.Joints[JointType.ShoulderLeft].Position.X + tam_traste * 2
                            )
                        {
                            nota = Nota.La_up;
                        }

                        if (bones.Joints[JointType.WristLeft].Position.X < bones.Joints[JointType.ShoulderLeft].Position.X + tam_traste * 2
                            && bones.Joints[JointType.WristLeft].Position.X > bones.Joints[JointType.ShoulderLeft].Position.X + tam_traste * 3
                            )
                        {
                            nota = Nota.Sol_up;
                        }

                        if (bones.Joints[JointType.WristLeft].Position.X < bones.Joints[JointType.ShoulderLeft].Position.X + tam_traste * 3
                            && bones.Joints[JointType.WristLeft].Position.X > bones.Joints[JointType.ShoulderLeft].Position.X + tam_traste * 4
                            )
                        {
                            nota = Nota.Fa_up;
                        }

                        if (bones.Joints[JointType.WristLeft].Position.X < bones.Joints[JointType.ShoulderLeft].Position.X + tam_traste * 4
                            && bones.Joints[JointType.WristLeft].Position.X > bones.Joints[JointType.ShoulderLeft].Position.X + tam_traste * 5
                            )
                        {
                            nota = Nota.Mi_up;
                        }

                        if (bones.Joints[JointType.WristLeft].Position.X < bones.Joints[JointType.ShoulderLeft].Position.X + tam_traste * 5
                            && bones.Joints[JointType.WristLeft].Position.X > bones.Joints[JointType.ShoulderLeft].Position.X + tam_traste * 6
                            )
                        {
                            nota = Nota.Re_up;
                        }

                        if (bones.Joints[JointType.WristLeft].Position.X < bones.Joints[JointType.ShoulderLeft].Position.X + tam_traste * 6
                            && bones.Joints[JointType.WristLeft].Position.X > bones.Joints[JointType.ShoulderLeft].Position.X + tam_traste * 7
                            )
                        {
                            nota = Nota.Do_up;
                        }
                    }
                    else     //Si la mano izquierda está en el rango de los acordes menores vemos en qué nota (Acorde menor, de abajo a arriba)
                    {

                        if (bones.Joints[JointType.WristLeft].Position.X < bones.Joints[JointType.ShoulderLeft].Position.X - tam_traste
                            && bones.Joints[JointType.WristLeft].Position.X > bones.Joints[JointType.ShoulderLeft].Position.X + tam_traste
                            )
                        {
                            nota = Nota.Sim_up;
                        }

                        if (bones.Joints[JointType.WristLeft].Position.X < bones.Joints[JointType.ShoulderLeft].Position.X + tam_traste
                            && bones.Joints[JointType.WristLeft].Position.X > bones.Joints[JointType.ShoulderLeft].Position.X + tam_traste * 2
                            )
                        {
                            nota = Nota.Lam_up;
                        }

                        if (bones.Joints[JointType.WristLeft].Position.X < bones.Joints[JointType.ShoulderLeft].Position.X + tam_traste * 2
                            && bones.Joints[JointType.WristLeft].Position.X > bones.Joints[JointType.ShoulderLeft].Position.X + tam_traste * 3
                            )
                        {
                            nota = Nota.Solm_up;
                        }

                        if (bones.Joints[JointType.WristLeft].Position.X < bones.Joints[JointType.ShoulderLeft].Position.X + tam_traste * 3
                            && bones.Joints[JointType.WristLeft].Position.X > bones.Joints[JointType.ShoulderLeft].Position.X + tam_traste * 4
                            )
                        {
                            nota = Nota.Fam_up;
                        }

                        if (bones.Joints[JointType.WristLeft].Position.X < bones.Joints[JointType.ShoulderLeft].Position.X + tam_traste * 4
                            && bones.Joints[JointType.WristLeft].Position.X > bones.Joints[JointType.ShoulderLeft].Position.X + tam_traste * 5
                            )
                        {
                            nota = Nota.Mim_up;
                        }

                        if (bones.Joints[JointType.WristLeft].Position.X < bones.Joints[JointType.ShoulderLeft].Position.X + tam_traste * 5
                            && bones.Joints[JointType.WristLeft].Position.X > bones.Joints[JointType.ShoulderLeft].Position.X + tam_traste * 6
                            )
                        {
                            nota = Nota.Rem_up;
                        }

                        if (bones.Joints[JointType.WristLeft].Position.X < bones.Joints[JointType.ShoulderLeft].Position.X + tam_traste * 6
                            && bones.Joints[JointType.WristLeft].Position.X > bones.Joints[JointType.ShoulderLeft].Position.X + tam_traste * 7
                            )
                        {
                            nota = Nota.Dom_up;
                        }
                    }
                }
            }

            return nota;
        }

        private void toca_nota(Nota nota)
        {
            switch (nota)
            {
                case Nota.Aire:
                   // player.SoundLocation = Path.GetFullPath("..\\..\\Sounds/Aire.wav");
                    solucionP.Content = "Nota al aire";
                break;

                case Nota.Do:
                player.SoundLocation = Path.GetFullPath("..\\..\\Sounds/C.wav");
            
                    solucionP.Content = "Nota Do";
                    player.Play();
                    break;

                case Nota.Re:
                solucionP.Content = "Nota Re";
                player.SoundLocation = Path.GetFullPath("..\\..\\Sounds/D.wav");
                player.Play();
                break;

                case Nota.Mi:
                player.SoundLocation = Path.GetFullPath("..\\..\\Sounds/E.wav");
                solucionP.Content = "Nota Mi";
                player.Play();
                break;

                case Nota.Fa:
                //player.SoundLocation = Path.GetFullPath("..\\..\\Sounds/F.wav");
                solucionP.Content = "Nota Fa";
                break;

                case Nota.Sol:
                //player.SoundLocation = Path.GetFullPath("..\\..\\Sounds/G.wav");
                solucionP.Content = "Nota Sol";
                break;

                case Nota.La:
                //player.SoundLocation = Path.GetFullPath("..\\..\\Sounds/A.wav");
                solucionP.Content = "Nota La";
                break;

                case Nota.Si:
                //player.SoundLocation = Path.GetFullPath("..\\..\\Sounds/B.wav");
                solucionP.Content = "Nota Si";
                break;

                case Nota.Aire_up:
                //player.SoundLocation = Path.GetFullPath("..\\..\\Sounds/Aire_up.wav");
                solucionP.Content = "Nota al aire_up";
                break;

                case Nota.Do_up:
                //player.SoundLocation = Path.GetFullPath("..\\..\\Sounds/C_up.wav");

                solucionP.Content = "Nota Do_up";
                break;

                case Nota.Re_up:
                solucionP.Content = "Nota Re_up";
                //player.SoundLocation = Path.GetFullPath("..\\..\\Sounds/D_up.wav");
                break;

                case Nota.Mi_up:
                //player.SoundLocation = Path.GetFullPath("..\\..\\Sounds/E_up.wav");
                solucionP.Content = "Nota Mi_up";
                break;

                case Nota.Fa_up:
                //player.SoundLocation = Path.GetFullPath("..\\..\\Sounds/F_up.wav");
                solucionP.Content = "Nota Fa_up";
                break;

                case Nota.Sol_up:
                //player.SoundLocation = Path.GetFullPath("..\\..\\Sounds/G_up.wav");
                solucionP.Content = "Nota Sol_up";
                break;

                case Nota.La_up:
                //player.SoundLocation = Path.GetFullPath("..\\..\\Sounds/A_up.wav");
                solucionP.Content = "Nota La_up";
                break;

                case Nota.Si_up:
                //player.SoundLocation = Path.GetFullPath("..\\..\\Sounds/B_up.wav");
                solucionP.Content = "Nota Si_up";
                break;

                case Nota.Dom:
                //player.SoundLocation = Path.GetFullPath("..\\..\\Sounds/Cm.wav");

                solucionP.Content = "Nota Dom";
                break;

                case Nota.Rem:
                solucionP.Content = "Nota Rem";
                //player.SoundLocation = Path.GetFullPath("..\\..\\Sounds/Dm.wav");
                break;

                case Nota.Mim:
                //player.SoundLocation = Path.GetFullPath("..\\..\\Sounds/Em.wav");
                solucionP.Content = "Nota Mim";
                break;

                case Nota.Fam:
                //player.SoundLocation = Path.GetFullPath("..\\..\\Sounds/Fm.wav");
                solucionP.Content = "Nota Fam";
                break;

                case Nota.Solm:
                //player.SoundLocation = Path.GetFullPath("..\\..\\Sounds/Gm.wav");
                solucionP.Content = "Nota Solm";
                break;

                case Nota.Lam:
                //player.SoundLocation = Path.GetFullPath("..\\..\\Sounds/Am.wav");
                solucionP.Content = "Nota Lam";
                break;

                case Nota.Sim:
                //player.SoundLocation = Path.GetFullPath("..\\..\\Sounds/Bm.wav");
                solucionP.Content = "Nota Sim";
                break;

                case Nota.Dom_up:
                //player.SoundLocation = Path.GetFullPath("..\\..\\Sounds/Cm_up.wav");

                solucionP.Content = "Nota Dom_up";
                break;

                case Nota.Rem_up:
                solucionP.Content = "Nota Rem_up";
                //player.SoundLocation = Path.GetFullPath("..\\..\\Sounds/Dm_up.wav");
                break;

                case Nota.Mim_up:
                //player.SoundLocation = Path.GetFullPath("..\\..\\Sounds/Em_up.wav");
                solucionP.Content = "Nota Mim_up";
                break;

                case Nota.Fam_up:
                //player.SoundLocation = Path.GetFullPath("..\\..\\Sounds/Fm_up.wav");
                solucionP.Content = "Nota Fam_up";
                break;

                case Nota.Solm_up:
                //player.SoundLocation = Path.GetFullPath("..\\..\\Sounds/Gm_up.wav");
                solucionP.Content = "Nota Solm_up";
                break;

                case Nota.Lam_up:
                //player.SoundLocation = Path.GetFullPath("..\\..\\Sounds/Am_up.wav");
                solucionP.Content = "Nota Lam_up";
                break;

                case Nota.Sim_up:
                //player.SoundLocation = Path.GetFullPath("..\\..\\Sounds/Bm_up.wav");
                solucionP.Content = "Nota Sim_up";
                break;

                default:
                solucionP.Content = "Fallo";
                break;
            }
            //player.Play();
        }
    }
}