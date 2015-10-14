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
        Agachado
    };

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //Variables donde almaceno toda la información requerida para calcular las posiciones necesarias.
        puntosMovimiento cadera, rodillaIzquierda, rodillaDerecha, tobilloIzquierdo, tobilloDerecho;
        puntosMovimiento rodillaInicialDerecha = new puntosMovimiento();
        puntosMovimiento rodillaInicialIzquierda = new puntosMovimiento();
        puntosMovimiento caderaInicial = new puntosMovimiento();
        puntosMovimiento tobillaInicialDerecho = new puntosMovimiento();
        puntosMovimiento tobillaInicialIzquierdo = new puntosMovimiento();
        puntosMovimiento rodillaIzquierdaActualizada = new puntosMovimiento();
        puntosMovimiento rodillaDerechaActualizada = new puntosMovimiento();
        puntosMovimiento caderaActualizada = new puntosMovimiento();
        //Variable para saber si ha finalizado el movimiento.
        bool finalizado = false;
        bool correcto = false;
        bool baja = false;
        bool error = false;
        const int numeroPostura = 10;
        int cont = 0;
        bool posicionInicialCorrecta = false;

        //Obtención si la postura es correcta o incorrecta.
        posturas posturaInicial = posturas.Mal;
        posturas posturaBajando = posturas.Sigue_Bajando;
        posturas posturaBien = posturas.Agachado;
        posturas posturaDetectada = posturas.Postura_Inicial;
        posturas posturaVuelta = posturas.Mal;
        

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
        private readonly Pen trackedBonePen = new Pen(Brushes.Green, 6);


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

            using (DrawingContext dc = this.drawingGroup.Open())
            {
                // Draw a transparent background to set the render size
                dc.DrawRectangle(Brushes.Black, null, new Rect(0.0, 0.0, RenderWidth, RenderHeight));

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
            obtenerPuntos(skeletons);
            
        }
        //Compruebo si es un la postura correcta.
        private void posturaCorrecta(puntosMovimiento cadera, puntosMovimiento rodillaIzquierda, puntosMovimiento rodillaDerecha, puntosMovimiento tobilloDerecho, puntosMovimiento tobilloIzquierdo)
        {
            //Compruebo la postura inicial es con las piernas rectas.
            //Si entro en este primer If, lo que ontengo es que el usuario esta en la posición correcta y almacenado de referencia esa postura.
            if (detectoRecto(cadera, rodillaIzquierda, rodillaDerecha, tobilloDerecho, tobilloIzquierdo))
            {
                if (deteccionDePostura(posturas.Postura_Inicial))
                {
                    finalizado = false;
                    solucionP.Content = posturaInicial.ToString();
                    posicionInicialCorrecta = true;
                    //Guardo las cadera en la posicion correcta.         
                    caderaInicial.X = cadera.X;
                    caderaActualizada.Y = caderaInicial.Y = cadera.Y;
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
                }
            }//Si ya esta almacenada la posicion correcta o esta mal entra aqui.
            else
            {
                if(posicionInicialCorrecta)//Si ha partido de la posición correcta.
                {
                    if (detectoPosicionFinal(cadera, caderaActualizada, rodillaInicialDerecha))//Compruebo que el usuario ha llegado a la posición final.
                    {
                        finalizado = true;//Guardo que ha finalizado.
                        solucionP.Content = posturaBien.ToString();
                        correcto = true;
                        baja = false;
                    }
                    else if (detectoBajando(cadera, caderaActualizada) && !finalizado)//Compruebo que no se esta bajando y no ha llegado a la posición final.
                    {
                        solucionP.Content = posturaBajando.ToString();
                        baja = true;
                        correcto = false;
                    }
                    else if(detectaSubida(cadera, caderaActualizada))//Compruebo que el usuario no suba de la posicion inicial que hemos guardado anteriormente.
                    {
                        solucionP.Content = "Vuelva a Empezar";//Mensaje por pantalla para que el usuario no suba.
                        posicionInicialCorrecta = false;
                        
                    }
                }
                else if (deteccionDePostura(posturas.Mal))
                {
                    solucionP.Content = "Coloquese en la posición de inicio";
                    baja = false;
                    correcto = false;
                }
            }
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
            if (caderaActualizada.Y > cadera.Y + 0.01)
            {
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
                caderaActualizada.Y = cadera.Y;
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
        //Método que comprueba si se parte de la posición donde el usuario este completamente recto, es decir con una abertura de unos 10 cm entre los dos pies.
        bool detectoRecto(puntosMovimiento cadera, puntosMovimiento rodillaIzquierda, puntosMovimiento rodillaDerecha, puntosMovimiento tobilloIzquierdo, puntosMovimiento tobilloDerecho)
        {
            if ((rodillaIzquierda.Z - tobilloIzquierdo.Z) - (cadera.Z - rodillaIzquierda.Z) > 0.01f && (rodillaDerecha.Z - tobilloDerecho.Z) - (cadera.Z - rodillaDerecha.Z) > 0.01f)
            {
                return true;
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
                    
                }
            }
            posturaCorrecta(cadera, rodillaIzquierda, rodillaDerecha, tobilloIzquierdo, tobilloDerecho);
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
                else if (jointType0 == JointType.KneeLeft && jointType1 == JointType.AnkleLeft)//Compruebo Punto rodilla izquierda con tobillo izquierda
                {
                    drawPen = cambiarColorHuesos();
                }
                else if (jointType0 == JointType.KneeRight && jointType1 == JointType.AnkleRight)//Compruebo Punto rodilla derecha con tobillo derecha
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
                if (correcto)
                {
                    return pintaHuesosFinal;
                }
                else if (baja)
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
    }
}