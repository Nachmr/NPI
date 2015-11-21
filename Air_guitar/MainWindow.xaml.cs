//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.CoordinateMappingBasics
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using Microsoft.Kinect;

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

    public enum Nota
    {
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
        /// Format we will use for the depth stream
        /// </summary>
        private const DepthImageFormat DepthFormat = DepthImageFormat.Resolution320x240Fps30;

        /// <summary>
        /// Format we will use for the color stream
        /// </summary>
        private const ColorImageFormat ColorFormat = ColorImageFormat.RgbResolution640x480Fps30;

        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        private KinectSensor sensor;

        /// <summary>
        /// Bitmap that will hold color information
        /// </summary>
        private WriteableBitmap colorBitmap;

        /// <summary>
        /// Bitmap that will hold opacity mask information
        /// </summary>
        private WriteableBitmap playerOpacityMaskImage = null;

        /// <summary>
        /// Intermediate storage for the depth data received from the sensor
        /// </summary>
        private DepthImagePixel[] depthPixels;

        /// <summary>
        /// Intermediate storage for the color data received from the camera
        /// </summary>
        private byte[] colorPixels;

        /// <summary>
        /// Intermediate storage for the player opacity mask
        /// </summary>
        private int[] playerPixelData;

        /// <summary>
        /// Intermediate storage for the depth to color mapping
        /// </summary>
        private ColorImagePoint[] colorCoordinates;

        /// <summary>
        /// Inverse scaling factor between color and depth
        /// </summary>
        private int colorToDepthDivisor;

        /// <summary>
        /// Width of the depth image
        /// </summary>
        private int depthWidth;

        /// <summary>
        /// Height of the depth image
        /// </summary>
        private int depthHeight;

        /// <summary>
        /// Indicates opaque in an opacity mask
        /// </summary>
        private int opaquePixelValue = -1;

        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Execute startup tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            player = new System.Media.SoundPlayer();
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
                // Turn on the depth stream to receive depth frames
                this.sensor.DepthStream.Enable(DepthFormat);

                this.depthWidth = this.sensor.DepthStream.FrameWidth;

                this.depthHeight = this.sensor.DepthStream.FrameHeight;

                this.sensor.ColorStream.Enable(ColorFormat);

                int colorWidth = this.sensor.ColorStream.FrameWidth;
                int colorHeight = this.sensor.ColorStream.FrameHeight;

                this.colorToDepthDivisor = colorWidth / this.depthWidth;

                // Turn on to get player masks
                this.sensor.SkeletonStream.Enable();

                // Allocate space to put the depth pixels we'll receive
                this.depthPixels = new DepthImagePixel[this.sensor.DepthStream.FramePixelDataLength];

                // Allocate space to put the color pixels we'll create
                this.colorPixels = new byte[this.sensor.ColorStream.FramePixelDataLength];

                this.playerPixelData = new int[this.sensor.DepthStream.FramePixelDataLength];

                this.colorCoordinates = new ColorImagePoint[this.sensor.DepthStream.FramePixelDataLength];

                // This is the bitmap we'll display on-screen
                this.colorBitmap = new WriteableBitmap(colorWidth, colorHeight, 96.0, 96.0, PixelFormats.Bgr32, null);

                // Set the image we display to point to the bitmap where we'll put the image data
                this.MaskedColor.Source = this.colorBitmap;

                // Add an event handler to be called whenever there is new depth frame data
                this.sensor.AllFramesReady += this.SensorAllFramesReady;

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
                this.sensor = null;
            }
        }

        /// <summary>
        /// Event handler for Kinect sensor's DepthFrameReady event
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void SensorAllFramesReady(object sender, AllFramesReadyEventArgs e)
        {
            // in the middle of shutting down, so nothing to do
            if (null == this.sensor)
            {
                return;
            }

            bool depthReceived = false;
            bool colorReceived = false;

            using (DepthImageFrame depthFrame = e.OpenDepthImageFrame())
            {
                if (null != depthFrame)
                {
                    // Copy the pixel data from the image to a temporary array
                    depthFrame.CopyDepthImagePixelDataTo(this.depthPixels);

                    depthReceived = true;
                }
            }

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

            using (ColorImageFrame colorFrame = e.OpenColorImageFrame())
            {
                if (null != colorFrame)
                {
                    // Copy the pixel data from the image to a temporary array
                    colorFrame.CopyPixelDataTo(this.colorPixels);

                    colorReceived = true;
                }
            }

            // do our processing outside of the using block
            // so that we return resources to the kinect as soon as possible
            if (true == depthReceived)
            {
                this.sensor.CoordinateMapper.MapDepthFrameToColorFrame(
                    DepthFormat,
                    this.depthPixels,
                    ColorFormat,
                    this.colorCoordinates);

                Array.Clear(this.playerPixelData, 0, this.playerPixelData.Length);

                // loop over each row and column of the depth
                for (int y = 0; y < this.depthHeight; ++y)
                {
                    for (int x = 0; x < this.depthWidth; ++x)
                    {
                        // calculate index into depth array
                        int depthIndex = x + (y * this.depthWidth);

                        DepthImagePixel depthPixel = this.depthPixels[depthIndex];

                        int player = depthPixel.PlayerIndex;

                        // if we're tracking a player for the current pixel, sets it opacity to full
                        if (player > 0)
                        {
                            // retrieve the depth to color mapping for the current depth pixel
                            ColorImagePoint colorImagePoint = this.colorCoordinates[depthIndex];

                            // scale color coordinates to depth resolution
                            int colorInDepthX = colorImagePoint.X / this.colorToDepthDivisor;
                            int colorInDepthY = colorImagePoint.Y / this.colorToDepthDivisor;

                            // make sure the depth pixel maps to a valid point in color space
                            // check y > 0 and y < depthHeight to make sure we don't write outside of the array
                            // check x > 0 instead of >= 0 since to fill gaps we set opaque current pixel plus the one to the left
                            // because of how the sensor works it is more correct to do it this way than to set to the right
                            if (colorInDepthX > 0 && colorInDepthX < this.depthWidth && colorInDepthY >= 0 && colorInDepthY < this.depthHeight)
                            {
                                // calculate index into the player mask pixel array
                                int playerPixelIndex = colorInDepthX + (colorInDepthY * this.depthWidth);

                                // set opaque
                                this.playerPixelData[playerPixelIndex] = opaquePixelValue;

                                // compensate for depth/color not corresponding exactly by setting the pixel 
                                // to the left to opaque as well
                                this.playerPixelData[playerPixelIndex - 1] = opaquePixelValue;
                            }
                        }
                    }
                }
            }

            // do our processing outside of the using block
            // so that we return resources to the kinect as soon as possible
            if (true == colorReceived)
            {
                // Write the pixel data into our bitmap
                this.colorBitmap.WritePixels(
                    new Int32Rect(0, 0, this.colorBitmap.PixelWidth, this.colorBitmap.PixelHeight),
                    this.colorPixels,
                    this.colorBitmap.PixelWidth * sizeof(int),
                    0);

                if (this.playerOpacityMaskImage == null)
                {
                    this.playerOpacityMaskImage = new WriteableBitmap(
                        this.depthWidth,
                        this.depthHeight,
                        96,
                        96,
                        PixelFormats.Bgra32,
                        null);

                    MaskedColor.OpacityMask = new ImageBrush { ImageSource = this.playerOpacityMaskImage };
                }

                this.playerOpacityMaskImage.WritePixels(
                    new Int32Rect(0, 0, this.depthWidth, this.depthHeight),
                    this.playerPixelData,
                    this.depthWidth * ((this.playerOpacityMaskImage.Format.BitsPerPixel + 7) / 8),
                    0);
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

                    if (bones.Joints[JointType.WristLeft].Position.Y < bones.Joints[JointType.ElbowLeft].Position.Y + 0.01
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

        private void Sonar(Skeleton bones, bool direccion)
        {
            Nota nota = pos_mano_izquierda(bones, direccion);
            toca_nota(nota);
        }

        private Nota pos_mano_izquierda(Skeleton bones, bool direccion)
        {
            Nota nota = Nota.Aire;

            if (direccion) //Si la mano derecha va de arriba a abajo
            {
                //Si la mano izquierda fuera de la region
                if (bones.Joints[JointType.WristLeft].Position.Y < bones.Joints[JointType.HipCenter].Position.Y
                    || bones.Joints[JointType.WristLeft].Position.Y > bones.Joints[JointType.Head].Position.Y
                    || bones.Joints[JointType.WristLeft].Position.X > bones.Joints[JointType.ShoulderLeft].Position.X + bones.Joints[JointType.ShoulderLeft].Position.X - tam_traste)
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
                    || bones.Joints[JointType.WristLeft].Position.X > bones.Joints[JointType.ShoulderLeft].Position.X - tam_traste)
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

                case Nota.Aire_up:
                    player.SoundLocation = Path.GetFullPath("..\\..\\Sounds/Aire_up.wav");
                    solucionP.Content = "Nota al aire_up";
                    break;

                case Nota.Do_up:
                    player.SoundLocation = Path.GetFullPath("..\\..\\Sounds/C_up.wav");

                    solucionP.Content = "Nota Do_up";
                    break;

                case Nota.Re_up:
                    solucionP.Content = "Nota Re_up";
                    player.SoundLocation = Path.GetFullPath("..\\..\\Sounds/D_up.wav");
                    break;

                case Nota.Mi_up:
                    player.SoundLocation = Path.GetFullPath("..\\..\\Sounds/E_up.wav");
                    solucionP.Content = "Nota Mi_up";
                    break;

                case Nota.Fa_up:
                    player.SoundLocation = Path.GetFullPath("..\\..\\Sounds/F_up.wav");
                    solucionP.Content = "Nota Fa_up";
                    break;

                case Nota.Sol_up:
                    player.SoundLocation = Path.GetFullPath("..\\..\\Sounds/G_up.wav");
                    solucionP.Content = "Nota Sol_up";
                    break;

                case Nota.La_up:
                    player.SoundLocation = Path.GetFullPath("..\\..\\Sounds/A_up.wav");
                    solucionP.Content = "Nota La_up";
                    break;

                case Nota.Si_up:
                    player.SoundLocation = Path.GetFullPath("..\\..\\Sounds/B_up.wav");
                    solucionP.Content = "Nota Si_up";
                    break;

                case Nota.Dom:
                    player.SoundLocation = Path.GetFullPath("..\\..\\Sounds/Cm.wav");

                    solucionP.Content = "Nota Dom";
                    break;

                case Nota.Rem:
                    solucionP.Content = "Nota Rem";
                    player.SoundLocation = Path.GetFullPath("..\\..\\Sounds/Dm.wav");
                    break;

                case Nota.Mim:
                    player.SoundLocation = Path.GetFullPath("..\\..\\Sounds/Em.wav");
                    solucionP.Content = "Nota Mim";
                    break;

                case Nota.Fam:
                    player.SoundLocation = Path.GetFullPath("..\\..\\Sounds/Fm.wav");
                    solucionP.Content = "Nota Fam";
                    break;

                case Nota.Solm:
                    player.SoundLocation = Path.GetFullPath("..\\..\\Sounds/Gm.wav");
                    solucionP.Content = "Nota Solm";
                    break;

                case Nota.Lam:
                    player.SoundLocation = Path.GetFullPath("..\\..\\Sounds/Am.wav");
                    solucionP.Content = "Nota Lam";
                    break;

                case Nota.Sim:
                    player.SoundLocation = Path.GetFullPath("..\\..\\Sounds/Bm.wav");
                    solucionP.Content = "Nota Sim";
                    break;

                case Nota.Dom_up:
                    player.SoundLocation = Path.GetFullPath("..\\..\\Sounds/Cm_up.wav");

                    solucionP.Content = "Nota Dom_up";
                    break;

                case Nota.Rem_up:
                    solucionP.Content = "Nota Rem_up";
                    player.SoundLocation = Path.GetFullPath("..\\..\\Sounds/Dm_up.wav");
                    break;

                case Nota.Mim_up:
                    player.SoundLocation = Path.GetFullPath("..\\..\\Sounds/Em_up.wav");
                    solucionP.Content = "Nota Mim_up";
                    break;

                case Nota.Fam_up:
                    player.SoundLocation = Path.GetFullPath("..\\..\\Sounds/Fm_up.wav");
                    solucionP.Content = "Nota Fam_up";
                    break;

                case Nota.Solm_up:
                    player.SoundLocation = Path.GetFullPath("..\\..\\Sounds/Gm_up.wav");
                    solucionP.Content = "Nota Solm_up";
                    break;

                case Nota.Lam_up:
                    player.SoundLocation = Path.GetFullPath("..\\..\\Sounds/Am_up.wav");
                    solucionP.Content = "Nota Lam_up";
                    break;

                case Nota.Sim_up:
                    player.SoundLocation = Path.GetFullPath("..\\..\\Sounds/Bm_up.wav");
                    solucionP.Content = "Nota Sim_up";
                    break;

                default:
                    solucionP.Content = "Fallo";
                    break;
            }
            player.Play();
        }
    }
}