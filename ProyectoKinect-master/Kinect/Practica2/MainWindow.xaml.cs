//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.SkeletonBasics
{
    using System.IO;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using Microsoft.Kinect;

    public struct puntosMovimiento
    {
        public float X;
        public float Y;
        public float Z;
    };

    public enum posturas
    {
        Mal,
        Postura_Inicial,
        Sigue_Bajando,
        Agachado,
        BrazosFin,
        Sigue_Avanzando
    };

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //Variables donde almaceno toda la información requerida para calcular las posiciones necesarias.
        puntosMovimiento cadera, rodillaIzquierda, rodillaDerecha, tobilloIzquierdo, tobilloDerecho, codoDerecho, codoIzquierdo, muniecaDerecha, muniecaIzquierda, hombroDerecho, hombroIzquierdo;
        puntosMovimiento rodillaInicialDerecha = new puntosMovimiento();
        puntosMovimiento rodillaInicialIzquierda = new puntosMovimiento();
        puntosMovimiento caderaInicial = new puntosMovimiento();
        puntosMovimiento tobillaInicialDerecho = new puntosMovimiento();
        puntosMovimiento tobillaInicialIzquierdo = new puntosMovimiento();
        puntosMovimiento rodillaIzquierdaActualizada = new puntosMovimiento();
        puntosMovimiento rodillaDerechaActualizada = new puntosMovimiento();
        puntosMovimiento caderaActualizada = new puntosMovimiento();
        puntosMovimiento codoInicialDerecho = new puntosMovimiento();
        puntosMovimiento codoInicialIzquierdo = new puntosMovimiento(); 
        puntosMovimiento muniecaInicialDerecha = new puntosMovimiento(); 
        puntosMovimiento muniecaInicialIzquierda = new puntosMovimiento();
        puntosMovimiento hombroInicialDerecho = new puntosMovimiento(); 
        puntosMovimiento hombroInicialIzquierdo = new puntosMovimiento();
        //Variable para saber si ha finalizado el movimiento.
        bool finalizado= false;
        bool correcto = false;
        bool baja = false;
        //bool error = false;
        //bool brazosFinalizado = false;
        bool correctoBrazo = false;
        bool atrasBrazo = false;

        /*bool correcto = false;
        bool baja = false;
        bool error = false;*/
        const int numeroPostura = 10;
        int cont = 0;
        bool posicionInicialCorrecta = false;
        bool brazosRectos = false;
        float valorError;

        //Obtención si la postura es correcta o incorrecta.
        posturas posturaInicial = posturas.Mal;
        posturas posturaBajando = posturas.Sigue_Bajando;
        posturas posturaBien = posturas.Agachado;
        posturas posturaDetectada = posturas.Postura_Inicial;
        posturas posturaVuelta = posturas.Mal;
        posturas posturaTerminada = posturas.BrazosFin;
        posturas posturaAvanzando = posturas.Sigue_Avanzando;
        
        //Variables para la camara de color y profundidad.
        //Información obtenida del ejemplo de kinect color Basic.

        private WriteableBitmap colorBitmap;
        private byte[] colorPixels;

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
        private readonly Brush inferredJointBrush = Brushes.Red;

        /// <summary>
        /// Pen used for drawing bones that are currently tracked
        /// </summary>
        private readonly Pen trackedBonePen = new Pen(Brushes.BlueViolet, 6);


        //Pinta en color Chocolate si esta en posicion Inicial.
        private readonly Pen pintaHuesosInicial = new Pen(Brushes.Azure, 8);
        //pinta en color Verde si esta en la posicion final.
        private readonly Pen pintaHuesosFinal = new Pen(Brushes.Green , 8);
        //Pinta en color turquesa si esta llegando a la posicion final.
        private readonly Pen pintaHuesosLlegando = new Pen(Brushes.Turquoise, 8);
        //Pinta en rojo si esta haciendo un movimiento incorrecto.
        private readonly Pen pintaHuesosMal = new Pen(Brushes.Red, 8);


        /// <summary>
        /// Pen used for drawing bones that are currently inferred
        /// </summary>        
        private readonly Pen inferredBonePen = new Pen(Brushes.Gray, 20);

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
            // Create the drawing group we'll use for drawing
            this.drawingGroup = new DrawingGroup();

            // Create an image source that we can use in our image control
            this.imageSource = new DrawingImage(this.drawingGroup);

            // Display the drawing using our image control
            Image.Source = this.imageSource;

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
                //Inicio sensor camara de color y profundidad.
                this.sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
                this.colorPixels = new byte[this.sensor.ColorStream.FramePixelDataLength];
                this.colorBitmap = new WriteableBitmap(this.sensor.ColorStream.FrameWidth, this.sensor.ColorStream.FrameHeight, 96.0, 96.0, PixelFormats.Bgr32, null);
                this.ColorImage.Source = this.colorBitmap;
                this.sensor.ColorFrameReady += this.SensorColorFrameReady;

                // Turn on the skeleton stream to receive skeleton frames
                this.sensor.SkeletonStream.Enable();

                // Add an event handler to be called whenever there is new color frame data
                this.sensor.SkeletonFrameReady += this.SensorSkeletonFrameReady;

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
                    this.colorBitmap.WritePixels(new Int32Rect(0, 0, this.colorBitmap.PixelWidth, this.colorBitmap.PixelHeight),this.colorPixels,this.colorBitmap.PixelWidth * sizeof(int),0);
                }
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

            using (DrawingContext dc = this.drawingGroup.Open())
            {
                // Draw a transparent background to set the render size
                dc.DrawRectangle(Brushes.Transparent, null, new Rect(0.0, 0.0, RenderWidth, RenderHeight));

                if (skeletons.Length != 0)
                {
                    foreach (Skeleton skel in skeletons)
                    {
                        RenderClippedEdges(skel, dc);

                        if (skel.TrackingState == SkeletonTrackingState.Tracked)
                        {
                            this.DrawBonesAndJoints(skel, dc);
                        }
                        else if (skel.TrackingState == SkeletonTrackingState.PositionOnly)
                        {
                            dc.DrawEllipse(
                            this.centerPointBrush,
                            null,
                            this.SkeletonPointToScreen(skel.Position),
                            BodyCenterThickness,
                            BodyCenterThickness);
                        }
                    }
                }

                // prevent drawing outside of our render area
                this.drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, RenderWidth, RenderHeight));
            }
            int aux = (int)(valorError * 100);
            string texto = " " + aux + "%";
            this.MuestraError.Text = texto;
            obtenerPuntos(skeletons);
            
        }
        //Compruebo si es un la postura correcta.
        private void posturaCorrecta(puntosMovimiento cadera, puntosMovimiento rodillaIzquierda, puntosMovimiento rodillaDerecha, puntosMovimiento tobilloDerecho, puntosMovimiento tobilloIzquierdo, puntosMovimiento codoDerecho, puntosMovimiento codoIzquierdo, puntosMovimiento muniecaDerecha, puntosMovimiento muniecaIzquierda, puntosMovimiento hombroDerecho, puntosMovimiento hombroIzquierdo)
        {
            //Compruebo la postura inicial es con las piernas rectas.
            //Si entro en este primer If, lo que ontengo es que el usuario esta en la posición correcta y almacenado de referencia esa postura.
            if (detectoRecto(cadera, rodillaIzquierda, rodillaDerecha, tobilloDerecho, tobilloIzquierdo, codoDerecho, codoIzquierdo, muniecaDerecha, muniecaIzquierda, hombroDerecho, hombroIzquierdo))
            {
                if (deteccionDePostura(posturas.Postura_Inicial))
                {
                    finalizado = false;
                    solucionP.Content = posturaInicial.ToString();
                    //Guardo las cadera en la posicion correcta.         
                    caderaInicial.X = cadera.X;
                    caderaActualizada.Y = cadera.Y;
                    caderaInicial.Y = cadera.Y;
                    caderaInicial.Z = cadera.Z;
                    //Guardo la rodilla derecha en la posicion correcta
                    rodillaInicialDerecha.X = rodillaDerecha.X;
                    rodillaInicialDerecha.Y = rodillaDerecha.Y;
                    rodillaInicialDerecha.Z = rodillaDerecha.Z;
                    //Guardo la rodilla izquierda en la posición correcta
                    rodillaInicialIzquierda.X = rodillaIzquierda.X;
                    rodillaInicialIzquierda.Y = rodillaIzquierda.Y;
                    rodillaInicialIzquierda.Z = rodillaIzquierda.Z;
                    //Guardo el tobillo derecho en la posición correcta.
                    tobillaInicialDerecho.X = tobilloDerecho.X;
                    tobillaInicialDerecho.Y = tobilloDerecho.Y;
                    tobillaInicialDerecho.Z = tobilloDerecho.Z;
                    //Guardo el tobillo izquierdo en la posición correcta.
                    tobillaInicialIzquierdo.X = tobilloIzquierdo.X;
                    tobillaInicialIzquierdo.Y = tobilloIzquierdo.Y;
                    tobillaInicialIzquierdo.Z = tobilloIzquierdo.Z;
                    atrasBrazo = false;
                    correctoBrazo = false;
                    baja = false;
                    correcto = false;
                    //error = false;
                    //brazosFinalizado = false;
                    brazosRectos = true;
                    posicionInicialCorrecta = true;
                }
            }//Si ya esta almacenada la posicion correcta o esta mal entra aqui.
            else
            {
                if(posicionInicialCorrecta)//Si ha partido de la posición correcta.
                {
                    if (detectoPosicionFinal(cadera, caderaActualizada, rodillaInicialDerecha) /*&& detectoBrazosFinal(codoDerecho, codoIzquierdo, muniecaDerecha, muniecaIzquierda, hombroDerecho, hombroIzquierdo)*/)//Compruebo que el usuario ha llegado a la posición final.
                    {
                        if (detectoBrazosFinal(codoDerecho, codoIzquierdo, muniecaDerecha, muniecaIzquierda, hombroDerecho, hombroIzquierdo))
                        {
                            finalizado = true;//Guardo que ha finalizado.
                            solucionP.Content = posturaBien.ToString();
                            correcto = true;
                            baja = false;
                            correctoBrazo = true;
                            atrasBrazo = false;
                            //brazosFinalizado = false;
                            //error = false;

                        }
                        //movimientosBrazos(codoDerecho, codoIzquierdo, muniecaDerecha, muniecaIzquierda, hombroDerecho, hombroIzquierdo);
                    }
                    else if (detectoBajando(cadera, caderaActualizada) && !finalizado /*&& detectoBrazosAvanzando(codoDerecho, codoIzquierdo, muniecaDerecha, muniecaIzquierda, hombroDerecho, hombroIzquierdo)*/)//Compruebo que no se esta bajando y no ha llegado a la posición final.
                    {
                        if (detectoBrazosAvanzando(codoDerecho, codoIzquierdo, muniecaDerecha, muniecaIzquierda, hombroDerecho, hombroIzquierdo))
                        {
                            //caderaActualizada.Y = cadera.Y;
                            solucionP.Content = posturaBajando.ToString();
                            baja = true;
                            correcto = false;
                            atrasBrazo = true;
                            correctoBrazo = false;
                        }
                    }
                    else if (detectaSubida(cadera, caderaActualizada) || detectoBrazosAtras(codoDerecho, codoIzquierdo, muniecaDerecha, muniecaIzquierda, hombroDerecho, hombroIzquierdo))//Compruebo que el usuario no suba de la posicion inicial que hemos guardado anteriormente.
                    {
                        
                        
                            solucionP.Content = "Vuelva a Empezar";//Mensaje por pantalla para que el usuario no suba.
                            posicionInicialCorrecta = false;
                            brazosRectos = false;
                            baja = false;
                            atrasBrazo = false;
                            correctoBrazo = false;
                            correcto = false;
                        

                    }
                }
                else if (deteccionDePostura(posturas.Mal))
                {
                    solucionP.Content = "Coloquese en la posición de inicio";
                    baja = false;
                    correcto = false;
                    brazosRectos = false;
                    correctoBrazo = false;
                    atrasBrazo = false;
                }
            }
        }
        //Método que comprueba si se parte de la posición donde el usuario este completamente recto, es decir con una abertura de unos 10 cm entre los dos pies.
        bool detectoRecto(puntosMovimiento cadera, puntosMovimiento rodillaIzquierda, puntosMovimiento rodillaDerecha, puntosMovimiento tobilloIzquierdo, puntosMovimiento tobilloDerecho, puntosMovimiento codoDerecho, puntosMovimiento codoIzquierdo, puntosMovimiento muniecaDerecha, puntosMovimiento muniecaIzquierda, puntosMovimiento hombroDerecho, puntosMovimiento hombroIzquierdo)
        {
            if ((rodillaIzquierda.Z - tobilloIzquierdo.Z) - (cadera.Z - rodillaIzquierda.Z) > 0.01f && (rodillaDerecha.Z - tobilloDerecho.Z) - (cadera.Z - rodillaDerecha.Z) > 0.01f)
            {
                if (detectoBrazosRectos(codoDerecho, codoIzquierdo, muniecaDerecha, muniecaIzquierda, hombroDerecho, hombroIzquierdo))
                {
                    return true;
                }

            }
            return false;
        }
        //Método que detecta la posición final que se le ha establecido
        public bool detectoPosicionFinal(puntosMovimiento cadera, puntosMovimiento rodillaInicialDerecha, puntosMovimiento caderaActualizada)
        {
            if(caderaInicial.Y > cadera.Y+0.15)
            {
                caderaActualizada.Y = cadera.Y;
                return true;
            }
            return false;
        }
        //Método que detecta si hay subida desde la posición de agachado
        public bool detectaSubida(puntosMovimiento cadera, puntosMovimiento caderaActualizada)
        {
            
            //if (cadera.Y > caderaActualizada.Y+0.1)
            //if (caderaActualizada.Y > cadera.Y - 0.01 )
            //if(cadera.Y-0.01 > caderaActualizada.Y)
            if(caderaActualizada.Y < cadera.Y -0.01f)
            {
                //caderaActualizada.Y = cadera.Y;
                return true;
            }
            return false;

        }
        //Método comprobación de si el usuario esta bajando y actualizado la posición actual para que el usuario no suba hasta terminar el ejercicio.
        public bool detectoBajando(puntosMovimiento cadera, puntosMovimiento caderaActualizada)
        {
            //solucionP.Content = "Inicio rodilla " + rodillaInicialDerecha.Z + "\nRodilla Derecha " + rodillaDerecha.Z;
            if (caderaActualizada.Y > cadera.Y + 0.01)
            {
                caderaActualizada.Y = cadera.Y-0.01f;
                return true;
            }
            return false;
        }
        
        //Método de detección de la postura actual.
        public bool deteccionDePostura(posturas posturaActual)
        {
            if (posturaDetectada != posturaActual)
            {
                cont = 0;
                posturaDetectada = posturaActual;
                return false;
            }
            if (cont < numeroPostura)
            {
                cont++;
                return false;
            }
            if (posturaActual != posturaInicial)
            {
                cont = 0;
                posturaInicial = posturaActual;
                return true;
            }
            else
                cont = 0;
            return false;
        }
        public bool detectoBrazosAtras(puntosMovimiento codoDerecho, puntosMovimiento codoIzquierdo, puntosMovimiento muniecaDerecha, puntosMovimiento muniecaIzquierda, puntosMovimiento hombroDerecho, puntosMovimiento hombroIzquierdo)
        {
            if (codoDerecho.Y > hombroDerecho.Y + 0.06 || codoDerecho.Y < hombroDerecho.Y - 0.06 ||
               muniecaDerecha.Y > hombroDerecho.Y + 0.08 || muniecaDerecha.Y < hombroDerecho.Y - 0.08 ||
               codoIzquierdo.Y > hombroIzquierdo.Y + 0.06 || codoIzquierdo.Y < hombroIzquierdo.Y - 0.06 ||
               muniecaIzquierda.Y > hombroIzquierdo.Y + 0.08 || muniecaIzquierda.Y < hombroIzquierdo.Y - 0.08 ||
               rodillaDerecha.Y > rodillaInicialDerecha.Y + 0.06 || rodillaIzquierda.Y > rodillaInicialIzquierda.Y + 0.06)
            {
                return true;
            }
            return false;
        }
        //Metodo para comprobar que realiza el movimiento de los brazos establecido

        /*public void movimientosBrazos(puntosMovimiento codoDerecho, puntosMovimiento codoIzquierdo, puntosMovimiento muniecaDerecha, puntosMovimiento muniecaIzquierda, puntosMovimiento hombroDerecho, puntosMovimiento hombroIzquierdo)
        {
            if (detectoBrazosRectos(codoDerecho, codoIzquierdo, muniecaDerecha, muniecaIzquierda, hombroDerecho, hombroIzquierdo))
            {
                brazosRectos = true;
            }
            else
            {
                if (brazosRectos)
                {
                    if (detectoBrazosFinal(codoDerecho, codoIzquierdo, muniecaDerecha, muniecaIzquierda, hombroDerecho, hombroIzquierdo))
                    {
                        brazosFinalizado = true;//Guardo que ha finalizado.
                        solucionP.Content = posturaTerminada.ToString();
                        correctoBrazo = true;
                        atrasBrazo = false;
                    }
                    else if (detectoBrazosAvanzando(codoDerecho, codoIzquierdo, muniecaDerecha, muniecaIzquierda, hombroDerecho, hombroIzquierdo))
                    {
                        solucionP.Content = posturaAvanzando.ToString();
                        atrasBrazo = true;
                        correctoBrazo = false;
                    }
                    else if (detectoBrazosDetras() && !brazosFinalizado)
                    {
                        solucionP.Content = "Vuelva a Empezar";
                        brazosRectos = false;
                        atrasBrazo = false;
                    }
                }
                else if (deteccionDePostura(posturas.Mal))
                {
                    solucionP.Content = "Coloquese en la posición de inicio";
                    correctoBrazo = false;
                    atrasBrazo = false;
                    brazosRectos = false;
                }
            }

        }*/
        public bool detectoBrazosFinal(puntosMovimiento codoDerecho, puntosMovimiento codoIzquierdo, puntosMovimiento muniecaDerecha, puntosMovimiento muniecaIzquierda, puntosMovimiento hombroDerecho, puntosMovimiento hombroIzquierdo)
        {
            if (codoDerecho.Z < hombroDerecho.Z - 0.22 && muniecaDerecha.Z < hombroDerecho.Z - 0.22)
            {
                if (codoIzquierdo.Z < hombroIzquierdo.Z - 0.22 && muniecaIzquierda.Z < hombroIzquierdo.Z - 0.22)
                {
                    return true;
                }
            }
            return false;
        }
        public bool detectoBrazosAvanzando(puntosMovimiento codoDerecho, puntosMovimiento codoIzquierdo, puntosMovimiento muniecaDerecha, puntosMovimiento muniecaIzquierda, puntosMovimiento hombroDerecho, puntosMovimiento hombroIzquierdo)
        {
            if (codoDerecho.Z < hombroDerecho.Z - 0.04 && muniecaDerecha.Z < hombroDerecho.Z - 0.04)
            {
                if (codoIzquierdo.Z < hombroIzquierdo.Z - 0.04 && muniecaIzquierda.Z < hombroIzquierdo.Z - 0.04)
                {
                    return true;
                }
            }
            return false;
        }
        public bool detectoBrazosDetras()
        {
            if (codoDerecho.Z > hombroDerecho.Z + 0.04 && muniecaDerecha.Z > hombroDerecho.Z + 0.04)
            {
                if(codoIzquierdo.Z > hombroIzquierdo.Z +0.04 && muniecaIzquierda.Z > hombroIzquierdo.Z + 0.04)
                {
                       return true;
                }
            }
            return false;
        }

        public bool detectoBrazosRectos(puntosMovimiento codoDerecho, puntosMovimiento codoIzquierdo, puntosMovimiento muniecaDerecha, puntosMovimiento muniecaIzquierda, puntosMovimiento hombroDerecho, puntosMovimiento hombroIzquierdo)
        {
            if (codoDerecho.Y < hombroDerecho.Y + 0.03 && codoDerecho.Y > hombroDerecho.Y - 0.03 &&
                   muniecaDerecha.Y < hombroDerecho.Y + 0.05 && muniecaDerecha.Y > hombroDerecho.Y - 0.05
                   && codoDerecho.Z < hombroDerecho.Z + 0.03 && codoDerecho.Z > hombroDerecho.Z - 0.1 &&
                   muniecaDerecha.Z < hombroDerecho.Z + 0.03 && muniecaDerecha.Z > hombroDerecho.Z - 0.15)
            {
                if (codoIzquierdo.Y < hombroIzquierdo.Y + 0.03 && codoIzquierdo.Y > hombroIzquierdo.Y - 0.03 &&
                muniecaIzquierda.Y < hombroIzquierdo.Y + 0.05 && muniecaIzquierda.Y > hombroIzquierdo.Y - 0.05
                && codoIzquierdo.Z < hombroIzquierdo.Z + 0.03 && codoIzquierdo.Z > hombroIzquierdo.Z - 0.1 &&
                muniecaIzquierda.Z < hombroIzquierdo.Z + 0.03 && muniecaIzquierda.Z > hombroIzquierdo.Z - 0.15)
                {
                    return true;
                }
            }
            return false;
        }

        //Método obtener puntos para almacenarlos y poderlos utilizar para calcular la postura inicial, final, intermedia o mal.
        private void obtenerPuntos(Skeleton[] skeletons)
        {
            foreach (Skeleton bones in skeletons)
            {
                if (bones.TrackingState == SkeletonTrackingState.Tracked)
                {
                    //Obtengos puntos de la cadera. Punto central
                    cadera = new puntosMovimiento();
                    cadera.X = bones.Joints[JointType.HipCenter].Position.X;
                    cadera.Y = bones.Joints[JointType.HipCenter].Position.Y;
                    cadera.Z = bones.Joints[JointType.HipCenter].Position.Z;
                    
                    //Puntos de la rodilla izquierda.
                    rodillaIzquierda = new puntosMovimiento();
                    rodillaIzquierda.X = bones.Joints[JointType.KneeLeft].Position.X;
                    rodillaIzquierda.Y = bones.Joints[JointType.KneeLeft].Position.Y;
                    rodillaIzquierda.Z = bones.Joints[JointType.KneeLeft].Position.Z;

                    //Puntos de la rodilla derecha.
                    rodillaDerecha = new puntosMovimiento();
                    rodillaDerecha.X = bones.Joints[JointType.KneeRight].Position.X;
                    rodillaDerecha.Y = bones.Joints[JointType.KneeRight].Position.Y;
                    rodillaDerecha.Z = bones.Joints[JointType.KneeRight].Position.Z;

                    //Puntos del tobillo izquierdo.
                    tobilloIzquierdo = new puntosMovimiento();
                    tobilloIzquierdo.X = bones.Joints[JointType.AnkleLeft].Position.X;
                    tobilloIzquierdo.Y = bones.Joints[JointType.AnkleLeft].Position.Y;
                    tobilloIzquierdo.Z = bones.Joints[JointType.AnkleLeft].Position.Z;

                    //Puntos del tobillo derecho
                    tobilloDerecho = new puntosMovimiento();
                    tobilloDerecho.X = bones.Joints[JointType.AnkleRight].Position.X;
                    tobilloDerecho.Y = bones.Joints[JointType.AnkleRight].Position.Y;
                    tobilloDerecho.Z = bones.Joints[JointType.AnkleRight].Position.Z;
                    //Puntos del codo derecho
                    codoDerecho = new puntosMovimiento();
                    codoDerecho.X = bones.Joints[JointType.ElbowRight].Position.X;
                    codoDerecho.Y = bones.Joints[JointType.ElbowRight].Position.Y;
                    codoDerecho.Z = bones.Joints[JointType.ElbowRight].Position.Z;
                    //Puntos del codo izquierdo
                    codoIzquierdo = new puntosMovimiento();
                    codoIzquierdo.X = bones.Joints[JointType.ElbowLeft].Position.X;
                    codoIzquierdo.Y = bones.Joints[JointType.ElbowLeft].Position.Y;
                    codoIzquierdo.Z = bones.Joints[JointType.ElbowLeft].Position.Z;
                    //Puntos de la muñeca derecha
                    muniecaDerecha = new puntosMovimiento();
                    muniecaDerecha.X = bones.Joints[JointType.WristRight].Position.X;
                    muniecaDerecha.Y = bones.Joints[JointType.WristRight].Position.Y;
                    muniecaDerecha.Z = bones.Joints[JointType.WristRight].Position.Z;
                    //Puntos de la muñeca izquierdo
                    muniecaIzquierda = new puntosMovimiento();
                    muniecaIzquierda.X = bones.Joints[JointType.WristLeft].Position.X;
                    muniecaIzquierda.Y = bones.Joints[JointType.WristLeft].Position.Y;
                    muniecaIzquierda.Z = bones.Joints[JointType.WristLeft].Position.Z;
                    //Puntos del hombro derecho
                    hombroDerecho = new puntosMovimiento();
                    hombroDerecho.X = bones.Joints[JointType.ShoulderRight].Position.X;
                    hombroDerecho.Y = bones.Joints[JointType.ShoulderRight].Position.Y;
                    hombroDerecho.Z = bones.Joints[JointType.ShoulderRight].Position.Z;
                    //Puntos del hombro izquierdo
                    hombroIzquierdo = new puntosMovimiento();
                    hombroIzquierdo.X = bones.Joints[JointType.ShoulderLeft].Position.X;
                    hombroIzquierdo.Y = bones.Joints[JointType.ShoulderLeft].Position.Y;
                    hombroIzquierdo.Z = bones.Joints[JointType.ShoulderLeft].Position.Z;

                    
                }
            }
            posturaCorrecta(cadera, rodillaIzquierda, rodillaDerecha, tobilloIzquierdo, tobilloDerecho, codoDerecho, codoIzquierdo, muniecaDerecha, muniecaIzquierda, hombroDerecho, hombroIzquierdo);
        }

        /// <summary>
        /// Draws a skeleton's bones and joints
        /// </summary>
        /// <param name="skeleton">skeleton to draw</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private void DrawBonesAndJoints(Skeleton skeleton, DrawingContext drawingContext)
        {
            // Render Torso
            this.DrawBone(skeleton, drawingContext, JointType.Head, JointType.ShoulderCenter);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.ShoulderLeft);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.ShoulderRight);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.Spine);
            this.DrawBone(skeleton, drawingContext, JointType.Spine, JointType.HipCenter);
            this.DrawBone(skeleton, drawingContext, JointType.HipCenter, JointType.HipLeft);
            this.DrawBone(skeleton, drawingContext, JointType.HipCenter, JointType.HipRight);

            // Left Arm
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderLeft, JointType.ElbowLeft);
            this.DrawBone(skeleton, drawingContext, JointType.ElbowLeft, JointType.WristLeft);
            this.DrawBone(skeleton, drawingContext, JointType.WristLeft, JointType.HandLeft);

            // Right Arm
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderRight, JointType.ElbowRight);
            this.DrawBone(skeleton, drawingContext, JointType.ElbowRight, JointType.WristRight);
            this.DrawBone(skeleton, drawingContext, JointType.WristRight, JointType.HandRight);
            
            // Left Leg
            this.DrawBone(skeleton, drawingContext, JointType.HipLeft, JointType.KneeLeft);
            this.DrawBone(skeleton, drawingContext, JointType.KneeLeft, JointType.AnkleLeft);
            this.DrawBone(skeleton, drawingContext, JointType.AnkleLeft, JointType.FootLeft);

            // Right Leg
            this.DrawBone(skeleton, drawingContext, JointType.HipRight, JointType.KneeRight);
            this.DrawBone(skeleton, drawingContext, JointType.KneeRight, JointType.AnkleRight);
            this.DrawBone(skeleton, drawingContext, JointType.AnkleRight, JointType.FootRight);
            

            
            // Render Joints
            foreach (Joint joint in skeleton.Joints)
            {
                Brush drawBrush = null;

                if (joint.TrackingState == JointTrackingState.Tracked)
                {
                    drawBrush = this.trackedJointBrush;                    
                }
                else if (joint.TrackingState == JointTrackingState.Inferred)
                {
                    drawBrush = this.inferredJointBrush;                    
                }

                if (drawBrush != null)
                {
                    drawingContext.DrawEllipse(drawBrush, null, this.SkeletonPointToScreen(joint.Position), JointThickness, JointThickness);
                }
            }
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
        /// Draws a bone line between two joints
        /// </summary>
        /// <param name="skeleton">skeleton to draw bones from</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        /// <param name="jointType0">joint to start drawing from</param>
        /// <param name="jointType1">joint to end drawing at</param>
        private void DrawBone(Skeleton skeleton, DrawingContext drawingContext, JointType jointType0, JointType jointType1)
        {
            Joint joint0 = skeleton.Joints[jointType0];
            Joint joint1 = skeleton.Joints[jointType1];

            // If we can't find either of these joints, exit
            if (joint0.TrackingState == JointTrackingState.NotTracked ||
                joint1.TrackingState == JointTrackingState.NotTracked)
            {
                return;
            }

            // Don't draw if both points are inferred
            if (joint0.TrackingState == JointTrackingState.Inferred &&
                joint1.TrackingState == JointTrackingState.Inferred)
            {
                return;
            }

            // We assume all drawn bones are inferred unless BOTH joints are tracked
            Pen drawPen = this.inferredBonePen;
            if (joint0.TrackingState == JointTrackingState.Tracked && joint1.TrackingState == JointTrackingState.Tracked)
            {
                if (jointType0 == JointType.HipCenter && jointType1 == JointType.HipLeft)//Compruebo Punto cadera central con cadera izquierda
                {
                    drawPen = cambiarColorHuesos();//Cambio color con el que pinta
                }
                else if (jointType0 == JointType.HipCenter && jointType1 == JointType.HipRight)//Compruebo Punto cadera central con cadera derecha
                {
                    drawPen = cambiarColorHuesos();
                }
                else if (jointType0 == JointType.HipLeft && jointType1 == JointType.KneeLeft)//Compruebo Punto cadera izquierda con rodilla izquierda
                {
                    drawPen = cambiarColorHuesos();
                }
                else if (jointType0 == JointType.HipRight && jointType1 == JointType.KneeRight)//Compruebo Punto cadera derecha con rodilla derecha
                {
                    drawPen = cambiarColorHuesos();
                }
                else if (jointType0 == JointType.KneeLeft && jointType1 == JointType.AnkleLeft)//Compruebo Punto rodilla izquierda con tobillo izquierdo
                {
                    drawPen = cambiarColorHuesos();
                }
                else if (jointType0 == JointType.KneeRight && jointType1 == JointType.AnkleRight)//Compruebo Punto rodilla derecha con tobillo derecho
                {
                    drawPen = cambiarColorHuesos();
                }
                else if (jointType0 == JointType.ShoulderRight && jointType1 == JointType.ElbowRight)//Compruebo hombro derecho con codo derecho
                {
                    drawPen = cambiarColorHuesos();
                }
                else if (jointType0 == JointType.ShoulderLeft && jointType1 == JointType.ElbowLeft)//Compruebo hombro izquierdo con codo izquierdo
                {
                    drawPen = cambiarColorHuesos();
                }
                else if (jointType0 == JointType.ElbowRight && jointType1 == JointType.WristRight)//Compruebo codo derecho con muñeca derecha
                {
                    drawPen = cambiarColorHuesos();
                }
                else if (jointType0 == JointType.ElbowLeft && jointType1 == JointType.WristLeft)//Compruebo codo izquierdo con muñeca izquierda
                {
                    drawPen = cambiarColorHuesos();
                }
                else if (jointType0 == JointType.WristRight && jointType1 == JointType.HandRight)//Compruebo muñeca derecha con mano derecha
                {
                    drawPen = cambiarColorHuesos();
                }
                else if (jointType0 == JointType.WristLeft && jointType1 == JointType.HandLeft)//Compruebo muñeca izquierda con mano izquierda
                {
                    drawPen = cambiarColorHuesos();
                }
                else
                    drawPen = this.trackedBonePen;
            }

            drawingContext.DrawLine(drawPen, this.SkeletonPointToScreen(joint0.Position), this.SkeletonPointToScreen(joint1.Position));
        }
        //Método que comprueba la posición para devolver si tiene que pintar en un sitio o no.
        public Pen cambiarColorHuesos()
        {
            if (posicionInicialCorrecta)
            {
                if (correcto && correctoBrazo)
                {
                    return pintaHuesosFinal;
                }
                else if (baja && atrasBrazo)
                {
                    return pintaHuesosLlegando;
                }
                else
                {
                    return pintaHuesosInicial;
                }
            }
            else
            {
                return pintaHuesosMal;
            }
            
        }
        /*public Pen cambiarColorHuesosBrazos()
        {
            if (brazosRectos)
            {
                if (correctoBrazo)
                {
                    return pintaHuesosFinal;
                }
                else if (atrasBrazo)
                {
                    return pintaHuesosLlegando;
                }
                else
                {
                    return pintaHuesosInicial;
                }
            }
            else
            {
                return pintaHuesosMal;
            }

        }*/
        

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

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            brazosRectos = false;
            posicionInicialCorrecta = false;
            atrasBrazo = false;
            correctoBrazo = false;
            baja = false;
            correcto = false;
            finalizado = false;
            //error = false;
            //brazosFinalizado = false;
        }

        private void slider1_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            valorError = (float)this.slider1.Value/100;
        }
    }
}