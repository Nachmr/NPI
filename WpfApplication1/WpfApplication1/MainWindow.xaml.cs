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

    public struct Coordenadas
    {
        public float X;
        public float Y;
        public float Z;
    };

    public enum Posicion
    {
        Inicial,
        Sigue_bajando,
        Agachado,
        Salto,
        Salta_mas
    };

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //Tobillo incluido para comprobar que al empezar, no se está ya agachado
        Coordenadas cadera, cabeza, tobilloderecho, tobilloizquierdo, caderainicial, cabezainicial, tobilloderechoinicial, tobilloizquierdoincial;

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
        private readonly Pen pintaHuesosFinal = new Pen(Brushes.Green, 8);
        //Pinta en color turquesa si esta llegando a la posicion final.
        private readonly Pen pintaHuesosLlegando = new Pen(Brushes.Turquoise, 8);
        //Pinta en rojo si esta haciendo un movimiento incorrecto.
        private readonly Pen pintaHuesosMal = new Pen(Brushes.Red, 8);
    }
}