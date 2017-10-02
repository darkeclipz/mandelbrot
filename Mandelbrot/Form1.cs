using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Mandelbrot
{
    public partial class Form1 : Form
    {
        bool busy = false;
        bool cancel = false;
        delegate void Cancel();
        Cancel OnCancel;
        double zoom = 1;
        double panX = 0;
        double panY = 0;
        double maxIterations = 50;
        double iterMultiplier = 1;

        public Form1()
        {
            InitializeComponent();
            DrawMandelbrot();
            OnCancel += DrawMandelbrot;
        }

        Complex GetComplex(double x, double y,  double width, double height, double scale, double panX, double panY, double absRange)
        {
            var min = width > height ? height : width;
            var step = absRange / (min * scale);
            return new Complex
            {
                Real = x * step - (absRange / 2) + panX,
                Imaginary = y * step - (absRange / 2) + panY
            };
        }

        public void DrawMandelbrot()
        {
            toolStripStatusLabel1.Text = $"Scale: {zoom}, PanX: {panX}, PanY: {panY}, iMul: {iterMultiplier}";

            if(busy)
            {
                cancel = true;
                return;
            }

            busy = true;

            Task.Run(new Action(() =>
            {
                double width = pictureBox1.Width;
                double height = pictureBox1.Height;
                double absRange = 4;
                //double maxIterations = 50;

                var bmp = new Bitmap((int)width, (int)height);
                var log = new StringBuilder();

                double zrsq = 0;
                double zisq = 0;

                var maxIterationsMultiplied = maxIterations * iterMultiplier;

                for (int i = 0; i < width && !cancel; i++)
                {
                    for (int j = 0; j < height && !cancel; j++)
                    {
                        var c = GetComplex(i, j, width, height, zoom, panX, panY, absRange);
                        var finalIterations = 0;
                        var z = new Complex();
                        var inSet = false;
                        zrsq = 0;
                        zisq = 0;
                        for (int k = 0; (zrsq+zisq) < 4 && !cancel; k++)
                        {
                            var temp = z;
                            zrsq = z.Real * z.Real;
                            zisq = z.Imaginary * z.Imaginary;
                            temp.Real = zrsq - zisq + c.Real;
                            temp.Imaginary = 2 * z.Real * z.Imaginary + c.Imaginary;
                            z = temp;

                            finalIterations = k;
                            if (k > maxIterationsMultiplied)
                            {
                                inSet = true;
                                break;
                            }
                        }
                        
                        //log.AppendLine($"x: {i}, y: {j}, complex: ({c.Real}, {c.Imaginary}i), z: ({c.Real}, {c.Imaginary}i), iter: {finalIterations}, inSet: {(c.Conjugate < 6 ? "Yes" : "No")}");

                        var p = finalIterations / ((maxIterations + 1) * iterMultiplier);
                        if (p > 1)
                        {
                            p = 1;
                        }
                        Color color = Color.FromArgb((int)(p * 255), (int)(p) > 0 ? (int)(p * 255) : 0, 0);
                        bmp.SetPixel(i, j, inSet ? Color.Black : color);
                    }
                    var progress = Math.Round((i + 1) / width * 100, 2);
                    Debug.Print($"-- Generating... ({progress}%)");
                    this.Invoke(new Action(() =>
                    {
                        try
                        {
                            toolStripProgressBar1.Value = (int)progress;
                            toolStripProgressBar1.Maximum = 100;
                        }
                        catch { }
                    }));
                }

                busy = false;
                if (!cancel)
                {
                    pictureBox1.Invoke(new Action(() =>
                    {
                        pictureBox1.Image = bmp;
                        toolStripProgressBar1.Value = 0;
                    }));
                }
                else
                {
                    cancel = false;
                    OnCancel?.Invoke();
                }
            }));
        }

        struct Point
        {
            public Point(int x, int y)
            {
                X = x;
                Y = y;
            }
            public int X { get; set; }
            public int Y { get; set; }
        }
        struct Complex
        {
            public Complex(double real, double imaginary)
            {
                Real = real;
                Imaginary = imaginary;
            }

            public double Real { get; set; }
            public double Imaginary { get; set; }
            //public double Conjugate { get { return Real * Real + Imaginary * Imaginary; } }
        }

        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            DrawMandelbrot();
        }

        private void Form1_KeyUp(object sender, KeyEventArgs e)
        {
            var panStep = (1 / (zoom * 2)) * 1;

            if (e.KeyCode == Keys.Up)
            {
                panY -= panStep;
            }
            else if(e.KeyCode == Keys.Down)
            {
                panY += panStep;
            }
            else if(e.KeyCode == Keys.Left)
            {
                panX -= panStep;
            }
            else if(e.KeyCode == Keys.Right)
            {
                panX += panStep;
            }
            else if(e.KeyCode == Keys.Add)
            {
                zoom *= 2;
            }
            else if(e.KeyCode == Keys.Subtract)
            {
                zoom /= 2;
            }
            else if(e.KeyCode == Keys.Multiply)
            {
                iterMultiplier += 1;
            }
            else if(e.KeyCode == Keys.Divide)
            {
                iterMultiplier -= 1;
                if (iterMultiplier <= 0)
                    iterMultiplier = 1;
            }

            DrawMandelbrot();
        }
    }
}
